namespace ProjectSYNCS.Models;

// One of her Minecraft counts for one Paris day (/report minecraft, Papa's digest): the daily bucket beside the
// all-time MinecraftStat, written in the same save. Same keys (Helpers/Minecraft/McStats: stored keys, append-only).
// The buckets start in 7.50.0 and don't sum to the totals.
public class MinecraftDailyStat
{
    public int Id { get; set; }
    /// <summary>AppTime.DayKey: yyyymmdd, Paris.</summary>
    public int Day { get; set; }
    public string Key { get; set; } = string.Empty;
    public long Count { get; set; }
}
