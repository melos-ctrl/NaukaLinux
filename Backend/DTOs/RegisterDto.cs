namespace Backend.DTOs;
using System.ComponentModel.DataAnnotations;
using Backend.Validation;
public class RegisterDto
{
    [Required]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [PasswordPolicy]
    public string Password { get; set; } = string.Empty;
}