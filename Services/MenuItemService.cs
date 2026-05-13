using NearU_Backend_Revised.DTOs.MenuItem;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories.Interfaces;
using NearU_Backend_Revised.Services.Interfaces;

namespace NearU_Backend_Revised.Services
{
    public class MenuItemService : IMenuItemService
    {
        private readonly IMenuItemRepository _itemrepository;
        private readonly IFoodShopRepository _shoprepository;
        private readonly IImageService _imageService;

        public MenuItemService(IMenuItemRepository itemrepository, IFoodShopRepository shoprepository, IImageService imageService)
        {
            _itemrepository = itemrepository;
            _shoprepository = shoprepository;
            _imageService = imageService;
        }

        public async Task<IEnumerable<MenuItemResponse>> GetItemsByShopAsync(string shopId)
        {
            var items = await _itemrepository.GetByShopIdAsync(shopId);
            return items.Select(item => MapToResponse(item));
        }

        public async Task<MenuItemResponse?> GetItemByIdAsync(string id)
        {
            var item = await _itemrepository.GetByIdAsync(id);
            if (item == null) return null;
            return MapToResponse(item);
        }

        public async Task<MenuItemResponse?> CreateItemAsync(string shopId, CreateMenuItem menuItemData)
        {
            var shopExists = await _shoprepository.GetByIdAsync(shopId);
            if (shopExists == null) return null;

            string? photoUrl = null;
            if (menuItemData.Photo != null)
            {
                photoUrl = await _imageService.UploadImageAsync(menuItemData.Photo, "menuitems");
            }

            var item = new MenuItem
            {
                Id = Guid.NewGuid().ToString(),
                FoodShopId = shopId,
                Name = menuItemData.Name,
                Description = menuItemData.Description,
                Price = menuItemData.Price,
                PhotoUrl = photoUrl,
                CreatedAt = DateTime.UtcNow,

            };

            var created = await _itemrepository.CreateAsync(item);
            return MapToResponse(created);
        }

        public async Task<MenuItemResponse?> UpdateItemAsync(string id, UpdateMenuItem menuItemData)
        {
            var item = await _itemrepository.GetByIdAsync(id);
            if (item == null) return null;

            item.Name = menuItemData.Name ?? item.Name;
            item.Description = menuItemData.Description ?? item.Description;
            item.Price = menuItemData.Price ?? item.Price;

            if (menuItemData.Photo != null)
            {
                item.PhotoUrl = await _imageService.UploadImageAsync(menuItemData.Photo, "menuitems");
            }

            var updated = await _itemrepository.UpdateAsync(item);
            if (updated == null) return null;
            return MapToResponse(updated);
        }

        public async Task<bool> DeleteItemAsync(string id)
        {
            return await _itemrepository.DeleteAsync(id);
        }

        private static MenuItemResponse MapToResponse(MenuItem item) // Made static for better performance
        {
            return new MenuItemResponse
            {
                Id = item.Id,
                FoodShopId = item.FoodShopId,
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                PhotoUrl = item.PhotoUrl,
            };
        }
    }
}

