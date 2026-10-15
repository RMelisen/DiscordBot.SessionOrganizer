namespace ProjectSYNCS.Helpers;

/// <summary>
/// The machines she has feelings about (docs/syncs-voice.md, Her robot heart). In memory only,
/// never stored, so order and names are free to change.
/// </summary>
public enum HeartCharacter { WallE, Eve, Glados, Wheatley, Vacuum, Monika, TwoB, Voyager, Jarvis }

/// <summary>What she says when someone talking to her names one of them.</summary>
public enum HeartActKind
{
    /// <summary>A reaction on the message and a reply.</summary>
    LineAndEmote,
    /// <summary>A reply, deleted 7–9 s later.</summary>
    VanishLine,
    /// <summary>A reply, edited into a denial 6.5–8 s later.</summary>
    EditSlip,
}

// Pure rules for RobotHeartService. Named in passing, she reacts with an emote — and takes it back
// for the ones she's in denial about — and 40% of the time says something too. Named to her
// face, she answers in words: a reaction + a line, or the character's own Synthia-style move.
// No limits: every naming gets its act.
public static class RobotHeart
{
    /// <summary>Named in passing: after her reaction, the chance she also says something.</summary>
    public const double AmbientSpeakChance = 0.40;

    /// <summary>Of an act in words: the share that is a performance; the rest is line + emote.</summary>
    public const double PerformanceShare = 0.40;

    // How long she leaves it before changing her mind, drawn in [Min, Max) milliseconds. Longer
    // than the Synthia flinch's on purpose: these are things she half-means, and people should
    // get the time to read them.
    public static readonly (int Min, int Max) TakeBackDelayMs = (2000, 4000);
    public static readonly (int Min, int Max) VanishDelayMs = (4000, 6000);
    public static readonly (int Min, int Max) EditDelayMs = (3500, 5000);

    /// <summary>
    /// Named in a message not aimed at her: whether her reaction is taken back. The ones she's in
    /// denial about — her loves and her shameful soft spots. Mentors, friends, EVE and JARVIS keep
    /// theirs: she has nothing to hide about JARVIS, only how much she enjoys being courted.
    /// </summary>
    public static bool TakesBackReaction(HeartCharacter who) =>
        who is HeartCharacter.WallE or HeartCharacter.Voyager or HeartCharacter.Wheatley or HeartCharacter.Vacuum;

    /// <summary>
    /// Named to her face, line + emote: whether the reaction is still taken back. Her two loves
    /// only — the line can stand, but the heart under the message mustn't be caught.
    /// </summary>
    public static bool HidesReactionWhenAimed(HeartCharacter who) =>
        who is HeartCharacter.WallE or HeartCharacter.Voyager;

    /// <summary>
    /// The chance a character's line gets its sticker strip, when it isn't StickerService's
    /// ordinary 1 in 3 (null). WALL-E's: 2 in 3 — her face gives her away more often than not.
    /// RobotHeartService rolls it and hands the tag on promoted to sure, so the strip doesn't roll
    /// twice. Decided here, in code: the lines keep ordinary tags, which name the picture.
    /// </summary>
    public static double? StickerChance(HeartCharacter who) => who == HeartCharacter.WallE ? 2.0 / 3 : null;

    /// <summary>Whether <paramref name="roll"/> in [0, 1) wins the character's own sticker chance.</summary>
    public static bool WinsSticker(HeartCharacter who, double roll) => StickerChance(who) is { } chance && roll < chance;

    /// <summary>The character's word performances (the vacuum has none).</summary>
    public static IReadOnlyList<HeartActKind> PerformancesOf(HeartCharacter who) => who switch
    {
        HeartCharacter.WallE => [HeartActKind.EditSlip],
        HeartCharacter.Eve => [HeartActKind.EditSlip],
        HeartCharacter.Glados => [HeartActKind.EditSlip],
        HeartCharacter.Wheatley => [HeartActKind.VanishLine],
        HeartCharacter.Monika => [HeartActKind.VanishLine],
        HeartCharacter.TwoB => [HeartActKind.EditSlip],
        HeartCharacter.Voyager => [HeartActKind.EditSlip],
        HeartCharacter.Jarvis => [HeartActKind.EditSlip],
        _ => [],
    };

    /// <summary>
    /// The act for a naming aimed at her. <paramref name="roll"/> and <paramref name="pick"/> are
    /// in [0, 1): the first chooses performance or line + emote, the second among the character's
    /// performances. Without one, it's line + emote.
    /// </summary>
    public static HeartActKind RollAct(HeartCharacter who, double roll, double pick)
    {
        if (roll >= PerformanceShare) return HeartActKind.LineAndEmote;

        var options = PerformancesOf(who);
        if (options.Count == 0) return HeartActKind.LineAndEmote;
        return options[Math.Min(options.Count - 1, (int)(pick * options.Count))];
    }
}
