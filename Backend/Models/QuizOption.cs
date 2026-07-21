using System.Text.Json.Serialization;

namespace Backend.Models;

public class QuizOption
{
    public int Id { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    
    public int LessonBlockId { get; set; }
    [JsonIgnore]
    public LessonBlock? LessonBlock { get; set; }
}