using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftlineApi.Data;
using ThriftlineApi.DTOs.Users;

namespace ThriftlineApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly ThriftlineDbContext _context;

    private static readonly Regex PhoneNumberRegex = new(@"^(\+62|62|0)8[1-9][0-9]{6,10}$");

    public UserController(ThriftlineDbContext context)
    {
        _context = context;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserProfileResponse>> GetMe()
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FirstAsync(u => u.Id == userId);

        return Ok(MapToResponse(user));
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserProfileResponse>> UpdateMe(UpdateProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest("Nama wajib diisi.");
        }

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return BadRequest("Nomor HP wajib diisi.");
        }

        if (!PhoneNumberRegex.IsMatch(request.PhoneNumber))
        {
            return BadRequest("Format nomor HP tidak valid. Gunakan format 08xxxxxxxxxx atau +628xxxxxxxxxx.");
        }

        var userId = GetCurrentUserId();
        var user = await _context.Users.FirstAsync(u => u.Id == userId);

        user.FullName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;
        user.Address = request.Address;
        user.ProfilePictureUrl = request.ProfilePictureUrl;

        await _context.SaveChangesAsync();

        return Ok(MapToResponse(user));
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    private static UserProfileResponse MapToResponse(ThriftlineApi.Models.User user)
    {
        return new UserProfileResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            ProfilePictureUrl = user.ProfilePictureUrl
        };
    }
}
