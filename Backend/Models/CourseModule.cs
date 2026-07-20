namespace Backend.Models;

public class CourseModule
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    
    public int CourseId { get; set; }
    public Course? Course { get; set; }
    
    public List<Lesson> Lessons { get; set; } = new(); 
}