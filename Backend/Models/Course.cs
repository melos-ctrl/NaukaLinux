using System.Text.Json.Serialization;

namespace Backend.Models;

public class Course
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MentorId { get; set; }
    
    [JsonIgnore]
    public User? Mentor { get; set; }
    
    public List<Lesson> Lessons { get; set; } = new();
}