using Newtonsoft.Json;

namespace CoffeeShop.Core;

public static class Stock
{
    private static readonly object _lock = new();

    private static readonly string FilePath = "stock.json";

    private static Dictionary<string, int> _products = new()
    {
        ["coffee"] = 0,
        ["tea"] = 0,
        ["croissant"] = 0,
    };

    public static Dictionary<string, int> GetSnapshot()
    {
        lock (_lock)
        {
            return new Dictionary<string, int>(_products);
        }
    }

    public static bool TryTake(Dictionary<string, int> requested)
    {
        lock (_lock)
        {
            foreach (var (product, quantity) in requested)
            {
                if (
                    quantity <= 0
                    || !_products.TryGetValue(product, out int available)
                    || available < quantity
                )
                    return false;
            }

            foreach (var (product, quantity) in requested)
                _products[product] -= quantity;

            SaveUnsafe();
            return true;
        }
    }

    public static void Add(Dictionary<string, int> delivered)
    {
        lock (_lock)
        {
            foreach (var (product, quantity) in delivered)
            {
                if (quantity <= 0 || !_products.ContainsKey(product))
                    throw new ArgumentException("Invalid delivery.");
            }

            foreach (var (product, quantity) in delivered)
                _products[product] = checked(_products[product] + quantity);

            SaveUnsafe();
        }
    }

    public static void Load()
    {
        lock (_lock)
        {
            if (!File.Exists(FilePath))
            {
                var random = new Random();

                foreach (string product in _products.Keys)
                    _products[product] = random.Next(10, 51);

                SaveUnsafe();
                return;
            }

            var loaded = JsonConvert.DeserializeObject<Dictionary<string, int>>(
                File.ReadAllText(FilePath)
            );

            if (
                loaded is null
                || loaded.Count != _products.Count
                || loaded.Any(p => !_products.ContainsKey(p.Key) || p.Value < 0)
            )
                throw new InvalidDataException("Invalid stock file.");

            _products = loaded;
        }
    }

    // unsafe because it should be called from a locked context
    private static void SaveUnsafe()
    {
        string json = JsonConvert.SerializeObject(_products, Formatting.Indented);

        File.WriteAllText(FilePath, json);
    }
}
