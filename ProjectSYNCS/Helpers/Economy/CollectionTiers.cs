namespace ProjectSYNCS.Helpers;

// A set's badge, by how many times it was completed. Derived from CollectionCompletion.Count,
// never stored — so the thresholds can move without a migration. The names are part of the
// badge icon keys (ItemEmojis.BadgeKey), so they are not renamed once the art ships.
public enum CollectionTier { None, Bronze, Silver, Gold, Platinum, Diamond }

/// <summary>
/// The tier ladder of a collection set: Bronze at the first completion (discovering every item),
/// then Argent, Or, Platine and Diamant as turn-ins add up. Pure.
/// </summary>
public static class CollectionTiers
{
    // Completions needed for Bronze, Argent, Or, Platine, Diamant — in CollectionTier order.
    public static readonly IReadOnlyList<int> Thresholds = new[] { 1, 2, 3, 5, 10 };

    public static CollectionTier TierFor(int count)
    {
        var tier = CollectionTier.None;
        for (var i = 0; i < Thresholds.Count && count >= Thresholds[i]; i++)
            tier = (CollectionTier)(i + 1);
        return tier;
    }

    /// <summary>The next tier up and the completions it needs; null once Diamant.</summary>
    public static (CollectionTier Tier, int At)? Next(int count)
    {
        var next = (int)TierFor(count) + 1;
        return next > Thresholds.Count ? null : ((CollectionTier)next, Thresholds[next - 1]);
    }

    public static string Label(CollectionTier tier) => tier switch
    {
        CollectionTier.Bronze => "Bronze",
        CollectionTier.Silver => "Argent",
        CollectionTier.Gold => "Or",
        CollectionTier.Platinum => "Platine",
        CollectionTier.Diamond => "Diamant",
        _ => "aucun",
    };

    // The medal half of the placeholder badge, until a set × tier icon is uploaded.
    public static string Medal(CollectionTier tier) => tier switch
    {
        CollectionTier.Bronze => "🥉",
        CollectionTier.Silver => "🥈",
        CollectionTier.Gold => "🥇",
        CollectionTier.Platinum => "🏅",
        CollectionTier.Diamond => "💠",
        _ => "",
    };

    /// <summary>
    /// A set's badge at a tier: its own icon (badge.&lt;set&gt;_&lt;tier&gt;) once uploaded, else the
    /// tier's medal beside the set's icon. Empty below Bronze.
    /// </summary>
    public static string Badge(CollectionSet set, CollectionTier tier) =>
        tier == CollectionTier.None ? "" : ItemEmojis.For(ItemEmojis.BadgeKey(set.Key, tier)) ?? Medal(tier) + set.Emoji;

    // Whether an Assets/Icons file name is a badge: badge.<set key>_<tier>, for a real set and tier.
    public static bool IsBadgeKey(string key) =>
        ItemCatalog.Sets.Any(s => Enum.GetValues<CollectionTier>()
            .Any(t => t != CollectionTier.None && ItemEmojis.BadgeKey(s.Key, t) == key));
}
