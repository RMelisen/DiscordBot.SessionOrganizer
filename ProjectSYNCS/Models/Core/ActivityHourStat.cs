namespace ProjectSYNCS.Models;

// Messages and voice person-minutes for one guild, one Paris day and one hour (0–23): the peak hours
// of /report activity. Written by ActivityFlushService from ActivityCounter. Buckets only, no
// totals: nothing needs an all-time figure older than the buckets (root CLAUDE.md, the
// MemberFlowDailyStat precedent). Counted from 7.6.0 on. One row per (guild, day, hour), unique.
public class ActivityHourStat
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public int Day { get; set; }
    public int Hour { get; set; }
    public long Messages { get; set; }
    // Minutes × people present (muted included, AFK channel excluded): presence, not XP-eligible time.
    public long VoiceMinutes { get; set; }
}
