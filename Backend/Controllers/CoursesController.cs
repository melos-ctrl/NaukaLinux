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

    [HttpPost("save")]
    public async Task<IActionResult> SaveCourse([FromBody] CreateCourseDto dto)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId)) 
            return Unauthorized();

        Course course;

        if (dto.Id.HasValue && dto.Id > 0)
        {
            course = await _context.Courses
                .Include(c => c.Lessons)
                    .ThenInclude(l => l.Blocks)
                        .ThenInclude(b => b.Options)
                .FirstOrDefaultAsync(c => c.Id == dto.Id && c.MentorId == userId);

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
    
        if (course.MentorId != userId && !isEnrolled)
        {
            return Forbid("Nie masz dostępu do tego kursu. Musisz się najpierw zapisać.");
        }

        return Ok(course);
    }
    
    [HttpGet("my-courses")]
    [Authorize]
    public async Task<IActionResult> GetMyCourses()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdString == null) return Unauthorized();

        int userId = int.Parse(userIdString);
        
        var courses = await _context.Courses
            .Where(c => c.MentorId == userId) 
            .ToListAsync();

        return Ok(courses);
    }
}