using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories.Interfaces;
using NearU_Backend_Revised.Services.Interfaces;

namespace NearU_Backend_Revised.Services
{
  public class TestimonialService : ITestimonialService
  {
    private readonly ITestimonialRepository _repo;

    public TestimonialService(ITestimonialRepository repo)
    {
      _repo = repo;
    }

    public async Task<IEnumerable<object>> GetAllApprovedAsync()
    {
      var testimonials = await _repo.GetAllApprovedAsync();

      return testimonials.Select(t => new
      {
        t.Id,
        t.Message,
        t.Rating,
        t.CreatedAt,
        UserName = t.User.Username,    
        UserEmail = t.User.Email   
      });
    }

    public async Task<Testimonial> CreateAsync(string userId, string message, int rating)
    {
      var testimonial = new Testimonial
      {
        UserId = userId,
        Message = message,
        Rating = rating,
        IsApproved = true,
        CreatedAt = DateTime.UtcNow
      };

      return await _repo.CreateAsync(testimonial);
    }

    public async Task<bool> DeleteAsync(int id, string requestingUserId)
    {
      var testimonial = await _repo.GetByIdAsync(id);
      if (testimonial == null) return false;
      if (testimonial.UserId != requestingUserId) 
        throw new UnauthorizedAccessException("Cannot delete another user's testimonial");

      return await _repo.DeleteAsync(id);
    }
  }
}