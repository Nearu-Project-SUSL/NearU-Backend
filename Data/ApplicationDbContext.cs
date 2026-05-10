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
        
        // Ride & Tracking DbSets
        public DbSet<RiderStatus> RiderStatuses { get; set; } = null!;
        public DbSet<TrackingLog> TrackingLogs { get; set; } = null!;
        public DbSet<RideRequest> RideRequests { get; set; } = null!;
        public DbSet<RideHistory> RideHistories { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Enable PostGIS Extension
            modelBuilder.HasPostgresExtension("postgis");

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

            // Configure RiderStatus
            modelBuilder.Entity<RiderStatus>(entity =>
            {
                entity.HasKey(rs => rs.RiderId);
                
                entity.Property(rs => rs.ApprovalStatus)
                    .HasConversion<string>();

                entity.Property(rs => rs.RiderTier)
                    .HasConversion<string>();

                entity.Property(rs => rs.LastLocation)
                    .HasColumnType("geography(Point, 4326)");
            });

            // Configure RideRequest
            modelBuilder.Entity<RideRequest>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.Property(r => r.Status)
                    .HasConversion<string>();

                entity.Property(r => r.ServiceType)
                    .HasConversion<string>();

                entity.Property(r => r.Details)
                    .HasColumnType("jsonb");

                entity.Property(r => r.PriceRateSnapshot)
                    .HasColumnType("jsonb");

                entity.Property(r => r.PickupLocation)
                    .HasColumnType("geography(Point, 4326)");

                entity.Property(r => r.DropoffLocation)
                    .HasColumnType("geography(Point, 4326)");

                entity.Property(r => r.OTP)
                    .HasMaxLength(4)
                    .IsFixedLength();

                entity.Property(r => r.EstimatedFare)
                    .HasColumnType("decimal(10,2)");

                entity.Property(r => r.CalculatedDistance)
                    .HasColumnType("decimal(6,3)");

                entity.HasIndex(r => r.Status);
                entity.HasIndex(r => r.StudentId);
                entity.HasIndex(r => r.RiderId);
                entity.HasIndex(r => r.CreatedAt);

                entity.HasOne(r => r.Student)
                    .WithMany()
                    .HasForeignKey(r => r.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Rider)
                    .WithMany()
                    .HasForeignKey(r => r.RiderId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure TrackingLog
            modelBuilder.Entity<TrackingLog>(entity =>
            {
                entity.HasKey(t => t.Id);

                 entity.Property(t => t.Coordinates)
                    .HasColumnType("geography(Point, 4326)");

                entity.HasIndex(t => t.RideId);
                entity.HasIndex(t => t.Timestamp);

                entity.HasOne(t => t.RideRequest)
                    .WithMany(r => r.TrackingLogs)
                    .HasForeignKey(t => t.RideId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RideHistory>(entity =>
            {
                entity.HasKey(h => h.Id);

                entity.Property(h => h.ServiceType)
                    .HasConversion<string>();

                entity.Property(h => h.FinalFare)
                    .HasColumnType("decimal(10,2)");

                entity.Property(h => h.CalculatedDistance)
                    .HasColumnType("decimal(6,3)");

                entity.HasIndex(h => h.RideId)
                    .IsUnique();

                entity.HasIndex(h => h.CompletedAt);
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
