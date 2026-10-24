using System.Security.Claims;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentController : ControllerBase
{
    private readonly AppDbContext _context;

    public StudentController(AppDbContext context)
    {
        _context = context;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // pobiernie kursów i wznawianie od ostatniego etapu
    [HttpGet("course/{courseId}")]
    public async Task<IActionResult> GetCourseWithProgress(int courseId)
    {
        var userId = GetUserId();

        var course = await _context.Courses
            .Include(c => c.Lessons.OrderBy(l => l.OrderIndex))
                .ThenInclude(l => l.Blocks.OrderBy(b => b.OrderIndex))
                    .ThenInclude(b => b.Options)
            .FirstOrDefaultAsync(c => c.Id == courseId && c.isPublished);

        if (course == null) return NotFound("Kurs nie istnieje lub nie jest opublikowany.");

        var progress = await _context.UserProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.CourseId == courseId);

        return Ok(new
        {
            Course = course,
            LastCompletedLessonOrderIndex = progress?.LastCompletedLessonId ?? 0,
            Score = progress?.Score ?? 0
        });
    }

    // 2. sprawdzanie poprawnosci zadania
    [HttpPost("verify-block/{blockId}")]
    public async Task<IActionResult> VerifyTask(int blockId, [FromBody] string userAnswer)
    {
        var block = await _context.LessonBlocks
            .Include(b => b.Options)
            .FirstOrDefaultAsync(b => b.Id == blockId);

        if (block == null) return NotFound("Nie znaleziono zadania.");

        bool isCorrect = block.Options.Any(o =>
            o.IsCorrect && o.OptionText.Trim().Equals(userAnswer.Trim(), StringComparison.OrdinalIgnoreCase));

        return Ok(new { IsCorrect = isCorrect });
    }

    // zapis postępu studenta
    [HttpPost("progress")]
    public async Task<IActionResult> SaveProgress([FromBody] SaveProgressDto dto)
    {
        var userId = GetUserId();
        var progress = await _context.UserProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.CourseId == dto.CourseId);

        if (progress == null)
        {
            progress = new UserProgress
            {
                UserId = userId,
                CourseId = dto.CourseId,
                LastCompletedLessonId = dto.CompletedLessonOrderIndex,
                Score = dto.Score
            };
            _context.UserProgresses.Add(progress);
        }
        else
        {
            if (dto.CompletedLessonOrderIndex > progress.LastCompletedLessonId)
            {
                progress.LastCompletedLessonId = dto.CompletedLessonOrderIndex;
            }
            progress.Score += dto.Score;
            progress.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(progress);
    }

    // wysyłanie opini feedback co nie 
    [HttpPost("feedback")]
    public async Task<IActionResult> SendFeedback([FromBody] CreateFeedbackDto dto)
    {
        var feedback = new Feedback
        {
            UserId = GetUserId(),
            CourseId = dto.CourseId,
            LessonBlockId = dto.LessonBlockId,
            Message = dto.Message,
            CreatedAt = DateTime.UtcNow
        };

        _context.Feedbacks.Add(feedback);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Zgłoszenie zostało zapisane." });
    }
}