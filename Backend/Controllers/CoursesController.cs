using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.DTOs;

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
                c.MentorId
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

        var isAdmin = User.IsInRole("Admin");
        Course? course;

        if (dto.Id.HasValue && dto.Id > 0)
        {
            course = await _context.Courses
                .Include(c => c.Lessons)
                    .ThenInclude(l => l.Blocks)
                        .ThenInclude(b => b.Options)
                .FirstOrDefaultAsync(c => c.Id == dto.Id && (c.MentorId == userId || isAdmin));

            if (course == null) return NotFound();

            _context.Lessons.RemoveRange(course.Lessons);
            
            course.Title = dto.Title;
            course.Description = dto.Description;
            course.isPublished = dto.IsPublished;
            course.Lessons = MapLessons(dto);
        }
        else
        {
            course = new Course
            {
                Title = dto.Title,
                Description = dto.Description,
                MentorId = userId,
                isPublished = dto.IsPublished,
                Lessons = MapLessons(dto)
            };

            _context.Courses.Add(course);
        }

        await _context.SaveChangesAsync();
        
        return Ok(new { message = "Kurs został pomyślnie zapisany!", id = course.Id });
    }

    private List<Lesson> MapLessons(CreateCourseDto dto)
    {
        return dto.Lessons.Select(l => new Lesson
        {
            Title = l.Title,
            OrderIndex = l.OrderIndex,
            Blocks = l.Blocks.Select(b => new LessonBlock
            {
                Type = b.Type,
                OrderIndex = b.OrderIndex,
                TextContent = b.TextContent,
                QuestionText = b.QuestionText,
                FrontText = b.FrontText,
                BackText = b.BackText,
                VideoUrl = b.VideoUrl,
                Hint = b.Hint, // <-- DODANE MAPOWANIE PODPOWIEDZI
                Options = b.Options?.Select(o => new QuizOption
                {
                    OptionText = o.OptionText,
                    IsCorrect = o.IsCorrect
                }).ToList() ?? new List<QuizOption>()
            }).ToList()
        }).ToList();
    }

    [HttpPost("{courseId}/enroll")]
    public async Task<IActionResult> EnrollInCourse(int courseId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId)) 
            return Unauthorized();

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
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course == null) return NotFound("Nie znaleziono kursu.");
        
        var isEnrolled = await _context.CourseEnrollments.AnyAsync(e => e.CourseId == courseId && e.UserId == userId);
    
        if (course.MentorId != userId && !isEnrolled && !User.IsInRole("Admin"))
        {
            return Forbid("Nie masz dostępu do tego kursu. Musisz się najpierw zapisać.");
        }

        return Ok(course);
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
            .ToListAsync();

        return Ok(courses);
    }

    // dodatkowe endpointy dla mentroa i admina 

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