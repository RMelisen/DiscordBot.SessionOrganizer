using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

public enum RevokeWarningOutcome { Revoked, NotFound, AlreadyRevoked }

// /admin warn, warnings and unwarn. Rows are never deleted: a revoked or expired warning stays
// in the history and only stops counting (WarningRules.IsActive).
public class WarningService
{
    private readonly AppDbContext _db_context;

    public WarningService(AppDbContext db_context)
    {
        _db_context = db_context;
    }

    /// <summary>Records a warning and returns it, with how many are now active, this one included.</summary>
    public async Task<(MemberWarning Warning, int ActiveCount)> AddAsync(
        ulong guildId, ulong userId, ulong moderatorId, string reason, DateTimeOffset now)
    {
        var warning = new MemberWarning
        {
            GuildId = guildId,
            UserId = userId,
            ModeratorId = moderatorId,
            Reason = reason,
            CreatedAt = now,
        };
        _db_context.MemberWarnings.Add(warning);
        await _db_context.SaveChangesAsync();

        var history = await HistoryAsync(guildId, userId);
        return (warning, history.Count(w => WarningRules.IsActive(w, now)));
    }

    /// <summary>One person's warnings in this guild, newest first.</summary>
    public async Task<List<MemberWarning>> HistoryAsync(ulong guildId, ulong userId)
    {
        // Filtered by ids in SQL, ordered in memory: SQLite can't translate DateTimeOffset.
        var rows = await _db_context.MemberWarnings
            .AsNoTracking()
            .Where(w => w.GuildId == guildId && w.UserId == userId)
            .ToListAsync();
        return rows.OrderByDescending(w => w.CreatedAt).ThenByDescending(w => w.Id).ToList();
    }

    /// <summary>
    /// Revokes warning <paramref name="id"/>, if it belongs to this guild. An expired warning can
    /// still be revoked: it then reads as revoked in the history, which is what staff meant.
    /// </summary>
    public async Task<(RevokeWarningOutcome Outcome, MemberWarning? Warning)> RevokeAsync(
        ulong guildId, int id, ulong revokedById, DateTimeOffset now)
    {
        var warning = await _db_context.MemberWarnings.FirstOrDefaultAsync(w => w.Id == id && w.GuildId == guildId);
        if (warning is null) return (RevokeWarningOutcome.NotFound, null);
        if (warning.RevokedAt is not null) return (RevokeWarningOutcome.AlreadyRevoked, warning);

        warning.RevokedAt = now;
        warning.RevokedById = revokedById;
        await _db_context.SaveChangesAsync();
        return (RevokeWarningOutcome.Revoked, warning);
    }
}
