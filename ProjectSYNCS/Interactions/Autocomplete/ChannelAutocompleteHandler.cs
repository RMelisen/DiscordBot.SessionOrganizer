using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Autocomplete;

/// <summary>
/// Suggests the text channels <c>/tell</c> can post into, for a plain string option.
/// </summary>
/// <remarks>
/// <para><b>This exists because a native channel option cannot work in a DM.</b>
/// Discord's channel picker resolves against the guild the command was invoked in, and
/// a DM has none — so the option renders with nothing to choose. Autocomplete is driven
/// by the bot rather than by the client's context, which makes it the only way to pick a
/// guild channel from a private message.</para>
/// <para>Used in guilds too rather than keeping the native picker beside it: one option
/// that behaves identically everywhere beats two overlapping ones that each work half
/// the time, and would need conflict handling between them.</para>
/// <para>Only channels the bot can actually <b>send</b> in are offered. Listing one it
/// cannot post to would turn a typo into a failed send that only surfaces afterwards.</para>
/// </remarks>
public sealed class ChannelAutocompleteHandler : AutocompleteHandler
{
    // Discord rejects a response carrying more than this.
    private const int MaxSuggestions = 25;

    public override Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction interaction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        // The command this serves is owner-only. Suggesting nothing to everyone else
        // keeps it from listing channels they may not be able to see — the command
        // itself is registered globally and therefore visible to them.
        if (context.User.Id != AvailabilityService.OwnerId)
            return Task.FromResult(AutocompletionResult.FromSuccess());

        var typed = interaction.Data.Current.Value as string ?? string.Empty;
        var client = (DiscordSocketClient)context.Client;
        var manyGuilds = client.Guilds.Count > 1;

        var matches = client.Guilds
            .SelectMany(g => g.TextChannels.Select(c => (Guild: g, Channel: c)))
            // A voice channel's text chat and a forum are not places to post a line.
            .Where(x => x.Channel is not SocketVoiceChannel)
            .Where(x => CanSendIn(x.Guild, x.Channel))
            .Where(x => typed.Length == 0
                        || x.Channel.Name.Contains(typed, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Guild.Name)
            .ThenBy(x => x.Channel.Position)
            .Take(MaxSuggestions)
            // The *value* is the id, so resolution never has to guess from a name; the
            // label is what the owner reads.
            .Select(x => new AutocompleteResult(
                Label(x.Guild, x.Channel, manyGuilds),
                x.Channel.Id.ToString()))
            .ToList();

        return Task.FromResult(AutocompletionResult.FromSuccess(matches));
    }

    // Guild name only when there is more than one, so the common case stays short.
    // Truncated because Discord caps a choice name at 100 characters.
    private static string Label(SocketGuild guild, SocketTextChannel channel, bool manyGuilds)
    {
        var name = manyGuilds ? $"{guild.Name} / #{channel.Name}" : $"#{channel.Name}";
        return name.Length <= 100 ? name : name[..100];
    }

    private static bool CanSendIn(SocketGuild guild, SocketTextChannel channel)
    {
        var me = guild.CurrentUser;
        if (me is null) return false;

        var perms = me.GetPermissions(channel);
        return perms.ViewChannel && perms.SendMessages;
    }
}
