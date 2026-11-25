using Discord;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Waking after a power cut or a crash. One wake for the whole process (she has one body), told
// in the main channel of every server where the ambient feature is on and she isn't silenced,
// each server its own line and its own « first_wake ». Part of AmbientService.
internal sealed partial class AmbientService
{
    // How long to wait for the main channels after the first Ready before giving up the wake line.
    private static readonly TimeSpan WakeChannelWait = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ChannelPollInterval = TimeSpan.FromSeconds(10);

    private const ulong PowerCutSalt = 0x5359_4E43_504F_5752;   // "SYNCPOWR"
    private const ulong CrashSalt = 0x5359_4E43_4352_5348;      // "SYNCCRSH"
    private const ulong GlitchSalt = 0x5359_4E43_474C_4954;     // "SYNCGLIT"

    private int _woken;

    // Ready fires on every reconnect; only the first one of the process is a wake-up.
    private Task OnReadyAsync()
    {
        if (Interlocked.Exchange(ref _woken, 1) == 0)
            _ = Task.Run(WakeAsync);
        return Task.CompletedTask;
    }

    private async Task WakeAsync()
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var today = AppTime.DayKey(now);
            // Only a power cut or a crash gets a wake line, whatever the hour, however many
            // times today. An ordinary restart or update is silent.
            var stop = _health.LastStop;
            if (!PiHealth.IsAbrupt(stop)) return;

            var day = AppTime.DayNumber(now);
            // One roll for the whole wake: the same kind of waking on every server.
            bool glitch = Random.Shared.NextDouble() < Ambient.GlitchWakeChance;

            var wakes = (await WaitForMainChannelsAsync())
                .Where(c => !_breakdown.IsActive(c.Channel.Id))
                .Select(c => Task.Run(() => glitch
                    ? GlitchWakeAsync(c.GuildId, c.Channel, day, today)
                    : PlainWakeAsync(c.GuildId, c.Channel, stop, now, day, today)));
            await Task.WhenAll(wakes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ambient: wake line failed.");
        }
    }

    private async Task PlainWakeAsync(ulong guildId, IMessageChannel channel, StopKind stop, DateTimeOffset now, int day, int today)
    {
        try
        {
            var line = string.Format(stop == StopKind.PowerCut
                    ? DailyRotation.Pick(BotResponses.PowerCutWakeLines.At(now), day, PowerCutSalt)
                    : DailyRotation.Pick(BotResponses.CrashWakeLines, day, CrashSalt),
                PiHealth.Duration(_health.Downtime));

            var sent = await BotChat.PostWithTypingAsync(channel, line, _logger, "wake line", AllowedMentions.None);
            _herMessages.Remember(sent, ReplyContext.Wake);
            if (sent is null) return;
            lock (_stateLock) SaveState(LoadState() with { LastVersion = AppInfo.Version, LastWakeDay = today });
            ArmFirstWake(guildId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ambient: wake line failed in guild {GuildId}.", guildId);
        }
    }

    // Back from a power cut or a crash, corrupted: the glitched line stands for a while — two
    // minutes, or until someone speaks in the main channel and she snaps back — then the same
    // message is edited clean. The glitch strip follows the corrupted line and is deleted as she
    // snaps back, so nothing is left to prove it. It runs beside the wait, not before it, so
    // someone speaking during the upload still catches her. Off the gateway handler (WakeAsync
    // runs in Task.Run), one per server, side by side.
    private async Task GlitchWakeAsync(ulong guildId, IMessageChannel channel, int day, int today)
    {
        try
        {
            var (glitched, recovered) = DailyRotation.Pick(BotResponses.GlitchWakeLines.Now, day, GlitchSalt);
            var sent = await BotChat.PostWithTypingAsync(channel, glitched, _logger, "glitch wake line", AllowedMentions.None);
            if (sent is null) return;
            _herMessages.Remember(sent, ReplyContext.Wake);
            lock (_stateLock) SaveState(LoadState() with { LastVersion = AppInfo.Version, LastWakeDay = today });
            ArmFirstWake(guildId);

            var strip = Task.Run(async () =>
            {
                await Task.Delay(StickerService.Beat);
                return await StickerService.SendAsync(channel, Sticker.GlitchMood, _logger);
            });

            var clean = string.Format(recovered, PiHealth.Duration(_health.Downtime));
            await CorrectLaterAsync(guildId, sent, clean, Ambient.GlitchEditAfter, "glitch wake line");

            if (await strip is { } picture) await picture.DeleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ambient: glitch wake failed in guild {GuildId}.", guildId);
        }
    }

    private void ArmFirstWake(ulong guildId)
    {
        lock (_gate) StateOf(guildId).FirstWakeUntil = DateTimeOffset.UtcNow + Ambient.FirstWakeWindow;
    }

    // After the first Ready the guilds arrive one by one: wait until at least one main channel is
    // there, never past WakeChannelWait (a bot on no server with the feature just runs it out).
    private async Task<IReadOnlyList<(ulong GuildId, IMessageChannel Channel)>> WaitForMainChannelsAsync()
    {
        var deadline = DateTimeOffset.UtcNow + WakeChannelWait;
        while (true)
        {
            var channels = new List<(ulong, IMessageChannel)>();
            foreach (var guild in _client.Guilds)
            {
                if (_silence.IsSilent(guild.Id)) continue;
                if (await MainChannelAsync(guild) is { } main) channels.Add((guild.Id, main));
            }
            if (channels.Count > 0 || DateTimeOffset.UtcNow >= deadline) return channels;
            await Task.Delay(ChannelPollInterval);
        }
    }
}
