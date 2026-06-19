using NearU_Backend_Revised.DTOs;

namespace NearU_Backend_Revised.Services.Interfaces
{
    public interface ITrainRouteService
    {
        Task<List<TrainRouteDto>> GetAllAsync();
        Task<TrainRouteDto?> GetByIdAsync(int id);
        Task<TrainRouteDto> CreateAsync(TrainRouteUpdateDto dto);
        Task<TrainRouteDto?> UpdateAsync(int id, TrainRouteUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}