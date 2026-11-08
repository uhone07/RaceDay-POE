using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

// The form that carries the image file (Swagger needs it wrapped like this).
public class ImageUploadRequest
{
    public IFormFile File { get; set; } = null!;
}

[ApiController]
[Route("api/uploads")]
[SessionAuthorize] // any logged-in user unless overridden below
public class UploadsController : ControllerBase
{
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] AllowedTypes = { "image/jpeg", "image/png", "image/webp", "image/gif" };

    private readonly IBlobStorageService _blobs;

    public UploadsController(IBlobStorageService blobs)
    {
        _blobs = blobs;
    }

    /// <summary>Uploads an event banner image to Azure Blob Storage (Organiser only).</summary>
    /// <response code="200">Returns the public URL of the uploaded image.</response>
    /// <response code="400">No file, wrong file type, or file too large.</response>
    /// <response code="403">Not an Organiser.</response>
    [HttpPost("event-banner")]
    [SessionAuthorize("Organiser")]
    [Consumes("multipart/form-data")]
    public Task<IActionResult> UploadEventBanner([FromForm] ImageUploadRequest request)
        => Upload(request.File, "banners");

    /// <summary>Uploads the logged-in user's profile picture to Azure Blob Storage.</summary>
    /// <response code="200">Returns the public URL of the uploaded image.</response>
    /// <response code="400">No file, wrong file type, or file too large.</response>
    [HttpPost("profile-picture")]
    [Consumes("multipart/form-data")]
    public Task<IActionResult> UploadProfilePicture([FromForm] ImageUploadRequest request)
        => Upload(request.File, "profiles");

    private async Task<IActionResult> Upload(IFormFile? file, string folder)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file was uploaded." });

        if (file.Length > MaxBytes)
            return BadRequest(new { message = "The image must be 5 MB or smaller." });

        if (!AllowedTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Only JPG, PNG, WEBP or GIF images are allowed." });

        try
        {
            var url = await _blobs.UploadImageAsync(file, folder);
            return Ok(new { url });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503, new { message = ex.Message });
        }
    }
}