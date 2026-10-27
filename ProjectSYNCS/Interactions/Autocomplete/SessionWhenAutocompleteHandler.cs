using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Interactions.Autocomplete;

/// <summary>
/// Shows what a typed « quand » reads as, before it's sent: « samedi 21h » → « sam. 1 nov. à 21:00 ».
/// The value stays what was typed, so the command parses it again; the preview is only a promise
/// that it will read the same.
/// </summary>
public sealed class SessionWhenAutocompleteHandler : AutocompleteHandler
{
    private const int MaxLabel = 100;

    public override Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction interaction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        var typed = (interaction.Data.Current.Value as string ?? string.Empty).Trim();
        if (typed.Length == 0)
            return Task.FromResult(AutocompletionResult.FromSuccess(new[]
            {
                new AutocompleteResult("ex. « samedi 21h », « demain », « 25/12 20:30 », « ce soir »", "ce soir"),
            }));

        var at = SessionTime.Parse(typed, DateTimeOffset.UtcNow);
        var label = at is { } t
            ? $"{SessionTime.Preview(t)} — « {typed} »"
            : $"Date non reconnue ou passée — « {typed} »";
        if (label.Length > MaxLabel) label = label[..(MaxLabel - 1)] + "…";
        // A choice's value is capped at 100 characters too.
        var value = typed.Length <= MaxLabel ? typed : typed[..MaxLabel];
        return Task.FromResult(AutocompletionResult.FromSuccess(new[] { new AutocompleteResult(label, value) }));
    }
}
