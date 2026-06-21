using System.ComponentModel.DataAnnotations;

namespace NearU_Backend_Revised.DTOs.User
{
    public class DeleteAccountRequest
    {
        [Required(ErrorMessage = "Password is required to delete the account.")]
        public string Password { get; set; } = null!;
    }
}
