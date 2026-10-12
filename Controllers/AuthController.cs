using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly RaceDayContext db;
    private readonly PasswordHasher<object> _hasher = new();

    public AuthController(RaceDayContext db)
    {
        this.db = db;
    }

    /// <summary>Register a new account as an Organiser or a Participant.</summary>
    /// <remarks>
    /// Expects role ("Organiser" or "Participant"), fullName, email and password (min 8 chars).
    /// A Participant must also supply dateOfBirth (yyyy-MM-dd). The password is hashed (PBKDF2) before it is stored.
    /// </remarks>
    /// <response code="201">Account created.</response>
    /// <response code="400">Validation failed (bad role, missing date of birth, weak password...).</response>
    /// <response code="409">Email already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            if (request.Role == "Participant" && request.DateOfBirth is null)
            {
                ModelState.AddModelError(nameof(request.DateOfBirth), "dateOfBirth is required for a Participant.");
                return ValidationProblem(ModelState);
            }

            var email = request.Email.Trim().ToLowerInvariant();

            var emailTaken = await db.Organisers.AnyAsync(o => o.Email.ToLower() == email)
                           || await db.Users.AnyAsync(u => u.Email.ToLower() == email);
            if (emailTaken)
            {
                return Conflict(new { message = "Email already registered." });
            }

            if (request.Role == "Organiser")
            {
                var organiser = new Organiser
                {
                    FullName = request.FullName,
                    Email = request.Email,
                };
                organiser.PasswordHash = _hasher.HashPassword(organiser, request.Password);

                db.Organisers.Add(organiser);
                await db.SaveChangesAsync();

                return CreatedAtAction(nameof(Register), new RegisterResponse
                {
                    Id = organiser.OrganiserId,
                    Role = "Organiser",
                    Email = organiser.Email
                });
            }
            else
            {
                var user = new User
                {
                    FullName = request.FullName,
                    Email = request.Email,
                    Role = "Participant",
                    DateOfBirth = request.DateOfBirth
                };
                user.PasswordHash = _hasher.HashPassword(user, request.Password);

                db.Users.Add(user);
                await db.SaveChangesAsync();

                return CreatedAtAction(nameof(Register), new RegisterResponse
                {
                    Id = user.UserId,
                    Role = "Participant",
                    Email = user.Email
                });
            }
        }
        catch (Exception ex)
        {
            return ServerError(ex);
        }
    }

    /// <summary>Log in and start a server-side session.</summary>
    /// <remarks>
    /// On success the user's id and role are stored in server-side session state and a session cookie is returned.
    /// The cookie is sent automatically by the browser (including Swagger UI) on later requests.
    /// </remarks>
    /// <response code="200">Logged in; returns the user's id, role, name and email.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var email = request.Email.Trim().ToLowerInvariant();

            var organiser = await db.Organisers.FirstOrDefaultAsync(o => o.Email.ToLower() == email);
            if (organiser is not null && VerifyPassword(organiser, organiser.PasswordHash, request.Password))
            {
                StartSession(organiser.OrganiserId, "Organiser", organiser.Email);
                return Ok(new LoginResponse
                {
                    UserId = organiser.OrganiserId,
                    Role = "Organiser",
                    FullName = organiser.FullName,
                    Email = organiser.Email
                });
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (user is not null && VerifyPassword(user, user.PasswordHash, request.Password))
            {
                StartSession(user.UserId, user.Role, user.Email);
                return Ok(new LoginResponse
                {
                    UserId = user.UserId,
                    Role = user.Role,
                    FullName = user.FullName,
                    Email = user.Email
                });
            }

            return Unauthorized(new { message = "Invalid email or password." });
        }
        catch (Exception ex)
        {
            return ServerError(ex);
        }
    }

    /// <summary>Log out and destroy the server-side session.</summary>
    /// <response code="204">Session ended.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        try
        {
            HttpContext.Session.Clear();
            return NoContent();
        }
        catch (Exception ex)
        {
            return ServerError(ex);
        }
    }

    // Replaces any existing session contents with the newly authenticated user's id, role and email.
    private void StartSession(int id, string role, string email)
    {
        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32(SessionKeys.UserId, id);
        HttpContext.Session.SetString(SessionKeys.Role, role);
        HttpContext.Session.SetString(SessionKeys.Email, email);
    }

    private bool VerifyPassword(object entity, string hash, string suppliedPassword)
    {
        var result = _hasher.VerifyHashedPassword(entity, hash, suppliedPassword);
        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
