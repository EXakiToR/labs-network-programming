using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CoffeeShop.Requests;

public record RequestEnvelope(string Type, JToken Payload)
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        DateParseHandling = DateParseHandling.None
    };

    public static RequestEnvelope? Parse(string json)
    {
        var envelope = JsonConvert.DeserializeObject<JObject>(json, JsonSettings);
        var type = envelope?.GetValue(nameof(Type), StringComparison.OrdinalIgnoreCase);
        var payload = envelope?.GetValue(nameof(Payload), StringComparison.OrdinalIgnoreCase);
        if (
            type?.Type != JTokenType.String
            || payload is null
            || payload.Type is JTokenType.Null or JTokenType.Undefined
        )
            return null;

        string? name = (string?)type;
        return string.IsNullOrWhiteSpace(name) ? null : new RequestEnvelope(name, payload);
    }
}
