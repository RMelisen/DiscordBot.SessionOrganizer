namespace ProjectSYNCS.Models;

// One person's cumulative XP in one guild — talking, reacting, interacting with the
// bot directly. One row per (guild, user); TotalXp only ever goes up.
//
// No cached Level column, and no daily-bucket sibling table. The level is always
// derivable instantly from TotalXp (see Helpers/LevelCurve), unlike EmoteDailyStat/
// BotFeedbackDailyStat's day, which cannot be reconstructed from a running total —
// and /level is a single ever-growing ranking with no rolling-window view to serve,
// so there is nothing here that would need a bucket.
public class MemberXp
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }

    public long TotalXp { get; set; }
}
