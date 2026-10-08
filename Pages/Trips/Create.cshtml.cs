using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using my_project.Domain;
using my_project.Services;

namespace my_project.Pages.Trips;

public class CreateModel(TripService trips, TimeProvider timeProvider) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Give the trip a name.")]
        [StringLength(Trip.MaxNameLength, MinimumLength = 1)]
        [Display(Name = "Trip name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Where are you going?")]
        [StringLength(Trip.MaxLocationLength, MinimumLength = 1)]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pick a start date.")]
        [DataType(DataType.Date)]
        [Display(Name = "Start date")]
        public DateOnly? StartDate { get; set; }

        [Required(ErrorMessage = "Pick an end date.")]
        [DataType(DataType.Date)]
        [Display(Name = "End date")]
        public DateOnly? EndDate { get; set; }

        [Required(ErrorMessage = "Tell the group who you are.")]
        [StringLength(DisplayNameRules.MaxLength, MinimumLength = 1)]
        [Display(Name = "Your name")]
        public string DisplayName { get; set; } = string.Empty;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        // INV-1 / INV-2. Reported on the form so the entered details survive (SC-002).
        if (Input.EndDate < Input.StartDate)
            ModelState.AddModelError("Input.EndDate", "The end date cannot be before the start date.");
        if (Input.StartDate < today)
            ModelState.AddModelError("Input.StartDate", "A trip cannot start in the past.");
        if (!DisplayNameRules.TryNormalize(Input.DisplayName, out _))
            ModelState.AddModelError("Input.DisplayName", "Enter a name of 1 to 50 characters.");

        if (!ModelState.IsValid) return Page();

        var trip = await trips.CreateTripAsync(
            Input.Name, Input.Location, Input.StartDate!.Value, Input.EndDate!.Value, Input.DisplayName);

        return RedirectToPage("/Trips/Details", new { tripId = trip.Id, created = true });
    }
}
