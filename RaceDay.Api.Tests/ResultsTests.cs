using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.Api.Tests;

// Covers Results: Organisers capture finish times/positions, Participants view their own.
public class ResultsTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory factory;

    public ResultsTests(RaceDayApiFactory factory)
    {
        this.factory = factory;
    }

    private async Task<(HttpClient organiser, HttpClient participant, int enrolmentId, int eventId)> SetUpAsync()
    {
        var organiser = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser");
        var eventId = await TestAuthHelper.CreateEventAsync(organiser);
        var categoryId = await TestAuthHelper.CreateCategoryAsync(organiser, eventId);

        var participant = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(participant, "Participant");
        var enrolmentId = await TestAuthHelper.EnrolAsync(participant, categoryId);

        return (organiser, participant, enrolmentId, eventId);
    }

    [Fact]
    public async Task Organiser_CanCaptureResult_AndParticipantCanViewIt()
    {
        var (organiser, participant, enrolmentId, _) = await SetUpAsync();

        var create = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
            new { finishTime = "01:23:45", position = 3 });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var mine = await participant.GetAsync("/api/results/me");
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        var results = await mine.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, results.GetArrayLength());
        Assert.Equal(3, results[0].GetProperty("position").GetInt32());
        Assert.StartsWith("01:23:45", results[0].GetProperty("finishTime").GetString());
    }

    [Fact]
    public async Task Organiser_CanUpdateResult()
    {
        var (organiser, _, enrolmentId, _) = await SetUpAsync();
        var create = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
            new { finishTime = "00:50:00", position = 5 });
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var resultId = created.GetProperty("resultId").GetInt32();

        var update = await organiser.PutAsJsonAsync($"/api/results/{resultId}", new { position = 2 });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, updated.GetProperty("position").GetInt32());
    }

    [Fact]
    public async Task Participant_CannotCaptureResults()
    {
        var (_, participant, enrolmentId, _) = await SetUpAsync();

        var response = await participant.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
            new { finishTime = "00:10:00", position = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_CannotCaptureResultsForSomeoneElsesEvent()
    {
        var (_, _, enrolmentId, _) = await SetUpAsync();

        var other = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(other, "Organiser");

        var response = await other.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
            new { finishTime = "00:10:00", position = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_Twice_ReturnsConflict()
    {
        var (organiser, _, enrolmentId, _) = await SetUpAsync();
        var body = new { finishTime = "00:40:00", position = 1 };

        Assert.Equal(HttpStatusCode.Created, (await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", body)).StatusCode);
    }

    [Fact]
    public async Task CaptureResult_ForMissingEnrolment_ReturnsNotFound()
    {
        var organiser = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/enrolments/999999/results", new { finishTime = "00:40:00", position = 1 });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_InvalidPosition_ReturnsBadRequest()
    {
        var (organiser, _, enrolmentId, _) = await SetUpAsync();

        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
            new { finishTime = "00:40:00", position = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ViewMyResults_WithoutLogin_ReturnsUnauthorized()
    {
        var response = await factory.CreateClient().GetAsync("/api/results/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUseParticipantResultsEndpoint()
    {
        var organiser = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser");

        var response = await organiser.GetAsync("/api/results/me");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_OnlySeesTheirOwnResults()
    {
        var (organiser, _, enrolmentId, eventId) = await SetUpAsync();
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", new { finishTime = "00:30:00", position = 1 });

        var stranger = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(stranger, "Participant");

        var mine = await (await stranger.GetAsync("/api/results/me")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, mine.GetArrayLength());

        // The public leaderboard for the event still shows the captured result.
        var board = await (await stranger.GetAsync($"/api/events/{eventId}/results")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, board.GetArrayLength());
    }
}
