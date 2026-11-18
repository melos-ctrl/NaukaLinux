using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.DTOs;
using Backend.Validation;
using Backend.Service;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CoursesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("published")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublishedCourses()
    {
        var courses = await _context.Courses
            .Where(c => c.isPublished)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Description,
                c.MentorId,
                MentorName = c.Mentor != null ? c.Mentor.Username : "Nieznany autor"
            })
            .ToListAsync();

        return Ok(courses);
    }

    [Authorize(Roles = "Admin,Mentor")]
    [HttpPost("save")]
    public async Task<IActionResult> SaveCourse([FromBody] CreateCourseDto dto)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest(new { message = "Podaj tytuł kursu." });

        // Linki do wideo: tylko YouTube, zapisywane od razu jako adres embed
        var invalidVideoLesson = NormalizeVideoUrls(dto);
        if (invalidVideoLesson != null)
        {
            return BadRequest(new
            {
                message = $"Nieprawidłowy link do wideo w lekcji \"{invalidVideoLesson}\". Obsługiwane są tylko linki do YouTube."
            });
        }

        var modules = NormalizeModules(dto);

        // Szkic może być niekompletny, opublikowany kurs - nie
        if (dto.IsPublished)
        {
            var publishError = CourseValidator.ValidateForPublish(modules);
            if (publishError != null)
                return BadRequest(new { message = publishError });
        }

        var isAdmin = User.IsInRole("Admin");
        Course? course;

        if (dto.Id is > 0)
        {
            course = await _context.Courses
                .Include(c => c.Modules)
                .Include(c => c.Lessons)
                    .ThenInclude(l => l.Blocks)
                        .ThenInclude(b => b.Options)
                .FirstOrDefaultAsync(c => c.Id == dto.Id && (c.MentorId == userId || isAdmin));

            if (course == null) return NotFound(new { message = "Nie znaleziono kursu." });
        }
        else
        {
            course = new Course { MentorId = userId };
            _context.Courses.Add(course);
        }

        course.Title = dto.Title.Trim();
        course.Description = dto.Description?.Trim() ?? string.Empty;
        course.isPublished = dto.IsPublished;

        ApplyStructure(course, modules);

        // Zapis struktury i przeliczenie odpowiedzi muszą się udać razem albo wcale
        await using var transaction = await _context.Database.BeginTransactionAsync();
        await _context.SaveChangesAsync();
        await RecalculateAnswersAsync(course.Id);
        await transaction.CommitAsync();

        return Ok(new
        {
            message = dto.IsPublished ? "Kurs został opublikowany!" : "Zapisano wersję roboczą.",
            id = course.Id,
            isPublished = course.isPublished
        });
    }

    /// <summary>Zwraca tytuł pierwszej lekcji z błędnym linkiem albo null, gdy wszystko jest w porządku.</summary>
    private static string? NormalizeVideoUrls(CreateCourseDto dto)
    {
        var lessons = dto.Modules.SelectMany(module => module.Lessons).Concat(dto.Lessons);
        foreach (var lesson in lessons)
        {
            foreach (var block in lesson.Blocks)
            {
                if (!VideoUrlValidator.TryNormalize(block.VideoUrl, out var embedUrl))
                    return lesson.Title;

                block.VideoUrl = embedUrl;
            }
        }

        return null;
    }

    /// <summary>
    /// Ujednolica dane z kreatora: stary format (same lekcje bez modułów) zamienia na jeden moduł,
    /// przycina tytuły i usuwa puste odpowiedzi (kreator domyślnie dodaje dwie puste opcje).
    /// </summary>
    private static List<CreateModuleDto> NormalizeModules(CreateCourseDto dto)
    {
        var modules = dto.Modules.Count > 0
            ? dto.Modules
            : dto.Lessons.Count > 0
                ? new List<CreateModuleDto> { new() { Title = "Moduł 1", OrderIndex = 1, Lessons = dto.Lessons } }
                : new List<CreateModuleDto>();

        foreach (var module in modules)
        {
            module.Title = module.Title?.Trim() ?? string.Empty;
            foreach (var lesson in module.Lessons)
            {
                lesson.Title = lesson.Title?.Trim() ?? string.Empty;
                foreach (var block in lesson.Blocks)
                {
                    block.Options = (block.Options ?? new List<CreateQuizOptionDto>())
                        .Where(option => !string.IsNullOrWhiteSpace(option.OptionText))
                        .Select(option =>
                        {
                            option.OptionText = option.OptionText.Trim();
                            return option;
                        })
                        .ToList();
                }
            }
        }

        return modules;
    }

    /// <summary>
    /// Nakłada strukturę z kreatora na kurs BEZ kasowania wszystkiego:
    /// elementy z ID są aktualizowane w miejscu, nowe są dodawane, a usuwane są tylko te,
    /// których mentor faktycznie się pozbył. Dzięki temu odpowiedzi kursantów (powiązane z ID bloków
    /// i ID opcji) przetrwają poprawienie literówki w opublikowanym kursie.
    /// </summary>
    private void ApplyStructure(Course course, IReadOnlyList<CreateModuleDto> moduleDtos)
    {
        var existingModules = course.Modules.ToDictionary(module => module.Id);
        var existingLessons = course.Lessons.ToDictionary(lesson => lesson.Id);
        var existingBlocks = course.Lessons.SelectMany(lesson => lesson.Blocks).ToDictionary(block => block.Id);

        var keptModuleIds = new HashSet<int>();
        var keptLessonIds = new HashSet<int>();
        var keptBlockIds = new HashSet<int>();
        var keptOptionIds = new HashSet<int>();

        for (var moduleIndex = 0; moduleIndex < moduleDtos.Count; moduleIndex++)
        {
            var moduleDto = moduleDtos[moduleIndex];

            CourseModule module;
            if (moduleDto.Id is int moduleId && existingModules.TryGetValue(moduleId, out var existingModule))
            {
                module = existingModule;
                keptModuleIds.Add(moduleId);
            }
            else
            {
                module = new CourseModule { Course = course };
                course.Modules.Add(module);
            }

            module.Title = moduleDto.Title;
            module.Order = moduleDto.OrderIndex > 0 ? moduleDto.OrderIndex : moduleIndex + 1;

            foreach (var lessonDto in moduleDto.Lessons)
            {
                Lesson lesson;
                if (lessonDto.Id is int lessonId && existingLessons.TryGetValue(lessonId, out var existingLesson))
                {
                    lesson = existingLesson;
                    keptLessonIds.Add(lessonId);
                }
                else
                {
                    lesson = new Lesson { Course = course };
                    course.Lessons.Add(lesson);
                }

                lesson.Title = lessonDto.Title;
                lesson.OrderIndex = lessonDto.OrderIndex;
                lesson.CourseModule = module;

                foreach (var blockDto in lessonDto.Blocks)
                {
                    LessonBlock block;
                    var isExistingBlock = blockDto.Id is int blockId && existingBlocks.TryGetValue(blockId, out _);
                    if (isExistingBlock)
                    {
                        block = existingBlocks[blockDto.Id!.Value];
                        keptBlockIds.Add(block.Id);
                        block.Lesson = lesson; // blok mógł zostać przeniesiony do innej lekcji
                    }
                    else
                    {
                        block = new LessonBlock();
                        lesson.Blocks.Add(block);
                    }

                    block.Type = blockDto.Type;
                    block.OrderIndex = blockDto.OrderIndex;
                    block.TextContent = blockDto.TextContent;
                    block.QuestionText = blockDto.QuestionText;
                    block.QuestionType = blockDto.QuestionType;
                    block.FrontText = blockDto.FrontText;
                    block.BackText = blockDto.BackText;
                    block.VideoUrl = blockDto.VideoUrl;
                    block.Hint = blockDto.Hint;

                    var existingOptions = isExistingBlock
                        ? block.Options.ToDictionary(option => option.Id)
                        : new Dictionary<int, QuizOption>();

                    foreach (var optionDto in blockDto.Options ?? new List<CreateQuizOptionDto>())
                    {
                        QuizOption option;
                        if (optionDto.Id is int optionId && existingOptions.TryGetValue(optionId, out var existingOption))
                        {
                            option = existingOption;
                            keptOptionIds.Add(optionId);
                        }
                        else
                        {
                            option = new QuizOption();
                            block.Options.Add(option);
                        }

                        option.OptionText = optionDto.OptionText;
                        option.IsCorrect = optionDto.IsCorrect;
                    }
                }
            }
        }

        // Przypisania rodziców muszą zostać wykryte, zanim zaczniemy usuwać - inaczej kaskada
        // mogłaby skasować blok przeniesiony do innej lekcji razem z jego starą lekcją.
        _context.ChangeTracker.DetectChanges();

        var removedOptions = existingBlocks.Values
            .Where(block => keptBlockIds.Contains(block.Id))
            .SelectMany(block => block.Options)
            .Where(option => option.Id != 0 && !keptOptionIds.Contains(option.Id))
            .ToList();
        _context.QuizOptions.RemoveRange(removedOptions);

        // Usunięcie bloku kasuje też odpowiedzi kursantów na to pytanie - i tak ma być, pytania już nie ma.
        _context.LessonBlocks.RemoveRange(existingBlocks.Values.Where(block => !keptBlockIds.Contains(block.Id)));
        _context.Lessons.RemoveRange(existingLessons.Values.Where(lesson => !keptLessonIds.Contains(lesson.Id)));
        _context.CourseModules.RemoveRange(existingModules.Values.Where(module => !keptModuleIds.Contains(module.Id)));
    }

    /// <summary>
    /// Po edycji mentor mógł zmienić poprawną odpowiedź - przeliczamy zapisane odpowiedzi kursantów
    /// i ich wyniki, żeby statystyki i podsumowania zgadzały się z aktualną treścią kursu.
    /// </summary>
    private async Task RecalculateAnswersAsync(int courseId)
    {
        var blocks = await _context.LessonBlocks
            .Include(block => block.Options)
            .Where(block => block.Lesson != null && block.Lesson.CourseId == courseId)
            .ToDictionaryAsync(block => block.Id);

        if (blocks.Count == 0) return;

        var blockIds = blocks.Keys.ToList();
        var answers = await _context.StudentAnswers
            .Where(answer => blockIds.Contains(answer.LessonBlockId))
            .ToListAsync();

        foreach (var answer in answers)
        {
            var block = blocks[answer.LessonBlockId];
            var optionIds = JsonSerializer.Deserialize<List<int>>(answer.OptionIdsJson) ?? new List<int>();
            answer.IsCorrect = AnswerEvaluator.IsQuestion(block)
                && AnswerEvaluator.Evaluate(block, optionIds, answer.TextAnswer);
        }

        var correctByUser = answers
            .Where(answer => answer.IsCorrect)
            .GroupBy(answer => answer.UserId)
            .ToDictionary(group => group.Key, group => group.Count());

        var progresses = await _context.UserProgresses
            .Where(progress => progress.CourseId == courseId)
            .ToListAsync();

        foreach (var progress in progresses)
        {
            progress.Score = correctByUser.GetValueOrDefault(progress.UserId) * AnswerEvaluator.PointsPerCorrectAnswer;
        }

        await _context.SaveChangesAsync();
    }

    [HttpPost("{courseId}/enroll")]
    public async Task<IActionResult> EnrollInCourse(int courseId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            return Unauthorized();

        var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null) return NotFound("Nie znaleziono kursu.");

        // Na nieopublikowany kurs może "zapisać się" tylko jego autor albo admin (np. żeby go przetestować)
        if (!course.isPublished && course.MentorId != userId && !User.IsInRole("Admin"))
            return NotFound("Nie znaleziono kursu.");

        var existingEnrollment = await _context.CourseEnrollments
            .FirstOrDefaultAsync(e => e.CourseId == courseId && e.UserId == userId);

        if (existingEnrollment == null)
        {
            _context.CourseEnrollments.Add(new CourseEnrollment
            {
                CourseId = courseId,
                UserId = userId
            });
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "Zapisano na kurs pomyślnie!" });
    }

    [HttpGet("{courseId}/content")]
    public async Task<IActionResult> GetCourseContent(int courseId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            return Unauthorized();

        var course = await _context.Courses
            .Include(c => c.Lessons)
            .ThenInclude(l => l.Blocks)
            .ThenInclude(b => b.Options)
            .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
                    .ThenInclude(l => l.Blocks)
                        .ThenInclude(b => b.Options)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course == null) return NotFound("Nie znaleziono kursu.");

        var isAdmin = User.IsInRole("Admin");
        var isOwner = course.MentorId == userId;
        var enrollment = await _context.CourseEnrollments
            .FirstOrDefaultAsync(e => e.CourseId == courseId && e.UserId == userId);
        var isEnrolled = enrollment != null;

        // Uwaga: Forbid("tekst") traktuje tekst jako nazwę schematu uwierzytelniania i kończy się błędem 500,
        // dlatego komunikat zwracamy przez StatusCode(403, ...)
        if (!isOwner && !isAdmin && (!isEnrolled || !course.isPublished))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                "Nie masz dostępu do tego kursu. Musisz się najpierw zapisać.");
        }

        if (enrollment != null)
        {
            enrollment.LastOpenedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return Ok(CourseContentDto.FromCourse(course, isOwner || isAdmin));
    }

    [HttpGet("my-courses")]
    [Authorize(Roles = "Admin,Mentor")]
    public async Task<IActionResult> GetMyCourses()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdString == null) return Unauthorized();

        int userId = int.Parse(userIdString);

        var isAdmin = User.IsInRole("Admin");
        var courses = await _context.Courses
            .Where(c => isAdmin || c.MentorId == userId)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Description,
                IsPublished = c.isPublished,
                c.MentorId,
                MentorName = c.Mentor != null ? c.Mentor.Username : "Nieznany autor"
            })
            .ToListAsync();

        return Ok(courses);
    }

    [HttpGet("{courseId:int}/statistics")]
    [Authorize(Roles = "Admin,Mentor")]
    public async Task<IActionResult> GetCourseStatistics(int courseId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdString == null || !int.TryParse(userIdString, out var userId)) return Unauthorized();

        var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null) return NotFound("Nie znaleziono kursu.");
        if (!User.IsInRole("Admin") && course.MentorId != userId) return Forbid();

        var blocks = await _context.LessonBlocks
            .Where(block => block.Lesson != null && block.Lesson.CourseId == courseId && block.QuestionText != null)
            .Include(block => block.Lesson)
            .Include(block => block.Options)
            .OrderBy(block => block.Lesson!.OrderIndex)
            .ThenBy(block => block.OrderIndex)
            .ToListAsync();

        var blockIds = blocks.Select(block => block.Id).ToList();
        var answers = await _context.StudentAnswers
            .Where(answer => blockIds.Contains(answer.LessonBlockId))
            .Include(answer => answer.User)
            .ToListAsync();

        var questions = blocks.Select(block =>
        {
            var blockAnswers = answers.Where(answer => answer.LessonBlockId == block.Id).ToList();
            var decodedAnswers = blockAnswers.Select(answer => new
            {
                Answer = answer,
                OptionIds = JsonSerializer.Deserialize<List<int>>(answer.OptionIdsJson) ?? new List<int>()
            }).ToList();
            var respondentCount = blockAnswers.Select(answer => answer.UserId).Distinct().Count();

            var options = block.Options.OrderBy(option => option.Id).Select(option =>
            {
                var selected = decodedAnswers
                    .Where(item => item.OptionIds.Contains(option.Id))
                    .GroupBy(item => item.Answer.UserId)
                    .Select(group => group.First().Answer)
                    .ToList();

                return new
                {
                    OptionId = option.Id,
                    OptionText = option.OptionText,
                    SelectionCount = selected.Count,
                    Percentage = respondentCount == 0 ? 0 : Math.Round(selected.Count * 100d / respondentCount, 1),
                    Students = selected
                        .Select(answer => answer.User?.Username ?? "Usunięty użytkownik")
                        .OrderBy(username => username)
                        .ToList()
                };
            }).ToList();

            var textAnswers = decodedAnswers
                .Where(item => !string.IsNullOrWhiteSpace(item.Answer.TextAnswer))
                .GroupBy(item => item.Answer.TextAnswer!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var selected = group
                        .GroupBy(item => item.Answer.UserId)
                        .Select(userAnswers => userAnswers.First().Answer)
                        .ToList();

                    return new
                    {
                        AnswerText = group.First().Answer.TextAnswer!.Trim(),
                        SelectionCount = selected.Count,
                        Percentage = respondentCount == 0 ? 0 : Math.Round(selected.Count * 100d / respondentCount, 1),
                        Students = selected
                            .Select(answer => answer.User?.Username ?? "Usunięty użytkownik")
                            .OrderBy(username => username)
                            .ToList()
                    };
                })
                .OrderByDescending(answer => answer.SelectionCount)
                .ToList();

            return new
            {
                BlockId = block.Id,
                LessonTitle = block.Lesson?.Title ?? "Lekcja",
                QuestionText = block.QuestionText,
                QuestionType = block.QuestionType.ToString(),
                ResponseCount = respondentCount,
                Options = options,
                TextAnswers = textAnswers
            };
        }).ToList();

        return Ok(new { CourseId = course.Id, CourseTitle = course.Title, Questions = questions });
    }

    // dodatkowe endpointy dla mentora i admina

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Mentor")]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdString == null) return Unauthorized();
        int userId = int.Parse(userIdString);

        var course = await _context.Courses.FindAsync(id);
        if (course == null) return NotFound();

        if (!User.IsInRole("Admin") && course.MentorId != userId)
        {
            return Forbid();
        }

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("mentor-results")]
    [Authorize(Roles = "Admin,Mentor")]
    public async Task<IActionResult> GetMentorResults()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdString == null) return Unauthorized();
        int userId = int.Parse(userIdString);

        var isAdmin = User.IsInRole("Admin");
        var query = _context.UserProgresses
            .Include(p => p.User)
            .Include(p => p.Course)
            .AsQueryable();

        if (!isAdmin)
        {
            query = query.Where(p => p.Course != null && p.Course.MentorId == userId);
        }

        var results = await query
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new
            {
                p.Id,
                Username = p.User != null ? p.User.Username : "Usunięty",
                CourseTitle = p.Course != null ? p.Course.Title : "Usunięty",
                p.Score,
                p.LastCompletedLessonId,
                // LastCompletedLessonId to tak naprawdę OrderIndex lekcji (np. 101) - zamieniamy go na tytuł
                CompletedLessonTitle = _context.Lessons
                    .Where(lesson => lesson.CourseId == p.CourseId && lesson.OrderIndex == p.LastCompletedLessonId)
                    .Select(lesson => lesson.Title)
                    .FirstOrDefault(),
                CompletedModuleTitle = _context.Lessons
                    .Where(lesson => lesson.CourseId == p.CourseId && lesson.OrderIndex == p.LastCompletedLessonId)
                    .Select(lesson => lesson.CourseModule != null ? lesson.CourseModule.Title : null)
                    .FirstOrDefault(),
                // Postęp: ile lekcji kursu ma OrderIndex <= ostatnio ukończonej
                TotalLessons = _context.Lessons.Count(lesson => lesson.CourseId == p.CourseId),
                CompletedLessons = _context.Lessons.Count(lesson =>
                    lesson.CourseId == p.CourseId && lesson.OrderIndex <= p.LastCompletedLessonId),
                // Maksymalny wynik: każde pytanie w kursie jest warte 10 pkt (tak jak w StudentController)
                MaxScore = _context.LessonBlocks.Count(block =>
                    block.Lesson != null && block.Lesson.CourseId == p.CourseId && block.QuestionText != null) * 10,
                p.UpdatedAt
            })
            .ToListAsync();

        return Ok(results);
    }

    [HttpGet("mentor-feedbacks")]
    [Authorize(Roles = "Admin,Mentor")]
    public async Task<IActionResult> GetMentorFeedbacks()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdString == null) return Unauthorized();
        int userId = int.Parse(userIdString);

        var isAdmin = User.IsInRole("Admin");
        var mentorCourseIds = await _context.Courses
            .Where(c => isAdmin || c.MentorId == userId)
            .Select(c => c.Id)
            .ToListAsync();

        var feedbacks = await _context.Feedbacks
            .Include(f => f.User)
            .Where(f => mentorCourseIds.Contains(f.CourseId))
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
}