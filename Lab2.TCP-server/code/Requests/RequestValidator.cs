using System.Text.Json;
using Json.Schema;

namespace CoffeeShop.Requests;

public static class RequestValidator
{
    private static readonly JsonSchema Schema = JsonSchema.FromFile(
        Path.Combine(AppContext.BaseDirectory, "Schemas", "request.schema.json")
    );

    public static bool IsValid(string json)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);

            var result = Schema.Evaluate(document.RootElement);

            return result.IsValid;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
