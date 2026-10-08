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
}
