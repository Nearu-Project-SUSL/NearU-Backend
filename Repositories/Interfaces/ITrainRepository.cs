using  NearU_Backend_Revised.Models;

namespace NearU_Backend_Revised.Repositories.Interfaces
{
    public interface ITrainRouteRepository
    {
        Task<List<TrainRoute>> GetAllAsync();
        Task<TrainRoute?> GetByIdAsync(int id);
        Task<TrainRoute> AddAsync(TrainRoute route);
        Task<TrainRoute?> UpdateAsync(TrainRoute route);
        Task<bool> DeleteAsync(int id);
    }
}