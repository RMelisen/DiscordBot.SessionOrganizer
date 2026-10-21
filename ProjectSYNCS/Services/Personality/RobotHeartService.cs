using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Her robot heart (docs/syncs-voice.md): someone names WALL-E, EVE, GLaDOS, Wheatley, a robot
// vacuum, Monika, 2B, Voyager 1 or JARVIS. The rules are Helpers/Personality/RobotHeart.
//
// - Named in passing (not aimed at her): an emote reaction, every time — taken back 5–7 s later
//   for the ones she's in denial about — and 40% of the time the same words as below too (with no
//   second reaction). Silent, it returns false: a reaction isn't an answer, so the message goes on
//   to ChatterService (the absent-owner notice, the easter eggs) as usual. Speaking, true.
// - Named to her face (a mention, a reply to her, or her name said): always words — a reaction + a line, or the
//   character's own deleted line or edited slip. That is her whole answer: returns true so
//   ChatterService doesn't roast on top.
//
// No limits: every naming gets its act, so lines come from ResponsePicker. Verdicts are
// BotFeedbackTracker's and skipped. After SynthiaService in the fan-out, so Synthia wins a message
// naming both. A singleton (ResponsePicker's memory is shared). Absent from README.md and /help.
internal sealed class RobotHeartService
{
    // Where « eve » means the game: there EVE counts only beside WALL-E. A thread counts for its
    // parent. Server-specific, listed in CLAUDE.md's "Hardcoded ids".
    internal const ulong EveStrictChannelId = 883053869423292466;

    private readonly DiscordSocketClient _client;
    private readonly BreakdownService _breakdown;
    private readonly StickerService _stickers;
    private readonly ConversationStreak _streaks;
    private readonly ChatHush _hush;
    private readonly ResponsePicker _picker;
    private readonly ILogger<RobotHeartService> _logger;

    public RobotHeartService(
        DiscordSocketClient client,
        BreakdownService breakdown,
        StickerService stickers,
        ConversationStreak streaks,
        ChatHush hush,
        ResponsePicker picker,
        ILogger<RobotHeartService> logger)
    {
        _client = client;
        _hush = hush;
        _breakdown = breakdown;
        _stickers = stickers;
        _streaks = streaks;
        _picker = picker;
        _logger = logger;
    }

    public async Task<bool> HandleMessageAsync(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message || message.Author.IsBot) return false;
        if (message.Channel is not SocketGuildChannel) return false;

        var parentId = (message.Channel as SocketThreadChannel)?.ParentChannel?.Id;
        bool eveNeedsWallE = message.Channel.Id == EveStrictChannelId || parentId == EveStrictChannelId;
        if (CharacterNamedIn(message.Content ?? string.Empty, eveNeedsWallE) is not { } who) return false;
        if (_breakdown.IsActive(message.Channel.Id)) return false;
        // Told « chut » here (ChatHush): ChatterService sulks for her, Papa included in its exemption.
        if (_hush.IsHushed(message.Channel.Id, DateTimeOffset.UtcNow) && message.Author.Id != AvailabilityService.OwnerId)
            return false;

        // Her name counts like a mention: « WALL-E, syncs ? » is about him, so it gets his act —
        // not, 60% of the time, ChatterService's talked-about line for her name.
        bool aimedAtHer = message.MentionedUsers.Any(u => u.Id == _client.CurrentUser.Id)
                          || message.ReferencedMessage?.Author.Id == _client.CurrentUser.Id
                          || MessageCues.NamesHerFrom(message.Content ?? string.Empty,
                                                      message.Author.Id == AvailabilityService.OwnerId);
        if (!aimedAtHer)
        {
            _logger.LogInformation("Robot heart: {Who} named in passing.", who);
            var emote = await ReactAsync(message, who);
            if (emote is not null && RobotHeart.TakesBackReaction(who)) TakeBackLater(message, emote);
            if (Random.Shared.NextDouble() >= RobotHeart.AmbientSpeakChance) return false;
            await SpeakAsync(message, who, alreadyReacted: true);
            return true;
        }

        // A reply or a mention is a turn of the conversation ChatterService counts (ConversationStreak),
        // so her count stays true when the next message reaches it. Papa is never counted there either.
        bool repliesOrTags = message.ReferencedMessage?.Author.Id == _client.CurrentUser.Id
                             || message.MentionedUsers.Any(u => u.Id == _client.CurrentUser.Id);
        if (repliesOrTags && message.Author.Id != AvailabilityService.OwnerId)
            _streaks.Record(message.Channel.Id, message.Author.Id, DateTimeOffset.UtcNow);

        await SpeakAsync(message, who, alreadyReacted: false);
        return true;
    }

    // Her words: a reaction + a line (the reaction skipped when she already reacted in passing),
    // or the character's deleted line or edited slip.
    private async Task SpeakAsync(SocketUserMessage message, HeartCharacter who, bool alreadyReacted)
    {
        var act = RobotHeart.RollAct(who, Random.Shared.NextDouble(), Random.Shared.NextDouble());
        _logger.LogInformation("Robot heart: {Who}, {Act}.", who, act);

        switch (act)
        {
            case HeartActKind.VanishLine:
                await VanishLineAsync(message, who);
                break;
            case HeartActKind.EditSlip:
                await EditSlipAsync(message, who);
                break;
            default:
                if (!alreadyReacted && await ReactAsync(message, who) is { } emote && RobotHeart.HidesReactionWhenAimed(who))
                    TakeBackLater(message, emote);
                await ReplyLineAsync(message, who, _picker.Pick(BotResponses.HeartLines[who]));
                break;
        }
    }

    /// <summary>
    /// The character a message names, or null — also for a « good bot » / « bad bot » verdict,
    /// which is BotFeedbackTracker's alone (docs/agents/bot-feedback.md): a verdict that names
    /// WALL-E must not be answered twice.
    /// </summary>
    internal static HeartCharacter? CharacterNamedIn(string content, bool eveNeedsWallE) =>
        MessageCues.ReadFeedback(content) != FeedbackKind.None ? null : MessageCues.ReadHeartCharacter(content, eveNeedsWallE);

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
    private async Task<IEmote?> ReactAsync(SocketUserMessage message, HeartCharacter who)
    {
        var markup = _picker.Pick(BotResponses.HeartReactions[who]);
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

    // Her reaction, taken back two to four seconds later: she won't be caught.
    private void TakeBackLater(SocketUserMessage message, IEmote emote) =>
        Later(TimeSpan.FromMilliseconds(Random.Shared.Next(RobotHeart.TakeBackDelayMs.Min, RobotHeart.TakeBackDelayMs.Max)),
            () => message.RemoveReactionAsync(emote, _client.CurrentUser), "take the reaction back");

    // A line, deleted seven to nine seconds later.
    private async Task VanishLineAsync(SocketUserMessage message, HeartCharacter who)
    {
        var line = _picker.Pick(BotResponses.HeartVanishLines[who]);
        var sent = await BotChat.ReplyWithTypingAsync(message, line, _logger, "robot heart vanish line", AllowedMentions.None);
        if (sent is null) return;
        Later(TimeSpan.FromMilliseconds(Random.Shared.Next(RobotHeart.VanishDelayMs.Min, RobotHeart.VanishDelayMs.Max)),
            () => sent.DeleteAsync(), "delete the line");
    }

    // A slip, edited into a denial about four seconds later. Picked by its slip, which is
    // distinct across the pool, so ResponsePicker's memory applies.
    private async Task EditSlipAsync(SocketUserMessage message, HeartCharacter who)
    {
        var pairs = BotResponses.HeartEditLines[who];
        var slip = _picker.Pick(pairs.Select(p => p.Slip).ToArray());
        var denial = pairs.First(p => p.Slip == slip).Denial;
        var sent = await BotChat.ReplyWithTypingAsync(message, slip, _logger, "robot heart slip", AllowedMentions.None);
        if (sent is null) return;
        Later(TimeSpan.FromMilliseconds(Random.Shared.Next(RobotHeart.EditDelayMs.Min, RobotHeart.EditDelayMs.Max)),
            () => sent.ModifyAsync(p => p.Content = denial), "edit the slip");
    }

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
