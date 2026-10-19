using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

/// <summary>
/// Counts people joining and leaving each guild, one <see cref="MemberFlowDailyStat"/> row per Paris
/// day, for /report community and the monthly digest. Bots are skipped. Fed by BotService's
/// <c>UserJoined</c> / <c>UserLeft</c> fan-out.
/// </summary>
/// <remarks>
/// Singleton, so it opens its own scope per event (root CLAUDE.md). A failed count is logged and
/// dropped: a join is rare and must never cost the welcome that rides the same event.
/// </remarks>
public sealed class MemberFlowTracker
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MemberFlowTracker> _logger;

    public MemberFlowTracker(IServiceProvider services, ILogger<MemberFlowTracker> logger)
    {
        _services = services;
        _logger = logger;
    }

    public Task HandleUserJoinedAsync(SocketGuildUser user) =>
        user.IsBot ? Task.CompletedTask : CountAsync(user.Guild.Id, joined: true);

    public Task HandleUserLeftAsync(SocketGuild guild, SocketUser user) =>
        user.IsBot ? Task.CompletedTask : CountAsync(guild.Id, joined: false);

    private async Task CountAsync(ulong guildId, bool joined)
    {
        try
        {
            await using var scope = _services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var day = AppTime.TodayKey;
            var row = await db.MemberFlowDailyStats.FirstOrDefaultAsync(x => x.GuildId == guildId && x.Day == day);
            if (row is null)
            {
                row = new MemberFlowDailyStat { GuildId = guildId, Day = day };
                db.MemberFlowDailyStats.Add(row);
            }
            if (joined) row.Joins++;
            else row.Leaves++;
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not count a member {Flow} in guild {GuildId}.", joined ? "join" : "leave", guildId);
        }
    }
}
