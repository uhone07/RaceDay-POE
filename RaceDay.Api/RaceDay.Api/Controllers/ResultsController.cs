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

    /// <summary>Captures or updates the result for an enrolment on one of your events (Organiser only).</summary>
    /// <response code="200">Result saved.</response>
    /// <response code="400">Position is greater than total finishers.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Enrolment does not exist.</response>
    [HttpPut("enrolments/{enrolmentId}/result")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> SaveResult(int enrolmentId, ResultDto dto)
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

        if (enrolment.Result == null)
        {
            enrolment.Result = new Result
            {
                FinishTime = dto.FinishTime,
                FinishPosition = dto.FinishPosition,
                TotalFinishers = dto.TotalFinishers
            };
        }
        else
        {
            enrolment.Result.FinishTime = dto.FinishTime;
            enrolment.Result.FinishPosition = dto.FinishPosition;
            enrolment.Result.TotalFinishers = dto.TotalFinishers;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            enrolment.Result.ResultId,
            enrolment.EnrolmentId,
            enrolment.Result.FinishTime,
            enrolment.Result.FinishPosition,
            enrolment.Result.TotalFinishers
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
    [HttpGet("results/mine")]
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