using  NearU_Backend_Revised.Models;

namespace NearU_Backend_Revised.Repositories.Interfaces
{
    public interface IBusRouteRepository
    {
        Task<List<BusRoute>> GetAllAsync();
        Task<BusRoute?> GetByIdAsync(int id);
        Task<BusRoute> AddAsync(BusRoute route);
        Task<BusRoute?> UpdateAsync(BusRoute route);
        Task<bool> DeleteAsync(int id);
    }
}