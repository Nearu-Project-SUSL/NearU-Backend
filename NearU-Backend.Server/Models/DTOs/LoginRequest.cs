using System.ComponentModel.DataAnnotations;

namespace NearU_Backend.Server.Models.DTOs;

/// <summary>
/// Request model for user login.
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "Username or email is required")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}
