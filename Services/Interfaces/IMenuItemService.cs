using NearU_Backend_Revised.DTOs.MenuItem;

namespace NearU_Backend_Revised.Services.Interfaces
{
    public interface IMenuItemService
    {
        Task<IEnumerable<MenuItemResponse>> GetItemsByShopAsync(string shopId);
        Task<MenuItemResponse?> GetItemByIdAsync(string id);
        Task<MenuItemResponse?> CreateItemAsync(string shopId,CreateMenuItem dto);
        Task<MenuItemResponse?> UpdateItemAsync(string id, UpdateMenuItem dto);
        Task<bool> DeleteItemAsync(string id);
    }
}