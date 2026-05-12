using NearU_Backend_Revised.DTOs.AccommodationItem;

namespace NearU_Backend_Revised.Services.Interfaces
{
    public interface IAccommodationItemService
    {
        Task<IEnumerable<AccommodationItemResponse>> GetItemsByAccommodationAsync(string accommodationId);
        Task<AccommodationItemResponse?> GetItemByIdAsync(string id);
        Task<AccommodationItemResponse?> CreateItemAsync(string accommodationId,CreateAccommodationItem dto);
        Task<AccommodationItemResponse?> UpdateItemAsync(string id, UpdateAccommodationItem dto);
        Task<bool> DeleteItemAsync(string id);
    }
}
