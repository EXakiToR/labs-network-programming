using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CoffeeShop.Requests;

public record RequestEnvelope(string Type, JToken Payload)
{
    public static RequestEnvelope? Parse(string json)
    {
        if (!RequestValidator.IsValid(json))
            return null;

        return JsonConvert.DeserializeObject<RequestEnvelope>(json);
    }
}

// type      payload
// manu      MenuRequest
// oredr     OrderRequest
// register  RegisterRequest
// deliver   DeliveryRequest
