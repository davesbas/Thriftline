using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftlineApi.Data;
using ThriftlineApi.DTOs.Wallets;
using ThriftlineApi.Models.Enums;
using ThriftlineApi.Services;

namespace ThriftlineApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly ThriftlineDbContext _context;

    public WalletController(ThriftlineDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<WalletResponse>> GetMyWallet()
    {
        var userId = GetCurrentUserId();
        var wallet = await WalletHelper.GetOrCreateWalletAsync(_context, userId);
        await _context.SaveChangesAsync();

        return Ok(new WalletResponse { Balance = wallet.Balance });
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<List<WalletTransactionResponse>>> GetTransactions()
    {
        var userId = GetCurrentUserId();
        var wallet = await WalletHelper.GetOrCreateWalletAsync(_context, userId);
        await _context.SaveChangesAsync();

        var transactions = await _context.WalletTransactions
            .Where(t => t.WalletId == wallet.Id)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new WalletTransactionResponse
            {
                Id = t.Id,
                Type = t.Type.ToString(),
                Amount = t.Amount,
                BalanceAfter = t.BalanceAfter,
                Description = t.Description,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return Ok(transactions);
    }

    [HttpPost("topup")]
    public async Task<ActionResult<WalletResponse>> TopUp(TopUpRequest request)
    {
        if (request.Amount < 10000)
        {
            return BadRequest("Minimal top up Rp10.000.");
        }

        if (request.Amount > 10000000)
        {
            return BadRequest("Maksimal top up Rp10.000.000 per transaksi.");
        }

        var userId = GetCurrentUserId();
        var wallet = await WalletHelper.GetOrCreateWalletAsync(_context, userId);

        WalletHelper.RecordTransaction(_context, wallet, WalletTransactionType.TopUp, request.Amount, "Top up saldo");

        await _context.SaveChangesAsync();

        return Ok(new WalletResponse { Balance = wallet.Balance });
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }
}
