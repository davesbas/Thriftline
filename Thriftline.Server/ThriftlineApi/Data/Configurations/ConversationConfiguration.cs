using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ThriftlineApi.Models;

namespace ThriftlineApi.Data.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        // One persistent thread per buyer/store pair - repeat questions reuse it
        // instead of spawning a new conversation each time.
        builder.HasIndex(c => new { c.BuyerId, c.StoreId }).IsUnique();

        builder.HasOne(c => c.Buyer)
            .WithMany(u => u.Conversations)
            .HasForeignKey(c => c.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Store)
            .WithMany(s => s.Conversations)
            .HasForeignKey(c => c.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional context product; if it's removed the conversation still stands.
        builder.HasOne(c => c.Product)
            .WithMany(p => p.Conversations)
            .HasForeignKey(c => c.ProductId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
