namespace ProjectSYNCS.Models;

// A collection set someone has completed, and how many times. One row per (guild, user, set),
// enforced by a unique index. The first completion is discovering every item (CompletedAt, paid
// once by InventoryService.AddAsync); every later one is a turn-in that spends one of each
// (InventoryService.TurnInAsync). Count is the totals half of the collector ranking — see
// CollectionDailyStat — and a concurrency token, so two turn-ins can't both read the same count.
public class CollectionCompletion
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }

    // ItemCatalog.Sets key.
    public string SetKey { get; set; } = string.Empty;
    public DateTimeOffset CompletedAt { get; set; }

    // Times completed: 1 on discovery, +1 per turn-in. Its tier is CollectionTiers.TierFor(Count).
    public int Count { get; set; } = 1;
}
