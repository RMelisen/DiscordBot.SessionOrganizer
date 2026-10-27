using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// /confess's memory: who wrote which number, and how soon someone may confess again.
//
// Singleton for the cooldown and the numbering lock, so it scopes AppDbContext per call like every
// stateful singleton here. A number is reserved (row saved) before the post goes out, so two
// confessions at once can't both be #12; a post that fails takes its row back, leaving a gap
// rather than a number pointing at nothing.
public sealed class ConfessionService
{
    // Long enough that nobody floods the channel, short enough that a second thought still fits
    // in an evening. In memory: a restart forgets it, which costs nothing.
    private readonly CooldownGate<ulong> _cooldown = new(TimeSpan.FromMinutes(10), TimeSpan.FromHours(1));

    private readonly SemaphoreSlim _numbering = new(1, 1);
    private readonly IServiceProvider _services;

    public ConfessionService(IServiceProvider services)
    {
        _services = services;
    }

    /// <summary>Claims the author's cooldown; false (with when it ends) while it still runs.</summary>
    public bool TryClaim(ulong userId, out DateTimeOffset readyAt) => _cooldown.TryClaim(userId, out readyAt);

    /// <summary>Gives the cooldown back after a confession that never went out.</summary>
    public void Release(ulong userId) => _cooldown.Release(userId);

    /// <summary>Saves the confession's row and returns its number.</summary>
    public async Task<int> ReserveAsync(ulong guildId, ulong authorId)
    {
        await _numbering.WaitAsync();
        try
        {
            await using var scope = _services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var last = await db.Confessions
                .Where(c => c.GuildId == guildId)
                .MaxAsync(c => (int?)c.Number) ?? 0;

            var confession = new AnonymousConfession
            {
                GuildId = guildId,
                Number = last + 1,
                AuthorId = authorId,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            db.Confessions.Add(confession);
            await db.SaveChangesAsync();
            return confession.Number;
        }
        finally
        {
            _numbering.Release();
        }
    }

    /// <summary>Records where the confession was posted.</summary>
    public async Task AttachAsync(ulong guildId, int number, ulong channelId, ulong messageId)
    {
        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var confession = await db.Confessions.FirstOrDefaultAsync(c => c.GuildId == guildId && c.Number == number);
        if (confession is null) return;
        confession.ChannelId = channelId;
        confession.MessageId = messageId;
        await db.SaveChangesAsync();
    }

    /// <summary>Drops a reserved number whose post never went out.</summary>
    public async Task RemoveAsync(ulong guildId, int number)
    {
        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Confessions.Where(c => c.GuildId == guildId && c.Number == number).ExecuteDeleteAsync();
    }

    /// <summary>The confession with that number in that guild, or null.</summary>
    public async Task<AnonymousConfession?> FindAsync(ulong guildId, int number)
    {
        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Confessions.AsNoTracking()
            .FirstOrDefaultAsync(c => c.GuildId == guildId && c.Number == number);
    }
}
