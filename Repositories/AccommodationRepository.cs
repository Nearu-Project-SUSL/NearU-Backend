using Microsoft.EntityFrameworkCore;
using NearU_Backend_Revised.Data;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories.Interfaces;

namespace NearU_Backend_Revised.Repositories
{
    public class AccommodationRepository : IAccommodationRepository
    {
        private readonly ApplicationDbContext _context;

        public AccommodationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Accommodation>> GetAllAsync()
        {
            return await _context.Accommodations
                .OrderByDescending(acc => acc.CreatedAt) //sort result newest first
                .ToListAsync(); //get result as a list
        }

        public async Task<Accommodation?> GetByIdAsync(string id)
        {
            return await _context.Accommodations
                .FirstOrDefaultAsync(acc => acc.Id == id);
        }

        public async Task<Accommodation> CreateAsync(Accommodation accommodation)
        {
            _context.Accommodations.Add(accommodation);
            await _context.SaveChangesAsync();
            return accommodation;
        }

        public async Task<Accommodation?> UpdateAsync(Accommodation accommodation)
        {
            _context.Accommodations.Update(accommodation);
            await _context.SaveChangesAsync();
            return accommodation;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var accommodation = await _context.Accommodations.FindAsync(id); //faster than firstordefault for primary key
            if (accommodation == null) return false;

            _context.Accommodations.Remove(accommodation);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(string id)
        {
            return await _context.Accommodations
                .AnyAsync(acc => acc.Id == id); //return true if any accommodation exists with the given id
        }
    }
}
