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

        public AccommodationService(IAccommodationRepository repository, IImageService imageService)
        {
            _repository = repository;
            _imageService = imageService;
        }

        public async Task<IEnumerable<AccommodationResponse>> GetAllAccommodationsAsync()
        {
            var accommodations = await _repository.GetAllAsync();
            return accommodations.Select(accommodation => MapToResponse(accommodation)); //transform each accommodation into AccommodationData
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
                Id = Guid.NewGuid().ToString(), //generate a unique id
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
            return MapToResponse(updated);
        }

        public async Task<bool> DeleteAccommodationAsync(string id)
        {
            return await _repository.DeleteAsync(id);
        }

        private static AccommodationResponse MapToResponse(Accommodation accommodation) //takes a model and return a AccommodationData
        {
            // Parse comma-separated amenities string into a list
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
