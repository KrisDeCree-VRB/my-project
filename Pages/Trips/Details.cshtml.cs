using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using my_project.Domain;
using my_project.Services;

namespace my_project.Pages.Trips;

public class DetailsModel(TripService trips) : PageModel
{
    public Trip Trip { get; private set; } = null!;
    public Participant Me { get; private set; } = null!;

    /// <summary>The full invite link, shown to every participant while it is active (FR-008).</summary>
    public string? InviteUrl { get; private set; }

    public bool IsStarter => Trip.IsStarter(Me.Id);

    /// <summary>Set straight after creating the trip, to greet the starter with the link.</summary>
    [BindProperty(SupportsGet = true)]
    public bool Created { get; set; }

    [BindProperty]
    public string? NewDisplayName { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid tripId) =>
        await LoadAsync(tripId) ?? Page();

    /// <summary>How many are in, out and unsure (FR-007).</summary>
    public Headcount Headcount => Trip.Headcount;

    /// <summary>
    /// Records my own answer (FR-003). The posted value says what, never who: who comes
    /// from the binding inside the service (INV-11, NFR-003).
    /// </summary>
    public async Task<IActionResult> OnPostAttendanceAsync(Guid tripId, string? status)
    {
        // Parsed here rather than model-bound: a bad value must be rejected outright, not
        // quietly bound to the enum's default (EC-3). TryParse also accepts raw numbers,
        // hence IsDefined.
        if (!Enum.TryParse<AttendanceStatus>(status, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
            return BadRequest();

        if (!await trips.SetAttendanceAsync(tripId, parsed))
            return NotFound(); // Not a participant, or an unknown trip — the same answer either way (EC-4).

        return RedirectToPage(new { tripId });
    }

    public async Task<IActionResult> OnPostRevokeAsync(Guid tripId)
    {
        if (await LoadAsync(tripId) is { } failure) return failure;
        if (!IsStarter) return StatusCode(StatusCodes.Status403Forbidden); // FR-009: only the starter controls the link.

        await trips.RevokeInviteLinkAsync(Trip);
        return RedirectToPage(new { tripId });
    }

    public async Task<IActionResult> OnPostRegenerateAsync(Guid tripId)
    {
        if (await LoadAsync(tripId) is { } failure) return failure;
        if (!IsStarter) return StatusCode(StatusCodes.Status403Forbidden); // FR-010.

        await trips.RegenerateInviteLinkAsync(Trip);
        return RedirectToPage(new { tripId });
    }

    public async Task<IActionResult> OnPostRenameAsync(Guid tripId)
    {
        if (await LoadAsync(tripId) is { } failure) return failure;

        var outcome = await trips.RenameAsync(Trip, Me, NewDisplayName ?? string.Empty);
        if (!outcome.Succeeded)
        {
            ModelState.AddModelError(nameof(NewDisplayName), outcome.Error switch
            {
                JoinError.NameTaken => "Someone in this trip already uses that name.",
                _ => "Enter a name of 1 to 50 characters.",
            });
            return Page();
        }

        return RedirectToPage(new { tripId });
    }

    /// <summary>
    /// Loads the trip and this browser's identity in it. Returns a result to short-circuit
    /// with when the browser has no business here; null when the page may render.
    /// A visitor that is not a participant learns nothing, not even that the trip exists
    /// (NFR-006).
    /// </summary>
    private async Task<IActionResult?> LoadAsync(Guid tripId)
    {
        var trip = await trips.FindTripAsync(tripId);
        var me = trip is null ? null : await trips.GetBoundParticipantAsync(trip.Id);

        if (trip is null || me is null) return NotFound();

        Trip = trip;
        Me = me;

        var token = trip.ActiveInviteLink?.Token;
        InviteUrl = token is null ? null : $"{Request.Scheme}://{Request.Host}/join/{token}";

        return null;
    }
}
