using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// EF access for /minecraft and /report minecraft: account links and her stats — transient. MinecraftBodyService (a
// singleton) opens a scope around it.
public class MinecraftDataService
{
    private readonly AppDbContext _db;

    public MinecraftDataService(AppDbContext db) => _db = db;

    public Task<MinecraftLink?> LinkOfUserAsync(ulong userId) =>
        _db.MinecraftLinks.FirstOrDefaultAsync(l => l.UserId == userId);

    public Task<MinecraftLink?> LinkOfPlayerAsync(string player)
    {
        var key = player.ToLowerInvariant();
        return _db.MinecraftLinks.FirstOrDefaultAsync(l => l.PlayerKey == key);
    }

    /// <summary>Links the member to the player, replacing the member's own previous link. The caller checked the name is free.</summary>
    public async Task SaveLinkAsync(ulong userId, string player)
    {
        var link = await LinkOfUserAsync(userId);
        if (link is null) _db.MinecraftLinks.Add(link = new MinecraftLink { UserId = userId });
        link.Player = player;
        link.PlayerKey = player.ToLowerInvariant();
        link.LinkedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Adds a batch of counts to her stats (McTally.Drain): the totals and <paramref name="day"/>'s bucket
    /// (<see cref="AppTime.DayKey"/>), in one save.
    /// </summary>
    public async Task AddStatsAsync(IReadOnlyDictionary<string, long> counts, int day)
    {
        if (counts.Count == 0) return;
        var keys = counts.Keys.ToList();
        var rows = await _db.MinecraftStats.Where(s => keys.Contains(s.Key)).ToDictionaryAsync(s => s.Key);
        var buckets = await _db.MinecraftDailyStats.Where(s => s.Day == day && keys.Contains(s.Key)).ToDictionaryAsync(s => s.Key);
        foreach (var (key, n) in counts)
        {
            if (!rows.TryGetValue(key, out var row)) _db.MinecraftStats.Add(row = new MinecraftStat { Key = key });
            row.Count += n;
            if (!buckets.TryGetValue(key, out var bucket)) _db.MinecraftDailyStats.Add(bucket = new MinecraftDailyStat { Day = day, Key = key });
            bucket.Count += n;
        }
        await _db.SaveChangesAsync();
    }

    /// <summary>/report minecraft: the window and the one before it, from the daily buckets (Day filtered in SQL).</summary>
    public async Task<Compared<MinecraftReportData>> ReportAsync(ReportWindow window)
    {
        async Task<MinecraftReportData> ComputeAsync(ReportWindow w) => MinecraftReport.Compute(
            await _db.MinecraftDailyStats.AsNoTracking().Where(s => s.Day >= w.FromDay && s.Day <= w.ToDay).ToListAsync());
        var current = await ComputeAsync(window);
        var previous = window.Previous is { } p ? await ComputeAsync(p) : null;
        return new(current, previous);
    }

    public Task<Dictionary<string, long>> StatsAsync() => _db.MinecraftStats.ToDictionaryAsync(s => s.Key, s => s.Count);

    /// <summary>Deletes the member's link; false when they had none.</summary>
    public async Task<bool> RemoveLinkAsync(ulong userId)
    {
        if (await LinkOfUserAsync(userId) is not { } link) return false;
        _db.MinecraftLinks.Remove(link);
        await _db.SaveChangesAsync();
        return true;
    }
}
