using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lounge.Web.Stats
{
    public class MkcRegistryApi : IMkcRegistryApi
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MkcRegistryApi> _logger;

        public MkcRegistryApi(IHttpClientFactory httpClientFactory, ILogger<MkcRegistryApi> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<DetailedRegistryData> GetPlayerRegistryDataAsync(int registryId)
        {
            var client = _httpClientFactory.CreateClient("WithRedirects");
            var url = $"https://www.mariokartcentral.com/mkc/api/registry/players/{registryId}";
            var resp = await client.GetAsync(url);
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                var contentStream = await resp.Content.ReadAsStreamAsync();
                var registryData = await JsonSerializer.DeserializeAsync<DetailedRegistryData>(contentStream);
                if (registryData == null)
                    throw new Exception("Got null when deserializing player registry data");
                return registryData;
            }

            throw new Exception("Failed to get player registry data");
        }

        public async Task<int?> GetRegistryIdAsync(int mkcId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("NoRedirects");
                var url = $"https://www.mariokartcentral.com/mkc/registry/users/{mkcId}";
                var resp = await client.GetAsync(url);
                if (resp.StatusCode != HttpStatusCode.Found)
                    return null;

                var location = resp.Headers.Location;
                if (location is null)
                    return null;

                var path = location.AbsolutePath;
                var lastSegmentIndex = path.LastIndexOf('/');
                if (lastSegmentIndex < 0)
                    return null;

                var lastSegment = path[(lastSegmentIndex + 1)..];
                if (int.TryParse(lastSegment, out var registryId))
                    return registryId;

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown when getting registry ID");
                return null;
            }
        }

        public async Task<IReadOnlyList<SimplePlayerRegistryData>> GetAllPlayersRegistryDataAsync()
        {
            var client = _httpClientFactory.CreateClient("WithRedirects");
            var url = "https://www.mariokartcentral.com/mkc/api/registry/players/category/all";
            var resp = await client.GetAsync(url);
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                var contentStream = await resp.Content.ReadAsStreamAsync();
                var registryPlayerList = await JsonSerializer.DeserializeAsync<RegistryPlayerList>(contentStream);
                if (registryPlayerList == null)
                    throw new Exception("Got null when deserializing player registry data");
                return registryPlayerList.Data;
            }

            throw new Exception("Failed to get player registry data");
        }
    }
}
