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

        private static AccommodationResponse MapToResponse(Accommodation accommodation) //taks a model and return a AccommodationData
        {
            return new AccommodationResponse
            {
                Id = accommodation.Id,
                Name = accommodation.Name,
                Description = accommodation.Description,
                Address = accommodation.Address,
                PhoneNumber = accommodation.PhoneNumber,
                PhotoUrl = accommodation.PhotoUrl,
                CreatedAt = accommodation.CreatedAt,
            };
        }
    }
}
