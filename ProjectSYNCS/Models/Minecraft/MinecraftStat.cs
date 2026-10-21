namespace ProjectSYNCS.Models;

// One of her all-time Minecraft counts (/minecraft stats), by key (Helpers/Minecraft/McStats: stored keys, append-only).
// Totals only, on purpose: nothing ranks them by date, so there are no daily buckets.
public class MinecraftStat
{
    public string Key { get; set; } = string.Empty;
    public long Count { get; set; }
}
