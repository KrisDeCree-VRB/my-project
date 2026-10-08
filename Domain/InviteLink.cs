namespace my_project.Domain;

public enum InviteLinkStatus
{
    Active,
    Revoked,
}

/// <summary>A shareable credential admitting new participants to one trip.</summary>
public class InviteLink
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid TripId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public InviteLinkStatus Status { get; private set; } = InviteLinkStatus.Active;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; private set; }

    private InviteLink() { }

    internal InviteLink(Guid tripId, string token)
    {
        TripId = tripId;
        Token = token;
    }

    public bool IsActive => Status == InviteLinkStatus.Active;

    /// <summary>Revocation is terminal — a revoked link never becomes active again (INV-7).</summary>
    internal void Revoke()
    {
        if (Status == InviteLinkStatus.Revoked) return;

        Status = InviteLinkStatus.Revoked;
        RevokedAt = DateTime.UtcNow;
    }
}
