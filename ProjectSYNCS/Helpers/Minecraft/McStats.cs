namespace ProjectSYNCS.Helpers;

/// <summary>
/// The keys of her Minecraft stats (/minecraft stats, MinecraftStat rows). **Stored keys: append-only**, never renamed.
/// </summary>
public static class McStats
{
    public const string Deaths = "deaths";
    public const string Fish = "fish";
    public const string Treasure = "treasure";
    public const string Kills = "kills";
    public const string Harvested = "harvested";
    public const string Minutes = "minutes";
    public const string Orders = "orders";

    public const string GatheredPrefix = "gathered:";
    public const string OrdersByPrefix = "orders:";

    /// <summary>Blocks gathered of one resource, as the body names it (« wood », « iron »).</summary>
    public static string Gathered(string resource) => GatheredPrefix + resource.ToLowerInvariant();

    /// <summary>Orders given by one player, by their name in lower case (Minecraft names ignore case).</summary>
    public static string OrdersBy(string player) => OrdersByPrefix + player.ToLowerInvariant();
}

/// <summary>
/// Her stats counted in memory between two writes (MinecraftBodyService flushes them every few minutes): never a
/// database write per event. Self-locking, cheap, safe from the body's event thread.
/// </summary>
public sealed class McTally
{
    private readonly object _lock = new();
    private readonly Dictionary<string, long> _counts = new();

    public void Add(string key, long n)
    {
        if (n == 0) return;
        lock (_lock) _counts[key] = _counts.GetValueOrDefault(key) + n;
    }

    /// <summary>What was counted since the last drain, and empties it.</summary>
    public IReadOnlyDictionary<string, long> Drain()
    {
        lock (_lock)
        {
            var copy = new Dictionary<string, long>(_counts);
            _counts.Clear();
            return copy;
        }
    }

    /// <summary>What was counted since the last drain, kept.</summary>
    public IReadOnlyDictionary<string, long> Peek()
    {
        lock (_lock) return new Dictionary<string, long>(_counts);
    }
}
