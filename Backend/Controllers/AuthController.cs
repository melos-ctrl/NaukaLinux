using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AppDbContext context, IConfiguration configuration, IEmailService emailService, ILogger<AuthController> logger)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService; // Przypisujemy wstrzyknięty serwis
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var userExists = await _context.Users.AnyAsync(u => u.Username == request.Username);
        if (userExists)
        {
            return BadRequest("User Exists.");
        }

        var emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail);
        if (emailExists)
        {
            return BadRequest("Konto z tym adresem e-mail już istnieje.");
        }

        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var verificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        var newUser = new User
        {   
            Email = normalizedEmail,
            Username = request.Username,
            PasswordHash = hashedPassword,
            IsEmailVerified = false,
            EmailVerificationTokenHash = HashToken(verificationToken),
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24)
        };
        
        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        var frontendUrl = (_configuration["FrontendUrl"] ?? "http://localhost:5173").TrimEnd('/');
        var verificationLink = $"{frontendUrl}/verify-email?token={Uri.EscapeDataString(verificationToken)}";
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
        var tokenHash = HashToken(request.Token);
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
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
        const string responseMessage = "Jeżeli konto wymaga potwierdzenia, wysłaliśmy nowy link.";

        if (user == null || user.IsEmailVerified)
        {
            return Ok(new { Message = responseMessage });
        }

        var verificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        user.EmailVerificationTokenHash = HashToken(verificationToken);
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        await _context.SaveChangesAsync();

        var frontendUrl = (_configuration["FrontendUrl"] ?? "http://localhost:5173").TrimEnd('/');
        var verificationLink = $"{frontendUrl}/verify-email?token={Uri.EscapeDataString(verificationToken)}";
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
        // szukanie użytkownika w bazie
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null)
        {
            return BadRequest("Invalid Email or password.");
        }
        
        // weryfikowanie hasła
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!isPasswordValid)
        {
            return BadRequest("Invalid Email or password.");
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
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
       if (user == null) return Ok(new { Message = "Jeżeli podany adres email istnieje w naszej bazie danych, wysłaliśmy do Ciebie link do zresetowania hasła." });

       user.ResetToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
       user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
       await _context.SaveChangesAsync();

        var frontendUrl = _configuration["FrontendUrl"];
        var resetLink = $"{frontendUrl}/reset-password?token={user.ResetToken}";

        _ = _emailService.SendForgotPasswordEmailAsync(user.Email, resetLink);

        return Ok(new { Message = "Jeżeli podany adres email istnieje w naszej bazie danych, wysłaliśmy do Ciebie link do zresetowania hasła." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.ResetToken == request.Token);

        if (user == null || user.ResetTokenExpiry < DateTime.UtcNow)
        {
            return BadRequest("Invalid or expired token.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        user.ResetToken = null;
        user.ResetTokenExpiry = null;

        await _context.SaveChangesAsync();

        return Ok(new { Message = "Password has been reset successfully." });
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
            Expires = DateTime.Now.AddDays(1),
            SigningCredentials = creds
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}