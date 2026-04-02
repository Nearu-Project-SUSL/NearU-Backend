using NearU_Backend_Revised.DTOs.GiftProduct;
using NearU_Backend_Revised.DTOs.GiftShop;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories.Interfaces;
using NearU_Backend_Revised.Services.Interfaces;

namespace NearU_Backend_Revised.Services
{
    public class GiftShopService : IGiftShopService
    {
        private readonly IGiftShopRepository _giftShopRepository;
        private readonly IImageService _imageService;

        public GiftShopService(
            IGiftShopRepository giftShopRepository,
            IImageService imageService)
        {
            _giftShopRepository = giftShopRepository;
            _imageService = imageService;
        }

        public async Task<IEnumerable<GiftShopResponseDto>> GetAllAsync(string? keyword, string? location, bool? isActive)
        {
            var giftShops = await _giftShopRepository.GetAllAsync(keyword, location, isActive);
            return giftShops.Select(MapGiftShopToResponse);
        }

        public async Task<GiftShopResponseDto?> GetByIdAsync(Guid id)
        {
            var giftShop = await _giftShopRepository.GetByIdAsync(id);
            return giftShop == null ? null : MapGiftShopToResponse(giftShop);
        }

        public async Task<GiftShopResponseDto> CreateGiftShopAsync(CreateGiftShopDto dto)
        {
            string? uploadedImageUrl = null;

            if (dto.Image != null)
            {
                uploadedImageUrl = await _imageService.UploadImageAsync(dto.Image, "/gift-shops");
            }

            var giftShop = new GiftShop
            {
                Name = dto.Name.Trim(),
                ImageUrl = uploadedImageUrl,
                LocationName = dto.LocationName.Trim(),
                Phone = dto.Phone.Trim(),
                Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
                Address = dto.Address.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _giftShopRepository.AddGiftShopAsync(giftShop);
            await _giftShopRepository.SaveChangesAsync();

            var created = await _giftShopRepository.GetByIdAsync(giftShop.Id);
            return MapGiftShopToResponse(created!);
        }

        public async Task<GiftShopResponseDto?> UpdateGiftShopAsync(Guid id, UpdateGiftShopDto dto)
        {
            var giftShop = await _giftShopRepository.GetByIdAsync(id);
            if (giftShop == null) return null;

            if (dto.Image != null)
            {
                var uploadedImageUrl = await _imageService.UploadImageAsync(dto.Image, "/gift-shops");
                if (!string.IsNullOrWhiteSpace(uploadedImageUrl))
                {
                    giftShop.ImageUrl = uploadedImageUrl;
                }
            }

            giftShop.Name = dto.Name.Trim();
            giftShop.LocationName = dto.LocationName.Trim();
            giftShop.Phone = dto.Phone.Trim();
            giftShop.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
            giftShop.Address = dto.Address.Trim();
            giftShop.IsActive = dto.IsActive;
            giftShop.UpdatedAt = DateTime.UtcNow;

            _giftShopRepository.UpdateGiftShop(giftShop);
            await _giftShopRepository.SaveChangesAsync();

            var updated = await _giftShopRepository.GetByIdAsync(id);
            return updated == null ? null : MapGiftShopToResponse(updated);
        }

        public async Task<bool> DeleteGiftShopAsync(Guid id)
        {
            var giftShop = await _giftShopRepository.GetByIdAsync(id);
            if (giftShop == null) return false;

            _giftShopRepository.DeleteGiftShop(giftShop);
            return await _giftShopRepository.SaveChangesAsync();
        }

        public async Task<GiftProductResponseDto?> AddProductAsync(Guid giftShopId, CreateGiftProductDto dto)
        {
            var giftShop = await _giftShopRepository.GetByIdAsync(giftShopId);
            if (giftShop == null) return null;

            string? uploadedPhotoUrl = null;

            if (dto.Photo != null)
            {
                uploadedPhotoUrl = await _imageService.UploadImageAsync(dto.Photo, "/gift-products");
            }

            var product = new GiftProduct
            {
                GiftShopId = giftShopId,
                Name = dto.Name.Trim(),
                PhotoUrl = uploadedPhotoUrl,
                Price = dto.Price,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _giftShopRepository.AddProductAsync(product);
            await _giftShopRepository.SaveChangesAsync();

            return MapGiftProductToResponse(product);
        }

        public async Task<GiftProductResponseDto?> UpdateProductAsync(Guid productId, UpdateGiftProductDto dto)
        {
            var product = await _giftShopRepository.GetProductByIdAsync(productId);
            if (product == null) return null;

            if (dto.Photo != null)
            {
                var uploadedPhotoUrl = await _imageService.UploadImageAsync(dto.Photo, "/gift-products");
                if (!string.IsNullOrWhiteSpace(uploadedPhotoUrl))
                {
                    product.PhotoUrl = uploadedPhotoUrl;
                }
            }

            product.Name = dto.Name.Trim();
            product.Price = dto.Price;
            product.IsActive = dto.IsActive;
            product.UpdatedAt = DateTime.UtcNow;

            _giftShopRepository.UpdateProduct(product);
            await _giftShopRepository.SaveChangesAsync();

            return MapGiftProductToResponse(product);
        }

        public async Task<bool> DeleteProductAsync(Guid productId)
        {
            var product = await _giftShopRepository.GetProductByIdAsync(productId);
            if (product == null) return false;

            _giftShopRepository.DeleteProduct(product);
            return await _giftShopRepository.SaveChangesAsync();
        }

        private static GiftShopResponseDto MapGiftShopToResponse(GiftShop giftShop)
        {
            return new GiftShopResponseDto
            {
                Id = giftShop.Id,
                Name = giftShop.Name,
                ImageUrl = giftShop.ImageUrl,
                LocationName = giftShop.LocationName,
                Phone = giftShop.Phone,
                Email = giftShop.Email,
                Address = giftShop.Address,
                IsActive = giftShop.IsActive,
                CreatedAt = giftShop.CreatedAt,
                UpdatedAt = giftShop.UpdatedAt,
                Products = giftShop.Products?
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(MapGiftProductToResponse)
                    .ToList() ?? new List<GiftProductResponseDto>()
            };
        }

        private static GiftProductResponseDto MapGiftProductToResponse(GiftProduct product)
        {
            return new GiftProductResponseDto
            {
                Id = product.Id,
                GiftShopId = product.GiftShopId,
                Name = product.Name,
                PhotoUrl = product.PhotoUrl,
                Price = product.Price,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };
        }
    }
}