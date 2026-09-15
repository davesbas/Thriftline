namespace ThriftlineApi.Models;

public class Store
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // One-to-one with User: the seller who owns this store.
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? Address { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
}
