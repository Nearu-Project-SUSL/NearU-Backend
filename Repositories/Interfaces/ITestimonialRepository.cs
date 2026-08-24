using NearU_Backend_Revised.Models;

namespace NearU_Backend_Revised.Repositories.Interfaces
{
  public interface ITestimonialRepository
  {
    Task<IEnumerable<Testimonial>> GetAllApprovedAsync();
    Task<Testimonial?> CreateAsync(Testimonial testimonial);
    Task<Testimonial?> GetByIdAsync(int id);
    Task<bool> DeleteAsync(int id);
  }
}