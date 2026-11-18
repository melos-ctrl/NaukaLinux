namespace Backend.DTOs;

public class SaveProgressDto
{
    public int CourseId { get; set; }
    public int CompletedLessonOrderIndex { get; set; }
    // score został usunięty serwer liczy sam, więc nie jest potrzebny w DTO
}