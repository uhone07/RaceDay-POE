using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
[SessionAuthorize] // any logged-in user unless overridden below
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EnrolmentsController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>Enters the logged-in Participant into an event under a chosen category.</summary>
    /// <response code="201">Enrolment recorded.</response>
    /// <response code="400">Category does not belong to this event.</response>
    /// <response code="403">Not a Participant.</response>
    /// <response code="404">Event does not exist.</response>
    /// <response code="409">Already enrolled in this event.</response>
    [HttpPost("events/{eventId}/enrolmentsments")]
    [SessionAuthorize("Participant")]
    public async Task<IActionResult> Enrol(int eventId, EnrolDto dto)
    {
        var participantId = HttpContext.Session.GetUserId();

        var ev = await _context.Events.FindAsync(eventId);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId && c.EventId == eventId);
        if (category == null)
            return BadRequest(new { message = "Category does not belong to this event." });

        if (await _context.Enrolments.AnyAsync(e => e.ParticipantId == participantId && e.EventId == eventId))
            return Conflict(new { message = "You are already enrolled in this event." });

        var enrolment = new Enrolment
        {
            ParticipantId = participantId,
            EventId = eventId,
            CategoryId = dto.CategoryId,
            Status = "Pending"
        };

        _context.Enrolments.Add(enrolment);
        await _context.SaveChangesAsync();

        return StatusCode(201, new
        {
            enrolment.EnrolmentId,
            enrolment.EventId,
            enrolment.CategoryId,
            enrolment.Status,
            enrolment.EnrolmentDate
        });
    }

    /// <summary>Lists the logged-in Participant's own enrolments.</summary>
    /// <response code="200">List of enrolments.</response>
    /// <response code="403">Not a Participant.</response>
    [HttpGet("enrolments/my")]
    [SessionAuthorize("Participant")]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var participantId = HttpContext.Session.GetUserId();

        var list = await _context.Enrolments
            .Where(e => e.ParticipantId == participantId)
            .Include(e => e.Event)
            .Include(e => e.Category)
            .OrderBy(e => e.Event!.EventDate)
            .Select(e => new
            {
                e.EnrolmentId,
                e.EventId,
                EventName = e.Event!.Name,
                e.Event.EventDate,
                CategoryName = e.Category!.Name,
                e.Status,
                e.EnrolmentDate
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>Lists all enrolments for one of your events (Organiser only).</summary>
    /// <response code="200">List of enrolments.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("events/{eventId}/enrolmentsments")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> GetEventEnrolments(int eventId)
    {
        var ev = await _context.Events.FindAsync(eventId);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != HttpContext.Session.GetUserId())
            return StatusCode(403, new { message = "You can only view enrolments for your own events." });

        var list = await _context.Enrolments
            .Where(e => e.EventId == eventId)
            .Include(e => e.Participant).ThenInclude(p => p!.Profile)
            .Include(e => e.Category)
            .Select(e => new
            {
                e.EnrolmentId,
                e.ParticipantId,
                ParticipantName = e.Participant!.Profile!.FullName,
                CategoryName = e.Category!.Name,
                e.Status,
                e.EnrolmentDate
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>Sets the status of an enrolment on one of your events (Organiser only).</summary>
    /// <response code="200">Status updated.</response>
    /// <response code="400">Invalid status value.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Enrolment does not exist.</response>
    [HttpPut("enrolments/{id}/status")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateEnrolmentStatusDto dto)
    {
        var valid = new[] { "Pending", "Confirmed", "Cancelled" };
        if (!valid.Contains(dto.Status))
            return BadRequest(new { message = "Status must be Pending, Confirmed or Cancelled." });

        var enrolment = await _context.Enrolments
            .Include(e => e.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);

        if (enrolment == null)
            return NotFound(new { message = "Enrolment not found." });

        if (enrolment.Event!.OrganiserId != HttpContext.Session.GetUserId())
            return StatusCode(403, new { message = "You can only update enrolments for your own events." });

        enrolment.Status = dto.Status;
        await _context.SaveChangesAsync();
        return Ok(new { enrolment.EnrolmentId, enrolment.Status });
    }

    /// <summary>Cancels one of your own enrolments (Participant only).</summary>
    /// <response code="200">Enrolment cancelled.</response>
    /// <response code="403">Not a Participant, or not your enrolment.</response>
    /// <response code="404">Enrolment does not exist.</response>
    [HttpPut("enrolments/{id}/cancel")]
    [SessionAuthorize("Participant")]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await _context.Enrolments.FindAsync(id);
        if (enrolment == null)
            return NotFound(new { message = "Enrolment not found." });

        if (enrolment.ParticipantId != HttpContext.Session.GetUserId())
            return StatusCode(403, new { message = "You can only cancel your own enrolments." });

        enrolment.Status = "Cancelled";
        await _context.SaveChangesAsync();
        return Ok(new { enrolment.EnrolmentId, enrolment.Status });
    }
}