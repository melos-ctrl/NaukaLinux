using System.Security.Claims;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using System.Text.Json;

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
        var savedAnswers = await _context.StudentAnswers
            .Where(answer => answer.UserId == userId && answer.LessonBlock!.Lesson!.CourseId == courseId)
            .Select(answer => new
            {
                answer.LessonBlockId,
                answer.OptionIdsJson,
                answer.TextAnswer,
                answer.IsCorrect
            })
            .ToListAsync();

        return Ok(new
        {
            Course = CourseContentDto.FromCourse(course, includeAnswerKey: false),
            LastCompletedLessonOrderIndex = progress?.LastCompletedLessonId ?? 0,
            Score = progress?.Score ?? 0,
            Answers = savedAnswers.Select(answer => new
            {
                BlockId = answer.LessonBlockId,
                OptionIds = JsonSerializer.Deserialize<List<int>>(answer.OptionIdsJson) ?? new List<int>(),
                answer.TextAnswer,
                answer.IsCorrect
            })
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

        var savedAnswer = await _context.StudentAnswers
            .FirstOrDefaultAsync(saved => saved.UserId == userId && saved.LessonBlockId == blockId);
        if (savedAnswer == null)
        {
            savedAnswer = new StudentAnswer
            {
                UserId = userId,
                LessonBlockId = blockId
            };
            _context.StudentAnswers.Add(savedAnswer);
        }

        savedAnswer.OptionIdsJson = JsonSerializer.Serialize(answer.OptionIds ?? new List<int>());
        savedAnswer.TextAnswer = answer.TextAnswer;
        savedAnswer.IsCorrect = isCorrect;
        savedAnswer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { IsCorrect = isCorrect });
    }

    [HttpDelete("answer/{blockId}")]
    public async Task<IActionResult> DeleteAnswer(int blockId)
    {
        var userId = GetUserId();
        var block = await _context.LessonBlocks
            .Include(lessonBlock => lessonBlock.Lesson)
                .ThenInclude(lesson => lesson!.Course)
            .FirstOrDefaultAsync(lessonBlock => lessonBlock.Id == blockId);

        if (block?.Lesson?.Course == null) return NotFound("Nie znaleziono pytania.");

        var course = block.Lesson.Course;
        var isAdmin = User.IsInRole("Admin");
        var isOwner = course.MentorId == userId;
        var isEnrolled = await _context.CourseEnrollments
            .AnyAsync(enrollment => enrollment.CourseId == course.Id && enrollment.UserId == userId);

        if ((!course.isPublished && !isOwner && !isAdmin) || (!isOwner && !isEnrolled && !isAdmin))
            return Forbid();

        var savedAnswer = await _context.StudentAnswers
            .FirstOrDefaultAsync(answer => answer.UserId == userId && answer.LessonBlockId == blockId);
        if (savedAnswer != null)
        {
            _context.StudentAnswers.Remove(savedAnswer);
            await _context.SaveChangesAsync();
        }

        return NoContent();
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
                progress.Score += dto.Score;
            }
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