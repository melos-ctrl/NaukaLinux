namespace Backend.DTOs;

public class CreateLessonDto
{
    public string Title { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public List<CreateLessonBlockDto> Blocks { get; set; } = new();
}