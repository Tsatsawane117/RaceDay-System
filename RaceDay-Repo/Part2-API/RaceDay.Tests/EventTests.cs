using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;
public class EventTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EventTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    private static object ValidEvent(string name = "Unit Test Run") => new
    {
        eventName = name,
        eventDate = DateTime.UtcNow.AddMonths(3),
        location = "Test Park",
        description = "A test event",
        distanceKM = 5,
        eventType = "Run",
        status = "Planned"
    };

    [Fact]
    public async Task GetEvents_WithoutLogin_Returns200_PublicAccess()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var events = await response.Content.ReadFromJsonAsync<List<EventDto>>();
        Assert.True(events!.Count >= 3);
    }

    [Fact]
    public async Task GetEvents_FilterByStatus_ReturnsOnlyMatching()
    {
        var client = _factory.CreateClient();

        var events = await client.GetFromJsonAsync<List<EventDto>>("/api/events?status=Planned");

        Assert.NotEmpty(events!);
        Assert.All(events!, e => Assert.Equal("Planned", e.Status));
    }

    [Fact]
    public async Task GetEvent_Existing_Returns200_WithCategories()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ev = await response.Content.ReadFromJsonAsync<EventDto>();
        Assert.NotEmpty(ev!.Categories!);
    }

    [Fact]
    public async Task GetEvent_Unknown_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithoutLogin_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/events", ValidEvent());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsParticipant_Returns403()
    {
        var client = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant1);

        var response = await client.PostAsJsonAsync("/api/events", ValidEvent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsOrganiser_Returns201_AndOrganiserIsOwner()
    {
        var client = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);

        var response = await client.PostAsJsonAsync("/api/events", ValidEvent());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var ev = await response.Content.ReadFromJsonAsync<EventDto>();
        Assert.Equal("Run", ev!.EventType);
        Assert.Equal("Planned", ev.Status);
    }

    [Fact]
    public async Task CreateEvent_InvalidEventType_Returns400()
    {
        var client = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);

        var response = await client.PostAsJsonAsync("/api/events", new
        {
            eventName = "Bad Type",
            eventDate = DateTime.UtcNow.AddMonths(1),
            location = "Park",
            distanceKM = 5,
            eventType = "Swim"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_MissingName_Returns400()
    {
        var client = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);

        var response = await client.PostAsJsonAsync("/api/events", new
        {
            eventDate = DateTime.UtcNow.AddMonths(1),
            location = "Park",
            distanceKM = 5,
            eventType = "Run"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_AsOwner_Returns200()
    {
        var client = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);
        var created = await (await client.PostAsJsonAsync("/api/events", ValidEvent("Before"))).Content.ReadFromJsonAsync<EventDto>();

        var response = await client.PutAsJsonAsync($"/api/events/{created!.EventID}", ValidEvent("After"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EventDto>();
        Assert.Equal("After", updated!.EventName);
    }

    [Fact]
    public async Task UpdateEvent_AsDifferentOrganiser_Returns403()
    {
        var other = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser2);

        // Event 1 belongs to Organiser1 (Thabo).
        var response = await other.PutAsJsonAsync("/api/events/1", ValidEvent("Hijacked"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_AsParticipant_Returns403()
    {
        var client = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant1);

        var response = await client.PutAsJsonAsync("/api/events/1", ValidEvent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_Unknown_Returns404()
    {
        var client = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);

        var response = await client.PutAsJsonAsync("/api/events/99999", ValidEvent());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_AsOwner_WithNoEnrolments_Returns200_ThenEventIsGone()
    {
        var client = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);
        var created = await (await client.PostAsJsonAsync("/api/events", ValidEvent("To delete"))).Content.ReadFromJsonAsync<EventDto>();

        var delete = await client.DeleteAsync($"/api/events/{created!.EventID}");
        var get = await client.GetAsync($"/api/events/{created.EventID}");

        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_WithEnrolments_Returns409()
    {
        var client = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);

        // Seeded event 1 already has enrolments.
        var response = await client.DeleteAsync("/api/events/1");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_AsParticipant_Returns403()
    {
        var client = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant1);

        var response = await client.DeleteAsync("/api/events/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_WithoutLogin_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync("/api/events/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddCategory_AsOwner_Returns201_AndAppearsInPublicList()
    {
        var client = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);
        var created = await (await client.PostAsJsonAsync("/api/events", ValidEvent())).Content.ReadFromJsonAsync<EventDto>();

        var add = await client.PostAsJsonAsync($"/api/events/{created!.EventID}/categories",
            new { categoryName = "Under 20", distanceKM = 5, maxParticipants = 50, entryFee = 80 });
        var list = await _factory.CreateClient().GetFromJsonAsync<List<CategoryDto>>($"/api/events/{created.EventID}/categories");

        Assert.Equal(HttpStatusCode.Created, add.StatusCode);
        Assert.Contains(list!, c => c.CategoryName == "Under 20");
    }

    [Fact]
    public async Task AddCategory_AsParticipant_Returns403()
    {
        var client = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant1);

        var response = await client.PostAsJsonAsync("/api/events/1/categories",
            new { categoryName = "Sneaky", distanceKM = 5, maxParticipants = 50, entryFee = 0 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddCategory_ToAnotherOrganisersEvent_Returns403()
    {
        var other = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser2);

        var response = await other.PostAsJsonAsync("/api/events/1/categories",
            new { categoryName = "Sneaky", distanceKM = 5, maxParticipants = 50, entryFee = 0 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddCategory_InvalidData_Returns400()
    {
        var client = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);

        var response = await client.PostAsJsonAsync("/api/events/1/categories",
            new { categoryName = "", distanceKM = 5, maxParticipants = 50, entryFee = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_AsOwner_Returns200()
    {
        var client = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);
        var (_, categoryId) = await TestHelpers.CreateOpenEventWithCategoryAsync(client);

        var response = await client.PutAsJsonAsync($"/api/categories/{categoryId}",
            new { categoryName = "Renamed", distanceKM = 12, maxParticipants = 80, entryFee = 200 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var category = await response.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.Equal("Renamed", category!.CategoryName);
    }

    [Fact]
    public async Task UpdateCategory_AsParticipant_Returns403()
    {
        var client = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant1);

        var response = await client.PutAsJsonAsync("/api/categories/1",
            new { categoryName = "Renamed", distanceKM = 12, maxParticipants = 80, entryFee = 200 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_WithoutEnrolments_Returns200()
    {
        var client = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);
        var (_, categoryId) = await TestHelpers.CreateOpenEventWithCategoryAsync(client);

        var response = await client.DeleteAsync($"/api/categories/{categoryId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_WithEnrolments_Returns409()
    {
        var client = await TestHelpers.LoginAsync(_factory, TestHelpers.Organiser1);

        // Seeded category 2 (21km Half Marathon) has an enrolment.
        var response = await client.DeleteAsync("/api/categories/2");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
