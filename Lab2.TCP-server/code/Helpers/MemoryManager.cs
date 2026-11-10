namespace CoffeeShop.Helpers;

public static class MemoryManager
{
    public static double GetHeapUsagePercentage()
    {
        var info = GC.GetGCMemoryInfo();
        return 100.0f * info.HeapSizeBytes / info.TotalAvailableMemoryBytes;
    }
}
