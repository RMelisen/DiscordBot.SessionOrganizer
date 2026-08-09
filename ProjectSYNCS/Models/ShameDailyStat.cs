namespace ProjectSYNCS.Models;

// One day's shame for one person in one guild — the same three counters as ShameRecord,
// bucketed by date so `/shame` can be scoped to a rolling window.
//
// LastVoteDay has no bucket here: it is not a counter, it is a "when did you last do
// this" flag, and the day it names is already the only day it is ever compared to.
//
// As everywhere else in this project, the buckets do not sum to the totals — they only
// cover data recorded since they shipped, and all-time stays the exact figure.
public class ShameDailyStat
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }

    // The calendar day in Europe/Paris, encoded yyyymmdd by AppTime.DayKey. An int so
    // SQLite can compare a range without the query first being pulled into memory.
    public int Day { get; set; }

    public long MeanHits { get; set; }
    public long BanVotes { get; set; }
    public long PerfidyHits { get; set; }
}
