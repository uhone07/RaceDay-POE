using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/profile")]
[SessionAuthorize] // any logged-in user
public class ProfileController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public ProfileController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>Returns the logged-in user's own profile.</summary>
    /// <response code="200">Profile returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">Profile not found.</response>
    [HttpGet]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = HttpContext.Session.GetUserId();

        var user = await _context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user?.Profile == null)
            return NotFound(new { message = "Profile not found." });

        return Ok(new
        {
            user.UserId,
            user.Email,
            user.Role,
            user.Profile.FullName,
            user.Profile.PhoneNumber,
            user.Profile.ProfilePictureUrl
        });
    }

    /// <summary>Updates the logged-in user's own profile.</summary>
    /// <response code="200">Profile updated.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">Profile not found.</response>
    [HttpPut]
    public async Task<IActionResult> UpdateMyProfile(UpdateProfileDto dto)
    {
        var userId = HttpContext.Session.GetUserId();

        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
            return NotFound(new { message = "Profile not found." });

        profile.FullName = dto.FullName;
        profile.PhoneNumber = dto.PhoneNumber;
        profile.ProfilePictureUrl = dto.ProfilePictureUrl;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            profile.FullName,
            profile.PhoneNumber,
            profile.ProfilePictureUrl
        });
    }
}