using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Whether a conversation is friendly or tense: rivals and conflicts are tense, every other bond friendly.
public enum ConvoFlavor { Friendly, Tense }

// One line of a script: who says it (A raised the subject, B is the other) and what.
public sealed record ConvoLine(bool ByA, string Text);

/// <summary>
/// A conversation written as one piece: two, four, six or eight lines that each answer the one before. Line 0 is
/// A's opener — narration, then A's words, separated by one \n; the rest are one spoken line each, from
/// whichever of them the script says, so one may speak twice in a row. The story shows them two to a
/// step, so the count must stay even. The last line leads into doing something. {S} is A, {L} is B; {s:m|f} agrees with A, {l:m|f} with B; {P} is the subject (custom
/// scripts only, and never after « de » or « à »). A line may start with a face tag — see
/// <see cref="PlynlingVisitStory.Untag"/>.
/// </summary>
public sealed record Script(ConvoLine[] Lines);

/// <summary>
/// What two Plynlings say to each other about a passion, the middle of a visit. Catalog passions have
/// scripts written for them, by flavour and by whether B shares the passion; a typed passion only gets
/// the generic ones, which say nothing about the subject beyond <c>{P}</c>. Generated from the
/// writing sheets — to add a conversation, add a script to the right key.
/// </summary>
public static partial class PlynlingScripts
{
    private static ConvoLine A(string text) => new(true, text);
    private static ConvoLine B(string text) => new(false, text);
    private static Script S(params ConvoLine[] lines) => new(lines);

    public static Script[] For(PlynlingPassion passion, ConvoFlavor flavor, bool shared) => Catalog[(passion, flavor, shared)];

    public static Script[] ForCustom(ConvoFlavor flavor, bool shared) => Custom[(flavor, shared)];

    /// <summary>Every script, for the checks.</summary>
    public static IEnumerable<Script> All => Catalog.Values.Concat(Custom.Values).SelectMany(x => x);

    /// <summary>Every key, with its scripts, for the checks.</summary>
    public static IEnumerable<(string Key, Script[] Scripts)> Keys =>
        Catalog.Select(kv => ($"{kv.Key.Item1}/{kv.Key.Item2}/{kv.Key.Item3}", kv.Value))
            .Concat(Custom.Select(kv => ($"custom/{kv.Key.Item1}/{kv.Key.Item2}", kv.Value)));

    // One method per passion in PlynlingScripts/, merged in passion order. Each part is a method rather
    // than a field: static field order across partial files is undefined.
    private static readonly IReadOnlyDictionary<(PlynlingPassion, ConvoFlavor, bool), Script[]> Catalog =
        new[]
        {
            CookingScripts(), MusicScripts(), GamingScripts(), AstronomyScripts(),
            GardeningScripts(), RocksScripts(), StoriesScripts(), DanceScripts(),
            PaintingScripts(), SportScripts(), InsectsScripts(), NapsScripts(),
        }.SelectMany(part => part).ToDictionary(kv => kv.Key, kv => kv.Value);
}
