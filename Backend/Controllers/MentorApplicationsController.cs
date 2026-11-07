using System.Security.Claims;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/mentor-applications")]
[Authorize]
public class MentorApplicationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public MentorApplicationsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyApplication(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var application = await _context.MentorApplications
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.SubmittedAt)
            .Select(item => new
            {
                item.FirstName,
                item.LastName,
                item.Experience,
                item.PortfolioUrl,
                item.Status,
                item.SubmittedAt,
                item.ReviewedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (application is null) return NoContent();

        var canResubmitAt = application.Status == "Rejected"
            ? (application.ReviewedAt ?? application.SubmittedAt).AddDays(14)
            : (DateTime?)null;

        return Ok(new
        {
            application.FirstName,
            application.LastName,
            application.Experience,
            application.PortfolioUrl,
            application.Status,
            application.SubmittedAt,
            application.ReviewedAt,
            CanResubmitAt = canResubmitAt,
            CanResubmit = canResubmitAt.HasValue && DateTime.UtcNow >= canResubmitAt.Value
        });
    }

    [HttpPost]
    public async Task<IActionResult> Submit(
        [FromBody] CreateMentorApplicationDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var user = await _context.Users.FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null) return Unauthorized();
        if (user.Role.Equals("Mentor", StringComparison.OrdinalIgnoreCase) ||
            user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            return Conflict("Twoje konto ma już uprawnienia mentora.");

        var latestApplication = await _context.MentorApplications
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.SubmittedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (latestApplication?.Status == "Pending")
            return Conflict("Masz już oczekujące zgłoszenie.");

        if (latestApplication?.Status == "Rejected")
        {
            var canResubmitAt = (latestApplication.ReviewedAt ?? latestApplication.SubmittedAt).AddDays(14);
            if (DateTime.UtcNow < canResubmitAt)
                return Conflict($"Kolejne zgłoszenie możesz wysłać od {canResubmitAt:O}.");
        }

        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        var experience = request.Experience.Trim();
        var portfolioUrl = request.PortfolioUrl?.Trim();

        if (firstName.Length == 0 || lastName.Length == 0 || experience.Length < 30)
            return BadRequest("Uzupełnij imię, nazwisko i opisz doświadczenie (co najmniej 30 znaków).");

        if (!string.IsNullOrEmpty(portfolioUrl) &&
            (!Uri.TryCreate(portfolioUrl, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)))
            return BadRequest("Link do portfolio musi być prawidłowym adresem HTTP lub HTTPS.");

        var application = new MentorApplication
        {
            UserId = userId,
            FirstName = firstName,
            LastName = lastName,
            Experience = experience,
            PortfolioUrl = string.IsNullOrEmpty(portfolioUrl) ? null : portfolioUrl,
            Status = "Pending",
            SubmittedAt = DateTime.UtcNow
        };

        _context.MentorApplications.Add(application);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            application.FirstName,
            application.LastName,
            application.Experience,
            application.PortfolioUrl,
            application.Status,
            application.SubmittedAt,
            application.ReviewedAt,
            CanResubmitAt = (DateTime?)null,
            CanResubmit = false
        });
    }

    private bool TryGetUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}