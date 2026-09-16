using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftlineApi.Data;
using ThriftlineApi.DTOs.Common;
using ThriftlineApi.DTOs.Products;
using ThriftlineApi.Models;
using ThriftlineApi.Models.Enums;

namespace ThriftlineApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly ThriftlineDbContext _context;

    public ProductController(ThriftlineDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductSummaryResponse>>> GetAll([FromQuery] ProductQueryParameters query)
    {
        var productsQuery = _context.Products
            .Include(p => p.Images)
            .Include(p => p.Store)
            .Include(p => p.Category)
            .Where(p => p.Status == ProductStatus.Available)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            productsQuery = productsQuery.Where(p => p.Name.Contains(query.Search));
        }

        if (query.CategoryId.HasValue)
        {
            productsQuery = productsQuery.Where(p => p.CategoryId == query.CategoryId);
        }

        var totalCount = await productsQuery.CountAsync();

        var products = await productsQuery
            .OrderByDescending(p => p.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var items = products.Select(p => new ProductSummaryResponse
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            Condition = p.Condition,
            Status = p.Status,
            PrimaryImageUrl = p.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? p.Images.FirstOrDefault()?.ImageUrl,
            StoreName = p.Store.Name,
            CategoryName = p.Category.Name
        }).ToList();

        return Ok(new PagedResult<ProductSummaryResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDetailResponse>> GetById(Guid id)
    {
        var product = await _context.Products
            .Include(p => p.Images)
            .Include(p => p.Store)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        return Ok(MapToDetail(product));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ProductDetailResponse>> Create(CreateProductRequest request)
    {
        var store = await GetCurrentUserStoreAsync();
        if (store is null)
        {
            return BadRequest("Anda belum memiliki toko. Buat toko terlebih dahulu sebelum menambah produk.");
        }

        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == request.CategoryId);
        if (!categoryExists)
        {
            return BadRequest("Kategori tidak ditemukan.");
        }

        var product = new Product
        {
            StoreId = store.Id,
            CategoryId = request.CategoryId,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Condition = request.Condition,
            Stock = request.Stock,
            Images = request.ImageUrls.Select((url, index) => new ProductImage
            {
                ImageUrl = url,
                IsPrimary = index == 0,
                DisplayOrder = index
            }).ToList()
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        await _context.Entry(product).Reference(p => p.Store).LoadAsync();
        await _context.Entry(product).Reference(p => p.Category).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, MapToDetail(product));
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, UpdateProductRequest request)
    {
        var store = await GetCurrentUserStoreAsync();
        if (store is null)
        {
            return Forbid();
        }

        var product = await _context.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        if (product.StoreId != store.Id)
        {
            return Forbid();
        }

        product.CategoryId = request.CategoryId;
        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.Condition = request.Condition;
        product.Stock = request.Stock;
        product.Status = request.Status;
        product.UpdatedAt = DateTime.UtcNow;

        _context.ProductImages.RemoveRange(product.Images);
        product.Images = request.ImageUrls.Select((url, index) => new ProductImage
        {
            ImageUrl = url,
            IsPrimary = index == 0,
            DisplayOrder = index
        }).ToList();

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var store = await GetCurrentUserStoreAsync();
        if (store is null)
        {
            return Forbid();
        }

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        if (product.StoreId != store.Id)
        {
            return Forbid();
        }

        product.Status = ProductStatus.Inactive;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<Store?> GetCurrentUserStoreAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);
    }

    private static ProductDetailResponse MapToDetail(Product product)
    {
        return new ProductDetailResponse
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Condition = product.Condition,
            Stock = product.Stock,
            Status = product.Status,
            CategoryId = product.CategoryId,
            CategoryName = product.Category.Name,
            StoreId = product.StoreId,
            StoreName = product.Store.Name,
            ImageUrls = product.Images.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).ToList(),
            CreatedAt = product.CreatedAt
        };
    }
}
