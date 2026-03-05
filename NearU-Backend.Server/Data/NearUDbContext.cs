using Microsoft.EntityFrameworkCore;
using NearU_Backend.Server.Models;

namespace NearU_Backend.Server.Data;

/// <summary>
/// Database context for NearU application.
/// </summary>
public class NearUDbContext : DbContext
{
    public NearUDbContext(DbContextOptions<NearUDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.RoleId);

            entity.HasOne(e => e.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Role entity
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Configure RefreshToken entity
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ExpiresAt);

            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed Roles
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = UserRoles.Admin, Description = "System Administrator", CreatedAt = DateTime.UtcNow },
            new Role { Id = 2, Name = UserRoles.Student, Description = "Student User", CreatedAt = DateTime.UtcNow },
            new Role { Id = 3, Name = UserRoles.Business, Description = "Business User", CreatedAt = DateTime.UtcNow },
            new Role { Id = 4, Name = UserRoles.Rider, Description = "Delivery Rider", CreatedAt = DateTime.UtcNow }
        );
    }
}
