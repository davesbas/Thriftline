using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ThriftlineApi.Models;

namespace ThriftlineApi.Data.Configurations;

public class ForumCommentConfiguration : IEntityTypeConfiguration<ForumComment>
{
    public void Configure(EntityTypeBuilder<ForumComment> builder)
    {
        builder.Property(fc => fc.Content).IsRequired();

        builder.HasOne(fc => fc.ForumPost)
            .WithMany(fp => fp.Comments)
            .HasForeignKey(fc => fc.ForumPostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fc => fc.User)
            .WithMany()
            .HasForeignKey(fc => fc.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Product)
            .WithMany()
            .HasForeignKey(c => c.ProductId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.Store)
            .WithMany()
            .HasForeignKey(c => c.StoreId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
