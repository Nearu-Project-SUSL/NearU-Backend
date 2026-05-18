namespace NearU_Backend_Revised.Configuration
{
    public class SendGridSettings
    {
        public string ApiKey { get; set; } = null!;
        public string FromEmail { get; set; } = null!;
        public string FromName { get; set; } = null!;
    }
}
