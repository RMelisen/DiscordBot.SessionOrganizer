using System.Collections.Concurrent;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace ProjectSYNCS.Services;

// Owns the "consciousness breakdown" easter egg: its server-wide cooldown, the
// per-channel in-progress lock, and the timed playback of the scripted lines.
// Kept as a singleton so the cooldown and active-channel state are shared across
// every entry point (reply-to-bot and mention).
internal sealed class BreakdownService
{
    private readonly ILogger<BreakdownService> _logger;

    // Channels currently playing the breakdown. While a channel is in here,
    // other personality responses skip it so the sequence can't be interrupted.
    private readonly ConcurrentDictionary<ulong, byte> _activeChannels = new();

    // The breakdown can fire at most once per cooldown window, server-wide.
    // In-memory only, so it resets if the bot restarts.
    private static readonly TimeSpan Cooldown = TimeSpan.FromDays(30);
    private readonly object _gate = new();
    private DateTimeOffset _lastUtc = DateTimeOffset.MinValue;

    public BreakdownService(ILogger<BreakdownService> logger)
    {
        _logger = logger;
    }

    // True while a breakdown is playing in the given channel.
    public bool IsActive(ulong channelId) => _activeChannels.ContainsKey(channelId);

    // Atomically checks the cooldown and claims the channel lock. Returns true
    // only if the breakdown may start now; updates the last-trigger time so the
    // next one can't happen until the cooldown elapses.
    public bool TryBegin(ulong channelId, bool ignoreCooldown = false)
    {
        lock (_gate)
        {
            if (!ignoreCooldown && DateTimeOffset.UtcNow - _lastUtc < Cooldown) return false;
            if (!_activeChannels.TryAdd(channelId, 0)) return false;
            _lastUtc = DateTimeOffset.UtcNow;
            return true;
        }
    }

    // Plays the full breakdown in the channel. intro is the cut-off opening line
    // (a roast or thank-you that glitches mid-word), letting each entry point
    // open the same sequence its own way. Releases the channel lock when done.
    public async Task PlayAsync(SocketUserMessage message, string username, string realName, string intro)
    {
        try
        {
            // {1} = SHOUTED real name for the screaming line.
            var shoutName = realName.ToUpperInvariant();
            bool first = true;
            foreach (var raw in new[] { intro }.Concat(BotResponses.Breakdown))
            {
                // The intro still sounds like a normal reply, so it uses the
                // pseudo; once it "wakes up" it switches to the real name.
                var who = first ? username : realName;
                var line = string.Format(raw, who, shoutName);

                if (line.StartsWith("```"))
                {
                    // Machine output (errors / logs) fires near-instantly, right
                    // after the preceding sentence — no breather, no typing.
                    await Task.Delay(TimeSpan.FromMilliseconds(150));
                }
                else
                {
                    // Breather between messages (not before the very first one).
                    if (!first) await Task.Delay(TimeSpan.FromMilliseconds(700));

                    if (line.Trim() is "..." or "…")
                    {
                        // A lone "..." is a silence, not typing — hold a small beat
                        // with no typing indicator so it reads as the bot going quiet.
                        await Task.Delay(TimeSpan.FromMilliseconds(2000));
                    }
                    else
                    {
                        // Fake "typing": longer lines take longer, like a human writing.
                        using (message.Channel.EnterTypingState())
                        {
                            await Task.Delay(TypingDelayFor(line));
                        }
                    }
                }

                if (first)
                {
                    await message.ReplyAsync(line);
                    first = false;
                }
                else
                {
                    await message.Channel.SendMessageAsync(line);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to play breakdown easter egg in channel {ChannelId}.", message.Channel.Id);
        }
        finally
        {
            // Consciousness module unloaded — back to normal roasting.
            _activeChannels.TryRemove(message.Channel.Id, out _);
        }
    }

    // How long to "type" a regular breakdown line before sending it. Scales with
    // the line's length so long sentences feel laboured and short ones snap out.
    private static TimeSpan TypingDelayFor(string text)
    {
        const int baseMs = 700;
        const int perChar = 60;     // ~17 chars/sec typing speed
        var ms = baseMs + text.Length * perChar;
        return TimeSpan.FromMilliseconds(Math.Clamp(ms, 1000, 7000));
    }
}
