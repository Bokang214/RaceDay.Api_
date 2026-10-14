using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.Api.Tests;

// Covers: "Different roles are rejected when attempting to access role-based functionalities"
public class RoleAccessTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory factory;

    public RoleAccessTests(RaceDayApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Participant_CannotCreateEvent()
    {
        var client = factory.CreateClient();
        var participant = await TestAuthHelper.RegisterAndLoginAsync(client, "Participant");

        var response = await client.PostAsJsonAsync("/api/events", new
        {
            eventName = "Rogue Event",
            eventDate = "2026-12-01",
            location = "Nowhere",
            distance = 5.0,
            eventType = "Run"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrolInACategory()
    {
        var organiserClient = factory.CreateClient();
        var organiser = await TestAuthHelper.RegisterAndLoginAsync(organiserClient, "Organiser");

        var eventResponse = await organiserClient.PostAsJsonAsync("/api/events", new
        {
            eventName = "Cape Town Cycle Tour",
            eventDate = "2026-11-08",
            location = "Cape Town",
            distance = 45.0,
            eventType = "Cycle"
        });
        var ev = await eventResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var eventId = ev.GetProperty("eventId").GetInt32();

        var categoryResponse = await organiserClient.PostAsJsonAsync(
            $"/api/events/{eventId}/categories", new { categoryName = "Half Century Ride" });
        var category = await categoryResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var categoryId = category.GetProperty("categoryId").GetInt32();

        // The same Organiser (not a Participant) tries to enrol - must be rejected.
        var enrolResponse = await organiserClient.PostAsync($"/api/categories/{categoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Forbidden, enrolResponse.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewOrganiserOnlyEnrolmentList()
    {
        var organiserClient = factory.CreateClient();
        var organiser = await TestAuthHelper.RegisterAndLoginAsync(organiserClient, "Organiser");

        var eventResponse = await organiserClient.PostAsJsonAsync("/api/events", new
        {
            eventName = "Durban Beachfront Park Run",
            eventDate = "2026-09-20",
            location = "Durban",
            distance = 10.0,
            eventType = "Run"
        });
        var ev = await eventResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var eventId = ev.GetProperty("eventId").GetInt32();

        var participantClient = factory.CreateClient();
        var participant = await TestAuthHelper.RegisterAndLoginAsync(participantClient, "Participant");

        var response = await participantClient.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }


    [Fact]
    public async Task Participant_CannotUpdateOrDeleteEvents()
    {
        var organiser = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser");
        var eventId = await TestAuthHelper.CreateEventAsync(organiser);

        var participant = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(participant, "Participant");

        var update = await participant.PutAsJsonAsync($"/api/events/{eventId}", new { location = "Hacked" });
        var delete = await participant.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotViewParticipantOnlyEnrolments()
    {
        var organiser = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser");

        var response = await organiser.GetAsync("/api/enrolments/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotWithdrawAnEnrolment()
    {
        var organiser = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser");
        var eventId = await TestAuthHelper.CreateEventAsync(organiser);
        var categoryId = await TestAuthHelper.CreateCategoryAsync(organiser, eventId);

        var participant = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(participant, "Participant");
        var enrolmentId = await TestAuthHelper.EnrolAsync(participant, categoryId);

        var response = await organiser.DeleteAsync($"/api/enrolments/{enrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/users/me")]
    [InlineData("GET", "/api/enrolments/me")]
    [InlineData("GET", "/api/results/me")]
    [InlineData("GET", "/api/events/1/enrolments")]
    [InlineData("POST", "/api/categories/1/enrolments")]
    [InlineData("POST", "/api/enrolments/1/results")]
    [InlineData("PUT", "/api/results/1")]
    [InlineData("DELETE", "/api/events/1")]
    public async Task ProtectedEndpoints_RejectAnonymousRequests(string method, string url)
    {
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST" or "PUT")
            request.Content = JsonContent.Create(new { });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
