using System.Security.Claims;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]

public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    
    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    private int GetAdminId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    //zarządzanie użytkownikami 
    //Wyświetlanie
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers() =>
    Ok(await _context.Users.Select(u => new {u.Id, u.Email, u.Role}).ToListAsync());

    //zmiana roli
    [HttpPut("users/{id}/role")]
    public async Task<IActionResult> ChangeRole(int id, [FromBody] string NewRole)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.Role = NewRole;
        await _context.SaveChangesAsync();
        return Ok();
    }
    //usuwanie użytkownika
    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return Ok();
    }
    // kurst i zadania AED
    [HttpGet("courses")]
    public async Task<IActionResult> GetCourses()
    {
        var courses = await _context.Courses
            .Include(c => c.Lessons.OrderBy(l => l.OrderIndex))
                .ThenInclude(l => l.Blocks.OrderBy(b => b.OrderIndex))
                    .ThenInclude(b => b.Options)
            .ToListAsync();

        return Ok(courses);
    }

    [HttpPost("courses")]
    public async Task<IActionResult> SaveCourse([FromBody] CreateCourseDto dto)
    {
        Course? course;

        if(dto.Id.HasValue && dto.Id.Value > 0)
        {
            course = await _context.Courses
                .Include(c => c.Lessons)
                .FirstOrDefaultAsync(c => c.Id == dto.Id.Value);

            if (course == null) return NotFound("Nie znaleziono kursu.");

            course.Title = dto.Title;
            course.Description = dto.Description;
            course.isPublished = dto.IsPublished;

            _context.Lessons.RemoveRange(course.Lessons);

        } else
        {
            course = new Course
            {
                Title = dto.Title,
                Description = dto.Description,
                isPublished = dto.IsPublished,
                MentorId = GetAdminId()
            };
            _context.Courses.Add(course); 
        }
        course.Lessons = dto.Lessons.Select(l => new Lesson
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
                Hint = b.Hint,
                Options = b.Options?.Select(o => new QuizOption
                {
                    OptionText = o.OptionText,
                    IsCorrect = o.IsCorrect
                }).ToList() ?? new()
            }).ToList()
        }).ToList();

        await _context.SaveChangesAsync();
        return Ok(course);
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
    // wyniki
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
                p.UpdatedAt
            })
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync();

        return Ok(results);
    }
    //zgłoszone błedy i opinie
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


}