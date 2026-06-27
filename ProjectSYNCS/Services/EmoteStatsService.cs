using ProjectSYNCS.Data;
using ProjectSYNCS.Models;
using Microsoft.EntityFrameworkCore;

namespace ProjectSYNCS.Services;

// A single emote occurrence. Custom: Id != 0, Unicode == "". Unicode emoji:
// Id == 0, Unicode holds the emoji (and Name mirrors it for display).
public readonly record struct EmoteRef(ulong Id, string Name, bool IsAnimated, string Unicode = "")
{
    // Custom emote from a server (has a numeric id).
    public static EmoteRef Custom(ulong id, string name, bool animated) => new(id, name, animated);
    // Standard unicode emoji (no id).
    public static EmoteRef FromUnicode(string emoji) => new(0, emoji, false, emoji);
}

public class EmoteStatsService
{
    private readonly AppDbContext _db_context;

    public EmoteStatsService(AppDbContext db_context)
    {
        _db_context = db_context;
    }

    // Adds written occurrences for a batch of emotes (a message may contain the
    // same emote several times). counts maps each emote to how many times it
    // appeared in the message.
    public async Task AddWrittenAsync(ulong guildId, IReadOnlyDictionary<EmoteRef, int> counts)
    {
        if (counts.Count == 0) return;
        foreach (var (emote, n) in counts)
        {
            var row = await GetOrCreateAsync(guildId, emote);
            row.WrittenCount += n;
        }
        await _db_context.SaveChangesAsync();
    }

    // Adjusts the reacted count for one emote by delta (+1 on add, -1 on remove).
    // The stored count never drops below zero.
    public async Task AddReactedAsync(ulong guildId, EmoteRef emote, int delta)
    {
        var row = await GetOrCreateAsync(guildId, emote);
        row.ReactedCount = Math.Max(0, row.ReactedCount + delta);
        await _db_context.SaveChangesAsync();
    }

    // How many distinct emotes a guild has on record.
    public async Task<int> GetCountAsync(ulong guildId)
    {
        return await _db_context.EmoteStats.CountAsync(s => s.GuildId == guildId);
    }

    // One page of a guild's emotes, ranked by total usage (written + reacted).
    public async Task<List<EmoteStat>> GetPageAsync(ulong guildId, int skip, int take)
    {
        return await _db_context.EmoteStats
            .Where(s => s.GuildId == guildId)
            .OrderByDescending(s => s.WrittenCount + s.ReactedCount)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    private async Task<EmoteStat> GetOrCreateAsync(ulong guildId, EmoteRef emote)
    {
        var row = await _db_context.EmoteStats.FirstOrDefaultAsync(s =>
            s.GuildId == guildId && s.EmoteId == emote.Id && s.Unicode == emote.Unicode);

        if (row is null)
        {
            row = new EmoteStat
            {
                GuildId = guildId,
                EmoteId = emote.Id,
                Unicode = emote.Unicode,
                Name = emote.Name,
                IsAnimated = emote.IsAnimated
            };
            _db_context.EmoteStats.Add(row);
        }
        else
        {
            // Keep the latest name/animated flag in case the emote was renamed.
            row.Name = emote.Name;
            row.IsAnimated = emote.IsAnimated;
        }
        return row;
    }
}
