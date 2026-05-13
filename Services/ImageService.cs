using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NearU_Backend_Revised.Configuration;
using NearU_Backend_Revised.Services.Interfaces;

namespace NearU_Backend_Revised.Services
{
    public class ImageService : IImageService
    {
        private readonly ImageKitSettings _imageKitSettings;
        private readonly HttpClient _httpClient;

        public ImageService(IOptions<ImageKitSettings> imageKitSettings, IHttpClientFactory httpClientFactory)
        {
            _imageKitSettings = imageKitSettings.Value;
            _httpClient = httpClientFactory.CreateClient(nameof(ImageService));
        }

        public async Task<string?> UploadImageAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                return null;

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();
            var base64 = Convert.ToBase64String(bytes);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://upload.imagekit.io/api/v1/files/upload");

            var authValue = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_imageKitSettings.PrivateKey}:")
            );

            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "file", base64 },
                { "fileName", $"{Guid.NewGuid()}_{file.FileName}" },
                { "folder", folder },
                { "useUniqueFileName", "true" }
            });

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"ImageKit upload failed: {response.StatusCode} - {responseBody}");

            using var jsonDoc = JsonDocument.Parse(responseBody);

            if (jsonDoc.RootElement.TryGetProperty("url", out var urlElement))
                return urlElement.GetString();

            return null;
        }
    }
}
