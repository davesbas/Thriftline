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
        var currentUserId = GetCurrentUserIdOrNull();

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
            var subCategoryIds = await _context.Categories
                .Where(c => c.ParentCategoryId == query.CategoryId)
                .Select(c => c.Id)
                .ToListAsync();

            var categoryIds = new List<Guid> { query.CategoryId.Value };
            categoryIds.AddRange(subCategoryIds);

            productsQuery = productsQuery.Where(p => categoryIds.Contains(p.CategoryId));
        }

        if (query.StoreId.HasValue)
        {
            productsQuery = productsQuery.Where(p => p.StoreId == query.StoreId);
        }

        var totalCount = await productsQuery.CountAsync();

        var products = await productsQuery
            .OrderByDescending(p => p.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var wishlistedIds = await GetWishlistedIdsAsync(currentUserId);

        var items = products.Select(p => new ProductSummaryResponse
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            Condition = p.Condition,
            Status = p.Status,
            PrimaryImageUrl = p.Images.FirstOrDefault(i => i.IsPrimary && i.MediaType == MediaType.Image)?.ImageUrl
                ?? p.Images.FirstOrDefault(i => i.MediaType == MediaType.Image)?.ImageUrl,
            StoreName = p.Store.Name,
            CategoryName = p.Category.Name,
            IsWishlisted = wishlistedIds.Contains(p.Id)
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
        var currentUserId = GetCurrentUserIdOrNull();

        var product = await _context.Products
            .Include(p => p.Images)
            .Include(p => p.Store)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        var isWishlisted = currentUserId.HasValue
            && await _context.Wishlists.AnyAsync(w => w.UserId == currentUserId && w.ProductId == id);

        return Ok(MapToDetail(product, isWishlisted));
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<ActionResult<List<ProductDetailResponse>>> GetMyProducts()
    {
        var store = await GetCurrentUserStoreAsync();
        if (store is null)
        {
            return Ok(new List<ProductDetailResponse>());
        }

        var products = await _context.Products
            .Include(p => p.Images)
            .Include(p => p.Store)
            .Include(p => p.Category)
            .Where(p => p.StoreId == store.Id)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var currentUserId = GetCurrentUserIdOrNull();
        var wishlistedIds = await GetWishlistedIdsAsync(currentUserId);

        return Ok(products.Select(p => MapToDetail(p, wishlistedIds.Contains(p.Id))).ToList());
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
            Images = request.Media.Select((m, index) => new ProductImage
            {
                ImageUrl = m.Url,
                MediaType = m.MediaType,
                IsPrimary = index == 0,
                DisplayOrder = index
            }).ToList()
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        await _context.Entry(product).Reference(p => p.Store).LoadAsync();
        await _context.Entry(product).Reference(p => p.Category).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, MapToDetail(product, false));
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

        var newImages = request.Media.Select((m, index) => new ProductImage
        {
            ProductId = product.Id,
            ImageUrl = m.Url,
            MediaType = m.MediaType,
            IsPrimary = index == 0,
            DisplayOrder = index
        }).ToList();
        _context.ProductImages.AddRange(newImages);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Produk ini baru saja diubah di tempat lain. Muat ulang lalu coba lagi.");
        }

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

    private Guid? GetCurrentUserIdOrNull()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim is not null && Guid.TryParse(claim, out var id) ? id : null;
    }

    private async Task<HashSet<Guid>> GetWishlistedIdsAsync(Guid? userId)
    {
        if (!userId.HasValue)
        {
            return new HashSet<Guid>();
        }

        var ids = await _context.Wishlists
            .Where(w => w.UserId == userId)
            .Select(w => w.ProductId)
            .ToListAsync();

        return ids.ToHashSet();
    }

    private static ProductDetailResponse MapToDetail(Product product, bool isWishlisted)
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
            Media = product.Images
                .OrderBy(i => i.DisplayOrder)
                .Select(i => new ProductMediaItem { Url = i.ImageUrl, MediaType = i.MediaType })
                .ToList(),
            IsWishlisted = isWishlisted,
            CreatedAt = product.CreatedAt
        };
    }
}
