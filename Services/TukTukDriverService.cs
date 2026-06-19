using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories.Interfaces;
using NearU_Backend_Revised.Services.Interfaces;
using NearU_Backend_Revised.DTOs;


namespace NearU_Backend_Revised.Services
{
    public class TukTukDriverService : ITukTukDriverService
    {
        private readonly ITukTukDriverRepository _repository;

        public TukTukDriverService(ITukTukDriverRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<TukTukDriverDto>> GetAllAsync()
        {
            var drivers = await _repository.GetAllAsync();
            return drivers.Select(MapToDto).ToList();
        }

        public async Task<TukTukDriverDto?> GetByIdAsync(int id)
        {
            var driver = await _repository.GetByIdAsync(id);
            return driver == null ? null : MapToDto(driver);
        }

        public async Task<TukTukDriverDto> CreateAsync(TukTukDriverUpdateDto dto)
        {
            var driver = new TukTukDriver
            {
                Name = dto.Name,
                PhoneNumber = dto.PhoneNumber,
                PlateNumber = dto.PlateNumber,
                OperatingArea = dto.OperatingArea,
                Notes = dto.Notes
            };

            var created = await _repository.AddAsync(driver);
            return MapToDto(created);
        }

        public async Task<TukTukDriverDto?> UpdateAsync(int id, TukTukDriverUpdateDto dto)
        {
            var driver = new TukTukDriver
            {
                Id = id,
                Name = dto.Name,
                PhoneNumber = dto.PhoneNumber,
                PlateNumber = dto.PlateNumber,
                OperatingArea = dto.OperatingArea,
                Notes = dto.Notes
            };

            var updated = await _repository.UpdateAsync(driver);
            return updated == null ? null : MapToDto(updated);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        private static TukTukDriverDto MapToDto(TukTukDriver driver)
        {
            return new TukTukDriverDto
            {
                Id = driver.Id,
                Name = driver.Name,
                PhoneNumber = driver.PhoneNumber,
                PlateNumber = driver.PlateNumber,
                OperatingArea = driver.OperatingArea,
                Notes = driver.Notes
            };
        }
    }
}