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

    [SlashCommand("forage", "Envoyer ton Plynling fouiller les environs — un objet ou de quoi manger, toutes les 4 h")]
    public async Task ForageAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var result = await _plynlings.ForageAsync(Context.Guild.Id, Context.User.Id, now, Random.Shared);
        var gender = result.Plynling?.Gender ?? PlynlingGender.Male;
        if (result.Outcome == CareOutcome.TooSoon)
        {
            await RespondAsync(PlynlingText.ForageTooSoon(gender, result.ReadyAt!.Value), ephemeral: true);
            return;
        }
        if (result.Outcome != CareOutcome.Done || result.Find is null || result.Plynling is null)
        {
            await RespondAsync(PlynlingCareService.Refusal(result.Outcome, gender), ephemeral: true);
            return;
        }
        var line = PlynlingText.FindLines(
            PlynlingText.Foraged(PlynlingCardUi.SafeName(result.Plynling.Name), gender, result.Find.Item), result.Find, Context.User.Id);
        await RespondCardAsync(result.Plynling, now, line);
    }

    [SlashCommand("collection", "Le carnet de collection de quelqu'un (le tien par défaut)")]
    public async Task CollectionAsync([Summary("user", "De qui (par défaut : toi)")] IUser? user = null)
    {
        var target = user ?? Context.User;
        var held = await _inventory.GetAllAsync(Context.Guild.Id, target.Id);
        var completions = await _inventory.GetCompletionsAsync(Context.Guild.Id, target.Id);
        await RespondAsync(embed: BuildCollectionEmbed(target.Id, held, completions),
            allowedMentions: AllowedMentions.None);
    }

    // The book: one field per set. An item ever held is shown for good (even at quantity 0); the
    // rest are « ??? » with only their rarity — and season, since that is when to look.
    public static Embed BuildCollectionEmbed(ulong userId, IReadOnlyCollection<InventoryItem> held,
        IReadOnlyCollection<CollectionCompletion> completions)
    {
        var discovered = held.Select(i => i.Key).ToHashSet();
        var done = completions.Select(c => c.SetKey).ToHashSet();
        var total = ItemCatalog.Collectibles.Count();
        var found = ItemCatalog.Collectibles.Count(i => discovered.Contains(i.Key));

        var embed = new EmbedBuilder()
            .WithTitle("📖 Carnet de collection")
            .WithColor(Color.Purple)
            .WithDescription($"<@{userId}> · **{found}/{total}** objets découverts · {done.Count}/{ItemCatalog.Sets.Count} collections complètes");
        foreach (var set in ItemCatalog.Sets)
        {
            var items = ItemCatalog.InSet(set.Key).ToList();
            var have = items.Count(i => discovered.Contains(i.Key));
            var lines = items.Select(i =>
            {
                var season = i.Season == Season.None ? "" : $" · {ItemCatalog.SeasonLabel(i.Season)}";
                return discovered.Contains(i.Key)
                    ? $"{i.Emoji} {i.Name}{season}"
                    : $"❔ ??? · {ItemCatalog.RarityLabel(i.Rarity)}{season}";
            });
            var status = done.Contains(set.Key) ? "✅ complète" : $"{have}/{items.Count} · complète : +{PebbleEconomy.Cailloux(set.Reward)}";
            embed.AddField($"{set.Emoji} {set.Name} — {status}", string.Join("\n", lines), inline: true);
        }
        embed.WithFooter("Trouve-les avec /plynling forage, le cadeau du jour, les jeux et les visites — ou échange-les.");
        return embed.Build();
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
