using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Autocomplete;

/// <summary>
/// Suggests the servers she is on, for a plain string option (<c>/debug silence server:</c>).
/// A string rather than a native option for the same reason as <see cref="ChannelAutocompleteHandler"/>:
/// it has to work from a DM. The value is the server's id; the label its name.
/// </summary>
public sealed class GuildAutocompleteHandler : AutocompleteHandler
{
    // Discord rejects a response carrying more than this.
    private const int MaxSuggestions = 25;

    public override Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction interaction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        // Owner-only, like the command it serves: nobody else is shown the server list.
        if (context.User.Id != AvailabilityService.OwnerId)
            return Task.FromResult(AutocompletionResult.FromSuccess());

        var typed = interaction.Data.Current.Value as string ?? string.Empty;
        var client = (DiscordSocketClient)context.Client;

        var matches = client.Guilds
            .Where(g => typed.Length == 0 || g.Name.Contains(typed, StringComparison.OrdinalIgnoreCase))
            .OrderBy(g => g.Name)
            .Take(MaxSuggestions)
            // Discord caps a choice name at 100 characters.
            .Select(g => new AutocompleteResult(g.Name.Length <= 100 ? g.Name : g.Name[..100], g.Id.ToString()))
            .ToList();

        return Task.FromResult(AutocompletionResult.FromSuccess(matches));
    }
}
