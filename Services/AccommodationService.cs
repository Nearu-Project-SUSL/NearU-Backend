using NearU_Backend_Revised.DTOs.Accommodation;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories.Interfaces;
using NearU_Backend_Revised.Services.Interfaces;

namespace NearU_Backend_Revised.Services
{
    public class AccommodationService : IAccommodationService
    {
        private readonly IAccommodationRepository _repository;
        private readonly IImageService _imageService;
        private readonly ICacheService _cache;

        private const string AllAccommodationsCacheKey = "nearu:accommodations:all";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        public AccommodationService(IAccommodationRepository repository, IImageService imageService, ICacheService cache)
        {
            _repository = repository;
            _imageService = imageService;
            _cache = cache;
        }

        public async Task<IEnumerable<AccommodationResponse>> GetAllAccommodationsAsync()
        {
            var cached = await _cache.GetAsync<List<AccommodationResponse>>(AllAccommodationsCacheKey);
            if (cached is not null)
                return cached;

            var accommodations = await _repository.GetAllAsync();
            var result = accommodations.Select(MapToResponse).ToList();

            await _cache.SetAsync(AllAccommodationsCacheKey, result, CacheTtl);
            return result;
        }

        public async Task<AccommodationResponse?> GetAccommodationByIdAsync(string id)
        {
            var accommodation = await _repository.GetByIdAsync(id);
            if (accommodation == null) return null; 
            return MapToResponse(accommodation);
        }

        public async Task<AccommodationResponse?> CreateAccommodationAsync(CreateAccommodation AccommodationData)
        {
            string? photoUrl = null;

            if (AccommodationData.Photo != null)
            {
                photoUrl = await _imageService.UploadImageAsync(AccommodationData.Photo, "Accommodations");
            }

            var accommodation = new Accommodation
            {
                Id = Guid.NewGuid().ToString(),
                Name = AccommodationData.Name,
                Description = AccommodationData.Description,
                Address = AccommodationData.Address,
                PhoneNumber = AccommodationData.PhoneNumber,
                PhotoUrl = photoUrl,
                Type = AccommodationData.Type ?? "Boarding",
                DistanceKm = AccommodationData.DistanceKm,
                MonthlyRent = AccommodationData.MonthlyRent,
                AvailableBeds = AccommodationData.AvailableBeds,
                Amenities = AccommodationData.Amenities,
                CreatedAt = DateTime.UtcNow,
            };

            var created = await _repository.CreateAsync(accommodation);
            await _cache.RemoveAsync(AllAccommodationsCacheKey);
            return MapToResponse(created);
        }

        public async Task<AccommodationResponse?> UpdateAccommodationAsync(string id, UpdateAccommodation AccommodationData)
        {
            var accommodation = await _repository.GetByIdAsync(id);
            if (accommodation == null) return null;

            accommodation.Name = !string.IsNullOrWhiteSpace(AccommodationData.Name) ? AccommodationData.Name : accommodation.Name!;
            accommodation.Description = AccommodationData.Description ?? accommodation.Description;
            accommodation.Address = !string.IsNullOrWhiteSpace(AccommodationData.Address) ? AccommodationData.Address : accommodation.Address;
            accommodation.PhoneNumber = !string.IsNullOrWhiteSpace(AccommodationData.PhoneNumber) ? AccommodationData.PhoneNumber : accommodation.PhoneNumber;
            accommodation.Type = !string.IsNullOrWhiteSpace(AccommodationData.Type) ? AccommodationData.Type : accommodation.Type;
            accommodation.DistanceKm = AccommodationData.DistanceKm ?? accommodation.DistanceKm;
            accommodation.MonthlyRent = AccommodationData.MonthlyRent ?? accommodation.MonthlyRent;
            accommodation.AvailableBeds = AccommodationData.AvailableBeds ?? accommodation.AvailableBeds;
            accommodation.Amenities = AccommodationData.Amenities ?? accommodation.Amenities;

            if (AccommodationData.Photo != null)
            {
                accommodation.PhotoUrl = await _imageService.UploadImageAsync(AccommodationData.Photo , "Accommodations");
            }

            var updated = await _repository.UpdateAsync(accommodation);
            if (updated == null) return null;

            await _cache.RemoveAsync(AllAccommodationsCacheKey);
            return MapToResponse(updated);
        }

        public async Task<bool> DeleteAccommodationAsync(string id)
        {
            var result = await _repository.DeleteAsync(id);
            await _cache.RemoveAsync(AllAccommodationsCacheKey);
            return result;
        }

        private static AccommodationResponse MapToResponse(Accommodation accommodation)
        {
            var amenitiesList = string.IsNullOrWhiteSpace(accommodation.Amenities)
                ? new List<string>()
                : accommodation.Amenities
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(a => a.Trim())
                    .Where(a => a.Length > 0)
                    .ToList();

            return new AccommodationResponse
            {
                Id = accommodation.Id,
                Name = accommodation.Name,
                Description = accommodation.Description,
                Address = accommodation.Address,
                PhoneNumber = accommodation.PhoneNumber,
                PhotoUrl = accommodation.PhotoUrl,
                Type = accommodation.Type,
                DistanceKm = accommodation.DistanceKm,
                MonthlyRent = accommodation.MonthlyRent,
                AvailableBeds = accommodation.AvailableBeds,
                Amenities = amenitiesList,
                CreatedAt = accommodation.CreatedAt,
            };
        }
    }
}
