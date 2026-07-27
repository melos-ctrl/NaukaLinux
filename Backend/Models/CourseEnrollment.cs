namespace Backend.Models;

public class CourseEnrollment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CourseId { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
}