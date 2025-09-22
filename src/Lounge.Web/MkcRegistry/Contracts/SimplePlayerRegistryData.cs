using System.Text.Json.Serialization;

namespace Lounge.Web.MkcRegistry.Contracts;

public record MkcRegistryData(
    [property: JsonPropertyName("id")] int RegistryId,
    [property: JsonPropertyName("switch_fc")] string? SwitchFc,
    [property: JsonPropertyName("country_code")] string CountryCode);
