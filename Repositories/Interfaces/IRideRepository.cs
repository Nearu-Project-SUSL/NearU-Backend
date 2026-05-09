using NearU_Backend_Revised.Models;

namespace NearU_Backend_Revised.Repositories.Interfaces
{
    public interface IRideRepository
    {
        Task<RideRequest> CreateAsync(RideRequest ride);
        Task<RideRequest?> GetByIdAsync(Guid id);
        Task<RideRequest?> GetByIdWithLockAsync(Guid id); //for update
        Task<RideRequest?> UpdateAsync(RideRequest ride);
        Task<IEnumerable<RideRequest>> GetPendingOlderThanAsync(DateTime cutoff);
        Task<int> SaveChangesAsync();
    }
}