using NearU_Backend_Revised.Models;

namespace NearU_Backend_Revised.Repositories.Interfaces
{
    public interface IAccommodationRepository
    {
        Task<IEnumerable<Accommodation>> GetAllAsync(); //IEnumerable is a basic collection type , can loop but no add/remove
        Task<Accommodation?> GetByIdAsync(string id);
        Task<Accommodation> CreateAsync(Accommodation accommodation);
        Task<Accommodation?> UpdateAsync(Accommodation accommodation);
        Task<bool> DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
    }
}
