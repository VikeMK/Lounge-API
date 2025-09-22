using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lounge.Web.MkcRegistry.Contracts;

public record RegistryPlayerList(
    [property: JsonPropertyName("data")] IReadOnlyList<MkcRegistryData> Data);
