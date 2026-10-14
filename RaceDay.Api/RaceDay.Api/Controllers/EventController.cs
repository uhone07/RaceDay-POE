using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
[SessionAuthorize] // any logged-in user unless overridden below
public class EventsController : ControllerBase
{
    private static readonly string[] ValidTypes = { "Run", "Walk", "Cycle" };
    private readonly RaceDayDbContext _context;

    public EventsController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>Lists all events, optionally filtered by type (Run, Walk, Cycle).</summary>
    /// <response code="200">List of events.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet]
    public async Task<IActionResult> GetEvents([FromQuery] string? type)
    {
        var query = _context.Events.AsQueryable();

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(e => e.EventType == type);

        var events = await query
            .OrderBy(e => e.EventDate)
            .Select(e => new
            {
                e.EventId,
                e.Name,
                e.Description,
                e.EventDate,
                e.Location,
                e.DistanceKm,
                e.EventType,
                e.BannerImageUrl,
                e.OrganiserId
            })
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>Returns one event with its categories.</summary>
    /// <response code="200">Event found.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEvent(int id)
    {
        var ev = await _context.Events
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev == null)
            return NotFound(new { message = "Event not found." });

        return Ok(new
        {
            ev.EventId,
            ev.Name,
            ev.Description,
            ev.EventDate,
            ev.Location,
            ev.DistanceKm,
            ev.EventType,
            ev.BannerImageUrl,
            ev.OrganiserId,
            Categories = ev.Categories.Select(c => new
            {
                c.CategoryId,
                c.Name,
                c.MinAge,
                c.MaxAge,
                c.DistanceKm
            })
        });
    }

    /// <summary>Creates a new event (Organiser only).</summary>
    /// <response code="201">Event created.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser.</response>
    [HttpPost]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> CreateEvent(EventDto dto)
    {
        if (!ValidTypes.Contains(dto.EventType))
            return BadRequest(new { message = "EventType must be Run, Walk or Cycle." });

        var ev = new Event
        {
            OrganiserId = HttpContext.Session.GetUserId(),
            Name = dto.Name,
            Description = dto.Description,
            EventDate = dto.EventDate,
            Location = dto.Location,
            DistanceKm = dto.DistanceKm,
            EventType = dto.EventType,
            BannerImageUrl = dto.BannerImageUrl
        };

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventId }, new { ev.EventId, ev.Name });
    }

    /// <summary>Updates an event you created (Organiser only).</summary>
    /// <response code="200">Event updated.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpPut("{id}")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> UpdateEvent(int id, EventDto dto)
    {
        if (!ValidTypes.Contains(dto.EventType))
            return BadRequest(new { message = "EventType must be Run, Walk or Cycle." });

        var ev = await _context.Events.FindAsync(id);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != HttpContext.Session.GetUserId())
            return StatusCode(403, new { message = "You can only edit your own events." });

        ev.Name = dto.Name;
        ev.Description = dto.Description;
        ev.EventDate = dto.EventDate;
        ev.Location = dto.Location;
        ev.DistanceKm = dto.DistanceKm;
        ev.EventType = dto.EventType;
        ev.BannerImageUrl = dto.BannerImageUrl;

        await _context.SaveChangesAsync();
        return Ok(new { ev.EventId, ev.Name });
    }

    /// <summary>Deletes an event you created (Organiser only).</summary>
    /// <response code="204">Event deleted.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Event does not exist.</response>
    /// <response code="409">Event has enrolments and cannot be deleted.</response>
    [HttpDelete("{id}")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _context.Events.FindAsync(id);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != HttpContext.Session.GetUserId())
            return StatusCode(403, new { message = "You can only delete your own events." });

        if (await _context.Enrolments.AnyAsync(e => e.EventId == id))
            return Conflict(new { message = "Event has enrolments and cannot be deleted." });

        _context.Events.Remove(ev);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}