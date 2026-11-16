//An order is a list of products with their quantities.

namespace CoffeeShop.Requests;

record DeliveryRequest(List<OrderItem> Items);

record DeliveryItem(string Product, int Quantity);
