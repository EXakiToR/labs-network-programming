//An order is a list of products with their quantities.

namespace CoffeeShop.Requests;

// if "type" is "order", then the payload is an OrderRequest object.
record OrderRequest(List<OrderItem> Items);

record OrderItem(string Product, int Quantity);

// done in generic way in case if there would be more types producrs, not just 'coffee', 'tea' and 'crossant'.
