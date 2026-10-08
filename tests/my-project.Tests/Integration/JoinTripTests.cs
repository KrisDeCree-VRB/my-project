using System.Net;
using Microsoft.EntityFrameworkCore;

namespace my_project.Tests.Integration;

public class JoinTripTests : AppTest
{
    [Fact] // SC-005
    public async Task The_invite_shows_the_trip_before_I_commit_to_joining()
    {
        var trip = await StartTripAsync();
        var friend = App.NewDevice();

        var html = await friend.GetHtmlAsync(trip.InvitePath);

        Assert.Contains("Ardennes weekend", html);
        Assert.Contains("Durbuy", html);
        Assert.Contains("Kris", html);

        // Looking is not joining.
        await using var db = App.NewDbContext();
        Assert.Equal(1, await db.Participants.CountAsync());
    }

    [Fact] // SC-005
    public async Task Joining_makes_me_a_participant_the_others_can_see()
    {
        var trip = await StartTripAsync();
        var friend = App.NewDevice();

        var response = await JoinAsync(friend, trip.InvitePath, "Sam");

        response.EnsureSuccessStatusCode();
        Assert.Equal(trip.Path, response.RequestMessage!.RequestUri!.AbsolutePath);
        Assert.Contains("Sam", await response.Content.ReadAsStringAsync());

        var asKrisSeesIt = await trip.Starter.GetHtmlAsync(trip.Path);
        Assert.Contains("Sam", asKrisSeesIt);

        await using var db = App.NewDbContext();
        Assert.Equal(2, await db.Participants.CountAsync());
    }

    [Theory] // SC-006, EC-9
    [InlineData("Sam")]
    [InlineData("  sam  ")]
    [InlineData("SAM")]
    public async Task A_name_already_used_in_the_trip_is_refused(string attempt)
    {
        var trip = await StartTripAsync();
        await JoinAsync(App.NewDevice(), trip.InvitePath, "Sam");

        var latecomer = App.NewDevice();
        var response = await JoinAsync(latecomer, trip.InvitePath, attempt);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("already uses that name", html);
        await using var db = App.NewDbContext();
        Assert.Equal(2, await db.Participants.CountAsync());
    }

    [Fact] // SC-007
    public async Task Coming_back_on_the_same_device_takes_me_straight_in()
    {
        var trip = await StartTripAsync();
        var friend = App.NewDevice();
        await JoinAsync(friend, trip.InvitePath, "Sam");

        var response = await friend.GetAsync(trip.InvitePath);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(trip.Path, response.RequestMessage!.RequestUri!.AbsolutePath);
        Assert.DoesNotContain("Join as someone new", html);
    }

    [Fact] // SC-013
    public async Task A_device_keeps_exactly_one_identity_per_trip()
    {
        var trip = await StartTripAsync();
        var friend = App.NewDevice();
        await JoinAsync(friend, trip.InvitePath, "Sam");

        await friend.GetAsync(trip.InvitePath);

        await using var db = App.NewDbContext();
        Assert.Equal(2, await db.ParticipantBindings.CountAsync()); // Kris's and Sam's
        Assert.Equal(2, await db.Visitors.CountAsync());
    }

    [Fact] // SC-008, FR-020, FR-021
    public async Task I_can_resume_as_myself_from_a_second_device()
    {
        var trip = await StartTripAsync();
        var phone = App.NewDevice();
        await JoinAsync(phone, trip.InvitePath, "Sam");

        Guid samId;
        await using (var db = App.NewDbContext())
        {
            samId = (await db.Participants.SingleAsync(p => p.DisplayName == "Sam")).Id;
        }

        var laptop = App.NewDevice();
        var invitePage = await laptop.GetHtmlAsync(trip.InvitePath);
        Assert.Contains("Join as someone new", invitePage);
        Assert.Contains("Already joined on another device", invitePage);

        var response = await laptop.PostFormAsync($"{trip.InvitePath}?handler=Resume", invitePage,
            new Dictionary<string, string> { ["participantId"] = samId.ToString() });

        response.EnsureSuccessStatusCode();
        Assert.Equal(trip.Path, response.RequestMessage!.RequestUri!.AbsolutePath);

        await using var check = App.NewDbContext();
        Assert.Equal(2, await check.Participants.CountAsync()); // no second Sam
        Assert.Equal(2, await check.ParticipantBindings.CountAsync(b => b.ParticipantId == samId));
    }

    [Fact] // NFR-006
    public async Task A_visitor_who_is_not_a_participant_cannot_open_the_trip()
    {
        var trip = await StartTripAsync();
        var stranger = App.NewDevice();

        var response = await stranger.GetAsync(trip.Path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact] // EC-4
    public async Task Being_in_another_trip_does_not_make_me_a_member_of_this_one()
    {
        var first = await StartTripAsync(starter: "Kris", name: "Ardennes weekend");
        var second = await StartTripAsync(starter: "Robin", name: "Zeeland");

        // Kris's browser already has an identity, but only in the first trip.
        var html = await first.Starter.GetHtmlAsync(second.InvitePath);

        Assert.Contains("Join as someone new", html);
        Assert.Equal(HttpStatusCode.NotFound, (await first.Starter.GetAsync(second.Path)).StatusCode);
    }

    [Fact] // EC-12
    public async Task Merely_opening_an_invite_establishes_no_visitor_identity()
    {
        var trip = await StartTripAsync();
        var crawler = App.NewDevice();

        await crawler.GetAsync(trip.InvitePath);

        await using var db = App.NewDbContext();
        Assert.Equal(1, await db.Visitors.CountAsync()); // only the starter's
    }

    [Fact] // NFR-006
    public async Task The_invite_page_asks_search_engines_to_stay_away()
    {
        var trip = await StartTripAsync();

        Assert.Contains("noindex", await App.NewDevice().GetHtmlAsync(trip.InvitePath));
    }

    [Fact] // FR-018
    public async Task I_can_change_my_own_display_name()
    {
        var trip = await StartTripAsync(starter: "Kris");

        var response = await trip.Starter.SubmitFormAsync(trip.Path, $"{trip.Path}?handler=Rename",
            new Dictionary<string, string> { ["NewDisplayName"] = "Kris D" });

        response.EnsureSuccessStatusCode();
        await using var db = App.NewDbContext();
        Assert.Equal("Kris D", (await db.Participants.SingleAsync()).DisplayName);
    }

    [Fact] // FR-018 + INV-3
    public async Task I_cannot_rename_myself_onto_someone_elses_name()
    {
        var trip = await StartTripAsync(starter: "Kris");
        await JoinAsync(App.NewDevice(), trip.InvitePath, "Sam");

        var response = await trip.Starter.SubmitFormAsync(trip.Path, $"{trip.Path}?handler=Rename",
            new Dictionary<string, string> { ["NewDisplayName"] = "sam" });

        Assert.Contains("already uses that name", await response.Content.ReadAsStringAsync());
        await using var db = App.NewDbContext();
        Assert.Equal(["Kris", "Sam"], await db.Participants.Select(p => p.DisplayName).OrderBy(n => n).ToListAsync());
    }
}
