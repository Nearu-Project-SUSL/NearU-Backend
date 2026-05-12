using Imagekit;
using Imagekit.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NearU_Backend_Revised.Configurations;
using NearU_Backend_Revised.Services.Interfaces;

namespace NearU_Backend_Revised.Services
{
    public class ImageService : IImageService
    {
        private readonly ImageKitSettings _imageKitSettings;

        public ImageService(IOptions<ImageKitSettings> imageKitSettings)
        {
            _imageKitSettings = imageKitSettings.Value;
        }

        public async Task<string?> UploadImageAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                return null;

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();
            var base64 = Convert.ToBase64String(bytes);

            var imagekit = new ImagekitClient(
                _imageKitSettings.PublicKey,
                _imageKitSettings.PrivateKey,
                _imageKitSettings.UrlEndpoint
            );

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";

            var uploadRequest = new FileCreateRequest
            {
                file = base64,
                fileName = fileName,
                folder = folder,
                useUniqueFileName = true
            };

            var result = await imagekit.UploadAsync(uploadRequest);

            if (result == null || string.IsNullOrWhiteSpace(result.url))
                return null;

            return result.url;
        }
    }
}