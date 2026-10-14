using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Api.Tests;

/// <summary>Helpers shared by the test classes. Each HttpClient keeps its own session cookie.</summary>
public static class TestAuthHelper
{
    public const string Password = "SuperSecret123!";

    public record RegisteredAccount(string Email, string Password, string Role, int UserId);

    /// <summary>Registers a new account and logs in; the client then carries the session cookie.</summary>
    public static async Task<RegisteredAccount> RegisterAndLoginAsync(
        HttpClient client, string role, string? emailPrefix = null)
    {
        var email = $"{emailPrefix ?? role.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";

        var registerBody = new Dictionary<string, object?>
        {
            ["role"] = role,
            ["fullName"] = $"Test {role}",
            ["email"] = email,
            ["password"] = Password
        };
        if (role == "Participant") registerBody["dateOfBirth"] = "1995-06-15";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerBody);
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        loginResponse.EnsureSuccessStatusCode();

        var json = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        return new RegisteredAccount(email, Password, role, json.GetProperty("userId").GetInt32());
    }

    public static async Task<int> CreateEventAsync(HttpClient organiserClient, string name = "Soweto Marathon", string type = "Run")
    {
        var response = await organiserClient.PostAsJsonAsync("/api/events", new
        {
            eventName = name,
            description = "Test event",
            eventDate = "2026-11-15",
            location = "Soweto, Johannesburg",
            distance = 42.2,
            eventType = type
        });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("eventId").GetInt32();
    }

    public static async Task<int> CreateCategoryAsync(HttpClient organiserClient, int eventId, string name = "10km")
    {
        var response = await organiserClient.PostAsJsonAsync($"/api/events/{eventId}/categories", new { categoryName = name });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("categoryId").GetInt32();
    }

    public static async Task<int> EnrolAsync(HttpClient participantClient, int categoryId)
    {
        var response = await participantClient.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("enrolmentId").GetInt32();
    }
}
