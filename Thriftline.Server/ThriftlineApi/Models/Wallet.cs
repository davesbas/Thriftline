namespace ThriftlineApi.Models;

public class Wallet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public decimal Balance { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
}