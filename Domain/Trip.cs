namespace my_project.Domain;

/// <summary>One weekend away. The aggregate every later feature hangs off.</summary>
public class Trip
{
    public const int MaxNameLength = 100;
    public const int MaxLocationLength = 200;

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public Guid StarterParticipantId { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public List<Participant> Participants { get; private set; } = [];
    public List<InviteLink> InviteLinks { get; private set; } = [];

    private Trip() { }

    /// <summary>
    /// Creates a trip together with its starter participant and its first invite link —
    /// a trip is never valid without both (INV-4, INV-5, FR-005, FR-006).
    /// </summary>
    /// <param name="today">Injected rather than read from the clock so INV-2 is testable.</param>
    public static Trip Create(
        string name,
        string location,
        DateOnly startDate,
        DateOnly endDate,
        string starterDisplayName,
        DateOnly today,
        string inviteToken)
    {
        name = (name ?? string.Empty).Trim();
        location = (location ?? string.Empty).Trim();

        if (name.Length is 0 or > MaxNameLength)
            throw new ArgumentException("Trip name must be 1–100 characters.", nameof(name));
        if (location.Length is 0 or > MaxLocationLength)
            throw new ArgumentException("Location must be 1–200 characters.", nameof(location));
        if (endDate < startDate)
            throw new ArgumentException("The end date cannot be before the start date.", nameof(endDate));
        if (startDate < today)
            throw new ArgumentException("A trip cannot start in the past.", nameof(startDate));

        var trip = new Trip
        {
            Name = name,
            Location = location,
            StartDate = startDate,
            EndDate = endDate,
        };

        var starter = new Participant(trip.Id, starterDisplayName);
        trip.Participants.Add(starter);
        trip.StarterParticipantId = starter.Id;

        trip.InviteLinks.Add(new InviteLink(trip.Id, inviteToken));

        return trip;
    }

    /// <summary>How many are in, out and unsure right now — derived, never stored (FR-007, INV-13).</summary>
    public Headcount Headcount => Headcount.Of(Participants);

    /// <summary>
    /// Where this trip sits relative to <paramref name="today"/> (FR-016). The caller
    /// passes one <c>today</c> for the whole page so a trip cannot flicker between
    /// current and past within a single render (EC-13).
    /// </summary>
    public TripPhase PhaseOn(DateOnly today) =>
        today < StartDate ? TripPhase.Upcoming
        : today <= EndDate ? TripPhase.Current
        : TripPhase.Past;

    /// <summary>Whole days until the start; 0 on the start date itself, negative after (FR-012, EC-10).</summary>
    public int DaysUntilStart(DateOnly today) => StartDate.DayNumber - today.DayNumber;

    /// <summary>The one invite link that currently admits new participants, if any (INV-6).</summary>
    public InviteLink? ActiveInviteLink => InviteLinks.SingleOrDefault(l => l.IsActive);

    public bool IsStarter(Guid participantId) => participantId == StarterParticipantId;

    /// <summary>
    /// Adds a participant. Returns null when the name is already taken in this trip
    /// (INV-3); the database's unique index is the real backstop under concurrency (EC-1).
    /// </summary>
    public Participant? AddParticipant(string displayName)
    {
        if (!DisplayNameRules.TryNormalize(displayName, out var normalized)) return null;
        if (IsNameTaken(normalized, exceptParticipantId: null)) return null;

        var participant = new Participant(Id, normalized);
        Participants.Add(participant);
        return participant;
    }

    public bool IsNameTaken(string displayName, Guid? exceptParticipantId)
    {
        var key = DisplayNameRules.ToComparisonKey(displayName);
        return Participants.Any(p => p.ComparisonKey == key && p.Id != exceptParticipantId);
    }

    /// <summary>Revokes the active link, if there is one. Existing participants are untouched (INV-8).</summary>
    public void RevokeInviteLink() => ActiveInviteLink?.Revoke();

    /// <summary>
    /// Replaces the active link: the incumbent is revoked in the same step, so a trip
    /// never has two live links (INV-6, EC-2).
    /// </summary>
    public InviteLink RegenerateInviteLink(string newToken)
    {
        RevokeInviteLink();
        var link = new InviteLink(Id, newToken);
        InviteLinks.Add(link);
        return link;
    }
}
