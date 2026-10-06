using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Backend.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class PasswordPolicyAttribute : ValidationAttribute
{

    private const int MinimumCharactersCount = 12;
    private const int MaximumUtf8Bytes = 72;
    public PasswordPolicyAttribute()
    {
        ErrorMessage = "Hasło musi zawierać co najmniej 12 znaków, w tym jedną dużą literę, jedną małą literę i jedną cyfrę.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        
        

        if (value is not string password)
        {
            return ValidationResult.Success;
        }
        var charactersCount = 0;
        var hasUppercase = false;
        var hasLowercase = false;
        var hasDigit = false;

        foreach (var character in password.EnumerateRunes())
        {
        charactersCount++;

        hasUppercase |= Rune.IsUpper(character);
        hasLowercase |= Rune.IsLower(character);
        hasDigit |= Rune.IsDigit(character);
        }

        if (charactersCount < MinimumCharactersCount ||
        Encoding.UTF8.GetByteCount(password) > MaximumUtf8Bytes ||
        !hasUppercase ||
        !hasLowercase ||
        !hasDigit)
        {
            return new ValidationResult(ErrorMessage);
        }

        return ValidationResult.Success;
    }
}