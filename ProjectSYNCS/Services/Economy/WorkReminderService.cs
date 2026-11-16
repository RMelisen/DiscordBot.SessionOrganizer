using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>
/// The /work DM reminder, switched on from /balance: once a member's cooldown has ended, one DM
/// saying `/work` is ready again. Its own 2-minute loop (a couple of minutes late is nothing on a
/// 4-hour cooldown). Each wallet is claimed before the DM goes out, so a crash costs the DM and never
/// sends a second; a refused DM is dropped, never relayed to a channel. Not muted by silence mode:
/// the member asked for it, like a /remindme.
/// </summary>
public class WorkReminderService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(2);

    private readonly IServiceProvider _services;
    private readonly DiscordSocketClient _client;
    private readonly ResponsePicker _picker;
    private readonly ILogger<WorkReminderService> _logger;

    public WorkReminderService(IServiceProvider services, DiscordSocketClient client, ResponsePicker picker,
        ILogger<WorkReminderService> logger)
    {
        _services = services;
        _client = client;
        _picker = picker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Interval, stoppingToken);
            if (_client.ConnectionState != ConnectionState.Connected) continue;

            var now = DateTimeOffset.UtcNow;
            List<(int Id, ulong UserId)> due;
            try
            {
                await using var scope = _services.CreateAsyncScope();
                var wallets = await scope.ServiceProvider.GetRequiredService<PebbleService>().GetReminderWalletsAsync();
                due = wallets
                    .Where(w => PebbleEconomy.WorkReminderDue(w.LastWorkAt, w.WorkRemindedAt, now))
                    .Select(w => (w.Id, w.UserId))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read the /work reminders.");
                continue;
            }

            foreach (var (id, userId) in due)
            {
                try
                {
                    await RemindAsync(id, userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send the /work reminder for wallet {WalletId}.", id);
                }
            }
        }
    }

    private async Task RemindAsync(int walletId, ulong userId)
    {
        await using (var scope = _services.CreateAsyncScope())
        {
            if (!await scope.ServiceProvider.GetRequiredService<PebbleService>().ClaimWorkReminderAsync(walletId))
                return;
        }

        try
        {
            var user = await _client.GetUserAsync(userId);
            if (user is null) return;
            await user.SendMessageAsync(_picker.Pick(BotResponses.WorkReadyLines), allowedMentions: AllowedMentions.None);
        }
        catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
        {
            _logger.LogDebug("/work reminder for {UserId} not delivered: their DMs are closed.", userId);
        }
    }
}
