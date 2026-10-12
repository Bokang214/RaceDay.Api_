using System.Security.Claims;

namespace RaceDay.Api.Services;

// Small helper to read the authenticated user's id/role from the principal that
// SessionAuthenticationHandler builds from the server-side session.
public static class CurrentUserExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)
                      ?? throw new InvalidOperationException("No user id in session.");
        return int.Parse(idClaim.Value);
    }

    public static string GetRole(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Role)?.Value
               ?? throw new InvalidOperationException("No role in session.");
    }

    public static bool IsOrganiser(this ClaimsPrincipal user) => user.GetRole() == "Organiser";
    public static bool IsParticipant(this ClaimsPrincipal user) => user.GetRole() == "Participant";
}
