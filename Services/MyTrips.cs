using my_project.Domain;

namespace my_project.Services;

/// <summary>One row of the trips list: the trip, who this browser is in it, and the
/// derived facts the row shows (FR-011, FR-012).</summary>
public record TripSummary(Trip Trip, Participant Me, Headcount Headcount, TripPhase Phase, int DaysUntilStart);

/// <summary>
/// Everything one browser can see of its own trips, already split and ordered (FR-015).
/// Private to the visitor it was built for; nothing here is addressable by anyone else
/// (INV-15, NFR-004).
/// </summary>
public record MyTrips(IReadOnlyList<TripSummary> CurrentAndUpcoming, IReadOnlyList<TripSummary> Past)
{
    public static readonly MyTrips Empty = new([], []);

    public bool IsEmpty => CurrentAndUpcoming.Count == 0 && Past.Count == 0;
}
