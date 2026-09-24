namespace ProjectSYNCS.Models;

// One person's cailloux in one guild ("pebble" in code). One row per (guild, user).
//
// The passive cap needs to know what was earned *today*, and it is kept here as two
// columns rather than in a daily-bucket table: nothing ranks cailloux by date, so the
// totals+buckets pair the leaderboards use would be a table with no reader. PassiveToday
// counts only while PassiveDay is today; a new day simply starts it from zero.
public class PebbleWallet
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }

    public long Balance { get; set; }

    // Null until the first /work; the 4-hour cooldown counts from here.
    public DateTimeOffset? LastWorkAt { get; set; }

    public int PassiveDay { get; set; }
    public long PassiveToday { get; set; }
}
