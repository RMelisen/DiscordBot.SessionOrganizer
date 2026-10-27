using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /confess — an anonymous message she posts, numbered, in the channel set with
// /config confess-channel (none set: closed). The author only gets an ephemeral receipt, so nobody
// sees who used the command; only the owner can look the author up, with /debug confession.
// She doesn't comment on confessions: her chiming in would make them feel less anonymous.
[CommandContextType(InteractionContextType.Guild)]
public class ConfessModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly GuildConfigService _config;
    private readonly ConfessionService _confessions;
    private readonly ILogger<ConfessModule> _logger;

    public ConfessModule(GuildConfigService config, ConfessionService confessions, ILogger<ConfessModule> logger)
    {
        _config = config;
        _confessions = confessions;
        _logger = logger;
    }

    [SlashCommand("confess", "Poster une confession anonyme")]
    public async Task ConfessAsync(
        [Summary("texte", "Ta confession — personne ne saura que c'est toi")]
        [MaxLength(InputCaps.Confession)]
        string texte)
    {
        // Ephemeral from the start: a public defer would show « X utilise /confess » to the room.
        await DeferAsync(ephemeral: true);

        var config = await _config.GetAsync(Context.Guild.Id);
        if (config.ConfessChannelId == 0 || Context.Guild.GetTextChannel(config.ConfessChannelId) is not { } channel)
        {
            await FollowupAsync("Les confessions ne sont pas ouvertes sur ce serveur.", ephemeral: true);
            return;
        }

        var text = texte.Trim();
        if (text.Length == 0)
        {
            await FollowupAsync("Une confession vide, c'est un peu court.", ephemeral: true);
            return;
        }

        if (!_confessions.TryClaim(Context.User.Id, out var readyAt))
        {
            await FollowupAsync(
                $"Une confession toutes les 10 minutes. La prochaine, {TimestampTag.FormatFromDateTimeOffset(readyAt, TimestampTagStyles.Relative)}.",
                ephemeral: true);
            return;
        }

        int number;
        try
        {
            number = await _confessions.ReserveAsync(Context.Guild.Id, Context.User.Id);
        }
        catch (Exception ex)
        {
            _confessions.Release(Context.User.Id);
            _logger.LogWarning(ex, "Failed to save a confession in guild {GuildId}.", Context.Guild.Id);
            await FollowupAsync("Je n'ai pas réussi à l'enregistrer. Réessaie dans un instant.", ephemeral: true);
            return;
        }

        IUserMessage posted;
        try
        {
            // No mentions at all: a confession must never ping anyone, let alone @everyone.
            posted = await channel.SendMessageAsync(embed: BuildCard(number, text), allowedMentions: AllowedMentions.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to post confession #{Number} in channel {ChannelId}.", number, channel.Id);
            _confessions.Release(Context.User.Id);
            try { await _confessions.RemoveAsync(Context.Guild.Id, number); }
            catch (Exception removeEx) { _logger.LogWarning(removeEx, "Failed to drop confession #{Number}'s row.", number); }
            await FollowupAsync($"Je n'arrive pas à écrire dans {channel.Mention}. Préviens le staff.", ephemeral: true);
            return;
        }

        try
        {
            await _confessions.AttachAsync(Context.Guild.Id, number, channel.Id, posted.Id);
        }
        catch (Exception ex)
        {
            // The confession is out and its author is saved; only the link to the post is missing.
            _logger.LogWarning(ex, "Failed to record confession #{Number}'s message.", number);
        }

        await FollowupAsync("Confession envoyée.", ephemeral: true);
    }

    /// <summary>The posted card: a number and the text, nothing about who wrote it.</summary>
    public static Embed BuildCard(int number, string text) =>
        new EmbedBuilder()
            .WithTitle($"Confession #{number}")
            .WithDescription(text)
            .WithColor(new Color(0x6A0DAD))
            .WithFooter("Anonyme · /confess")
            .Build();
}
