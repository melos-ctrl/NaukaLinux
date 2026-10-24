namespace Backend.DTOs;

public class CreateFeedbackDto
{
    public int? LessonBlockId { get; set; } 
    
    public int CourseId { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? Rating { get; set; }
}