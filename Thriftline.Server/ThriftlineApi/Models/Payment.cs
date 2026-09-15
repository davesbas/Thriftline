using ThriftlineApi.Models.Enums;

namespace ThriftlineApi.Models;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // One-to-one with Order.
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public decimal Amount { get; set; }
    public string? TransactionReference { get; set; }
    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
