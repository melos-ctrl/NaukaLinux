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
            .Include(c => c.Modules.OrderBy(m => m.Order))
                .ThenInclude(m => m.Lessons.OrderBy(l => l.OrderIndex))
                    .ThenInclude(l => l.Blocks.OrderBy(b => b.OrderIndex))
                        .ThenInclude(b => b.Options)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course == null) return NotFound("Kurs nie istnieje.");

        var isAdmin = User.IsInRole("Admin");
        var isOwner = course.MentorId == userId;
        var isEnrolled = await _context.CourseEnrollments
            .AnyAsync(enrollment => enrollment.CourseId == courseId && enrollment.UserId == userId);

        if ((!course.isPublished && !isOwner && !isAdmin) || (!isOwner && !isEnrolled && !isAdmin))
            return Forbid();

        var progress = await _context.UserProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.CourseId == courseId);

        return Ok(new
        {
            Course = CourseContentDto.FromCourse(course, includeAnswerKey: false),
            LastCompletedLessonOrderIndex = progress?.LastCompletedLessonId ?? 0,
            Score = progress?.Score ?? 0
        });
    }

    [HttpPost("verify-block/{blockId}")]
    public async Task<IActionResult> VerifyTask(int blockId, [FromBody] VerifyAnswerDto answer)
    {
        var block = await _context.LessonBlocks
            .Include(b => b.Options)
            .Include(b => b.Lesson)
                .ThenInclude(lesson => lesson!.Course)
            .FirstOrDefaultAsync(b => b.Id == blockId);

        if (block == null) return NotFound("Nie znaleziono zadania.");

        var course = block.Lesson?.Course;
        if (course == null) return NotFound("Nie znaleziono kursu dla zadania.");

        var userId = GetUserId();
        var isAdmin = User.IsInRole("Admin");
        var isOwner = course.MentorId == userId;
        var isEnrolled = await _context.CourseEnrollments
            .AnyAsync(enrollment => enrollment.CourseId == course.Id && enrollment.UserId == userId);

        if ((!course.isPublished && !isOwner && !isAdmin) || (!isOwner && !isEnrolled && !isAdmin))
            return Forbid();

        bool isCorrect;
        switch (block.QuestionType)
        {
            case QuestionType.MultipleChoice:
            {
                var selectedIds = answer.OptionIds?.ToHashSet();
                if (selectedIds == null || selectedIds.Count == 0)
                    return BadRequest("Wybierz co najmniej jedną odpowiedź.");
                if (selectedIds.Any(id => block.Options.All(option => option.Id != id)))
                    return BadRequest("Wybrana odpowiedź nie należy do tego pytania.");

                var correctIds = block.Options.Where(option => option.IsCorrect).Select(option => option.Id).ToHashSet();
                isCorrect = correctIds.Count > 0 && selectedIds.SetEquals(correctIds);
                break;
            }
            case QuestionType.TrueFalse:
            {
                if (answer.OptionIds is not { Count: 1 })
                    return BadRequest("Wybierz jedną odpowiedź.");

                var selectedOption = block.Options.FirstOrDefault(option => option.Id == answer.OptionIds[0]);
                if (selectedOption == null)
                    return BadRequest("Wybrana odpowiedź nie należy do tego pytania.");
                isCorrect = selectedOption.IsCorrect;
                break;
            }
            case QuestionType.TextAnswer:
            {
                if (string.IsNullOrWhiteSpace(answer.TextAnswer))
                    return BadRequest("Wpisz odpowiedź.");

                isCorrect = block.Options.Any(option => option.IsCorrect &&
                    string.Equals(option.OptionText.Trim(), answer.TextAnswer.Trim(), StringComparison.OrdinalIgnoreCase));
                break;
            }
            default:
                return BadRequest("Ten typ pytania nie jest obsługiwany.");
        }

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