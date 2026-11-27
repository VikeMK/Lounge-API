using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Lounge.Web.Storage
{
    /// <summary>
    /// Purges single URLs from Cloudflare's cache ("purge by URL"). Does nothing when ZoneId/PurgeToken
    /// aren't configured, e.g. local development, where there is no Cloudflare in front of S3Mock.
    /// </summary>
    public class CloudflareCachePurger : ICdnCachePurger
    {
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

        private readonly HttpClient _httpClient;
        private readonly CloudflareCacheOptions _options;

        public CloudflareCachePurger(HttpClient httpClient, IOptions<CloudflareCacheOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task PurgeAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(_options.ZoneId) || string.IsNullOrWhiteSpace(_options.PurgeToken))
                return;

            using var cts = new CancellationTokenSource(RequestTimeout);
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://api.cloudflare.com/client/v4/zones/{_options.ZoneId}/purge_cache");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.PurgeToken);
            request.Content = JsonContent.Create(new { files = new[] { url } });

            using var response = await _httpClient.SendAsync(request, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cts.Token);
                throw new HttpRequestException($"Cloudflare cache purge for {url} failed with {(int)response.StatusCode}: {body}");
            }
        }
    }
}
