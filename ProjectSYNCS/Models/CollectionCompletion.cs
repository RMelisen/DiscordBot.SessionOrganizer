namespace ProjectSYNCS.Models;

// A collection set someone has completed — every item in it discovered — and been paid for.
// One row per (guild, user, set), enforced by a unique index: the set's reward is paid once.
public class CollectionCompletion
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }

    // ItemCatalog.Sets key.
    public string SetKey { get; set; } = string.Empty;
    public DateTimeOffset CompletedAt { get; set; }
}
