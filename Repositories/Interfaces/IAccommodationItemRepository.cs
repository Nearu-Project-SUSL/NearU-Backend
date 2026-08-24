using NearU_Backend_Revised.Models;

namespace NearU_Backend_Revised.Repositories.Interfaces
{
    public interface IAccommodationItemRepository
    {
        Task<IEnumerable<AccommodationItem>> GetByAccommodationIdAsync(string accommodationId);
        Task<AccommodationItem?> GetByIdAsync(string id);
        Task<AccommodationItem> CreateAsync(AccommodationItem item);
        Task<AccommodationItem?> UpdateAsync(AccommodationItem item);
        Task<bool> DeleteAsync(string id);
    }
}
