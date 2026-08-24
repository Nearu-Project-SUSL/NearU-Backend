using NearU_Backend_Revised.DTOs.GiftProduct;
using NearU_Backend_Revised.DTOs.GiftShop;

namespace NearU_Backend_Revised.Services.Interfaces
{
    public interface IGiftShopService
    {
        Task<IEnumerable<GiftShopResponseDto>> GetAllAsync(string? keyword, string? location, bool? isActive);
        Task<GiftShopResponseDto?> GetByIdAsync(Guid id);
        Task<GiftShopResponseDto> CreateGiftShopAsync(CreateGiftShopDto dto);
        Task<GiftShopResponseDto?> UpdateGiftShopAsync(Guid id, UpdateGiftShopDto dto);
        Task<bool> DeleteGiftShopAsync(Guid id);
        Task<GiftProductResponseDto?> AddProductAsync(Guid giftShopId, CreateGiftProductDto dto);
        Task<GiftProductResponseDto?> UpdateProductAsync(Guid productId, UpdateGiftProductDto dto);
        Task<bool> DeleteProductAsync(Guid productId);
    }
}