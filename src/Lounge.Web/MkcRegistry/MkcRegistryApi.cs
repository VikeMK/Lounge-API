using Lounge.Web.MkcRegistry.Contracts;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lounge.Web.MkcRegistry;

public class MkcRegistryApi : IMkcRegistryApi
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MkcRegistryApi> _logger;

    public MkcRegistryApi(IHttpClientFactory httpClientFactory, ILogger<MkcRegistryApi> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<MkcRegistryData> GetPlayerRegistryDataAsync(int registryId)
    {
        var client = _httpClientFactory.CreateClient("WithRedirects");
        var url = $"https://mkcentral.com/api/registry/players/{registryId}/lounge";
        var resp = await client.GetAsync(url);
        if (resp.StatusCode == HttpStatusCode.OK)
        {
            var contentStream = await resp.Content.ReadAsStreamAsync();
            var registryData = await JsonSerializer.DeserializeAsync<MkcRegistryData>(contentStream) 
                ?? throw new Exception("Got null when deserializing player registry data");
            return registryData;
        }

        throw new Exception("Failed to get player registry data");
    }
}
