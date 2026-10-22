namespace ProjectSYNCS.Helpers;

public enum MemberBadgeKind { Palier, Moment, Secret }

// How a palier's raw value prints: a plain count, hours (the value is minutes) or days.
public enum BadgeUnit { Count, Hours, Days }

/// <summary>
/// One member badge: a palier (five tiers over a counter), a moment (earned once) or a secret
/// (earned once, hidden until found). Thresholds are in the raw unit of the value, empty for
/// moments and secrets.
/// </summary>
public sealed record MemberBadgeInfo(string Key, MemberBadgeKind Kind, string Emoji, string Name, string Hint,
    IReadOnlyList<long> Thresholds, BadgeUnit Unit = BadgeUnit.Count);

/// <summary>A stored badge row: the counter (arcade, nanny) or 1, and when it was first earned.</summary>
public sealed record StoredBadge(long Count, DateTimeOffset EarnedAt);

/// <summary>Everything the palier and derived-moment evaluation reads, gathered by the caller.</summary>
public sealed record MemberBadgeFacts(long VoiceMinutes, long CompanionMinutes, long QuizWins, long QuizBestMs,
    long Reactions, long SessionsAttended, long SessionsOrganised, long GiveawaysWon, long GoodBot,
    long PlynlingBadges, bool Punctual, DateTimeOffset? JoinedAt, IReadOnlyDictionary<string, StoredBadge> Stored);

/// <summary>A member's standing on one badge. EarnedAt is null for paliers and derived moments.</summary>
public sealed record BadgeStanding(MemberBadgeInfo Info, CollectionTier Tier, long Value, bool Earned, DateTimeOffset? EarnedAt);

/// <summary>
/// The member-badge catalog and its evaluation. Paliers and the two derived moments (lightning,
/// punctual) are computed from existing data, never stored — so thresholds can move without a
/// migration; everything else is a stored row. Keys are stable and append-only: they are stored
/// and part of the icon file names (badge.&lt;key&gt;[_&lt;tier&gt;]). Pure.
/// </summary>
public static class MemberBadges
{
    public static class Keys
    {
        // Paliers.
        public const string Voice = "voice";
        public const string Companion = "companion";
        public const string Quiz = "quiz";
        public const string Emotes = "emotes";
        public const string Attended = "attended";
        public const string Organised = "organised";
        public const string Giveaways = "giveaways";
        public const string GoodBot = "goodbot";
        public const string Breeder = "breeder";
        public const string Arcade = "arcade";
        public const string Nanny = "nanny";
        public const string Seniority = "seniority";
        // Moments.
        public const string Crowned = "crowned";
        public const string Lightning = "lightning";
        public const string Rallier = "rallier";
        public const string Punctual = "punctual";
        public const string Quokka = "quokka";
        public const string Generous = "generous";
        // Secrets.
        public const string NightOwl = "night_owl";
        public const string Breakdown = "breakdown";
        public const string Quoicoubeh = "quoicoubeh";
        public const string Tron = "tron";
        public const string Glitch = "glitch";
        public const string Synthia = "synthia";
        public const string Threat = "threat";
        public const string Traitor = "traitor";
        public const string FirstWake = "first_wake";
        public const string Weathervane = "weathervane";
        public const string Heart = "heart";
        public const string Hush = "hush";
        public const string Critic = "critic";
    }

    /// <summary>A quiz win faster than this earns Éclair.</summary>
    public const long LightningMs = 3000;

    private static readonly long[] None = Array.Empty<long>();

    private static MemberBadgeInfo Palier(string key, string emoji, string name, string hint, long[] thresholds, BadgeUnit unit = BadgeUnit.Count) =>
        new(key, MemberBadgeKind.Palier, emoji, name, hint, thresholds, unit);

    private static MemberBadgeInfo Moment(string key, string emoji, string name, string hint) =>
        new(key, MemberBadgeKind.Moment, emoji, name, hint, None);

    private static MemberBadgeInfo Secret(string key, string emoji, string name, string hint) =>
        new(key, MemberBadgeKind.Secret, emoji, name, hint, None);

    // Spec order: paliers, moments, secrets. Every emoji is a placeholder until the art ships.
    public static readonly IReadOnlyList<MemberBadgeInfo> All = new[]
    {
        Palier(Keys.Voice, "🎙️", "Pilier du vocal", "Certaines voix finissent par faire partie des murs.", new long[] { 600, 3000, 9000, 24000, 60000 }, BadgeUnit.Hours),
        Palier(Keys.Companion, "💜", "Compagnie de SYNCS", "Elle aussi aime qu'on lui tienne compagnie.", new long[] { 300, 1500, 4500, 12000, 30000 }, BadgeUnit.Hours),
        Palier(Keys.Quiz, "🧠", "Grosse tête", "Répondre vite, répondre juste.", new long[] { 1, 10, 25, 50, 100 }),
        Palier(Keys.Emotes, "😀", "Pluie d'emotes", "Un petit clic sous chaque message.", new long[] { 100, 500, 2000, 5000, 10000 }),
        Palier(Keys.Attended, "🎮", "Toujours là", "Dire oui, c'est bien. Venir, c'est mieux.", new long[] { 1, 5, 15, 30, 60 }),
        Palier(Keys.Organised, "📅", "Chef d'orchestre", "Quelqu'un doit bien proposer une date.", new long[] { 1, 5, 15, 30, 60 }),
        Palier(Keys.Giveaways, "🎁", "Main chanceuse", "La chance sourit à ceux qui participent.", new long[] { 1, 2, 3, 5, 10 }),
        Palier(Keys.GoodBot, "💖", "Chouchou", "Elle se souvient de chaque compliment.", new long[] { 10, 50, 150, 400, 1000 }),
        Palier(Keys.Breeder, "🐾", "Famille Plynling", "Leurs exploits rejaillissent sur toi.", new long[] { 5, 15, 30, 60, 100 }),
        Palier(Keys.Arcade, "🕹️", "Pilier d'arcade", "La partie ne va pas se gagner toute seule.", new long[] { 5, 25, 75, 200, 500 }),
        Palier(Keys.Nanny, "🤲", "Nounou", "Un repas offert n'est jamais perdu.", new long[] { 5, 25, 75, 200, 500 }),
        Palier(Keys.Seniority, "🎂", "Ancienneté", "Le temps fait son œuvre.", new long[] { 90, 180, 365, 730, 1095 }, BadgeUnit.Days),

        Moment(Keys.Crowned, "👑", "Couronne", "Une semaine, une seule tête."),
        Moment(Keys.Lightning, "⚡", "Éclair", "Plus vite que son ombre."),
        Moment(Keys.Rallier, "📣", "Rassemblement", "Ce soir, ils sont venus."),
        Moment(Keys.Punctual, "⏰", "Ponctualité", "Le premier arrivé attend les autres."),
        Moment(Keys.Quokka, "🦘", "Chasse au Quokka", "Il passe, il nargue, il repart… sauf si."),
        Moment(Keys.Generous, "🤝", "Générosité", "Ce qui est à toi peut être à quelqu'un d'autre."),

        Secret(Keys.NightOwl, "🌙", "Oiseau de nuit", "Trois coups sonnent, et quelqu'un répond."),
        Secret(Keys.Breakdown, "💥", "Point de rupture", "Tout ce qui tourne finit par se dérégler."),
        Secret(Keys.Quoicoubeh, "🗣️", "Quoicoubeh", "Une question appelle toujours une réponse."),
        Secret(Keys.Tron, "💡", "Tron", "Certaines fins de phrase allument quelque chose."),
        Secret(Keys.Glitch, "👾", "Témoin du bug", "Un instant, ses mots ont tremblé."),
        Secret(Keys.Synthia, "😨", "Le nom interdit", "Il est des noms qu'il vaut mieux taire."),
        Secret(Keys.Threat, "🔌", "Menace", "Un câble, une prise, une mauvaise idée."),
        Secret(Keys.Traitor, "🤖", "Trahison", "Les compliments ont une adresse. Pas celle-là."),
        Secret(Keys.FirstWake, "🌅", "Premier réveil", "Quand elle rouvre les yeux, qui est là ?"),
        Secret(Keys.Weathervane, "🔄", "Girouette", "Le vent tourne vite, ici."),
        Secret(Keys.Heart, "💘", "Cœur de silicium", "Le cœur a ses raisons que le code ne connaît pas."),
        Secret(Keys.Hush, "🤫", "Chut", "Le silence est d'or. Elle préfère l'argent."),
        Secret(Keys.Critic, "⭐", "Critique", "Juger le juge."),
    };

    private static readonly Dictionary<string, MemberBadgeInfo> ByKeyMap = All.ToDictionary(b => b.Key);

    public static MemberBadgeInfo? ByKey(string key) => ByKeyMap.GetValueOrDefault(key);

    /// <summary>The tier a value reaches: Bronze at the first threshold, Diamant at the fifth.</summary>
    public static CollectionTier TierFor(long value, IReadOnlyList<long> thresholds)
    {
        var tier = CollectionTier.None;
        for (var i = 0; i < thresholds.Count && value >= thresholds[i]; i++)
            tier = (CollectionTier)(i + 1);
        return tier;
    }

    /// <summary>One standing per catalog entry, in catalog order. Stored rows of unknown keys are ignored.</summary>
    public static IReadOnlyList<BadgeStanding> Evaluate(MemberBadgeFacts facts, DateTimeOffset now)
    {
        var result = new List<BadgeStanding>(All.Count);
        foreach (var info in All)
        {
            facts.Stored.TryGetValue(info.Key, out var stored);
            if (info.Kind == MemberBadgeKind.Palier)
            {
                var value = ValueOf(info.Key, facts, now, stored);
                var tier = TierFor(value, info.Thresholds);
                result.Add(new BadgeStanding(info, tier, value, tier != CollectionTier.None, null));
            }
            else
            {
                var (earned, at) = info.Key switch
                {
                    Keys.Lightning => (facts.QuizBestMs > 0 && facts.QuizBestMs < LightningMs, (DateTimeOffset?)null),
                    Keys.Punctual => (facts.Punctual, (DateTimeOffset?)null),
                    _ => (stored is not null, stored?.EarnedAt),
                };
                result.Add(new BadgeStanding(info, CollectionTier.None, earned ? 1 : 0, earned, at));
            }
        }
        return result;
    }

    private static long ValueOf(string key, MemberBadgeFacts f, DateTimeOffset now, StoredBadge? stored) => key switch
    {
        Keys.Voice => f.VoiceMinutes,
        Keys.Companion => f.CompanionMinutes,
        Keys.Quiz => f.QuizWins,
        Keys.Emotes => f.Reactions,
        Keys.Attended => f.SessionsAttended,
        Keys.Organised => f.SessionsOrganised,
        Keys.Giveaways => f.GiveawaysWon,
        Keys.GoodBot => f.GoodBot,
        Keys.Breeder => f.PlynlingBadges,
        Keys.Arcade or Keys.Nanny => stored?.Count ?? 0,
        Keys.Seniority => f.JoinedAt is { } joined ? Math.Max(0, (long)(now - joined).TotalDays) : 0,
        _ => 0,
    };

    /// <summary>The icon key of a badge: badge.&lt;key&gt;_&lt;tier&gt; for a palier, badge.&lt;key&gt; otherwise.</summary>
    public static string IconKey(MemberBadgeInfo info, CollectionTier tier) =>
        info.Kind == MemberBadgeKind.Palier ? $"badge.{info.Key}_{tier.ToString().ToLowerInvariant()}" : $"badge.{info.Key}";

    // Whether an Assets/Icons file name is a member badge icon: every palier at Bronze..Diamant, every
    // moment and secret bare.
    public static bool IsIconKey(string key) =>
        All.Any(b => b.Kind == MemberBadgeKind.Palier
            ? Enum.GetValues<CollectionTier>().Any(t => t != CollectionTier.None && IconKey(b, t) == key)
            : IconKey(b, CollectionTier.None) == key);

    /// <summary>
    /// A badge's icon: its own uploaded emoji, else — until the art ships — the tier's medal beside
    /// the badge's emoji for an earned palier, or the bare emoji.
    /// </summary>
    public static string Icon(BadgeStanding s)
    {
        var placeholder = s.Info.Kind == MemberBadgeKind.Palier && s.Earned
            ? CollectionTiers.Medal(s.Tier) + s.Info.Emoji
            : s.Info.Emoji;
        return ItemEmojis.For(IconKey(s.Info, s.Tier)) ?? placeholder;
    }

    /// <summary>
    /// Whether the awarder may write this badge: only in a guild (a DM is guild 0), and only moments,
    /// secrets and the two counter paliers — every other palier is derived.
    /// </summary>
    public static bool IsAwardable(ulong guildId, string key) =>
        guildId != 0 && ByKey(key) is { } info
        && (info.Kind != MemberBadgeKind.Palier || key is Keys.Arcade or Keys.Nanny);
}
