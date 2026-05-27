using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NearU_Backend_Revised.Models;

/// <summary>
/// Stores FCM push notification tokens on a per-user-per-device basis.
/// One user may have multiple tokens (phone + tablet, etc.).
/// </summary>
public class UserFcmToken
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string UserId { get; set; } = null!;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    /// <summary>The FCM registration token issued by Firebase SDK on the client device.</summary>
    [Required]
    [MaxLength(512)]
    public string Token { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Refreshed every time the client sends a new token (Firebase token rotation).</summary>
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
}
