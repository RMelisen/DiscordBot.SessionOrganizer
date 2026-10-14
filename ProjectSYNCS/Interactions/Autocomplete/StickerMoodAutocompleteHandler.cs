using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Autocomplete;

/// <summary>
/// Suggests the sticker strips for <c>/debug sticker</c> (<see cref="Sticker.AllStrips"/>, the
/// glitch included), matched on the stem. Owner-only like the command. There are more strips than
/// Discord's 25 suggestions: the empty list shows the first 25, typing narrows it.
/// </summary>
public sealed class StickerMoodAutocompleteHandler : AutocompleteHandler
{
    private const int MaxSuggestions = 25;

    public override Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction interaction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        if (context.User.Id != AvailabilityService.OwnerId)
            return Task.FromResult(AutocompletionResult.FromSuccess());

        var typed = interaction.Data.Current.Value as string ?? string.Empty;
        var matches = Sticker.AllStrips
            .Where(m => typed.Length == 0 || m.Contains(typed, StringComparison.OrdinalIgnoreCase))
            .Take(MaxSuggestions)
            .Select(m => new AutocompleteResult(m, m));

        return Task.FromResult(AutocompletionResult.FromSuccess(matches));
    }
}
