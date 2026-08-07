using System.Globalization;

namespace ProjectSYNCS.Helpers;

// The text that goes inside /level's card and /leaderboard's rows. Pure string work,
// kept out of LevelModule for the same reason StatsPeriodUi is: the module assembles
// Discord components and cannot be exercised without a gateway, whereas everything
// here is a function of numbers and can be checked outright.
//
// Not shared with /emotestats or /goodbot — those render emote and verdict tallies,
// which have no level, no progress and no podium. A shared "ranking UI" helper would
// only have the page arithmetic in common, which is three lines.
public static class LevelCardUi
{
    /// <summary>Cells in the progress bar. 20 gives 5% resolution, which is as fine as
    /// anyone reads a bar, and stays narrow enough not to wrap on a phone.</summary>
    public const int BarCells = 20;

    // Full block / light shade. Both are plain text glyphs, so they scale with the
    // client's font instead of rendering as images the way emoji squares would.
    private const char Filled = '█';
    private const char Empty = '░';

    // The culture is already loaded and load-bearing here (see AppTime), so grouping
    // costs nothing. Raw "12400" reads as an id; "12 400" reads as a quantity.
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    /// <summary>An XP amount with French thousands grouping — 3400 becomes "3 400".</summary>
    public static string Xp(long amount) => amount.ToString("N0", Fr);

    /// <summary>
    /// The rank marker: a medal for the podium, a plain number below it. Bounded to the
    /// top three deliberately — every row wearing one would read as decoration rather
    /// than as rank.
    /// </summary>
    public static string RankMarker(int rank) => rank switch
    {
        1 => "🥇",
        2 => "🥈",
        3 => "🥉",
        _ => $"**#{rank}**",
    };

    /// <summary>
    /// How far through the current level, 0–100. <paramref name="span"/> is the whole
    /// level's cost, not the cumulative total.
    /// </summary>
    public static int Percent(long into, long span)
    {
        if (span <= 0) return 100;
        return (int)Math.Clamp(into * 100 / span, 0, 100);
    }

    /// <summary>
    /// The bar itself. Clamped at both ends: a level's span is never really zero or
    /// negative, but a bar is decoration and must not be the thing that throws.
    /// </summary>
    public static string ProgressBar(long into, long span)
    {
        var filled = span <= 0
            ? BarCells
            : (int)Math.Clamp(into * BarCells / span, 0, BarCells);

        return new string(Filled, filled) + new string(Empty, BarCells - filled);
    }

    /// <summary>
    /// One leaderboard row. Two lines on purpose: a Section gives real vertical room
    /// beside its avatar, and a single line next to one looks stranded. Rendered as a
    /// mention so the name stays current and stays clickable — callers must send with
    /// AllowedMentions.None, since a TextDisplay is real message content and would
    /// otherwise ping everyone on the page on every re-render.
    /// </summary>
    public static string Row(int rank, ulong userId, int level, long totalXp) =>
        $"{RankMarker(rank)} **<@{userId}>**\nNiveau {level} · {Xp(totalXp)} XP";

    /// <summary>
    /// The headline of /level's card: who, what level, and where they place. Rank is
    /// null for someone with no XP at all — they still get a card, so the placement
    /// line has to say something rather than being omitted.
    /// </summary>
    public static string CardHeading(ulong userId, int level, int? rank) =>
        $"## <@{userId}>\nNiveau **{level}** · {(rank is { } r ? $"rang **#{r}**" : "non classé")}";

    /// <summary>
    /// The progress block under the heading. <paramref name="into"/> is XP earned into
    /// the current level and <paramref name="span"/> what the level costs in full, so
    /// the two numbers shown are the ones the bar is actually drawn from.
    /// </summary>
    public static string CardProgress(long into, long span, int nextLevel, long totalXp) =>
        $"`{ProgressBar(into, span)}`  {Percent(into, span)}%\n"
        + $"{Xp(into)} / {Xp(span)} XP vers le niveau {nextLevel}  ·  {Xp(totalXp)} XP au total";
}
