using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class VerifyEmailDto
{
    [Required]
    public string Token { get; set; } = string.Empty;
}