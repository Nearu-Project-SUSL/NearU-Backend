namespace NearU_Backend_Revised.Models;

public class BusinessApplication
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public User User { get; set; } = null!;

    public string BusinessType { get; set; } = string.Empty;

    public string BusinessName { get; set; } = string.Empty;

    public string OwnerName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string RegistrationNumber { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending";

    public string ApplicationDataJson { get; set; } = "{}";

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}