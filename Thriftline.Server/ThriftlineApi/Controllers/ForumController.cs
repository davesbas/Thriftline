using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftlineApi.Data;
using ThriftlineApi.DTOs.Common;
using ThriftlineApi.DTOs.Forum;
using ThriftlineApi.Models;
using ThriftlineApi.Models.Enums;
using ThriftlineApi.Services;

namespace ThriftlineApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ForumController : ControllerBase
{
    private readonly ThriftlineDbContext _context;

    public ForumController(ThriftlineDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ForumPostSummaryResponse>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var currentUserId = GetCurrentUserIdOrNull();

        var query = _context.ForumPosts
            .Include(p => p.User)
            .Include(p => p.Store)
            .Include(p => p.Comments)
            .Include(p => p.Media)
            .Include(p => p.Likes)
            .Include(p => p.Product)
                .ThenInclude(pr => pr!.Images)
            .OrderByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync();
        var posts = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var items = posts.Select(p => MapToSummary(p, currentUserId)).ToList();

        return Ok(new PagedResult<ForumPostSummaryResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<ActionResult<List<ForumPostSummaryResponse>>> GetMine()
    {
        var userId = GetCurrentUserId();

        var posts = await _context.ForumPosts
            .Include(p => p.User)
            .Include(p => p.Store)
            .Include(p => p.Comments)
            .Include(p => p.Media)
            .Include(p => p.Likes)
            .Include(p => p.Product)
                .ThenInclude(pr => pr!.Images)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return Ok(posts.Select(p => MapToSummary(p, userId)).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ForumPostDetailResponse>> GetById(Guid id)
    {
        var currentUserId = GetCurrentUserIdOrNull();

        var post = await _context.ForumPosts
            .Include(p => p.User)
            .Include(p => p.Store)
            .Include(p => p.Media)
            .Include(p => p.Likes)
            .Include(p => p.Product)
                .ThenInclude(pr => pr!.Images)
            .Include(p => p.Comments)
                .ThenInclude(c => c.User)
            .Include(p => p.Comments)
                .ThenInclude(c => c.Store)
            .Include(p => p.Comments)
                .ThenInclude(c => c.Product)
                    .ThenInclude(pr => pr!.Images)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post is null)
        {
            return NotFound();
        }

        return Ok(MapToDetail(post, currentUserId));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ForumPostDetailResponse>> Create(CreateForumPostRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("Judul dan isi wajib diisi.");
        }

        var userId = GetCurrentUserId();

        Guid? productId = null;
        if (request.ProductId.HasValue)
        {
            var productExists = await _context.Products.AnyAsync(p => p.Id == request.ProductId);
            if (!productExists)
            {
                return BadRequest("Produk yang dilampirkan tidak ditemukan.");
            }
            productId = request.ProductId;
        }

        Guid? storeId = null;
        if (request.StoreId.HasValue)
        {
            var ownsStore = await _context.Stores.AnyAsync(s => s.Id == request.StoreId && s.OwnerId == userId);
            if (!ownsStore)
            {
                return BadRequest("Toko tidak ditemukan atau bukan milik Anda.");
            }
            storeId = request.StoreId;
        }

        var post = new ForumPost
        {
            UserId = userId,
            Title = request.Title,
            Content = request.Content,
            ProductId = productId,
            StoreId = storeId,
            Media = request.Media.Select((m, index) => new ForumPostMedia
            {
                Url = m.Url,
                MediaType = m.MediaType,
                DisplayOrder = index
            }).ToList()
        };

        _context.ForumPosts.Add(post);
        await _context.SaveChangesAsync();

        await _context.Entry(post).Reference(p => p.User).LoadAsync();
        if (post.StoreId.HasValue)
        {
            await _context.Entry(post).Reference(p => p.Store).LoadAsync();
        }
        if (post.ProductId.HasValue)
        {
            await _context.Entry(post).Reference(p => p.Product).LoadAsync();
            if (post.Product is not null)
            {
                await _context.Entry(post.Product).Collection(pr => pr.Images).LoadAsync();
            }
        }

        return CreatedAtAction(nameof(GetById), new { id = post.Id }, MapToDetail(post, userId));
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, UpdateForumPostRequest request)
    {
        var userId = GetCurrentUserId();

        var post = await _context.ForumPosts.FirstOrDefaultAsync(p => p.Id == id);
        if (post is null)
        {
            return NotFound();
        }

        if (post.UserId != userId)
        {
            return Forbid();
        }

        post.Title = request.Title;
        post.Content = request.Content;
        post.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();

        var post = await _context.ForumPosts.FirstOrDefaultAsync(p => p.Id == id);
        if (post is null)
        {
            return NotFound();
        }

        if (post.UserId != userId)
        {
            return Forbid();
        }

        _context.ForumPosts.Remove(post);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/like")]
    [Authorize]
    public async Task<ActionResult<ToggleLikeResponse>> ToggleLike(Guid id)
    {
        var userId = GetCurrentUserId();

        var postExists = await _context.ForumPosts.AnyAsync(p => p.Id == id);
        if (!postExists)
        {
            return NotFound();
        }

        var existingLike = await _context.ForumPostLikes
            .FirstOrDefaultAsync(l => l.ForumPostId == id && l.UserId == userId);

        bool liked;
        if (existingLike is not null)
        {
            _context.ForumPostLikes.Remove(existingLike);
            liked = false;
        }
        else
        {
            _context.ForumPostLikes.Add(new ForumPostLike { ForumPostId = id, UserId = userId });
            liked = true;
        }

        await _context.SaveChangesAsync();

        var likeCount = await _context.ForumPostLikes.CountAsync(l => l.ForumPostId == id);

        return Ok(new ToggleLikeResponse { Liked = liked, LikeCount = likeCount });
    }

    [HttpPost("{id}/comments")]
    [Authorize]
    public async Task<ActionResult<ForumCommentResponse>> AddComment(Guid id, CreateForumCommentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("Komentar tidak boleh kosong.");
        }

        var post = await _context.ForumPosts.FirstOrDefaultAsync(p => p.Id == id);
        if (post is null)
        {
            return NotFound("Post tidak ditemukan.");
        }

        var userId = GetCurrentUserId();

        Guid? productId = null;
        if (request.ProductId.HasValue)
        {
            var productExists = await _context.Products.AnyAsync(p => p.Id == request.ProductId);
            if (!productExists)
            {
                return BadRequest("Produk yang dilampirkan tidak ditemukan.");
            }
            productId = request.ProductId;
        }

        Guid? storeId = null;
        if (request.StoreId.HasValue)
        {
            var ownsStore = await _context.Stores.AnyAsync(s => s.Id == request.StoreId && s.OwnerId == userId);
            if (!ownsStore)
            {
                return BadRequest("Toko tidak ditemukan atau bukan milik Anda.");
            }
            storeId = request.StoreId;
        }

        var comment = new ForumComment
        {
            ForumPostId = id,
            UserId = userId,
            Content = request.Content,
            ProductId = productId,
            StoreId = storeId
        };

        _context.ForumComments.Add(comment);

        if (post.UserId != userId)
        {
            NotificationHelper.QueueNotification(
                _context,
                post.UserId,
                NotificationType.Forum,
                "Komentar baru",
                $"Ada komentar baru di post \"{post.Title}\".",
                "ForumPost",
                post.Id);
        }

        await _context.SaveChangesAsync();

        await _context.Entry(comment).Reference(c => c.User).LoadAsync();
        if (comment.StoreId.HasValue)
        {
            await _context.Entry(comment).Reference(c => c.Store).LoadAsync();
        }
        if (comment.ProductId.HasValue)
        {
            await _context.Entry(comment).Reference(c => c.Product).LoadAsync();
            if (comment.Product is not null)
            {
                await _context.Entry(comment.Product).Collection(pr => pr.Images).LoadAsync();
            }
        }

        return Ok(MapToCommentResponse(comment));
    }

    [HttpDelete("comments/{commentId}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(Guid commentId)
    {
        var userId = GetCurrentUserId();

        var comment = await _context.ForumComments.FirstOrDefaultAsync(c => c.Id == commentId);
        if (comment is null)
        {
            return NotFound();
        }

        if (comment.UserId != userId)
        {
            return Forbid();
        }

        _context.ForumComments.Remove(comment);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    private Guid? GetCurrentUserIdOrNull()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim is not null && Guid.TryParse(claim, out var id) ? id : null;
    }

    private static ForumPostSummaryResponse MapToSummary(ForumPost p, Guid? currentUserId)
    {
        return new ForumPostSummaryResponse
        {
            Id = p.Id,
            Title = p.Title,
            AuthorId = p.UserId,
            AuthorName = p.Store?.Name ?? p.User.FullName,
            AuthorAvatarUrl = p.Store?.LogoUrl ?? p.User.ProfilePictureUrl,
            PostedAsStoreId = p.StoreId,
            CommentCount = p.Comments.Count,
            LikeCount = p.Likes.Count,
            IsLikedByMe = currentUserId.HasValue && p.Likes.Any(l => l.UserId == currentUserId),
            ThumbnailUrl = p.Media.OrderBy(m => m.DisplayOrder).FirstOrDefault(m => m.MediaType == MediaType.Image)?.Url,
            ProductId = p.ProductId,
            ProductName = p.Product?.Name,
            ProductPrice = p.Product?.Price,
            ProductImageUrl = p.Product?.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                ?? p.Product?.Images.FirstOrDefault()?.ImageUrl,
            CreatedAt = p.CreatedAt
        };
    }

    private static ForumPostDetailResponse MapToDetail(ForumPost post, Guid? currentUserId)
    {
        return new ForumPostDetailResponse
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            AuthorId = post.UserId,
            AuthorName = post.Store?.Name ?? post.User.FullName,
            AuthorAvatarUrl = post.Store?.LogoUrl ?? post.User.ProfilePictureUrl,
            PostedAsStoreId = post.StoreId,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            Media = post.Media.OrderBy(m => m.DisplayOrder).Select(m => new ForumMediaItem { Url = m.Url, MediaType = m.MediaType }).ToList(),
            ProductId = post.ProductId,
            ProductName = post.Product?.Name,
            ProductPrice = post.Product?.Price,
            ProductImageUrl = post.Product?.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                ?? post.Product?.Images.FirstOrDefault()?.ImageUrl,
            LikeCount = post.Likes.Count,
            IsLikedByMe = currentUserId.HasValue && post.Likes.Any(l => l.UserId == currentUserId),
            Comments = post.Comments.OrderBy(c => c.CreatedAt).Select(MapToCommentResponse).ToList()
        };
    }

    private static ForumCommentResponse MapToCommentResponse(ForumComment comment)
    {
        return new ForumCommentResponse
        {
            Id = comment.Id,
            AuthorId = comment.UserId,
            AuthorName = comment.Store?.Name ?? comment.User.FullName,
            AuthorAvatarUrl = comment.Store?.LogoUrl ?? comment.User.ProfilePictureUrl,
            PostedAsStoreId = comment.StoreId,
            Content = comment.Content,
            ProductId = comment.ProductId,
            ProductName = comment.Product?.Name,
            ProductPrice = comment.Product?.Price,
            ProductImageUrl = comment.Product?.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                ?? comment.Product?.Images.FirstOrDefault()?.ImageUrl,
            CreatedAt = comment.CreatedAt
        };
    }
}
