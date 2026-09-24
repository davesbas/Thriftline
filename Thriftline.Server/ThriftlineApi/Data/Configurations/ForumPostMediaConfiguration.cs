using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ThriftlineApi.Models;

namespace ThriftlineApi.Data.Configurations;

public class ForumPostMediaConfiguration : IEntityTypeConfiguration<ForumPostMedia>
{
    public void Configure(EntityTypeBuilder<ForumPostMedia> builder)
    {
        builder.Property(m => m.Url).IsRequired();
        builder.Property(m => m.MediaType).HasConversion<string>().HasMaxLength(10);

        builder.HasOne(m => m.ForumPost)
            .WithMany(fp => fp.Media)
            .HasForeignKey(m => m.ForumPostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
