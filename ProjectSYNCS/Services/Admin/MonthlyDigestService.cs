using System.Text.Json;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Her monthly report to Papa: on the 1st, from 09:00 Paris, the four /report embeds for the month
// just ended (a calendar month, ReportWindow.CalendarMonth) go to the owner's DMs, one per message
// under a line of hers. In a DM, so the reports may name people, as /report does.
//
// report-state.json, next to the database (so under /data in production), keeps the last month
// sent; it is written *before* sending, so a crash costs a digest, never a second one. With no
// file yet (first start after the update, or a lost /data) the previous month is marked as sent
// without sending: a deploy never DMs a surprise. /debug digest sends one on demand.
//
// Its own hourly loop, like the other loops (root CLAUDE.md): the 1st at 09:00 give or take an
// hour is fine, and a digest missed while she was down goes out at the next check that month.
public sealed class MonthlyDigestService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    private readonly DiscordSocketClient _client;
    private readonly IServiceProvider _services;
    private readonly ILogger<MonthlyDigestService> _logger;
    private readonly string _statePath;

    public MonthlyDigestService(DiscordSocketClient client, IServiceProvider services, IConfiguration config,
        ILogger<MonthlyDigestService> logger)
    {
        _client = client;
        _services = services;
        _logger = logger;

        var dbPath = Path.GetFullPath(config["Database:Path"] ?? "ProjectSYNCS.db");
        _statePath = Path.Combine(Path.GetDirectoryName(dbPath) ?? ".", "report-state.json");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (LoadState() is null)
        {
            var (year, month) = MonthlyDigest.PreviousMonth(DateTimeOffset.UtcNow);
            SaveState(new DigestState(MonthlyDigest.MonthKey(year, month)));
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CheckInterval, stoppingToken);
            if (_client.ConnectionState != ConnectionState.Connected) continue;
            try
            {
                await TickAsync();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // An exception escaping the loop would stop the whole bot.
                _logger.LogWarning(ex, "Monthly digest check failed.");
            }
        }
    }

    private async Task TickAsync()
    {
        var last = LoadState()?.LastMonth ?? 0;
        if (MonthlyDigest.Due(DateTimeOffset.UtcNow, last) is not { } due) return;

        // Claimed before sending: a crash mid-send costs this digest, never a second one.
        SaveState(new DigestState(MonthlyDigest.MonthKey(due.Year, due.Month)));
        await SendAsync(due.Year, due.Month);
    }

    /// <summary>
    /// Sends the digest for one month to the owner now, without touching the state: the loop's send,
    /// and /debug digest's. Returns whether the DM went out (the reports themselves may have failed
    /// one by one, logged).
    /// </summary>
    public async Task<bool> SendAsync(int year, int month)
    {
        var window = ReportWindow.CalendarMonth(year, month);
        IDMChannel dm;
        try
        {
            var owner = await _client.GetUserAsync(AvailabilityService.OwnerId);
            if (owner is null)
            {
                _logger.LogWarning("Monthly digest: could not resolve the owner.");
                return false;
            }
            dm = await owner.CreateDMChannelAsync();
            var line = string.Format(
                DailyRotation.Pick(BotResponses.OwnerDigestLines, year * 12 + month, salt: 0xD16E57),
                window.Label);
            await dm.SendMessageAsync(line, allowedMentions: AllowedMentions.None);
        }
        catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
        {
            _logger.LogWarning("Monthly digest: the owner's DMs are closed.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Monthly digest: could not open the owner's DMs.");
            return false;
        }

        // One report per message, each in its own try: a failed one doesn't cost the others, and
        // four embeds in one message could pass the 6000-character cap.
        foreach (var kind in Enum.GetValues<ReportKind>())
        {
            try
            {
                await using var scope = _services.CreateAsyncScope();
                var reports = scope.ServiceProvider.GetRequiredService<ReportService>();
                var embed = await reports.BuildAsync(kind, HomeGuild.Id, window, names: true);
                await dm.SendMessageAsync(embed: embed, allowedMentions: AllowedMentions.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Monthly digest: the {Kind} report failed for {Month}.", kind, window.Label);
            }
        }
        _logger.LogInformation("Monthly digest for {Month} sent.", window.Label);
        return true;
    }

    // What a restart must not forget: the last month whose digest went out (yyyymm).
    private sealed record DigestState(int LastMonth);

    private DigestState? LoadState()
    {
        try
        {
            if (File.Exists(_statePath))
                return JsonSerializer.Deserialize<DigestState>(File.ReadAllText(_statePath));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Monthly digest: could not read {Path}.", _statePath);
        }
        return null;
    }

    private void SaveState(DigestState state)
    {
        try
        {
            File.WriteAllText(_statePath, JsonSerializer.Serialize(state));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Monthly digest: could not write {Path}.", _statePath);
        }
    }
}
