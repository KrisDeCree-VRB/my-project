using Microsoft.AspNetCore.Mvc.RazorPages;
using my_project.Services;

namespace my_project.Pages.Trips;

/// <summary>
/// The trips this browser has joined (story 005). Built only from this visitor's own
/// bindings, so there is nothing here to address as someone else (INV-15, NFR-004).
/// </summary>
public class IndexModel(TripService trips) : PageModel
{
    public MyTrips Trips { get; private set; } = MyTrips.Empty;

    public async Task OnGetAsync() => Trips = await trips.GetMyTripsAsync();
}
