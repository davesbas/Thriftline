using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftlineApi.Data;
using ThriftlineApi.DTOs.Orders;
using ThriftlineApi.Models;
using ThriftlineApi.Models.Enums;
using ThriftlineApi.Services;

namespace ThriftlineApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly ThriftlineDbContext _context;

    private static readonly Dictionary<OrderStatus, OrderStatus> AllowedNextStatus = new()
    {
        [OrderStatus.Paid] = OrderStatus.Processing,
        [OrderStatus.Processing] = OrderStatus.Shipped,
        [OrderStatus.Shipped] = OrderStatus.Completed
    };

    private static readonly Dictionary<string, decimal> CourierRates = new()
    {
        ["JNE Reguler"] = 15000m,
        ["J&T Express"] = 13000m,
        ["SiCepat"] = 12000m
    };

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
                .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p.Images)
            .Where(o => o.BuyerId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var reviewedOrderItemIds = (await _context.ProductReviews
            .Where(r => r.UserId == userId)
            .Select(r => r.OrderItemId)
            .ToListAsync())
            .ToHashSet();

        var result = orders.Select(o => new OrderSummaryResponse
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Status = o.Status.ToString(),
            TotalAmount = o.TotalAmount,
            ItemCount = o.OrderItems.Sum(oi => oi.Quantity),
            CreatedAt = o.CreatedAt,
            Items = o.OrderItems.Select(oi => new OrderItemResponse
            {
                OrderItemId = oi.Id,
                ProductId = oi.ProductId,
                ProductName = oi.Product.Name,
                ProductImageUrl = oi.Product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                    ?? oi.Product.Images.FirstOrDefault()?.ImageUrl,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                Subtotal = oi.UnitPrice * oi.Quantity,
                IsReviewed = reviewedOrderItemIds.Contains(oi.Id)
            }).ToList()
        }).ToList();

        return Ok(result);
    }

    [HttpGet("store")]
    public async Task<ActionResult<List<StoreOrderSummaryResponse>>> GetStoreOrders()
    {
        var userId = GetCurrentUserId();

        var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);
        if (store is null)
        {
            return Ok(new List<StoreOrderSummaryResponse>());
        }

        var orders = await _context.Orders
            .Include(o => o.Buyer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Images)
            .Where(o => o.OrderItems.Any(oi => oi.Product.StoreId == store.Id))
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var result = orders.Select(o => new StoreOrderSummaryResponse
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Status = o.Status.ToString(),
            TotalAmount = o.TotalAmount,
            ShippingAddress = o.ShippingAddress,
            ShippingCourier = o.ShippingCourier,
            Note = o.Note,
            BuyerName = o.Buyer.FullName,
            CreatedAt = o.CreatedAt,
            Items = o.OrderItems
                .Where(oi => oi.Product.StoreId == store.Id)
                .Select(oi => new OrderItemResponse
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product.Name,
                    ProductImageUrl = oi.Product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                        ?? oi.Product.Images.FirstOrDefault()?.ImageUrl,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    Subtotal = oi.UnitPrice * oi.Quantity
                }).ToList()
        }).ToList();

        return Ok(result);
    }

    [HttpGet("couriers")]
    public ActionResult<List<CourierOptionResponse>> GetCouriers()
    {
        var result = CourierRates
            .Select(kv => new CourierOptionResponse { Name = kv.Key, Cost = kv.Value })
            .ToList();

        return Ok(result);
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userId = GetCurrentUserId();

        var order = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        if (order.BuyerId != userId)
        {
            return Forbid();
        }

        if (order.Status != OrderStatus.Pending)
        {
            return BadRequest("Hanya pesanan yang belum dibayar yang bisa dibatalkan.");
        }

        order.Status = OrderStatus.Cancelled;

        foreach (var item in order.OrderItems)
        {
            item.Product.Stock += item.Quantity;
            if (item.Product.Status == ProductStatus.Reserved)
            {
                item.Product.Status = ProductStatus.Available;
            }
        }

        var storeIds = order.OrderItems.Select(oi => oi.Product.StoreId).Distinct().ToList();
        var storeOwnerIds = await _context.Stores
            .Where(s => storeIds.Contains(s.Id))
            .Select(s => s.OwnerId)
            .ToListAsync();

        foreach (var ownerId in storeOwnerIds)
        {
            NotificationHelper.QueueNotification(
                _context,
                ownerId,
                NotificationType.Order,
                "Pesanan dibatalkan",
                $"Pesanan {order.OrderNumber} dibatalkan oleh pembeli.",
                "Order",
                order.Id);
        }
        await _context.SaveChangesAsync();

        return NoContent();
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

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateOrderStatusRequest request)
    {
        var userId = GetCurrentUserId();

        var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);
        if (store is null)
        {
            return Forbid();
        }

        var order = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        var belongsToStore = order.OrderItems.Any(oi => oi.Product.StoreId == store.Id);
        if (!belongsToStore)
        {
            return Forbid();
        }

        if (!Enum.TryParse<OrderStatus>(request.Status, out var newStatus))
        {
            return BadRequest("Status tidak valid.");
        }

        if (!AllowedNextStatus.TryGetValue(order.Status, out var expectedNext) || expectedNext != newStatus)
        {
            return BadRequest($"Tidak bisa mengubah status dari {order.Status} ke {newStatus}.");
        }

        order.Status = newStatus;

        NotificationHelper.QueueNotification(
            _context,
            order.BuyerId,
            NotificationType.Order,
            "Status pesanan diperbarui",
            $"Pesanan {order.OrderNumber} sekarang berstatus {newStatus}.",
            "Order",
            order.Id);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<List<OrderDetailResponse>>> Checkout(CheckoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ShippingAddress))
        {
            return BadRequest("Alamat pengiriman wajib diisi.");
        }

        if (request.CartItemIds is null || request.CartItemIds.Count == 0)
        {
            return BadRequest("Pilih minimal satu produk untuk checkout.");
        }

        var userId = GetCurrentUserId();

        var cartItems = await _context.CartItems
            .Include(ci => ci.Product)
                .ThenInclude(p => p.Images)
            .Include(ci => ci.Product)
                .ThenInclude(p => p.Store)
            .Where(ci => ci.UserId == userId && request.CartItemIds.Contains(ci.Id))
            .ToListAsync();

        if (cartItems.Count == 0)
        {
            return BadRequest("Cart kosong atau item yang dipilih tidak ditemukan.");
        }

        foreach (var item in cartItems)
        {
            if (item.Product.Store.OwnerId == userId)
            {
                return BadRequest($"Produk '{item.Product.Name}' adalah milik Anda sendiri dan tidak dapat dibeli.");
            }

            if (item.Product.Status != ProductStatus.Available || item.Product.Stock < item.Quantity)
            {
                return BadRequest($"Produk '{item.Product.Name}' sudah tidak tersedia dalam jumlah yang diminta.");
            }
        }

        var groups = cartItems.GroupBy(ci => ci.Product.StoreId).ToList();
        var orders = new List<Order>();

        foreach (var group in groups)
        {
            var storeOption = request.StoreOptions.FirstOrDefault(o => o.StoreId == group.Key);
            if (storeOption is null)
            {
                return BadRequest("Opsi pengiriman untuk salah satu toko belum dipilih.");
            }

            if (!CourierRates.TryGetValue(storeOption.ShippingCourier, out var shippingCost))
            {
                return BadRequest("Kurir pengiriman tidak valid.");
            }

            orders.Add(BuildOrder(
                userId,
                request.ShippingAddress,
                storeOption.ShippingCourier,
                shippingCost,
                storeOption.Note,
                group.Select(ci => (ci.Product, ci.Quantity))));
        }

        _context.Orders.AddRange(orders);
        _context.CartItems.RemoveRange(cartItems);

        await _context.SaveChangesAsync();

        return Ok(orders.Select(MapToDetail).ToList());
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

        if (!CourierRates.TryGetValue(request.ShippingCourier, out var shippingCost))
        {
            return BadRequest("Kurir Pengiriman tidak valid.");
        }

        var userId = GetCurrentUserId();

        var product = await _context.Products
            .Include(p => p.Images)
            .Include(p => p.Store)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId);

        if (product is null)
        {
            return NotFound("Produk tidak ditemukan.");
        }

        if (product.Store.OwnerId == userId)
        {
            return BadRequest("Tidak bisa membeli produk dari toko sendiri.");
        }

        if (product.Status != ProductStatus.Available || product.Stock < request.Quantity)
        {
            return BadRequest("Produk sudah tidak tersedia dalam jumlah yang diminta.");
        }

        var order = BuildOrder(userId, request.ShippingAddress, request.ShippingCourier, shippingCost, null, new[] { (product, request.Quantity) });

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, MapToDetail(order));
    }

    private static Order BuildOrder(Guid buyerId, string shippingAddress, string shippingCourier, decimal shippingCost, string? note, IEnumerable<(Product Product, int Quantity)> lines)
    {
        var orderItems = new List<OrderItem>();
        decimal itemsTotal = 0;

        foreach (var (product, quantity) in lines)
        {
            orderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = quantity,
                UnitPrice = product.Price
            });

            itemsTotal += product.Price * quantity;

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
            TotalAmount = itemsTotal + shippingCost,
            ShippingAddress = shippingAddress,
            ShippingCourier = shippingCourier,
            ShippingCost = shippingCost,
            Note = note,
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
            ShippingCourier = order.ShippingCourier,
            ShippingCost = order.ShippingCost,
            Note = order.Note,
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
