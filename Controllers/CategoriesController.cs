using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
public class CategoriesController : ApiControllerBase
{
    private readonly RaceDayContext db;

    public CategoriesController(RaceDayContext db)
    {
        this.db = db;
    }

    /// <summary>List the categories of an event (public).</summary>
    /// <param name="eventId">The event id.</param>
    /// <response code="200">The event's categories (e.g. Under 20, Senior, 10km, 21km).</response>
    /// <response code="404">No event with that id.</response>
    [HttpGet("api/events/{eventId:int}/categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        try
        {
            var eventExists = await db.Events.AnyAsync(e => e.EventId == eventId);
            if (!eventExists) return NotFound();

            var categories = await db.Categories
                .Where(c => c.EventId == eventId)
                .Select(c => new CategoryResponse { CategoryId = c.CategoryId, EventId = c.EventId, CategoryName = c.CategoryName })
                .ToListAsync();

            return Ok(categories);
        }
        catch (Exception ex)
        {
            return ServerError(ex);
        }
    }

    /// <summary>Add an age or distance category to an event (owning Organiser only).</summary>
    /// <param name="eventId">The event id.</param>
    /// <response code="201">The created category.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Participant, or an Organiser who does not own the event.</response>
    /// <response code="404">No event with that id.</response>
    [HttpPost("api/events/{eventId:int}/categories")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddToEvent(int eventId, [FromBody] CreateCategoryRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var ev = await db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
            if (ev is null) return NotFound();
            if (ev.OrganiserId != User.GetUserId()) return Forbid();

            var category = new Category { EventId = eventId, CategoryName = request.CategoryName };
            db.Categories.Add(category);
            await db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetForEvent), new { eventId }, new CategoryResponse
            {
                CategoryId = category.CategoryId,
                EventId = category.EventId,
                CategoryName = category.CategoryName
            });
        }
        catch (Exception ex)
        {
            return ServerError(ex);
        }
    }

    /// <summary>Rename a category (owning Organiser only).</summary>
    /// <param name="id">The category id.</param>
    /// <response code="200">The updated category.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Participant, or an Organiser who does not own the event.</response>
    /// <response code="404">No category with that id.</response>
    [HttpPut("api/categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var category = await db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category is null) return NotFound();
            if (category.Event.OrganiserId != User.GetUserId()) return Forbid();

            if (!string.IsNullOrWhiteSpace(request.CategoryName)) category.CategoryName = request.CategoryName;

            await db.SaveChangesAsync();

            return Ok(new CategoryResponse { CategoryId = category.CategoryId, EventId = category.EventId, CategoryName = category.CategoryName });
        }
        catch (Exception ex)
        {
            return ServerError(ex);
        }
    }

    /// <summary>Delete a category (owning Organiser only).</summary>
    /// <remarks>Refused with 409 while Participants are enrolled in it.</remarks>
    /// <param name="id">The category id.</param>
    /// <response code="204">Deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Participant, or an Organiser who does not own the event.</response>
    /// <response code="404">No category with that id.</response>
    /// <response code="409">The category has enrolments.</response>
    [HttpDelete("api/categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var category = await db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category is null) return NotFound();
            if (category.Event.OrganiserId != User.GetUserId()) return Forbid();

            var hasEnrolments = await db.Enrolments.AnyAsync(en => en.CategoryId == id);
            if (hasEnrolments) return Conflict(new { message = "Category already has enrolments." });

            db.Categories.Remove(category);
            await db.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return ServerError(ex);
        }
    }
}
