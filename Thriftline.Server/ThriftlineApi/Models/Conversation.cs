namespace ThriftlineApi.Models;

public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // A conversation always happens between one buyer and one store (seller).
    public Guid BuyerId { get; set; }
    public User Buyer { get; set; } = null!;

    public Guid StoreId { get; set; }
    public Store Store { get; set; } = null!;

    // Optional product the conversation started from (e.g. "chat" button on a product page).
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastMessageAt { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
