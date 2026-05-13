using Microsoft.EntityFrameworkCore;
using NearU_Backend_Revised.Data;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories.Interfaces;

namespace NearU_Backend_Revised.Repositories
{
    public class GiftShopRepository : IGiftShopRepository
    {
        private readonly ApplicationDbContext _context;

        public GiftShopRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<GiftShop>> GetAllAsync(string? keyword, string? location, bool? isActive)
        {
            IQueryable<GiftShop> query = _context.GiftShops
                .Include(g => g.Products)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(g => g.Name.ToLower().Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                location = location.Trim().ToLower();
                query = query.Where(g => g.LocationName.ToLower().Contains(location));
            }

            if (isActive.HasValue)
            {
                query = query.Where(g => g.IsActive == isActive.Value);
            }

            return await query
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();
        }

        public async Task<GiftShop?> GetByIdAsync(Guid id)
        {
            return await _context.GiftShops
                .Include(g => g.Products)
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task<GiftProduct?> GetProductByIdAsync(Guid productId)
        {
            return await _context.GiftProducts.FirstOrDefaultAsync(p => p.Id == productId);
        }

        public async Task AddGiftShopAsync(GiftShop giftShop)
        {
            await _context.GiftShops.AddAsync(giftShop);
        }

        public async Task AddProductAsync(GiftProduct product)
        {
            await _context.GiftProducts.AddAsync(product);
        }

        public void UpdateGiftShop(GiftShop giftShop)
        {
            _context.GiftShops.Update(giftShop);
        }

        public void UpdateProduct(GiftProduct product)
        {
            _context.GiftProducts.Update(product);
        }

        public void DeleteGiftShop(GiftShop giftShop)
        {
            _context.GiftShops.Remove(giftShop);
        }

        public void DeleteProduct(GiftProduct product)
        {
            _context.GiftProducts.Remove(product);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
