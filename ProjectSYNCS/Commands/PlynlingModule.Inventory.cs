using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Interactions.Autocomplete;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// The inventory half of /plynling: buying food ahead, giving items away, and looking at what
// you hold. Items belong to the person, not to a Plynling — they survive its death.
public partial class PlynlingModule
{
    // How many of one food the shop sells at once.
    private const int MaxShopQuantity = 20;

    [SlashCommand("shop", "Acheter de la nourriture d'avance pour ton garde-manger (−10 % dès 5)")]
    public async Task ShopAsync(
        [Summary("food", "Quoi acheter")] PlynlingFood food,
        [Summary("quantity", "Combien (1 à 20)")] [MinValue(1)] [MaxValue(MaxShopQuantity)] int quantity = 1)
    {
        var info = PlynlingCatalog.Info(food);
        var (bought, price, balance) = await _inventory.BuyAsync(Context.Guild.Id, Context.User.Id, food, quantity, DateTimeOffset.UtcNow);
        await RespondAsync(bought
                ? PlynlingText.Bought(quantity, info.Name, price, balance, discounted: quantity >= 5)
                : $"{PlynlingText.ShopTooPoor} ({PebbleEconomy.Cailloux(price)}, tu en as {PebbleEconomy.Cailloux(balance)})",
            ephemeral: true);
    }

    [SlashCommand("give", "Offrir un objet de ton inventaire à quelqu'un")]
    public async Task GiveAsync(
        [Summary("user", "À qui l'offrir")] IUser user,
        [Summary("item", "Quel objet")] [Autocomplete(typeof(InventoryItemAutocompleteHandler))] string item,
        [Summary("quantity", "Combien")] [MinValue(1)] [MaxValue(99)] int quantity = 1)
    {
        if (user.Id == Context.User.Id)
        {
            await RespondAsync(PlynlingText.GiveSelf, ephemeral: true);
            return;
        }
        if (user.IsBot)
        {
            await RespondAsync(PlynlingText.GiveBot, ephemeral: true);
            return;
        }

        var (outcome, completed) = await _inventory.GiveAsync(Context.Guild.Id, Context.User.Id, user.Id, item, quantity, DateTimeOffset.UtcNow);
        if (outcome != InventoryService.GiveOutcome.Given)
        {
            await RespondAsync(outcome == InventoryService.GiveOutcome.UnknownItem ? PlynlingText.UnknownItem : PlynlingText.NotEnoughItems,
                ephemeral: true);
            return;
        }

        var info = ItemCatalog.ByKey(item)!;
        var text = PlynlingText.Gave(Context.User.Id, user.Id, quantity, info.Emoji, info.Name);
        foreach (var set in completed) text += "\n" + PlynlingText.SetCompleted(user.Id, set);
        // Public, and it pings the recipient — users only, as every relay here.
        await RespondAsync(text, allowedMentions: new AllowedMentions(AllowedMentionTypes.Users));
    }

    [SlashCommand("inventory", "Ton inventaire : garde-manger, objets trouvés, cailloux")]
    public async Task InventoryAsync()
    {
        var held = await _inventory.GetAllAsync(Context.Guild.Id, Context.User.Id);
        var completions = await _inventory.GetCompletionsAsync(Context.Guild.Id, Context.User.Id);
        var balance = await _inventory.BalanceAsync(Context.Guild.Id, Context.User.Id);
        await RespondAsync(embed: BuildInventoryEmbed(held, completions.Count, balance), ephemeral: true);
    }

    // Static and Context-free, like every other builder here, so its size is checkable.
    public static Embed BuildInventoryEmbed(IReadOnlyCollection<InventoryItem> held, int setsCompleted, long balance)
    {
        var byKey = held.ToDictionary(i => i.Key);
        int Count(string key) => byKey.TryGetValue(key, out var row) ? row.Quantity : 0;

        var pantry = string.Join("\n", PlynlingCatalog.Foods.Select(f =>
        {
            var item = ItemCatalog.ByKey(ItemCatalog.FoodKey(f.Food))!;
            return $"{item.Emoji} {item.Name} : **{Count(item.Key)}**";
        }));

        var embed = new EmbedBuilder()
            .WithTitle("🎒 Ton inventaire")
            .WithColor(Color.Purple)
            .AddField("🧺 Garde-manger", pantry + "\n-# Nourrir puise ici d'abord : 1 pour ton Plynling, 2 pour celui d'un autre.");

        // One field per set, holding only what is in hand — a set of 8 is at most ~8 short lines.
        foreach (var set in ItemCatalog.Sets)
        {
            var lines = ItemCatalog.InSet(set.Key).Where(i => Count(i.Key) > 0)
                .Select(i => $"{i.Emoji} {i.Name} ×{Count(i.Key)}").ToList();
            if (lines.Count > 0) embed.AddField($"{set.Emoji} {set.Name}", string.Join("\n", lines), inline: true);
        }

        var discovered = ItemCatalog.Collectibles.Count(i => byKey.ContainsKey(i.Key));
        embed.AddField("📖 Collection",
            $"{discovered}/{ItemCatalog.Collectibles.Count()} objets découverts · {setsCompleted}/{ItemCatalog.Sets.Count} collections complètes");
        embed.WithFooter($"🪨 {PebbleEconomy.Cailloux(balance)}");
        return embed.Build();
    }
}
