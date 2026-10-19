namespace ProjectSYNCS.Models;

// How long she has sat in a voice call with each person (VoiceSeatService): one minute per sweep
// that finds her confirmed in a call, credited to every human connected there, muted or not —
// they were in the call with her. Read by /recap, /report syncs and the monthly digest.
//
// The totals + daily buckets pattern (root CLAUDE.md, Data): this is the all-time figure,
// VoiceCompanionDailyStat the per-day rows a month is summed from. Both are written in the same
// call. Summing the buckets reproduces the totals only because both started together.
public class VoiceCompanionStat
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }

    public long Minutes { get; set; }
}

public class VoiceCompanionDailyStat
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }

    // The calendar day in Europe/Paris, yyyymmdd (AppTime.DayKey): a month filters in SQL.
    public int Day { get; set; }

    public long Minutes { get; set; }
}
