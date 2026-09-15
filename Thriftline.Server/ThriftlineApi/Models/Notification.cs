using ThriftlineApi.Models.Enums;

namespace ThriftlineApi.Models;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }

    // Loose (non-FK) reference to whatever triggered the notification - an Order, a
    // Conversation, a ForumPost, etc. Kept generic instead of one nullable FK per
    // possible target type, since a notification can point to any of them.
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
