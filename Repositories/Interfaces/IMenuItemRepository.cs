using NearU_Backend_Revised.Models;

namespace NearU_Backend_Revised.Repositories.Interfaces
{
    public interface IMenuItemRepository
    {
        Task<IEnumerable<MenuItem>> GetByShopIdAsync(string shopId);
        Task<MenuItem?> GetByIdAsync(string id);
        Task<MenuItem> CreateAsync(MenuItem item);
        Task<MenuItem?> UpdateAsync(MenuItem item);
        Task<bool> DeleteAsync(string id);
    }
}