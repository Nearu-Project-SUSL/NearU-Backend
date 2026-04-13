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

        // DbSets for entities
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<FoodShop> FoodShops { get; set; } = null!;
        public DbSet<Accommodation> Accommodations { get; set; } = null!;
        public DbSet<MenuItem> MenuItems { get; set; } = null!;
        public DbSet<AccommodationItem> AccommodationItems { get; set; } = null!;
        public DbSet<Job> Jobs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure RefreshToken entity
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(rt => rt.Id);

                // Configure Id as auto-increment (SERIAL in PostgreSQL, AUTOINCREMENT in SQLite)
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

                // Create index on Token for faster lookups
                entity.HasIndex(rt => rt.Token)
                    .IsUnique();

                // Create index on UserId for faster user token queries
                entity.HasIndex(rt => rt.UserId);

                // Foreign key relationship with User
                entity.HasOne(rt => rt.User)
                    .WithMany(u => u.RefreshTokens)
                    .HasForeignKey(rt => rt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                // Add other User configurations here as needed
            });


            modelBuilder.Entity<Accommodation>(entity =>
            {
                entity.HasKey(acc => acc.Id);

                entity.Property(acc => acc.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(acc => acc.Description)
                    .HasMaxLength(500);

                entity.Property(acc => acc.Address)
                    .HasMaxLength(200);

                entity.Property(acc => acc.PhoneNumber)
                   .HasMaxLength(20);

                entity.Property(acc => acc.CreatedAt)
                    .HasDefaultValueSql("NOW()");

                entity.HasMany(acc => acc.AccommodationItems)
                    .WithOne(mi => mi.Accommodation)
                    .HasForeignKey(mi => mi.AccommodationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AccommodationItem>(entity =>
            {
                entity.HasKey(mi => mi.Id);

                entity.Property(mi => mi.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(mi => mi.Description)
                    .HasMaxLength(300);

                entity.Property(mi => mi.Price)
                    .IsRequired()
                    .HasColumnType("decimal(10,2)");

                entity.Property(mi => mi.PhotoUrl)
                    .HasMaxLength(500);

                entity.HasIndex(mi => mi.AccommodationId);
            });

            modelBuilder.Entity<FoodShop>(entity =>
            {
                entity.HasKey(fs => fs.Id);

                entity.Property(fs => fs.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(fs => fs.Description)
                    .HasMaxLength(500);

                entity.Property(fs => fs.Address)
                    .HasMaxLength(200);

                entity.Property(fs => fs.PhoneNumber)
                   .HasMaxLength(20);

                entity.Property(fs => fs.CreatedAt)
                    .HasDefaultValueSql("NOW()");

                entity.HasMany(fs => fs.MenuItems)
                    .WithOne(mi => mi.FoodShop)
                    .HasForeignKey(mi => mi.FoodShopId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<MenuItem>(entity =>
            {
                entity.HasKey(mi => mi.Id);

                entity.Property(mi => mi.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(mi => mi.Description)
                    .HasMaxLength(300);

                entity.Property(mi => mi.Price)
                    .IsRequired()
                    .HasColumnType("decimal(10,2)");

                entity.Property(mi => mi.PhotoUrl)
                    .HasMaxLength(500);

                entity.HasIndex(mi => mi.FoodShopId);
            });

            modelBuilder.Entity<Job>(entity =>
            {
                entity.HasKey(j => j.Id);

                entity.Property(j => j.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(j => j.Company)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(j => j.Location)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(j => j.PayRange)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(j => j.JobType)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(j => j.Category)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(j => j.Description)
                    .HasMaxLength(500);

                entity.Property(j => j.LongDescription)
                    .HasMaxLength(2000);

                entity.Property(j => j.PostedByName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(j => j.PostedByUserId)
                    .IsRequired();

                entity.Property(j => j.CreatedAt)
                    .HasDefaultValueSql("NOW()");

                entity.HasOne(j => j.PostedByUser)
                    .WithMany()
                    .HasForeignKey(j => j.PostedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(j => j.Category);
                entity.HasIndex(j => j.IsNew);
                entity.HasIndex(j => j.CreatedAt);
                entity.HasIndex(j => j.PostedByUserId);
            });
        }
    }
}
