using Microsoft.EntityFrameworkCore;
using ThriftlineApi.Models;

namespace ThriftlineApi.Data;

public class ThriftlineDbContext : DbContext
{
    public ThriftlineDbContext(DbContextOptions<ThriftlineDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<ForumPost> ForumPosts => Set<ForumPost>();
    public DbSet<ForumComment> ForumComments => Set<ForumComment>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ThriftlineDbContext).Assembly);
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = Guid.Parse("11111111-1111-1111-1111-111111111101"), Name = "Fashion Pria" },
            new Category { Id = Guid.Parse("11111111-1111-1111-1111-111111111102"), Name = "Atasan Pria", ParentCategoryId = Guid.Parse("11111111-1111-1111-1111-111111111101") },
            new Category { Id = Guid.Parse("11111111-1111-1111-1111-111111111103"), Name = "Celana Pria", ParentCategoryId = Guid.Parse("11111111-1111-1111-1111-111111111101") },
            new Category { Id = Guid.Parse("11111111-1111-1111-1111-111111111104"), Name = "Fashion Wanita" },
            new Category { Id = Guid.Parse("11111111-1111-1111-1111-111111111105"), Name = "Atasan Wanita", ParentCategoryId = Guid.Parse("11111111-1111-1111-1111-111111111104") },
            new Category { Id = Guid.Parse("11111111-1111-1111-1111-111111111106"), Name = "Bawahan Wanita", ParentCategoryId = Guid.Parse("11111111-1111-1111-1111-111111111104") },
            new Category { Id = Guid.Parse("11111111-1111-1111-1111-111111111107"), Name = "Sepatu" },
            new Category { Id = Guid.Parse("11111111-1111-1111-1111-111111111108"), Name = "Tas" },
            new Category { Id = Guid.Parse("11111111-1111-1111-1111-111111111109"), Name = "Elektronik" },
            new Category { Id = Guid.Parse("11111111-1111-1111-1111-111111111110"), Name = "Aksesoris" }
        );

    }
}
