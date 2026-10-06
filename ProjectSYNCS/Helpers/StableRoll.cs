namespace ProjectSYNCS.Helpers;

/// <summary>
/// A number in [0, 1) hashed from three integers — SplitMix64, stable across runs and machines,
/// unlike <c>string.GetHashCode</c> or a seeded <see cref="Random"/>. Every Plynling roll that must
/// come out the same whoever computes it goes through here: sickness mornings (purpose + 1 as
/// <paramref name="c"/>), trait draws and base stats (with <paramref name="c"/> = 0 and a salt in
/// <paramref name="b"/>).
/// </summary>
public static class StableRoll
{
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
