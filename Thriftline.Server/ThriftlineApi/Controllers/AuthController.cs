using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ThriftlineApi.Data;
using ThriftlineApi.DTOs.Auth;
using ThriftlineApi.Models;

namespace ThriftlineApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ThriftlineDbContext _context;
    private readonly IConfiguration _configuration;

    // Nomor Indonesia: diawali 08, 8, atau +62/62 lalu 8, total 9-12 digit setelah kode awal.
    private static readonly Regex PhoneNumberRegex = new(@"^(\+62|62|0)8[1-9][0-9]{6,10}$");

    public AuthController(ThriftlineDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return Conflict("Email sudah digunakan.");
        }

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return BadRequest("Nomor HP wajib diisi.");
        }

        if (!PhoneNumberRegex.IsMatch(request.PhoneNumber))
        {
            return BadRequest("Format nomor HP tidak valid. Gunakan format 08xxxxxxxxxx atau +628xxxxxxxxxx.");
        }

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            PhoneNumber = request.PhoneNumber
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var (token, expiresAt) = GenerateToken(user);

        return StatusCode(201, new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            ExpiresAt = expiresAt
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized("Email atau password salah.");
        }

        var (token, expiresAt) = GenerateToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            ExpiresAt = expiresAt
        });
    }

    private (string Token, DateTime ExpiresAt) GenerateToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName)
        };

        var expiresAt = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiryMinutes"]!));

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
