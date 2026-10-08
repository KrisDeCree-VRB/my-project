using Microsoft.EntityFrameworkCore;

namespace my_project.Tests.Integration;

public class CreateTripTests : AppTest
{
    [Fact] // SC-001
    public async Task Creating_a_trip_shows_it_with_the_starter_and_an_invite_link()
    {
        var trip = await StartTripAsync();

        Assert.Contains("Ardennes weekend", trip.Html);
        Assert.Contains("Durbuy", trip.Html);
        Assert.Contains("Kris", trip.Html);
        Assert.StartsWith("/join/", trip.InvitePath);

        await using var db = App.NewDbContext();
        var stored = await db.Trips.Include(t => t.Participants).SingleAsync();
        var starter = Assert.Single(stored.Participants);
        Assert.Equal("Kris", starter.DisplayName);
        Assert.Equal(starter.Id, stored.StarterParticipantId);
        Assert.Equal(Start, stored.StartDate);
        Assert.Equal(End, stored.EndDate);
    }

    [Fact] // SC-001: this browser is bound to the starter (FR-019)
    public async Task Creating_a_trip_binds_this_browser_to_the_starter()
    {
        await StartTripAsync();

        await using var db = App.NewDbContext();
        var binding = await db.ParticipantBindings.Include(b => b.Participant).SingleAsync();
        Assert.Equal("Kris", binding.Participant.DisplayName);
        Assert.Single(db.Visitors);
    }

    [Fact] // SC-002
    public async Task End_date_before_start_date_is_refused_and_the_form_keeps_what_I_typed()
    {
        var device = App.NewDevice();

        var response = await device.SubmitFormAsync("/trips/create", "/trips/create",
            CreateTripForm(start: Start, end: Start.AddDays(-2)));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("end date cannot be before the start date", html);
        Assert.Contains("Ardennes weekend", html); // still in the form
        Assert.Contains("Durbuy", html);
        Assert.Empty(App.NewDbContext().Trips);
    }

    [Fact] // SC-003
    public async Task A_start_date_in_the_past_is_refused()
    {
        var device = App.NewDevice();
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);

        var response = await device.SubmitFormAsync("/trips/create", "/trips/create",
            CreateTripForm(start: yesterday, end: yesterday.AddDays(3)));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("cannot start in the past", html);
        Assert.Empty(App.NewDbContext().Trips);
    }

    [Theory] // FR-002, FR-005
    [InlineData("Input.Name", "")]
    [InlineData("Input.Location", "")]
    [InlineData("Input.DisplayName", "   ")]
    public async Task Required_fields_are_enforced(string field, string value)
    {
        var device = App.NewDevice();
        var form = CreateTripForm();
        form[field] = value;

        await device.SubmitFormAsync("/trips/create", "/trips/create", form);

        Assert.Empty(App.NewDbContext().Trips);
    }

    [Fact] // NFR-006
    public async Task The_trip_page_asks_search_engines_to_stay_away()
    {
        var trip = await StartTripAsync();

        Assert.Contains("noindex", trip.Html);
    }
}
