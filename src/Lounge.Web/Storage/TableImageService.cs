using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace Lounge.Web.Storage
{
    public class TableImageService : ITableImageService
    {
        private const string AzureStorageConnectionStringKey = "AzureStorage";
        private const string TableImageContainer = "table-images";
        private readonly string AzureStorageConnectionString;

        public TableImageService(IConfiguration configuration)
        {
            this.AzureStorageConnectionString = configuration.GetConnectionString(AzureStorageConnectionStringKey);
        }

        public async Task UploadTableImageAsync(int tableId, byte[] image)
        {
            BlobClient blobClient = await GetBlobClientAsync(tableId);
            if (await blobClient.ExistsAsync())
                return;

            using var ms = new MemoryStream(image);
            await blobClient.UploadAsync(ms);
        }

        public async Task<Stream?> DownloadTableImageAsync(int tableId)
        {
            BlobClient blobClient = await GetBlobClientAsync(tableId);
            if (!await blobClient.ExistsAsync())
                return null;

            return await blobClient.OpenReadAsync();
        }

        private async Task<BlobClient> GetBlobClientAsync(int tableId)
        {
            var blobServiceClient = new BlobServiceClient(this.AzureStorageConnectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(TableImageContainer);

            await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
            return blobContainerClient.GetBlobClient($"{tableId}.png");
        }
    }
}
