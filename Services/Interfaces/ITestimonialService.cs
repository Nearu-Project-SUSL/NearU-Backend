using NearU_Backend_Revised.Models;

namespace NearU_Backend_Revised.Services.Interfaces
{
  public interface ITestimonialService
  {
    Task<IEnumerable<object>> GetAllApprovedAsync();
    Task<Testimonial?> CreateAsync(string userId, string message, int rating);
    Task<bool> DeleteAsync(int id, string requestingUserId);
  }
}