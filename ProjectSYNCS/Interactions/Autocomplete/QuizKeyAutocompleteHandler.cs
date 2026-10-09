using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Autocomplete;

/// <summary>
/// Suggests QuizBank keys for <c>/debug quiz</c>, matched on the key or the question.
/// Owner-only like the command: everyone else gets nothing, since the labels give the
/// questions away (the answers are a click further, but still).
/// </summary>
public sealed class QuizKeyAutocompleteHandler : AutocompleteHandler
{
    // Discord rejects more than 25 suggestions, and a label over 100 characters.
    private const int MaxSuggestions = 25;
    private const int MaxLabel = 100;

    public override Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction interaction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        if (context.User.Id != AvailabilityService.OwnerId)
            return Task.FromResult(AutocompletionResult.FromSuccess());

        var typed = interaction.Data.Current.Value as string ?? string.Empty;
        var matches = QuizBank.All
            .Where(q => typed.Length == 0
                        || q.Key.Contains(typed, StringComparison.OrdinalIgnoreCase)
                        || q.Prompt.Contains(typed, StringComparison.OrdinalIgnoreCase))
            .Take(MaxSuggestions)
            .Select(q => new AutocompleteResult(Label(q), q.Key));

        return Task.FromResult(AutocompletionResult.FromSuccess(matches));
    }

    private static string Label(QuizQuestion question)
    {
        var label = $"{question.Key} — {(question.IsChoice ? "QCM" : "libre")} — {question.Prompt}";
        return label.Length <= MaxLabel ? label : label[..(MaxLabel - 1)] + "…";
    }
}
