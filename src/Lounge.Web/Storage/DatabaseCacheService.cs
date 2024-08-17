using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Lounge.Web.Data.ChangeTracking;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lounge.Web.Storage
{
    public class DatabaseCacheService : IDatabaseCacheService
    {
        public record DatabaseCache(long Version, DbCacheData CacheData);

        private const string AzureStorageConnectionStringKey = "AzureStorage";
        private const string DatabaseCacheContainer = "database-cache";
        private const string CacheFileName = "cache.json";
        private readonly string _azureStorageConnectionString;

        public DatabaseCacheService(IConfiguration configuration)
        {
            _azureStorageConnectionString = configuration.GetConnectionString(AzureStorageConnectionStringKey)!;
        }

        public async Task<DatabaseCache?> GetLatestCacheDataAsync()
        {
            var cacheDataBlob = await GetCacheBlobClient();
            if (!await cacheDataBlob.ExistsAsync())
                return null;

            await using var stream = await cacheDataBlob.OpenReadAsync();
            return await JsonSerializer.DeserializeAsync<DatabaseCache>(stream);
        }

        public async Task UpdateLatestCacheDataAsync(long version, DbCacheData dbCache)
        {
            var cacheDataBlob = await GetCacheBlobClient();
            await using var stream = await cacheDataBlob.OpenWriteAsync(overwrite: true);
            await JsonSerializer.SerializeAsync<DatabaseCache>(stream, new(version, dbCache));
        }

        private async Task<BlobClient> GetCacheBlobClient()
        {
            var blobServiceClient = new BlobServiceClient(_azureStorageConnectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(DatabaseCacheContainer);
            await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
            return blobContainerClient.GetBlobClient(CacheFileName);
        }
    }
}
