using ThriftlineApi.Models.Enums;

namespace ThriftlineApi.Models;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid StoreId { get; set; }
    public Store Store { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public ProductCondition Condition { get; set; }
    public int Stock { get; set; } = 1;
    public ProductStatus Status { get; set; } = ProductStatus.Available;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
}
