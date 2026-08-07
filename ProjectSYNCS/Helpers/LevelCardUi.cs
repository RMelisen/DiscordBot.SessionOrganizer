using System.Globalization;
using ProjectSYNCS.Services;

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
    /// A duration in whole minutes, read as a human would say it: "3 h 05" past an
    /// hour, "45 min" below one, and days once it passes 24 h — a voice total climbs
    /// into the hundreds of hours over a year, where raw minutes stop meaning anything.
    /// </summary>
    public static string Duration(long minutes)
    {
        if (minutes <= 0) return "0 min";
        if (minutes < 60) return $"{minutes} min";

        var hours = minutes / 60;
        var restMinutes = minutes % 60;

        if (hours < 24) return $"{hours} h {restMinutes:00}";

        var days = hours / 24;
        return $"{days} j {hours % 24} h";
    }

    /// <summary>
    /// One leaderboard row. Two lines on purpose: a Section gives real vertical room
    /// beside its avatar, and a single line next to one looks stranded. Rendered as a
    /// mention so the name stays current and stays clickable — callers must send with
    /// AllowedMentions.None, since a TextDisplay is real message content and would
    /// otherwise ping everyone on the page on every re-render.
    /// </summary>
    /// <remarks>
    /// The second line is whatever the current view ranks by, and only that: showing
    /// all three numbers on every row would make the ordering look arbitrary, since
    /// two of them wouldn't explain it. The XP view keeps the level alongside, because
    /// there the level *is* the headline and the XP is how it was reached.
    /// </remarks>
    public static string Row(int rank, ulong userId, LeaderboardView view, StatsPeriod period, MemberTally tally) =>
        $"{RankMarker(rank)} **<@{userId}>**\n{RowValue(view, period, tally)}";

    // The XP line is the only one that changes with the period, and it has to: a level
    // is a property of the *lifetime* total, so it cannot be recomputed for a window,
    // and printing it beside a 7-day XP figure would make the ordering look wrong.
    // A count is a count, so the other two views read the same either way.
    private static string RowValue(LeaderboardView view, StatsPeriod period, MemberTally tally) => view switch
    {
        LeaderboardView.Reactions => $"{Xp(tally.ReactionsUsed)} réaction(s)",
        LeaderboardView.Voice => Duration(tally.VoiceMinutes),
        _ when period != StatsPeriod.AllTime => $"{Xp(tally.TotalXp)} XP gagnés",
        _ => $"Niveau {tally.Level} · {Xp(tally.TotalXp)} XP",
    };

    /// <summary>The heading above the ranking, naming what is being ranked.</summary>
    public static string Title(LeaderboardView view) => view switch
    {
        LeaderboardView.Reactions => "## Classement des réactions",
        LeaderboardView.Voice => "## Classement du vocal",
        _ => "## Classement",
    };

    /// <summary>The button label for a view.</summary>
    public static string ViewLabel(LeaderboardView view) => view switch
    {
        LeaderboardView.Reactions => "Réactions",
        LeaderboardView.Voice => "Vocal",
        _ => "Niveaux",
    };

    /// <summary>
    /// What an empty board says. A window being empty is ordinary — nobody did this
    /// *lately* — while an empty all-time board really does mean nobody ever, so the
    /// two read differently and neither gets reported as broken.
    /// </summary>
    public static string EmptyLine(LeaderboardView view, StatsPeriod period)
    {
        if (period != StatsPeriod.AllTime)
            return "Rien sur cette période. Vous étiez tous où ? (ᵕ • ᴗ •)";

        return view switch
        {
            LeaderboardView.Reactions =>
                "Personne n'a encore réagi à quoi que ce soit. Je compte depuis peu, laissez-moi le temps (ᵕ • ᴗ •)",
            LeaderboardView.Voice =>
                "Personne n'a encore passé de temps en vocal. Je compte depuis peu, laissez-moi le temps (ᵕ • ᴗ •)",
            _ => "Personne n'a encore gagné d'XP. Parlez, réagissez, faites du bruit ദ്ദി◝ ⩊ ◜.ᐟ",
        };
    }

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
