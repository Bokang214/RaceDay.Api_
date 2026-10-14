using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using Xunit;

namespace RaceDay.Api.Tests;

// Covers: "User registration and login" - success and failure scenarios,
// plus the server-side session that keeps the user authenticated.
public class AuthTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory factory;
    private readonly HttpClient client;

    public AuthTests(RaceDayApiFactory factory)
    {
        this.factory = factory;
        this.client = factory.CreateClient();
    }

    private static object NewBody(string role, string? email = null, string password = "SuperSecret123!", bool dob = false) =>
        dob
            ? new { role, fullName = "Test Person", email = email ?? $"{role}-{Guid.NewGuid():N}@example.com", password, dateOfBirth = "1995-06-15" }
            : new { role, fullName = "Test Person", email = email ?? $"{role}-{Guid.NewGuid():N}@example.com", password, dateOfBirth = (string?)null };

    [Fact]
    public async Task Register_Organiser_ReturnsCreated()
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", NewBody("Organiser"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_Participant_WithDateOfBirth_ReturnsCreated()
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", NewBody("Participant", dob: true));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_ParticipantWithoutDateOfBirth_ReturnsBadRequest()
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", NewBody("Participant"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_ReturnsBadRequest()
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", NewBody("Admin"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_ReturnsBadRequest()
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", NewBody("Organiser", password: "short"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidEmail_ReturnsBadRequest()
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", NewBody("Organiser", email: "not-an-email"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var body = NewBody("Organiser", email: $"dup-{Guid.NewGuid():N}@example.com");

        var first = await client.PostAsJsonAsync("/api/auth/register", body);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/auth/register", body);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotThePlainText()
    {
        var email = $"hash-{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync("/api/auth/register", NewBody("Organiser", email: email, password: "PlainTextCheck123!"));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayContext>();
        var stored = db.Organisers.Single(o => o.Email == email);

        Assert.NotEqual("PlainTextCheck123!", stored.PasswordHash);
        Assert.DoesNotContain("PlainTextCheck123!", stored.PasswordHash);
        Assert.True(stored.PasswordHash.Length > 20);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsRoleAndStartsSession()
    {
        var account = await TestAuthHelper.RegisterAndLoginAsync(client, "Organiser");
        Assert.True(account.UserId > 0);

        // The session created at login authenticates the next request.
        var me = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task Login_ParticipantRole_IsReturned()
    {
        var email = $"p-{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync("/api/auth/register", NewBody("Participant", email: email, dob: true));

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "SuperSecret123!" });
        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Participant", json.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        var email = $"badlogin-{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync("/api/auth/register", NewBody("Organiser", email: email));

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = $"nobody-{Guid.NewGuid():N}@example.com", password = "SuperSecret123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task FailedLogin_DoesNotCreateASession()
    {
        var email = $"nosession-{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync("/api/auth/register", NewBody("Organiser", email: email));
        await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword123!" });

        var me = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithoutLogin_ReturnsUnauthorized()
    {
        var response = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsTheSession()
    {
        await TestAuthHelper.RegisterAndLoginAsync(client, "Participant");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/me")).StatusCode);

        var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutLogin_ReturnsUnauthorized()
    {
        var response = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Session_StoresTheRole_SoRoleChecksFollowLogin()
    {
        // Same client logs in as an Organiser, then as a Participant: the session role changes with it.
        var organiserEmail = $"o-{Guid.NewGuid():N}@example.com";
        var participantEmail = $"p-{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync("/api/auth/register", NewBody("Organiser", email: organiserEmail));
        await client.PostAsJsonAsync("/api/auth/register", NewBody("Participant", email: participantEmail, dob: true));

        await client.PostAsJsonAsync("/api/auth/login", new { email = organiserEmail, password = "SuperSecret123!" });
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/enrolments/me")).StatusCode);

        await client.PostAsJsonAsync("/api/auth/login", new { email = participantEmail, password = "SuperSecret123!" });
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/enrolments/me")).StatusCode);
    }
}
