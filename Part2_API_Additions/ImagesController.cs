// Add to RaceDay.API/Controllers (Part 2).
//
// Assumed names, adjust to match your Part 2 code:
//   AppDbContext  with DbSet<Event> Events and DbSet<User> Users
//   Event.BannerImageUrl (string?), Event.OrganiserId (int)
//   User.ProfileImageUrl (string?)
// After adding the two nullable columns run:
//   dotnet ef migrations add AddImageUrls --project RaceDay.API
//   dotnet ef database update --project RaceDay.API
// Also return bannerImageUrl on event responses and profileImageUrl on GET /api/users/me.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Data;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[ApiController]
public class ImagesController : ControllerBase
{
    private static readonly string[] AllowedTypes = { "image/jpeg", "image/png", "image/webp" };
    private const long MaxBytes = 5 * 1024 * 1024;

    private readonly AppDbContext _db;
    private readonly IBlobStorageService _blobs;

    public ImagesController(AppDbContext db, IBlobStorageService blobs)
    {
        _db = db;
        _blobs = blobs;
    }

    [HttpPost("api/events/{id:int}/banner")]
    [Authorize(Roles = "Organiser")]
    [RequestSizeLimit(MaxBytes + 1024 * 1024)]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file, CancellationToken ct)
    {
        var problem = Validate(file);
        if (problem is not null) return BadRequest(new { message = problem });

        var ev = await _db.Events.FindAsync(new object[] { id }, ct);
        if (ev is null) return NotFound(new { message = "Event not found." });

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || ev.OrganiserId != userId)
            return Forbid();

        var url = await _blobs.UploadImageAsync(file, "banners", ct);
        ev.BannerImageUrl = url;
        await _db.SaveChangesAsync(ct);

        return Ok(new { bannerImageUrl = url });
    }

    [HttpPost("api/users/me/picture")]
    [Authorize]
    [RequestSizeLimit(MaxBytes + 1024 * 1024)]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file, CancellationToken ct)
    {
        var problem = Validate(file);
        if (problem is not null) return BadRequest(new { message = problem });

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var user = await _db.Users.FindAsync(new object[] { userId }, ct);
        if (user is null) return NotFound(new { message = "User not found." });

        var url = await _blobs.UploadImageAsync(file, "profiles", ct);
        user.ProfileImageUrl = url;
        await _db.SaveChangesAsync(ct);

        return Ok(new { profileImageUrl = url });
    }

    private static string? Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0) return "Choose an image to upload.";
        if (!AllowedTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            return "Upload a JPG, PNG or WebP image.";
        if (file.Length > MaxBytes) return "The image is larger than 5 MB.";
        return null;
    }
}
