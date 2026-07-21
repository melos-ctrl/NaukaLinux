using System.Text.Json.Serialization;

namespace Backend.Models;

public class LessonBlock
{
    public int Id { get; set; }
    public int Type { get; set; }
    
    public int OrderIndex { get; set; }
    
    public string? VideoUrl { get; set; }
    public string? TextContent { get; set; }
    public string? QuestionText { get; set; }
    public string? FrontText { get; set; }
    public string? BackText { get; set; }
    
    public int LessonId { get; set; }
    [JsonIgnore]
    public Lesson? Lesson { get; set; }
    
    public List<QuizOption> Options { get; set; } = new();
}