using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// Persists giveaways and their entries. Deliberately dumb, like PollService: it
// records what it is told and reads it back. *When* a giveaway is due belongs to
// GiveawayDrawService, which watches the clock.
//
// The one exception is TryDrawAsync, which has to live here: picking the winners and
// closing the giveaway must be a single decision against the stored state, or a
// restart mid-sweep would draw twice.
//
// Transient, like every other service wrapping AppDbContext.
public class GiveawayService
{
    private readonly AppDbContext _db_context;

    public GiveawayService(AppDbContext db_context)
    {
        _db_context = db_context;
    }

    public async Task<Giveaway> CreateAsync(
        ulong guildId, ulong channelId, ulong organizerId,
        string prize, string description, int winnerCount, DateTimeOffset endsAt)
    {
        var giveaway = new Giveaway
        {
            GuildId = guildId,
            ChannelId = channelId,
            OrganizerId = organizerId,
            Prize = prize,
            Description = description,
            WinnerCount = winnerCount,
            EndsAt = endsAt
        };

        _db_context.Giveaways.Add(giveaway);
        await _db_context.SaveChangesAsync();
        return giveaway;
    }

    public Task<Giveaway?> GetAsync(int giveawayId) =>
        _db_context.Giveaways
            .Include(g => g.Entries)
            .FirstOrDefaultAsync(g => g.Id == giveawayId);

    public async Task SetMessageLocationAsync(int giveawayId, ulong channelId, ulong messageId)
    {
        var giveaway = await _db_context.Giveaways.FindAsync(giveawayId);
        if (giveaway is null) return;

        giveaway.ChannelId = channelId;
        giveaway.MessageId = messageId;
        await _db_context.SaveChangesAsync();
    }

    /// <summary>
    /// The guild's running giveaways, soonest to end first.
    /// </summary>
    public async Task<List<Giveaway>> GetActiveAsync(ulong guildId)
    {
        var open = await _db_context.Giveaways
            .Include(g => g.Entries)
            .Where(g => g.GuildId == guildId && !g.IsClosed)
            .ToListAsync();

        // SQLite can't order by DateTimeOffset; sort in memory.
        return open.OrderBy(g => g.EndsAt).ToList();
    }

    /// <summary>
    /// Enters this person. Returns false if they had already entered, so the caller can
    /// say so rather than silently doing nothing.
    /// </summary>
    /// <remarks>
    /// Add and remove are two methods rather than one toggle because the card now has
    /// two buttons. A toggle behind an explicit "Participer" would mean clicking it
    /// twice withdraws you — which is exactly the confusion the second button exists to
    /// remove.
    /// </remarks>
    public async Task<bool> AddEntryAsync(int giveawayId, ulong userId)
    {
        bool already = await _db_context.GiveawayEntries
            .AnyAsync(e => e.GiveawayId == giveawayId && e.UserId == userId);
        if (already) return false;

        _db_context.GiveawayEntries.Add(new GiveawayEntry
        {
            GiveawayId = giveawayId,
            UserId = userId
        });
        await _db_context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Withdraws this person. Returns false if they were not entered in the first place.
    /// </summary>
    public async Task<bool> RemoveEntryAsync(int giveawayId, ulong userId)
    {
        var existing = await _db_context.GiveawayEntries
            .FirstOrDefaultAsync(e => e.GiveawayId == giveawayId && e.UserId == userId);
        if (existing is null) return false;

        _db_context.GiveawayEntries.Remove(existing);
        await _db_context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Giveaways whose time is up and which have not been drawn yet.
    /// </summary>
    /// <remarks>
    /// The <c>EndsAt</c> cutoff is applied in memory: SQLite cannot translate a
    /// <see cref="DateTimeOffset"/> comparison, and sending one would throw at
    /// runtime. Only open rows are fetched, so the list stays small.
    /// </remarks>
    public async Task<List<Giveaway>> GetDueAsync()
    {
        var open = await _db_context.Giveaways
            .Include(g => g.Entries)
            .Where(g => !g.IsClosed)
            .ToListAsync();

        var now = DateTimeOffset.UtcNow;
        return open.Where(g => g.EndsAt <= now).ToList();
    }

    /// <summary>
    /// Closes the giveaway and marks the drawn entries, returning the winners' ids —
    /// empty when nobody entered. Returns <c>null</c> if it was already drawn.
    /// </summary>
    /// <remarks>
    /// Deciding and recording in one call is what makes the draw safe to retry. The
    /// sweep can die between drawing and announcing, or be restarted mid-pass, and the
    /// worst case is an announcement that never goes out — never a second draw, and
    /// never a different set of winners than the card shows.
    /// </remarks>
    public async Task<List<ulong>?> TryDrawAsync(int giveawayId)
    {
        var giveaway = await _db_context.Giveaways
            .Include(g => g.Entries)
            .FirstOrDefaultAsync(g => g.Id == giveawayId);

        if (giveaway is null || giveaway.IsClosed) return null;

        var drawn = Draw(giveaway.Entries, giveaway.WinnerCount);
        foreach (var entry in drawn) entry.IsWinner = true;

        giveaway.IsClosed = true;
        await _db_context.SaveChangesAsync();

        return drawn.Select(e => e.UserId).ToList();
    }

    /// <summary>
    /// Picks up to <paramref name="count"/> distinct entries at random.
    /// </summary>
    /// <remarks>
    /// A partial Fisher-Yates over a copy: every entrant is equally likely and nobody
    /// can be drawn twice. Not <c>OrderBy(_ =&gt; Random)</c>, whose comparer is called
    /// an unspecified number of times with a fresh key each time.
    /// </remarks>
    private static List<GiveawayEntry> Draw(List<GiveawayEntry> entries, int count)
    {
        var pool = entries.ToList();
        var take = Math.Min(Math.Max(0, count), pool.Count);

        for (int i = 0; i < take; i++)
        {
            var j = Random.Shared.Next(i, pool.Count);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        return pool.Take(take).ToList();
    }

    /// <summary>Removes the giveaway and, by cascade, its entries.</summary>
    public async Task DeleteAsync(int giveawayId)
    {
        var giveaway = await _db_context.Giveaways.FindAsync(giveawayId);
        if (giveaway is null) return;

        _db_context.Giveaways.Remove(giveaway);
        await _db_context.SaveChangesAsync();
    }
}
