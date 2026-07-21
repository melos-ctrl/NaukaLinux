using System.Text.Json.Serialization;

namespace Backend.Models;

public class Lesson
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    
    public int CourseId { get; set; }
    [JsonIgnore]
    public Course? Course { get; set; }
    
    public List<LessonBlock> Blocks { get; set; } = new();
}