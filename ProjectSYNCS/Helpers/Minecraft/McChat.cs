using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// Her Discord pools as she says them in game (<see cref="Game"/>): the same lines, without what only Discord can show.
/// Sticker tags, custom emotes, ✨ and bold go; a line that needs Discord to make sense (a slash command, a tag, a
/// channel, a reaction, struck-through or spoiler text) is left out, as is a tag the game's font can't draw. Kaomoji are
/// kept on one line in four at most, the in-game rate (<c>docs/syncs-voice.md</c>). Pure, and computed once per pool.
/// </summary>
public static partial class McChat
{
    // The longest name she says in game ({0}: 16 letters) and the longest weekday ({1}), for the 256-character cap.
    private const string LongestName = "Abcdefghijklmnop";
    private const string LongestWeekday = "mercredi";
    private const int MaxChat = 256;

    // Words that only mean something on Discord, accent-stripped (Rating.Normalize). « serveur », « message », « sondage »
    // and « session » stay: the Minecraft server is a server too, and her sessions and polls are her life, wherever she is.
    private static readonly HashSet<string> DiscordWords = new()
    {
        "discord", "salon", "salons", "tag", "tags", "tague", "tagues", "taguer", "taguee", "tagge", "taggue", "ping", "pings",
        "pingue", "pingues", "pinguer", "mention", "mentions", "mentionne", "mentionner", "reaction", "reactions", "react",
        "reagis", "reagir", "emote", "emotes", "emoji", "emojis", "sticker", "stickers", "dm", "dms", "mp", "mps", "vocal",
        "vocale", "vocaux", "commande", "commandes", "role", "roles", "epingle", "epingler", "notif", "notifs",
        "notification", "notifications", "scroll", "scroller", "gif", "gifs",
    };

    // Discord markup that would show raw: emotes, mentions, channels, timestamps, code, strike-through, spoilers.
    private static readonly string[] DiscordMarks = { "<:", "<a:", "<@", "<#", "<t:", "`", "~~", "||" };

    // A tag the game's font draws no glyph for, though every code point passes the check below (docs/syncs-voice.md).
    private static readonly string[] Undrawable = { "ദ്ദി◝ ⩊ ◜.ᐟ", "⩊", "👁👄👁️" };

    [GeneratedRegex(@"<a?:\w+:\d+>")]
    private static partial Regex CustomEmote();

    // A slash command written without backticks: « /schedule », « /plynling feed ».
    [GeneratedRegex(@"(^|[\s(«])/[a-z]")]
    private static partial Regex SlashCommand();

    // A kaomoji at the end of a line: a bracketed face with no Latin letter or digit in it (« (¬_¬) », « ( ˶ˆ ᗜ ˆ˵ ) »,
    // « ٩(˶ᵔ ᵕ ᵔ˶)۶ », « (｀・ω・´)ゞ »), so « (enfin presque) » is words. ♡ and UwU are not faces: they stay.
    [GeneratedRegex(@"\s*[^\s\p{IsBasicLatin}]?\((?=[^()]*[^\sa-zA-Z0-9À-ÿ()])[^()a-zA-Z0-9]{1,24}\)[^\s\p{IsBasicLatin}]{0,2}\s*$")]
    private static partial Regex TrailingFace();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex Spaces();

    // The body cuts a whisper at 200 characters (the « /tell name » around it counts towards the game's 256).
    private const int MaxWhisper = 200;

    private static readonly ConcurrentDictionary<string[], string[]> _game = new();
    private static readonly ConcurrentDictionary<string[], string[]> _whispered = new();

    /// <summary>
    /// The pool as she says it in game: each line cleaned (<see cref="Line"/>), the ones that can't be left out,
    /// duplicates dropped, then the faces thinned to one line in four. Whispered, only the lines short enough not to be
    /// cut (all of them if none is). Computed once per pool (by reference); may be empty.
    /// </summary>
    public static string[] Game(string[] pool, bool whispered = false) =>
        whispered ? _whispered.GetOrAdd(pool, p => Game(p) is var all && all.Where(ShortEnough).ToArray() is { Length: > 0 } fit ? fit : all)
            : _game.GetOrAdd(pool, Derive);

    private static bool ShortEnough(string line) => string.Format(line, LongestName, LongestWeekday, "", "").Length <= MaxWhisper;

    private static string[] Derive(string[] pool)
    {
        var lines = pool.Select(Line).OfType<string>().Distinct().ToArray();
        // One face in four lines at most, spread through the pool: a line keeps its face while the kept ones stay
        // under a quarter of the lines seen so far, counting this one.
        var kept = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            if (!TrailingFace().IsMatch(lines[i])) continue;
            if ((kept + 1) * 4 <= i + 1) kept++;
            else lines[i] = TrailingFace().Replace(lines[i], "");
        }
        return lines.Distinct().ToArray();
    }

    /// <summary>One Discord line as she'd say it in game, or null when it only makes sense on Discord (<see cref="WhyNot"/>).</summary>
    public static string? Line(string line) => Clean(line) is var clean && WhyNot(clean) is null ? clean : null;

    /// <summary>
    /// Why a cleaned line can't be said in game (null: it can). For the checks and the owner's review of what was left out.
    /// </summary>
    public static string? WhyNot(string clean)
    {
        if (clean.Length == 0) return "empty";
        if (DiscordMarks.FirstOrDefault(clean.Contains) is { } mark) return $"Discord markup « {mark} »";
        if (SlashCommand().IsMatch(clean)) return "slash command";
        var words = Rating.Normalize(clean).Split(' ');
        // Ping-Qilin is her mascot, not a ping.
        if (words.Where((w, i) => DiscordWords.Contains(w) && !(w == "ping" && i + 1 < words.Length && words[i + 1] == "qilin"))
                .FirstOrDefault() is { } word)
            return $"Discord word « {word} »";
        if (Undrawable.FirstOrDefault(clean.Contains) is { } glyph) return $"undrawable « {glyph} »";
        if (!Drawable(clean)) return "undrawable character";
        try
        {
            if (string.Format(clean, LongestName, LongestWeekday, "", "").Length > MaxChat) return "over 256 characters";
        }
        catch (FormatException)
        {
            return "format";
        }
        return null;
    }

    /// <summary>
    /// What's left of a Discord line in game: no sticker tag, no custom emote, no ✨, no bold, and no face at the end the
    /// game can't draw. Also used on text built from other pools at send time (an opinion's verdict).
    /// </summary>
    public static string Clean(string line)
    {
        var s = CustomEmote().Replace(Sticker.Strip(line), "").Replace("✨", "").Replace("**", "");
        // A face the font can't draw goes, the line stays: « (>⩊<) », « (ง ͠ಥ_ಥ)ง ».
        if (TrailingFace().Match(s) is { Success: true } face
            && (Undrawable.Any(face.Value.Contains) || !Drawable(face.Value)))
            s = s[..face.Index];
        foreach (var glyph in Undrawable)
            if (s.TrimEnd().EndsWith(glyph, StringComparison.Ordinal))
                s = s.TrimEnd()[..^glyph.Length];
        // Emoji at the end (past the BMP), whole words of them.
        var words = s.TrimEnd().Split(' ').ToList();
        while (words.Count > 0 && words[^1].Length > 0 && words[^1].Any(char.IsSurrogate) && !words[^1].Any(char.IsLetter))
            words.RemoveAt(words.Count - 1);
        return Spaces().Replace(string.Join(' ', words), " ").Trim();
    }

    /// <summary>Whether the line ends on a kaomoji (one in four lines at most in game).</summary>
    public static bool HasFace(string line) => TrailingFace().IsMatch(line);

    /// <summary>
    /// Whether Minecraft's font draws every character: the Basic Multilingual Plane one code point at a time, so nothing
    /// past it (most emoji), no combining marks, no format characters, no § (colour codes). The checks' rule.
    /// </summary>
    public static bool Drawable(string s) =>
        s.All(c => !char.IsSurrogate(c) && c != '§' && CharUnicodeInfo.GetUnicodeCategory(c) is not
            (UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark
            or UnicodeCategory.Format));
}
