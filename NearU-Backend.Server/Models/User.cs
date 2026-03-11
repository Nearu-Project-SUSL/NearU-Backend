using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NearU_Backend.Server.Models;

/// <summary>
/// Represents a user in the NearU system.
/// </summary>
[Table("Users")]
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Column(TypeName = "nvarchar(50)")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [Column(TypeName = "nvarchar(255)")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [Column(TypeName = "nvarchar(255)")]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(20)]
    [Column(TypeName = "nvarchar(20)")]
    public string? PhoneNumber { get; set; }

    [Required]
    public int RoleId { get; set; }

    [ForeignKey("RoleId")]
    public virtual Role Role { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public bool EmailConfirmed { get; set; } = false;

    [Column(TypeName = "datetime2")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime2")]
    public DateTime? UpdatedAt { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime? LastLogin { get; set; }

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

/// <summary>
/// Represents a role in the system.
/// </summary>
[Table("Roles")]
public class Role
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Column(TypeName = "nvarchar(50)")]
    public string Name { get; set; } = string.Empty;

    [StringLength(255)]
    [Column(TypeName = "nvarchar(255)")]
    public string? Description { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}

/// <summary>
/// Defines available user roles in the NearU marketplace.
/// </summary>
public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Student = "Student";
    public const string Business = "Business";
    public const string Rider = "Rider";

    public static readonly string[] All = { Admin, Student, Business, Rider };

    public static bool IsValid(string role)
    {
        return All.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}
