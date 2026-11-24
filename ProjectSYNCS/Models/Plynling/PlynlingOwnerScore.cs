namespace ProjectSYNCS.Models;

// One person's Plynling score, all their Plynlings together and kept across deaths and abandonment: every
// point a Plynling earns is added to its owner's row in the same save. Shown nowhere yet (the owner's
// call, 2026-11-24), recorded from the start so nothing is lost when something does. One row per
// (guild, user), unique.
public class PlynlingOwnerScore
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public long Points { get; set; }
}
