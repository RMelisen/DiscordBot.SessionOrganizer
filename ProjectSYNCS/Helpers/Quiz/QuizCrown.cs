namespace ProjectSYNCS.Helpers;

/// <summary>One won round, as the crown needs it.</summary>
public readonly record struct CrownWin(ulong WinnerId, long Reward, DateTimeOffset PostedAt, DateTimeOffset WonAt);

/// <summary>One answerer's week.</summary>
public sealed record CrownEntry(ulong UserId, int Wins, long Cailloux, double AvgMs);

// « Grosse tête de la semaine »: which week is being rewarded right now, and who won it. Pure, so the
// Monday 10:00 edge and the DST Sunday are checkable without a clock or a gateway.
public static class QuizCrown
{
    /// <summary>
    /// Monday, this hour Paris: the crown changes hands — after the morning hello (8:00–10:00),
    /// before the first quiz slot (11:00).
    /// </summary>
    public const int CrownHour = 10;

    /// <summary>
    /// The DayKey of the Monday starting the week whose champion reigns at <paramref name="now"/>:
    /// from Monday 10:00 Paris, the week that just ended; before that, the one before it. Date
    /// arithmetic on the Paris calendar date only, so a DST change never skips or doubles a week.
    /// </summary>
    public static int RewardedWeekStart(DateTimeOffset now)
    {
        var paris = AppTime.ToZoned(now);
        var date = paris.Date;
        int sinceMonday = ((int)date.DayOfWeek + 6) % 7;   // Monday = 0
        var thisMonday = date.AddDays(-sinceMonday);
        bool crownedYet = sinceMonday > 0 || paris.Hour >= CrownHour;
        return Key(thisMonday.AddDays(crownedYet ? -7 : -14));
    }

    /// <summary>The week's first and last DayKeys, Monday to Sunday.</summary>
    public static (int From, int To) WeekRange(int weekStart) =>
        (weekStart, Key(AppTime.FromDayKey(weekStart).AddDays(6)));

    /// <summary>
    /// Everyone tied for first: most wins, then most cailloux (harder questions pay more), then the
    /// fastest average answer. Empty when nobody won anything.
    /// </summary>
    public static IReadOnlyList<CrownEntry> Champions(IEnumerable<CrownWin> wins)
    {
        var ranked = wins
            .GroupBy(w => w.WinnerId)
            .Select(g => new CrownEntry(g.Key, g.Count(), g.Sum(w => w.Reward),
                g.Average(w => (w.WonAt - w.PostedAt).TotalMilliseconds)))
            .OrderByDescending(e => e.Wins).ThenByDescending(e => e.Cailloux).ThenBy(e => e.AvgMs)
            .ToList();
        if (ranked.Count == 0) return ranked;

        var top = ranked[0];
        return ranked
            .TakeWhile(e => e.Wins == top.Wins && e.Cailloux == top.Cailloux && e.AvgMs == top.AvgMs)
            .ToList();
    }

    /// <summary>« 1 bonne réponse » / « 5 bonnes réponses ».</summary>
    public static string WinsText(int wins) => wins == 1 ? "1 bonne réponse" : $"{wins} bonnes réponses";

    /// <summary>« &lt;@a&gt; », « &lt;@a&gt; et &lt;@b&gt; », « &lt;@a&gt;, &lt;@b&gt; et &lt;@c&gt; ».</summary>
    public static string JoinMentions(IReadOnlyList<ulong> ids)
    {
        var mentions = ids.Select(id => $"<@{id}>").ToList();
        return mentions.Count <= 1
            ? string.Concat(mentions)
            : string.Join(", ", mentions.Take(mentions.Count - 1)) + " et " + mentions[^1];
    }

    private static int Key(DateTime date) => date.Year * 10000 + date.Month * 100 + date.Day;
}
