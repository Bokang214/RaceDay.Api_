using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
public class EnrolmentsController : ApiControllerBase
{
    private readonly RaceDayContext db;

    public EnrolmentsController(RaceDayContext db)
    {
        this.db = db;
    }

    /// <summary>Enrol in an event by choosing a category (Participant only).</summary>
    /// <remarks>The event is worked out from the category. The link between the Participant, the event and the category is recorded.</remarks>
    /// <param name="categoryId">The category being entered.</param>
    /// <response code="201">The recorded enrolment.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    /// <response code="404">No category with that id.</response>
    /// <response code="409">Already enrolled in this category.</response>
    [HttpPost("api/categories/{categoryId:int}/enrolments")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int categoryId)
    {
        try
        {
            var category = await db.Categories.FirstOrDefaultAsync(c => c.CategoryId == categoryId);
            if (category is null) return NotFound();

            var userId = User.GetUserId();

            var alreadyEnrolled = await db.Enrolments.AnyAsync(en => en.UserId == userId && en.CategoryId == categoryId);
            if (alreadyEnrolled) return Conflict(new { message = "Already enrolled in this category." });

            var enrolment = new Enrolment
            {
                UserId = userId,
                CategoryId = categoryId,
                EventId = category.EventId // resolved automatically from the category, per the Part 1 plan
            };

            db.Enrolments.Add(enrolment);
            await db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMine), null, ToResponse(enrolment));
        }
        catch (Exception ex)
        {
            return ServerError(ex);
        }
    }

    /// <summary>List my enrolments (Participant only).</summary>
    /// <response code="200">The logged-in Participant's enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    [HttpGet("api/enrolments/me")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDetailResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine()
    {
        try
        {
            var userId = User.GetUserId();

            var enrolments = await db.Enrolments
                .Where(en => en.UserId == userId)
                .Select(en => new EnrolmentDetailResponse
                {
                    EnrolmentId = en.EnrolmentId,
                    UserId = en.UserId,
                    EventId = en.EventId,
                    CategoryId = en.CategoryId,
                    EnrolmentDate = en.EnrolmentDate,
                    ParticipantName = en.User.FullName,
                    EventName = en.Event.EventName,
                    CategoryName = en.Category.CategoryName
                })
                .ToListAsync();

            return Ok(enrolments);
        }
        catch (Exception ex)
        {
            return ServerError(ex);
        }
    }

    /// <summary>List all enrolments for an event (owning Organiser only).</summary>
    /// <param name="eventId">The event id.</param>
    /// <response code="200">Every enrolment for the event.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Participant, or an Organiser who does not own the event.</response>
    /// <response code="404">No event with that id.</response>
    [HttpGet("api/events/{eventId:int}/enrolments")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        try
        {
            var ev = await db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
            if (ev is null) return NotFound();
            if (ev.OrganiserId != User.GetUserId()) return Forbid();

            var enrolments = await db.Enrolments
                .Where(en => en.EventId == eventId)
                .Select(en => new EnrolmentDetailResponse
                {
                    EnrolmentId = en.EnrolmentId,
                    UserId = en.UserId,
                    EventId = en.EventId,
                    CategoryId = en.CategoryId,
                    EnrolmentDate = en.EnrolmentDate,
                    ParticipantName = en.User.FullName,
                    EventName = en.Event.EventName,
                    CategoryName = en.Category.CategoryName
                })
                .ToListAsync();

            return Ok(enrolments);
        }
        catch (Exception ex)
        {
            return ServerError(ex);
        }
    }

    /// <summary>Withdraw my enrolment (Participant only, own enrolments).</summary>
    /// <param name="id">The enrolment id.</param>
    /// <response code="204">Withdrawn.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not your enrolment, or logged in as an Organiser.</response>
    /// <response code="404">No enrolment with that id.</response>
    [HttpDelete("api/enrolments/{id:int}")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Withdraw(int id)
    {
        try
        {
            var enrolment = await db.Enrolments.FirstOrDefaultAsync(en => en.EnrolmentId == id);
            if (enrolment is null) return NotFound();
            if (enrolment.UserId != User.GetUserId()) return Forbid();

            db.Enrolments.Remove(enrolment);
            await db.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return ServerError(ex);
        }
    }

    private static EnrolmentResponse ToResponse(Enrolment en) => new()
    {
        EnrolmentId = en.EnrolmentId,
        UserId = en.UserId,
        EventId = en.EventId,
        CategoryId = en.CategoryId,
        EnrolmentDate = en.EnrolmentDate
    };
}
