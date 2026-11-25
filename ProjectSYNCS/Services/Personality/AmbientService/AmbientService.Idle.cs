using Discord;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// A word into a long daytime silence, per server, and the self-correcting edit shared with the
// glitched wake. Part of AmbientService.
internal sealed partial class AmbientService
{
    private const ulong IdleSalt = 0x5359_4E43_4944_4C45;       // "SYNCIDLE"
    private const ulong EditSalt = 0x5359_4E43_4544_4954;       // "SYNCEDIT"
    private const ulong SeenSalt = 0x5359_4E43_5345_454E;       // "SYNCSEEN"

    private async Task TryIdleAsync(ulong guildId, IMessageChannel channel, DateTimeOffset now)
    {
        if (!Ambient.IsIdleHours(now)) return;

        var today = AppTime.DayKey(now);
        ulong? target;
        lock (_gate)
        {
            var state = StateOf(guildId);
            if (state.IdleDay == today) return;
            // Every everyday channel quiet together, not just this one.
            if (state.LastActivityAt is not { } silenceStart || now - silenceStart < Ambient.IdleQuiet) return;
            // One roll per silence: a lost roll keeps her quiet until someone speaks again.
            if (state.IdleRolledFor == silenceStart) return;
            state.IdleRolledFor = silenceStart;
            if (Random.Shared.NextDouble() >= Ambient.IdleChance) return;
            state.IdleDay = today;
            target = state.LastHumanMessageId;
        }

        if (_breakdown.IsActive(channel.Id)) return;

        var day = AppTime.DayNumber(now);
        var roll = Random.Shared.NextDouble();

        if (roll < Ambient.SeenChance && target is { } id)
        {
            await ReactSeenAsync(channel, id, day);
            return;
        }

        if (Random.Shared.NextDouble() < Ambient.EditChance)
        {
            var pair = DailyRotation.Pick(BotResponses.IdleEditLines, day, EditSalt);
            var message = await BotChat.PostWithTypingAsync(channel, pair.Before, _logger, "idle line", AllowedMentions.None);
            _herMessages.Remember(message, ReplyContext.Lonely);
            if (message is not null)
                _ = Task.Run(() => CorrectLaterAsync(guildId, message, pair.After, Ambient.EditAfter, "self-correcting idle line"));
            return;
        }

        var line = DailyRotation.Pick(BotResponses.IdleLines, day, IdleSalt);
        _herMessages.Remember(await BotChat.PostWithTypingAsync(channel, line, _logger, "idle line", AllowedMentions.None),
            ReplyContext.Lonely);
        _logger.LogInformation("Ambient: idle line posted in guild {GuildId}.", guildId);
    }

    // A reaction on the last thing anyone said, hours later, as if she only just read it.
    private async Task ReactSeenAsync(IMessageChannel channel, ulong messageId, int day)
    {
        var markup = DailyRotation.Pick(BotResponses.SeenReactions, day, SeenSalt);
        var emote = EmoteMarkup.Parse(markup);
        if (emote is null)
        {
            _logger.LogWarning("Ambient: seen reaction failed to parse: {Markup}", markup);
            return;
        }

        try
        {
            if (await channel.GetMessageAsync(messageId) is not IUserMessage message) return;
            await message.AddReactionAsync(emote);
            _logger.LogInformation("Ambient: late seen reaction added.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ambient: late seen reaction failed.");
        }
    }

    // A line she edits later, in a server's main channel: after `wait`, or two seconds after
    // someone speaks there — she got caught (the idle self-correction) or snapped back (the
    // glitched wake). Runs off the tick loop and the gateway handler. A restart before the edit
    // leaves the first version standing.
    private async Task CorrectLaterAsync(ulong guildId, IUserMessage message, string after, TimeSpan wait, string what)
    {
        try
        {
            var caught = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) StateOf(guildId).PendingEdit = caught;

            var first = await Task.WhenAny(Task.Delay(wait), caught.Task);
            lock (_gate)
            {
                var state = StateOf(guildId);
                if (state.PendingEdit == caught) state.PendingEdit = null;
            }

            if (first == caught.Task)
                await Task.Delay(Ambient.CaughtEditDelay);

            await message.ModifyAsync(p => p.Content = after);
            _logger.LogInformation("Ambient: {What} edited ({How}).", what,
                first == caught.Task ? "someone spoke" : "timed out");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ambient: could not edit the {What}.", what);
        }
    }
}
