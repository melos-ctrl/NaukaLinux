using System.Text.Json.Serialization;

namespace Backend.Models;

public class Feedback
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    
    public int CourseId { get; set; }
    [JsonIgnore] public Course? Course { get; set; }

    public int? LessonBlockId { get; set; }
    [JsonIgnore] public LessonBlock? LessonBlock { get; set; }
    
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}