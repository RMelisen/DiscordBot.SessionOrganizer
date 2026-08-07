using Discord;
using Microsoft.Extensions.Logging;

namespace ProjectSYNCS.Helpers;

// The single send path for the bot's *own* chatter. Every personality line goes
// through here, so it pauses behind the typing indicator first — she reads as
// composing an answer instead of firing back instantly — and so the swallow-and-log
// lives in exactly one place.
//
// Two paths deliberately skip this and send directly: the owner-reply relay and the
// DM acknowledgements. Delaying a human's words, or a "✅ transmis" receipt, only
// adds latency.
//
// Three send methods share that one pause: ReplyWithTypingAsync / PostWithTypingAsync
// for plain text, PostEmbedWithTypingAsync for an embed (the level-up card, so far
// its only caller).
public static class BotChat
{
    /// <summary>
    /// Replies behind the typing indicator. Returns the sent message, or null if the
    /// send failed — callers that need to remember what they just said (see
    /// BotFeedbackTracker suppressing verdicts on its own bad-bot replies) need its
    /// id, and there is no other way to learn it.
    /// </summary>
    public static async Task<IUserMessage?> ReplyWithTypingAsync(
        IUserMessage replyTo, string line, ILogger logger, string what)
    {
        try
        {
            using (replyTo.Channel.EnterTypingState())
            {
                await Task.Delay(TypingDelayFor(line));
            }
            return await replyTo.ReplyAsync(line);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send {What} in channel {ChannelId}.", what, replyTo.Channel.Id);
            return null;
        }
    }

    /// <summary>
    /// Posts behind the typing indicator. <paramref name="allowedMentions"/> is for the
    /// rare line that is *meant* to ping — the giveaway draw announcing its winners —
    /// and should be as narrow as the line needs (users only, never roles or everyone).
    /// Left null, Discord's default applies, which is what ordinary chatter wants.
    /// </summary>
    public static async Task PostWithTypingAsync(
        IMessageChannel channel, string line, ILogger logger, string what,
        AllowedMentions? allowedMentions = null)
    {
        try
        {
            using (channel.EnterTypingState())
            {
                await Task.Delay(TypingDelayFor(line));
            }
            await channel.SendMessageAsync(line, allowedMentions: allowedMentions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send {What} in channel {ChannelId}.", what, channel.Id);
        }
    }

    /// <summary>
    /// Posts an embed behind the same typing pause. <paramref name="delayText"/> is
    /// only used to size that pause — the embed carries the actual content, and
    /// nothing here is sent as message text.
    /// </summary>
    public static async Task PostEmbedWithTypingAsync(
        IMessageChannel channel, Embed embed, string delayText, ILogger logger, string what)
    {
        try
        {
            using (channel.EnterTypingState())
            {
                await Task.Delay(TypingDelayFor(delayText));
            }
            await channel.SendMessageAsync(embed: embed);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send {What} in channel {ChannelId}.", what, channel.Id);
        }
    }

    // How long to "type" a chat line before sending it. Much snappier than
    // BreakdownService's laboured pacing, which is dramatising a collapse: this fires
    // on every single reply, and the whole pause has to stay comfortably inside
    // Discord.Net's 3 s HandlerTimeout.
    private static TimeSpan TypingDelayFor(string text)
    {
        const int baseMs = 350;
        const int perChar = 30;     // ~33 chars/sec — a brisk typist
        var ms = baseMs + text.Length * perChar;
        return TimeSpan.FromMilliseconds(Math.Clamp(ms, 500, 2000));
    }
}
