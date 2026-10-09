using Discord;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// The glitch easter egg: very rarely, a line she was about to say comes out corrupted
// (Helpers/Glitch), and a few seconds later she edits it back to the real one without a word,
// as if nothing happened. Wraps Helpers/BotChat for the conversational chatter only —
// ChatterService and RivalryService send through here. Everything that must stay exact never
// does: the morning hello and the 3 a.m. line are found again by their text, the giveaway
// draw carries pings, the quiz and the verdict replies are tracked elsewhere.
//
// A singleton: the daily cap is in-memory state (a restart resets it, by design). Absent from
// README.md and /help, like the other easter eggs.
public sealed class GlitchService
{
    private readonly BreakdownService _breakdown;
    private readonly ILogger<GlitchService> _logger;

    // One glitch a day at most, wherever it lands. Its own gate, shared with nobody.
    private readonly CooldownGate<int> _daily = new(Glitch.Cooldown);

    public GlitchService(BreakdownService breakdown, ILogger<GlitchService> logger)
    {
        _breakdown = breakdown;
        _logger = logger;
    }

    /// <summary><see cref="BotChat.ReplyWithTypingAsync"/>, with the rare glitch.</summary>
    public async Task<IUserMessage?> ReplyWithTypingAsync(
        IUserMessage replyTo, string line, ILogger logger, string what)
    {
        var glitched = TryGlitch(line, replyTo.Channel.Id);
        var sent = await BotChat.ReplyWithTypingAsync(replyTo, glitched ?? line, logger, what);
        if (sent is not null && glitched is not null) Restore(sent, line, what);
        return sent;
    }

    /// <summary><see cref="BotChat.PostWithTypingAsync"/>, with the rare glitch.</summary>
    public async Task<IUserMessage?> PostWithTypingAsync(
        IMessageChannel channel, string line, ILogger logger, string what)
    {
        var glitched = TryGlitch(line, channel.Id);
        var sent = await BotChat.PostWithTypingAsync(channel, glitched ?? line, logger, what);
        if (sent is not null && glitched is not null) Restore(sent, line, what);
        return sent;
    }

    // The roll first, the daily claim only on a win: a lost roll must not use up the day.
    private string? TryGlitch(string line, ulong channelId)
    {
        if (Random.Shared.NextDouble() >= Glitch.Chance) return null;
        if (_breakdown.IsActive(channelId)) return null;

        var glitched = Glitch.Corrupt(line, Random.Shared);
        if (glitched is null || !_daily.TryClaim(0)) return null;
        return glitched;
    }

    // Off the gateway handler: the wait must not hold up the fan-out, nor Discord.Net's 3 s
    // handler timeout. A failed edit leaves the glitch standing, which is fine for a glitch.
    private void Restore(IUserMessage sent, string line, string what)
    {
        _logger.LogInformation("Glitch: {What} came out corrupted.", what);
        var wait = Glitch.RestoreMin + (Glitch.RestoreMax - Glitch.RestoreMin) * Random.Shared.NextDouble();
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(wait);
                await sent.ModifyAsync(p => p.Content = line);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Glitch: could not restore the {What}.", what);
            }
        });
    }
}
