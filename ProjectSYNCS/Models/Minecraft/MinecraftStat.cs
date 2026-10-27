namespace ProjectSYNCS.Models;

// One of her all-time Minecraft counts (/minecraft stats), by key (Helpers/Minecraft/McStats: stored keys, append-only).
// The all-time totals; MinecraftDailyStat holds the same counts by day, for /report minecraft.
public class MinecraftStat
{
    public string Key { get; set; } = string.Empty;
    public long Count { get; set; }
}
