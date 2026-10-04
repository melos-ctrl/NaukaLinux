using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class DeleteAccountDto
{
    [Required]
    public string Password { get; set; } = string.Empty;
}
