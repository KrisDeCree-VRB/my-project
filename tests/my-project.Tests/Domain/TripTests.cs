using my_project.Domain;

namespace my_project.Tests.Domain;

public class TripTests
{
    private static readonly DateOnly Today = new(2026, 10, 8);

    private static Trip CreateTrip(
        DateOnly? start = null,
        DateOnly? end = null,
        string starter = "Kris") =>
        Trip.Create(
            "Ardennes weekend",
            "Durbuy",
            start ?? Today.AddDays(36),
            end ?? Today.AddDays(38),
            starter,
            Today,
            "token-1");

    [Fact]
    public void Create_sets_the_trip_details()
    {
        var trip = CreateTrip();

        Assert.Equal("Ardennes weekend", trip.Name);
        Assert.Equal("Durbuy", trip.Location);
        Assert.Equal(Today.AddDays(36), trip.StartDate);
        Assert.Equal(Today.AddDays(38), trip.EndDate);
    }

    [Fact]
    public void Create_makes_the_starter_the_only_participant() // INV-4, INV-5, FR-005
    {
        var trip = CreateTrip();

        var starter = Assert.Single(trip.Participants);
        Assert.Equal("Kris", starter.DisplayName);
        Assert.Equal(starter.Id, trip.StarterParticipantId);
        Assert.Equal(trip.Id, starter.TripId);
        Assert.True(trip.IsStarter(starter.Id));
    }

    [Fact]
    public void Create_issues_an_active_invite_link() // FR-006, INV-6
    {
        var trip = CreateTrip();

        Assert.NotNull(trip.ActiveInviteLink);
        Assert.Equal("token-1", trip.ActiveInviteLink!.Token);
    }

    [Fact]
    public void Create_rejects_an_end_date_before_the_start_date() // INV-1
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            CreateTrip(start: Today.AddDays(38), end: Today.AddDays(36)));

        Assert.Contains("end date", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_accepts_a_single_day_trip() // INV-1 boundary: end == start is fine
    {
        var day = Today.AddDays(5);
        var trip = CreateTrip(start: day, end: day);

        Assert.Equal(day, trip.EndDate);
    }

    [Fact]
    public void Create_rejects_a_start_date_in_the_past() // INV-2
    {
        Assert.Throws<ArgumentException>(() =>
            CreateTrip(start: Today.AddDays(-1), end: Today.AddDays(1)));
    }

    [Fact]
    public void Create_accepts_a_trip_starting_today() // INV-2 boundary
    {
        var trip = CreateTrip(start: Today, end: Today.AddDays(2));

        Assert.Equal(Today, trip.StartDate);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_a_blank_name(string name) =>
        Assert.Throws<ArgumentException>(() =>
            Trip.Create(name, "Durbuy", Today, Today, "Kris", Today, "t"));

    [Fact]
    public void Create_rejects_a_name_over_100_characters() =>
        Assert.Throws<ArgumentException>(() =>
            Trip.Create(new string('x', 101), "Durbuy", Today, Today, "Kris", Today, "t"));

    [Fact]
    public void Create_rejects_a_location_over_200_characters() =>
        Assert.Throws<ArgumentException>(() =>
            Trip.Create("Trip", new string('x', 201), Today, Today, "Kris", Today, "t"));

    [Fact]
    public void AddParticipant_adds_a_second_person()
    {
        var trip = CreateTrip();

        var sam = trip.AddParticipant("Sam");

        Assert.NotNull(sam);
        Assert.Equal("Sam", sam!.DisplayName);
        Assert.Equal(2, trip.Participants.Count);
    }

    [Theory]
    [InlineData("Sam")]
    [InlineData("sam")]
    [InlineData("  SAM  ")]
    public void AddParticipant_refuses_a_name_already_used_in_the_trip(string attempt) // INV-3
    {
        var trip = CreateTrip();
        trip.AddParticipant("Sam");

        Assert.Null(trip.AddParticipant(attempt));
        Assert.Equal(2, trip.Participants.Count);
    }

    [Fact]
    public void AddParticipant_allows_a_name_used_in_another_trip() // INV-3 is per trip
    {
        var one = CreateTrip(starter: "Sam");
        var other = Trip.Create("Other", "Gent", Today, Today, "Kris", Today, "token-2");

        Assert.NotNull(other.AddParticipant("Sam"));
        Assert.Single(one.Participants);
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public void AddParticipant_refuses_a_blank_name(string name) => // EC-9
        Assert.Null(CreateTrip().AddParticipant(name));

    [Fact]
    public void AddParticipant_trims_surrounding_whitespace() // EC-9
    {
        var participant = CreateTrip().AddParticipant("  Sam  ");

        Assert.Equal("Sam", participant!.DisplayName);
    }

    [Fact]
    public void RevokeInviteLink_leaves_the_trip_without_an_active_link() // FR-009, INV-7
    {
        var trip = CreateTrip();
        var link = trip.ActiveInviteLink!;

        trip.RevokeInviteLink();

        Assert.Null(trip.ActiveInviteLink);
        Assert.Equal(InviteLinkStatus.Revoked, link.Status);
        Assert.NotNull(link.RevokedAt);
    }

    [Fact]
    public void A_revoked_link_never_becomes_active_again() // INV-7
    {
        var trip = CreateTrip();
        var first = trip.ActiveInviteLink!;

        trip.RevokeInviteLink();
        trip.RegenerateInviteLink("token-2");

        Assert.False(first.IsActive);
        Assert.Equal("token-2", trip.ActiveInviteLink!.Token);
    }

    [Fact]
    public void Revoking_keeps_the_participants() // INV-8
    {
        var trip = CreateTrip();
        trip.AddParticipant("Sam");

        trip.RevokeInviteLink();

        Assert.Equal(2, trip.Participants.Count);
    }

    [Fact]
    public void Regenerating_twice_leaves_exactly_one_active_link() // INV-6, EC-2
    {
        var trip = CreateTrip();

        trip.RegenerateInviteLink("token-2");
        trip.RegenerateInviteLink("token-3");

        Assert.Equal(3, trip.InviteLinks.Count);
        Assert.Single(trip.InviteLinks, l => l.IsActive);
        Assert.Equal("token-3", trip.ActiveInviteLink!.Token);
    }

    [Fact]
    public void IsNameTaken_ignores_the_participant_being_renamed() // FR-018
    {
        var trip = CreateTrip(starter: "Kris");

        Assert.False(trip.IsNameTaken("kris", exceptParticipantId: trip.StarterParticipantId));
        Assert.True(trip.IsNameTaken("kris", exceptParticipantId: null));
    }
}
