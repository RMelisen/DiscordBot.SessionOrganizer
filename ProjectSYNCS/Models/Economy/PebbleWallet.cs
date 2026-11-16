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

    // When their Plynling last went foraging (/plynling forage, every 4 h per person). Stored, so
    // a restart cannot hand out an extra find.
    public DateTimeOffset? LastForageAt { get; set; }

    // The /work DM reminder, switched from /balance. WorkRemindedAt is when the last DM went out (or
    // when it was switched on with /work already available): a shift whose LastWorkAt is later
    // hasn't had its DM yet (PebbleEconomy.WorkReminderDue).
    public bool WorkReminder { get; set; }
    public DateTimeOffset? WorkRemindedAt { get; set; }

    // /daily (Helpers/Economy/DailyStreak). On the wallet so the claim, its cailloux and the streak
    // save together, and so Balance's concurrency token makes a double claim lose the race and be
    // refused on retry. DailyDay is the Paris yyyymmdd of the last claim, 0 if never. DailyBest is
    // the longest streak ever: the « Assiduité » badge reads it, so a reset never takes a tier away.
    public int DailyDay { get; set; }
    public int DailyStreak { get; set; }
    public int DailyBest { get; set; }
}
