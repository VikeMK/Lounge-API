using System.Text.Json.Serialization;

namespace Lounge.Web.Stats
{
    public record SimplePlayerRegistryData(
        [property: JsonPropertyName("player_id")] int RegistryId,
        [property: JsonPropertyName("switch_fc")] string? SwitchFc,
        [property: JsonPropertyName("country_code")] string CountryCode);
}
