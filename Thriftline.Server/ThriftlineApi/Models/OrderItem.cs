namespace ThriftlineApi.Models;

public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; } = 1;

    // Snapshot of the product price at purchase time, so later price changes
    // don't rewrite historical order totals.
    public decimal UnitPrice { get; set; }
}
