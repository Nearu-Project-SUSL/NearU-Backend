using System.Threading.Tasks;

namespace NearU_Backend_Revised.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string plainTextContent, string htmlContent);
    }
}
