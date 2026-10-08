using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using my_project.Domain;
using my_project.Services;

namespace my_project.Pages;

/// <summary>
/// The invite entry point. Rate-limited so the token space cannot be probed in bulk
/// (NFR-004).
/// </summary>
[EnableRateLimiting("invite")]
public class JoinModel(TripService trips) : PageModel
{
    public Trip? Trip { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = string.Empty;

    [BindProperty]
    public string? DisplayName { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Trip = await trips.FindTripByActiveInviteAsync(Token);

        // An unknown token and a revoked one give the same answer, so the link cannot be
        // used to discover which trips exist (SC-010, SC-011).
        if (Trip is null) return InviteNotValid();

        // Already one of us on this browser: straight in, no prompt, no second identity
        // (FR-016, SC-013).
        if (await trips.GetBoundParticipantAsync(Trip.Id) is not null)
            return RedirectToTrip(Trip.Id);

        return Page();
    }

    public Task<IActionResult> OnPostJoinAsync() =>
        HandleAsync(() => trips.JoinAsync(Token, DisplayName ?? string.Empty));

    public Task<IActionResult> OnPostResumeAsync(Guid participantId) =>
        HandleAsync(() => trips.ResumeAsync(Token, participantId));

    private async Task<IActionResult> HandleAsync(Func<Task<JoinOutcome>> action)
    {
        var outcome = await action();
        if (outcome.Succeeded) return RedirectToTrip(outcome.Trip!.Id);

        switch (outcome.Error)
        {
            case JoinError.InviteNotValid:
                return InviteNotValid();

            // The browser gained an identity in this trip meanwhile — just go in.
            case JoinError.AlreadyInTrip:
                Trip = await trips.FindTripByActiveInviteAsync(Token);
                return Trip is null ? InviteNotValid() : RedirectToTrip(Trip.Id);

            default:
                ModelState.AddModelError(nameof(DisplayName), outcome.Error switch
                {
                    JoinError.NameTaken => "Someone in this trip already uses that name. Pick another one.",
                    JoinError.ParticipantNotFound => "That person is no longer in this trip.",
                    _ => "Enter a name of 1 to 50 characters.",
                });
                Trip = await trips.FindTripByActiveInviteAsync(Token);
                return Trip is null ? InviteNotValid() : Page();
        }
    }

    private IActionResult RedirectToTrip(Guid tripId) =>
        RedirectToPage("/Trips/Details", new { tripId });

    private IActionResult InviteNotValid() => RedirectToPage("/InviteNotValid");
}
