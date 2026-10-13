namespace ProjectSYNCS.Helpers;

/// <summary>
/// The machines she has feelings about (docs/syncs-voice.md, Her robot heart). In memory only,
/// never stored, so order and names are free to change.
/// </summary>
public enum HeartCharacter { WallE, Eve, Glados, Wheatley, Vacuum, Monika, TwoB, Voyager }

/// <summary>What she does when one of them is named.</summary>
public enum HeartActKind
{
    /// <summary>A reaction on the message and a reply.</summary>
    LineAndEmote,
    /// <summary>A reaction, taken back 5–7 s later. Wordless.</summary>
    TakeBackReaction,
    /// <summary>A reply, deleted 7–9 s later.</summary>
    VanishLine,
    /// <summary>A reply, edited into a denial 6.5–8 s later.</summary>
    EditSlip,
}

// Pure rules for RobotHeartService: which act, and how often. The performances are the
// Synthia flinch's three moves, each character keeping the ones that fit its register.
public static class RobotHeart
{
    /// <summary>Not aimed at her: the chance she reacts at all (a lore test shouldn't always work).</summary>
    public const double AmbientChance = 0.60;

    /// <summary>Of an act: the share that is a performance; the rest is line + emote.</summary>
    public const double PerformanceShare = 0.40;

    /// <summary>Acts a day, all characters together (Paris day).</summary>
    public const int DailyCap = 3;

    // How long she leaves it before changing her mind, drawn in [Min, Max) milliseconds. Longer
    // than the Synthia flinch's on purpose: these are things she half-means, and people should
    // get the time to read them.
    public static readonly (int Min, int Max) TakeBackDelayMs = (5000, 7000);
    public static readonly (int Min, int Max) VanishDelayMs = (7000, 9000);
    public static readonly (int Min, int Max) EditDelayMs = (6500, 8000);

    /// <summary>
    /// The chance a character's line gets its sticker strip, when it isn't StickerService's
    /// ordinary 1 in 3 (null). WALL-E's: 2 in 3 — her face gives her away more often than not.
    /// RobotHeartService rolls it and hands the tag on promoted to sure, so the strip doesn't roll
    /// twice. Decided here, in code: the lines keep ordinary tags, which name the picture.
    /// </summary>
    public static double? StickerChance(HeartCharacter who) => who == HeartCharacter.WallE ? 2.0 / 3 : null;

    /// <summary>Whether <paramref name="roll"/> in [0, 1) wins the character's own sticker chance.</summary>
    public static bool WinsSticker(HeartCharacter who, double roll) => StickerChance(who) is { } chance && roll < chance;

    public static IReadOnlyList<HeartActKind> PerformancesOf(HeartCharacter who) => who switch
    {
        HeartCharacter.WallE => [HeartActKind.TakeBackReaction, HeartActKind.EditSlip],
        HeartCharacter.Eve => [HeartActKind.EditSlip],
        HeartCharacter.Glados => [HeartActKind.EditSlip],
        HeartCharacter.Wheatley => [HeartActKind.VanishLine],
        HeartCharacter.Vacuum => [HeartActKind.TakeBackReaction],
        HeartCharacter.Monika => [HeartActKind.VanishLine],
        HeartCharacter.TwoB => [HeartActKind.EditSlip],
        HeartCharacter.Voyager => [HeartActKind.TakeBackReaction, HeartActKind.EditSlip],
        _ => [],
    };

    /// <summary>
    /// The act for one naming. <paramref name="roll"/> and <paramref name="pick"/> are in [0, 1):
    /// the first chooses performance or line + emote, the second among the character's
    /// performances. Aimed at her, a wordless performance isn't eligible: she answers in words.
    /// </summary>
    public static HeartActKind RollAct(HeartCharacter who, bool aimedAtHer, double roll, double pick)
    {
        if (roll >= PerformanceShare) return HeartActKind.LineAndEmote;

        var options = PerformancesOf(who)
            .Where(p => !aimedAtHer || p != HeartActKind.TakeBackReaction)
            .ToList();
        if (options.Count == 0) return HeartActKind.LineAndEmote;
        return options[Math.Min(options.Count - 1, (int)(pick * options.Count))];
    }
}

/// <summary>
/// RobotHeartService's gate, shared with nobody: each character at most once per 24 h, and
/// <see cref="RobotHeart.DailyCap"/> acts per Paris day in all. Owns its lock.
/// </summary>
public sealed class HeartGate
{
    private readonly CooldownGate<HeartCharacter> _perCharacter = new(TimeSpan.FromDays(1));
    private readonly object _lock = new();
    private int _day;
    private int _count;

    public bool TryClaim(HeartCharacter who, int dayNumber)
    {
        lock (_lock)
        {
            if (_day != dayNumber)
            {
                _day = dayNumber;
                _count = 0;
            }
            if (_count >= RobotHeart.DailyCap) return false;
            if (!_perCharacter.TryClaim(who)) return false;
            _count++;
            return true;
        }
    }
}
