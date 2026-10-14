using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.Api.Tests;

// Covers Categories: Organisers define them, both roles (and the public) can view them.
public class CategoryTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory factory;

    public CategoryTests(RaceDayApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Organiser_CanAddUpdateAndDeleteCategory()
    {
        var organiser = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser");
        var eventId = await TestAuthHelper.CreateEventAsync(organiser);
        var categoryId = await TestAuthHelper.CreateCategoryAsync(organiser, eventId, "Under 20");

        var update = await organiser.PutAsJsonAsync($"/api/categories/{categoryId}", new { categoryName = "Senior" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Senior", updated.GetProperty("categoryName").GetString());

        var delete = await organiser.DeleteAsync($"/api/categories/{categoryId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task Participant_CanViewCategories_ButNotCreateThem()
    {
        var organiser = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser");
        var eventId = await TestAuthHelper.CreateEventAsync(organiser);
        await TestAuthHelper.CreateCategoryAsync(organiser, eventId, "21km");

        var participant = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(participant, "Participant");

        var list = await participant.GetAsync($"/api/events/{eventId}/categories");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var items = await list.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, items.GetArrayLength());

        var create = await participant.PostAsJsonAsync($"/api/events/{eventId}/categories", new { categoryName = "Sneaky" });
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotAddCategoryToAnotherOrganisersEvent()
    {
        var owner = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(owner, "Organiser");
        var eventId = await TestAuthHelper.CreateEventAsync(owner);

        var other = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(other, "Organiser");

        var response = await other.PostAsJsonAsync($"/api/events/{eventId}/categories", new { categoryName = "Not mine" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddCategory_WithoutLogin_ReturnsUnauthorized()
    {
        var organiser = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser");
        var eventId = await TestAuthHelper.CreateEventAsync(organiser);

        var anonymous = factory.CreateClient();
        var response = await anonymous.PostAsJsonAsync($"/api/events/{eventId}/categories", new { categoryName = "Anon" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddCategory_ToMissingEvent_ReturnsNotFound()
    {
        var organiser = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events/999999/categories", new { categoryName = "Ghost" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotDeleteCategoryThatHasEnrolments()
    {
        var organiser = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser");
        var eventId = await TestAuthHelper.CreateEventAsync(organiser);
        var categoryId = await TestAuthHelper.CreateCategoryAsync(organiser, eventId);

        var participant = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(participant, "Participant");
        await TestAuthHelper.EnrolAsync(participant, categoryId);

        var response = await organiser.DeleteAsync($"/api/categories/{categoryId}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
