namespace ThriftlineApi.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? Address { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // A user optionally owns one store (seller profile).
    public Store? Store { get; set; }
    public Wallet? Wallet { get; set; }
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<ForumPost> ForumPosts { get; set; } = new List<ForumPost>();
    public ICollection<ForumComment> ForumComments { get; set; } = new List<ForumComment>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
}
