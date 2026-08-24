using Microsoft.EntityFrameworkCore;
using NearU_Backend_Revised.Data;
using  NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories.Interfaces;

namespace NearU_Backend_Revised.Repositories
{
    public class TukTukDriverRepository : ITukTukDriverRepository
    {
        private readonly ApplicationDbContext _context;

        public TukTukDriverRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<TukTukDriver>> GetAllAsync()
        {
            return await _context.TukTukDrivers
                .AsNoTracking()
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        public async Task<TukTukDriver?> GetByIdAsync(int id)
        {
            return await _context.TukTukDrivers
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<TukTukDriver> AddAsync(TukTukDriver driver)
        {
            _context.TukTukDrivers.Add(driver);
            await _context.SaveChangesAsync();
            return driver;
        }

        public async Task<TukTukDriver?> UpdateAsync(TukTukDriver driver)
        {
            var existing = await _context.TukTukDrivers.FindAsync(driver.Id);
            if (existing == null) return null;

            existing.Name = driver.Name;
            existing.PhoneNumber = driver.PhoneNumber;
            existing.PlateNumber = driver.PlateNumber;
            existing.OperatingArea = driver.OperatingArea;
            existing.Notes = driver.Notes;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _context.TukTukDrivers.FindAsync(id);
            if (existing == null) return false;

            _context.TukTukDrivers.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}