using NearU_Backend_Revised.DTOs;

namespace NearU_Backend_Revised.Services.Interfaces
{
    public interface IBusRouteService
    {
        Task<List<BusRouteDto>> GetAllAsync();
        Task<BusRouteDto?> GetByIdAsync(int id);
        Task<BusRouteDto> CreateAsync(BusRouteUpdateDto dto);
        Task<BusRouteDto?> UpdateAsync(int id, BusRouteUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}