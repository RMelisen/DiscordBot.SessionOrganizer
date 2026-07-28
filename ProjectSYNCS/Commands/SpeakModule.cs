using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// Lets the owner speak through the bot on his own initiative — as opposed to
// ChatterService's relay, which answers a mention he received while absent.
// /tell posts into a channel (optionally as a reply to a linked message), /dm
// sends someone a private message. In both, the text goes out as the bot's own
// words by default; "announce" opts into a herald line naming the owner.
public class SpeakModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<SpeakModule> _logger;
    private readonly ResponsePicker _picker;

    // Discord caps a message at 2000 characters; leave room for the herald line
    // and the blockquote markers.
    private const int MaxMessageLength = 1500;

    // The tail of a Discord message link, whatever the host (discord.com,
    // canary/ptb subdomains, the old discordapp.com): .../channels/{guild}/{channel}/{message}
    private static readonly Regex _messageLinkRegex =
        new(@"channels/(\d+)/(\d+)/(\d+)", RegexOptions.Compiled);

    public SpeakModule(ILogger<SpeakModule> logger, ResponsePicker picker)
    {
        _logger = logger;
        _picker = picker;
    }

    [SlashCommand("tell", "Faire dire un message au bot")]
    public async Task SpeakAsync(
        [Summary("message", "Le message à faire dire au bot")]
        string message,
        [Summary("channel", "Salon de destination (par défaut : le salon actuel)")]
        ITextChannel? channel = null,
        [Summary("announce", "Faire une annonce")]
        bool announce = false,
        [Summary("respond_to", "Lien du message auquel répondre (clic droit → Copier le lien du message)")]
        string? respond_to = null)
    {
        if (Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
            return;
        }

        // Resolving the link and posting are several round-trips; defer so the
        // interaction can't time out. Every answer below is a followup.
        await DeferAsync(ephemeral: true);

        var (text, textError) = PrepareText(message);
        if (text is null)
        {
            await FollowupAsync(textError, ephemeral: true);
            return;
        }

        // A link decides the destination by itself: it carries the channel.
        ITextChannel? target;
        IMessage? repliedTo = null;

        if (!string.IsNullOrWhiteSpace(respond_to))
        {
            var resolved = await ResolveLinkedMessageAsync(respond_to, channel);
            if (resolved.Error is not null)
            {
                await FollowupAsync(resolved.Error, ephemeral: true);
                return;
            }
            target = resolved.Channel;
            repliedTo = resolved.Message;
        }
        else
        {
            target = channel ?? Context.Channel as ITextChannel;
        }

        if (target is null)
        {
            await FollowupAsync("Cette commande ne fonctionne que dans un salon textuel.", ephemeral: true);
            return;
        }

        var content = BuildContent(text, announce, target.Id);

        try
        {
            await target.SendMessageAsync(
                content,
                // Honour users the owner mentioned, but never @everyone/@here or a
                // role — the bot must not become a way to mass-ping. When replying,
                // ping the person being answered.
                allowedMentions: new AllowedMentions(AllowedMentionTypes.Users)
                {
                    MentionRepliedUser = repliedTo is not null
                },
                messageReference: repliedTo is null
                    ? null
                    : new MessageReference(
                        messageId: repliedTo.Id,
                        channelId: target.Id,
                        guildId: target.GuildId));

            _logger.LogInformation(
                "Owner spoke through the bot in channel {ChannelId} (heralded: {Heralded}, replying: {Replying}).",
                target.Id, announce, repliedTo is not null);

            await FollowupAsync(
                repliedTo is null
                    ? $"Envoyé dans {target.Mention}. ✅"
                    : $"Réponse envoyée à **{repliedTo.Author.Username}** dans {target.Mention}. ✅",
                ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to speak through the bot in channel {ChannelId}.", target.Id);
            await FollowupAsync(
                $"Impossible d'écrire dans {target.Mention} — je n'ai peut-être pas la permission. ❌",
                ephemeral: true);
        }
    }

    [SlashCommand("dm", "Faire envoyer un message privé par le bot")]
    public async Task DirectMessageAsync(
        [Summary("user", "La personne à qui envoyer le message")]
        IUser user,
        [Summary("message", "Le message à faire envoyer au bot")]
        string message,
        [Summary("announce", "Faire une annonce")]
        bool announce = false)
    {
        if (Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        var (text, textError) = PrepareText(message);
        if (text is null)
        {
            await FollowupAsync(textError, ephemeral: true);
            return;
        }

        if (user.IsBot)
        {
            await FollowupAsync("Je ne peux pas envoyer de message privé à un bot. 🤖", ephemeral: true);
            return;
        }

        try
        {
            var dm = await user.CreateDMChannelAsync();
            await dm.SendMessageAsync(
                BuildContent(text, announce, user.Id),
                // Same policy as everywhere else: no @everyone/@here, no roles.
                allowedMentions: new AllowedMentions(AllowedMentionTypes.Users));

            _logger.LogInformation("Owner sent a DM through the bot to {UserId} (heralded: {Heralded}).",
                user.Id, announce);

            await FollowupAsync($"Envoyé en privé à **{user.Username}**. ✅", ephemeral: true);
        }
        catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
        {
            await FollowupAsync(
                $"**{user.Username}** n'accepte pas les messages privés du serveur. ❌", ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send a DM through the bot to {UserId}.", user.Id);
            await FollowupAsync($"Impossible d'envoyer le message à **{user.Username}**. ❌", ephemeral: true);
        }
    }

    // Normalises the owner's raw input, or explains why it can't be sent.
    // Slash-command input is single-line, so a literal \n stands for a break.
    private static (string? Text, string? Error) PrepareText(string message)
    {
        var text = message.Replace("\\n", "\n").Trim();

        if (text.Length == 0)
            return (null, "Ton message est vide, je n'ai rien à dire. ✍️");

        if (text.Length > MaxMessageLength)
            return (null, $"Ton message fait {text.Length} caractères, le maximum est {MaxMessageLength}. ✂️");

        return (text, null);
    }

    // Either a herald line naming the owner, or — by default — pure
    // ventriloquism: the bot's own voice, nothing pointing back to him.
    // bucketId only groups the herald history (destination channel, or recipient
    // for a DM), so the same one doesn't come up twice in a row.
    private string BuildContent(string text, bool announce, ulong bucketId)
    {
        if (!announce) return text;

        var ownerName = (Context.User as SocketGuildUser)?.Nickname
            ?? Context.User.GlobalName
            ?? Context.User.Username;
        var herald = string.Format(
            _picker.Pick(bucketId, BotResponses.OwnerAnnouncementHeralds),
            ownerName);

        return $"{herald}\n{MessageFormat.Quote(text, MaxMessageLength)}";
    }

    // Turns a pasted message link into the channel and message it points at.
    // Returns a ready-to-send French error instead of throwing, so the caller can
    // just forward it to the owner.
    private async Task<(ITextChannel? Channel, IMessage? Message, string? Error)> ResolveLinkedMessageAsync(
        string link, ITextChannel? requestedChannel)
    {
        var match = _messageLinkRegex.Match(link);
        if (!match.Success)
        {
            return (null, null,
                "Je n'ai pas reconnu ce lien. Fais un clic droit sur le message → " +
                "**Copier le lien du message**. ❌");
        }

        var guildId = ulong.Parse(match.Groups[1].Value);
        var channelId = ulong.Parse(match.Groups[2].Value);
        var messageId = ulong.Parse(match.Groups[3].Value);

        var guild = Context.Client.GetGuild(guildId);
        if (guild is null)
            return (null, null, "Ce lien pointe vers un serveur où je ne suis pas. ❌");

        var channel = guild.GetTextChannel(channelId);
        if (channel is null)
            return (null, null, "Ce lien pointe vers un salon que je ne vois pas. ❌");

        // The link already names a channel; a conflicting "channel" argument is a
        // mistake worth surfacing rather than silently resolving one way.
        if (requestedChannel is not null && requestedChannel.Id != channel.Id)
        {
            return (null, null,
                $"Le lien pointe vers {channel.Mention}, mais tu as choisi {requestedChannel.Mention}. " +
                "Enlève l'un des deux. ❌");
        }

        var original = await channel.GetMessageAsync(messageId);
        if (original is null)
            return (null, null, "Je ne retrouve pas ce message — il a peut-être été supprimé. ❌");

        return (channel, original, null);
    }
}
