using ThriftlineApi.Models.Enums;

namespace ThriftlineApi.Models;

public class ProductImage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string ImageUrl { get; set; } = string.Empty;
    public MediaType MediaType { get; set; } = MediaType.Image;
    public bool IsPrimary { get; set; }
    public int DisplayOrder { get; set; }
}
