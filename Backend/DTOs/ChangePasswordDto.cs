using System.ComponentModel.DataAnnotations;
using Backend.Validation;

namespace Backend.DTOs;

public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; } = null!;

    [Required]
    [PasswordPolicy]
    public string NewPassword { get; set; } = null!;
}