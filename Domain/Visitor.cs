namespace my_project.Domain;

/// <summary>
/// One browser that has used the system — the device-level identity. The session token
/// lives in an HttpOnly cookie; the bindings say who this browser is in each trip.
/// </summary>
public class Visitor
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string SessionToken { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; private set; } = DateTime.UtcNow;

    public List<ParticipantBinding> Bindings { get; private set; } = [];

    private Visitor() { }

    public Visitor(string sessionToken) => SessionToken = sessionToken;

    public void Touch() => LastSeenAt = DateTime.UtcNow;

    /// <summary>
    /// Binds this browser to a participant. INV-14 (at most one binding per trip) is
    /// enforced by the caller, which has to look at the trip's participants anyway.
    /// </summary>
    public ParticipantBinding BindTo(Participant participant)
    {
        var binding = new ParticipantBinding(Id, participant.Id);
        Bindings.Add(binding);
        return binding;
    }
}
