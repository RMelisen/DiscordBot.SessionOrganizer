using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Autocomplete;

/// <summary>
/// Suggests the items the person typing actually holds — for <c>/inventory give</c>, trade and
/// sell. The value sent back is the item's stable catalog key, the display its emoji, name and
/// count, so nothing ever has to parse a name back into an item. In <c>/inventory trade</c>'s
/// <c>give</c>, once the <c>user</c> option is filled in, each line also says how many that person
/// already has — whether they need it is the whole question of an offer.
/// </summary>
/// <remarks>
/// A suggestion is only a convenience: the command still re-checks the key and the count,
/// since anyone can type a value the list never offered.
/// </remarks>
public sealed class InventoryItemAutocompleteHandler : AutocompleteHandler
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
        await using var scope = services.CreateAsyncScope();
        var inventory = scope.ServiceProvider.GetRequiredService<InventoryService>();
        var held = await inventory.GetAllAsync(context.Guild.Id, context.User.Id);

        // The other side of a trade, when there is one yet.
        Dictionary<string, int>? theirs = null;
        var otherName = "";
        if (interaction.Data.Current.Name == "give"
            && ulong.TryParse(interaction.Data.Options.FirstOrDefault(o => o.Name == "user")?.Value?.ToString(), out var otherId)
            && otherId != context.User.Id)
        {
            theirs = (await inventory.GetAllAsync(context.Guild.Id, otherId)).ToDictionary(i => i.Key, i => i.Quantity);
            otherName = await TradeAutocomplete.NameAsync(context.Guild, otherId);
        }

        var results = held
            .Where(i => i.Quantity > 0)
            .Select(i => (Row: i, Info: ItemCatalog.ByKey(i.Key)))
            .Where(x => x.Info is not null)
            .Where(x => typed.Length == 0 || x.Info!.Name.Contains(typed, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Info!.Kind).ThenBy(x => x.Info!.Name, StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("fr-FR"), true))
            .Take(MaxSuggestions)
            .Select(x =>
            {
                var line = $"{ItemCatalog.TextEmoji(x.Info!)}{ItemCatalog.ClearName(x.Info!)} ×{x.Row.Quantity}";
                if (theirs is not null) line += $" · {otherName} en a {theirs.GetValueOrDefault(x.Info!.Key)}";
                return new AutocompleteResult(TradeAutocomplete.Clamp(line), x.Info!.Key);
            });
        return AutocompletionResult.FromSuccess(results);
    }
}
