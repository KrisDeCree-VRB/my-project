namespace my_project.Tests.Integration;

/// <summary>
/// Base for the integration tests: a fresh app and database per test, plus the two moves
/// every scenario starts from — start a trip, and open its invite link.
/// </summary>
public abstract class AppTest : IDisposable
{
    protected readonly WeekendAwayApp App = new();

    protected static readonly DateOnly Start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
    protected static readonly DateOnly End = Start.AddDays(2);

    public void Dispose()
    {
        App.Dispose();
        GC.SuppressFinalize(this);
    }

    protected static Dictionary<string, string> CreateTripForm(
        string name = "Ardennes weekend",
        string location = "Durbuy",
        DateOnly? start = null,
        DateOnly? end = null,
        string displayName = "Kris") => new()
        {
            ["Input.Name"] = name,
            ["Input.Location"] = location,
            ["Input.StartDate"] = (start ?? Start).ToString("yyyy-MM-dd"),
            ["Input.EndDate"] = (end ?? End).ToString("yyyy-MM-dd"),
            ["Input.DisplayName"] = displayName,
        };

    /// <summary>Starts a trip on a fresh device and returns that device plus its trip page.</summary>
    protected async Task<Trip> StartTripAsync(string starter = "Kris", string name = "Ardennes weekend")
    {
        var device = App.NewDevice();
        var response = await device.SubmitFormAsync("/trips/create", "/trips/create",
            CreateTripForm(name: name, displayName: starter));

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        var path = response.RequestMessage!.RequestUri!.AbsolutePath;

        return new Trip(device, path, html, BrowserExtensions.InvitePath(html)!);
    }

    protected record Trip(HttpClient Starter, string Path, string Html, string InvitePath);

    /// <summary>Joins a trip from a new device under a new display name.</summary>
    protected async Task<HttpResponseMessage> JoinAsync(HttpClient device, string invitePath, string displayName) =>
        await device.SubmitFormAsync(invitePath, $"{invitePath}?handler=Join",
            new Dictionary<string, string> { ["DisplayName"] = displayName });
}
