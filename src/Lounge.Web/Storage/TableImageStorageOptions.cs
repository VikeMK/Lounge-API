namespace Lounge.Web.Storage
{
    /// <summary>
    /// Settings for table image storage (the "TableImages" config section).
    /// Production: Cloudflare R2. Local development: the S3Mock container from compose.yaml.
    /// </summary>
    public class TableImageStorageOptions
    {
        public const string SectionName = "TableImages";

        /// <summary>S3 API endpoint the app uploads to, e.g. https://&lt;account-id&gt;.r2.cloudflarestorage.com</summary>
        public string S3Endpoint { get; set; } = default!;

        /// <summary>Signing region. R2 uses "auto".</summary>
        public string Region { get; set; } = "auto";

        public string AccessKeyId { get; set; } = default!;

        public string SecretAccessKey { get; set; } = default!;

        public string Bucket { get; set; } = default!;

        /// <summary>
        /// Public base URL that browsers are redirected to, without a trailing slash,
        /// e.g. https://images.example.com. Images are served at {PublicBaseUrl}/{tableId}.png.
        /// </summary>
        public string PublicBaseUrl { get; set; } = default!;
    }
}
