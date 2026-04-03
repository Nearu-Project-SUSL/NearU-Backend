using Microsoft.EntityFrameworkCore;
using NearU_Backend_Revised.Models;

namespace NearU_Backend_Revised.Data
{
    /// <summary>
    /// Application database context for managing entities
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Existing DbSets
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        // Add these new DbSets
        public DbSet<GiftShop> GiftShops { get; set; } = null!;
        public DbSet<GiftProduct> GiftProducts { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure RefreshToken entity
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(rt => rt.Id);

                entity.Property(rt => rt.Id)
                    .ValueGeneratedOnAdd();

                entity.Property(rt => rt.Token)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(rt => rt.UserId)
                    .IsRequired();

                entity.Property(rt => rt.ExpiryDate)
                    .IsRequired();

                entity.Property(rt => rt.CreatedDate)
                    .IsRequired();

                entity.Property(rt => rt.ReplacedByToken)
                    .HasMaxLength(500);

                entity.Property(rt => rt.ReasonRevoked)
                    .HasMaxLength(200);

                entity.HasIndex(rt => rt.Token)
                    .IsUnique();

                entity.HasIndex(rt => rt.UserId);

                entity.HasOne(rt => rt.User)
                    .WithMany(u => u.RefreshTokens)
                    .HasForeignKey(rt => rt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
            });

            // Configure GiftShop entity
            modelBuilder.Entity<GiftShop>(entity =>
            {
                entity.HasKey(gs => gs.Id);

                entity.Property(gs => gs.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(gs => gs.ImageUrl)
                    .HasMaxLength(500);

                entity.Property(gs => gs.LocationName)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(gs => gs.Phone)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(gs => gs.Email)
                    .HasMaxLength(150);

                entity.Property(gs => gs.Address)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(gs => gs.IsActive)
                    .HasDefaultValue(true);

                entity.Property(gs => gs.CreatedAt)
                    .IsRequired();

                entity.Property(gs => gs.UpdatedAt)
                    .IsRequired();
            });

            // Configure GiftProduct entity
            modelBuilder.Entity<GiftProduct>(entity =>
            {
                entity.HasKey(gp => gp.Id);

                entity.Property(gp => gp.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(gp => gp.PhotoUrl)
                    .HasMaxLength(500);

                entity.Property(gp => gp.Price)
                    .HasColumnType("numeric(18,2)");

                entity.Property(gp => gp.IsActive)
                    .HasDefaultValue(true);

                entity.Property(gp => gp.CreatedAt)
                    .IsRequired();

                entity.Property(gp => gp.UpdatedAt)
                    .IsRequired();

                entity.HasOne(gp => gp.GiftShop)
                    .WithMany(gs => gs.Products)
                    .HasForeignKey(gp => gp.GiftShopId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}