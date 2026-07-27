// Controllers/AuthController.cs
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
namespace Backend.Controllers;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
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