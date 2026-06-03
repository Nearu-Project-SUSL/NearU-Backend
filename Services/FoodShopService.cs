using NearU_Backend_Revised.DTOs.FoodShop;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories.Interfaces;
using NearU_Backend_Revised.Services.Interfaces;
using NearU_Backend_Revised.Enums;

namespace NearU_Backend_Revised.Services
{
    public class FoodShopService : IFoodShopService
    {
        private readonly IFoodShopRepository _repository;
        private readonly IImageService _imageService;
        private readonly ICacheService _cache;

        // Cache key for the full shop list — shared across all filter/search operations
        private const string AllShopsCacheKey = "nearu:foodshops:all";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        public FoodShopService(IFoodShopRepository repository, IImageService imageService, ICacheService cache)
        {
            _repository = repository;
            _imageService = imageService;
            _cache = cache;
        }

        public async Task<PagedResponse<FoodShopResponse>> GetAllShopsAsync(
            int page, 
            int pageSize, 
            string? category, 
            string? search)
        {
            // Try cache first — the full unfiltered list is cached; filtering/paging happens in-memory
            var allShops = await _cache.GetAsync<List<FoodShopResponse>>(AllShopsCacheKey);

            if (allShops is null)
            {
                // Cache miss — fetch from DB and populate the cache
                var entities = await _repository.GetAllAsync();
                allShops = entities.Select(MapToResponse).ToList();
                await _cache.SetAsync(AllShopsCacheKey, allShops, CacheTtl);
            }

            // Apply filters in-memory on the cached list
            IEnumerable<FoodShopResponse> filtered = allShops;

            if (!string.IsNullOrWhiteSpace(category) && category != "All")
                filtered = filtered.Where(s => s.Category == category);

            if (!string.IsNullOrWhiteSpace(search))
                filtered = filtered.Where(s =>
                    s.Name != null && s.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (s.Description != null && s.Description.Contains(search, StringComparison.OrdinalIgnoreCase)));

            var filteredList = filtered.ToList();
            var totalCount = filteredList.Count;
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var pagedShops = filteredList
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            return new PagedResponse<FoodShopResponse>
            {
                Items = pagedShops,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }

        public async Task<FoodShopResponse?> GetShopByIdAsync(string id)
        {
            var shop = await _repository.GetByIdAsync(id);
            if (shop == null) return null; 
            return MapToResponse(shop);
        }

        public async Task<FoodShopResponse?> CreateShopAsync(CreateFoodShop foodShopData)
        {
            string? photoUrl = null;

            if (foodShopData.Photo != null)
            {
                photoUrl = await _imageService.UploadImageAsync(foodShopData.Photo, "foodshops");
            }

            var category = FoodCategory.IsValid(foodShopData.Category)
                    ? foodShopData.Category!
                    : FoodCategory.Default;

            var shop = new FoodShop
            {
                Id = Guid.NewGuid().ToString(),
                Name = foodShopData.Name,
                Description = foodShopData.Description,
                Address = foodShopData.Address,
                PhoneNumber = foodShopData.PhoneNumber,
                PhotoUrl = photoUrl,
                Category = category,
                CreatedAt = DateTime.UtcNow,
            };

            var created = await _repository.CreateAsync(shop);

            // Invalidate the list cache so the next GET fetches the updated data
            await _cache.RemoveAsync(AllShopsCacheKey);

            return MapToResponse(created);
        }

        public async Task<FoodShopResponse?> UpdateShopAsync(string id, UpdateFoodShop foodShopData)
        {
            var shop = await _repository.GetByIdAsync(id);
            if (shop == null) return null;
             
            shop.Name = foodShopData.Name ?? shop.Name!;
            shop.Description = foodShopData.Description ?? shop.Description;
            shop.Address = foodShopData.Address ?? shop.Address;
            shop.PhoneNumber = foodShopData.PhoneNumber ?? shop.PhoneNumber;

            if (FoodCategory.IsValid(foodShopData.Category))
            {
                shop.Category = foodShopData.Category!;
            }

            if (foodShopData.Photo != null)
            {
                shop.PhotoUrl = await _imageService.UploadImageAsync(foodShopData.Photo , "foodshops");
            }

            var updated = await _repository.UpdateAsync(shop);
            if (updated == null) return null;

            // Invalidate the list cache
            await _cache.RemoveAsync(AllShopsCacheKey);

            return MapToResponse(updated);
        }

        public async Task<bool> DeleteShopAsync(string id)
        {
            var result = await _repository.DeleteAsync(id);

            // Invalidate the list cache regardless of whether the delete succeeded
            await _cache.RemoveAsync(AllShopsCacheKey);

            return result;
        }

        private static FoodShopResponse MapToResponse(FoodShop shop)
        {
            return new FoodShopResponse
            {
                Id = shop.Id,
                Name = shop.Name,
                Description = shop.Description,
                Address = shop.Address,
                PhoneNumber = shop.PhoneNumber,
                PhotoUrl = shop.PhotoUrl,
                Category = shop.Category,
                CreatedAt = shop.CreatedAt,
            };
        }
    }
}
