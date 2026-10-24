namespace Backend.Models;

public class UserProgress
{
    public int Id {get; set;}
    public int UserId {get; set;}
    public User? User {get; set;}

    public int CourseId {get; set;}
    public Course? Course {get; set;}

    public int LastCompletedLessonId {get; set;}

    public int Score { get; set;}

    public DateTime UpdatedAt {get; set;} = DateTime.UtcNow;

}