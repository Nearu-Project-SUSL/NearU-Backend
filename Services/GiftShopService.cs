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
        private readonly ICacheService _cache;

        // Keyed by filter fingerprint so different filter combinations don't poison each other
        private const string AllGiftShopsCacheKey = "nearu:giftshops:all";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        public GiftShopService(
            IGiftShopRepository giftShopRepository,
            IImageService imageService,
            ICacheService cache)
        {
            _giftShopRepository = giftShopRepository;
            _imageService = imageService;
            _cache = cache;
        }

        public async Task<IEnumerable<GiftShopResponseDto>> GetAllAsync(string? keyword, string? location, bool? isActive)
        {
            // Cache the full active list; narrow filters are applied in memory for simplicity.
            // If fine-grained per-filter caching is needed in future, key on filter hash.
            var cached = await _cache.GetAsync<List<GiftShopResponseDto>>(AllGiftShopsCacheKey);
            if (cached is not null)
            {
                return ApplyFilters(cached, keyword, location, isActive);
            }

            var giftShops = await _giftShopRepository.GetAllAsync(null, null, null);
            var all = giftShops.Select(MapGiftShopToResponse).ToList();

            await _cache.SetAsync(AllGiftShopsCacheKey, all, CacheTtl);

            return ApplyFilters(all, keyword, location, isActive);
        }

        private static IEnumerable<GiftShopResponseDto> ApplyFilters(
            IEnumerable<GiftShopResponseDto> source,
            string? keyword,
            string? location,
            bool? isActive)
        {
            if (!string.IsNullOrWhiteSpace(keyword))
                source = source.Where(s =>
                    s.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (s.Address != null && s.Address.Contains(keyword, StringComparison.OrdinalIgnoreCase)));

            if (!string.IsNullOrWhiteSpace(location))
                source = source.Where(s =>
                    s.LocationName != null &&
                    s.LocationName.Contains(location, StringComparison.OrdinalIgnoreCase));

            if (isActive.HasValue)
                source = source.Where(s => s.IsActive == isActive.Value);

            return source;
        }

        public async Task<GiftShopResponseDto?> GetByIdAsync(Guid id)
        {
            var giftShop = await _giftShopRepository.GetByIdAsync(id);
            return giftShop == null ? null : MapGiftShopToResponse(giftShop);
        }

        public async Task<GiftShopResponseDto> CreateGiftShopAsync(CreateGiftShopDto giftShopDto)
        {
            string? uploadedImageUrl = null;

            if (giftShopDto.Image != null)
            {
                uploadedImageUrl = await _imageService.UploadImageAsync(giftShopDto.Image, "/gift-shops");
            }

            var giftShop = new GiftShop
            {
                Name = giftShopDto.Name.Trim(),
                ImageUrl = uploadedImageUrl,
                LocationName = giftShopDto.LocationName.Trim(),
                Phone = giftShopDto.Phone.Trim(),
                Email = string.IsNullOrWhiteSpace(giftShopDto.Email) ? null : giftShopDto.Email.Trim(),
                Address = giftShopDto.Address.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _giftShopRepository.AddGiftShopAsync(giftShop);
            await _giftShopRepository.SaveChangesAsync();

            var created = await _giftShopRepository.GetByIdAsync(giftShop.Id);

            await _cache.RemoveAsync(AllGiftShopsCacheKey);

            return MapGiftShopToResponse(created!);
        }

        public async Task<GiftShopResponseDto?> UpdateGiftShopAsync(Guid id, UpdateGiftShopDto giftShopDto)
        {
            var giftShop = await _giftShopRepository.GetByIdAsync(id);
            if (giftShop == null) return null;

            if (giftShopDto.Image != null)
            {
                var uploadedImageUrl = await _imageService.UploadImageAsync(giftShopDto.Image, "/gift-shops");
                if (!string.IsNullOrWhiteSpace(uploadedImageUrl))
                {
                    giftShop.ImageUrl = uploadedImageUrl;
                }
            }

            giftShop.Name = giftShopDto.Name.Trim();
            giftShop.LocationName = giftShopDto.LocationName.Trim();
            giftShop.Phone = giftShopDto.Phone.Trim();
            giftShop.Email = string.IsNullOrWhiteSpace(giftShopDto.Email) ? null : giftShopDto.Email.Trim();
            giftShop.Address = giftShopDto.Address.Trim();
            giftShop.IsActive = giftShopDto.IsActive;
            giftShop.UpdatedAt = DateTime.UtcNow;

            _giftShopRepository.UpdateGiftShop(giftShop);
            await _giftShopRepository.SaveChangesAsync();

            await _cache.RemoveAsync(AllGiftShopsCacheKey);

            var updated = await _giftShopRepository.GetByIdAsync(id);
            return updated == null ? null : MapGiftShopToResponse(updated);
        }

        public async Task<bool> DeleteGiftShopAsync(Guid id)
        {
            var giftShop = await _giftShopRepository.GetByIdAsync(id);
            if (giftShop == null) return false;

            _giftShopRepository.DeleteGiftShop(giftShop);
            var result = await _giftShopRepository.SaveChangesAsync();

            await _cache.RemoveAsync(AllGiftShopsCacheKey);

            return result;
        }

        public async Task<GiftProductResponseDto?> AddProductAsync(Guid giftShopId, CreateGiftProductDto productDto)
        {
            var giftShop = await _giftShopRepository.GetByIdAsync(giftShopId);
            if (giftShop == null) return null;

            string? uploadedPhotoUrl = null;

            if (productDto.Photo != null)
            {
                uploadedPhotoUrl = await _imageService.UploadImageAsync(productDto.Photo, "/gift-products");
            }

            var product = new GiftProduct
            {
                GiftShopId = giftShopId,
                Name = productDto.Name.Trim(),
                PhotoUrl = uploadedPhotoUrl,
                Price = productDto.Price,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _giftShopRepository.AddProductAsync(product);
            await _giftShopRepository.SaveChangesAsync();

            // Product list changed — invalidate the shop list cache
            await _cache.RemoveAsync(AllGiftShopsCacheKey);

            return MapGiftProductToResponse(product);
        }

        public async Task<GiftProductResponseDto?> UpdateProductAsync(Guid productId, UpdateGiftProductDto productDto)
        {
            var product = await _giftShopRepository.GetProductByIdAsync(productId);
            if (product == null) return null;

            if (productDto.Photo != null)
            {
                var uploadedPhotoUrl = await _imageService.UploadImageAsync(productDto.Photo, "/gift-products");
                if (!string.IsNullOrWhiteSpace(uploadedPhotoUrl))
                {
                    product.PhotoUrl = uploadedPhotoUrl;
                }
            }

            product.Name = productDto.Name.Trim();
            product.Price = productDto.Price;
            product.IsActive = productDto.IsActive;
            product.UpdatedAt = DateTime.UtcNow;

            _giftShopRepository.UpdateProduct(product);
            await _giftShopRepository.SaveChangesAsync();

            await _cache.RemoveAsync(AllGiftShopsCacheKey);

            return MapGiftProductToResponse(product);
        }

        public async Task<bool> DeleteProductAsync(Guid productId)
        {
            var product = await _giftShopRepository.GetProductByIdAsync(productId);
            if (product == null) return false;

            _giftShopRepository.DeleteProduct(product);
            var result = await _giftShopRepository.SaveChangesAsync();

            await _cache.RemoveAsync(AllGiftShopsCacheKey);

            return result;
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
