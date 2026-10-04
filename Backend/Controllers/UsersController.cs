using System.Security.Claims;
using Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using Backend.DTOs;

namespace Backend.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private const long MaxAvatarBytes = 5 * 1024 * 1024;

    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        AppDbContext context,
        IWebHostEnvironment environment,
        ILogger<UsersController> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var user = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Username,
                u.Email,
                u.AvatarUrl
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return NotFound();

        return Ok(user);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateCurrentUser(
        [FromBody] UpdateAccountDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var username = request.Username.Trim();
        var email = request.Email.Trim();

        if (username.Length < 3 || string.IsNullOrWhiteSpace(email))
            return BadRequest("Podaj prawidłową nazwę użytkownika i adres e-mail.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return NotFound();

        var duplicateUsername = await _context.Users.AnyAsync(
            u => u.Id != userId && u.Username.ToLower() == username.ToLower(),
            cancellationToken);

        if (duplicateUsername)
            return Conflict("Ta nazwa użytkownika jest już zajęta.");

        var duplicateEmail = await _context.Users.AnyAsync(
            u => u.Id != userId && u.Email.ToLower() == email.ToLower(),
            cancellationToken);

        if (duplicateEmail)
            return Conflict("Ten adres e-mail jest już używany.");

        user.Username = username;
        user.Email = email;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { user.Username, user.Email });
    }

    [HttpPost("me/avatar")]
    [RequestSizeLimit(MaxAvatarBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxAvatarBytes)]
    public async Task<IActionResult> UploadAvatar(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        if (file is null || file.Length == 0)
            return BadRequest("Nie wybrano pliku.");

        if (file.Length > MaxAvatarBytes)
            return BadRequest("Plik może mieć maksymalnie 5 MB.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return NotFound();

        var webRoot = _environment.WebRootPath
            ?? Path.Combine(_environment.ContentRootPath, "wwwroot");

        var avatarsDirectory = Path.Combine(webRoot, "avatars");
        Directory.CreateDirectory(avatarsDirectory);

        var fileName = $"{Guid.NewGuid():N}.webp";
        var filePath = Path.Combine(avatarsDirectory, fileName);

        using var image = await Image.LoadAsync(file.OpenReadStream(), cancellationToken);

        image.Mutate(operation => operation.Resize(new ResizeOptions
        {
            Size = new Size(512, 512),
            Mode = ResizeMode.Crop,
            Position = AnchorPositionMode.Center
        }));

        await image.SaveAsWebpAsync(
            filePath,
            new WebpEncoder { Quality = 82 },
            cancellationToken);

        var previousAvatarUrl = user.AvatarUrl;
        user.AvatarUrl = $"/avatars/{fileName}";

        await _context.SaveChangesAsync(cancellationToken);

        DeletePreviousAvatar(previousAvatarUrl, avatarsDirectory, filePath);

        return Ok(new { avatarUrl = user.AvatarUrl });
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out userId);
    }

    private void DeletePreviousAvatar(
        string? previousAvatarUrl,
        string avatarsDirectory,
        string newFilePath)
    {
        if (previousAvatarUrl is null ||
            !previousAvatarUrl.StartsWith("/avatars/", StringComparison.Ordinal))
        {
            return;
        }

        var previousFilePath = Path.Combine(
            avatarsDirectory,
            Path.GetFileName(previousAvatarUrl));

        if (previousFilePath == newFilePath || !System.IO.File.Exists(previousFilePath))
            return;

        try
        {
            System.IO.File.Delete(previousFilePath);
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "Nie udało się usunąć poprzedniego avatara.");
        }
    }

    [HttpGet("me/courses")]
    public async Task<IActionResult> GetMyEnrolledCourses(
     CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var courses = await _context.CourseEnrollments
            .Where(enrollment => enrollment.UserId == userId)
            .Join(
                _context.Courses,
                enrollment => enrollment.CourseId,
                course => course.Id,
                (enrollment, course) => new
                {
                    courseId = course.Id,
                    title = course.Title,
                    description = course.Description
                })
            .ToListAsync(cancellationToken);

        return Ok(courses);
    }
    [HttpPost("me/password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordDto changePasswordDto,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.PasswordHash))
            return BadRequest("Nieprawidłowe aktualne hasło.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Hasło zostało zmienione." });
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteCurrentUser(
        [FromBody] DeleteAccountDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return BadRequest("Nieprawidłowe hasło.");

        var ownsCourses = await _context.Courses
            .AnyAsync(course => course.MentorId == userId, cancellationToken);

        if (ownsCourses)
        {
            return Conflict(
                "Nie można usunąć konta mentora, który ma utworzone kursy. Usuń najpierw swoje kursy.");
        }

        var avatarUrl = user.AvatarUrl;
        var webRoot = _environment.WebRootPath
            ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var avatarsDirectory = Path.Combine(webRoot, "avatars");

        var enrollments = await _context.CourseEnrollments
            .Where(enrollment => enrollment.UserId == userId)
            .ToListAsync(cancellationToken);

        _context.CourseEnrollments.RemoveRange(enrollments);
        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);

        DeletePreviousAvatar(avatarUrl, avatarsDirectory, string.Empty);

        return NoContent();
    }
}