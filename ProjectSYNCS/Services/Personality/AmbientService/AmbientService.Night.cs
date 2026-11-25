using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// The 3 a.m. line and the scolding for answering it, per server. Part of AmbientService.
internal sealed partial class AmbientService
{
    private const ulong NightSalt = 0x5359_4E43_4E49_4748;      // "SYNCNIGH"

    // Sends to bed someone who answered the 3 a.m. line. Returns whether she answered.
    private async Task<bool> ScoldAsync(SocketUserMessage message, ulong guildId)
    {
        // They answered the line, whether or not the scolding goes out.
        _badges.Award(guildId, message.Author.Id, MemberBadges.Keys.NightOwl);
        if (_breakdown.IsActive(message.Channel.Id)) return false;

        var line = string.Format(_picker.Pick(BotResponses.NightScoldLines),
            BotResponses.DisplayNameFor(message.Author));
        await BotChat.ReplyWithTypingAsync(message, line, _logger, "night scolding");
        _logger.LogInformation("Ambient: sent {UserId} to bed in guild {GuildId}.", message.Author.Id, guildId);
        return true;
    }

    // The 3 a.m. line goes to the bots channel; the quiet it waits for is the main channel's
    // (`main`). The scolding answers whoever answers the line, there or by replying to it.
    private async Task TryNightLineAsync(ulong guildId, IMessageChannel main, DateTimeOffset now)
    {
        if (!Ambient.IsNightLineHour(now)) return;

        var today = AppTime.DayKey(now);
        lock (_gate)
        {
            var state = StateOf(guildId);
            if (state.NightDay != today)
            {
                state.NightDay = today;
                state.NightMinute = Ambient.NightMinute(Random.Shared);
                state.NightDone = false;
                _logger.LogInformation("Ambient: night decision for {Day} in guild {GuildId}: {Minute}.", today, guildId,
                    state.NightMinute is { } m ? $"3:{m:00}" : "quiet");
            }

            if (state.NightDone || state.NightMinute is not { } minute || AppTime.ToZoned(now).Minute < minute) return;
            // Not into a conversation: the line is for an empty channel, found in the morning.
            if (state.LastHumanAt is not { } last || now - last < Ambient.NightQuiet) return;
            state.NightDone = true;
        }

        if (_breakdown.IsActive(main.Id)) return;
        var channel = await ResolveBotsChannelAsync(guildId);
        if (channel is null) return;
        // A restart inside the hour draws a new decision; this keeps her to one line a night.
        if (await SaidNightLineTodayAsync(channel, today)) return;

        var line = DailyRotation.Pick(BotResponses.NightLines, AppTime.DayNumber(now), NightSalt);
        var sent = await BotChat.PostWithTypingAsync(channel, line, _logger, "night line", AllowedMentions.None);
        if (sent is null) return;

        lock (_gate)
        {
            var state = StateOf(guildId);
            state.NightLineAt = sent.Timestamp;
            state.NightLineId = sent.Id;
            state.NightChannelId = channel.Id;
            state.Scolded.Clear();
        }
        _logger.LogInformation("Ambient: night line posted in guild {GuildId}.", guildId);
    }

    private async Task<bool> SaidNightLineTodayAsync(IMessageChannel channel, int today)
    {
        try
        {
            var recent = await channel.GetMessagesAsync(HistoryDepth).FlattenAsync();
            return recent.Any(m => m.Author.Id == _client.CurrentUser.Id
                                   && AppTime.DayKey(m.Timestamp) == today
                                   && NightLineSet.Contains(m.Content));
        }
        catch (Exception ex)
        {
            // Better a rare second line than none.
            _logger.LogWarning(ex, "Ambient: could not check for tonight's line.");
            return false;
        }
    }
}
