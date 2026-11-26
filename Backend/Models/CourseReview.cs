using System.Text.Json.Serialization;

namespace Backend.Models;

public class CourseReview
{
    public const int MinRating = 1;
    public const int MaxRating = 5;
    public const int MaxCommentLength = 1000;

    public int Id { get; set; }

    public int UserId { get; set; }
    [JsonIgnore]
    public User? User { get; set; }

    public int CourseId { get; set; }
    [JsonIgnore]
    public Course? Course { get; set; }

    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}