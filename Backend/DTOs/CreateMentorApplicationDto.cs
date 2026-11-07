using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class CreateMentorApplicationDto
{
    [Required, StringLength(80, MinimumLength = 1)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80, MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;

    [Required, StringLength(4000, MinimumLength = 30)]
    public string Experience { get; set; } = string.Empty;

    [StringLength(2048)]
    public string? PortfolioUrl { get; set; }
}