using ThriftlineApi.Models.Enums;

namespace ThriftlineApi.Models;

public class ForumPostMedia
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ForumPostId { get; set; }
    public ForumPost ForumPost { get; set; } = null!;

    public string Url { get; set; } = string.Empty;
    public MediaType MediaType { get; set; } = MediaType.Image;
    public int DisplayOrder { get; set; }
}
