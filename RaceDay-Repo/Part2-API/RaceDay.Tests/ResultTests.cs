using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;
public class ResultTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public ResultTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Organiser, HttpClient Participant, int EventId, int EnrolmentId)> SetUpEnrolmentAsync()
    {
        var organiser = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);
        var (eventId, categoryId) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);
        var participant = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant1);
        var enrolment = await TestHelpers.EnrolAsync(participant, categoryId);
        return (organiser, participant, eventId, enrolment.EnrolmentID);
    }

    [Fact]
    public async Task CaptureResult_AsOwningOrganiser_Returns201()
    {
        var (organiser, _, _, enrolmentId) = await SetUpEnrolmentAsync();

        var response = await organiser.PostAsJsonAsync("/api/results",
            new { enrolmentID = enrolmentId, finishTime = "00:45:10", position = 1, status = "Finished" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ResultDto>();
        Assert.Equal(1, result!.Position);
        Assert.Equal(new TimeSpan(0, 45, 10), result.FinishTime);
    }

    [Fact]
    public async Task CaptureResult_Twice_UpdatesExistingResult_Returns200()
    {
        var (organiser, _, _, enrolmentId) = await SetUpEnrolmentAsync();
        await organiser.PostAsJsonAsync("/api/results", new { enrolmentID = enrolmentId, finishTime = "00:45:10", position = 3, status = "Finished" });

        var second = await organiser.PostAsJsonAsync("/api/results",
            new { enrolmentID = enrolmentId, finishTime = "00:44:00", position = 2, status = "Finished" });

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var result = await second.Content.ReadFromJsonAsync<ResultDto>();
        Assert.Equal(2, result!.Position);
    }

    [Fact]
    public async Task CaptureResult_AsParticipant_Returns403()
    {
        var (_, participant, _, enrolmentId) = await SetUpEnrolmentAsync();

        var response = await participant.PostAsJsonAsync("/api/results",
            new { enrolmentID = enrolmentId, finishTime = "00:30:00", position = 1, status = "Finished" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_WithoutLogin_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/results",
            new { enrolmentID = 1, finishTime = "00:30:00", position = 1, status = "Finished" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_ForAnotherOrganisersEvent_Returns403()
    {
        var (_, _, _, enrolmentId) = await SetUpEnrolmentAsync();
        var otherOrganiser = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser2);

        var response = await otherOrganiser.PostAsJsonAsync("/api/results",
            new { enrolmentID = enrolmentId, finishTime = "00:30:00", position = 1, status = "Finished" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_UnknownEnrolment_Returns404()
    {
        var organiser = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);

        var response = await organiser.PostAsJsonAsync("/api/results",
            new { enrolmentID = 99999, finishTime = "00:30:00", position = 1, status = "Finished" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_FinishedWithoutTime_Returns400()
    {
        var (organiser, _, _, enrolmentId) = await SetUpEnrolmentAsync();

        var response = await organiser.PostAsJsonAsync("/api/results",
            new { enrolmentID = enrolmentId, position = 1, status = "Finished" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_InvalidStatus_Returns400()
    {
        var (organiser, _, _, enrolmentId) = await SetUpEnrolmentAsync();

        var response = await organiser.PostAsJsonAsync("/api/results",
            new { enrolmentID = enrolmentId, finishTime = "00:30:00", position = 1, status = "Winner" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ViewOwnResult_AsParticipant_Returns200()
    {
        var (organiser, participant, _, enrolmentId) = await SetUpEnrolmentAsync();
        await organiser.PostAsJsonAsync("/api/results", new { enrolmentID = enrolmentId, finishTime = "00:50:00", position = 4, status = "Finished" });

        var response = await participant.GetAsync($"/api/enrolments/{enrolmentId}/results");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ResultDto>();
        Assert.Equal(4, result!.Position);
    }

    [Fact]
    public async Task ViewResult_OfAnotherParticipant_Returns403()
    {
        var (organiser, _, _, enrolmentId) = await SetUpEnrolmentAsync();
        await organiser.PostAsJsonAsync("/api/results", new { enrolmentID = enrolmentId, finishTime = "00:50:00", position = 4, status = "Finished" });
        var stranger = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant2);

        var response = await stranger.GetAsync($"/api/enrolments/{enrolmentId}/results");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ViewResult_WithoutLogin_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/enrolments/1/results");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ViewResult_BeforeOneIsCaptured_Returns404()
    {
        var (_, participant, _, enrolmentId) = await SetUpEnrolmentAsync();

        var response = await participant.GetAsync($"/api/enrolments/{enrolmentId}/results");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task EventResults_ArePublic_AndOrderedByPosition()
    {
        var (organiser, _, eventId, enrolmentId) = await SetUpEnrolmentAsync();
        await organiser.PostAsJsonAsync("/api/results", new { enrolmentID = enrolmentId, finishTime = "00:50:00", position = 1, status = "Finished" });
        var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync($"/api/events/{eventId}/results");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<List<ResultDto>>();
        Assert.Single(results!);
    }

    [Fact]
    public async Task EventResults_UnknownEvent_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events/99999/results");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
