using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftlineApi.Data;
using ThriftlineApi.DTOs.Reviews;
using ThriftlineApi.Models;
using ThriftlineApi.Models.Enums;

namespace ThriftlineApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewController : ControllerBase
{
    private readonly ThriftlineDbContext _context;

    public ReviewController(ThriftlineDbContext context)
    {
        _context = context;
    }

    [HttpGet("reviewable")]
    [Authorize]
    public async Task<ActionResult<List<ReviewableItemResponse>>> GetReviewable()
    {
        var userId = GetCurrentUserId();

        var items = await _context.OrderItems
            .Include(oi => oi.Order)
            .Include(oi => oi.Product)
                .ThenInclude(p => p.Images)
            .Where(oi => oi.Order.BuyerId == userId && oi.Order.Status == OrderStatus.Completed)
            .ToListAsync();

        var reviewedItemIds = (await _context.ProductReviews
            .Where(r => r.UserId == userId)
            .Select(r => r.OrderItemId)
            .ToListAsync())
            .ToHashSet();

        var result = items.Select(oi => new ReviewableItemResponse
        {
            OrderItemId = oi.Id,
            ProductId = oi.ProductId,
            ProductName = oi.Product.Name,
            ProductImageUrl = oi.Product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                ?? oi.Product.Images.FirstOrDefault()?.ImageUrl,
            IsReviewed = reviewedItemIds.Contains(oi.Id)
        }).ToList();

        return Ok(result);
    }

    [HttpGet("mine/{orderItemId}")]
    [Authorize]
    public async Task<ActionResult<ReviewResponse>> GetMineByOrderItem(Guid orderItemId)
    {
        var userId = GetCurrentUserId();

        var review = await _context.ProductReviews
            .Include(r => r.User)
            .Include(r => r.Product)
                .ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(r => r.OrderItemId == orderItemId);

        if (review is null)
        {
            return NotFound("Ulasan tidak ditemukan.");
        }

        if (review.UserId != userId)
        {
            return Forbid();
        }

        var imageUrl = review.Product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
            ?? review.Product.Images.FirstOrDefault()?.ImageUrl;

        return Ok(MapToResponse(review, review.Product.Name, imageUrl));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ReviewResponse>> Create(CreateReviewRequest request)
    {
        if (request.Rating < 1 || request.Rating > 5)
        {
            return BadRequest("Rating harus antara 1 sampai 5.");
        }

        var userId = GetCurrentUserId();

        var orderItem = await _context.OrderItems
            .Include(oi => oi.Order)
            .Include(oi => oi.Product)
                .ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(oi => oi.Id == request.OrderItemId);

        if (orderItem is null)
        {
            return NotFound("Item pesanan tidak ditemukan.");
        }

        if (orderItem.Order.BuyerId != userId)
        {
            return Forbid();
        }

        if (orderItem.Order.Status != OrderStatus.Completed)
        {
            return BadRequest("Pesanan harus berstatus selesai sebelum bisa diulas.");
        }

        var alreadyReviewed = await _context.ProductReviews.AnyAsync(r => r.OrderItemId == request.OrderItemId);
        if (alreadyReviewed)
        {
            return Conflict("Item ini sudah diulas.");
        }

        var review = new ProductReview
        {
            OrderItemId = orderItem.Id,
            UserId = userId,
            ProductId = orderItem.ProductId,
            Rating = request.Rating,
            Comment = request.Comment
        };

        _context.ProductReviews.Add(review);
        await _context.SaveChangesAsync();

        await _context.Entry(review).Reference(r => r.User).LoadAsync();

        var imageUrl = orderItem.Product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
            ?? orderItem.Product.Images.FirstOrDefault()?.ImageUrl;

        return Ok(MapToResponse(review, orderItem.Product.Name, imageUrl));
    }

    [HttpGet("product/{productId}")]
    public async Task<ActionResult<List<ReviewResponse>>> GetByProduct(Guid productId)
    {
        var reviews = await _context.ProductReviews
            .Include(r => r.User)
            .Include(r => r.Product)
                .ThenInclude(p => p.Images)
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(reviews.Select(r => MapToResponse(
            r,
            r.Product.Name,
            r.Product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? r.Product.Images.FirstOrDefault()?.ImageUrl
        )).ToList());
    }

    [HttpGet("store/{storeId}")]
    public async Task<ActionResult<List<ReviewResponse>>> GetByStore(Guid storeId)
    {
        var reviews = await _context.ProductReviews
            .Include(r => r.User)
            .Include(r => r.Product)
                .ThenInclude(p => p.Images)
            .Where(r => r.Product.StoreId == storeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(reviews.Select(r => MapToResponse(
            r,
            r.Product.Name,
            r.Product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? r.Product.Images.FirstOrDefault()?.ImageUrl
        )).ToList());
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    private static ReviewResponse MapToResponse(ProductReview r, string productName, string? productImageUrl)
    {
        return new ReviewResponse
        {
            Id = r.Id,
            ProductId = r.ProductId,
            ProductName = productName,
            ProductImageUrl = productImageUrl,
            AuthorId = r.UserId,
            AuthorName = r.User.FullName,
            Rating = r.Rating,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt
        };
    }
}
