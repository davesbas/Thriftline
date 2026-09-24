using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ThriftlineApi.Models;

namespace ThriftlineApi.Data.Configurations;

public class ForumPostConfiguration : IEntityTypeConfiguration<ForumPost>
{
    public void Configure(EntityTypeBuilder<ForumPost> builder)
    {
        builder.Property(fp => fp.Title).IsRequired().HasMaxLength(200);
        builder.Property(fp => fp.Content).IsRequired();

        builder.HasOne(fp => fp.User)
            .WithMany(u => u.ForumPosts)
            .HasForeignKey(fp => fp.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(fp => fp.Product)
            .WithMany()
            .HasForeignKey(fp => fp.ProductId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(fp => fp.Store)
            .WithMany()
            .HasForeignKey(fp => fp.StoreId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
