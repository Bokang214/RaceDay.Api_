using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.Api.Tests;

// Covers the User Profile requirement: both roles can view and update their own profile.
public class ProfileTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory factory;

    public ProfileTests(RaceDayApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Participant_CanViewAndUpdateOwnProfile()
    {
        var client = factory.CreateClient();
        var account = await TestAuthHelper.RegisterAndLoginAsync(client, "Participant");

        var update = await client.PutAsJsonAsync("/api/users/me", new { fullName = "Updated Name", dateOfBirth = "2000-01-31" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var me = await (await client.GetAsync("/api/users/me")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Updated Name", me.GetProperty("fullName").GetString());
        Assert.Equal("Participant", me.GetProperty("role").GetString());
        Assert.Equal(account.Email, me.GetProperty("email").GetString());
        Assert.Equal("2000-01-31", me.GetProperty("dateOfBirth").GetString());
    }

    [Fact]
    public async Task Organiser_CanViewAndUpdateOwnProfile()
    {
        var client = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(client, "Organiser");

        var update = await client.PutAsJsonAsync("/api/users/me", new { fullName = "Race Director" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var me = await (await client.GetAsync("/api/users/me")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Race Director", me.GetProperty("fullName").GetString());
        Assert.Equal("Organiser", me.GetProperty("role").GetString());
    }

    [Fact]
    public async Task UpdateProfile_WithoutLogin_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();
        var response = await client.PutAsJsonAsync("/api/users/me", new { fullName = "Hacker" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_EmptyName_ReturnsBadRequest()
    {
        var client = factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(client, "Participant");

        var response = await client.PutAsJsonAsync("/api/users/me", new { fullName = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
