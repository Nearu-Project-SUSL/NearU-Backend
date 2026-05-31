using Microsoft.EntityFrameworkCore;
using NearU_Backend_Revised.Data;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories.Interfaces;

namespace NearU_Backend_Revised.Repositories
{
    public class FoodShopRepository : IFoodShopRepository
    {
        private readonly ApplicationDbContext _context;

        public FoodShopRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<FoodShop>> GetAllAsync()
        {
            return await _context.FoodShops
                .OrderByDescending(fs => fs.CreatedAt) //sort result newest first
                .ToListAsync(); //get result as a list
        }

        public async Task<FoodShop?> GetByIdAsync(string id)
        {
            return await _context.FoodShops

                .Include(fs => fs.MenuItems)  //also fetch menuitems when fetching shops
                .FirstOrDefaultAsync(fs => fs.Id == id);
        }

        public async Task<FoodShop> CreateAsync(FoodShop shop)
        {
            _context.FoodShops.Add(shop);
            await _context.SaveChangesAsync();
            return shop;
        }

        public async Task<FoodShop?> UpdateAsync(FoodShop shop)
        {
            _context.FoodShops.Update(shop);
            await _context.SaveChangesAsync();
            return shop;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var shop = await _context.FoodShops.FindAsync(id); //faster than firstordefault for primary key
            if (shop == null) return false;

            _context.FoodShops.Remove(shop);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(string id)
        {
            return await _context.FoodShops
                .AnyAsync(fs => fs.Id == id); //return true if any shop exists with the given id
        }
    }
}
