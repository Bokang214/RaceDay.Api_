using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ApiControllerBase
{
    private readonly RaceDayContext db;

    public EventsController(RaceDayContext db)
    {
        this.db = db;
    }

    /// <summary>List events (public).</summary>
    /// <remarks>No login needed. Optional query filters: <c>location</c> (partial match), <c>from</c> and <c>to</c> (yyyy-MM-dd).</remarks>
    /// <response code="200">Events ordered by date.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<EventListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? location, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        try
        {
            var query = db.Events.AsQueryable();

            if (!string.IsNullOrWhiteSpace(location))
                query = query.Where(e => e.Location.Contains(location));
            if (from is not null)
                query = query.Where(e => e.EventDate >= from);
            if (to is not null)
                query = query.Where(e => e.EventDate <= to);

            var events = await query
                .OrderBy(e => e.EventDate)
                .Select(e => new EventListItemResponse
                {
                    EventId = e.EventId,
                    EventName = e.EventName,
                    EventDate = e.EventDate,
                    Location = e.Location,
                    Distance = e.Distance,
                    EventType = e.EventType
                })
                .ToListAsync();

            return Ok(events);
        }
        catch (Exception ex)
        {
            return ServerError(ex);
        }
    }

    /// <summary>Get one event with its categories (public).</summary>
    /// <param name="id">The event id.</param>
    /// <response code="200">The event and its categories.</response>
    /// <response code="404">No event with that id.</response>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var ev = await db.Events.Include(e => e.Categories).FirstOrDefaultAsync(e => e.EventId == id);
            if (ev is null) return NotFound();

            return Ok(ToDetail(ev));
        }
        catch (Exception ex)
        {
            return ServerError(ex);
        }
    }

    /// <summary>Create an event (Organiser only).</summary>
    /// <remarks>Expects name, description, date (yyyy-MM-dd), location, distance (km) and eventType (Run, Walk or Cycle). The logged-in Organiser becomes the owner.</remarks>
    /// <response code="201">The created event.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant.</response>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var organiserId = User.GetUserId();

            var ev = new Event
            {
                OrganiserId = organiserId,
                EventName = request.EventName,
                Description = request.Description,
                EventDate = request.EventDate,
                Location = request.Location,
                Distance = request.Distance,
                EventType = EventTypes.Normalise(request.EventType)
            };

            db.Events.Add(ev);
            await db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, ToDetail(ev));
        }
        catch (Exception ex)
        {
            return ServerError(ex);
        }
    }

    /// <summary>Update an event (owning Organiser only).</summary>
    /// <remarks>Only the fields supplied are changed.</remarks>
    /// <param name="id">The event id.</param>
    /// <response code="200">The updated event.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Participant, or an Organiser who does not own this event.</response>
    /// <response code="404">No event with that id.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEventRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var ev = await db.Events.Include(e => e.Categories).FirstOrDefaultAsync(e => e.EventId == id);
            if (ev is null) return NotFound();

            if (ev.OrganiserId != User.GetUserId()) return Forbid();

            if (request.EventName is not null) ev.EventName = request.EventName;
            if (request.Description is not null) ev.Description = request.Description;
            if (request.EventDate is not null) ev.EventDate = request.EventDate.Value;
            if (request.Location is not null) ev.Location = request.Location;
            if (request.Distance is not null) ev.Distance = request.Distance.Value;
            if (request.EventType is not null) ev.EventType = EventTypes.Normalise(request.EventType);

            await db.SaveChangesAsync();

            return Ok(ToDetail(ev));
        }
        catch (Exception ex)
        {
            return ServerError(ex);
        }
    }

    /// <summary>Delete an event (owning Organiser only).</summary>
    /// <remarks>Its categories and enrolments are removed with it.</remarks>
    /// <param name="id">The event id.</param>
    /// <response code="204">Deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Participant, or an Organiser who does not own this event.</response>
    /// <response code="404">No event with that id.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var ev = await db.Events.FirstOrDefaultAsync(e => e.EventId == id);
            if (ev is null) return NotFound();

            if (ev.OrganiserId != User.GetUserId()) return Forbid();

            db.Events.Remove(ev);
            await db.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return ServerError(ex);
        }
    }

    private static EventDetailResponse ToDetail(Event e) => new()
    {
        EventId = e.EventId,
        EventName = e.EventName,
        Description = e.Description,
        EventDate = e.EventDate,
        Location = e.Location,
        Distance = e.Distance,
        EventType = e.EventType,
        OrganiserId = e.OrganiserId,
        Categories = e.Categories.Select(c => new CategoryResponse
        {
            CategoryId = c.CategoryId,
            EventId = c.EventId,
            CategoryName = c.CategoryName
        }).ToList()
    };
}
