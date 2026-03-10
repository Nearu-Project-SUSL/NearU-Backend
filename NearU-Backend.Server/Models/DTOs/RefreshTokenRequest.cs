using System.ComponentModel.DataAnnotations;

namespace NearU_Backend.Server.Models.DTOs;

/// <summary>
/// Request model for refreshing access token.
/// </summary>
public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}
