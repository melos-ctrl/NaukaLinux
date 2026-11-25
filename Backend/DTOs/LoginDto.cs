using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class LoginDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public string? CfToken { get; set; }
}