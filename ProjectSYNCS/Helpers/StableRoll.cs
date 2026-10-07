namespace ProjectSYNCS.Helpers;

/// <summary>
/// A number in [0, 1) hashed from three integers — SplitMix64, stable across runs and machines,
/// unlike <c>string.GetHashCode</c> or a seeded <see cref="Random"/>. Every Plynling roll that must
/// come out the same whoever computes it goes through here: sickness mornings (purpose + 1 as the
/// third input), trait draws, base stats and every event roll (a salt names each purpose).
/// </summary>
public static class StableRoll
{
    // One of `items`, uniformly, from a roll of (a, b, c). Never empty.
    public static T Pick<T>(IReadOnlyList<T> items, int a, int b, int c) =>
        items[Math.Min(items.Count - 1, (int)(Unit(a, b, c) * items.Count))];

    // One of `pool` in proportion to its weight, from a roll of (a, b, c). Never empty; with every
    // weight zero, the last.
    public static T Weighted<T>(IReadOnlyList<(T Item, double Weight)> pool, int a, int b, int c)
    {
        var target = Unit(a, b, c) * pool.Sum(x => x.Weight);
        foreach (var (item, weight) in pool)
        {
            if (target < weight) return item;
            target -= weight;
        }
        return pool[^1].Item;
    }

    public static double Unit(int a, int b, int c)
    {
        ulong x = (ulong)(uint)a * 0x9E3779B97F4A7C15UL
                  ^ (ulong)(uint)b * 0xC2B2AE3D27D4EB4FUL
                  ^ (ulong)(uint)c * 0x165667B19E3779F9UL;
        x += 0x9E3779B97F4A7C15UL;
        x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
        x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
        x ^= x >> 31;
        return (x >> 11) * (1.0 / (1UL << 53));
    }
}
