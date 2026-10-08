namespace my_project.Domain;

/// <summary>
/// Links one visitor to one participant. A participant may be bound from several
/// visitors — phone and laptop (FR-021).
/// </summary>
public class ParticipantBinding
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid VisitorId { get; private set; }
    public Guid ParticipantId { get; private set; }
    public DateTime BoundAt { get; private set; } = DateTime.UtcNow;

    public Participant Participant { get; private set; } = null!;

    private ParticipantBinding() { }

    internal ParticipantBinding(Guid visitorId, Guid participantId)
    {
        VisitorId = visitorId;
        ParticipantId = participantId;
    }
}
