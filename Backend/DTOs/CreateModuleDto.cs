namespace Backend.DTOs;

public class CreateModuleDto
{
    public string Title { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public List<CreateLessonDto> Lessons { get; set; } = new();
}