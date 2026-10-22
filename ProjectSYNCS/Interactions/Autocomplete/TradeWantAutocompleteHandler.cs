using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Autocomplete;

/// <summary>
/// <c>/inventory trade</c>'s <c>want</c>: what the chosen person holds, once the <c>user</c>
/// option is filled in — asking for something they do not have would only be refused. Before
/// that, the whole catalog. Each line also says how many the person typing already has, so they
/// can tell what they actually need.
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

        await using var scope = services.CreateAsyncScope();
        var inventory = scope.ServiceProvider.GetRequiredService<InventoryService>();
        var mine = (await inventory.GetAllAsync(context.Guild.Id, context.User.Id)).ToDictionary(i => i.Key, i => i.Quantity);
        string Mine(ItemInfo i) => $" · tu en as {mine.GetValueOrDefault(i.Key)}";

        var userOption = interaction.Data.Options.FirstOrDefault(o => o.Name == "user")?.Value?.ToString();
        if (!ulong.TryParse(userOption, out var userId))
        {
            return AutocompletionResult.FromSuccess(ItemCatalog.All.Where(Matches)
                .OrderBy(i => i.Kind).ThenBy(i => i.Name, french)
                .Take(MaxSuggestions)
                .Select(i => new AutocompleteResult(TradeAutocomplete.Clamp($"{ItemCatalog.TextEmoji(i)}{ItemCatalog.ClearName(i)}{Mine(i)}"), i.Key)));
        }

        var held = await inventory.GetAllAsync(context.Guild.Id, userId);
        return AutocompletionResult.FromSuccess(held
            .Where(r => r.Quantity > 0)
            .Select(r => (Row: r, Info: ItemCatalog.ByKey(r.Key)))
            .Where(x => x.Info is not null && Matches(x.Info))
            .OrderBy(x => x.Info!.Kind).ThenBy(x => x.Info!.Name, french)
            .Take(MaxSuggestions)
            .Select(x => new AutocompleteResult(
                TradeAutocomplete.Clamp($"{ItemCatalog.TextEmoji(x.Info!)}{ItemCatalog.ClearName(x.Info!)} ×{x.Row.Quantity}{Mine(x.Info!)}"),
                x.Info!.Key)));
    }
}

// What the two trade autocompletes share.
internal static class TradeAutocomplete
{
    // Discord caps a suggestion's display at 100 characters.
    private const int MaxDisplay = 100;
    private const int MaxName = 20;

    public static string Clamp(string line) => line.Length <= MaxDisplay ? line : line[..(MaxDisplay - 1)] + "…";

    // The other side's name, short enough to leave room for the item; « l'autre » when they
    // aren't in the member cache.
    public static async Task<string> NameAsync(IGuild guild, ulong userId)
    {
        var user = await guild.GetUserAsync(userId, CacheMode.CacheOnly);
        if (user is null) return "l'autre";
        var name = BotResponses.DisplayNameFor(user);
        return name.Length <= MaxName ? name : name[..(MaxName - 1)] + "…";
    }
}
