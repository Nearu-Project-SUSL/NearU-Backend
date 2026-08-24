using NearU_Backend_Revised.DTOs.Accommodation;

namespace NearU_Backend_Revised.Services.Interfaces
{
    public interface IAccommodationService
    {
        Task<IEnumerable<AccommodationResponse>> GetAllAccommodationsAsync();
        Task<AccommodationResponse?> GetAccommodationByIdAsync(string id);
        Task<AccommodationResponse?> CreateAccommodationAsync(CreateAccommodation dto);
        Task<AccommodationResponse?> UpdateAccommodationAsync(string id, UpdateAccommodation dto);
        Task<bool> DeleteAccommodationAsync(string id);
    }
}
