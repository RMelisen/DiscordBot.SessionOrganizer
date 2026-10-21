using System.Globalization;
using Discord;

namespace ProjectSYNCS.Helpers;

// What a MyStats question asks about (« je suis quel niveau ? », « j'ai combien de cailloux ? »,
// « comment va mon plynling ? »), or All for « mes stats ». In memory only: free to reorder.
public enum MyStatsTopic
{
    All,
    Level,
    Balance,
    Plynling,
}

/// <summary>
/// Someone's own numbers in finished words for her chat lines ({2} and {3}), read from /recap's
/// all-time stats (RecapStats), so chat and /recap agree. Null when there is nothing to say on that
/// topic (no XP yet, no living Plynling): the caller has a pool for that. Pure.
/// </summary>
public static class MyStats
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    /// <returns>{2} and {3}; null when the topic has nothing to show.</returns>
    public static (string A, string B)? Describe(RecapStats s, MyStatsTopic topic) => topic switch
    {
        MyStatsTopic.Level => Level(s) is { } level ? (level, Rank(s)) : null,
        MyStatsTopic.Balance => (Cailloux(s.Balance), string.Empty),
        MyStatsTopic.Plynling => s.PlynlingName is { Length: > 0 } name ? (Format.Sanitize(name), Age(s.PlynlingAge)) : null,
        _ => (All(s), string.Empty),
    };

    // « niveau 12, n° 4 du classement, 1 250 cailloux et Pixel (12 jours) ».
    private static string All(RecapStats s)
    {
        var parts = new List<string>();
        if (Level(s) is { } level) parts.Add($"{level}, {Rank(s)}");
        parts.Add(Cailloux(s.Balance));
        if (s.PlynlingName is { Length: > 0 } name) parts.Add($"{Format.Sanitize(name)} ({Age(s.PlynlingAge)})");
        return parts.Count == 1 ? parts[0] : $"{string.Join(", ", parts.Take(parts.Count - 1))} et {parts[^1]}";
    }

    private static string? Level(RecapStats s) => s is { Level: { } level, Xp: > 0 } ? $"niveau {level}" : null;

    // « n° 4 » as her fact lines write it (ChatFacts), never « 4e » / « 1er » / « 1re »: the number
    // reads the same whoever holds it.
    private static string Rank(RecapStats s) => s.Rank is { } rank ? $"n° {rank} du classement" : "hors classement";

    private static string Cailloux(long balance) => balance switch
    {
        0 => "zéro caillou",
        1 => "1 caillou",
        _ => $"{balance.ToString("N0", French)} cailloux",
    };

    private static string Age(TimeSpan? age) => age?.TotalDays switch
    {
        null or < 1 => "quelques heures",
        < 2 => "1 jour",
        { } days => $"{(int)days} jours",
    };
}
