namespace ProjectSYNCS.Models;

// One person's standing on the wall of shame, in one guild. One row per (guild, user).
//
// The row holds every side of the wall, which is why the counters read oddly next to
// each other: MeanHits and PerfidyHits are things you *did* (messages of yours that
// read hostile; times you turned to a rival bot), BanVotes is something that was *done
// to you* (other people spending their daily vote on you). Splitting them into separate
// tables would mean three lookups and three upserts to answer one command, for no gain
// — nobody ever reads one without the others.
//
// LastVoteDay is a third kind again: it belongs to this person as a *voter*, not as a
// target. It is what enforces one vote per person per day, and it lives in the database
// rather than in ShameTracker's memory on purpose — the personality state resets on
// restart harmlessly, but a limit that vanishes on restart is an exploit, not a quirk.
//
// Totals here, per-day buckets in ShameDailyStat — the fourth instance of the pattern
// EmoteStat, BotFeedback and MemberXp already established, for the same reason: a date
// cannot be recovered from a running total. **Summing every bucket does not reproduce
// this row, and must not be made to.**
public class ShameRecord
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }

    // How many people this person has been hostile to, summed over messages: one hit
    // per human targeted, so a mean message tagging three people counts three times.
    // Uncapped by design — see ShameTracker.
    public long MeanHits { get; set; }

    // Votes received through `/shame user:@…`. Only ever goes up; there is no unvote,
    // and a vote is never withdrawn when the voter changes their mind.
    public long BanVotes { get; set; }

    // How often this person has turned to *another* bot — replying to one, mentioning
    // one, or running one's slash command. Rationed by a per-channel cooldown, unlike
    // MeanHits: talking to a music bot is mundane and bursty, and uncapped it would
    // just rank whoever queues the most songs.
    public long PerfidyHits { get; set; }

    // The day (AppTime.DayKey, yyyymmdd) this person last *cast* a vote. Zero means
    // never. Compared against today rather than being reset by anything, so no sweep
    // has to run at midnight.
    public int LastVoteDay { get; set; }
}
