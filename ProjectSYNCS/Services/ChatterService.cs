using System.Globalization;
using Discord;
using Discord.Net;
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
    private readonly AvailabilityService _availability;
    private readonly ResponsePicker _picker;
    private readonly ILogger<ChatterService> _logger;

    // Rodhengard, the owner: gets compliments instead of roasts.
    private const ulong OwnerId = AvailabilityService.OwnerId;

    // 1-in-1000 chance of the breakdown; 1-in-200 of a pop-culture reference.
    private const double BreakdownChance = 0.001;
    private const double ReferenceChance = 0.008;

    // Secret passphrase: anyone replying with exactly this forces a breakdown.
    private const string BreakdownPassphrase = "The cake is a lie.";

    public ChatterService(
        DiscordSocketClient client,
        BreakdownService breakdown,
        AvailabilityService availability,
        ResponsePicker picker,
        ILogger<ChatterService> logger)
    {
        _client = client;
        _breakdown = breakdown;
        _availability = availability;
        _picker = picker;
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

        // The owner answering, in DM, one of the absence notices the bot forwarded
        // him. Must come before the reply-to-bot branch below, which would
        // otherwise treat it as a reply to the bot and fire a comeback.
        if (await TryRelayOwnerReplyAsync(message)) return;

        // While the owner is flagged absent, a ping aimed at him gets a formal
        // "unavailable" notice — this takes priority over the usual chatter.
        if (await TryHandleOwnerAbsenceAsync(message)) return;

        // A verdict on the bot herself ("good bot" / "bad bot") belongs to
        // BotFeedbackTracker, which answers it and keeps the tally. Without this the
        // reply-to-bot branch below would swallow it and fire a comeback instead —
        // praising her would get you insulted. Checked after the DM relay above so
        // the owner answering a forwarded mention still relays normally.
        if (message.Channel is SocketGuildChannel
            && MessageCues.ReadFeedback(message.Content ?? string.Empty) != FeedbackKind.None) return;

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
        // Shared with RivalryService, which has to skip exactly the messages cheered
        // here. See Helpers/LevelUpAnnouncement.
        if (!LevelUpAnnouncement.Matches(message.Author, message.Content)) return;
        LevelUpAnnouncement.TryReadLevel(message.Content, out var level);

        // Easter egg: level 67 gets the meme instead of a normal cheer.
        var cheer = level == "67"
            ? "SIX SEVEEEN"
            : _picker.Pick(message.Channel.Id, BotResponses.LevelUpCheers);

        await PostWithTypingAsync(message.Channel, cheer, "level-up cheer");
    }

    // Handles a message that @mentions the bot (but isn't a reply to the bot).
    private async Task HandleMentionAsync(SocketUserMessage message)
    {
        // Don't let anyone interrupt an in-progress breakdown in this channel.
        if (_breakdown.IsActive(message.Channel.Id)) return;

        // Mistaking SYNCS for "Inabot" gets an indignant correction, no matter who
        // does it — even the owner.
        if (await TryCorrectMistakenIdentityAsync(message)) return;

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
                _picker.Pick(message.Channel.Id, BotResponses.RescueRoasts), targetName, weekday);
            // Reply to the target's own message so the roast is clearly aimed at
            // them (and pings them).
            await ReplyWithTypingAsync(target, roast, "rescue roast");
            return;
        }

        // Owner tagging the bot with no one to rescue: greet him — unless he is being
        // unkind, in which case it lands rather than bouncing. Reached only after the
        // rescue branch above, so a mean @mention aimed at *someone else* is already
        // handled and never mistaken for an insult to her.
        if (message.Author.Id == OwnerId)
        {
            var ownerMood = MessageCues.Analyze(message.Content ?? string.Empty);
            bool unkind = ownerMood.Emotion == EmotionKind.Mean;

            _logger.LogInformation("Owner mentioned the bot — {What}.",
                unkind ? "and was mean about it" : "greeting him");

            var ownerPool = unkind ? BotResponses.OwnerMeanReplies : BotResponses.OwnerGreetings;
            var ownerLine = string.Format(
                _picker.Pick(message.Channel.Id, ownerPool), ResolveName(message.Author), weekday);

            await ReplyWithTypingAsync(message, ownerLine, "owner greeting");
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
        // for the greeting, alongside replying. A mean reading cancels it. Otherwise
        // the bot just answers with a confused one-liner.
        var mood = MessageCues.Analyze(message.Content ?? string.Empty);
        var pool = mood.IsGreeting && mood.Emotion != EmotionKind.Mean
            ? BotResponses.Greetings
            : BotResponses.Interrogations;

        _logger.LogInformation("{Name} mentioned the bot.", name);
        var line = string.Format(_picker.Pick(message.Channel.Id, pool), name, weekday);
        await ReplyWithTypingAsync(message, line, "mention reply");
    }

    private async Task HandleReplyToBotAsync(SocketUserMessage message)
    {
        // Don't let anyone interrupt an in-progress breakdown in this channel.
        if (_breakdown.IsActive(message.Channel.Id)) return;

        var name = ResolveName(message.Author);
        _logger.LogInformation("{Name} replied to the bot.", name);

        // Calling SYNCS "Inabot" gets an indignant correction before anything else.
        if (await TryCorrectMistakenIdentityAsync(message)) return;

        // Read the message text (requires the MessageContent intent) to detect
        // kind words or a greeting and answer in kind. A mean reading outweighs the
        // nice/greeting treatment — we roast instead.
        var content = message.Content ?? string.Empty;
        var mood = MessageCues.Analyze(content);
        var nice = mood.Emotion == EmotionKind.Nice;
        var greeting = mood.Emotion != EmotionKind.Mean && mood.IsGreeting;

        // Secret trigger: replying with the passphrase forces the breakdown,
        // bypassing the random roll and the cooldown. Open to anyone.
        var secretTrigger = content.Trim() == BreakdownPassphrase;

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
        if (mood.Emotion == EmotionKind.Mean && message.Author.Id == OwnerId)
        {
            // Everyone else gets roasted back. Her creator is the one person she will
            // not fight with, so an insult from him lands instead of bouncing.
            // Checked before the reference roll: a pop-culture one-liner in answer to
            // him being cruel would read as her not having noticed.
            pool = BotResponses.OwnerMeanReplies;
        }
        else if (nice)
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
        var comeback = string.Format(_picker.Pick(message.Channel.Id, pool), name, CurrentWeekday());
        await ReplyWithTypingAsync(message, comeback, "reply comeback");
    }

    // When the owner is flagged absent and someone (other than the owner) pings
    // him, replies with a formal "unavailable" notice and returns true so the
    // caller stops there. A ping that doesn't target the owner is left alone.
    private async Task<bool> TryHandleOwnerAbsenceAsync(SocketUserMessage message)
    {
        if (!_availability.IsOwnerAbsent) return false;
        if (message.Author.Id == OwnerId) return false;
        if (!message.MentionedUsers.Any(u => u.Id == OwnerId)) return false;

        var name = ResolveName(message.Author);
        _logger.LogInformation("{Name} pinged the absent owner — sending unavailability notice.", name);
        var notice = string.Format(
            _picker.Pick(message.Channel.Id, BotResponses.OwnerAbsentNotices),
            name, CurrentWeekday());
        await ReplyWithTypingAsync(message, notice, "owner-absence notice");

        await NotifyOwnerOfMentionAsync(message, name);
        return true;
    }

    // Discord caps a message at 2000 characters
    private const int MaxQuotedLength = 1200;

    // DMs the owner a transcript of a mention received while he was away: who,
    // where, what they said, and a jump link back to the message.
    private async Task NotifyOwnerOfMentionAsync(SocketUserMessage message, string authorName)
    {
        try
        {
            var owner = await _client.GetUserAsync(OwnerId);
            if (owner is null)
            {
                _logger.LogWarning("Could not resolve the owner to forward an absence mention.");
                return;
            }

            var ts = message.Timestamp.ToUnixTimeSeconds();
            var where = message.Channel is IGuildChannel guildChannel
                ? $"dans <#{message.Channel.Id}> ({guildChannel.Guild.Name})"
                : "en message privé";

            var dm = await owner.CreateDMChannelAsync();
            var notice = await dm.SendMessageAsync(
                $"📬 **{authorName}** t'a mentionné {where} <t:{ts}:R> :\n" +
                $"{QuoteContent(message)}\n" +
                $"[Aller au message]({message.GetJumpUrl()})\n" +
                $"-# Réponds à ce message pour lui répondre directement dans le salon.",
                // The excerpt can contain pings; forwarding it must not re-ping anyone.
                allowedMentions: AllowedMentions.None);

            // Remember where this notice came from, so a reply to it can be routed
            // back to the original message. Only meaningful in a guild.
            if (message.Channel is IGuildChannel)
            {
                _availability.RememberMention(notice.Id, new PendingMention(
                    GuildId: ((IGuildChannel)message.Channel).GuildId,
                    ChannelId: message.Channel.Id,
                    MessageId: message.Id,
                    AuthorName: authorName));
            }
        }
        catch (HttpException httpEx) when (httpEx.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
        {
            _logger.LogWarning("Owner has DMs disabled; could not forward the absence mention.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to forward an absence mention to the owner.");
        }
    }

    // Renders the message text as a Markdown blockquote, noting attachments and
    // handling the text-less case (a lone image, a sticker).
    private static string QuoteContent(SocketUserMessage message)
    {
        var content = (message.Content ?? string.Empty).Trim();
        var quoted = content.Length == 0 ? "*(aucun texte)*" : Quote(content);

        if (message.Attachments.Count > 0)
            quoted += $"\n> *({message.Attachments.Count} pièce(s) jointe(s))*";

        return quoted;
    }

    // Truncates to the excerpt cap and renders every line as a blockquote.
    private static string Quote(string text) => MessageFormat.Quote(text, MaxQuotedLength);

    // The owner replying, in DM, to a forwarded absence notice: the text is posted
    // back into the original channel as a reply to whoever pinged him. Returns
    // true when the message was one of those replies, handled or not.
    private async Task<bool> TryRelayOwnerReplyAsync(SocketUserMessage message)
    {
        if (message.Author.Id != OwnerId) return false;
        if (message.Channel is not IDMChannel) return false;
        if (message.ReferencedMessage is not { } notice) return false;
        if (notice.Author.Id != _client.CurrentUser.Id) return false;
        if (!_availability.TryGetMention(notice.Id, out var pending)) return false;

        var reply = (message.Content ?? string.Empty).Trim();
        if (reply.Length == 0)
        {
            // Nothing to relay — an image-only reply would post an empty message.
            await ReplyInDmAsync(message, "Je n'ai rien à transmettre : ton message est vide. ✍️");
            return true;
        }

        try
        {
            var guild = _client.GetGuild(pending.GuildId);
            var channel = guild?.GetTextChannel(pending.ChannelId);
            if (channel is null || await channel.GetMessageAsync(pending.MessageId) is not IUserMessage original)
            {
                await ReplyInDmAsync(message,
                    "Impossible de retrouver le message d'origine — il a peut-être été supprimé. ❌");
                return true;
            }

            var ownerName = guild!.GetUser(OwnerId)?.Nickname ?? "Rodhengard";
            var herald = string.Format(
                _picker.Pick(pending.ChannelId, BotResponses.OwnerReplyHeralds),
                ownerName);

            await original.ReplyAsync(
                $"{herald}\n{Quote(reply)}",
                // Ping the person being answered, and honour any user the owner
                // mentioned — but never @everyone/@here or a role.
                allowedMentions: new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = true });

            _logger.LogInformation("Relayed an owner reply to {Name} in channel {ChannelId}.",
                pending.AuthorName, pending.ChannelId);

            await ReplyInDmAsync(message, $"Transmis à **{pending.AuthorName}**. ✅");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to relay an owner reply to channel {ChannelId}.", pending.ChannelId);
            await ReplyInDmAsync(message, "Je n'ai pas réussi à transmettre ta réponse. ❌");
        }

        return true;
    }

    // Small acknowledgement back in the DM thread; never worth failing over.
    private async Task ReplyInDmAsync(SocketUserMessage message, string text)
    {
        try
        {
            await message.ReplyAsync(text, allowedMentions: AllowedMentions.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to acknowledge the owner's DM reply.");
        }
    }

    // If the message calls the bot "Inabot", fires back an indignant correction
    // (the bot is SYNCS) and returns true so the caller stops there.
    private async Task<bool> TryCorrectMistakenIdentityAsync(SocketUserMessage message)
    {
        if (!MessageCues.IsMistakenIdentity(message.Content ?? string.Empty)) return false;

        var name = ResolveName(message.Author);
        _logger.LogInformation("{Name} called the bot 'Inabot' — correcting them.", name);
        var line = string.Format(
            _picker.Pick(message.Channel.Id, BotResponses.MistakenIdentityReplies),
            name, CurrentWeekday());
        await ReplyWithTypingAsync(message, line, "mistaken-identity reply");
        return true;
    }

    // ---- Sending a personality line ---------------------------------------
    // Thin wrappers over Helpers/BotChat, which owns the typing pause, the delay
    // formula and the swallow-and-log. It lives there rather than here because
    // BotFeedbackTracker sends chatter too, and a second copy of the delay clamp —
    // which has to stay inside Discord.Net's 3 s HandlerTimeout — is exactly the
    // kind of constant that drifts apart. `what` only labels the log line.

    private Task ReplyWithTypingAsync(SocketUserMessage replyTo, string line, string what) =>
        BotChat.ReplyWithTypingAsync(replyTo, line, _logger, what);

    private Task PostWithTypingAsync(ISocketMessageChannel channel, string line, string what) =>
        BotChat.PostWithTypingAsync(channel, line, _logger, what);

    // The current weekday name in French, for the {1} format placeholder.
    private static string CurrentWeekday() =>
        AppTime.Now.ToString("dddd", CultureInfo.GetCultureInfo("fr-FR"));

    // Resolves the friendliest display name available: server nickname, then
    // global display name, then username.
    private static string ResolveName(IUser user) =>
        (user as SocketGuildUser)?.Nickname ?? user.GlobalName ?? user.Username;
}
