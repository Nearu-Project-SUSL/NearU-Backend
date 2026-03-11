using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NearU_Backend.Server.Models;

/// <summary>
/// Represents a refresh token for JWT authentication with device tracking.
/// </summary>
[Table("RefreshTokens")]
public class RefreshToken
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [Required]
    [StringLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string Token { get; set; } = string.Empty;

    [Column(TypeName = "datetime2")]
    public DateTime ExpiresAt { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime2")]
    public DateTime? RevokedAt { get; set; }

    [StringLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string? ReplacedByToken { get; set; }

    [StringLength(255)]
    [Column(TypeName = "nvarchar(255)")]
    public string? ReasonRevoked { get; set; }

    [Required]
    [StringLength(45)]
    [Column(TypeName = "nvarchar(45)")]
    public string CreatedByIp { get; set; } = string.Empty;

    [StringLength(45)]
    [Column(TypeName = "nvarchar(45)")]
    public string? RevokedByIp { get; set; }

    [StringLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string? DeviceInfo { get; set; }

    [StringLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public string? UserAgent { get; set; }

    [NotMapped]
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    [NotMapped]
    public bool IsRevoked => RevokedAt != null;

    [NotMapped]
    public bool IsActive => !IsRevoked && !IsExpired;
}
