namespace Backend.DTOs;
using System.ComponentModel.DataAnnotations;
using Backend.Validation;
public class RegisterDto
{
    [Required]
    [EmailAddress(ErrorMessage = "Podaj prawidłowy adres e-mail.")]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Nazwa użytkownika musi mieć od 3 do 100 znaków.")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [PasswordPolicy]
    public string Password { get; set; } = string.Empty;
}