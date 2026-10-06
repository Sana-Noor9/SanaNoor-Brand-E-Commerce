using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SanaNoor_Brand.Models;

namespace SanaNoor_Brand.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ============ DbSets ============
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductColor> ProductColors { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductVideo> ProductVideos { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Discount> Discounts { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<Contact> Contacts { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============ PRODUCT ============
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.SKU)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .Property(p => p.Availability)
                .HasColumnType("nvarchar(20)")
                .HasDefaultValue("in-stock");

            // Decimal precision
            modelBuilder.Entity<Product>()
                .Property(p => p.ShirtLength)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Product>()
                .Property(p => p.TrouserLength)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Product>()
                .Property(p => p.DupattaLength)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Product>()
                .Property(p => p.Weight)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Product>()
                .Property(p => p.ShirtQuantity)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Product>()
                .Property(p => p.TrouserQuantity)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Product>()
                .Property(p => p.DupattaQuantity)
                .HasColumnType("decimal(18,2)");

            // ============ PRODUCT COLOR ============
            modelBuilder.Entity<ProductColor>()
                .HasIndex(pc => new { pc.ProductId, pc.ColorName })
                .IsUnique();

            // ============ DISCOUNT ============
            modelBuilder.Entity<Discount>()
                .HasIndex(d => d.ProductId)
                .IsUnique();

            // ============ CART ============
            modelBuilder.Entity<Cart>()
        .HasIndex(c => new { c.UserId, c.ProductId, c.ProductColorId, c.Size })
        .IsUnique();

            modelBuilder.Entity<Cart>()
                .HasOne(c => c.Product)
                .WithMany()
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cart>()
                .HasOne(c => c.ProductColor)
                .WithMany(pc => pc.Carts)
                .HasForeignKey(c => c.ProductColorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cart>()
         .HasOne(c => c.User)
         .WithMany()
         .HasForeignKey(c => c.UserId)
         .OnDelete(DeleteBehavior.Restrict);

            // ============ ORDER ITEM ============
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.ProductColor)
                .WithMany(pc => pc.OrderItems)
                .HasForeignKey(oi => oi.ProductColorId)
                .OnDelete(DeleteBehavior.Restrict);

            // ============ ORDER ============
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderNumber)
                .IsUnique();

            // ============ WISHLIST ============
            modelBuilder.Entity<Wishlist>()
                .HasIndex(w => new { w.UserId, w.ProductId })
                .IsUnique();

            // ============ 🔴 FIX: CATEGORY - MISSING COLUMNS ADD KARO ============
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.CategoryId);

                entity.Property(c => c.CategoryName)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("nvarchar(100)");

                entity.Property(c => c.Slug)
                    .IsRequired()
                    .HasMaxLength(150)
                    .HasColumnType("nvarchar(150)");

                entity.HasIndex(c => c.Slug)
                    .IsUnique();

                entity.Property(c => c.IsActive)
                    .HasDefaultValue(true);

                entity.Property(c => c.DisplayOrder)
                    .HasDefaultValue(0);

                entity.Property(c => c.CreatedAt)
                    .HasDefaultValueSql("GETDATE()");
            });

            // ============ 🔴 FIX: PRODUCT IMAGE - MISSING COLUMNS ADD KARO ============
            modelBuilder.Entity<ProductImage>(entity =>
            {
                entity.HasKey(pi => pi.ProductImageId);

                entity.Property(pi => pi.ImagePath)
                    .IsRequired()
                    .HasColumnType("nvarchar(500)");

                entity.Property(pi => pi.DisplayOrder)
                    .HasDefaultValue(0);

                entity.Property(pi => pi.IsPrimary)
                    .HasDefaultValue(false);
            });

            // ============ REVIEW ============
            modelBuilder.Entity<Review>()
                .HasIndex(r => new { r.ProductId, r.UserId });

            // ============ PRODUCT VIDEO ============
            modelBuilder.Entity<ProductVideo>()
                .HasIndex(pv => pv.ProductId);
        }
    }
}