using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftlineApi.Data;
using ThriftlineApi.DTOs.Products;
using ThriftlineApi.DTOs.Wishlist;
using ThriftlineApi.Models;
using ThriftlineApi.Models.Enums;

namespace ThriftlineApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly ThriftlineDbContext _context;

    public WishlistController(ThriftlineDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductSummaryResponse>>> GetMyWishlist()
    {
        var userId = GetCurrentUserId();

        var wishlists = await _context.Wishlists
            .Include(w => w.Product).ThenInclude(p => p.Images)
            .Include(w => w.Product).ThenInclude(p => p.Store)
            .Include(w => w.Product).ThenInclude(p => p.Category)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();

        var items = wishlists.Select(w => new ProductSummaryResponse
        {
            Id = w.Product.Id,
            Name = w.Product.Name,
            Price = w.Product.Price,
            Condition = w.Product.Condition,
            Status = w.Product.Status,
            PrimaryImageUrl = w.Product.Images.FirstOrDefault(i => i.IsPrimary && i.MediaType == MediaType.Image)?.ImageUrl
                ?? w.Product.Images.FirstOrDefault(i => i.MediaType == MediaType.Image)?.ImageUrl,
            StoreName = w.Product.Store.Name,
            CategoryName = w.Product.Category.Name,
            IsWishlisted = true
        }).ToList();

        return Ok(items);
    }

    [HttpPost("{productId}")]
    public async Task<ActionResult<ToggleWishlistResponse>> Toggle(Guid productId)
    {
        var userId = GetCurrentUserId();

        var productExists = await _context.Products.AnyAsync(p => p.Id == productId);
        if (!productExists)
        {
            return NotFound();
        }

        var existing = await _context.Wishlists
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

        bool wishlisted;
        if (existing is not null)
        {
            _context.Wishlists.Remove(existing);
            wishlisted = false;
        }
        else
        {
            _context.Wishlists.Add(new Wishlist { UserId = userId, ProductId = productId });
            wishlisted = true;
        }

        await _context.SaveChangesAsync();

        return Ok(new ToggleWishlistResponse { Wishlisted = wishlisted });
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }
}
