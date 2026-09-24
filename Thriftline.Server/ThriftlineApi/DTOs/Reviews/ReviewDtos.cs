namespace ThriftlineApi.DTOs.Reviews;

public class ReviewResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateReviewRequest
{
    public Guid OrderItemId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

public class ReviewableItemResponse
{
    public Guid OrderItemId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public bool IsReviewed { get; set; }
}
