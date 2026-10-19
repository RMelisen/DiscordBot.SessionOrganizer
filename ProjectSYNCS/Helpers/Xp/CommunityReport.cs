using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>What /report community shows. Built by <see cref="CommunityReport.Compute"/>.</summary>
public sealed record CommunityReportData(
    int ActiveMembers,
    IReadOnlyList<long> DailyActive,
    long Xp,
    long VoiceMinutes,
    long Reactions,
    int Joins,
    int Leaves,
    IReadOnlyList<(string Markup, long Count)> TopEmotes,
    int QuizRounds,
    int QuizWon,
    int QuizWinners,
    IReadOnlyDictionary<ulong, long> XpByUser);

/// <summary>
/// The server's life over one window, from daily buckets only (each filtered on <c>Day</c> in SQL by
/// the caller). Pure, so the checks can feed it cases.
/// </summary>
/// <remarks>
/// Active means anything counted that day: XP (messages, reactions, voice), reactions or voice
/// minutes. There is no message count anywhere; XP is the closest thing.
/// </remarks>
public static class CommunityReport
{
    public const int EmoteCount = 5;

    public static CommunityReportData Compute(
        IReadOnlyList<MemberDailyStat> members,
        IReadOnlyList<MemberFlowDailyStat> flow,
        IReadOnlyList<EmoteDailyStat> emotes,
        IReadOnlyDictionary<(ulong EmoteId, string Unicode), string> emoteMarkup,
        IReadOnlyList<QuizRound> rounds,
        ReportWindow window)
    {
        var active = members.Where(m => m.XpEarned > 0 || m.ReactionsUsed > 0 || m.VoiceMinutes > 0).ToList();

        var first = active.Count == 0 ? window.ToDay : active.Min(m => m.Day);
        var perDay = active.GroupBy(m => m.Day).ToDictionary(g => g.Key, g => (long)g.Select(m => m.UserId).Distinct().Count());
        var daily = window.Days(first).Select(d => perDay.GetValueOrDefault(d)).ToList();

        var topEmotes = emotes
            .GroupBy(e => (e.EmoteId, e.Unicode))
            .Select(g => (Markup: emoteMarkup.TryGetValue(g.Key, out var m) ? m : g.Key.Unicode,
                Count: g.Sum(e => e.WrittenCount + e.ReactedCount)))
            .Where(x => x.Count > 0 && x.Markup.Length > 0)
            .OrderByDescending(x => x.Count)
            .Take(EmoteCount)
            .ToList();

        return new CommunityReportData(
            active.Select(m => m.UserId).Distinct().Count(),
            daily,
            members.Sum(m => m.XpEarned),
            members.Sum(m => m.VoiceMinutes),
            members.Sum(m => m.ReactionsUsed),
            flow.Sum(f => f.Joins),
            flow.Sum(f => f.Leaves),
            topEmotes,
            rounds.Count(r => r.MessageId != 0),   // a round whose card never went out is not a question
            rounds.Count(r => r.WinnerId != 0),
            rounds.Where(r => r.WinnerId != 0).Select(r => r.WinnerId).Distinct().Count(),
            members.GroupBy(m => m.UserId).ToDictionary(g => g.Key, g => g.Sum(m => m.XpEarned)));
    }

    /// <summary>The most active people of the window: XP earned, and what they earned the window before.</summary>
    public static IReadOnlyList<(ulong UserId, long Xp, long? Before)> Busiest(CommunityReportData current, CommunityReportData? previous) =>
        current.XpByUser
            .Where(x => x.Value > 0)
            .OrderByDescending(x => x.Value)
            .Take(SessionReport.ListSize)
            .Select(x => (x.Key, x.Value, previous is null ? (long?)null : previous.XpByUser.GetValueOrDefault(x.Key)))
            .ToList();

    /// <summary>
    /// People who were among the busy ones the window before and earned nothing this window: the
    /// ones going quiet. Empty without a previous window.
    /// </summary>
    public static IReadOnlyList<(ulong UserId, long Before)> Fading(CommunityReportData current, CommunityReportData? previous) =>
        previous is null ? Array.Empty<(ulong, long)>()
            : previous.XpByUser
                .Where(x => x.Value > 0 && current.XpByUser.GetValueOrDefault(x.Key) == 0)
                .OrderByDescending(x => x.Value)
                .Take(SessionReport.ListSize)
                .Select(x => (x.Key, x.Value))
                .ToList();
}
