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
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public ResultsController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>Captures the result for an enrolment on one of your events (Organiser only).</summary>
    /// <response code="201">Result captured.</response>
    /// <response code="400">Position is greater than total finishers.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Enrolment does not exist.</response>
    /// <response code="409">Result already captured for this enrolment.</response>
    [HttpPost("enrolments/{enrolmentId}/results")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> CreateResult(int enrolmentId, ResultDto dto)
    {
        if (dto.FinishPosition > dto.TotalFinishers)
            return BadRequest(new { message = "FinishPosition cannot be greater than TotalFinishers." });

        var enrolment = await _context.Enrolments
            .Include(e => e.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);

        if (enrolment == null)
            return NotFound(new { message = "Enrolment not found." });

        if (enrolment.Event!.OrganiserId != HttpContext.Session.GetUserId())
            return StatusCode(403, new { message = "You can only capture results for your own events." });

        if (enrolment.Result != null)
            return Conflict(new { message = "A result has already been captured for this enrolment." });

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            FinishTime = dto.FinishTime,
            FinishPosition = dto.FinishPosition,
            TotalFinishers = dto.TotalFinishers
        };

        _context.Results.Add(result);
        await _context.SaveChangesAsync();

        return StatusCode(201, new
        {
            result.ResultId,
            result.EnrolmentId,
            result.FinishTime,
            result.FinishPosition,
            result.TotalFinishers
        });
    }

    /// <summary>Updates a previously captured result (Organiser only).</summary>
    /// <response code="200">Result updated.</response>
    /// <response code="400">Position is greater than total finishers.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Result does not exist.</response>
    [HttpPut("results/{id}")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> UpdateResult(int id, ResultDto dto)
    {
        if (dto.FinishPosition > dto.TotalFinishers)
            return BadRequest(new { message = "FinishPosition cannot be greater than TotalFinishers." });

        var result = await _context.Results
            .Include(r => r.Enrolment).ThenInclude(e => e!.Event)
            .FirstOrDefaultAsync(r => r.ResultId == id);

        if (result == null)
            return NotFound(new { message = "Result not found." });

        if (result.Enrolment!.Event!.OrganiserId != HttpContext.Session.GetUserId())
            return StatusCode(403, new { message = "You can only update results for your own events." });

        result.FinishTime = dto.FinishTime;
        result.FinishPosition = dto.FinishPosition;
        result.TotalFinishers = dto.TotalFinishers;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            result.ResultId,
            result.EnrolmentId,
            result.FinishTime,
            result.FinishPosition,
            result.TotalFinishers
        });
    }

    /// <summary>Lists all results for one of your events (Organiser only).</summary>
    /// <response code="200">List of results.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("events/{eventId}/results")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> GetEventResults(int eventId)
    {
        var ev = await _context.Events.FindAsync(eventId);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != HttpContext.Session.GetUserId())
            return StatusCode(403, new { message = "You can only view results for your own events." });

        var results = await _context.Results
            .Where(r => r.Enrolment!.EventId == eventId)
            .OrderBy(r => r.FinishPosition)
            .Select(r => new
            {
                r.ResultId,
                r.EnrolmentId,
                ParticipantName = r.Enrolment!.Participant!.Profile!.FullName,
                CategoryName = r.Enrolment.Category!.Name,
                r.FinishTime,
                r.FinishPosition,
                r.TotalFinishers
            })
            .ToListAsync();

        return Ok(results);
    }

    /// <summary>Lists the logged-in Participant's own results (Participant only).</summary>
    /// <response code="200">List of results.</response>
    /// <response code="403">Not a Participant.</response>
    [HttpGet("results/my")]
    [SessionAuthorize("Participant")]
    public async Task<IActionResult> GetMyResults()
    {
        var participantId = HttpContext.Session.GetUserId();

        var results = await _context.Results
            .Where(r => r.Enrolment!.ParticipantId == participantId)
            .OrderByDescending(r => r.Enrolment!.Event!.EventDate)
            .Select(r => new
            {
                r.ResultId,
                EventName = r.Enrolment!.Event!.Name,
                r.Enrolment.Event.EventDate,
                CategoryName = r.Enrolment.Category!.Name,
                r.FinishTime,
                r.FinishPosition,
                r.TotalFinishers
            })
            .ToListAsync();

        return Ok(results);
    }
}