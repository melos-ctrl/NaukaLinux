namespace Backend.Models;

public class StudentAnswer
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int LessonBlockId { get; set; }
    public LessonBlock? LessonBlock { get; set; }
    public string OptionIdsJson { get; set; } = "[]";
    public string? TextAnswer { get; set; }
    public bool IsCorrect { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}