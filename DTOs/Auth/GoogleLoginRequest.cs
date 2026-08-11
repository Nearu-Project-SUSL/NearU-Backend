using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NearU_Backend_Revised.DTOs.Auth
{
    public class GoogleLoginRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }

    public class GoogleUserInfoPayload
    {
        [JsonPropertyName("sub")]
        public string Sub { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("picture")]
        public string Picture { get; set; } = string.Empty;
    }
}
