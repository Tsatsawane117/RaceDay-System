using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

public class EnrolmentTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EnrolmentTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Enrol_AsParticipant_Returns201_WithBibNumber_AndLinksParticipantAndCategory()
    {
        var organiser = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);
        var (eventId, categoryId) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);
        var participant = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant1);

        var enrolment = await TestHelpers.EnrolAsync(participant, categoryId);

        Assert.Equal(categoryId, enrolment.CategoryID);
        Assert.Equal(eventId, enrolment.EventID);
        Assert.False(string.IsNullOrEmpty(enrolment.BibNumber));
        Assert.Equal("Pending", enrolment.Status);
    }

    [Fact]
    public async Task Enrol_IsRecorded_AndAppearsInParticipantsOwnList()
    {
        var organiser = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);
        var (_, categoryId) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);
        var participant = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant1);
        var enrolment = await TestHelpers.EnrolAsync(participant, categoryId);

        var mine = await participant.GetFromJsonAsync<List<EnrolmentDto>>("/api/users/me/enrolments");

        Assert.Contains(mine!, e => e.EnrolmentID == enrolment.EnrolmentID && e.CategoryID == categoryId);
    }

    [Fact]
    public async Task Enrol_WithoutLogin_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/categories/1/enrol", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_AsOrganiser_Returns403()
    {
        var organiser = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);
        var (_, categoryId) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);

        var response = await organiser.PostAsync($"/api/categories/{categoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_UnknownCategory_Returns404()
    {
        var participant = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant1);

        var response = await participant.PostAsync("/api/categories/99999/enrol", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_Twice_InSameCategory_Returns409()
    {
        var organiser = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);
        var (_, categoryId) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);
        var participant = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant1);
        await TestHelpers.EnrolAsync(participant, categoryId);

        var second = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_WhenEventNotOpen_Returns400()
    {
        var organiser = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);
        var (_, categoryId) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser, status: "Closed");
        var participant = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant1);

        var response = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WhenCategoryFull_Returns400()
    {
        var organiser = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);
        var (_, categoryId) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser, maxParticipants: 1);
        var first = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant1);
        var second = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant2);
        await TestHelpers.EnrolAsync(first, categoryId);

        var response = await second.PostAsync($"/api/categories/{categoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MyEnrolments_AsOrganiser_Returns403()
    {
        var organiser = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);

        var response = await organiser.GetAsync("/api/users/me/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task EventEnrolments_AsOwningOrganiser_Returns200_AndShowsParticipant()
    {
        var organiser = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);
        var (eventId, categoryId) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);
        var participant = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant1);
        await TestHelpers.EnrolAsync(participant, categoryId);

        var response = await organiser.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<EnrolmentDto>>();
        Assert.Single(list!);
        Assert.Equal("Sipho Dlamini", list![0].ParticipantName);
    }

    [Fact]
    public async Task EventEnrolments_AsParticipant_Returns403()
    {
        var participant = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant1);

        var response = await participant.GetAsync("/api/events/1/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task EventEnrolments_AsDifferentOrganiser_Returns403()
    {
        var other = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser2);

        var response = await other.GetAsync("/api/events/1/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task EventEnrolments_WithoutLogin_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events/1/enrolments");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CancelEnrolment_AsOwner_Returns200_AndStatusBecomesCancelled()
    {
        var organiser = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);
        var (_, categoryId) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);
        var participant = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant1);
        var enrolment = await TestHelpers.EnrolAsync(participant, categoryId);

        var cancel = await participant.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentID}");
        var mine = await participant.GetFromJsonAsync<List<EnrolmentDto>>("/api/users/me/enrolments");

        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal("Cancelled", mine!.Single(e => e.EnrolmentID == enrolment.EnrolmentID).Status);
    }

    [Fact]
    public async Task CancelEnrolment_ByAnotherParticipant_Returns403()
    {
        var organiser = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);
        var (_, categoryId) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);
        var owner = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant1);
        var stranger = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant2);
        var enrolment = await TestHelpers.EnrolAsync(owner, categoryId);

        var response = await stranger.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentID}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReEnrol_AfterCancelling_Returns201_AndKeepsSameBibNumber()
    {
        var organiser = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);
        var (_, categoryId) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);
        var participant = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant1);
        var first = await TestHelpers.EnrolAsync(participant, categoryId);
        await participant.DeleteAsync($"/api/enrolments/{first.EnrolmentID}");

        var again = await TestHelpers.EnrolAsync(participant, categoryId);

        Assert.Equal(first.EnrolmentID, again.EnrolmentID);
        Assert.Equal(first.BibNumber, again.BibNumber);
        Assert.Equal("Pending", again.Status);
    }

    [Fact]
    public async Task Payment_CorrectAmount_Returns201_AndConfirmsEnrolment()
    {
        var organiser = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);
        var (_, categoryId) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser, entryFee: 150);
        var participant = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant1);
        var enrolment = await TestHelpers.EnrolAsync(participant, categoryId);

        var pay = await participant.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentID}/payments",
            new { amount = 150, paymentMethod = "Card" });
        var mine = await participant.GetFromJsonAsync<List<EnrolmentDto>>("/api/users/me/enrolments");

        Assert.Equal(HttpStatusCode.Created, pay.StatusCode);
        Assert.Equal("Confirmed", mine!.Single(e => e.EnrolmentID == enrolment.EnrolmentID).Status);
    }

    [Fact]
    public async Task Payment_WrongAmount_Returns400()
    {
        var organiser = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);
        var (_, categoryId) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser, entryFee: 150);
        var participant = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant1);
        var enrolment = await TestHelpers.EnrolAsync(participant, categoryId);

        var response = await participant.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentID}/payments",
            new { amount = 10, paymentMethod = "Card" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Payment_PayingTwice_Returns409()
    {
        var organiser = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);
        var (_, categoryId) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser, entryFee: 150);
        var participant = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant1);
        var enrolment = await TestHelpers.EnrolAsync(participant, categoryId);
        await participant.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentID}/payments", new { amount = 150, paymentMethod = "Card" });

        var second = await participant.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentID}/payments",
            new { amount = 150, paymentMethod = "Card" });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Payment_ForSomeoneElsesEnrolment_Returns403()
    {
        var organiser = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);
        var (_, categoryId) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser, entryFee: 150);
        var owner = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant1);
        var stranger = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant2);
        var enrolment = await TestHelpers.EnrolAsync(owner, categoryId);

        var response = await stranger.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentID}/payments",
            new { amount = 150, paymentMethod = "Card" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Payments_History_VisibleToOwnerAndOrganiser_NotToStranger()
    {
        var organiser = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);
        var (_, categoryId) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser, entryFee: 150);
        var owner = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant1);
        var stranger = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant2);
        var enrolment = await TestHelpers.EnrolAsync(owner, categoryId);
        await owner.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentID}/payments", new { amount = 150, paymentMethod = "EFT" });
        var url = $"/api/enrolments/{enrolment.EnrolmentID}/payments";

        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await organiser.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync(url)).StatusCode);
    }
}
