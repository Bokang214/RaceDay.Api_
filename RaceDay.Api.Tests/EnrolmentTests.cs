using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.Api.Tests;

// Covers: "Event enrolments - verifying that Participants can enrol and that the enrolment is correctly recorded"
public class EnrolmentTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory factory;

    public EnrolmentTests(RaceDayApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Participant_CanEnrolInCategory_AndEnrolmentIsRecordedWithResolvedEventId()
    {
        var organiserClient = factory.CreateClient();
        var organiser = await TestAuthHelper.RegisterAndLoginAsync(organiserClient, "Organiser");

        var eventResponse = await organiserClient.PostAsJsonAsync("/api/events", new
        {
            eventName = "Soweto Marathon",
            eventDate = "2026-11-15",
            location = "Soweto, Johannesburg",
            distance = 42.2,
            eventType = "Run"
        });
        var ev = await eventResponse.Content.ReadFromJsonAsync<JsonElement>();
        var eventId = ev.GetProperty("eventId").GetInt32();

        var categoryResponse = await organiserClient.PostAsJsonAsync(
            $"/api/events/{eventId}/categories", new { categoryName = "10km Fun Run" });
        var category = await categoryResponse.Content.ReadFromJsonAsync<JsonElement>();
        var categoryId = category.GetProperty("categoryId").GetInt32();

        var participantClient = factory.CreateClient();
        var participant = await TestAuthHelper.RegisterAndLoginAsync(participantClient, "Participant");

        var enrolResponse = await participantClient.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Created, enrolResponse.StatusCode);

        var enrolment = await enrolResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(categoryId, enrolment.GetProperty("categoryId").GetInt32());
        Assert.Equal(eventId, enrolment.GetProperty("eventId").GetInt32()); // resolved automatically, per the plan

        var mineResponse = await participantClient.GetAsync("/api/enrolments/me");
        Assert.Equal(HttpStatusCode.OK, mineResponse.StatusCode);
        var mine = await mineResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, mine.GetArrayLength());
    }

    [Fact]
    public async Task Participant_CannotDoubleEnrolInSameCategory()
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
        var ev = await eventResponse.Content.ReadFromJsonAsync<JsonElement>();
        var eventId = ev.GetProperty("eventId").GetInt32();

        var categoryResponse = await organiserClient.PostAsJsonAsync(
            $"/api/events/{eventId}/categories", new { categoryName = "Family Fun Ride" });
        var category = await categoryResponse.Content.ReadFromJsonAsync<JsonElement>();
        var categoryId = category.GetProperty("categoryId").GetInt32();

        var participantClient = factory.CreateClient();
        var participant = await TestAuthHelper.RegisterAndLoginAsync(participantClient, "Participant");

        var first = await participantClient.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await participantClient.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Participant_CanWithdrawOnlyTheirOwnEnrolment()
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
        var ev = await eventResponse.Content.ReadFromJsonAsync<JsonElement>();
        var eventId = ev.GetProperty("eventId").GetInt32();

        var categoryResponse = await organiserClient.PostAsJsonAsync(
            $"/api/events/{eventId}/categories", new { categoryName = "5km Park Run" });
        var category = await categoryResponse.Content.ReadFromJsonAsync<JsonElement>();
        var categoryId = category.GetProperty("categoryId").GetInt32();

        var participantAClient = factory.CreateClient();
        var participantA = await TestAuthHelper.RegisterAndLoginAsync(participantAClient, "Participant");
        var enrolResponse = await participantAClient.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        var enrolment = await enrolResponse.Content.ReadFromJsonAsync<JsonElement>();
        var enrolmentId = enrolment.GetProperty("enrolmentId").GetInt32();

        var participantBClient = factory.CreateClient();
        var participantB = await TestAuthHelper.RegisterAndLoginAsync(participantBClient, "Participant");

        var forbiddenWithdraw = await participantBClient.DeleteAsync($"/api/enrolments/{enrolmentId}");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenWithdraw.StatusCode);

        var successfulWithdraw = await participantAClient.DeleteAsync($"/api/enrolments/{enrolmentId}");
        Assert.Equal(HttpStatusCode.NoContent, successfulWithdraw.StatusCode);
    }


    [Fact]
    public async Task Organiser_CanViewAllEnrolmentsForTheirEvent()
    {
        var organiser = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser");
        var eventId = await TestAuthHelper.CreateEventAsync(organiser);
        var categoryId = await TestAuthHelper.CreateCategoryAsync(organiser, eventId);

        for (var i = 0; i < 2; i++)
        {
            var participant = factory.CreateClient();
            await TestAuthHelper.RegisterAndLoginAsync(participant, "Participant");
            await TestAuthHelper.EnrolAsync(participant, categoryId);
        }

        var response = await organiser.GetAsync($"/api/events/{eventId}/enrolments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, list.GetArrayLength());
    }

    [Fact]
    public async Task OtherOrganiser_CannotViewEnrolmentsForSomeoneElsesEvent()
    {
        var owner = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(owner, "Organiser");
        var eventId = await TestAuthHelper.CreateEventAsync(owner);

        var other = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(other, "Organiser");

        var response = await other.GetAsync($"/api/events/{eventId}/enrolments");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithoutLogin_ReturnsUnauthorized()
    {
        var response = await factory.CreateClient().PostAsync("/api/categories/1/enrolments", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InUnknownCategory_ReturnsNotFound()
    {
        var participant = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(participant, "Participant");

        var response = await participant.PostAsync("/api/categories/999999/enrolments", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enrolment_IsLinkedToParticipantEventAndCategory()
    {
        var organiser = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser");
        var eventId = await TestAuthHelper.CreateEventAsync(organiser, "Linked Event");
        var categoryId = await TestAuthHelper.CreateCategoryAsync(organiser, eventId, "Senior");

        var participant = factory.CreateClient();
        var account = await TestAuthHelper.RegisterAndLoginAsync(participant, "Participant");
        await TestAuthHelper.EnrolAsync(participant, categoryId);

        var mine = await (await participant.GetAsync("/api/enrolments/me")).Content.ReadFromJsonAsync<JsonElement>();
        var row = mine[0];
        Assert.Equal(account.UserId, row.GetProperty("userId").GetInt32());
        Assert.Equal(eventId, row.GetProperty("eventId").GetInt32());
        Assert.Equal(categoryId, row.GetProperty("categoryId").GetInt32());
        Assert.Equal("Linked Event", row.GetProperty("eventName").GetString());
        Assert.Equal("Senior", row.GetProperty("categoryName").GetString());
    }
}
