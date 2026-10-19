namespace ProjectSYNCS.Helpers;

/// <summary>A member of the guild as the report sees it: plain data.</summary>
public sealed record MemberSnapshot(ulong UserId, DateTimeOffset? JoinedAt, bool IsBot);

/// <summary>A member with no sign of life: the last active day (yyyymmdd), or null for never.</summary>
public sealed record Sleeper(ulong UserId, int? LastDay);

public sealed record SleepingReportData(int Days, int Members, IReadOnlyList<Sleeper> Sleepers);

/// <summary>
/// Members silent for N days: no XP, reaction or voice minute (MemberDailyStat) since then. Only
/// people on the server for longer than N days are listed (an unknown join date counts as old);
/// bots never are. XP buckets are reliable from <see cref="ReliableSince"/> (the ResetXpTotals
/// migration), so « never » means never since then. Activity in excluded channels earns nothing and
/// so is invisible here. Pure.
/// </summary>
public static class SleepingReport
{
    public static readonly IReadOnlyList<int> Choices = new[] { 30, 60, 90 };
    public const int ReliableSince = 20260809;

    public static SleepingReportData Compute(
        IReadOnlyList<MemberSnapshot> members, IReadOnlyDictionary<ulong, int> lastActiveDay, int days, DateTimeOffset now)
    {
        var since = now.AddDays(-days);
        var threshold = AppTime.DayKey(since);
        var people = members.Where(m => !m.IsBot).ToList();
        var sleepers = people
            .Where(m => m.JoinedAt is null || m.JoinedAt <= since)
            .Select(m => new Sleeper(m.UserId, lastActiveDay.TryGetValue(m.UserId, out var day) ? day : null))
            .Where(s => s.LastDay is null || s.LastDay < threshold)
            .OrderBy(s => s.LastDay ?? 0).ThenBy(s => s.UserId)
            .ToList();
        return new SleepingReportData(days, people.Count, sleepers);
    }
}
