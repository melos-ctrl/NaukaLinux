namespace Backend.Models;

public class Course
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = false; 
    public string MentorId { get; set; } = string.Empty;
    public List<CourseModule> Modules { get; set; } = new();
}