namespace ThriftlineApi.DTOs.Orders;

public class OrderItemResponse
{
    public Guid OrderItemId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public bool IsReviewed { get; set; }
}

public class OrderSummaryResponse
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemResponse> Items { get; set; } = new();
}

public class OrderDetailResponse
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingCourier { get; set; } = string.Empty;
    public decimal ShippingCost { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemResponse> Items { get; set; } = new();
}

public class CheckoutRequest
{
    public string ShippingAddress { get; set; } = string.Empty;
    public List<Guid>? CartItemIds { get; set; }
    public List<StoreShippingOptionRequest> StoreOptions { get; set; } = new();
}

public class StoreShippingOptionRequest
{
    public Guid StoreId { get; set; }
    public string ShippingCourier { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class BuyNowRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingCourier { get; set; } = string.Empty;
}

public class StoreOrderSummaryResponse
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingCourier { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<OrderItemResponse> Items { get; set; } = new();
}

public class UpdateOrderStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class CourierOptionResponse
{
    public string Name { get; set; } = string.Empty;
    public decimal Cost { get; set; }
}
