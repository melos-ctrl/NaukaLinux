namespace Backend.DTOs;

public class SaveProgressDto
{
    public int CourseId { get; set; }
    public int CompletedLessonOrderIndex { get; set; }
    public int Score { get; set; }
}