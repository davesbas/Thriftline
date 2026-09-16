using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftlineApi.Data;
using ThriftlineApi.DTOs.Orders;
using ThriftlineApi.Models;
using ThriftlineApi.Models.Enums;

namespace ThriftlineApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly ThriftlineDbContext _context;

    public OrderController(ThriftlineDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderSummaryResponse>>> GetMyOrders()
    {
        var userId = GetCurrentUserId();

        var orders = await _context.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.BuyerId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var result = orders.Select(o => new OrderSummaryResponse
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Status = o.Status.ToString(),
            TotalAmount = o.TotalAmount,
            ItemCount = o.OrderItems.Sum(oi => oi.Quantity),
            CreatedAt = o.CreatedAt
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDetailResponse>> GetById(Guid id)
    {
        var userId = GetCurrentUserId();

        var order = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        if (order.BuyerId != userId)
        {
            return Forbid();
        }

        return Ok(MapToDetail(order));
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<OrderDetailResponse>> Checkout(CheckoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ShippingAddress))
        {
            return BadRequest("Alamat pengiriman wajib diisi.");
        }

        var userId = GetCurrentUserId();

        var cartItemsQuery = _context.CartItems
            .Include(ci => ci.Product)
                .ThenInclude(p => p.Images)
            .Where(ci => ci.UserId == userId);

        if (request.CartItemIds is { Count: > 0 })
        {
            cartItemsQuery = cartItemsQuery.Where(ci => request.CartItemIds.Contains(ci.Id));
        }

        var cartItems = await cartItemsQuery.ToListAsync();

        if (cartItems.Count == 0)
        {
            return BadRequest("Cart kosong atau item yang dipilih tidak ditemukan.");
        }

        foreach (var item in cartItems)
        {
            if (item.Product.Status != ProductStatus.Available || item.Product.Stock < item.Quantity)
            {
                return BadRequest($"Produk '{item.Product.Name}' sudah tidak tersedia dalam jumlah yang diminta.");
            }
        }

        var order = BuildOrder(userId, request.ShippingAddress, cartItems.Select(ci => (ci.Product, ci.Quantity)));

        _context.Orders.Add(order);
        _context.CartItems.RemoveRange(cartItems);

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, MapToDetail(order));
    }

    [HttpPost("buy-now")]
    public async Task<ActionResult<OrderDetailResponse>> BuyNow(BuyNowRequest request)
    {
        if (request.Quantity < 1)
        {
            return BadRequest("Quantity minimal 1.");
        }

        if (string.IsNullOrWhiteSpace(request.ShippingAddress))
        {
            return BadRequest("Alamat pengiriman wajib diisi.");
        }

        var userId = GetCurrentUserId();

        var product = await _context.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId);

        if (product is null)
        {
            return NotFound("Produk tidak ditemukan.");
        }

        if (product.Status != ProductStatus.Available || product.Stock < request.Quantity)
        {
            return BadRequest("Produk sudah tidak tersedia dalam jumlah yang diminta.");
        }

        var order = BuildOrder(userId, request.ShippingAddress, new[] { (product, request.Quantity) });

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, MapToDetail(order));
    }

    private static Order BuildOrder(Guid buyerId, string shippingAddress, IEnumerable<(Product Product, int Quantity)> lines)
    {
        var orderItems = new List<OrderItem>();
        decimal totalAmount = 0;

        foreach (var (product, quantity) in lines)
        {
            orderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = quantity,
                UnitPrice = product.Price
            });

            totalAmount += product.Price * quantity;

            product.Stock -= quantity;
            if (product.Stock <= 0)
            {
                product.Stock = 0;
                product.Status = ProductStatus.Reserved;
            }
        }

        return new Order
        {
            BuyerId = buyerId,
            OrderNumber = GenerateOrderNumber(),
            Status = OrderStatus.Pending,
            TotalAmount = totalAmount,
            ShippingAddress = shippingAddress,
            OrderItems = orderItems
        };
    }

    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    private static OrderDetailResponse MapToDetail(Order order)
    {
        return new OrderDetailResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            ShippingAddress = order.ShippingAddress,
            CreatedAt = order.CreatedAt,
            Items = order.OrderItems.Select(oi => new OrderItemResponse
            {
                ProductId = oi.ProductId,
                ProductName = oi.Product.Name,
                ProductImageUrl = oi.Product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                    ?? oi.Product.Images.FirstOrDefault()?.ImageUrl,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                Subtotal = oi.UnitPrice * oi.Quantity
            }).ToList()
        };
    }
}
