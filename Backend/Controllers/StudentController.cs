using System.Security.Claims;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using System.Text.Json;
using Backend.Service;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentController : ControllerBase
{
    private const int PointsPerCorrectAnswer = AnswerEvaluator.PointsPerCorrectAnswer;
    private const int MaxFeedbackLength = 2000;

    private readonly AppDbContext _context;

    public StudentController(AppDbContext context)
    {
        _context = context;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Czy bieżący użytkownik może pracować z kursem: autor/admin zawsze,
    /// pozostali tylko z opublikowanym kursem, na który są zapisani.
    /// </summary>
    private async Task<bool> CanAccessCourseAsync(Course course, int userId)
    {
        if (User.IsInRole("Admin") || course.MentorId == userId) return true;
        if (!course.isPublished) return false;

        return await _context.CourseEnrollments
            .AnyAsync(enrollment => enrollment.CourseId == course.Id && enrollment.UserId == userId);
    }

    // pobieranie kursów i wznawianie od ostatniego etapu
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

        if (!await CanAccessCourseAsync(course, userId))
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
        if (!await CanAccessCourseAsync(course, userId))
            return Forbid();

        // Walidacja danych wejściowych (błędy formularza => 400). Samą ocenę robi AnswerEvaluator,
        // ten sam, który przelicza odpowiedzi po edycji kursu.
        switch (block.QuestionType)
        {
            case QuestionType.MultipleChoice:
            {
                var selectedIds = answer.OptionIds?.ToHashSet();
                if (selectedIds == null || selectedIds.Count == 0)
                    return BadRequest("Wybierz co najmniej jedną odpowiedź.");
                if (selectedIds.Any(id => block.Options.All(option => option.Id != id)))
                    return BadRequest("Wybrana odpowiedź nie należy do tego pytania.");
                break;
            }
            case QuestionType.TrueFalse:
            {
                if (answer.OptionIds is not { Count: 1 })
                    return BadRequest("Wybierz jedną odpowiedź.");
                var selectedOptionId = answer.OptionIds[0];
                if (block.Options.All(option => option.Id != selectedOptionId))
                    return BadRequest("Wybrana odpowiedź nie należy do tego pytania.");
                break;
            }
            case QuestionType.TextAnswer:
            {
                if (string.IsNullOrWhiteSpace(answer.TextAnswer))
                    return BadRequest("Wpisz odpowiedź.");
                break;
            }
            default:
                return BadRequest("Ten typ pytania nie jest obsługiwany.");
        }

        var isCorrect = AnswerEvaluator.Evaluate(block, answer.OptionIds?.ToList(), answer.TextAnswer);

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

        if (!await CanAccessCourseAsync(block.Lesson.Course, userId))
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

    // podsumowanie kursu (ekran po kliknięciu "Zakończ kurs")
    [HttpGet("course/{courseId}/summary")]
    public async Task<IActionResult> GetCourseSummary(int courseId)
    {
        var userId = GetUserId();

        var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null) return NotFound("Kurs nie istnieje.");

        if (!await CanAccessCourseAsync(course, userId))
            return Forbid();

        var lessons = await _context.Lessons
            .Where(lesson => lesson.CourseId == courseId)
            .Select(lesson => lesson.OrderIndex)
            .ToListAsync();

        var questions = await _context.LessonBlocks
            .Where(block => block.Lesson != null && block.Lesson.CourseId == courseId && block.QuestionText != null)
            .Select(block => new
            {
                block.Id,
                block.QuestionText,
                block.OrderIndex,
                LessonTitle = block.Lesson!.Title,
                LessonOrderIndex = block.Lesson.OrderIndex
            })
            .ToListAsync();

        var answers = await _context.StudentAnswers
            .Where(answer => answer.UserId == userId && answer.LessonBlock!.Lesson!.CourseId == courseId)
            .ToDictionaryAsync(answer => answer.LessonBlockId, answer => answer.IsCorrect);

        var progress = await _context.UserProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.CourseId == courseId);

        var lastCompleted = progress?.LastCompletedLessonId ?? 0;
        var lastLessonOrderIndex = lessons.Count == 0 ? 0 : lessons.Max();
        var correctCount = questions.Count(question => answers.TryGetValue(question.Id, out var ok) && ok);

        var questionsToReview = questions
            .Where(question => !(answers.TryGetValue(question.Id, out var ok) && ok))
            .OrderBy(question => question.LessonOrderIndex)
            .ThenBy(question => question.OrderIndex)
            .Select(question => new
            {
                BlockId = question.Id,
                question.QuestionText,
                question.LessonTitle,
                question.LessonOrderIndex,
                Status = answers.ContainsKey(question.Id) ? "wrong" : "unanswered"
            })
            .ToList();

        return Ok(new
        {
            CourseId = course.Id,
            CourseTitle = course.Title,
            IsCompleted = lessons.Count > 0 && lastCompleted >= lastLessonOrderIndex,
            TotalLessons = lessons.Count,
            CompletedLessons = lessons.Count(orderIndex => orderIndex <= lastCompleted),
            QuestionCount = questions.Count,
            AnsweredCount = questions.Count(question => answers.ContainsKey(question.Id)),
            CorrectCount = correctCount,
            Score = correctCount * PointsPerCorrectAnswer,
            MaxScore = questions.Count * PointsPerCorrectAnswer,
            Percentage = questions.Count == 0 ? 100 : (int)Math.Round(correctCount * 100.0 / questions.Count),
            CompletedAt = progress?.UpdatedAt,
            QuestionsToReview = questionsToReview
        });
    }

    // zapis postępu studenta
    // Wynik liczy serwer na podstawie zapisanych poprawnych odpowiedzi - pole Score z żądania jest ignorowane,
    // więc nie da się "dopisać" sobie punktów ręcznie wysłanym requestem.
    [HttpPost("progress")]
    public async Task<IActionResult> SaveProgress([FromBody] SaveProgressDto dto)
    {
        var userId = GetUserId();

        var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == dto.CourseId);
        if (course == null) return NotFound("Nie znaleziono kursu.");

        if (!await CanAccessCourseAsync(course, userId))
            return Forbid();

        var lessonOrderIndexes = await _context.Lessons
            .Where(lesson => lesson.CourseId == dto.CourseId)
            .Select(lesson => lesson.OrderIndex)
            .ToListAsync();

        var maxOrderIndex = lessonOrderIndexes.Count == 0
            ? 0
            : Math.Max(lessonOrderIndexes.Max(), lessonOrderIndexes.Count);

        if (dto.CompletedLessonOrderIndex < 0 || dto.CompletedLessonOrderIndex > maxOrderIndex)
            return BadRequest("Nieprawidłowy numer lekcji.");

        var correctAnswers = await _context.StudentAnswers
            .CountAsync(answer => answer.UserId == userId
                && answer.IsCorrect
                && answer.LessonBlock!.Lesson!.CourseId == dto.CourseId);

        var progress = await _context.UserProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.CourseId == dto.CourseId);

        if (progress == null)
        {
            progress = new UserProgress
            {
                UserId = userId,
                CourseId = dto.CourseId
            };
            _context.UserProgresses.Add(progress);
        }

        progress.LastCompletedLessonId = Math.Max(progress.LastCompletedLessonId, dto.CompletedLessonOrderIndex);
        progress.Score = correctAnswers * PointsPerCorrectAnswer;
        progress.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new
        {
            progress.CourseId,
            progress.LastCompletedLessonId,
            progress.Score,
            progress.UpdatedAt
        });
    }

    // wysyłanie opinii / zgłoszenia błędu
    [HttpPost("feedback")]
    public async Task<IActionResult> SendFeedback([FromBody] CreateFeedbackDto dto)
    {
        var userId = GetUserId();
        var message = dto.Message?.Trim() ?? string.Empty;

        if (message.Length == 0)
            return BadRequest("Wiadomość nie może być pusta.");
        if (message.Length > MaxFeedbackLength)
            return BadRequest($"Wiadomość może mieć maksymalnie {MaxFeedbackLength} znaków.");

        var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == dto.CourseId);
        if (course == null) return NotFound("Nie znaleziono kursu.");

        if (!await CanAccessCourseAsync(course, userId))
            return Forbid();

        if (dto.LessonBlockId.HasValue)
        {
            var blockBelongsToCourse = await _context.LessonBlocks
                .AnyAsync(block => block.Id == dto.LessonBlockId.Value && block.Lesson!.CourseId == dto.CourseId);

            if (!blockBelongsToCourse)
                return BadRequest("To zadanie nie należy do tego kursu.");
        }

        var feedback = new Feedback
        {
            UserId = userId,
            CourseId = dto.CourseId,
            LessonBlockId = dto.LessonBlockId,
            Message = message,
            CreatedAt = DateTime.UtcNow
        };

        _context.Feedbacks.Add(feedback);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Zgłoszenie zostało zapisane." });
    }
}