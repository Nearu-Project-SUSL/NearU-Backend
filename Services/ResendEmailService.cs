using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NearU_Backend_Revised.Configuration;
using NearU_Backend_Revised.Services.Interfaces;

namespace NearU_Backend_Revised.Services
{
    public class ResendEmailService : IEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly ResendSettings _settings;
        private readonly ILogger<ResendEmailService> _logger;

        public ResendEmailService(
            HttpClient httpClient,
            IOptions<ResendSettings> settings,
            ILogger<ResendEmailService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string plainTextContent, string htmlContent)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_settings.ApiKey))
                {
                    _logger.LogWarning("Resend ApiKey is not configured. Email will not be sent.");
                    return;
                }

                const string requestUri = "https://api.resend.com/emails";

                var payload = new
                {
                    from = $"{_settings.FromName} <{_settings.FromEmail}>",
                    to = new[] { toEmail },
                    subject = subject,
                    html = !string.IsNullOrWhiteSpace(htmlContent) ? htmlContent : plainTextContent,
                    text = plainTextContent
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

                _logger.LogInformation("Sending email to {ToEmail} with subject '{Subject}' using Resend", toEmail, subject);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to send email via Resend. Status: {StatusCode}, Body: {ResponseBody}", response.StatusCode, responseBody);
                    throw new Exception($"Failed to send email via Resend. Status: {response.StatusCode}");
                }

                _logger.LogInformation("Email sent successfully to {ToEmail} via Resend", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {ToEmail} via Resend", toEmail);
                throw;
            }
        }
    }
}
