namespace my_project.Domain;

/// <summary>
/// The answer to "are you coming?" (FR-001). There is no null or missing state: not
/// having answered is <see cref="Unsure"/>, which is a real answer, not an absence
/// (INV-10).
/// </summary>
public enum AttendanceStatus
{
    Unsure = 0,
    In = 1,
    Out = 2,
}
