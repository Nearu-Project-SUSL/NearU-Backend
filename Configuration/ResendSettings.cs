namespace NearU_Backend_Revised.Configuration
{
    public class ResendSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string FromEmail { get; set; } = "onboarding@resend.dev";
        public string FromName { get; set; } = "NearU Support";
    }
}
