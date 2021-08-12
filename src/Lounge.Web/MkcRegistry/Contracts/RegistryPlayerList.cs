using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lounge.Web.Stats
{
    public record RegistryPlayerList(
        [property: JsonPropertyName("data")] IReadOnlyList<SimplePlayerRegistryData> Data);
}
