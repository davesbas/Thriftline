using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ThriftlineApi.Models;

namespace ThriftlineApi.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(150);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.PhoneNumber).HasMaxLength(20);

        builder.HasIndex(u => u.Email).IsUnique();

        // User (1) --- (0..1) Store: a user optionally owns exactly one store.
        builder.HasOne(u => u.Store)
            .WithOne(s => s.Owner)
            .HasForeignKey<Store>(s => s.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
