using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using NearU_Backend_Revised.Configuration;
using NearU_Backend_Revised.Services.Interfaces;

namespace NearU_Backend_Revised.Services
{
    public class EmailService : IEmailService
    {
        private readonly SendGridSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<SendGridSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string plainTextContent, string htmlContent)
        {
            try
            {
                if (string.IsNullOrEmpty(_settings.ApiKey))
                {
                    _logger.LogWarning("SendGrid ApiKey is not configured. Email will not be sent.");
                    return;
                }

                var client = new SendGridClient(_settings.ApiKey);
                var from = new EmailAddress(_settings.FromEmail, _settings.FromName);
                var to = new EmailAddress(toEmail);
                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                
                _logger.LogInformation("Sending email to {ToEmail} with subject '{Subject}' using SendGrid", toEmail, subject);
                var response = await client.SendEmailAsync(msg);

                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Body.ReadAsStringAsync();
                    _logger.LogError("Failed to send email. Status code: {StatusCode}. Response: {Response}", response.StatusCode, responseBody);
                    throw new Exception($"Failed to send email via SendGrid. Status: {response.StatusCode}");
                }

                _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {ToEmail}", toEmail);
                throw;
            }
        }
    }
}
