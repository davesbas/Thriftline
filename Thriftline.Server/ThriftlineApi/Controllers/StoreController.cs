using System.Reflection.Metadata;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftlineApi.Data;
using ThriftlineApi.DTOs.Stores;
using ThriftlineApi.Models;

namespace ThriftlineApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoreController : ControllerBase
{
    private readonly ThriftlineDbContext _context;

    public StoreController(ThriftlineDbContext context)
    {
        _context = context;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StoreResponse>> GetById(Guid id)
    {
        var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == id);
        if (store is null)
        {
            return NotFound();
        }

        return Ok(MapToResponse(store));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<StoreResponse>> GetMyStore()
    {
        // Get the current user's ID
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);

        if (store is null)
        {
            return NotFound("Anda belum memiliki toko.");
        }

        return Ok(MapToResponse(store));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<StoreResponse>> Create(CreateStoreRequest request)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var alreadyHasStore = await _context.Stores.AnyAsync(s => s.OwnerId == userId);
        if (alreadyHasStore)
        {
            return Conflict("Anda sudah memiliki toko.");
        }

        var store = new Store
        {
            OwnerId = userId,
            Name = request.Name,
            Description = request.Description,
            LogoUrl = request.LogoUrl,
            Address = request.Address
        };

        _context.Stores.Add(store);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = store.Id }, MapToResponse(store));
    }

    private static StoreResponse MapToResponse(Store store)
    {
        return new StoreResponse
        {
            Id = store.Id,
            OwnerId = store.OwnerId,
            Name = store.Name,
            Description = store.Description,
            LogoUrl = store.LogoUrl,
            Address = store.Address
        };
    }
}