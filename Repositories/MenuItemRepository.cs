using Microsoft.EntityFrameworkCore;
using NearU_Backend_Revised.Data;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories.Interfaces;

namespace NearU_Backend_Revised.Repositories
{
    public class MenuItemRepository : IMenuItemRepository
    {
        private readonly ApplicationDbContext _context;
        
        public MenuItemRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MenuItem>> GetByShopIdAsync(string shopId)
        {
            return await _context.MenuItems
                .Where(mi => mi.FoodShopId == shopId)
                .OrderBy(mi => mi.Name)
                .ToListAsync();
        }

        public async Task<MenuItem?> GetByIdAsync(string id)
        {
            return await _context.MenuItems
                .FirstOrDefaultAsync(mi => mi.Id == id);
        }

        public async Task<MenuItem> CreateAsync(MenuItem item)
        {
            _context.MenuItems.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<MenuItem?> UpdateAsync(MenuItem item)
        {
            _context.MenuItems.Update(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null) return false;

            _context.MenuItems.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
