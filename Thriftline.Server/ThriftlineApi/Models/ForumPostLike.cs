namespace ThriftlineApi.Models;

public class ForumPostLike
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ForumPostId { get; set; }
    public ForumPost ForumPost { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}