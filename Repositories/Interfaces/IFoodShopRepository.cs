using NearU_Backend_Revised.Models;

namespace NearU_Backend_Revised.Repositories.Interfaces
{
    public interface IFoodShopRepository
    {
        Task<IEnumerable<FoodShop>> GetAllAsync(); //IEnumerable is a basic collection type , can loop but no add/remove
        Task<FoodShop?> GetByIdAsync(string id);
        Task<FoodShop> CreateAsync(FoodShop shop);
        Task<FoodShop?> UpdateAsync(FoodShop shop);
        Task<bool> DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
    }
}