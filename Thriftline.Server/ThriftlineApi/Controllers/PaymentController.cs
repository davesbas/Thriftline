using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftlineApi.Data;
using ThriftlineApi.DTOs.Payments;
using ThriftlineApi.Models;
using ThriftlineApi.Models.Enums;
using ThriftlineApi.Services;

namespace ThriftlineApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly ThriftlineDbContext _context;

    public PaymentController(ThriftlineDbContext context)
    {
        _context = context;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PaymentResponse>> GetById(Guid id)
    {
        var userId = GetCurrentUserId();

        var payment = await _context.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment is null)
        {
            return NotFound();
        }

        if (payment.Order.BuyerId != userId)
        {
            return Forbid();
        }

        return Ok(MapToResponse(payment));
    }

    [HttpPost]
    public async Task<ActionResult<PaymentResponse>> Create(CreatePaymentRequest request)
    {
        var userId = GetCurrentUserId();

        var order = await _context.Orders
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId);

        if (order is null)
        {
            return NotFound("Order tidak ditemukan.");
        }

        if (order.BuyerId != userId)
        {
            return Forbid();
        }

        if (order.Status != OrderStatus.Pending)
        {
            return BadRequest("Order ini sudah tidak menunggu pembayaran.");
        }

        if (order.Payment is not null)
        {
            return Conflict("Order ini sudah punya pembayaran.");
        }

        var payment = new Payment
        {
            OrderId = order.Id,
            Method = request.Method,
            Status = PaymentStatus.Pending,
            Amount = order.TotalAmount
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        payment.Order = order;
        return CreatedAtAction(nameof(GetById), new { id = payment.Id }, MapToResponse(payment));
    }

    [HttpPost("{id}/confirm")]
    public async Task<ActionResult<PaymentResponse>> Confirm(Guid id)
    {
        var userId = GetCurrentUserId();

        var payment = await _context.Payments
            .Include(p => p.Order)
                .ThenInclude(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Store)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment is null)
        {
            return NotFound();
        }

        if (payment.Order.BuyerId != userId)
        {
            return Forbid();
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            return BadRequest("Pembayaran ini sudah diproses sebelumnya.");
        }

        if (payment.Method == PaymentMethod.EWallet)
        {
            var wallet = await WalletHelper.GetOrCreateWalletAsync(_context, userId);
            if (wallet.Balance < payment.Amount)
            {
                return BadRequest("Saldo e-wallet tidak cukup.");
            }

            WalletHelper.RecordTransaction(_context, wallet, WalletTransactionType.Payment, -payment.Amount, $"Bayar pesanan {payment.Order.OrderNumber}", payment.Order.Id);
        }

        payment.Status = PaymentStatus.Success;
        payment.PaidAt = DateTime.UtcNow;
        payment.TransactionReference = $"TRX-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";

        payment.Order.Status = OrderStatus.Paid;

        foreach (var orderItem in payment.Order.OrderItems)
        {
            if (orderItem.Product.Stock <= 0)
            {
                orderItem.Product.Status = ProductStatus.Sold;
            }
        }

        var sellerIds = payment.Order.OrderItems
            .Select(oi => oi.Product.Store.OwnerId)
            .Distinct();

        foreach (var sellerId in sellerIds)
        {
            NotificationHelper.QueueNotification(
                _context,
                sellerId,
                NotificationType.Order,
                "Pesanan baru dibayar",
                $"Order {payment.Order.OrderNumber} telah dibayar.",
                "Order",
                payment.Order.Id);
        }

        await _context.SaveChangesAsync();

        return Ok(MapToResponse(payment));
    }

    [HttpPost("batch")]
    public async Task<ActionResult<List<PaymentResponse>>> CreateBatch(CreateBatchPaymentRequest request)
    {
        if (request.OrderIds is null || request.OrderIds.Count == 0)
        {
            return BadRequest("Tidak ada order yang dipilih.");
        }

        var userId = GetCurrentUserId();

        var orders = await _context.Orders
            .Include(o => o.Payment)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Store)
            .Where(o => request.OrderIds.Contains(o.Id))
            .ToListAsync();

        if (orders.Count != request.OrderIds.Distinct().Count())
        {
            return BadRequest("Sebagian order tidak ditemukan");
        }

        if (orders.Any(o => o.BuyerId != userId))
        {
            return Forbid();
        }

        if (orders.Any(o => o.Status != OrderStatus.Pending))
        {
            return BadRequest("Sebagian order sudah tidak menunggu pembayaran.");
        }

        if (orders.Any(o => o.Payment is not null))
        {
            return Conflict("Sebagian order sudah punya pembayaran.");
        }

        Wallet? wallet = null;
        if (request.Method == PaymentMethod.EWallet)
        {
            wallet = await WalletHelper.GetOrCreateWalletAsync(_context, userId);
            var totalNeeded = orders.Sum(o => o.TotalAmount);
            if (wallet.Balance < totalNeeded)
            {
                return BadRequest("Saldo e-wallet tidak cukup.");
            }
        }

        var payments = new List<Payment>();

        foreach (var order in orders)
        {
            var payment = new Payment
            {
                OrderId = order.Id,
                Order = order,
                Method = request.Method,
                Status = PaymentStatus.Success,
                Amount = order.TotalAmount,
                PaidAt = DateTime.UtcNow,
                TransactionReference = $"TRX-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}"
            };
            payments.Add(payment);
            _context.Payments.Add(payment);

            order.Status = OrderStatus.Paid;

            foreach (var orderItem in order.OrderItems)
            {
                if (orderItem.Product.Stock <= 0)
                {
                    orderItem.Product.Status = ProductStatus.Sold;
                }
            }

            var sellerIds = order.OrderItems.Select(oi => oi.Product.Store.OwnerId).Distinct();
            foreach (var sellerId in sellerIds)
            {
                NotificationHelper.QueueNotification(
                    _context,
                    sellerId,
                    NotificationType.Order,
                    "Pesanan baru dibayar",
                    $"Order {order.OrderNumber} telah dibayar.",
                    "Order",
                    order.Id);
            }
            if (wallet is not null)
            {
                WalletHelper.RecordTransaction(_context, wallet, WalletTransactionType.Payment, -order.TotalAmount, $"Bayar pesanan {order.OrderNumber}", order.Id);
            }
        }

        await _context.SaveChangesAsync();

        return Ok(payments.Select(MapToResponse).ToList());
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    private static PaymentResponse MapToResponse(Payment payment)
    {
        return new PaymentResponse
        {
            Id = payment.Id,
            OrderId = payment.OrderId,
            OrderNumber = payment.Order.OrderNumber,
            Method = payment.Method.ToString(),
            Status = payment.Status.ToString(),
            Amount = payment.Amount,
            TransactionReference = payment.TransactionReference,
            PaidAt = payment.PaidAt,
            CreatedAt = payment.CreatedAt
        };
    }
}
