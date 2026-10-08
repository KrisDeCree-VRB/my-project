using System.Net;
using Microsoft.EntityFrameworkCore;
using my_project.Domain;

namespace my_project.Tests.Integration;

public class InviteLinkControlTests : AppTest
{
    [Fact] // SC-004, FR-008
    public async Task Every_participant_can_see_and_copy_the_invite_link()
    {
        var trip = await StartTripAsync();
        var friend = App.NewDevice();
        await JoinAsync(friend, trip.InvitePath, "Sam");

        var html = await friend.GetHtmlAsync(trip.Path);

        Assert.Equal(trip.InvitePath, BrowserExtensions.InvitePath(html));
        Assert.Contains("id=\"copyInvite\"", html);
    }

    [Fact] // SC-009
    public async Task Revoking_kills_the_link_but_keeps_the_people()
    {
        var trip = await StartTripAsync();
        var sam = App.NewDevice();
        await JoinAsync(sam, trip.InvitePath, "Sam");

        await RevokeAsync(trip);

        // The old link admits nobody.
        var newcomer = App.NewDevice();
        var response = await newcomer.GetAsync(trip.InvitePath);
        Assert.Equal("/InviteNotValid", response.RequestMessage!.RequestUri!.AbsolutePath);

        // Sam still has full access.
        var samsView = await sam.GetAsync(trip.Path);
        samsView.EnsureSuccessStatusCode();
        Assert.Contains("Ardennes weekend", await samsView.Content.ReadAsStringAsync());
    }

    [Fact] // SC-009
    public async Task A_replacement_link_admits_people_and_the_old_one_stays_dead()
    {
        var trip = await StartTripAsync();
        var oldPath = trip.InvitePath;

        await RevokeAsync(trip);
        var newPath = await RegenerateAsync(trip);

        Assert.NotEqual(oldPath, newPath);

        var newcomer = App.NewDevice();
        await JoinAsync(newcomer, newPath, "Sam");
        await using var db = App.NewDbContext();
        Assert.Equal(2, await db.Participants.CountAsync());

        var stillDead = await App.NewDevice().GetAsync(oldPath);
        Assert.Equal("/InviteNotValid", stillDead.RequestMessage!.RequestUri!.AbsolutePath);
    }

    [Fact] // EC-2, INV-6
    public async Task Regenerating_twice_leaves_exactly_one_active_link()
    {
        var trip = await StartTripAsync();

        await RegenerateAsync(trip);
        await RegenerateAsync(trip);

        await using var db = App.NewDbContext();
        Assert.Equal(3, await db.InviteLinks.CountAsync());
        Assert.Equal(1, await db.InviteLinks.CountAsync(l => l.Status == InviteLinkStatus.Active));
    }

    [Fact] // SC-010 and SC-011 — the two answers must be indistinguishable
    public async Task A_revoked_link_and_an_unknown_link_answer_alike()
    {
        var trip = await StartTripAsync();
        await RevokeAsync(trip);

        var revoked = await App.NewDevice().GetAsync(trip.InvitePath);
        var unknown = await App.NewDevice().GetAsync("/join/abc123");

        Assert.Equal(unknown.StatusCode, revoked.StatusCode);
        Assert.Equal(
            await unknown.Content.ReadAsStringAsync(),
            await revoked.Content.ReadAsStringAsync());
    }

    [Fact] // SC-010
    public async Task A_dead_link_reveals_nothing_about_the_trip()
    {
        var trip = await StartTripAsync();
        await RevokeAsync(trip);

        var html = await App.NewDevice().GetHtmlAsync(trip.InvitePath);

        Assert.DoesNotContain("Ardennes weekend", html);
        Assert.DoesNotContain("Durbuy", html);
        Assert.DoesNotContain("Kris", html);
    }

    [Fact] // SC-012
    public async Task A_participant_who_is_not_the_starter_sees_no_link_controls()
    {
        var trip = await StartTripAsync();
        var sam = App.NewDevice();
        await JoinAsync(sam, trip.InvitePath, "Sam");

        var html = await sam.GetHtmlAsync(trip.Path);

        Assert.Contains(trip.InvitePath, html); // can still see and copy it
        Assert.DoesNotContain("handler=Revoke", html);
        Assert.DoesNotContain("handler=Regenerate", html);
    }

    [Fact] // FR-009: the absent button is not the only guard
    public async Task A_participant_who_is_not_the_starter_cannot_revoke_the_link()
    {
        var trip = await StartTripAsync();
        var sam = App.NewDevice();
        await JoinAsync(sam, trip.InvitePath, "Sam");

        var response = await sam.SubmitFormAsync(trip.Path, $"{trip.Path}?handler=Revoke", []);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var db = App.NewDbContext();
        Assert.Equal(1, await db.InviteLinks.CountAsync(l => l.Status == InviteLinkStatus.Active));
    }

    [Fact] // EC-11: no active link is a valid end state, not an error
    public async Task A_trip_with_no_active_link_still_works_for_everyone_inside()
    {
        var trip = await StartTripAsync();
        await RevokeAsync(trip);

        var html = await trip.Starter.GetHtmlAsync(trip.Path);

        Assert.Contains("no active invite link", html);
        Assert.Contains("Ardennes weekend", html);
    }

    private async Task RevokeAsync(Trip trip) =>
        (await trip.Starter.SubmitFormAsync(trip.Path, $"{trip.Path}?handler=Revoke", []))
            .EnsureSuccessStatusCode();

    private async Task<string> RegenerateAsync(Trip trip)
    {
        var response = await trip.Starter.SubmitFormAsync(trip.Path, $"{trip.Path}?handler=Regenerate", []);
        response.EnsureSuccessStatusCode();
        return BrowserExtensions.InvitePath(await response.Content.ReadAsStringAsync())!;
    }
}
