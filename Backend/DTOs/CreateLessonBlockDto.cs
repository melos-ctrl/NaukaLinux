namespace Backend.DTOs;

public class CreateLessonBlockDto
{
    public int Type { get; set; }
    public int OrderIndex { get; set; }
    public string? TextContent { get; set; }
    public string? QuestionText { get; set; }
    public string? FrontText { get; set; }
    public string? BackText { get; set; }
    
    public string? VideoUrl { get; set; }
    
    public List<CreateQuizOptionDto>? Options { get; set; }
}