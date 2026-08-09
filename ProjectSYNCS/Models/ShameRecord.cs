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
// There is deliberately nothing here about *voting*. The daily limit is on the target,
// not the voter — two votes per person per day, from everyone combined — and it is read
// straight off ShameDailyStat.BanVotes, which already counts exactly that. So the rule
// carries no state of its own and cannot drift from the number the wall shows. An
// earlier version rationed the voter instead and needed a LastVoteDay column here;
// restricting the command to staff made rationing them pointless.
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

    // Votes received through `/shame user:@…`, which only staff may cast. Only ever
    // goes up; there is no unvote, and a vote is never withdrawn when the voter changes
    // their mind. Capped at ShameService.MaxVotesPerTargetPerDay a day, enforced
    // against today's bucket rather than against anything stored here.
    public long BanVotes { get; set; }

    // How often this person has turned to *another* bot — replying to one, mentioning
    // one, or running one's slash command. Rationed by a per-channel cooldown, unlike
    // MeanHits: talking to a music bot is mundane and bursty, and uncapped it would
    // just rank whoever queues the most songs.
    public long PerfidyHits { get; set; }
}
