using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NearU_Backend_Revised.DTOs.Photographer;
using NearU_Backend_Revised.DTOs.PhotographyPackage;

namespace NearU_Backend_Revised.Services.Interfaces
{
    public interface IPhotographerService
    {
        Task<IEnumerable<PhotographerResponseDto>> GetAllAsync(string? keyword, string? location, bool? isActive);
        Task<PhotographerResponseDto?> GetByIdAsync(Guid id);
        Task<PhotographerResponseDto> CreatePhotographerAsync(CreatePhotographerDto dto, string? ownerId = null);
        Task<PhotographerResponseDto?> UpdatePhotographerAsync(Guid id, UpdatePhotographerDto dto);
        Task<bool> DeletePhotographerAsync(Guid id);
        Task<PhotographyPackageResponseDto?> AddPackageAsync(Guid photographerId, CreatePhotographyPackageDto dto);
        Task<PhotographyPackageResponseDto?> UpdatePackageAsync(Guid packageId, UpdatePhotographyPackageDto dto);
        Task<bool> DeletePackageAsync(Guid packageId);
    }
}
