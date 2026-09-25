namespace ProjectSYNCS.Models;

// What one person holds of one item (Helpers/ItemCatalog), in one guild — kept across all
// their Plynlings. One row per (guild, user, key), enforced by a unique index. A row whose
// quantity fell to 0 stays: it is what makes an item **discovered for good** in the
// collection book, whatever was traded or sold since.
public class InventoryItem
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }

    // ItemCatalog key — stable, never renamed.
    public string Key { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTimeOffset DiscoveredAt { get; set; }
}
