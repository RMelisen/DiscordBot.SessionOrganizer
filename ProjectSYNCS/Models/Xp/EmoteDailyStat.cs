namespace ProjectSYNCS.Models;

// One day's usage of one emote in one guild — the same counters as EmoteStat, but
// bucketed by date so a leaderboard can be scoped to a rolling window.
//
// This exists because EmoteStat is a pure running total with no notion of *when*
// anything happened, and adding a date to it retroactively is impossible: the
// history it holds has no dates to recover. So the two live side by side and answer
// different questions — EmoteStat stays the exact all-time figure including
// everything counted before buckets existed, and these rows cover week and month.
// A consequence worth knowing: summing every bucket does **not** reproduce
// EmoteStat, and is not meant to.
//
// Identity mirrors EmoteStat: custom emote -> EmoteId != 0, Unicode == ""; unicode
// emoji -> EmoteId == 0, Unicode holds the character. Name and IsAnimated are
// deliberately *not* duplicated here — every bucketed emote necessarily has an
// EmoteStat row (they are written together), so display markup is resolved from
// there and a rename stays in one place.
public class EmoteDailyStat
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong EmoteId { get; set; }

    // The raw unicode emoji, empty for custom emotes.
    public string Unicode { get; set; } = string.Empty;

    // The calendar day in Europe/Paris, encoded yyyymmdd by AppTime.DayKey. An int
    // so SQLite can compare a range without the query first being pulled into memory.
    public int Day { get; set; }

    public long WrittenCount { get; set; }
    public long ReactedCount { get; set; }
}
