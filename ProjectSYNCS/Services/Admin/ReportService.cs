using Discord;
using Discord.WebSocket;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>The four reports, in the order the digest sends them. Used in custom-ids by name: never rename.</summary>
public enum ReportKind
{
    Sessions,
    Polls,
    Community,
    Syncs,
}

/// <summary>
/// One report as an embed, whoever asks: /report, its period buttons and the monthly digest all go
/// through here, so they can't drift apart.
/// </summary>
/// <remarks>Transient: it wraps the transient report services.</remarks>
public class ReportService
{
    private readonly SchedulingReportService _scheduling;
    private readonly CommunityReportService _community;
    private readonly SyncsReportService _syncs;
    private readonly DiscordSocketClient _client;

    public ReportService(SchedulingReportService scheduling, CommunityReportService community,
        SyncsReportService syncs, DiscordSocketClient client)
    {
        _scheduling = scheduling;
        _community = community;
        _syncs = syncs;
        _client = client;
    }

    public async Task<Embed> BuildAsync(ReportKind kind, ulong guildId, ReportWindow window, bool names)
    {
        var now = DateTimeOffset.UtcNow;
        return kind switch
        {
            ReportKind.Sessions => ReportCards.BuildSessions(await _scheduling.GetSessionsAsync(guildId, window, now), window, names),
            // Her Wednesday poll is the one she organised herself.
            ReportKind.Polls => ReportCards.BuildPolls(
                await _scheduling.GetPollsAsync(guildId, window, _client.CurrentUser?.Id ?? 0), window, names),
            ReportKind.Community => ReportCards.BuildCommunity(await _community.GetAsync(guildId, window), window, names),
            _ => ReportCards.BuildSyncs(await _syncs.GetAsync(guildId, window, now), window, await _syncs.RecordAsync(), names),
        };
    }
}
