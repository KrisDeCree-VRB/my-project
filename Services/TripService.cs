using Microsoft.EntityFrameworkCore;
using my_project.Data;
using my_project.Domain;

namespace my_project.Services;

public enum JoinError
{
    InviteNotValid,
    NameInvalid,
    NameTaken,
    AlreadyInTrip,
    ParticipantNotFound,
}

public record JoinOutcome(Trip? Trip, Participant? Participant, JoinError? Error)
{
    public bool Succeeded => Error is null;

    public static JoinOutcome Ok(Trip trip, Participant participant) => new(trip, participant, null);
    public static JoinOutcome Fail(JoinError error) => new(null, null, error);
}

/// <summary>
/// The operations of spec 001-003: create a trip, open an invite, join or resume, and
/// control the link. The domain holds the invariants; this layer loads, binds and saves.
/// </summary>
public class TripService(AppDbContext db, VisitorIdentity visitorIdentity, TimeProvider timeProvider)
{
    // Entities carry client-generated Guid keys, so EF cannot tell a new child of an
    // already-tracked parent from an existing row and would issue an UPDATE. Every new
    // entity added to a loaded graph is therefore registered on its DbSet explicitly.

    private IQueryable<Trip> TripsWithDetail => db.Trips
        .Include(t => t.Participants)
        .Include(t => t.InviteLinks);

    public Task<Trip?> FindTripAsync(Guid tripId) =>
        TripsWithDetail.SingleOrDefaultAsync(t => t.Id == tripId);

    /// <summary>
    /// Creates the trip, its starter and its first invite link, and binds this browser to
    /// the starter (FR-001, FR-004, FR-005, FR-019).
    /// </summary>
    public async Task<Trip> CreateTripAsync(string name, string location, DateOnly startDate, DateOnly endDate, string starterDisplayName)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var trip = Trip.Create(name, location, startDate, endDate, starterDisplayName, today, TokenGenerator.Create());

        db.Trips.Add(trip);

        var visitor = await visitorIdentity.GetOrCreateAsync();
        db.ParticipantBindings.Add(visitor.BindTo(trip.Participants[0]));

        await db.SaveChangesAsync();
        return trip;
    }

    /// <summary>
    /// The trip an active invite token opens, or null. An unknown token and a revoked one
    /// are the same answer on purpose, so the link cannot probe which trips exist (SC-011).
    /// </summary>
    public async Task<Trip?> FindTripByActiveInviteAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var link = await db.InviteLinks.SingleOrDefaultAsync(l => l.Token == token);
        if (link is null || !link.IsActive) return null;

        return await FindTripAsync(link.TripId);
    }

    /// <summary>
    /// The participant this browser is in the given trip, or null if it is not in it.
    /// A binding whose participant has vanished is ignored (EC-13).
    /// </summary>
    public async Task<Participant?> GetBoundParticipantAsync(Guid tripId)
    {
        var visitor = await visitorIdentity.GetCurrentAsync();
        return visitor?.Bindings
            .Select(b => b.Participant)
            .FirstOrDefault(p => p is not null && p.TripId == tripId);
    }

    /// <summary>Joins as a brand-new participant (FR-014).</summary>
    public async Task<JoinOutcome> JoinAsync(string token, string displayName)
    {
        var trip = await FindTripByActiveInviteAsync(token);
        if (trip is null) return JoinOutcome.Fail(JoinError.InviteNotValid);

        if (await GetBoundParticipantAsync(trip.Id) is not null)
            return JoinOutcome.Fail(JoinError.AlreadyInTrip);

        if (!DisplayNameRules.TryNormalize(displayName, out var normalized))
            return JoinOutcome.Fail(JoinError.NameInvalid);

        var participant = trip.AddParticipant(normalized);
        if (participant is null) return JoinOutcome.Fail(JoinError.NameTaken);
        db.Participants.Add(participant);

        var visitor = await visitorIdentity.GetOrCreateAsync();
        db.ParticipantBindings.Add(visitor.BindTo(participant));

        return await SaveJoinAsync(trip, participant);
    }

    /// <summary>
    /// Binds this browser to a participant that already exists, so the same person on a
    /// second device does not become a second participant (FR-020, FR-021).
    /// </summary>
    public async Task<JoinOutcome> ResumeAsync(string token, Guid participantId)
    {
        var trip = await FindTripByActiveInviteAsync(token);
        if (trip is null) return JoinOutcome.Fail(JoinError.InviteNotValid);

        if (await GetBoundParticipantAsync(trip.Id) is not null)
            return JoinOutcome.Fail(JoinError.AlreadyInTrip);

        var participant = trip.Participants.SingleOrDefault(p => p.Id == participantId);
        if (participant is null) return JoinOutcome.Fail(JoinError.ParticipantNotFound);

        var visitor = await visitorIdentity.GetOrCreateAsync();
        db.ParticipantBindings.Add(visitor.BindTo(participant));

        return await SaveJoinAsync(trip, participant);
    }

    /// <summary>Renames a participant, subject to the per-trip uniqueness rule (FR-018).</summary>
    public async Task<JoinOutcome> RenameAsync(Trip trip, Participant participant, string displayName)
    {
        if (!DisplayNameRules.TryNormalize(displayName, out var normalized))
            return JoinOutcome.Fail(JoinError.NameInvalid);

        if (trip.IsNameTaken(normalized, exceptParticipantId: participant.Id))
            return JoinOutcome.Fail(JoinError.NameTaken);

        participant.Rename(normalized);
        return await SaveJoinAsync(trip, participant);
    }

    public async Task RevokeInviteLinkAsync(Trip trip)
    {
        trip.RevokeInviteLink();
        await db.SaveChangesAsync();
    }

    public async Task RegenerateInviteLinkAsync(Trip trip)
    {
        db.InviteLinks.Add(trip.RegenerateInviteLink(TokenGenerator.Create()));
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Saves a join or rename, turning the unique-index violation that a simultaneous
    /// join causes into the same "name taken" answer the in-memory check gives (EC-1).
    /// </summary>
    private async Task<JoinOutcome> SaveJoinAsync(Trip trip, Participant participant)
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return JoinOutcome.Fail(JoinError.NameTaken);
        }

        return JoinOutcome.Ok(trip, participant);
    }
}
