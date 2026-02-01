using System.IO;
using System.Threading.Tasks;

namespace Lounge.Web.Storage
{
    public interface ITableImageService
    {
        Task<Stream?> DownloadTableImageAsync(int tableId);
        Task UploadTableImageAsync(int tableId, byte[] image);
        Task DeleteTableImageAsync(int tableId);
    }
}