namespace Backend.DTOs;

public class CreateModuleDto
{
    public int? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public List<CreateLessonDto> Lessons { get; set; } = new();
}