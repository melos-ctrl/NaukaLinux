namespace Backend.DTOs;

public class CreateCourseDto
{
    public int? Id { get; set; } 
    public bool IsPublished { get; set; } 
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<CreateModuleDto> Modules { get; set; } = new();
    public List<CreateLessonDto> Lessons { get; set; } = new();
}