using ThriftlineApi.Models.Enums;

namespace ThriftlineApi.DTOs.Forum;

public class ForumMediaItem
{
    public string Url { get; set; } = string.Empty;
    public MediaType MediaType { get; set; } = MediaType.Image;
}

public class ForumPostSummaryResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }
    public Guid? PostedAsStoreId { get; set; }
    public int CommentCount { get; set; }
    public int LikeCount { get; set; }
    public bool IsLikedByMe { get; set; }
    public string? ThumbnailUrl { get; set; }
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal? ProductPrice { get; set; }
    public string? ProductImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ForumCommentResponse
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }
    public Guid? PostedAsStoreId { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal? ProductPrice { get; set; }
    public string? ProductImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ForumPostDetailResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }
    public Guid? PostedAsStoreId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<ForumMediaItem> Media { get; set; } = new();
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal? ProductPrice { get; set; }
    public string? ProductImageUrl { get; set; }
    public int LikeCount { get; set; }
    public bool IsLikedByMe { get; set; }
    public List<ForumCommentResponse> Comments { get; set; } = new();
}

public class CreateForumPostRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<ForumMediaItem> Media { get; set; } = new();
    public Guid? ProductId { get; set; }
    public Guid? StoreId { get; set; }
}

public class UpdateForumPostRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class CreateForumCommentRequest
{
    public string Content { get; set; } = string.Empty;
    public Guid? ProductId { get; set; }
    public Guid? StoreId { get; set; }
}

public class ToggleLikeResponse
{
    public bool Liked { get; set; }
    public int LikeCount { get; set; }
}
