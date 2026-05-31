using NearU_Backend_Revised.DTOs.FoodShop;

namespace NearU_Backend_Revised.Services.Interfaces
{
    public interface IFoodShopService
    {
        Task<PagedResponse<FoodShopResponse>> GetAllShopsAsync(
            int page,
            int pageSize,
            string? category,
            string? search
        );
        Task<FoodShopResponse?> GetShopByIdAsync(string id);
        Task<FoodShopResponse?> CreateShopAsync(CreateFoodShop dto);
        Task<FoodShopResponse?> UpdateShopAsync(string id, UpdateFoodShop dto);
        Task<bool> DeleteShopAsync(string id);
    }
}