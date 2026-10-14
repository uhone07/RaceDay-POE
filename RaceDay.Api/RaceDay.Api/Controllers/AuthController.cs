using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public AuthController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>Registers a new Organiser or Participant account.</summary>
    /// <response code="201">Account created.</response>
    /// <response code="400">Invalid data or invalid role.</response>
    /// <response code="409">Email already registered.</response>
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (dto.Role != "Organiser" && dto.Role != "Participant")
            return BadRequest(new { message = "Role must be 'Organiser' or 'Participant'." });

        var email = dto.Email.Trim().ToLower();

        if (await _context.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "Email is already registered." });

        var user = new User
        {
            Email = email,
            PasswordHash = PasswordHasher.Hash(dto.Password),
            Role = dto.Role,
            Profile = new UserProfile
            {
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber
            }
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return StatusCode(201, new { user.UserId, user.Email, user.Role });
    }

    /// <summary>Logs in and starts a session storing the user's ID and role.</summary>
    /// <response code="200">Login successful.</response>
    /// <response code="401">Wrong email or password.</response>
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLower();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !PasswordHasher.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("Role", user.Role);

        return Ok(new { user.UserId, user.Email, user.Role });
    }

    /// <summary>Ends the current session.</summary>
    /// <response code="200">Logged out.</response>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Logged out." });
    }
}