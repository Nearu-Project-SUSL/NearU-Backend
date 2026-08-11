using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NearU_Backend_Revised.Configuration;
using NearU_Backend_Revised.Services.Interfaces;

namespace NearU_Backend_Revised.Services
{
    /// <summary>
    /// Uploads images directly to an AWS S3 bucket and returns the public URL.
    /// Objects are stored under: {folder}/{guid}_{originalFileName}
    /// ACL is set to public-read so uploaded URLs are immediately accessible.
    /// </summary>
    public class ImageService : IImageService
    {
        private readonly S3Settings _s3;
        private readonly ILogger<ImageService> _logger;

        // Allowed MIME types — reject anything that isn't an image
        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/jpg", "image/png", "image/gif",
            "image/webp", "image/svg+xml", "image/avif"
        };

        // 10 MB hard limit per upload
        private const long MaxFileSizeBytes = 10 * 1024 * 1024;

        public ImageService(IOptions<S3Settings> s3Settings, ILogger<ImageService> logger)
        {
            _s3    = s3Settings.Value;
            _logger = logger;
        }

        public async Task<string?> UploadImageAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                return null;

            // ── Validation ────────────────────────────────────────────────────
            if (file.Length > MaxFileSizeBytes)
                throw new InvalidOperationException(
                    $"File size {file.Length / 1024 / 1024:F1} MB exceeds the 10 MB limit.");

            if (!AllowedContentTypes.Contains(file.ContentType))
                throw new InvalidOperationException(
                    $"File type '{file.ContentType}' is not allowed. Only images are accepted.");

            // ── Build a unique, safe S3 object key ────────────────────────────
            var safeFileName = Path.GetFileName(file.FileName)   // strip any path components
                                   .Replace(" ", "-");
            var key = $"{folder.Trim('/')}/{Guid.NewGuid():N}_{safeFileName}";

            // ── Upload to S3 ──────────────────────────────────────────────────
            var credentials = new BasicAWSCredentials(_s3.AccessKey, _s3.SecretKey);
            var region      = RegionEndpoint.GetBySystemName(_s3.Region);

            using var s3Client = new AmazonS3Client(credentials, region);
            using var stream   = file.OpenReadStream();

            var request = new PutObjectRequest
            {
                BucketName  = _s3.BucketName,
                Key         = key,
                InputStream = stream,
                ContentType = file.ContentType,
                // Note: Do NOT set CannedACL here.
                // Since April 2023, new S3 buckets have Object Ownership = "Bucket owner enforced"
                // which disables ACLs entirely. Public access is controlled via a Bucket Policy.
            };

            try
            {
                await s3Client.PutObjectAsync(request);
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex,
                    "S3 upload failed for key={Key} bucket={Bucket}: {Message}",
                    key, _s3.BucketName, ex.Message);
                throw new InvalidOperationException($"Image upload failed: {ex.Message}", ex);
            }

            // ── Build and return the public URL ───────────────────────────────
            if (!string.IsNullOrWhiteSpace(_s3.CdnBaseUrl))
                return $"{_s3.CdnBaseUrl.TrimEnd('/')}/{key}";

            return $"https://{_s3.BucketName}.s3.{_s3.Region}.amazonaws.com/{key}";
        }
    }
}
