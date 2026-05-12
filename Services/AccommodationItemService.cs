using NearU_Backend_Revised.DTOs.AccommodationItem;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories.Interfaces;
using NearU_Backend_Revised.Services.Interfaces;

namespace NearU_Backend_Revised.Services
{
    public class AccommodationItemService : IAccommodationItemService
    {
        private readonly IAccommodationItemRepository _itemrepository;
        private readonly IAccommodationRepository _accommodationrepository;
        private readonly IImageService _imageService;

        public AccommodationItemService(IAccommodationItemRepository itemrepository, IAccommodationRepository accommodationrepository, IImageService imageService)
        {
            _itemrepository = itemrepository;
            _accommodationrepository = accommodationrepository;
            _imageService = imageService;
        }

        public async Task<IEnumerable<AccommodationItemResponse>> GetItemsByAccommodationAsync(string accommodationId)
        {
            var items = await _itemrepository.GetByAccommodationIdAsync(accommodationId);
            return items.Select(item => MapToResponse(item));
        }

        public async Task<AccommodationItemResponse?> GetItemByIdAsync(string id)
        {
            var item = await _itemrepository.GetByIdAsync(id);
            if (item == null) return null;
            return MapToResponse(item);
        }

        public async Task<AccommodationItemResponse?> CreateItemAsync(string accommodationId, CreateAccommodationItem AccommodationItemData)
        {
            var accommodationExists = await _accommodationrepository.GetByIdAsync(accommodationId);
            if (accommodationExists == null) return null;

            string? photoUrl = null;
            if (AccommodationItemData.Photo != null)
            {
                photoUrl = await _imageService.UploadImageAsync(AccommodationItemData.Photo, "AccommodationItems");
            }

            var item = new AccommodationItem
            {
                Id = Guid.NewGuid().ToString(),
                AccommodationId = accommodationId,
                Name = AccommodationItemData.Name,
                Description = AccommodationItemData.Description,
                Price = AccommodationItemData.Price,
                PhotoUrl = photoUrl,
                CreatedAt = DateTime.UtcNow,

            };

            var created = await _itemrepository.CreateAsync(item);
            return MapToResponse(created);
        }

        public async Task<AccommodationItemResponse?> UpdateItemAsync(string id, UpdateAccommodationItem AccommodationItemData)
        {
            var item = await _itemrepository.GetByIdAsync(id);
            if (item == null) return null;

            item.Name = !string.IsNullOrWhiteSpace(AccommodationItemData.Name) ? AccommodationItemData.Name : item.Name;
            item.Description = AccommodationItemData.Description ?? item.Description;
            item.Price = AccommodationItemData.Price ?? item.Price;

            if (AccommodationItemData.Photo != null)
            {
                item.PhotoUrl = await _imageService.UploadImageAsync(AccommodationItemData.Photo, "AccommodationItems");
            }

            var updated = await _itemrepository.UpdateAsync(item);
            if (updated == null) return null;
            return MapToResponse(updated);
        }

        public async Task<bool> DeleteItemAsync(string id)
        {
            return await _itemrepository.DeleteAsync(id);
        }

        private static AccommodationItemResponse MapToResponse(AccommodationItem item) // Made static for better performance
        {
            return new AccommodationItemResponse
            {
                Id = item.Id,
                AccommodationId = item.AccommodationId,
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                PhotoUrl = item.PhotoUrl,
            };
        }
    }
}

