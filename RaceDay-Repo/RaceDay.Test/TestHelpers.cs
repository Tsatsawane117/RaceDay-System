using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

public static class TestHelpers
{
    // Seeded accounts (see DbInitializer). All use the same password.
    public const string SeedPassword = "Password123!";
    public const string Organiser1 = "thabo.mokoena@raceday.co.za";   
    public const string Organiser2 = "lindiwe.nkosi@raceday.co.za"; 
    public const string Participant1 = "sipho.dlamini@gmail.com";
    public const string Participant2 = "amahle.zulu@gmail.com";

    public static async Task<HttpClient> LoginAsync(RaceDayFactory factory, string email, string password = SeedPassword)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return client;
    }

    public static async Task<(int EventId, int CategoryId)> CreateOpenEventWithCategoryAsync(
        HttpClient organiser, int maxParticipants = 100, decimal entryFee = 150, string status = "Open")
    {
        var eventResponse = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventName = $"Test Event {Guid.NewGuid():N}",
            eventDate = DateTime.UtcNow.AddMonths(2),
            location = "Test Park",
            description = "Created by a unit test",
            distanceKM = 10,
            eventType = "Run",
            status
        });
        Assert.Equal(HttpStatusCode.Created, eventResponse.StatusCode);
        var ev = await eventResponse.Content.ReadFromJsonAsync<EventDto>();

        var categoryResponse = await organiser.PostAsJsonAsync($"/api/events/{ev!.EventID}/categories", new
        {
            categoryName = "10km",
            distanceKM = 10,
            maxParticipants,
            entryFee
        });
        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();

        return (ev.EventID, category!.CategoryID);
    }

    public static async Task<EnrolmentDto> EnrolAsync(HttpClient participant, int categoryId)
    {
        var response = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrolment = await response.Content.ReadFromJsonAsync<EnrolmentDto>();
        return enrolment!;
    }
}
