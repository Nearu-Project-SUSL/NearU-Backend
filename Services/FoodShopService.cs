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

        public FoodShopService(IFoodShopRepository repository, IImageService imageService)
        {
            _repository = repository;
            _imageService = imageService;
        }

        public async Task<PagedResponse<FoodShopResponse>> GetAllShopsAsync(
            int page, 
            int pageSize, 
            string? category, 
            string? search)
        {
            var allshops = await _repository.GetAllAsync(); // get all from repo
            
            //apply category filter if provided
            if(!string.IsNullOrWhiteSpace(category) && category != "All")
            {
                allshops = allshops.Where(s => s.Category == category);
            }

            //apply search filter if provided
            // StringComparison.OrdinalIgnoreCase = case insensitive
            if(!string.IsNullOrWhiteSpace(search))
            {
                allshops = allshops.Where(s =>
                s.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (s.Description != null && s.Description.Contains(search, StringComparison.OrdinalIgnoreCase))
                );
            }

            var totalCount = allshops.Count(); //count after filter

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            //apply pagination - skip items from previous page
            var pagedShops = allshops
                .Skip((page-1) * pageSize)
                .Take(pageSize)
                .Select(shop => MapToResponse(shop));

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
                Id = Guid.NewGuid().ToString(), //generate a unique id
                OwnerId = foodShopData.OwnerId,
                Name = foodShopData.Name,
                Description = foodShopData.Description,
                Address = foodShopData.Address,
                PhoneNumber = foodShopData.PhoneNumber,
                PhotoUrl = photoUrl,
                Category = category,
                CreatedAt = DateTime.UtcNow,
            };

            var created = await _repository.CreateAsync(shop);
            return MapToResponse(created);
        }

        public async Task<FoodShopResponse?> UpdateShopAsync(string id, UpdateFoodShop foodShopData)
        {
            var shop = await _repository.GetByIdAsync(id);
            if (shop == null) return null;
             
            shop.Name = foodShopData.Name ?? shop.Name!; //use left if not null otherwise use right
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
            return MapToResponse(updated);
        }

        public async Task<bool> DeleteShopAsync(string id)
        {
            return await _repository.DeleteAsync(id);
        }

        private static FoodShopResponse MapToResponse(FoodShop shop) //taks a model and return a foodShopData
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
