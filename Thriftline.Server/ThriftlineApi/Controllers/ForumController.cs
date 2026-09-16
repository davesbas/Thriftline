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
        var query = _context.ForumPosts
            .Include(p => p.User)
            .Include(p => p.Comments)
            .OrderByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync();
        var posts = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var items = posts.Select(p => new ForumPostSummaryResponse
        {
            Id = p.Id,
            Title = p.Title,
            AuthorId = p.UserId,
            AuthorName = p.User.FullName,
            CommentCount = p.Comments.Count,
            CreatedAt = p.CreatedAt
        }).ToList();

        return Ok(new PagedResult<ForumPostSummaryResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ForumPostDetailResponse>> GetById(Guid id)
    {
        var post = await _context.ForumPosts
            .Include(p => p.User)
            .Include(p => p.Comments)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post is null)
        {
            return NotFound();
        }

        return Ok(MapToDetail(post));
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

        var post = new ForumPost
        {
            UserId = userId,
            Title = request.Title,
            Content = request.Content
        };

        _context.ForumPosts.Add(post);
        await _context.SaveChangesAsync();

        await _context.Entry(post).Reference(p => p.User).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = post.Id }, MapToDetail(post));
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

        var comment = new ForumComment
        {
            ForumPostId = id,
            UserId = userId,
            Content = request.Content
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

    private static ForumPostDetailResponse MapToDetail(ForumPost post)
    {
        return new ForumPostDetailResponse
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            AuthorId = post.UserId,
            AuthorName = post.User.FullName,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            Comments = post.Comments.OrderBy(c => c.CreatedAt).Select(MapToCommentResponse).ToList()
        };
    }

    private static ForumCommentResponse MapToCommentResponse(ForumComment comment)
    {
        return new ForumCommentResponse
        {
            Id = comment.Id,
            AuthorId = comment.UserId,
            AuthorName = comment.User.FullName,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt
        };
    }
}
