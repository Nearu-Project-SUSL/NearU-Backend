using Microsoft.EntityFrameworkCore;
using NearU_Backend_Revised.Data;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories.Interfaces;

namespace NearU_Backend_Revised.Repositories
{
    public class AccommodationItemRepository : IAccommodationItemRepository
    {
        private readonly ApplicationDbContext _context;
        
        public AccommodationItemRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AccommodationItem>> GetByAccommodationIdAsync(string accommodationId)
        {
            return await _context.AccommodationItems
                .Where(ai => ai.AccommodationId == accommodationId)
                .OrderBy(ai => ai.Name)
                .ToListAsync();
        }

        public async Task<AccommodationItem?> GetByIdAsync(string id)
        {
            return await _context.AccommodationItems
                .FirstOrDefaultAsync(ai => ai.Id == id);
        }

        public async Task<AccommodationItem> CreateAsync(AccommodationItem item)
        {
            _context.AccommodationItems.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<AccommodationItem?> UpdateAsync(AccommodationItem item)
        {
            _context.AccommodationItems.Update(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var item = await _context.AccommodationItems.FindAsync(id);
            if (item == null) return false;

            _context.AccommodationItems.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
