using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ThriftlineApi.Models;

namespace ThriftlineApi.Data.Configurations;

public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.Property(s => s.Name).IsRequired().HasMaxLength(150);

        // Enforces the one-to-one relation from the dependent side as well.
        builder.HasIndex(s => s.OwnerId).IsUnique();
    }
}
