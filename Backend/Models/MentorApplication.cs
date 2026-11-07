namespace Backend.Models;

public class MentorApplication
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Experience { get; set; } = string.Empty;
    public string? PortfolioUrl { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}