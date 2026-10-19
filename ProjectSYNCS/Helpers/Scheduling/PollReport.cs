using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>What /report polls shows. Built by <see cref="PollReport.Compute"/>.</summary>
public sealed record PollReportData(
    int DatePolls,
    int TextPolls,
    int Open,
    int Dead,
    double? AverageVoters,
    int WeekendPosted,
    int WeekendWon,
    int WeekendBecameSession,
    int TonightCalls,
    int TonightReached,
    int TonightBecameSession,
    double? AverageTonightIn,
    IReadOnlyList<PersonTally> Voters,
    IReadOnlyList<PersonTally> Keen);

/// <summary>
/// Polls, her Wednesday poll and /tonight over one window. Pure, so the checks can feed it cases.
/// </summary>
/// <remarks>
/// <para>A poll or a call belongs to the window it was created in. A deleted poll is gone from the
/// database (PollService hard-deletes), so it is simply not counted; an un-vote deletes the vote,
/// so voters are the ones who stayed.</para>
/// <para>Nothing links a poll to the session it led to: a date poll « became a session » when a
/// session of the guild, not cancelled, sits on one of its slots, the same match PollModule uses
/// to refuse a duplicate.</para>
/// </remarks>
public static class PollReport
{
    public static PollReportData Compute(
        IEnumerable<Poll> polls, IEnumerable<TonightCall> calls, IEnumerable<SessionEvent> sessions,
        ReportWindow window, ulong botId)
    {
        var created = polls.Where(p => window.Contains(p.CreatedAt)).ToList();
        var voters = created.ToDictionary(p => p.Id,
            p => p.Options.SelectMany(o => o.Votes).Select(v => v.UserId).Distinct().ToList());

        var slots = sessions.Where(s => !s.IsCancelled).Select(s => s.ScheduledAt).ToHashSet();
        bool BecameSession(Poll p) => p.Kind == PollKind.DateSlots && p.Options.Any(o => slots.Contains(o.ScheduledAt));

        var weekend = created.Where(p => p.OrganizerId == botId && p.Kind == PollKind.DateSlots).ToList();

        var tonight = calls.Where(c => window.Contains(c.CreatedAt)).ToList();

        var topVoters = created
            .SelectMany(p => voters[p.Id])
            .GroupBy(id => id)
            .Select(g => new PersonTally(g.Key, g.Count(), created.Count))
            .OrderByDescending(x => x.Count)
            .Take(SessionReport.ListSize)
            .ToList();
        var keen = tonight
            .SelectMany(c => c.Responses.Where(r => r.Status == TonightStatus.In && r.UserId != c.OrganizerId))
            .GroupBy(r => r.UserId)
            .Select(g => new PersonTally(g.Key, g.Count(), tonight.Count))
            .OrderByDescending(x => x.Count)
            .Take(SessionReport.ListSize)
            .ToList();

        return new PollReportData(
            created.Count(p => p.Kind == PollKind.DateSlots),
            created.Count(p => p.Kind == PollKind.Text),
            created.Count(p => !p.IsClosed),
            created.Count(p => p.IsClosed && voters[p.Id].Count == 0),
            created.Count == 0 ? null : created.Average(p => voters[p.Id].Count),
            weekend.Count,
            weekend.Count(p => p.IsClosed && voters[p.Id].Count > 0),
            weekend.Count(BecameSession),
            tonight.Count,
            tonight.Count(c => c.ThresholdAnnounced),
            tonight.Count(c => c.SessionEventId != 0),
            tonight.Count == 0 ? null : tonight.Average(c => c.Responses.Count(r => r.Status == TonightStatus.In)),
            topVoters,
            keen);
    }
}
