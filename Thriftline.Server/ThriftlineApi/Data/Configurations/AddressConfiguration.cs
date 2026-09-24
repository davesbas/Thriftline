using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ThriftlineApi.Models;

namespace ThriftlineApi.Data.Configurations;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.Property(a => a.Label).IsRequired().HasMaxLength(50);
        builder.Property(a => a.RecipientName).IsRequired().HasMaxLength(100);
        builder.Property(a => a.PhoneNumber).IsRequired().HasMaxLength(20);
        builder.Property(a => a.FullAddress).IsRequired().HasMaxLength(500);

        builder.HasOne(a => a.User)
            .WithMany(u => u.Addresses)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
