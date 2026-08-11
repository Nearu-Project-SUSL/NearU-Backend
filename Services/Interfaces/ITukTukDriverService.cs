using NearU_Backend_Revised.DTOs;

namespace NearU_Backend_Revised.Services.Interfaces
{
    public interface ITukTukDriverService
    {
        Task<List<TukTukDriverDto>> GetAllAsync();
        Task<TukTukDriverDto?> GetByIdAsync(int id);
        Task<TukTukDriverDto> CreateAsync(TukTukDriverUpdateDto dto);
        Task<TukTukDriverDto?> UpdateAsync(int id, TukTukDriverUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}