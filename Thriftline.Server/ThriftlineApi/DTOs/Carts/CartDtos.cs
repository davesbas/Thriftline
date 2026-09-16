namespace ThriftlineApi.DTOs.Carts;

public class CartItemResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal ProductPrice { get; set; }
    public string? ProductImageUrl { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public int Quantity { get; set; }
    public decimal Subtotal { get; set; }
}

public class CartSummaryResponse
{
    public List<CartItemResponse> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public decimal TotalPrice { get; set; }
}

public class AddToCartRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class UpdateCartItemRequest
{
    public int Quantity { get; set; }
}