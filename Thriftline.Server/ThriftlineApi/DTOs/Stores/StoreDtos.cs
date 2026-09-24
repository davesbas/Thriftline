namespace ThriftlineApi.DTOs.Stores;

public class StoreResponse
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? Address { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}

public class CreateStoreRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public bool AgreedToTerms { get; set; }
}
