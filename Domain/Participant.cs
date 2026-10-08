namespace my_project.Domain;

/// <summary>A person in one trip. No account; scoped to its trip.</summary>
public class Participant
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid TripId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Uppercased, trimmed copy of <see cref="DisplayName"/>. Persisted so the database
    /// can carry the per-trip unique index that enforces INV-3 under concurrency (EC-1).
    /// </summary>
    public string ComparisonKey { get; private set; } = string.Empty;

    public DateTime JoinedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>Starts at <see cref="AttendanceStatus.Unsure"/> for everyone, starter included (FR-002).</summary>
    public AttendanceStatus AttendanceStatus { get; private set; } = AttendanceStatus.Unsure;

    /// <summary>Null until the first real change, then set on every change (FR-005).</summary>
    public DateTime? StatusChangedAt { get; private set; }

    public Trip Trip { get; private set; } = null!;

    private Participant() { }

    internal Participant(Guid tripId, string displayName)
    {
        if (!DisplayNameRules.TryNormalize(displayName, out var normalized))
            throw new ArgumentException("Invalid display name.", nameof(displayName));

        TripId = tripId;
        Rename(normalized);
    }

    /// <summary>Changes this participant's own display name (FR-018).</summary>
    public void Rename(string displayName)
    {
        if (!DisplayNameRules.TryNormalize(displayName, out var normalized))
            throw new ArgumentException("Invalid display name.", nameof(displayName));

        DisplayName = normalized;
        ComparisonKey = DisplayNameRules.ToComparisonKey(normalized);
    }

    /// <summary>
    /// Records this participant's own answer (FR-003). Nothing ever makes it final
    /// (INV-12). Re-affirming the current value is accepted but does not advance
    /// <see cref="StatusChangedAt"/> — it is not answering anew (EC-2).
    /// </summary>
    /// <remarks>
    /// Only ever reached through a binding to this participant, which is where INV-11
    /// is enforced; the entity itself has no way to know who is asking.
    /// </remarks>
    public void SetAttendance(AttendanceStatus status, DateTime changedAt)
    {
        if (!Enum.IsDefined(status))
            throw new ArgumentOutOfRangeException(nameof(status)); // EC-3.

        if (status == AttendanceStatus) return;

        AttendanceStatus = status;
        StatusChangedAt = changedAt;
    }
}
