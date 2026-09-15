using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ThriftlineApi.Models;

namespace ThriftlineApi.Data.Configurations;

public class ForumCommentConfiguration : IEntityTypeConfiguration<ForumComment>
{
    public void Configure(EntityTypeBuilder<ForumComment> builder)
    {
        builder.Property(fc => fc.Content).IsRequired();

        // ForumPost (1) --- (many) ForumComment: comments are owned entirely by the post.
        builder.HasOne(fc => fc.ForumPost)
            .WithMany(fp => fp.Comments)
            .HasForeignKey(fc => fc.ForumPostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fc => fc.User)
            .WithMany(u => u.ForumComments)
            .HasForeignKey(fc => fc.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
