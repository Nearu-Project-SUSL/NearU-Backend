namespace NearU_Backend_Revised.Configuration
{
    /// <summary>
    /// AWS S3 settings — bound from the AWS__ section in environment variables.
    /// </summary>
    public class S3Settings
    {
        public string AccessKey  { get; set; } = string.Empty;
        public string SecretKey  { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;
        public string Region     { get; set; } = string.Empty;

        /// <summary>
        /// Optional CloudFront CDN base URL (e.g. https://d1234.cloudfront.net).
        /// When set, uploaded image URLs use this prefix instead of the direct S3 URL.
        /// </summary>
        public string? CdnBaseUrl { get; set; }
    }
}
