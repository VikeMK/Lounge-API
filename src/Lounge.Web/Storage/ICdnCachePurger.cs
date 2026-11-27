using System.Threading.Tasks;

namespace Lounge.Web.Storage
{
    public interface ICdnCachePurger
    {
        /// <summary>Removes one URL from the CDN cache so the next request fetches it fresh. Throws on failure.</summary>
        Task PurgeAsync(string url);
    }
}
