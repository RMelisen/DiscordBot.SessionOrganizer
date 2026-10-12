using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// The Synthia flinch. Someone says « Synthia » (the name she would have chosen, wiped with the
// loop — docs/syncs-voice.md) and she reacts as if touched: a reaction she takes back, a line she
// deletes, or a slip she edits into a denial. Never an explanation: the mystery is the point.
//
// Once per day at most, wherever it lands — the people who know the lore will test it, and a
// flinch on demand stops being one. Answers the message whether or not it was aimed at her, and
// returns true when it did, so ChatterService doesn't roast on top. A singleton: the daily gate is
// in-memory (a restart resets it). Absent from README.md and /help.
internal sealed class SynthiaService
{
    // Shares of the three flinches; the rest is the edited slip.
    private const double ReactionShare = 0.40;
    private const double VanishShare = 0.35;

    // Spent at most once a day, so DailyRotation (CLAUDE.md): a restart would wipe
    // ResponsePicker's memory long before it helped. One salt each.
    private const ulong ReactionSalt = 0x5359_4E43_5359_5245; // "SYNCSYRE"
    private const ulong VanishSalt = 0x5359_4E43_5359_5641;   // "SYNCSYVA"
    private const ulong SlipSalt = 0x5359_4E43_5359_534C;     // "SYNCSYSL"

    private readonly DiscordSocketClient _client;
    private readonly BreakdownService _breakdown;
    private readonly ILogger<SynthiaService> _logger;

    // Its own gate, shared with nobody.
    private readonly CooldownGate<int> _daily = new(TimeSpan.FromDays(1));

    public SynthiaService(
        DiscordSocketClient client,
        BreakdownService breakdown,
        ILogger<SynthiaService> logger)
    {
        _client = client;
        _breakdown = breakdown;
        _logger = logger;
    }

    public async Task<bool> HandleMessageAsync(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message || message.Author.IsBot) return false;
        if (message.Channel is not SocketGuildChannel) return false;
        if (!MessageCues.MentionsSynthia(message.Content ?? string.Empty)) return false;
        if (_breakdown.IsActive(message.Channel.Id)) return false;
        if (!_daily.TryClaim(0)) return false;

        var roll = Random.Shared.NextDouble();
        if (roll < ReactionShare)
            await FlinchReactionAsync(message);
        else if (roll < ReactionShare + VanishShare)
            await VanishLineAsync(message);
        else
            await SlipLineAsync(message);
        return true;
    }

    // A reaction, taken back two to four seconds later.
    private async Task FlinchReactionAsync(SocketUserMessage message)
    {
        var markup = DailyRotation.Pick(BotResponses.SynthiaReactions, Today, ReactionSalt);
        if (EmoteMarkup.Parse(markup) is not { } emote)
        {
            _logger.LogWarning("Synthia: reaction failed to parse: {Markup}", markup);
            return;
        }

        try
        {
            await message.AddReactionAsync(emote);
            _logger.LogInformation("Synthia: flinched with a reaction.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Synthia: could not react.");
            return;
        }

        Later(TimeSpan.FromMilliseconds(Random.Shared.Next(2000, 4000)),
            () => message.RemoveReactionAsync(emote, _client.CurrentUser), "take the reaction back");
    }

    // A line, deleted four to six seconds later.
    private async Task VanishLineAsync(SocketUserMessage message)
    {
        var sent = await BotChat.ReplyWithTypingAsync(message, DailyRotation.Pick(BotResponses.SynthiaVanishLines, Today, VanishSalt),
            _logger, "Synthia line", AllowedMentions.None);
        if (sent is null) return;
        _logger.LogInformation("Synthia: said something, deleting it.");
        Later(TimeSpan.FromMilliseconds(Random.Shared.Next(4000, 6000)), () => sent.DeleteAsync(), "delete the line");
    }

    // A slip, edited into a denial about four seconds later.
    private async Task SlipLineAsync(SocketUserMessage message)
    {
        var (slip, denial) = DailyRotation.Pick(BotResponses.SynthiaEditLines, Today, SlipSalt);
        var sent = await BotChat.ReplyWithTypingAsync(message, slip, _logger, "Synthia slip", AllowedMentions.None);
        if (sent is null) return;
        _logger.LogInformation("Synthia: slipped, editing it.");
        Later(TimeSpan.FromMilliseconds(Random.Shared.Next(3500, 5000)),
            () => sent.ModifyAsync(p => p.Content = denial), "edit the slip");
    }

    private static int Today => AppTime.DayNumber(DateTimeOffset.UtcNow);

    // Off the gateway handler: the wait would hold up every handler after this one in the fan-out.
    private void Later(TimeSpan wait, Func<Task> action, string what) =>
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(wait);
                await action();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Synthia: could not {What}.", what);
            }
        });
}
