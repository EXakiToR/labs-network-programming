using Newtonsoft.Json.Linq;

namespace CoffeeShop.Requests;

// In one connection, use '\n' delimiter.
// Recieve one json
public record RequestEnvelope(
    string Type, // "order" for order reqeust, "menu" for menu request
    JToken Payload
);


// and then, determine which is the request.
