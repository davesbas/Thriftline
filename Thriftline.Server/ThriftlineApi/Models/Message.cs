namespace ThriftlineApi.Models;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    // The sender is always a User - either the buyer or the store owner replying.
    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!;

    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; }

    // Optional product reference so a message can show a product card
    // (e.g. the auto-message generated when a chat starts from a product page).
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
