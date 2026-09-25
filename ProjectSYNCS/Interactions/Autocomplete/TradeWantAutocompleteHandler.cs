using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Autocomplete;

/// <summary>
/// <c>/plynling trade</c>'s <c>want</c>: what the chosen person holds, once the <c>user</c>
/// option is filled in — asking for something they do not have would only be refused. Before
/// that, the whole catalog.
/// </summary>
public sealed class TradeWantAutocompleteHandler : AutocompleteHandler
{
    // Discord rejects a response carrying more than this.
    private const int MaxSuggestions = 25;

    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction interaction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        if (context.Guild is null) return AutocompletionResult.FromSuccess();
        var typed = interaction.Data.Current.Value as string ?? string.Empty;
        bool Matches(ItemInfo i) => typed.Length == 0 || i.Name.Contains(typed, StringComparison.OrdinalIgnoreCase);
        var french = StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("fr-FR"), true);

        var userOption = interaction.Data.Options.FirstOrDefault(o => o.Name == "user")?.Value?.ToString();
        if (!ulong.TryParse(userOption, out var userId))
        {
            return AutocompletionResult.FromSuccess(ItemCatalog.All.Where(Matches)
                .OrderBy(i => i.Kind).ThenBy(i => i.Name, french)
                .Take(MaxSuggestions)
                .Select(i => new AutocompleteResult($"{i.Emoji} {i.Name}", i.Key)));
        }

        await using var scope = services.CreateAsyncScope();
        var held = await scope.ServiceProvider.GetRequiredService<InventoryService>().GetAllAsync(context.Guild.Id, userId);
        return AutocompletionResult.FromSuccess(held
            .Where(r => r.Quantity > 0)
            .Select(r => (Row: r, Info: ItemCatalog.ByKey(r.Key)))
            .Where(x => x.Info is not null && Matches(x.Info))
            .OrderBy(x => x.Info!.Kind).ThenBy(x => x.Info!.Name, french)
            .Take(MaxSuggestions)
            .Select(x => new AutocompleteResult($"{x.Info!.Emoji} {x.Info.Name} ×{x.Row.Quantity}", x.Info.Key)));
    }
}
