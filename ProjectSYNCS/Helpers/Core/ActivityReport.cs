using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>A channel of the guild as the report sees it: plain data, so the arithmetic stays pure.</summary>
public sealed record ChannelInfo(ulong Id, bool IsVoice, bool Excluded);

/// <summary>One channel in a list: messages (text) or person-minutes (voice), and the voice peak.</summary>
public sealed record ChannelLine(ulong ChannelId, long Value, int Peak, bool Excluded);

/// <summary>What /report activity shows. Built by <see cref="ActivityReport.Compute"/>.</summary>
public sealed record ActivityReportData(
    long Messages,
    long VoiceMinutes,
    IReadOnlyList<long> MessagesByHour,
    IReadOnlyList<long> VoiceByHour,
    IReadOnlyList<int> BusiestHours,
    DayOfWeek? BusiestWeekday,
    IReadOnlyList<ChannelLine> TopText,
    IReadOnlyList<ChannelLine> SilentText,
    IReadOnlyList<ChannelLine> TopVoice,
    IReadOnlyList<ChannelLine> EmptyVoice,
    IReadOnlyDictionary<ulong, long> MessagesByChannel);

/// <summary>
/// When and where the server talks, over one window, from ActivityHourStat and ChannelDailyStat rows
/// (filtered on Day in SQL by the caller) and a snapshot of the guild's channels. A channel with
/// counts that is no longer in the guild is still ranked; channels the snapshot lists with nothing
/// counted are « silencieux » (text) or « inutilisés » (voice). Pure, so the checks can feed it cases.
/// </summary>
public static class ActivityReport
{
    public const int TopChannels = 8;

    public static ActivityReportData Compute(
        IReadOnlyList<ActivityHourStat> hours, IReadOnlyList<ChannelDailyStat> channels, IReadOnlyList<ChannelInfo> guildChannels)
    {
        var byHour = new long[24];
        var voiceByHour = new long[24];
        foreach (var h in hours)
        {
            byHour[h.Hour] += h.Messages;
            voiceByHour[h.Hour] += h.VoiceMinutes;
        }
        var busiest = Enumerable.Range(0, 24).Where(h => byHour[h] > 0)
            .OrderByDescending(h => byHour[h]).ThenBy(h => h).Take(3).ToList();
        var weekday = hours.Where(h => h.Messages > 0)
            .GroupBy(h => AppTime.FromDayKey(h.Day).DayOfWeek)
            .Select(g => (Day: g.Key, Messages: g.Sum(h => h.Messages)))
            .OrderByDescending(x => x.Messages)
            .Select(x => (DayOfWeek?)x.Day)
            .FirstOrDefault();

        var info = guildChannels.ToDictionary(c => c.Id);
        bool Excluded(ulong id) => info.TryGetValue(id, out var c) && c.Excluded;
        var perChannel = channels.GroupBy(c => c.ChannelId)
            .Select(g => (Id: g.Key, Messages: g.Sum(c => c.Messages), Voice: g.Sum(c => c.VoiceMinutes), Peak: g.Max(c => c.VoicePeak)))
            .ToList();

        var topText = perChannel.Where(c => c.Messages > 0)
            .OrderByDescending(c => c.Messages).Take(TopChannels)
            .Select(c => new ChannelLine(c.Id, c.Messages, 0, Excluded(c.Id))).ToList();
        var written = perChannel.Where(c => c.Messages > 0).Select(c => c.Id).ToHashSet();
        var silent = guildChannels.Where(c => !c.IsVoice && !written.Contains(c.Id))
            .Select(c => new ChannelLine(c.Id, 0, 0, c.Excluded)).ToList();

        var topVoice = perChannel.Where(c => c.Voice > 0)
            .OrderByDescending(c => c.Voice).Take(TopChannels)
            .Select(c => new ChannelLine(c.Id, c.Voice, c.Peak, Excluded(c.Id))).ToList();
        var occupied = perChannel.Where(c => c.Voice > 0).Select(c => c.Id).ToHashSet();
        var emptyVoice = guildChannels.Where(c => c.IsVoice && !occupied.Contains(c.Id))
            .Select(c => new ChannelLine(c.Id, 0, 0, c.Excluded)).ToList();

        return new ActivityReportData(
            byHour.Sum(), voiceByHour.Sum(), byHour, voiceByHour, busiest, weekday,
            topText, silent, topVoice, emptyVoice,
            perChannel.ToDictionary(c => c.Id, c => c.Messages));
    }
}
