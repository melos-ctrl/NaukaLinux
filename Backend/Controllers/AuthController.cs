using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
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

    public AuthController(AppDbContext context, IConfiguration configuration, IEmailService emailService)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService; // Przypisujemy wstrzyknięty serwis
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto request)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Username == request.Username);
        if (userExists)
        {
            return BadRequest("User Exists.");
        }

        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var newUser = new User
        {   
            Email = request.Email,
            Username = request.Username,
            PasswordHash = hashedPassword
        };
        
        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        _ = _emailService.SendWelcomeEmailAsync(newUser.Email, newUser.Username);

        return Ok(new { Message = "Register successful!" });
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
            new Claim(ClaimTypes.Name, user.Username)
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
}