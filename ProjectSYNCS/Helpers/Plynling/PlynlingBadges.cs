using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// What happened alongside an action that a badge can hinge on, beyond the counts.
public enum BadgeEvent { None, SavedFromStarving, Resurrected, BecameFriends, BecameBestFriends, BecameLovers }

/// <summary>
/// Everything a badge reads beyond the row, gathered by the caller (PlynlingService.BadgeFactsAsync):
/// its age, what just happened, its best body stat (which needs its traits) and whether it ever got
/// over an illness (its journal).
/// </summary>
public sealed record BadgeFacts(TimeSpan Age, BadgeEvent Event = BadgeEvent.None, int BestBodyStat = 0, bool Recovered = false);

/// <summary>
/// A palier: five tiers (Bronze → Diamant, as the member badges) over one counter. Each tier is its
/// own <see cref="BadgeInfo"/>, stored, paid and journaled once when reached.
/// </summary>
public sealed class BadgePalier
{
    private readonly string _nameM;
    private readonly string _nameF;
    private readonly Func<Plynling, BadgeFacts, long> _value;

    internal BadgePalier(string key, string emoji, string nameM, string nameF, Func<Plynling, BadgeFacts, long> value)
    {
        Key = key;
        Emoji = emoji;
        _nameM = nameM;
        _nameF = nameF;
        _value = value;
    }

    public string Key { get; }
    public string Emoji { get; }
    public IReadOnlyList<BadgeInfo> Tiers { get; internal set; } = Array.Empty<BadgeInfo>();

    public string Name(PlynlingGender gender) => gender.Agree(_nameM, _nameF);

    public long Value(Plynling p, BadgeFacts facts) => _value(p, facts);

    /// <summary>The highest tier among the earned keys, or null below Bronze.</summary>
    public BadgeInfo? Reached(IReadOnlySet<string> earned) => Tiers.LastOrDefault(t => earned.Contains(t.Key));

    /// <summary>The first tier not earned yet, or null once Diamant is.</summary>
    public BadgeInfo? Next(IReadOnlySet<string> earned) => Tiers.FirstOrDefault(t => !earned.Contains(t.Key));
}

/// <summary>
/// One stored badge: a palier's tier or a moment (earned once). The key is stored on each
/// PlynlingBadge row, so it is **stable**: renaming one orphans every copy already earned. The
/// keys from before the paliers (`week`, `first_game`, `meals_100`…) are tiers now, under their old
/// keys and rewards. The name agrees with the Plynling (« Sauvée de justesse »). The reward goes
/// to the owner once, when the badge is earned.
/// </summary>
public sealed class BadgeInfo
{
    private readonly string _nameM;
    private readonly string _nameF;
    private readonly Func<Plynling, BadgeFacts, bool> _earned;

    private BadgeInfo(string key, string emoji, string nameM, string nameF, long reward,
        Func<Plynling, BadgeFacts, bool> earned, BadgePalier? palier, CollectionTier tier, long threshold)
    {
        Key = key;
        Emoji = emoji;
        _nameM = nameM;
        _nameF = nameF;
        Reward = reward;
        _earned = earned;
        Palier = palier;
        Tier = tier;
        Threshold = threshold;
    }

    internal static BadgeInfo Moment(string key, string emoji, string nameM, string nameF, long reward, Func<Plynling, BadgeFacts, bool> earned) =>
        new(key, emoji, nameM, nameF, reward, earned, null, CollectionTier.None, 0);

    internal static BadgeInfo TierOf(BadgePalier palier, string key, CollectionTier tier, long threshold, long reward) =>
        new(key, palier.Emoji, "", "", reward, (p, f) => palier.Value(p, f) >= threshold, palier, tier, threshold);

    public string Key { get; }
    public long Reward { get; }
    /// <summary>Its palier, or null for a moment.</summary>
    public BadgePalier? Palier { get; }
    public CollectionTier Tier { get; }
    public long Threshold { get; }

    /// <summary>The bare emoji: a moment's own, a tier's palier's.</summary>
    public string Emoji { get; }

    /// <summary>
    /// Its rosette's icon key (Assets/Icons/&lt;key&gt;.png, tools/item-art): plbadge.&lt;palier&gt;_&lt;tier&gt; for a
    /// tier, plbadge.&lt;key&gt; for a moment — by palier, not by the stored key (`week` is longevity_bronze).
    /// </summary>
    public string IconKey => Palier is { } palier
        ? $"plbadge.{palier.Key}_{Tier.ToString().ToLowerInvariant()}"
        : $"plbadge.{Key}";

    /// <summary>
    /// What shows beside the name: its rosette once uploaded, else a tier's medal before its palier's
    /// emoji, a moment's emoji.
    /// </summary>
    public string Icon => ItemEmojis.For(IconKey) ?? (Palier is null ? Emoji : CollectionTiers.Medal(Tier) + Emoji);

    public string Name(PlynlingGender gender) => Palier is { } palier
        ? $"{palier.Name(gender)} ({CollectionTiers.Label(Tier)})"
        : gender.Agree(_nameM, _nameF);

    public bool IsEarned(Plynling p, BadgeFacts facts) => _earned(p, facts);
}

// Every badge a Plynling can earn — its own, not its owner's: they follow it to the graveyard.
// Checked after each action that can earn one, in the same save (PlynlingService), and hourly
// by the sweep for what time earns.
public static class PlynlingBadges
{
    // A new tier's reward by tier. The tiers that kept a key from before the paliers keep its reward.
    private static readonly long[] TierReward = { 0, 10, 20, 30, 40, 50 };

    // Catalog order: the journal lists them so. Appending is free; a key, once shipped, never changes.
    public static readonly IReadOnlyList<BadgePalier> Paliers = new[]
    {
        Palier("longevity", "🌳", "Longévité", "Longévité", (_, f) => (long)f.Age.TotalDays,
            ("week", 7, 10), ("month", 30, 20), ("six_months", 180, 40), ("year", 365, 50), ("two_years", 730, 0)),
        Palier("player", "🎲", "Joueur", "Joueuse", (p, _) => p.Plays,
            ("first_game", 1, 10), ("games_10", 10, 0), ("games_50", 50, 0), ("games_100", 100, 40), ("games_500", 500, 0)),
        Palier("champion", "🏆", "Champion", "Championne", (p, _) => p.PlaysWon,
            ("wins_1", 1, 0), ("wins_10", 10, 20), ("wins_50", 50, 40), ("wins_100", 100, 0), ("wins_250", 250, 0)),
        Palier("traveller", "🌍", "Voyageur", "Voyageuse", (p, _) => p.Visits,
            ("first_visit", 1, 10), ("visits_10", 10, 20), ("visits_30", 30, 40), ("visits_100", 100, 0), ("visits_250", 250, 0)),
        Palier("host", "🏠", "Hôte", "Hôtesse", (p, _) => p.Hosted,
            ("hosted_1", 1, 0), ("hosted_10", 10, 0), ("hosted_30", 30, 0), ("hosted_100", 100, 0), ("hosted_250", 250, 0)),
        Palier("gourmet", "🍄", "Gourmand", "Gourmande", (p, _) => p.Meals,
            ("meals_10", 10, 0), ("meals_50", 50, 0), ("meals_100", 100, 20), ("meals_500", 500, 0), ("meals_1000", 1000, 0)),
        Palier("cuddly", "🤲", "Pot de colle", "Pot de colle", (p, _) => p.Pets,
            ("pets_10", 10, 0), ("pets_50", 50, 0), ("pets_100", 100, 20), ("pets_500", 500, 0), ("pets_1000", 1000, 0)),
        Palier("beloved", "🎁", "Chouchou du village", "Chouchoute du village", (p, _) => p.FedByOthers,
            ("fed_by_friend", 1, 10), ("fed_by_friends_5", 5, 0), ("fed_by_friends_10", 10, 0), ("fed_by_friends_25", 25, 0), ("fed_by_friends_50", 50, 0)),
        Palier("training", "🏋️", "Entraînement", "Entraînement", (p, _) => p.Drills,
            ("drills_1", 1, 0), ("drills_25", 25, 0), ("drills_50", 50, 0), ("drills_200", 200, 0), ("drills_500", 500, 0)),
        Palier("level", "⭐", "Niveau", "Niveau", (p, _) => PlynlingRpg.Level(p.RpgXp),
            ("level_5", 5, 0), ("level_10", 10, 0), ("level_25", 25, 0), ("level_50", 50, 0), ("level_100", 100, 0)),
        Palier("athlete", "💪", "Athlète", "Athlète", (_, f) => f.BestBodyStat,
            ("athlete_10", 10, 0), ("athlete_15", 15, 0), ("athlete_20", 20, 0), ("athlete_30", 30, 0), ("athlete_40", 40, 0)),
    };

    public static readonly IReadOnlyList<BadgeInfo> Moments = new[]
    {
        BadgeInfo.Moment("saved", "😮‍💨", "Sauvé de justesse", "Sauvée de justesse", 20, (_, f) => f.Event == BadgeEvent.SavedFromStarving),
        // Pays nothing: only staff can resurrect, so it cannot be something anyone earns on purpose.
        BadgeInfo.Moment("resurrected", "✨", "Revenu d'entre les morts", "Revenue d'entre les morts", 0, (_, f) => f.Event == BadgeEvent.Resurrected),
        // Relationships: a first friend (reaching any closer bond counts too), a best friend, a couple.
        BadgeInfo.Moment("first_friend", "🤝", "Premier ami", "Première amie", 10,
            (_, f) => f.Event is BadgeEvent.BecameFriends or BadgeEvent.BecameBestFriends or BadgeEvent.BecameLovers),
        BadgeInfo.Moment("best_friend", "💛", "Meilleur ami", "Meilleure amie", 20,
            (_, f) => f.Event is BadgeEvent.BecameBestFriends or BadgeEvent.BecameLovers),
        BadgeInfo.Moment("couple", "💞", "En couple", "En couple", 30, (_, f) => f.Event == BadgeEvent.BecameLovers),
        BadgeInfo.Moment("recovered", "💊", "Guéri", "Guérie", 10, (_, f) => f.Recovered),
        BadgeInfo.Moment("dapper", "🎩", "Coquet", "Coquette", 10, (p, _) => WearsThreeAccessories(p)),
    };

    /// <summary>Every stored badge: each palier's tiers in order, then the moments.</summary>
    public static readonly IReadOnlyList<BadgeInfo> All = Paliers.SelectMany(p => p.Tiers).Concat(Moments).ToList();

    private static readonly Dictionary<string, BadgeInfo> ByKeyMap = All.ToDictionary(b => b.Key);

    public static BadgeInfo? ByKey(string key) => ByKeyMap.GetValueOrDefault(key);

    // Declared after All, in this file: a static initializer reading another file's field may see null.
    private static readonly HashSet<string> IconKeys = All.Select(b => b.IconKey).ToHashSet(StringComparer.Ordinal);

    /// <summary>Whether an Assets/Icons file name is a Plynling badge's rosette (ItemEmojis uploads those).</summary>
    public static bool IsIconKey(string key) => IconKeys.Contains(key);

    public static bool WearsThreeAccessories(Plynling p) =>
        !string.IsNullOrEmpty(p.AccessoryKey) && !string.IsNullOrEmpty(p.Accessory2Key) && !string.IsNullOrEmpty(p.Accessory3Key);

    /// <summary>The badges this Plynling qualifies for now and has not earned yet, in catalog order.</summary>
    public static List<BadgeInfo> Newly(Plynling p, IReadOnlySet<string> earned, BadgeFacts facts) =>
        All.Where(b => !earned.Contains(b.Key) && b.IsEarned(p, facts)).ToList();

    // Under the card that answered the action: one line per badge just earned.
    public static string NewBadgeLines(IEnumerable<BadgeInfo> badges, PlynlingGender gender) =>
        string.Join("\n", badges.Select(b =>
            $"🏅 Nouveau badge : {b.Icon} {b.Name(gender)}" + (b.Reward > 0 ? $" · +{PebbleEconomy.Cailloux(b.Reward)}" : "")));

    // A tier's reward of 0 in the table means "the tier's default" (TierReward); the old keys carry their own.
    private static BadgePalier Palier(string key, string emoji, string nameM, string nameF, Func<Plynling, BadgeFacts, long> value,
        params (string Key, long Threshold, long Reward)[] tiers)
    {
        if (tiers.Length != 5) throw new ArgumentException($"palier {key} needs five tiers", nameof(tiers));
        var palier = new BadgePalier(key, emoji, nameM, nameF, value);
        palier.Tiers = tiers.Select((t, i) =>
        {
            var tier = (CollectionTier)(i + 1);
            return BadgeInfo.TierOf(palier, t.Key, tier, t.Threshold, t.Reward > 0 ? t.Reward : TierReward[(int)tier]);
        }).ToList();
        return palier;
    }
}
