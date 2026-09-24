namespace ThriftlineApi.Models;

public class ForumPost
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    // Optional: if set, this post is posted "as" the user's store rather than
    // as themselves - the store's name/logo is shown instead of the personal one.
    public Guid? StoreId { get; set; }
    public Store? Store { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ForumPostMedia> Media { get; set; } = new List<ForumPostMedia>();
    public ICollection<ForumComment> Comments { get; set; } = new List<ForumComment>();
    public ICollection<ForumPostLike> Likes { get; set; } = new List<ForumPostLike>();
}
