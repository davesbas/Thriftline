namespace ThriftlineApi.Models;

public class ForumComment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ForumPostId { get; set; }
    public ForumPost ForumPost { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
