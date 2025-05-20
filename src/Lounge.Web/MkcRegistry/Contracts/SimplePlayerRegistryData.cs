using System.Text.Json.Serialization;

namespace Lounge.Web.Stats
{
    public record MkcRegistryData(
        [property: JsonPropertyName("id")] int RegistryId,
        [property: JsonPropertyName("switch_fc")] string? SwitchFc,
        [property: JsonPropertyName("country_code")] string CountryCode);
}
