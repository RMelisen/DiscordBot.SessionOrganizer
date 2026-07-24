using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// Lets the owner speak through the bot on his own initiative — as opposed to
// ChatterService's relay, which answers a mention he received while absent.
// The message is either heralded (the channel sees it comes from him) or posted
// anonymously as the bot's own words.
public class SpeakModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<SpeakModule> _logger;

    // Discord caps a message at 2000 characters; leave room for the herald line
    // and the blockquote markers.
    private const int MaxMessageLength = 1500;

    public SpeakModule(ILogger<SpeakModule> logger)
    {
        _logger = logger;
    }

    [SlashCommand("tell", "Faire dire un message au bot")]
    public async Task SpeakAsync(
        [Summary("message", "Le message à faire dire au bot")]
        string message,
        [Summary("salon", "Salon de destination (par défaut : le salon actuel)")]
        ITextChannel? salon = null,
        [Summary("anonyme", "Ne pas indiquer que le message vient de toi")]
        bool anonyme = false)
    {
        if (Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
            return;
        }

        // Slash-command input is single-line, so let a literal \n stand for a
        // line break — the only way to write a multi-line announcement.
        var text = message.Replace("\\n", "\n").Trim();

        if (text.Length == 0)
        {
            await RespondAsync("Ton message est vide, je n'ai rien à dire. ✍️", ephemeral: true);
            return;
        }

        if (text.Length > MaxMessageLength)
        {
            await RespondAsync(
                $"Ton message fait {text.Length} caractères, le maximum est {MaxMessageLength}. ✂️",
                ephemeral: true);
            return;
        }

        var target = salon ?? Context.Channel as ITextChannel;
        if (target is null)
        {
            await RespondAsync("Cette commande ne fonctionne que dans un salon textuel.", ephemeral: true);
            return;
        }

        string content;
        if (anonyme)
        {
            // Pure ventriloquism: the bot's own voice, nothing pointing back to him.
            content = text;
        }
        else
        {
            var ownerName = (Context.User as SocketGuildUser)?.Nickname
                ?? Context.User.GlobalName
                ?? Context.User.Username;
            var herald = string.Format(
                BotResponses.OwnerAnnouncementHeralds[
                    Random.Shared.Next(BotResponses.OwnerAnnouncementHeralds.Length)],
                ownerName);
            content = $"{herald}\n{MessageFormat.Quote(text, MaxMessageLength)}";
        }

        try
        {
            await target.SendMessageAsync(
                content,
                // Honour users the owner mentioned, but never @everyone/@here or a
                // role — the bot must not become a way to mass-ping.
                allowedMentions: new AllowedMentions(AllowedMentionTypes.Users));

            _logger.LogInformation("Owner spoke through the bot in channel {ChannelId} (anonymous: {Anonymous}).",
                target.Id, anonyme);

            await RespondAsync($"Envoyé dans {target.Mention}. ✅", ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to speak through the bot in channel {ChannelId}.", target.Id);
            await RespondAsync(
                $"Impossible d'écrire dans {target.Mention} — je n'ai peut-être pas la permission. ❌",
                ephemeral: true);
        }
    }
}
