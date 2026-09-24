using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ThriftlineApi.Models;

namespace ThriftlineApi.Data.Configurations;

public class ForumPostLikeConfiguration : IEntityTypeConfiguration<ForumPostLike>
{
    public void Configure(EntityTypeBuilder<ForumPostLike> builder)
    {
        builder.HasIndex(l => new { l.ForumPostId, l.UserId }).IsUnique();

        builder.HasOne(l => l.ForumPost)
            .WithMany(fp => fp.Likes)
            .HasForeignKey(l => l.ForumPostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}