using NearU_Backend_Revised.Models;

namespace NearU_Backend_Revised.Repositories.Interfaces
{
    public interface IGiftShopRepository
    {
        Task<List<GiftShop>> GetAllAsync(string? keyword, string? location, bool? isActive);
        Task<GiftShop?> GetByIdAsync(Guid id);
        Task<GiftProduct?> GetProductByIdAsync(Guid productId);
        Task AddGiftShopAsync(GiftShop giftShop);
        Task AddProductAsync(GiftProduct product);
        void UpdateGiftShop(GiftShop giftShop);
        void UpdateProduct(GiftProduct product);
        void DeleteGiftShop(GiftShop giftShop);
        void DeleteProduct(GiftProduct product);
        Task<bool> SaveChangesAsync();
    }
}