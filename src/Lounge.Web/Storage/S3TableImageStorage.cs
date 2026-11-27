using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Lounge.Web.Storage
{
    /// <summary>
    /// Table image storage over the S3 API. Works with Cloudflare R2 (production) and S3Mock (local).
    /// </summary>
    public sealed class S3TableImageStorage : ITableImageStorage, IDisposable
    {
        // Browser cache time only. Cloudflare's edge keeps images much longer (set by the cache rule for the
        // image domain) and the app purges an image's URL whenever it changes; browsers can't be purged, so keep this short.
        private const string ImageCacheControl = "public, max-age=300";
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

        private readonly AmazonS3Client _client;
        private readonly string _bucket;
        private readonly string _publicBaseUrl;

        public S3TableImageStorage(IOptions<TableImageStorageOptions> options)
        {
            var o = options.Value;
            _bucket = o.Bucket;
            _publicBaseUrl = o.PublicBaseUrl.TrimEnd('/');

            var config = new AmazonS3Config
            {
                ServiceURL = o.S3Endpoint,
                AuthenticationRegion = o.Region,
                // bucket in the path (endpoint/bucket/key) rather than the hostname; required by S3Mock, fine for R2
                ForcePathStyle = true,
                // Newer AWS SDKs add CRC checksums to every request by default, which S3-compatible
                // services (including R2) haven't always accepted. Only send/validate them when required.
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
            };

            _client = new AmazonS3Client(new BasicAWSCredentials(o.AccessKeyId, o.SecretAccessKey), config);
        }

        public async Task<bool> ExistsAsync(int tableId)
        {
            using var cts = new CancellationTokenSource(RequestTimeout);
            try
            {
                await _client.GetObjectMetadataAsync(_bucket, GetKey(tableId), cts.Token);
                return true;
            }
            catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public async Task UploadAsync(int tableId, byte[] image)
        {
            using var cts = new CancellationTokenSource(RequestTimeout);
            using var stream = new MemoryStream(image);
            var request = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = GetKey(tableId),
                InputStream = stream,
                ContentType = "image/png",
                // plain signed upload instead of aws-chunked streaming, for S3-compatible services
                UseChunkEncoding = false,
            };
            request.Headers.CacheControl = ImageCacheControl;

            await _client.PutObjectAsync(request, cts.Token);
        }

        public async Task DeleteAsync(int tableId)
        {
            using var cts = new CancellationTokenSource(RequestTimeout);
            await _client.DeleteObjectAsync(_bucket, GetKey(tableId), cts.Token);
        }

        public string GetPublicUrl(int tableId) => $"{_publicBaseUrl}/{GetKey(tableId)}";

        private static string GetKey(int tableId) => $"{tableId}.png";

        public void Dispose() => _client.Dispose();
    }
}
