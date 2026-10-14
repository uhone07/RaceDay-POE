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
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public CategoriesController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>Lists the categories for an event.</summary>
    /// <response code="200">List of categories.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("events/{eventId}/categories")]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        if (!await _context.Events.AnyAsync(e => e.EventId == eventId))
            return NotFound(new { message = "Event not found." });

        var categories = await _context.Categories
            .Where(c => c.EventId == eventId)
            .Select(c => new { c.CategoryId, c.EventId, c.Name, c.MinAge, c.MaxAge, c.DistanceKm })
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>Adds a category to one of your events (Organiser only).</summary>
    /// <response code="201">Category created.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpPost("events/{eventId}/categories")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> CreateCategory(int eventId, CategoryDto dto)
    {
        if (dto.MinAge.HasValue && dto.MaxAge.HasValue && dto.MinAge > dto.MaxAge)
            return BadRequest(new { message = "MinAge cannot be greater than MaxAge." });

        var ev = await _context.Events.FindAsync(eventId);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != HttpContext.Session.GetUserId())
            return StatusCode(403, new { message = "You can only add categories to your own events." });

        var category = new Category
        {
            EventId = eventId,
            Name = dto.Name,
            MinAge = dto.MinAge,
            MaxAge = dto.MaxAge,
            DistanceKm = dto.DistanceKm
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return StatusCode(201, new { category.CategoryId, category.Name });
    }

    /// <summary>Updates a category on one of your events (Organiser only).</summary>
    /// <response code="200">Category updated.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Category does not exist.</response>
    [HttpPut("categories/{id}")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> UpdateCategory(int id, CategoryDto dto)
    {
        if (dto.MinAge.HasValue && dto.MaxAge.HasValue && dto.MinAge > dto.MaxAge)
            return BadRequest(new { message = "MinAge cannot be greater than MaxAge." });

        var category = await _context.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
            return NotFound(new { message = "Category not found." });

        if (category.Event!.OrganiserId != HttpContext.Session.GetUserId())
            return StatusCode(403, new { message = "You can only edit categories on your own events." });

        category.Name = dto.Name;
        category.MinAge = dto.MinAge;
        category.MaxAge = dto.MaxAge;
        category.DistanceKm = dto.DistanceKm;

        await _context.SaveChangesAsync();
        return Ok(new { category.CategoryId, category.Name });
    }

    /// <summary>Deletes a category from one of your events (Organiser only).</summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Category does not exist.</response>
    /// <response code="409">Category has enrolments and cannot be deleted.</response>
    [HttpDelete("categories/{id}")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
            return NotFound(new { message = "Category not found." });

        if (category.Event!.OrganiserId != HttpContext.Session.GetUserId())
            return StatusCode(403, new { message = "You can only delete categories on your own events." });

        if (await _context.Enrolments.AnyAsync(e => e.CategoryId == id))
            return Conflict(new { message = "Category has enrolments and cannot be deleted." });

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
