using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftlineApi.Data;
using ThriftlineApi.DTOs.Stores;
using ThriftlineApi.Models;

namespace ThriftlineApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoreController : ControllerBase
{
    private readonly ThriftlineDbContext _context;

    private static readonly Regex PhoneNumberRegex = new(@"^(\+62|62|0)8[1-9][0-9]{6,10}$");

    public StoreController(ThriftlineDbContext context)
    {
        _context = context;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StoreResponse>> GetById(Guid id)
    {
        var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == id);
        if (store is null)
        {
            return NotFound();
        }

        var (avg, count) = await GetRatingStatsAsync(store.Id);
        return Ok(MapToResponse(store, avg, count));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<StoreResponse>> GetMyStore()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);

        if (store is null)
        {
            return NotFound("Anda belum memiliki toko.");
        }

        var (avg, count) = await GetRatingStatsAsync(store.Id);
        return Ok(MapToResponse(store, avg, count));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<StoreResponse>> Create(CreateStoreRequest request)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var alreadyHasStore = await _context.Stores.AnyAsync(s => s.OwnerId == userId);
        if (alreadyHasStore)
        {
            return Conflict("Anda sudah memiliki toko.");
        }

        if (!request.AgreedToTerms)
        {
            return BadRequest("Anda harus menyetujui Syarat & Ketentuan Toko.");
        }

        var user = await _context.Users.FirstAsync(u => u.Id == userId);

        var phoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? user.PhoneNumber : request.PhoneNumber;
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return BadRequest("Nomor HP wajib diisi sebelum membuka toko.");
        }
        if (!PhoneNumberRegex.IsMatch(phoneNumber))
        {
            return BadRequest("Format nomor HP tidak valid. Gunakan format 08xxxxxxxxxx atau +628xxxxxxxxxx.");
        }
        user.PhoneNumber = phoneNumber;

        var store = new Store
        {
            OwnerId = userId,
            Name = request.Name,
            Description = request.Description,
            LogoUrl = request.LogoUrl,
            Address = request.Address
        };

        _context.Stores.Add(store);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = store.Id }, MapToResponse(store, 0, 0));
    }

    private async Task<(double Average, int Count)> GetRatingStatsAsync(Guid storeId)
    {
        var ratings = await _context.ProductReviews
            .Where(r => r.Product.StoreId == storeId)
            .Select(r => r.Rating)
            .ToListAsync();

        if (ratings.Count == 0)
        {
            return (0, 0);
        }

        return (ratings.Average(), ratings.Count);
    }

    private static StoreResponse MapToResponse(Store store, double averageRating, int reviewCount)
    {
        return new StoreResponse
        {
            Id = store.Id,
            OwnerId = store.OwnerId,
            Name = store.Name,
            Description = store.Description,
            LogoUrl = store.LogoUrl,
            Address = store.Address,
            AverageRating = averageRating,
            ReviewCount = reviewCount
        };
    }
}
