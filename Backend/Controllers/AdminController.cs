using System.Security.Claims;
using Backend.Data;
using Backend.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private static readonly string[] AllowedRoles = ["User", "Mentor", "Admin"];

    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    private int GetAdminId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // --- zarządzanie użytkownikami ---

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers() =>
        Ok(await _context.Users
            .OrderBy(u => u.Id)
            .Select(u => new { u.Id, u.Username, u.Email, u.Role })
            .ToListAsync());

    [HttpPut("users/{id}/role")]
    public async Task<IActionResult> ChangeRole(int id, [FromBody] string newRole)
    {
        var role = AllowedRoles.FirstOrDefault(r => r.Equals(newRole?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (role == null)
            return BadRequest($"Nieznana rola. Dozwolone: {string.Join(", ", AllowedRoles)}.");

        if (id == GetAdminId() && role != "Admin")
            return Conflict("Nie możesz odebrać uprawnień administratora samemu sobie.");

        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.Role = role;
        await _context.SaveChangesAsync();
        return Ok(new { user.Id, user.Role });
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        if (id == GetAdminId())
            return Conflict("Nie możesz usunąć własnego konta z panelu administratora.");

        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return Ok();
    }

    // --- kursy ---
    // Tworzenie i edycja kursów idzie przez CoursesController (POST /api/courses/save), który obsługuje też rolę Admin.

    [HttpGet("courses")]
    public async Task<IActionResult> GetCourses()
    {
        var courses = await _context.Courses
            .OrderBy(c => c.Id)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Description,
                c.MentorId,
                MentorName = c.Mentor != null ? c.Mentor.Username : "Nieznany autor",
                IsPublished = c.isPublished,
                LessonCount = c.Lessons.Count
            })
            .ToListAsync();

        return Ok(courses);
    }

    [HttpDelete("courses/{id}")]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null) return NotFound();

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();
        return Ok();
    }

    // --- wyniki ---
    [HttpGet("results")]
    public async Task<IActionResult> GetResults()
    {
        var results = await _context.UserProgresses
            .Include(p => p.User)
            .Include(p => p.Course)
            .Select(p => new
            {
                p.Id,
                Username = p.User != null ? p.User.Username : "Usunięty",
                Email = p.User != null ? p.User.Email : "-",
                CourseTitle = p.Course != null ? p.Course.Title : "Usunięty",
                p.Score,
                p.LastCompletedLessonId,
                CompletedLessonTitle = _context.Lessons
                    .Where(lesson => lesson.CourseId == p.CourseId && lesson.OrderIndex == p.LastCompletedLessonId)
                    .Select(lesson => lesson.Title)
                    .FirstOrDefault(),
                CompletedModuleTitle = _context.Lessons
                    .Where(lesson => lesson.CourseId == p.CourseId && lesson.OrderIndex == p.LastCompletedLessonId)
                    .Select(lesson => lesson.CourseModule != null ? lesson.CourseModule.Title : null)
                    .FirstOrDefault(),
                p.UpdatedAt
            })
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync();

        return Ok(results);
    }

    // --- zgłoszone błędy i opinie ---
    [HttpGet("feedbacks")]
    public async Task<IActionResult> GetFeedbacks()
    {
        var feedbacks = await _context.Feedbacks
            .Include(f => f.User)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new
            {
                f.Id,
                Username = f.User != null ? f.User.Username : "Anonim",
                f.CourseId,
                f.LessonBlockId,
                Type = f.LessonBlockId.HasValue ? "Błąd w zadaniu" : "Opinia",
                f.Message,
                f.CreatedAt
            })
            .ToListAsync();

        return Ok(feedbacks);
    }

    [HttpGet("mentor-applications")]
    public async Task<IActionResult> GetMentorApplications(CancellationToken cancellationToken)
    {
        var applications = await _context.MentorApplications
            .OrderByDescending(application => application.SubmittedAt)
            .Select(application => new
            {
                application.Id,
                application.UserId,
                Username = application.User != null ? application.User.Username : "Usunięty użytkownik",
                Email = application.User != null ? application.User.Email : "-",
                application.FirstName,
                application.LastName,
                application.Experience,
                application.PortfolioUrl,
                application.Status,
                application.SubmittedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(applications);
    }

    [HttpPut("mentor-applications/{id:int}/review")]
    public async Task<IActionResult> ReviewMentorApplication(
        int id,
        [FromBody] ReviewMentorApplicationDto request,
        CancellationToken cancellationToken)
    {
        var application = await _context.MentorApplications
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (application is null) return NotFound("Nie znaleziono zgłoszenia.");
        if (application.Status != "Pending")
            return Conflict("To zgłoszenie zostało już rozpatrzone.");
        if (application.User is null) return Conflict("Konto użytkownika nie istnieje.");

        application.Status = request.Status;
        application.ReviewedAt = DateTime.UtcNow;
        if (request.Status == "Approved")
            application.User.Role = "Mentor";

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { application.Id, application.Status, UserRole = application.User.Role });
    }
}