using  NearU_Backend_Revised.Models;

namespace NearU_Backend_Revised.Repositories.Interfaces
{
    public interface ITukTukDriverRepository
    {
        Task<List<TukTukDriver>> GetAllAsync();
        Task<TukTukDriver?> GetByIdAsync(int id);
        Task<TukTukDriver> AddAsync(TukTukDriver driver);
        Task<TukTukDriver?> UpdateAsync(TukTukDriver driver);
        Task<bool> DeleteAsync(int id);
    }
}