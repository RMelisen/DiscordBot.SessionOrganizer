namespace ProjectSYNCS.Models;

// One person's standing in one guild: the XP they have earned, and the raw activity
// counters the other two leaderboard views rank. One row per (guild, user).
//
// The three numbers are not the same kind of thing, which is why they sit together
// rather than one being derived from another. TotalXp is a *reward* — rationed by
// XpTracker's cooldowns, so it says how consistently someone shows up. ReactionsUsed
// and VoiceMinutes are *facts*: every reaction counts, even the ones too quick after
// the last to earn anything, and every eligible voice minute counts once. Ranking by
// XP and ranking by reactions therefore give genuinely different orders, which is the
// whole point of offering both.
//
// Still no cached Level column: the level is always derivable instantly from TotalXp
// (see Helpers/LevelCurve), unlike EmoteDailyStat/BotFeedbackDailyStat's day, which
// cannot be reconstructed from a running total. And still no daily-bucket sibling —
// none of these three has a rolling-window view to serve.
//
// Both counters started at zero when this shipped: neither was recorded before, and
// neither can be backfilled. The boards are thin at first by necessity.
public class MemberXp
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }

    public long TotalXp { get; set; }

    // Reactions this person has added, anywhere XP can be earned. Decremented when one
    // is removed (clamped at zero), mirroring EmoteTracker — otherwise adding and
    // removing the same reaction in a loop would inflate the ranking without limit.
    public long ReactionsUsed { get; set; }

    // Minutes spent in voice that were *eligible* — someone else present, and not both
    // self-muted and self-deafened. Incremented by the same call that grants voice XP,
    // so the two can never disagree about which minutes counted.
    public long VoiceMinutes { get; set; }
}
