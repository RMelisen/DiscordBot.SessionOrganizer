using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// EF access for /tonight — transient, like every service wrapping AppDbContext. Date windows are
// applied in memory: SQLite cannot compare DateTimeOffset.
public class TonightService
{
    private readonly AppDbContext _db_context;

    public TonightService(AppDbContext db_context)
    {
        _db_context = db_context;
    }

    public async Task<TonightCall> CreateAsync(ulong guildId, ulong channelId, ulong organizerId,
        string game, DateTimeOffset startsAt, int minimum, DateTimeOffset expiresAt)
    {
        var call = new TonightCall
        {
            GuildId = guildId,
            ChannelId = channelId,
            OrganizerId = organizerId,
            Game = game,
            StartsAt = startsAt,
            Minimum = minimum,
            ExpiresAt = expiresAt,
        };
        // The organizer is in by definition: they asked.
        call.Responses.Add(new TonightResponse { UserId = organizerId, Status = TonightStatus.In });
        _db_context.TonightCalls.Add(call);
        await _db_context.SaveChangesAsync();
        return call;
    }

    public async Task<TonightCall?> GetAsync(int id) =>
        await _db_context.TonightCalls.Include(c => c.Responses).FirstOrDefaultAsync(c => c.Id == id);

    // The guild's open call for this evening, if any — one at a time.
    public async Task<TonightCall?> GetOpenAsync(ulong guildId)
    {
        var now = DateTimeOffset.UtcNow;
        var open = await _db_context.TonightCalls.AsNoTracking()
            .Where(c => c.GuildId == guildId && !c.Closed)
            .ToListAsync();
        return open.Where(c => c.ExpiresAt > now).OrderBy(c => c.CreatedAt).FirstOrDefault();
    }

    public async Task SetMessageAsync(int id, ulong messageId)
    {
        var call = await _db_context.TonightCalls.FindAsync(id);
        if (call is null) return;
        call.MessageId = messageId;
        await _db_context.SaveChangesAsync();
    }

    // Where the card lives after a repost (CardRepost).
    public async Task SetMessageLocationAsync(int id, ulong channelId, ulong messageId)
    {
        var call = await _db_context.TonightCalls.FindAsync(id);
        if (call is null) return;
        call.ChannelId = channelId;
        call.MessageId = messageId;
        await _db_context.SaveChangesAsync();
    }

    // Add or switch an answer, never a toggle. Returns whether anything changed.
    public async Task<bool> AnswerAsync(int id, ulong userId, TonightStatus status)
    {
        var existing = await _db_context.TonightResponses
            .FirstOrDefaultAsync(r => r.TonightCallId == id && r.UserId == userId);
        if (existing is not null && existing.Status == status) return false;
        if (existing is null)
            _db_context.TonightResponses.Add(new TonightResponse { TonightCallId = id, UserId = userId, Status = status });
        else
        {
            existing.Status = status;
            existing.AnsweredAt = DateTimeOffset.UtcNow;
        }
        await _db_context.SaveChangesAsync();
        return true;
    }

    // The once-only claim on the threshold ping: true for the one caller who flipped the flag.
    public async Task<bool> ClaimThresholdAsync(int id)
    {
        var rows = await _db_context.TonightCalls
            .Where(c => c.Id == id && !c.ThresholdAnnounced)
            .ExecuteUpdateAsync(u => u.SetProperty(c => c.ThresholdAnnounced, true));
        return rows == 1;
    }

    // The once-only claim on closing: true for the one caller who closed it (a double click on
    // « Créer la session », or the midnight sweep racing the button, gets false).
    public async Task<bool> ClaimCloseAsync(int id)
    {
        var rows = await _db_context.TonightCalls
            .Where(c => c.Id == id && !c.Closed)
            .ExecuteUpdateAsync(u => u.SetProperty(c => c.Closed, true));
        return rows == 1;
    }

    // Gives a close back when the session it was claimed for couldn't be created.
    public async Task ReopenAsync(int id) =>
        await _db_context.TonightCalls
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(u => u.SetProperty(c => c.Closed, false));

    public async Task SetSessionAsync(int id, int sessionEventId) =>
        await _db_context.TonightCalls
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(u => u.SetProperty(c => c.SessionEventId, sessionEventId));

    // Open calls whose evening is over, for ReminderService.
    public async Task<List<TonightCall>> GetExpiredOpenAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var open = await _db_context.TonightCalls.AsNoTracking()
            .Include(c => c.Responses)
            .Where(c => !c.Closed)
            .ToListAsync();
        return open.Where(c => c.ExpiresAt <= now).ToList();
    }
}
