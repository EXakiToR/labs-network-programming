namespace CoffeeShop.Requests;

record OrderRequest(List<OrderItem> Items);

record OrderItem(string Product, int Quantity)
{
    public static Dictionary<string, int> GetQuantities(IEnumerable<OrderItem> items)
    {
        var quantities = new Dictionary<string, int>();
        foreach (var item in items)
            quantities[item.Product] = checked(quantities.GetValueOrDefault(item.Product) + item.Quantity);
        return quantities;
    }
}
