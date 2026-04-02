using Microsoft.AspNetCore.Http;

namespace NearU_Backend_Revised.Services.Interfaces
{
    public interface IImageService
    {
        Task<string?> UploadImageAsync(IFormFile file, string folder);
    }
}