using ProjectSYNCS.Services;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// One person's /recap over one window, as RecapService read it. In a window every counter is the
/// sum of that window's daily buckets; all-time reads the totals. Level, rank and the quiz record
/// are lifetime figures, so they are null (or 0) in a window, as on /leaderboard.
/// </summary>
/// <remarks>
/// The Plynling, the graves and the balance are never windowed: they are state, not a flow, and
/// nothing stores a person's cailloux by day.
/// </remarks>
public sealed record RecapStats(
    StatsPeriod Period,
    long Xp, int? Level, int? Rank,
    long Reactions, long VoiceMinutes, long MinutesWithHer,
    long GoodBot, long BadBot,
    long QuizWins, long QuizBestMs,
    long MeanHits, long ShoutHits, long PerfidyHits, long BanVotes, long AbandonHits,
    int SessionsJoined, int SessionsAttended, int SessionsMissed,
    int TonightIn,
    string? PlynlingName, TimeSpan? PlynlingAge, int Graves,
    long Balance)
{
    public long ShamePoints => MeanHits + ShoutHits + PerfidyHits + BanVotes + AbandonHits;
}

// Her one-line read of a recap. Each kind has its own pool (BotResponses.RecapLines). In memory
// only, never stored: free to reorder.
public enum RecapProfileKind
{
    Ghost,
    Menace,
    Hater,
    Flaker,
    QuizBrain,
    Loyal,
    VoiceDweller,
    Fan,
    Chatterbox,
    Balanced,
}

/// <summary>
/// Which line /recap ends on: the first trait the numbers clearly show, in a fixed order, so the
/// worst habit wins over the nicest one (she notices the shame before the « good bot »s).
/// Thresholds scale with the window: what is a lot in a week is nothing over all time.
/// </summary>
public static class RecapProfile
{
    public static RecapProfileKind Classify(RecapStats s)
    {
        var f = s.Period switch
        {
            StatsPeriod.Week => 1,
            StatsPeriod.Month => 4,
            _ => 12,
        };

        if (s.Xp == 0 && s.Reactions == 0 && s.VoiceMinutes == 0 && s.GoodBot + s.BadBot == 0
            && s.QuizWins == 0 && s.SessionsJoined == 0 && s.TonightIn == 0 && s.ShamePoints == 0)
            return RecapProfileKind.Ghost;

        if (s.ShamePoints >= 5 * f) return RecapProfileKind.Menace;
        if (s.BadBot >= 2 && s.BadBot > s.GoodBot) return RecapProfileKind.Hater;
        if (s.SessionsMissed >= 2 && s.SessionsMissed > s.SessionsAttended) return RecapProfileKind.Flaker;
        if (s.QuizWins >= Math.Max(2, f)) return RecapProfileKind.QuizBrain;
        if (s.SessionsAttended >= Math.Max(2, f / 2)) return RecapProfileKind.Loyal;
        if (s.VoiceMinutes >= 180L * f) return RecapProfileKind.VoiceDweller;
        if (s.GoodBot >= Math.Max(3, 2 * f) && s.GoodBot > s.BadBot) return RecapProfileKind.Fan;
        if (s.Xp >= 2000L * f) return RecapProfileKind.Chatterbox;
        return RecapProfileKind.Balanced;
    }
}
