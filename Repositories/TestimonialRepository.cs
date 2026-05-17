using Microsoft.EntityFrameworkCore;
using NearU_Backend_Revised.Data;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories.Interfaces;

namespace NearU_Backend_Revised.Repositories
{
  public class TestimonialRepository : ITestimonialRepository
  {
    private readonly ApplicationDbContext _context;

    public TestimonialRepository(ApplicationDbContext context)
    {
      _context = context;
    }

    public async Task<IEnumerable<Testimonial>> GetAllApprovedAsync()
    {
      return await _context.Testimonials
        .Where(t => t.IsApproved)
        .OrderByDescending(t => t.CreatedAt)
        .Include(t => t.User)
        .ToListAsync();
    }

    public async Task<Testimonial?> CreateAsync(Testimonial testimonial)
    {
      _context.Testimonials.Add(testimonial);
      await _context.SaveChangesAsync();
      return testimonial;
    }

    public async Task<Testimonial?> GetByIdAsync(int id)
    {
      return await _context.Testimonials.FindAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
      var t = await _context.Testimonials.FindAsync(id);
      
      if (t == null) 
        return false;

      _context.Testimonials.Remove(t);
      await _context.SaveChangesAsync();
      return true;
    }
  }
}