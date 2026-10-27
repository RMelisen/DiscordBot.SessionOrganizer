using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Autocomplete;

/// <summary>
/// The ids of /schedule edit and /schedule cancel: the upcoming sessions of this server the person
/// may manage (theirs, or all of them for staff), with title and date, so nobody has to read the
/// card's footer.
/// </summary>
public sealed class SessionIdAutocompleteHandler : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context, IAutocompleteInteraction interaction, IParameterInfo parameter, IServiceProvider services)
    {
        if (context.Guild is null) return AutocompletionResult.FromSuccess();
        var typed = interaction.Data.Current.Value?.ToString() ?? string.Empty;

        await using var scope = services.CreateAsyncScope();
        var events = await scope.ServiceProvider.GetRequiredService<EventService>().GetActiveEventsAsync(context.Guild.Id);
        var results = events
            .Where(e => SessionPermissions.CanManage(context.User, e))
            .Where(e => typed.Length == 0 || e.Id.ToString().StartsWith(typed)
                        || e.Title.Contains(typed, StringComparison.OrdinalIgnoreCase))
            .Take(SchedulingIds.MaxSuggestions)
            .Select(e => new AutocompleteResult(
                SchedulingIds.Label($"#{e.Id} {SessionCategories.Emoji(e.Category)} {e.Title}", SessionTime.Preview(e.ScheduledAt)), e.Id));
        return AutocompletionResult.FromSuccess(results);
    }
}

/// <summary>The ids of /when delete: the open date polls the person may manage.</summary>
public sealed class PollIdAutocompleteHandler : AutocompleteHandler
{
    public override Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context, IAutocompleteInteraction interaction, IParameterInfo parameter, IServiceProvider services) =>
        SchedulingIds.PollsAsync(context, interaction, services, PollKind.DateSlots);
}

/// <summary>The ids of /vote delete: the open votes the person may manage.</summary>
public sealed class VoteIdAutocompleteHandler : AutocompleteHandler
{
    public override Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context, IAutocompleteInteraction interaction, IParameterInfo parameter, IServiceProvider services) =>
        SchedulingIds.PollsAsync(context, interaction, services, PollKind.Text);
}

internal static class SchedulingIds
{
    // Discord rejects more than 25 suggestions, and a label over 100 characters.
    public const int MaxSuggestions = 25;
    private const int MaxLabel = 100;

    // « #12 🎮 Among Us — sam. 1 nov. à 21:00 », the title cut first so the date always shows.
    public static string Label(string head, string tail)
    {
        var room = MaxLabel - tail.Length - 3;
        if (head.Length > room) head = head[..Math.Max(0, room - 1)] + "…";
        return $"{head} — {tail}";
    }

    public static async Task<AutocompletionResult> PollsAsync(
        IInteractionContext context, IAutocompleteInteraction interaction, IServiceProvider services, PollKind kind)
    {
        if (context.Guild is null) return AutocompletionResult.FromSuccess();
        var typed = interaction.Data.Current.Value?.ToString() ?? string.Empty;

        await using var scope = services.CreateAsyncScope();
        var polls = await scope.ServiceProvider.GetRequiredService<PollService>().GetActivePollsAsync(context.Guild.Id, kind);
        var results = polls
            .Where(p => SessionPermissions.CanManage(context.User, p))
            .Where(p => typed.Length == 0 || p.Id.ToString().StartsWith(typed)
                        || p.Title.Contains(typed, StringComparison.OrdinalIgnoreCase))
            .Take(MaxSuggestions)
            .Select(p => new AutocompleteResult(
                Label($"#{p.Id} {p.Title}", $"fin {SessionTime.Preview(p.ClosesAt)}"), p.Id));
        return AutocompletionResult.FromSuccess(results);
    }
}
