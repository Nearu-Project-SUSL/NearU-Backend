using NearU_Backend_Revised.DTOs.Photographer;
using NearU_Backend_Revised.DTOs.PhotographyPackage;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories.Interfaces;
using NearU_Backend_Revised.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NearU_Backend_Revised.Services
{
    public class PhotographerService : IPhotographerService
    {
        private readonly IPhotographerRepository _photographerRepository;
        private readonly IImageService _imageService;
        private readonly ICacheService _cache;

        private const string AllPhotographersCacheKey = "nearu:photographers:all";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        public PhotographerService(
            IPhotographerRepository photographerRepository,
            IImageService imageService,
            ICacheService cache)
        {
            _photographerRepository = photographerRepository;
            _imageService = imageService;
            _cache = cache;
        }

        public async Task<IEnumerable<PhotographerResponseDto>> GetAllAsync(string? keyword, string? location, bool? isActive)
        {
            var cached = await _cache.GetAsync<List<PhotographerResponseDto>>(AllPhotographersCacheKey);
            if (cached is not null)
            {
                return ApplyFilters(cached, keyword, location, isActive);
            }

            var photographers = await _photographerRepository.GetAllAsync(null, null, null);
            var all = photographers.Select(MapPhotographerToResponse).ToList();

            await _cache.SetAsync(AllPhotographersCacheKey, all, CacheTtl);

            return ApplyFilters(all, keyword, location, isActive);
        }

        private static IEnumerable<PhotographerResponseDto> ApplyFilters(
            IEnumerable<PhotographerResponseDto> source,
            string? keyword,
            string? location,
            bool? isActive)
        {
            if (!string.IsNullOrWhiteSpace(keyword))
                source = source.Where(p =>
                    p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (p.Bio != null && p.Bio.Contains(keyword, StringComparison.OrdinalIgnoreCase)));

            if (!string.IsNullOrWhiteSpace(location))
                source = source.Where(p =>
                    p.LocationName != null &&
                    p.LocationName.Contains(location, StringComparison.OrdinalIgnoreCase));

            if (isActive.HasValue)
                source = source.Where(p => p.IsActive == isActive.Value);

            return source;
        }

        public async Task<PhotographerResponseDto?> GetByIdAsync(Guid id)
        {
            var photographer = await _photographerRepository.GetByIdAsync(id);
            return photographer == null ? null : MapPhotographerToResponse(photographer);
        }

        public async Task<PhotographerResponseDto> CreatePhotographerAsync(CreatePhotographerDto dto, string? ownerId = null)
        {
            string? uploadedImageUrl = null;

            if (dto.Image != null)
            {
                uploadedImageUrl = await _imageService.UploadImageAsync(dto.Image, "/photographers");
            }

            var photographer = new Photographer
            {
                Name = dto.Name.Trim(),
                Bio = dto.Bio?.Trim(),
                BaseRatePerHour = dto.BaseRatePerHour,
                LocationName = dto.LocationName.Trim(),
                Phone = dto.Phone.Trim(),
                Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
                ImageUrl = uploadedImageUrl,
                IsActive = true,
                OwnerId = ownerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _photographerRepository.AddPhotographerAsync(photographer);
            await _photographerRepository.SaveChangesAsync();

            var created = await _photographerRepository.GetByIdAsync(photographer.Id);

            await _cache.RemoveAsync(AllPhotographersCacheKey);

            return MapPhotographerToResponse(created!);
        }

        public async Task<PhotographerResponseDto?> UpdatePhotographerAsync(Guid id, UpdatePhotographerDto dto)
        {
            var photographer = await _photographerRepository.GetByIdAsync(id);
            if (photographer == null) return null;

            if (dto.Image != null)
            {
                var uploadedImageUrl = await _imageService.UploadImageAsync(dto.Image, "/photographers");
                if (!string.IsNullOrWhiteSpace(uploadedImageUrl))
                {
                    photographer.ImageUrl = uploadedImageUrl;
                }
            }

            photographer.Name = dto.Name.Trim();
            photographer.Bio = dto.Bio?.Trim();
            photographer.BaseRatePerHour = dto.BaseRatePerHour;
            photographer.LocationName = dto.LocationName.Trim();
            photographer.Phone = dto.Phone.Trim();
            photographer.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
            photographer.IsActive = dto.IsActive;
            photographer.UpdatedAt = DateTime.UtcNow;

            _photographerRepository.UpdatePhotographer(photographer);
            await _photographerRepository.SaveChangesAsync();

            await _cache.RemoveAsync(AllPhotographersCacheKey);

            var updated = await _photographerRepository.GetByIdAsync(id);
            return updated == null ? null : MapPhotographerToResponse(updated);
        }

        public async Task<bool> DeletePhotographerAsync(Guid id)
        {
            var photographer = await _photographerRepository.GetByIdAsync(id);
            if (photographer == null) return false;

            _photographerRepository.DeletePhotographer(photographer);
            var result = await _photographerRepository.SaveChangesAsync();

            await _cache.RemoveAsync(AllPhotographersCacheKey);

            return result;
        }

        public async Task<PhotographyPackageResponseDto?> AddPackageAsync(Guid photographerId, CreatePhotographyPackageDto dto)
        {
            var photographer = await _photographerRepository.GetByIdAsync(photographerId);
            if (photographer == null) return null;

            var package = new PhotographyPackage
            {
                PhotographerId = photographerId,
                Name = dto.Name.Trim(),
                Price = dto.Price,
                Description = dto.Description?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _photographerRepository.AddPackageAsync(package);
            await _photographerRepository.SaveChangesAsync();

            await _cache.RemoveAsync(AllPhotographersCacheKey);

            return MapPackageToResponse(package);
        }

        public async Task<PhotographyPackageResponseDto?> UpdatePackageAsync(Guid packageId, UpdatePhotographyPackageDto dto)
        {
            var package = await _photographerRepository.GetPackageByIdAsync(packageId);
            if (package == null) return null;

            package.Name = dto.Name.Trim();
            package.Price = dto.Price;
            package.Description = dto.Description?.Trim();
            package.IsActive = dto.IsActive;
            package.UpdatedAt = DateTime.UtcNow;

            _photographerRepository.UpdatePackage(package);
            await _photographerRepository.SaveChangesAsync();

            await _cache.RemoveAsync(AllPhotographersCacheKey);

            return MapPackageToResponse(package);
        }

        public async Task<bool> DeletePackageAsync(Guid packageId)
        {
            var package = await _photographerRepository.GetPackageByIdAsync(packageId);
            if (package == null) return false;

            _photographerRepository.DeletePackage(package);
            var result = await _photographerRepository.SaveChangesAsync();

            await _cache.RemoveAsync(AllPhotographersCacheKey);

            return result;
        }

        private static PhotographerResponseDto MapPhotographerToResponse(Photographer photographer)
        {
            return new PhotographerResponseDto
            {
                Id = photographer.Id,
                Name = photographer.Name,
                Bio = photographer.Bio,
                BaseRatePerHour = photographer.BaseRatePerHour,
                LocationName = photographer.LocationName,
                Phone = photographer.Phone,
                Email = photographer.Email,
                ImageUrl = photographer.ImageUrl,
                IsActive = photographer.IsActive,
                OwnerId = photographer.OwnerId,
                CreatedAt = photographer.CreatedAt,
                UpdatedAt = photographer.UpdatedAt,
                Packages = photographer.Packages?
                    .OrderByDescending(pp => pp.CreatedAt)
                    .Select(MapPackageToResponse)
                    .ToList() ?? new List<PhotographyPackageResponseDto>()
            };
        }

        private static PhotographyPackageResponseDto MapPackageToResponse(PhotographyPackage package)
        {
            return new PhotographyPackageResponseDto
            {
                Id = package.Id,
                PhotographerId = package.PhotographerId,
                Name = package.Name,
                Price = package.Price,
                Description = package.Description,
                IsActive = package.IsActive,
                CreatedAt = package.CreatedAt,
                UpdatedAt = package.UpdatedAt
            };
        }
    }
}
