namespace ProjectSYNCS.Models;

// How often one person has told the bot "good bot" / "bad bot", scoped to the
// guild it was said in. One row per (guild, user); both counters only ever go up.
//
// Counting is rationed in BotFeedbackTracker rather than here: one verdict per
// person per thing the bot actually did, so the tally reflects reactions to her
// behaviour rather than how many times someone held down Enter.
public class BotFeedback
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }

    public long GoodCount { get; set; }
    public long BadCount { get; set; }
}
