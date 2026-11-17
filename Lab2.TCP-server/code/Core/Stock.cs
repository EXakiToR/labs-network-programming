using Newtonsoft.Json;

namespace CoffeeShop.Core;

public static class Stock
{
    private const int LowStockThreshold = 5;
    private const string FilePath = "stock.json";
    private static readonly object _lock = new();

    // The saved totals include reservations until the ready message is sent.
    private static Dictionary<string, int> _products = new()
    {
        ["coffee"] = 0,
        ["tea"] = 0,
        ["croissant"] = 0,
    };
    private static readonly Dictionary<string, int> _reserved = new();
    private static readonly Dictionary<string, LowStockAlert> _pendingAlerts = new();

    internal sealed record LowStockAlert(string Product, int Available);

    public static Dictionary<string, int> GetSnapshot()
    {
        lock (_lock)
        {
            return _products.ToDictionary(item => item.Key, item => AvailableUnsafe(item.Key));
        }
    }

    internal static (string Product, int Available)? Reserve(
        IReadOnlyDictionary<string, int> requested,
        out bool hasNewAlert
    )
    {
        lock (_lock)
        {
            hasNewAlert = false;
            foreach (var (product, quantity) in requested)
            {
                int available = AvailableUnsafe(product);
                if (available < quantity)
                    return (product, available);
            }

            foreach (var (product, quantity) in requested)
            {
                int available = AvailableUnsafe(product);
                _reserved[product] = _reserved.GetValueOrDefault(product) + quantity;
                int remaining = available - quantity;
                if (available >= LowStockThreshold && remaining < LowStockThreshold)
                {
                    _pendingAlerts[product] = new LowStockAlert(product, remaining);
                    hasNewAlert = true;
                }
            }

            return null;
        }
    }

    internal static void Return(IReadOnlyDictionary<string, int> items)
    {
        lock (_lock)
        {
            foreach (var (product, quantity) in items)
                _reserved[product] -= quantity;
        }
    }

    internal static void Complete(IReadOnlyDictionary<string, int> items)
    {
        lock (_lock)
        {
            foreach (var (product, quantity) in items)
            {
                _reserved[product] -= quantity;
                _products[product] -= quantity;
            }
            SaveUnsafe(_products);
        }
    }

    public static void Add(Dictionary<string, int> delivered)
    {
        lock (_lock)
        {
            var updated = new Dictionary<string, int>(_products);
            foreach (var (product, quantity) in delivered)
            {
                if (quantity <= 0 || !updated.ContainsKey(product))
                    throw new ArgumentException("Invalid delivery.");

                updated[product] = checked(updated[product] + quantity);
            }
            SaveUnsafe(updated);
            _products = updated;
        }
    }

    public static void Load()
    {
        lock (_lock)
        {
            if (!File.Exists(FilePath))
            {
                foreach (string product in _products.Keys)
                    _products[product] = Random.Shared.Next(10, 51);

                SaveUnsafe(_products);
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
            foreach (var (product, quantity) in _products)
            {
                if (quantity < LowStockThreshold)
                    _pendingAlerts[product] = new LowStockAlert(product, quantity);
            }
        }
    }

    internal static LowStockAlert[] GetPendingAlerts()
    {
        lock (_lock)
        {
            return _pendingAlerts.Values.ToArray();
        }
    }

    internal static void AcknowledgeAlert(LowStockAlert alert)
    {
        lock (_lock)
        {
            if (ReferenceEquals(_pendingAlerts.GetValueOrDefault(alert.Product), alert))
                _pendingAlerts.Remove(alert.Product);
        }
    }

    private static int AvailableUnsafe(string product) =>
        _products[product] - _reserved.GetValueOrDefault(product);

    private static void SaveUnsafe(Dictionary<string, int> products)
    {
        string temporaryPath = FilePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonConvert.SerializeObject(products, Formatting.Indented));
        File.Move(temporaryPath, FilePath, overwrite: true);
    }
}
