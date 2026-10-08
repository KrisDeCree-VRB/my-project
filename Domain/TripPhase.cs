namespace my_project.Domain;

/// <summary>
/// Where a trip sits in time. A trip counts as <see cref="UnderWay"/> from its start
/// date up to and including its end date, and stays above past trips while it does
/// (FR-016).
/// </summary>
public enum TripPhase
{
    Upcoming,
    Current,
    Past,
}
