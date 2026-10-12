using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
public class ResultsController : ApiControllerBase
{
    private readonly RaceDayContext db;

    public ResultsController(RaceDayContext db)
    {
        this.db = db;
    }

    /// <summary>Capture a finish time and position for an enrolment (owning Organiser only).</summary>
    /// <param name="enrolmentId">The enrolment the result belongs to.</param>
    /// <response code="201">The captured result.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Participant, or an Organiser who does not own the event.</response>
    /// <response code="404">No enrolment with that id.</response>
    /// <response code="409">A result already exists - use PUT.</response>
    [HttpPost("api/enrolments/{enrolmentId:int}/results")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(int enrolmentId, [FromBody] CreateResultRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var enrolment = await db.Enrolments
                .Include(en => en.Event)
                .Include(en => en.Result)
                .FirstOrDefaultAsync(en => en.EnrolmentId == enrolmentId);

            if (enrolment is null) return NotFound();
            if (enrolment.Event.OrganiserId != User.GetUserId()) return Forbid();
            if (enrolment.Result is not null)
                return Conflict(new { message = "A result already exists for this enrolment. Use PUT instead." });

            var result = new Result
            {
                EnrolmentId = enrolmentId,
                FinishTime = request.FinishTime,
                Position = request.Position
            };

            db.Results.Add(result);
            await db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMine), null, ToResponse(result));
        }
        catch (Exception ex)
        {
            return ServerError(ex);
        }
    }

    /// <summary>Correct a captured result (owning Organiser only).</summary>
    /// <param name="id">The result id.</param>
    /// <response code="200">The updated result.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Participant, or an Organiser who does not own the event.</response>
    /// <response code="404">No result with that id.</response>
    [HttpPut("api/results/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateResultRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var result = await db.Results
                .Include(r => r.Enrolment).ThenInclude(en => en.Event)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result is null) return NotFound();
            if (result.Enrolment.Event.OrganiserId != User.GetUserId()) return Forbid();

            if (request.FinishTime is not null) result.FinishTime = request.FinishTime;
            if (request.Position is not null) result.Position = request.Position;

            await db.SaveChangesAsync();

            return Ok(ToResponse(result));
        }
        catch (Exception ex)
        {
            return ServerError(ex);
        }
    }

    /// <summary>View my own results (Participant only).</summary>
    /// <response code="200">Results for the logged-in Participant's enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    [HttpGet("api/results/me")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(IEnumerable<LeaderboardEntryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine()
    {
        try
        {
            var userId = User.GetUserId();

            var results = await db.Results
                .Where(r => r.Enrolment.UserId == userId)
                .Select(r => new LeaderboardEntryResponse
                {
                    ParticipantName = r.Enrolment.User.FullName,
                    CategoryName = r.Enrolment.Category.CategoryName,
                    FinishTime = r.FinishTime,
                    Position = r.Position
                })
                .ToListAsync();

            return Ok(results);
        }
        catch (Exception ex)
        {
            return ServerError(ex);
        }
    }

    /// <summary>Event leaderboard (public).</summary>
    /// <param name="eventId">The event id.</param>
    /// <response code="200">Results grouped by category and ordered by position.</response>
    /// <response code="404">No event with that id.</response>
    [HttpGet("api/events/{eventId:int}/results")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<LeaderboardEntryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLeaderboard(int eventId)
    {
        try
        {
            var eventExists = await db.Events.AnyAsync(e => e.EventId == eventId);
            if (!eventExists) return NotFound();

            var results = await db.Results
                .Where(r => r.Enrolment.EventId == eventId)
                .OrderBy(r => r.Enrolment.CategoryId).ThenBy(r => r.Position)
                .Select(r => new LeaderboardEntryResponse
                {
                    ParticipantName = r.Enrolment.User.FullName,
                    CategoryName = r.Enrolment.Category.CategoryName,
                    FinishTime = r.FinishTime,
                    Position = r.Position
                })
                .ToListAsync();

            return Ok(results);
        }
        catch (Exception ex)
        {
            return ServerError(ex);
        }
    }

    private static ResultResponse ToResponse(Result r) => new()
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        FinishTime = r.FinishTime,
        Position = r.Position
    };
}
