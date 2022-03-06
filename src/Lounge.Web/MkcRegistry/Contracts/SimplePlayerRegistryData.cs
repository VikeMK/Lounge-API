using System.Text.Json.Serialization;

namespace Lounge.Web.Stats
{
    public record SimplePlayerRegistryData(
        [property: JsonPropertyName("player_id")] int RegistryId,
        [property: JsonPropertyName("user_id")] int ForumId,
        [property: JsonPropertyName("switch_fc")] string? SwitchFc,
        [property: JsonPropertyName("country_code")] string CountryCode);

    public record DetailedRegistryData(
        [property: JsonPropertyName("id")] int RegistryId,
        [property: JsonPropertyName("user_id")] int ForumId,
        [property: JsonPropertyName("switch_fc")] string? SwitchFc,
        [property: JsonPropertyName("country_code")] string CountryCode);
}
