using System.ComponentModel.DataAnnotations;
using Backend.Validation;

namespace Backend.DTOs;

public class ResetPasswordDto
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [PasswordPolicy]
    public string NewPassword { get; set; } = string.Empty;
}