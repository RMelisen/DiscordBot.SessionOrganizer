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
public static class BotChat
{
    public static async Task ReplyWithTypingAsync(
        IUserMessage replyTo, string line, ILogger logger, string what)
    {
        try
        {
            using (replyTo.Channel.EnterTypingState())
            {
                await Task.Delay(TypingDelayFor(line));
            }
            await replyTo.ReplyAsync(line);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send {What} in channel {ChannelId}.", what, replyTo.Channel.Id);
        }
    }

    public static async Task PostWithTypingAsync(
        IMessageChannel channel, string line, ILogger logger, string what)
    {
        try
        {
            using (channel.EnterTypingState())
            {
                await Task.Delay(TypingDelayFor(line));
            }
            await channel.SendMessageAsync(line);
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
