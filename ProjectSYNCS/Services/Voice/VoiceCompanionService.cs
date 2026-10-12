using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// Writes VoiceCompanionStat / VoiceCompanionDailyStat: the minutes she sat in a call with each
// person. Transient, like every service wrapping the context; VoiceSeatService scopes it per sweep.
public class VoiceCompanionService
{
    private readonly AppDbContext _db;

    public VoiceCompanionService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Credits <paramref name="minutes"/> to each person, totals and today's bucket in one save.</summary>
    public async Task AddAsync(ulong guildId, IReadOnlyCollection<ulong> userIds, long minutes)
    {
        if (userIds.Count == 0) return;
        var day = AppTime.TodayKey;
        var ids = userIds.ToList();

        var totals = await _db.VoiceCompanionStats
            .Where(x => x.GuildId == guildId && ids.Contains(x.UserId))
            .ToDictionaryAsync(x => x.UserId);
        var buckets = await _db.VoiceCompanionDailyStats
            .Where(x => x.GuildId == guildId && x.Day == day && ids.Contains(x.UserId))
            .ToDictionaryAsync(x => x.UserId);

        foreach (var id in ids)
        {
            if (!totals.TryGetValue(id, out var total))
                _db.VoiceCompanionStats.Add(total = new VoiceCompanionStat { GuildId = guildId, UserId = id });
            total.Minutes += minutes;

            if (!buckets.TryGetValue(id, out var bucket))
                _db.VoiceCompanionDailyStats.Add(bucket = new VoiceCompanionDailyStat { GuildId = guildId, UserId = id, Day = day });
            bucket.Minutes += minutes;
        }

        await _db.SaveChangesAsync();
    }
}
