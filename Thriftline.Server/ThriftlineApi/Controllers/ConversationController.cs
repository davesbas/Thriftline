using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftlineApi.Data;
using ThriftlineApi.DTOs.Conversations;
using ThriftlineApi.Models;
using ThriftlineApi.Models.Enums;
using ThriftlineApi.Services;

namespace ThriftlineApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConversationController : ControllerBase
{
    private readonly ThriftlineDbContext _context;

    public ConversationController(ThriftlineDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<ConversationSummaryResponse>>> GetMyConversations()
    {
        var userId = GetCurrentUserId();

        var conversations = await _context.Conversations
            .Include(c => c.Store)
            .Include(c => c.Buyer)
            .Include(c => c.Product)
            .Include(c => c.Messages)
            .Where(c => c.BuyerId == userId || c.Store.OwnerId == userId)
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ToListAsync();

        return Ok(conversations.Select(MapToSummary).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ConversationDetailResponse>> GetById(Guid id)
    {
        var userId = GetCurrentUserId();

        var conversation = await _context.Conversations
            .Include(c => c.Store)
            .Include(c => c.Buyer)
            .Include(c => c.Product)
            .Include(c => c.Messages)
                .ThenInclude(m => m.Sender)
            .Include(c => c.Messages)
                .ThenInclude(m => m.Product)
                    .ThenInclude(p => p!.Images)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (conversation is null)
        {
            return NotFound();
        }

        if (!IsParticipant(conversation, userId))
        {
            return Forbid();
        }

        return Ok(MapToDetail(conversation));
    }

    [HttpPost]
    public async Task<ActionResult<ConversationDetailResponse>> StartConversation(StartConversationRequest request)
    {
        var userId = GetCurrentUserId();

        var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == request.StoreId);
        if (store is null)
        {
            return NotFound("Toko tidak ditemukan.");
        }

        if (store.OwnerId == userId)
        {
            return BadRequest("Tidak bisa memulai percakapan dengan toko sendiri.");
        }

        Product? requestedProduct = null;
        if (request.ProductId.HasValue)
        {
            requestedProduct = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId && p.StoreId == store.Id);
            if (requestedProduct is null)
            {
                return BadRequest("Produk tidak ditemukan di toko ini.");
            }
        }

        var existing = await _context.Conversations
            .Include(c => c.Store)
            .Include(c => c.Buyer)
            .Include(c => c.Product)
            .Include(c => c.Messages)
                .ThenInclude(m => m.Sender)
            .Include(c => c.Messages)
                .ThenInclude(m => m.Product)
                    .ThenInclude(p => p!.Images)
            .FirstOrDefaultAsync(c => c.BuyerId == userId && c.StoreId == request.StoreId);

        if (existing is not null)
        {
            if (requestedProduct is not null && existing.ProductId != requestedProduct.Id)
            {
                // Just a targeted scalar update to mark "this is the product currently being
                // discussed" - no message is created here. It's only shown as a pending
                // attachment on the frontend and actually lands in the chat once the buyer
                // sends a message while it's attached (see SendMessage).
                await _context.Conversations
                    .Where(c => c.Id == existing.Id)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.ProductId, requestedProduct.Id));

                existing.ProductId = requestedProduct.Id;
                await _context.Entry(existing).Reference(c => c.Product).LoadAsync();
            }

            return Ok(MapToDetail(existing));
        }

        var conversation = new Conversation
        {
            BuyerId = userId,
            StoreId = request.StoreId,
            ProductId = requestedProduct?.Id
        };

        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        await _context.Entry(conversation).Reference(c => c.Store).LoadAsync();
        await _context.Entry(conversation).Reference(c => c.Buyer).LoadAsync();
        if (conversation.ProductId.HasValue)
        {
            await _context.Entry(conversation).Reference(c => c.Product).LoadAsync();
        }

        return CreatedAtAction(nameof(GetById), new { id = conversation.Id }, MapToDetail(conversation));
    }

    [HttpPost("{id}/messages")]
    public async Task<ActionResult<MessageResponse>> SendMessage(Guid id, SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("Pesan tidak boleh kosong.");
        }

        var userId = GetCurrentUserId();

        var conversation = await _context.Conversations
            .Include(c => c.Store)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (conversation is null)
        {
            return NotFound();
        }

        if (!IsParticipant(conversation, userId))
        {
            return Forbid();
        }

        // The product attached from the frontend's "pending" card - only accepted if it
        // genuinely belongs to this conversation's store, otherwise silently ignored.
        Guid? productId = null;
        if (request.ProductId.HasValue)
        {
            var belongsToStore = await _context.Products
                .AnyAsync(p => p.Id == request.ProductId && p.StoreId == conversation.StoreId);
            if (belongsToStore)
            {
                productId = request.ProductId;
            }
        }

        var message = new Message
        {
            ConversationId = conversation.Id,
            SenderId = userId,
            Content = request.Content,
            ProductId = productId
        };

        _context.Messages.Add(message);
        conversation.LastMessageAt = DateTime.UtcNow;

        var recipientId = userId == conversation.BuyerId ? conversation.Store.OwnerId : conversation.BuyerId;
        NotificationHelper.QueueNotification(
            _context,
            recipientId,
            NotificationType.Chat,
            "Pesan baru",
            request.Content.Length > 100 ? request.Content[..100] + "..." : request.Content,
            "Conversation",
            conversation.Id);

        await _context.SaveChangesAsync();

        await _context.Entry(message).Reference(m => m.Sender).LoadAsync();
        if (message.ProductId.HasValue)
        {
            await _context.Entry(message).Reference(m => m.Product).LoadAsync();
            if (message.Product is not null)
            {
                await _context.Entry(message.Product).Collection(p => p.Images).LoadAsync();
            }
        }

        return Ok(MapToMessageResponse(message));
    }

    private static bool IsParticipant(Conversation conversation, Guid userId)
    {
        return conversation.BuyerId == userId || conversation.Store.OwnerId == userId;
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    private static ConversationSummaryResponse MapToSummary(Conversation c)
    {
        var lastMessage = c.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault();

        return new ConversationSummaryResponse
        {
            Id = c.Id,
            StoreId = c.StoreId,
            StoreName = c.Store.Name,
            BuyerId = c.BuyerId,
            BuyerName = c.Buyer.FullName,
            ProductId = c.ProductId,
            ProductName = c.Product?.Name,
            CreatedAt = c.CreatedAt,
            LastMessageAt = c.LastMessageAt,
            LastMessagePreview = lastMessage?.Content
        };
    }

    private static ConversationDetailResponse MapToDetail(Conversation c)
    {
        return new ConversationDetailResponse
        {
            Id = c.Id,
            StoreId = c.StoreId,
            StoreName = c.Store.Name,
            BuyerId = c.BuyerId,
            BuyerName = c.Buyer.FullName,
            ProductId = c.ProductId,
            ProductName = c.Product?.Name,
            CreatedAt = c.CreatedAt,
            Messages = c.Messages.OrderBy(m => m.SentAt).Select(MapToMessageResponse).ToList()
        };
    }

    private static MessageResponse MapToMessageResponse(Message m)
    {
        return new MessageResponse
        {
            Id = m.Id,
            SenderId = m.SenderId,
            SenderName = m.Sender.FullName,
            Content = m.Content,
            IsRead = m.IsRead,
            SentAt = m.SentAt,
            ProductId = m.ProductId,
            ProductName = m.Product?.Name,
            ProductPrice = m.Product?.Price,
            ProductImageUrl = m.Product?.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                ?? m.Product?.Images.FirstOrDefault()?.ImageUrl
        };
    }
}
