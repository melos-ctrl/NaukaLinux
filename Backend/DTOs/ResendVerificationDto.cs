using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class ResendVerificationDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}