using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class ForgotPasswordDto
{
    [Required]
    [EmailAddress(ErrorMessage = "Podaj prawidłowy adres e-mail.")]
    public string Email { get; set; } = string.Empty;
}