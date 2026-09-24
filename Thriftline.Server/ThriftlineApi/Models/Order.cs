using ThriftlineApi.Models.Enums;

namespace ThriftlineApi.Models;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BuyerId { get; set; }
    public User Buyer { get; set; } = null!;

    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingCourier { get; set; } = string.Empty;
    public decimal ShippingCost { get; set; }
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    // One-to-one: a single payment record per order.
    public Payment? Payment { get; set; }
}
