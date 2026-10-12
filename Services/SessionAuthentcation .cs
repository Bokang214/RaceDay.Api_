using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace RaceDay.Api.Services;

public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
    public const string Email = "Email";
}

public static class SessionAuthenticationDefaults
{
    public const string Scheme = "RaceDaySession";
}

/// <summary>
/// Authentication handler backed by ASP.NET Core server-side session state.
/// On login the controller stores the user's id, role and email in the session
/// (kept on the server); the browser only holds an opaque session-id cookie.
/// On every later request this handler rebuilds the principal from the session,
/// so [Authorize] and [Authorize(Roles = "...")] work off the server-side data.
/// No session => 401 Unauthorized, wrong role => 403 Forbidden.
/// </summary>
public class SessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public SessionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        await Context.Session.LoadAsync();

        var userId = Context.Session.GetInt32(SessionKeys.UserId);
        var role = Context.Session.GetString(SessionKeys.Role);
        var email = Context.Session.GetString(SessionKeys.Email) ?? string.Empty;

        if (userId is null || string.IsNullOrEmpty(role))
        {
            return AuthenticateResult.NoResult();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.Value.ToString()),
            new(ClaimTypes.Role, role),
            new(ClaimTypes.Email, email)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}
