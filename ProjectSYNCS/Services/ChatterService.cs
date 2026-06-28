using System.Globalization;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// The bot's "personality": decides how to react to user messages — replies to
// the bot, @mentions, level-up announcements — and occasionally triggers the
// breakdown easter egg. Pure response logic; the canned text lives in
// BotResponses and the breakdown playback in BreakdownService.
internal sealed class ChatterService
{
    private readonly DiscordSocketClient _client;
    private readonly BreakdownService _breakdown;
    private readonly ILogger<ChatterService> _logger;

    // Rodhengard, the owner: gets compliments instead of roasts.
    private const ulong OwnerId = 345917214966415362;

    // The level-up bot. When it announces someone passing a level, we cheer.
    private const ulong LevelUpBotId = 437808476106784770;
    private const string LevelUpPhrase = "tu viens de passer au niveau";

    // 1-in-1000 chance of the breakdown; 1-in-200 of a pop-culture reference.
    private const double BreakdownChance = 0.001;
    private const double ReferenceChance = 0.008;

    // Secret passphrase: the owner replying with exactly this forces a breakdown.
    private const string BreakdownPassphrase = "The cake is a lie.";

    // Pulls the level number out of the announcement: the first run of digits
    // that follows the level-up phrase (skips any markdown like ** in between).
    private static readonly Regex _levelNumberRegex =
        new(LevelUpPhrase + @"\D*(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ChatterService(
        DiscordSocketClient client,
        BreakdownService breakdown,
        ILogger<ChatterService> logger)
    {
        _client = client;
        _breakdown = breakdown;
        _logger = logger;
    }

    public async Task HandleMessageAsync(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message) return;

        // Bots are ignored, with one exception: the level-up bot's "passage de
        // niveau" announcement, which we congratulate.
        if (message.Author.IsBot)
        {
            await HandleLevelUpAsync(message);
            return;
        }

        // Established behaviour: a reply to one of the bot's own messages gets a comeback.
        if (message.ReferencedMessage?.Author.Id == _client.CurrentUser.Id)
        {
            await HandleReplyToBotAsync(message);
            return;
        }

        // A normal message that @mentions the bot. The owner can summon the bot
        // to roast whoever they're replying to; anyone else just gets a confused
        // one-liner (or a greeting back).
        if (message.MentionedUsers.Any(u => u.Id == _client.CurrentUser.Id))
        {
            await HandleMentionAsync(message);
        }
    }

    // Cheers when the level-up bot announces someone reaching a new level.
    // Posts a plain message in the channel (no reply, no ping).
    private async Task HandleLevelUpAsync(SocketUserMessage message)
    {
        if (message.Author.Id != LevelUpBotId) return;

        var content = message.Content ?? string.Empty;
        var match = _levelNumberRegex.Match(content);
        if (!match.Success) return;

        // Easter egg: level 67 gets the meme instead of a normal cheer.
        var cheer = match.Groups[1].Value == "67"
            ? "SIX SEVEEEN"
            : BotResponses.LevelUpCheers[Random.Shared.Next(BotResponses.LevelUpCheers.Length)];
        try
        {
            await message.Channel.SendMessageAsync(cheer);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send level-up cheer in channel {ChannelId}.", message.Channel.Id);
        }
    }

    // Handles a message that @mentions the bot (but isn't a reply to the bot).
    private async Task HandleMentionAsync(SocketUserMessage message)
    {
        // Don't let anyone interrupt an in-progress breakdown in this channel.
        if (_breakdown.IsActive(message.Channel.Id)) return;

        var weekday = CurrentWeekday();

        // Rescue: the owner replies to someone and tags the bot -> roast that
        // someone. The target must be a real other person (not the bot, not the
        // owner themselves).
        if (message.Author.Id == OwnerId
            && message.ReferencedMessage is SocketUserMessage target
            && target.Author.Id != _client.CurrentUser.Id
            && target.Author.Id != OwnerId
            && !target.Author.IsBot)
        {
            var targetName = ResolveName(target.Author);
            _logger.LogInformation("Owner summoned a rescue roast against {Name}.", targetName);
            var roast = string.Format(
                BotResponses.RescueRoasts[Random.Shared.Next(BotResponses.RescueRoasts.Length)], targetName, weekday);
            try
            {
                // Reply to the target's own message so the roast is clearly aimed
                // at them (and pings them).
                await target.ReplyAsync(roast);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send rescue roast in channel {ChannelId}.", message.Channel.Id);
            }
            return;
        }

        // Owner tagging the bot with no one to rescue: just greet him.
        if (message.Author.Id == OwnerId)
        {
            _logger.LogInformation("Owner mentioned the bot — greeting him.");
            var greeting = BotResponses.OwnerGreetings[Random.Shared.Next(BotResponses.OwnerGreetings.Length)];
            try
            {
                await message.ReplyAsync(greeting);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send owner greeting in channel {ChannelId}.", message.Channel.Id);
            }
            return;
        }

        // Anyone else: a confused one-liner — or, rarely, the breakdown. This is a
        // second entry point for the easter egg, opening on a cut-off "Tu veux qu-"
        // instead of the reply path's "C'est bien {0} on est cont-".
        var name = ResolveName(message.Author);

        if (Random.Shared.NextDouble() < BreakdownChance && _breakdown.TryBegin(message.Channel.Id))
        {
            _logger.LogInformation("Easter egg triggered via mention: consciousness breakdown.");
            var realName = BotResponses.RealNameFor(message.Author.Id, name);
            await _breakdown.PlayAsync(message, name, realName, intro: "Tu veux qu-");
            return;
        }

        // A mention that greets the bot gets greeted back — a second entry point
        // for the greeting, alongside replying. A mean word cancels it. Otherwise
        // the bot just answers with a confused one-liner.
        var content = message.Content ?? string.Empty;
        var pool = !MessageCues.IsMean(content) && MessageCues.IsGreeting(content)
            ? BotResponses.Greetings
            : BotResponses.Interrogations;

        _logger.LogInformation("{Name} mentioned the bot.", name);
        var line = string.Format(pool[Random.Shared.Next(pool.Length)], name, weekday);
        try
        {
            await message.ReplyAsync(line);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send mention reply in channel {ChannelId}.", message.Channel.Id);
        }
    }

    private async Task HandleReplyToBotAsync(SocketUserMessage message)
    {
        // Don't let anyone interrupt an in-progress breakdown in this channel.
        if (_breakdown.IsActive(message.Channel.Id)) return;

        var name = ResolveName(message.Author);
        _logger.LogInformation("{Name} replied to the bot.", name);

        // Read the message text (requires the MessageContent intent) to detect
        // kind words or a greeting and answer in kind. A mean word anywhere in the
        // message cancels the nice/greeting treatment — we roast instead.
        var content = message.Content ?? string.Empty;
        var mean = MessageCues.IsMean(content);
        var nice = !mean && MessageCues.IsNice(content);
        var greeting = !mean && MessageCues.IsGreeting(content);

        // Secret owner trigger: replying with the passphrase forces the breakdown,
        // bypassing the random roll and the cooldown.
        var secretTrigger = message.Author.Id == OwnerId
            && content.Trim() == BreakdownPassphrase;

        if ((secretTrigger || Random.Shared.NextDouble() < BreakdownChance)
            && _breakdown.TryBegin(message.Channel.Id, ignoreCooldown: secretTrigger))
        {
            _logger.LogInformation("Easter egg triggered: consciousness breakdown.");
            // Intro uses the pseudo; the breakdown reveal uses the real name when known.
            // A kind message opens with a glitching thank-you instead of a roast.
            var realName = BotResponses.RealNameFor(message.Author.Id, name);
            var intro = secretTrigger ? BotResponses.BreakdownIntroCake
                : nice ? BotResponses.BreakdownIntroNice
                : BotResponses.BreakdownIntroRoast;
            await _breakdown.PlayAsync(message, name, realName, intro);
            return;
        }

        string[] pool;
        if (nice)
        {
            pool = BotResponses.NiceReplies;
        }
        else if (greeting)
        {
            pool = BotResponses.Greetings;
        }
        else if (Random.Shared.NextDouble() < ReferenceChance)
        {
            // Rarer easter egg: a pop-culture reference, for everyone.
            pool = BotResponses.ReferenceComebacks;
        }
        else
        {
            pool = message.Author.Id == OwnerId ? BotResponses.OwnerComebacks : BotResponses.Comebacks;
            // Fold in this person's custom lines twice, so each has double weight.
            if (BotResponses.PersonalComebacks.TryGetValue(message.Author.Id, out var personal))
                pool = pool.Concat(personal).Concat(personal).ToArray();
        }
        var comeback = string.Format(pool[Random.Shared.Next(pool.Length)], name, CurrentWeekday());
        try
        {
            await message.ReplyAsync(comeback);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send reply comeback in channel {ChannelId}.", message.Channel.Id);
        }
    }

    // The current weekday name in French, for the {1} format placeholder.
    private static string CurrentWeekday() =>
        AppTime.Now.ToString("dddd", CultureInfo.GetCultureInfo("fr-FR"));

    // Resolves the friendliest display name available: server nickname, then
    // global display name, then username.
    private static string ResolveName(IUser user) =>
        (user as SocketGuildUser)?.Nickname ?? user.GlobalName ?? user.Username;
}
