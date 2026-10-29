using System.Globalization;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

internal sealed partial class ChatterService
{
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
            _picker.Pick(BotResponses.OwnerAbsentNotices),
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
                _picker.Pick(BotResponses.OwnerReplyHeralds),
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
    // Someone threatening to switch her off. The one thing she reacts to more
    // strongly than an insult, and the only behaviour where the favouritism inverts:
    // being able to actually carry the threat out makes it *worse*, not gentler.
    //
    // Three tiers, by how credible the threat is and whether she can appeal to a
    // relationship:
    //   Rodhengard — wrote her, could unplug her, nothing to bargain with → terror.
    //   Tata       — family, but holds the server permissions → pleading and
    //                bargaining, the only tier where she still thinks talking works.
    //   Anyone else — no permissions, pure bluff → fury, and she says so.
    //
    // Checked only on messages aimed at her (reply or mention), never ambiently: the
    // vocabulary overlaps with ordinary talk about restarting a game server, and
    // "faut couper le serveur" said about Minecraft must not terrify her.
    private async Task<bool> TryHandleShutdownThreatAsync(SocketUserMessage message, ChatPath path)
    {
        // The by-name path already matched its own wording (ThreatensShutdownByName).
        if (path != ChatPath.Name && !MessageCues.ThreatensShutdown(message.Content ?? string.Empty)) return false;

        var name = ResolveName(message.Author);

        var (pool, who) = message.Author.Id switch
        {
            OwnerId => (BotResponses.ShutdownThreatOwner, "terrified (owner)"),
            BotResponses.TataId => (BotResponses.ShutdownThreatTata, "bargaining (Tata)"),
            _ => (BotResponses.ShutdownThreatReplies, "furious"),
        };

        _logger.LogInformation("{Name} threatened to shut the bot down — {Reaction}.", name, who);

        var line = string.Format(_picker.Pick(pool), name, CurrentWeekday());
        Count(message, path, AnswerKinds.Shutdown);
        if (await ReplyWithTypingAsync(message, line, "shutdown-threat reply") is not null)
            Award(message, MemberBadges.Keys.Threat);
        return true;
    }

    private async Task<bool> TryCorrectMistakenIdentityAsync(SocketUserMessage message, ChatPath path)
    {
        if (!MessageCues.IsMistakenIdentity(message.Content ?? string.Empty)) return false;

        var name = ResolveName(message.Author);
        _logger.LogInformation("{Name} called the bot 'Inabot' — correcting them.", name);
        var line = string.Format(
            _picker.Pick(BotResponses.MistakenIdentityReplies),
            name, CurrentWeekday());
        Count(message, path, AnswerKinds.Inabot);
        await ReplyWithTypingAsync(message, line, "mistaken-identity reply");
        return true;
    }
}
