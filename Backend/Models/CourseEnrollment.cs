using System.Text.Json.Serialization;

namespace Backend.Models;

public class CourseEnrollment
{
    public int Id { get; set; }

    public int UserId { get; set; }
    [JsonIgnore] public User? User { get; set; }

    public int CourseId { get; set; }
    [JsonIgnore] public Course? Course { get; set; }

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastOpenedAt { get; set; }
}