namespace Backend.Models;

public class Feedback
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    
    public int CourseId { get; set; }
    
    public int? LessonBlockId { get; set; } 
    
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}