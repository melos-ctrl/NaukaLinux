using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class ReviewMentorApplicationDto
{
    [Required]
    [RegularExpression("^(Approved|Rejected)$")]
    public string Status { get; set; } = string.Empty;
}