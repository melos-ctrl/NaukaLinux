using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
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

    [HttpPost]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto dto)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId)) 
            return Unauthorized();

        var course = new Course
        {
            Title = dto.Title,
            Description = dto.Description,
            MentorId = userId,
            Lessons = dto.Lessons.Select(l => new Lesson
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
            }).ToList()
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();
        
        return Ok(new { message = "Kurs został pomyślnie zapisany!", id = course.Id });
    }
}