using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Her robot heart (docs/syncs-voice.md): someone names WALL-E, EVE, GLaDOS, Wheatley, a robot
// vacuum, Monika, 2B or Voyager 1, and she reacts in that character's register — a reaction
// and a line, or one of the Synthia flinch's moves (a reaction taken back, a line deleted, a
// slip edited). Which one, and how often, is Helpers/Personality/RobotHeart.
//
// Not aimed at her, she reacts 60% of the time; aimed at her, always, in words, and the act is
// her whole answer: returns true so ChatterService doesn't roast on top. When the gate refuses
// (that character already today, or three acts already), returns false and the message goes on
// to ChatterService as usual. A singleton: the gate is in-memory (a restart resets it). After
// SynthiaService in the fan-out, so Synthia wins a message naming both. Absent from README.md
// and /help.
internal sealed class RobotHeartService
{
    // Spent at most once a day per character, so DailyRotation (CLAUDE.md). One salt per pool,
    // shifted by the character so two characters' pools don't move in lockstep.
    private const ulong ReactionSalt = 0x5359_4E43_4845_5245; // "SYNCHERE"
    private const ulong LineSalt = 0x5359_4E43_4845_4C49;     // "SYNCHELI"
    private const ulong VanishSalt = 0x5359_4E43_4845_5641;   // "SYNCHEVA"
    private const ulong EditSalt = 0x5359_4E43_4845_4544;     // "SYNCHEED"

    private readonly DiscordSocketClient _client;
    private readonly BreakdownService _breakdown;
    private readonly StickerService _stickers;
    private readonly ILogger<RobotHeartService> _logger;

    // Its own gate, shared with nobody.
    private readonly HeartGate _gate = new();

    public RobotHeartService(
        DiscordSocketClient client,
        BreakdownService breakdown,
        StickerService stickers,
        ILogger<RobotHeartService> logger)
    {
        _client = client;
        _breakdown = breakdown;
        _stickers = stickers;
        _logger = logger;
    }

    public async Task<bool> HandleMessageAsync(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message || message.Author.IsBot) return false;
        if (message.Channel is not SocketGuildChannel) return false;
        if (CharacterNamedIn(message.Content ?? string.Empty) is not { } who) return false;
        if (_breakdown.IsActive(message.Channel.Id)) return false;

        bool aimedAtHer = message.MentionedUsers.Any(u => u.Id == _client.CurrentUser.Id)
                          || message.ReferencedMessage?.Author.Id == _client.CurrentUser.Id;
        // The roll before the claim: a lost roll spends nothing.
        if (!aimedAtHer && Random.Shared.NextDouble() >= RobotHeart.AmbientChance) return false;

        var today = AppTime.DayNumber(DateTimeOffset.UtcNow);
        if (!_gate.TryClaim(who, today)) return false;

        var act = RobotHeart.RollAct(who, aimedAtHer, Random.Shared.NextDouble(), Random.Shared.NextDouble());
        _logger.LogInformation("Robot heart: {Who} named, {Act}.", who, act);

        switch (act)
        {
            case HeartActKind.TakeBackReaction:
                await TakeBackReactionAsync(message, who, today);
                break;
            case HeartActKind.VanishLine:
                await VanishLineAsync(message, who, today);
                break;
            case HeartActKind.EditSlip:
                await EditSlipAsync(message, who, today);
                break;
            default:
                await ReactAsync(message, who, today);
                await ReplyLineAsync(message, who,
                    DailyRotation.Pick(BotResponses.HeartLines[who], today, LineSalt ^ Shift(who)));
                break;
        }
        return true;
    }

    /// <summary>
    /// The character a message names, or null — also for a « good bot » / « bad bot » verdict,
    /// which is BotFeedbackTracker's alone (docs/agents/bot-feedback.md): a verdict that names
    /// WALL-E must not be answered twice.
    /// </summary>
    internal static HeartCharacter? CharacterNamedIn(string content) =>
        MessageCues.ReadFeedback(content) != FeedbackKind.None ? null : MessageCues.ReadHeartCharacter(content);

    // The line + emote reply. A character with its own sticker chance (WALL-E, 2 in 3): the tag is
    // split off here, the chance rolled here, and a win handed on promoted to sure so
    // StickerService doesn't roll its ordinary 1 in 3 on top. Everyone else goes through BotChat.
    private async Task ReplyLineAsync(SocketUserMessage message, HeartCharacter who, string line)
    {
        if (RobotHeart.StickerChance(who) is null || Sticker.Split(line) is not (var text, { } tag))
        {
            await BotChat.ReplyWithTypingAsync(message, line, _logger, "robot heart line", AllowedMentions.None);
            return;
        }

        var sent = await BotChat.ReplyWithTypingAsync(message, text, _logger, "robot heart line", AllowedMentions.None);
        if (sent is not null && RobotHeart.WinsSticker(who, Random.Shared.NextDouble()))
            _stickers.TryFollowUp(message.Channel, tag with { Sure = true });
    }

    // The character's reaction on the message; the emote it used, or null when it failed.
    private async Task<IEmote?> ReactAsync(SocketUserMessage message, HeartCharacter who, int today)
    {
        var markup = DailyRotation.Pick(BotResponses.HeartReactions[who], today, ReactionSalt ^ Shift(who));
        if (EmoteMarkup.Parse(markup) is not { } emote)
        {
            _logger.LogWarning("Robot heart: reaction failed to parse: {Markup}", markup);
            return null;
        }
        try
        {
            await message.AddReactionAsync(emote);
            return emote;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Robot heart: could not react.");
            return null;
        }
    }

    // A reaction, taken back five to seven seconds later.
    private async Task TakeBackReactionAsync(SocketUserMessage message, HeartCharacter who, int today)
    {
        if (await ReactAsync(message, who, today) is not { } emote) return;
        Later(TimeSpan.FromMilliseconds(Random.Shared.Next(RobotHeart.TakeBackDelayMs.Min, RobotHeart.TakeBackDelayMs.Max)),
            () => message.RemoveReactionAsync(emote, _client.CurrentUser), "take the reaction back");
    }

    // A line, deleted seven to nine seconds later.
    private async Task VanishLineAsync(SocketUserMessage message, HeartCharacter who, int today)
    {
        var line = DailyRotation.Pick(BotResponses.HeartVanishLines[who], today, VanishSalt ^ Shift(who));
        var sent = await BotChat.ReplyWithTypingAsync(message, line, _logger, "robot heart vanish line", AllowedMentions.None);
        if (sent is null) return;
        Later(TimeSpan.FromMilliseconds(Random.Shared.Next(RobotHeart.VanishDelayMs.Min, RobotHeart.VanishDelayMs.Max)),
            () => sent.DeleteAsync(), "delete the line");
    }

    // A slip, edited into a denial about seven seconds later.
    private async Task EditSlipAsync(SocketUserMessage message, HeartCharacter who, int today)
    {
        var (slip, denial) = DailyRotation.Pick(BotResponses.HeartEditLines[who], today, EditSalt ^ Shift(who));
        var sent = await BotChat.ReplyWithTypingAsync(message, slip, _logger, "robot heart slip", AllowedMentions.None);
        if (sent is null) return;
        Later(TimeSpan.FromMilliseconds(Random.Shared.Next(RobotHeart.EditDelayMs.Min, RobotHeart.EditDelayMs.Max)),
            () => sent.ModifyAsync(p => p.Content = denial), "edit the slip");
    }

    private static ulong Shift(HeartCharacter who) => (ulong)who << 8;

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
                _logger.LogWarning(ex, "Robot heart: could not {What}.", what);
            }
        });
}
