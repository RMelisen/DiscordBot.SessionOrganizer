namespace ProjectSYNCS.Models;

// One day's shame for one person in one guild — the same three counters as ShameRecord,
// bucketed by date so `/shame` can be scoped to a rolling window.
//
// The vote limit reads BanVotes on this row directly — two per target per day — so this
// bucket is not merely a record of what happened, it is what enforces the rule. That is
// why the limit needs no state of its own and cannot drift from the number the wall
// displays. An earlier design rationed the *voter* instead and needed a LastVoteDay
// column on ShameRecord; restricting the command to staff made that pointless.
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

    // Same counter as ShameRecord.ShoutHits, bucketed by day.
    public long ShoutHits { get; set; }
}
