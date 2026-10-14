using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.Api.Tests;

// Covers: "Event management - verifying that Organisers can manage events"
public class EventManagementTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory factory;

    public EventManagementTests(RaceDayApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Organiser_CanCreateUpdateAndDeleteTheirOwnEvent()
    {
        var client = factory.CreateClient();
        var organiser = await TestAuthHelper.RegisterAndLoginAsync(client, "Organiser");

        var createResponse = await client.PostAsJsonAsync("/api/events", new
        {
            eventName = "Durban Beachfront Park Run Challenge",
            description = "A family-friendly timed run.",
            eventDate = "2026-09-20",
            location = "Durban, KwaZulu-Natal",
            distance = 10.0,
            eventType = "Run"
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var eventId = created.GetProperty("eventId").GetInt32();

        var updateResponse = await client.PutAsJsonAsync($"/api/events/{eventId}", new { location = "Durban Beachfront" });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync($"/api/events/{eventId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/events/{eventId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent()
    {
        var ownerClient = factory.CreateClient();
        var owner = await TestAuthHelper.RegisterAndLoginAsync(ownerClient, "Organiser");

        var createResponse = await ownerClient.PostAsJsonAsync("/api/events", new
        {
            eventName = "Soweto Marathon",
            eventDate = "2026-11-15",
            location = "Soweto, Johannesburg",
            distance = 42.2,
            eventType = "Run"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var eventId = created.GetProperty("eventId").GetInt32();

        var otherClient = factory.CreateClient();
        var otherOrganiser = await TestAuthHelper.RegisterAndLoginAsync(otherClient, "Organiser");

        var updateResponse = await otherClient.PutAsJsonAsync($"/api/events/{eventId}", new { location = "Hacked" });

        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }

    [Fact]
    public async Task PublicUser_CanBrowseEventsWithoutLoggingIn()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }


    [Fact]
    public async Task CreateEvent_WithInvalidEventType_ReturnsBadRequest()
    {
        var client = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(client, "Organiser");

        var response = await client.PostAsJsonAsync("/api/events", new
        {
            eventName = "Swim Gala",
            eventDate = "2026-12-01",
            location = "Pretoria",
            distance = 1.5,
            eventType = "Swim"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithMissingFields_ReturnsBadRequest()
    {
        var client = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(client, "Organiser");

        var response = await client.PostAsJsonAsync("/api/events", new { description = "No name, date or location" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AcceptsWalkAndCycleTypes_CaseInsensitively()
    {
        var client = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(client, "Organiser");

        foreach (var type in new[] { "walk", "CYCLE", "Run" })
        {
            var response = await client.PostAsJsonAsync("/api/events", new
            {
                eventName = $"Event {type}",
                eventDate = "2026-12-01",
                location = "Pretoria",
                distance = 5.0,
                eventType = type
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }

    [Fact]
    public async Task CreateEvent_WithoutLogin_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/events", new
        {
            eventName = "Anonymous Event",
            eventDate = "2026-12-01",
            location = "Nowhere",
            distance = 5.0,
            eventType = "Run"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotDeleteAnotherOrganisersEvent()
    {
        var owner = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(owner, "Organiser");
        var eventId = await TestAuthHelper.CreateEventAsync(owner);

        var other = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(other, "Organiser");

        var response = await other.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetEvent_Unknown_ReturnsNotFound()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/events/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanViewEventDetails()
    {
        var organiser = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser");
        var eventId = await TestAuthHelper.CreateEventAsync(organiser, "Viewable Event", "Cycle");

        var participant = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(participant, "Participant");

        var response = await participant.GetAsync($"/api/events/{eventId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("Viewable Event", json.GetProperty("eventName").GetString());
        Assert.Equal("Cycle", json.GetProperty("eventType").GetString());
    }
}
