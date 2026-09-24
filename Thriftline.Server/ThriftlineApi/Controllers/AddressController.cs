using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftlineApi.Data;
using ThriftlineApi.DTOs.Addresses;
using ThriftlineApi.Models;

namespace ThriftlineApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AddressController : ControllerBase
{
    private readonly ThriftlineDbContext _context;

    public AddressController(ThriftlineDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<AddressResponse>>> GetMyAddresses()
    {
        var userId = GetCurrentUserId();

        var addresses = await _context.Addresses
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsPrimary)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();

        return Ok(addresses.Select(MapToResponse).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<AddressResponse>> Create(AddressRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Label) || string.IsNullOrWhiteSpace(request.RecipientName)
            || string.IsNullOrWhiteSpace(request.PhoneNumber) || string.IsNullOrWhiteSpace(request.FullAddress))
        {
            return BadRequest("Semua field alamat wajib diisi.");
        }

        var userId = GetCurrentUserId();

        var hasAny = await _context.Addresses.AnyAsync(a => a.UserId == userId);
        var makePrimary = request.IsPrimary || !hasAny;

        if (makePrimary)
        {
            await UnsetOtherPrimaries(userId);
        }

        var address = new Address
        {
            UserId = userId,
            Label = request.Label,
            RecipientName = request.RecipientName,
            PhoneNumber = request.PhoneNumber,
            FullAddress = request.FullAddress,
            IsPrimary = makePrimary
        };

        _context.Addresses.Add(address);
        await _context.SaveChangesAsync();

        return Ok(MapToResponse(address));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AddressResponse>> Update(Guid id, AddressRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Label) || string.IsNullOrWhiteSpace(request.RecipientName)
            || string.IsNullOrWhiteSpace(request.PhoneNumber) || string.IsNullOrWhiteSpace(request.FullAddress))
        {
            return BadRequest("Semua field alamat wajib diisi.");
        }

        var userId = GetCurrentUserId();

        var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (address is null)
        {
            return NotFound();
        }

        address.Label = request.Label;
        address.RecipientName = request.RecipientName;
        address.PhoneNumber = request.PhoneNumber;
        address.FullAddress = request.FullAddress;

        if (request.IsPrimary && !address.IsPrimary)
        {
            await UnsetOtherPrimaries(userId);
            address.IsPrimary = true;
        }

        await _context.SaveChangesAsync();

        return Ok(MapToResponse(address));
    }

    [HttpPut("{id}/primary")]
    public async Task<IActionResult> SetPrimary(Guid id)
    {
        var userId = GetCurrentUserId();

        var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (address is null)
        {
            return NotFound();
        }

        await UnsetOtherPrimaries(userId);
        address.IsPrimary = true;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();

        var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (address is null)
        {
            return NotFound();
        }

        var wasPrimary = address.IsPrimary;

        _context.Addresses.Remove(address);
        await _context.SaveChangesAsync();

        if (wasPrimary)
        {
            var next = await _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (next is not null)
            {
                next.IsPrimary = true;
                await _context.SaveChangesAsync();
            }
        }

        return NoContent();
    }

    private async Task UnsetOtherPrimaries(Guid userId)
    {
        var current = await _context.Addresses.Where(a => a.UserId == userId && a.IsPrimary).ToListAsync();
        foreach (var a in current)
        {
            a.IsPrimary = false;
        }
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    private static AddressResponse MapToResponse(Address a)
    {
        return new AddressResponse
        {
            Id = a.Id,
            Label = a.Label,
            RecipientName = a.RecipientName,
            PhoneNumber = a.PhoneNumber,
            FullAddress = a.FullAddress,
            IsPrimary = a.IsPrimary
        };
    }
}
