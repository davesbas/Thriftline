namespace ThriftlineApi.DTOs.Categories;

public class CategoryResponse
{
    public Guid Id { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public List<CategoryResponse> SubCategories { get; set; } = new();
}
