namespace ThriftlineApi.Models;

public class ProductReview
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderItemId { get; set; }
    public OrderItem OrderItem { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Rating { get; set; }
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}