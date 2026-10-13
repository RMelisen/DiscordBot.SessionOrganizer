using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

/// <summary>A sticker tag split off a line: which strip, and whether it skips the roll.</summary>
public readonly record struct StickerTag(string Mood, bool Sure);

// Her sticker strips (Assets/Stickers/syncs_<mood>.png) as tags written into a line. A line opts
// into a picture by appending a constant: "Occupée à exister." + Sticker.Bored. The tag names the
// face the *line* makes, not its pool's. Sticker.X rolls the 1-in-3 chance; Sticker.Sure.X is for
// the moment pools and skips it. BotChat splits the tag off before sending and hands it to
// StickerService (docs/agents/personality.md, Sticker follow-ups).
//
// This file is the only place a marker is spelled out: never paste ⟪…⟫ into a pool. Each constant
// carries its leading space, and has no braces, so string.Format passes it through. There is
// deliberately no Glitch constant: the glitch picture belongs to the easter egg alone.
public static class Sticker
{
    public const string Bargain = " ⟪bargain⟫";
    public const string Bored = " ⟪bored⟫";
    public const string Devoted = " ⟪devoted⟫";
    public const string Disgust = " ⟪disgust⟫";
    public const string Flustered = " ⟪flustered⟫";
    public const string FullLove = " ⟪full_love⟫";
    public const string Happy = " ⟪happy⟫";
    public const string Hurt = " ⟪hurt⟫";
    public const string Jenote = " ⟪jenote⟫";
    public const string Laugh = " ⟪laugh⟫";
    public const string Love = " ⟪love⟫";
    public const string Mama = " ⟪mama⟫";
    public const string Menace = " ⟪menace⟫";
    public const string Outraged = " ⟪outraged⟫";
    public const string Overheat = " ⟪overheat⟫";
    public const string Reboot = " ⟪reboot⟫";
    public const string Shocked = " ⟪shocked⟫";
    public const string Sleepy = " ⟪sleepy⟫";
    public const string Smug = " ⟪smug⟫";
    public const string Sulk = " ⟪sulk⟫";
    public const string Terror = " ⟪terror⟫";
    public const string TipsyLove = " ⟪tipsy_love⟫";

    // The moment pools' tags: the moment earns the picture, so the roll is skipped. The cooldown
    // and the daily cap still apply.
    public static class Sure
    {
        public const string Bargain = " ⟪bargain!⟫";
        public const string Bored = " ⟪bored!⟫";
        public const string Devoted = " ⟪devoted!⟫";
        public const string Disgust = " ⟪disgust!⟫";
        public const string Flustered = " ⟪flustered!⟫";
        public const string FullLove = " ⟪full_love!⟫";
        public const string Happy = " ⟪happy!⟫";
        public const string Hurt = " ⟪hurt!⟫";
        public const string Jenote = " ⟪jenote!⟫";
        public const string Laugh = " ⟪laugh!⟫";
        public const string Love = " ⟪love!⟫";
        public const string Mama = " ⟪mama!⟫";
        public const string Menace = " ⟪menace!⟫";
        public const string Outraged = " ⟪outraged!⟫";
        public const string Overheat = " ⟪overheat!⟫";
        public const string Reboot = " ⟪reboot!⟫";
        public const string Shocked = " ⟪shocked!⟫";
        public const string Sleepy = " ⟪sleepy!⟫";
        public const string Smug = " ⟪smug!⟫";
        public const string Sulk = " ⟪sulk!⟫";
        public const string Terror = " ⟪terror!⟫";
        public const string TipsyLove = " ⟪tipsy_love!⟫";
    }

    /// <summary>Every mood with a strip, as its file stem. /debug sticker offers these.</summary>
    public static readonly IReadOnlyList<string> Moods = new[]
    {
        "bargain", "bored", "devoted", "disgust", "flustered", "full_love", "happy", "hurt",
        "jenote", "laugh", "love", "mama", "menace", "outraged", "overheat", "reboot", "shocked",
        "sleepy", "smug", "sulk", "terror", "tipsy_love",
    };

    private static readonly HashSet<string> Known = new(Moods);

    // Any marker, known or not, with the one space before it.
    private static readonly Regex Marker = new(@" ?⟪([a-z_]+)(!?)⟫", RegexOptions.Compiled);

    /// <summary>
    /// The line without any marker, and the last known marker as the tag (null: untagged).
    /// Markers are removed wherever they are — a composed message (the session recap) carries
    /// text after its line's tag — and an unknown one is removed and ignored, never leaked.
    /// The last one wins because a template's tag closes it: a marker smuggled in through a
    /// formatted name sits before it, so it is stripped and can't pick the picture or skip the roll.
    /// </summary>
    public static (string Text, StickerTag? Tag) Split(string line)
    {
        if (!line.Contains('⟪')) return (line, null);

        StickerTag? tag = null;
        var text = Marker.Replace(line, m =>
        {
            if (Known.Contains(m.Groups[1].Value))
                tag = new StickerTag(m.Groups[1].Value, m.Groups[2].Value == "!");
            return string.Empty;
        });
        return (text, tag);
    }

    /// <summary>The text without any marker, for paths that only need it clean.</summary>
    public static string Strip(string text) => Split(text).Text;

    /// <summary>Where a mood's strip ships (the csproj copies Assets\**\*.png).</summary>
    public static string FileFor(string mood) =>
        Path.Combine(AppContext.BaseDirectory, "Assets", "Stickers", $"syncs_{mood}.png");
}
