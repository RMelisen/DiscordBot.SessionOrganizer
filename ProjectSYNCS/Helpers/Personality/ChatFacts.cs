using Discord;

namespace ProjectSYNCS.Helpers;

// One thing her records say about someone, quoted in a reply now and then (ChatterService, the
// roast fallback). Each kind has its own pool, BotResponses.ChatFactLines. In memory only, never
// stored: free to reorder.
public enum ChatFact
{
    MissedSessions,
    PerfectRecord,
    BadBotGiven,
    GoodBotGiven,
    TimeWithHer,
    QuizBrain,
    LivingPlynling,
    Graves,
    WallOfShame,
    TopRank,
    Broke,
}

/// <summary>
/// A fact ready to format: <see cref="A"/> is the line's {2} and <see cref="B"/> its {3}, already
/// written as the words the line needs (plural-safe noun phrases, a sanitised name). Empty when a
/// kind has nothing to quote there, so its lines don't use that placeholder.
/// </summary>
public readonly record struct ChatFactPick(ChatFact Kind, string A, string B);

/// <summary>
/// Which of someone's all-time numbers she could throw at them. Every fact that clearly applies is a
/// candidate and one is drawn at random, so the same person doesn't always hear the same thing.
/// Every fact must be true: the lines quote these numbers as they are.
/// </summary>
/// <remarks>
/// The good-bot count is never quoted, only hinted at: her treasure is never shown off
/// (docs/syncs-voice.md). Bad-bot counts are the grudge list, so they are.
/// </remarks>
public static class ChatFacts
{
    public static ChatFactPick? Pick(RecapStats s, Random rng)
    {
        var candidates = new List<ChatFactPick>();

        if (s.SessionsMissed >= 2)
            candidates.Add(new(ChatFact.MissedSessions, Count(s.SessionsMissed, "session", "sessions"), s.SessionsJoined.ToString()));
        if (s.SessionsAttended >= 3 && s.SessionsMissed == 0)
            candidates.Add(new(ChatFact.PerfectRecord, Count(s.SessionsAttended, "session", "sessions"), ""));
        if (s.BadBot >= 1)
            candidates.Add(new(ChatFact.BadBotGiven, $"{s.BadBot} bad bot", ""));
        if (s.GoodBot >= 3 && s.GoodBot > s.BadBot)
            candidates.Add(new(ChatFact.GoodBotGiven, "", ""));
        if (s.MinutesWithHer >= 60)
            candidates.Add(new(ChatFact.TimeWithHer, $"{s.MinutesWithHer / 60} h", ""));
        if (s.QuizWins >= 3)
            candidates.Add(new(ChatFact.QuizBrain, s.QuizWins.ToString(), ""));
        if (!string.IsNullOrWhiteSpace(s.PlynlingName))
            candidates.Add(new(ChatFact.LivingPlynling, Format.Sanitize(s.PlynlingName), ""));
        if (s.Graves >= 1)
            candidates.Add(new(ChatFact.Graves, Count(s.Graves, "Plynling", "Plynlings"), ""));
        if (s.ShamePoints >= 5)
            candidates.Add(new(ChatFact.WallOfShame, s.ShamePoints.ToString(), ""));
        if (s.Rank is { } rank and <= 3)
            candidates.Add(new(ChatFact.TopRank, $"n° {rank}", ""));
        if (s.Balance == 0 && s.Xp > 0)
            candidates.Add(new(ChatFact.Broke, "", ""));

        return candidates.Count == 0 ? null : candidates[rng.Next(candidates.Count)];
    }

    private static string Count(long n, string one, string many) => $"{n} {(n == 1 ? one : many)}";
}
