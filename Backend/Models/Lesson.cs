namespace Backend.Models;

public class Lesson
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    
    public string? TextContent { get; set; } 
    
    public string? VideoUrl { get; set; } 
    public string? TaskDescription { get; set; }
    
    public int CourseModuleId { get; set; }
    public CourseModule? CourseModule { get; set; }
    
    public List<ControlQuestion> ControlQuestions { get; set; } = new();
}