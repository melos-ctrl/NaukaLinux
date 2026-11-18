using Backend.DTOs;
using Backend.Models;
using Backend.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private const string ForgotPasswordResponse =
        "Jeżeli podany adres email istnieje w naszej bazie danych, wysłaliśmy do Ciebie link do zresetowania hasła.";

    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AppDbContext context, IConfiguration configuration, IEmailService emailService, ILogger<AuthController> logger)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
        _logger = logger;
    }

    private string FrontendUrl => (_configuration["FrontendUrl"] ?? "http://localhost:5173").TrimEnd('/');

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto request)
    {
        var normalizedEmail = TokenHelper.NormalizeEmail(request.Email);
        var username = request.Username.Trim();

        var userExists = await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower());
        if (userExists)
        {
            return BadRequest("Ta nazwa użytkownika jest już zajęta.");
        }

        var emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail);
        if (emailExists)
        {
            return BadRequest("Konto z tym adresem e-mail już istnieje.");
        }

        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var verificationToken = TokenHelper.Generate();

        var newUser = new User
        {
            Email = normalizedEmail,
            Username = username,
            PasswordHash = hashedPassword,
            IsEmailVerified = false,
            EmailVerificationTokenHash = TokenHelper.Hash(verificationToken),
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24)
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        var verificationLink = $"{FrontendUrl}/verify-email?token={Uri.EscapeDataString(verificationToken)}";
        try
        {
            await _emailService.SendEmailVerificationEmailAsync(newUser.Email, newUser.Username, verificationLink);
        }
        catch (Resend.ResendException exception)
        {
            _logger.LogError(exception, "Failed to send verification email for user {UserId}", newUser.Id);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                Code = "verification_email_failed",
                AccountCreated = true,
                Message = "Konto utworzono, ale nie udało się wysłać maila. Sprawdź konfigurację poczty i spróbuj ponownie."
            });
        }

        return Ok(new { Message = "Konto utworzone. Sprawdź skrzynkę i potwierdź adres e-mail." });
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto request)
    {
        var tokenHash = TokenHelper.Hash(request.Token);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmailVerificationTokenHash == tokenHash);

        if (user == null || user.EmailVerificationTokenExpiry <= DateTime.UtcNow)
        {
            return BadRequest("Link weryfikacyjny jest nieprawidłowy lub wygasł.");
        }

        user.IsEmailVerified = true;
        user.EmailVerificationTokenHash = null;
        user.EmailVerificationTokenExpiry = null;
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Adres e-mail został potwierdzony. Możesz się zalogować." });
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationDto request)
    {
        var normalizedEmail = TokenHelper.NormalizeEmail(request.Email);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
        const string responseMessage = "Jeżeli konto wymaga potwierdzenia, wysłaliśmy nowy link.";

        if (user == null || user.IsEmailVerified)
        {
            return Ok(new { Message = responseMessage });
        }

        var verificationToken = TokenHelper.Generate();
        user.EmailVerificationTokenHash = TokenHelper.Hash(verificationToken);
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        await _context.SaveChangesAsync();

        var verificationLink = $"{FrontendUrl}/verify-email?token={Uri.EscapeDataString(verificationToken)}";
        try
        {
            await _emailService.SendEmailVerificationEmailAsync(user.Email, user.Username, verificationLink);
        }
        catch (Resend.ResendException exception)
        {
            _logger.LogError(exception, "Failed to resend verification email for user {UserId}", user.Id);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                Code = "verification_email_failed",
                Message = "Nie udało się wysłać maila. Sprawdź konfigurację poczty i spróbuj ponownie."
            });
        }

        return Ok(new { Message = responseMessage });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        const string invalidCredentials = "Nieprawidłowa nazwa użytkownika lub hasło.";

        var username = request.Username.Trim();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            return BadRequest(invalidCredentials);
        }

        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!isPasswordValid)
        {
            return BadRequest(invalidCredentials);
        }

        if (!user.IsEmailVerified)
        {
            return BadRequest(new
            {
                Code = "email_not_verified",
                Message = "Potwierdź adres e-mail przed zalogowaniem."
            });
        }

        var token = CreateToken(user);
        return Ok(new { Message = "Login successful!", Token = token, Role = user.Role });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request)
    {
        var normalizedEmail = TokenHelper.NormalizeEmail(request.Email);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        // Ta sama odpowiedź niezależnie od tego, czy konto istnieje - nie zdradzamy, kto ma konto.
        if (user == null) return Ok(new { Message = ForgotPasswordResponse });

        var resetToken = TokenHelper.Generate();
        user.ResetToken = TokenHelper.Hash(resetToken); // w bazie tylko hash
        user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
        await _context.SaveChangesAsync();

        var resetLink = $"{FrontendUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}";

        try
        {
            await _emailService.SendForgotPasswordEmailAsync(user.Email, resetLink);
        }
        catch (Exception exception)
        {
            // Nie zwracamy błędu użytkownikowi (to zdradziłoby, że konto istnieje), ale logujemy problem.
            _logger.LogError(exception, "Failed to send password reset email for user {UserId}", user.Id);
        }

        return Ok(new { Message = ForgotPasswordResponse });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
    {
        var tokenHash = TokenHelper.Hash(request.Token);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.ResetToken == tokenHash);

        if (user == null || user.ResetTokenExpiry < DateTime.UtcNow)
        {
            return BadRequest("Link do resetu hasła jest nieprawidłowy lub wygasł.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        user.ResetToken = null;
        user.ResetTokenExpiry = null;

        await _context.SaveChangesAsync();

        return Ok(new { Message = "Hasło zostało zmienione. Możesz się zalogować." });
    }

    private string CreateToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(1),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = creds
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}