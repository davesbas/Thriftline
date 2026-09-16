using ThriftlineApi.Models.Enums;

namespace ThriftlineApi.DTOs.Products;

public class ProductSummaryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public ProductCondition Condition { get; set; }
    public ProductStatus Status { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
}

public class ProductDetailResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public ProductCondition Condition { get; set; }
    public int Stock { get; set; }
    public ProductStatus Status { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public Guid StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public List<string> ImageUrls { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class ProductQueryParameters
{
    public string? Search { get; set; }
    public Guid? CategoryId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CreateProductRequest
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public ProductCondition Condition { get; set; }
    public int Stock { get; set; } = 1;
    public List<string> ImageUrls { get; set; } = new();
}

public class UpdateProductRequest
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public ProductCondition Condition { get; set; }
    public int Stock { get; set; }
    public ProductStatus Status { get; set; }
    public List<string> ImageUrls { get; set; } = new();
}
