namespace my_project.Domain;

/// <summary>
/// A derived, read-only summary of a trip's participants (§5.3). Never stored; always
/// computed, so <c>In + Out + Unsure</c> cannot drift from the participant count (INV-13).
/// </summary>
public readonly record struct Headcount(int In, int Out, int Unsure)
{
    public int Total => In + Out + Unsure;

    public static Headcount Of(IEnumerable<Participant> participants)
    {
        var (inCount, outCount, unsureCount) = (0, 0, 0);

        foreach (var participant in participants)
        {
            switch (participant.AttendanceStatus)
            {
                case AttendanceStatus.In: inCount++; break;
                case AttendanceStatus.Out: outCount++; break;
                default: unsureCount++; break;
            }
        }

        return new Headcount(inCount, outCount, unsureCount);
    }
}
