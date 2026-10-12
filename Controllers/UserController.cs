using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize] // Any logged-in user (Organiser or Participant)
public class UsersController : ApiControllerBase
{
    private readonly RaceDayContext db;

    public UsersController(RaceDayContext db)
    {
        this.db = db;
    }

    /// <summary>View my own profile (Organiser or Participant).</summary>
    /// <response code="200">The logged-in user's profile.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        try
        {
            var id = User.GetUserId();

            if (User.IsOrganiser())
            {
                var organiser = await db.Organisers.FindAsync(id);
                if (organiser is null) return Unauthorized();

                return Ok(new ProfileResponse
                {
                    Id = organiser.OrganiserId,
                    Role = "Organiser",
                    FullName = organiser.FullName,
                    Email = organiser.Email
                });
            }
            else
            {
                var user = await db.Users.FindAsync(id);
                if (user is null) return Unauthorized();

                return Ok(new ProfileResponse
                {
                    Id = user.UserId,
                    Role = "Participant",
                    FullName = user.FullName,
                    Email = user.Email,
                    DateOfBirth = user.DateOfBirth
                });
            }
        }
        catch (Exception ex)
        {
            return ServerError(ex);
        }
    }

    /// <summary>Update my own profile (Organiser or Participant).</summary>
    /// <remarks>Send fullName and, for Participants, dateOfBirth. Fields left out are unchanged.</remarks>
    /// <response code="200">The updated profile.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var id = User.GetUserId();

            if (User.IsOrganiser())
            {
                var organiser = await db.Organisers.FindAsync(id);
                if (organiser is null) return Unauthorized();

                if (!string.IsNullOrWhiteSpace(request.FullName)) organiser.FullName = request.FullName;

                await db.SaveChangesAsync();

                return Ok(new ProfileResponse
                {
                    Id = organiser.OrganiserId,
                    Role = "Organiser",
                    FullName = organiser.FullName,
                    Email = organiser.Email
                });
            }
            else
            {
                var user = await db.Users.FindAsync(id);
                if (user is null) return Unauthorized();

                if (!string.IsNullOrWhiteSpace(request.FullName)) user.FullName = request.FullName;
                if (request.DateOfBirth is not null) user.DateOfBirth = request.DateOfBirth;

                await db.SaveChangesAsync();

                return Ok(new ProfileResponse
                {
                    Id = user.UserId,
                    Role = "Participant",
                    FullName = user.FullName,
                    Email = user.Email,
                    DateOfBirth = user.DateOfBirth
                });
            }
        }
        catch (Exception ex)
        {
            return ServerError(ex);
        }
    }
}
