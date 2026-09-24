using ThriftlineApi.Models.Enums;

namespace ThriftlineApi.Models;

public class WalletTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WalletId { get; set; }
    public Wallet Wallet { get; set; } = null!;

    public WalletTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? OrderId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}