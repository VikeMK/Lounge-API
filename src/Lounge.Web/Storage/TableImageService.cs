using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading;
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
            this.AzureStorageConnectionString = configuration.GetConnectionString(AzureStorageConnectionStringKey)!;
        }

        public async Task UploadTableImageAsync(int tableId, byte[] image)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            BlobClient blobClient = await GetBlobClientAsync(tableId, cts.Token);
            using var ms = new MemoryStream(image);
            await blobClient.UploadAsync(ms, overwrite: true, cancellationToken: cts.Token);
        }

        public async Task<Stream?> DownloadTableImageAsync(int tableId)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            BlobClient blobClient = await GetBlobClientAsync(tableId, cts.Token);
            if (!await blobClient.ExistsAsync(cancellationToken: cts.Token))
                return null;

            return await blobClient.OpenReadAsync(cancellationToken: cts.Token);
        }

        public async Task DeleteTableImageAsync(int tableId)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            BlobClient blobClient = await GetBlobClientAsync(tableId, cts.Token);
            await blobClient.DeleteIfExistsAsync(cancellationToken: cts.Token);
        }

        private async Task<BlobClient> GetBlobClientAsync(int tableId, CancellationToken cancellationToken = default)
        {
            var blobServiceClient = new BlobServiceClient(this.AzureStorageConnectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(TableImageContainer);

            await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
            return blobContainerClient.GetBlobClient($"{tableId}.png");
        }
    }
}
