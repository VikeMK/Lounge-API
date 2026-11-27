namespace Lounge.Web.Storage
{
    /// <summary>
    /// Settings for purging table images from Cloudflare's cache (the "Cloudflare" config section).
    /// Leave empty locally: purging is skipped when not configured.
    /// </summary>
    public class CloudflareCacheOptions
    {
        public const string SectionName = "Cloudflare";

        /// <summary>Zone ID of the domain serving table images (dashboard: zone overview, right-hand sidebar).</summary>
        public string? ZoneId { get; set; }

        /// <summary>API token with Zone > Cache Purge > Purge on that zone.</summary>
        public string? PurgeToken { get; set; }
    }
}
