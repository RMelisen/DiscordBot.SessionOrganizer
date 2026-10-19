using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>
/// Reads the daily buckets behind /report community; the arithmetic is in
/// <see cref="CommunityReport"/>. Every table here has an int <c>Day</c>, so windows filter in SQL.
/// </summary>
/// <remarks>Transient, like every service wrapping the context.</remarks>
public class CommunityReportService
{
    private readonly AppDbContext _db;

    public CommunityReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Compared<CommunityReportData>> GetAsync(ulong guildId, ReportWindow window)
    {
        // Markup lives on EmoteStat, so a renamed emote shows under its current name.
        var markup = await _db.EmoteStats.AsNoTracking()
            .Where(s => s.GuildId == guildId)
            .ToDictionaryAsync(s => (s.EmoteId, s.Unicode), s => s.Markup);
        var current = await ComputeAsync(guildId, window, markup);
        var previous = window.Previous is { } p ? await ComputeAsync(guildId, p, markup) : null;
        return new(current, previous);
    }

    private async Task<CommunityReportData> ComputeAsync(
        ulong guildId, ReportWindow w, IReadOnlyDictionary<(ulong, string), string> markup)
    {
        var members = await _db.MemberDailyStats.AsNoTracking()
            .Where(x => x.GuildId == guildId && x.Day >= w.FromDay && x.Day <= w.ToDay).ToListAsync();
        var flow = await _db.MemberFlowDailyStats.AsNoTracking()
            .Where(x => x.GuildId == guildId && x.Day >= w.FromDay && x.Day <= w.ToDay).ToListAsync();
        var emotes = await _db.EmoteDailyStats.AsNoTracking()
            .Where(x => x.GuildId == guildId && x.Day >= w.FromDay && x.Day <= w.ToDay).ToListAsync();
        var rounds = await _db.QuizRounds.AsNoTracking()
            .Where(x => x.GuildId == guildId && x.Day >= w.FromDay && x.Day <= w.ToDay).ToListAsync();
        return CommunityReport.Compute(members, flow, emotes, markup, rounds, w);
    }

    /// <summary>Each member's last day with any XP, reaction or voice minute (yyyymmdd), for /report sleeping.</summary>
    public async Task<IReadOnlyDictionary<ulong, int>> LastActiveDaysAsync(ulong guildId) =>
        await _db.MemberDailyStats.AsNoTracking()
            .Where(x => x.GuildId == guildId && (x.XpEarned > 0 || x.ReactionsUsed > 0 || x.VoiceMinutes > 0))
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Last = g.Max(x => x.Day) })
            .ToDictionaryAsync(x => x.UserId, x => x.Last);
}
