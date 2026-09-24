using Microsoft.EntityFrameworkCore;
using ThriftlineApi.Data;
using ThriftlineApi.Models;
using ThriftlineApi.Models.Enums;

namespace ThriftlineApi.Services;

public static class WalletHelper
{
    public static async Task<Wallet> GetOrCreateWalletAsync(ThriftlineDbContext context, Guid userId)
    {
        var wallet = await context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet is null)
        {
            wallet = new Wallet { UserId = userId, Balance = 0 };
            context.Wallets.Add(wallet);
        }

        return wallet;
    }

    public static void RecordTransaction(
        ThriftlineDbContext context,
        Wallet wallet,
        WalletTransactionType type,
        decimal amount,
        string description,
        Guid? orderId = null)
    {
        wallet.Balance += amount;

        context.WalletTransactions.Add(new WalletTransaction
        {
            WalletId = wallet.Id,
            Type = type,
            Amount = amount,
            BalanceAfter = wallet.Balance,
            Description = description,
            OrderId = orderId
        });
    }
}
