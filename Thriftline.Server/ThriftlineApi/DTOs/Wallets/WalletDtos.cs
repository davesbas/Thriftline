namespace ThriftlineApi.DTOs.Wallets;

public class WalletResponse
{
    public decimal Balance { get; set; }
}

public class WalletTransactionResponse
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class TopUpRequest
{
    public decimal Amount { get; set; }
}
