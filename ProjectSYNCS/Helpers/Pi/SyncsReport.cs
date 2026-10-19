using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>What /report syncs shows. Built by <see cref="SyncsReport.Compute"/>.</summary>
public sealed record SyncsReportData(
    int DaysMeasured,
    int? AverageMilli,
    int? MaxMilli,
    int? MaxDay,
    int HotMinutes,
    IReadOnlyList<long> DailyMax,
    IReadOnlyList<(UptimeEventKind Kind, int Count)> Outages,
    TimeSpan Downtime,
    double? Uptime,
    long Answers,
    long Unrecognised,
    long GoodBot,
    long BadBot,
    long VoiceMinutes,
    IReadOnlyList<PersonTally> Companions);

/// <summary>
/// How she has been over one window: her body (temperature, outages), her chatter, what people said
/// about her, and her time in voice. Pure, so the checks can feed it cases.
/// </summary>
/// <remarks>
/// The Pi's tables are not per guild — there is one Pi. Outages are dated by when they ended, as
/// /health does, and read whole by the caller (their DateTimeOffsets can't be compared in SQL).
/// Uptime is measured over the days the Pi recorded its temperature in the window, so a window
/// reaching back before she existed doesn't count as downtime.
/// </remarks>
public static class SyncsReport
{
    public static SyncsReportData Compute(
        IReadOnlyList<PiThermalDay> thermal,
        IReadOnlyList<UptimeEvent> uptime,
        IReadOnlyList<ChatAnswerDailyStat> answers,
        IReadOnlyList<BotFeedbackDailyStat> feedback,
        IReadOnlyList<VoiceCompanionDailyStat> companions,
        ReportWindow window,
        DateTimeOffset now)
    {
        var days = thermal.Where(t => window.Contains(t.Day)).OrderBy(t => t.Day).ToList();
        var samples = days.Sum(t => (long)t.Samples);
        var hottest = days.Count == 0 ? null : days.MaxBy(t => t.MaxMilli);

        var first = days.Count == 0 ? window.ToDay : days[0].Day;
        var maxByDay = days.ToDictionary(t => t.Day, t => (long)t.MaxMilli);
        var daily = window.Days(first).Select(d => maxByDay.GetValueOrDefault(d)).ToList();

        var events = uptime.Where(e => window.Contains(e.EndedAt)).ToList();
        var downtime = TimeSpan.FromTicks(events.Sum(e => Math.Max(0, (e.EndedAt - e.StartedAt).Ticks)));
        double? uptimeShare = null;
        if (days.Count > 0)
        {
            // The measured span: from the first recorded day's midnight (Paris) to now or the window's end.
            var start = AppTime.AtWallClock(AppTime.FromDayKey(days[0].Day));
            var end = AppTime.AtWallClock(AppTime.FromDayKey(window.ToDay).AddDays(1));
            if (end > now) end = now;
            var span = end - start;
            if (span > TimeSpan.Zero)
                uptimeShare = Math.Clamp(1 - downtime / span, 0, 1);
        }

        var people = companions
            .GroupBy(c => c.UserId)
            .Select(g => new PersonTally(g.Key, (int)g.Sum(c => c.Minutes), 0))
            .OrderByDescending(x => x.Count)
            .Take(SessionReport.ListSize)
            .ToList();

        return new SyncsReportData(
            days.Count,
            samples == 0 ? null : (int)(days.Sum(t => t.SumMilli) / samples),
            hottest?.MaxMilli,
            hottest?.Day,
            days.Sum(t => t.HotMinutes),
            daily,
            events.GroupBy(e => e.Kind).Select(g => (g.Key, g.Count())).OrderBy(x => x.Key).ToList(),
            downtime,
            uptimeShare,
            answers.Sum(a => a.Count),
            answers.Where(a => AnswerKinds.IsUnrecognised(a.Kind)).Sum(a => a.Count),
            feedback.Sum(f => f.GoodCount),
            feedback.Sum(f => f.BadCount),
            // Minutes she shared, not minutes per person: the busiest call counts once per sweep per
            // person, so the sum reads as « minutes of company ».
            companions.Sum(c => c.Minutes),
            people);
    }
}
