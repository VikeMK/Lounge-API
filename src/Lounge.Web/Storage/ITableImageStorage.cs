using System.Threading.Tasks;

namespace Lounge.Web.Storage
{
    /// <summary>
    /// Raw access to the table image bucket. The app never downloads images from it:
    /// browsers are redirected to <see cref="GetPublicUrl"/> instead.
    /// </summary>
    public interface ITableImageStorage
    {
        /// <summary>Checks whether {tableId}.png exists (an S3 HEAD request, no download).</summary>
        Task<bool> ExistsAsync(int tableId);

        Task UploadAsync(int tableId, byte[] image);

        Task DeleteAsync(int tableId);

        /// <summary>
        /// Public URL for the image. Deliberately has no query string: Cloudflare's cache rule for the image domain
        /// ignores query strings, and freshness after an upload comes from purging this exact URL.
        /// </summary>
        string GetPublicUrl(int tableId);
    }
}
