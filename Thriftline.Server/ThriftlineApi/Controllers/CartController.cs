using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftlineApi.Data;
using ThriftlineApi.DTOs.Carts;
using ThriftlineApi.Models;
using ThriftlineApi.Models.Enums;

namespace ThriftlineApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ThriftlineDbContext _context;

    public CartController(ThriftlineDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<CartSummaryResponse>> GetMyCart()
    {
        var userId = GetCurrentUserId();

        var cartItems = await _context.CartItems
            .Include(ci => ci.Product)
                .ThenInclude(p => p.Images)
            .Include(ci => ci.Product)
                .ThenInclude(p => p.Store)
            .Where(ci => ci.UserId == userId)
            .OrderByDescending(ci => ci.AddedAt)
            .ToListAsync();

        return Ok(MapToSummary(cartItems));
    }

    [HttpPost]
    public async Task<ActionResult<CartSummaryResponse>> AddToCart(AddToCartRequest request)
    {
        if (request.Quantity < 1)
        {
            return BadRequest("Quantity minimal 1.");
        }

        var userId = GetCurrentUserId();

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId);
        if (product is null)
        {
            return NotFound("Produk tidak ditemukan.");
        }

        if (product.Status != ProductStatus.Available)
        {
            return BadRequest("Produk sudah tidak tersedia.");
        }

        var existingItem = await _context.CartItems
            .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.ProductId == request.ProductId);

        if (existingItem is null)
        {
            _context.CartItems.Add(new CartItem
            {
                UserId = userId,
                ProductId = request.ProductId,
                Quantity = request.Quantity
            });
        }
        else
        {
            existingItem.Quantity += request.Quantity;
        }

        await _context.SaveChangesAsync();

        return await GetMyCart();
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CartSummaryResponse>> UpdateQuantity(Guid id, UpdateCartItemRequest request)
    {
        if (request.Quantity < 1)
        {
            return BadRequest("Quantity minimal 1. Gunakan DELETE untuk menghapus item.");
        }

        var userId = GetCurrentUserId();

        var cartItem = await _context.CartItems
            .FirstOrDefaultAsync(ci => ci.Id == id && ci.UserId == userId);

        if (cartItem is null)
        {
            return NotFound();
        }

        cartItem.Quantity = request.Quantity;
        await _context.SaveChangesAsync();

        return await GetMyCart();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> RemoveFromCart(Guid id)
    {
        var userId = GetCurrentUserId();

        var cartItem = await _context.CartItems
            .FirstOrDefaultAsync(ci => ci.Id == id && ci.UserId == userId);

        if (cartItem is null)
        {
            return NotFound();
        }

        _context.CartItems.Remove(cartItem);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    private static CartSummaryResponse MapToSummary(List<CartItem> cartItems)
    {
        var items = cartItems.Select(ci => new CartItemResponse
        {
            Id = ci.Id,
            ProductId = ci.ProductId,
            ProductName = ci.Product.Name,
            ProductPrice = ci.Product.Price,
            ProductImageUrl = ci.Product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                ?? ci.Product.Images.FirstOrDefault()?.ImageUrl,
            StoreName = ci.Product.Store.Name,
            IsAvailable = ci.Product.Status == ProductStatus.Available,
            Quantity = ci.Quantity,
            Subtotal = ci.Product.Price * ci.Quantity
        }).ToList();

        return new CartSummaryResponse
        {
            Items = items,
            TotalItems = items.Sum(i => i.Quantity),
            TotalPrice = items.Where(i => i.IsAvailable).Sum(i => i.Subtotal)
        };
    }
}
