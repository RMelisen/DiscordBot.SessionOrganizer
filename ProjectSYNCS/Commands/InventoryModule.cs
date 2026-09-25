using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Interactions.Autocomplete;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /inventory — what a person holds: the pantry, the collectibles and the book, and the ways
// items change hands. Its own group rather than part of /plynling because items belong to the
// person, not to a Plynling — they survive its death and abandonment — and because /plynling
// was running out of Discord's 25 subcommands.
//
// Guild-only: every query is scoped to Context.Guild.Id.
[CommandContextType(InteractionContextType.Guild)]
[Group("inventory", "Tes objets : garde-manger, collection, échanges")]
public class InventoryModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly InventoryService _inventory;
    private readonly TradeOffers _trades;

    public InventoryModule(InventoryService inventory, TradeOffers trades)
    {
        _inventory = inventory;
        _trades = trades;
    }

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

    [SlashCommand("trade", "Proposer un échange d'objets à quelqu'un (l'offre dure 1 h)")]
    public async Task TradeAsync(
        [Summary("user", "Avec qui échanger")] IUser user,
        [Summary("give", "Ce que tu donnes")] [Autocomplete(typeof(InventoryItemAutocompleteHandler))] string give,
        [Summary("want", "Ce que tu veux en échange")] [Autocomplete(typeof(TradeWantAutocompleteHandler))] string want,
        [Summary("give_quantity", "Combien tu en donnes")] [MinValue(1)] [MaxValue(99)] int giveQuantity = 1,
        [Summary("want_quantity", "Combien tu en veux")] [MinValue(1)] [MaxValue(99)] int wantQuantity = 1)
    {
        string? refusal =
            user.Id == Context.User.Id ? PlynlingText.TradeSelf
            : user.IsBot ? PlynlingText.GiveBot
            : ItemCatalog.ByKey(give) is null || ItemCatalog.ByKey(want) is null ? PlynlingText.UnknownItem
            : give == want ? PlynlingText.TradeSameItem
            : await _inventory.CountAsync(Context.Guild.Id, Context.User.Id, give) < giveQuantity ? PlynlingText.NotEnoughItems
            : await _inventory.CountAsync(Context.Guild.Id, user.Id, want) < wantQuantity ? PlynlingText.TradeTheyLack(user.Id)
            : null;
        if (refusal is not null)
        {
            await RespondAsync(refusal, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        var offer = _trades.Open(Context.Guild.Id, Context.User.Id, user.Id, give, giveQuantity, want, wantQuantity,
            DateTimeOffset.UtcNow, Random.Shared);
        var (giveText, wantText) = TradeSides(offer);
        var buttons = new ComponentBuilder()
            .WithButton("Accepter", $"plyn:tacc:{offer.Id}", ButtonStyle.Success, new Emoji("🤝"))
            .WithButton("Refuser", $"plyn:tdec:{offer.Id}", ButtonStyle.Danger)
            .Build();
        // Public, pinging only the person being asked.
        await RespondAsync(PlynlingText.TradeOffered(offer.FromId, offer.ToId, giveText, wantText, offer.ExpiresAt),
            components: buttons, allowedMentions: new AllowedMentions { UserIds = new List<ulong> { user.Id } });
    }

    // Both sides of an offer as « N × emoji nom », for every line that names them.
    public static (string Give, string Want) TradeSides(TradeOffer offer) =>
        (PlynlingText.TradeSide(ItemCatalog.ByKey(offer.GiveKey)!, offer.GiveQty),
         PlynlingText.TradeSide(ItemCatalog.ByKey(offer.WantKey)!, offer.WantQty));

    [SlashCommand("sell", "Vendre des objets contre des cailloux")]
    public async Task SellAsync(
        [Summary("item", "Quel objet")] [Autocomplete(typeof(InventoryItemAutocompleteHandler))] string item,
        [Summary("quantity", "Combien")] [MinValue(1)] [MaxValue(99)] int quantity = 1)
    {
        var (outcome, earned, balance) = await _inventory.SellAsync(Context.Guild.Id, Context.User.Id, item, quantity);
        await RespondAsync(outcome switch
        {
            InventoryService.GiveOutcome.Given => PlynlingText.Sold(quantity, ItemCatalog.ByKey(item)!, earned, balance),
            InventoryService.GiveOutcome.UnknownItem => PlynlingText.UnknownItem,
            _ => PlynlingText.NotEnoughItems,
        }, ephemeral: true);
    }

    [SlashCommand("view", "Ton inventaire : garde-manger, objets trouvés, cailloux")]
    public async Task InventoryAsync()
    {
        var held = await _inventory.GetAllAsync(Context.Guild.Id, Context.User.Id);
        var completions = await _inventory.GetCompletionsAsync(Context.Guild.Id, Context.User.Id);
        var balance = await _inventory.BalanceAsync(Context.Guild.Id, Context.User.Id);
        await RespondAsync(embed: BuildInventoryEmbed(held, completions.Count, balance), ephemeral: true);
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
            var status = done.Contains(set.Key) ? "✅ complète" : $"{have}/{items.Count} · complète : +{PebbleEconomy.Cailloux(set.Reward)}";
            // A big set is split by rarity (ItemCatalog.Sections); its status rides the first block.
            var first = true;
            foreach (var (label, section) in ItemCatalog.Sections(set.Key))
            {
                var lines = section.Select(i =>
                {
                    var season = i.Season == Season.None ? "" : $" · {ItemCatalog.SeasonLabel(i.Season)}";
                    return discovered.Contains(i.Key)
                        ? $"{i.Emoji} {i.Name}{season}"
                        : $"❔ ??? · {ItemCatalog.RarityLabel(i.Rarity)}{season}";
                });
                var name = first
                    ? $"{set.Emoji} {set.Name}{(label.Length > 0 ? $" ({label})" : "")} — {status}"
                    : $"{set.Emoji} {set.Name} ({label})";
                embed.AddField(name, string.Join("\n", lines), inline: true);
                first = false;
            }
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

        // One field per set (per rarity for a big one), holding only what is in hand.
        foreach (var set in ItemCatalog.Sets)
            foreach (var (label, section) in ItemCatalog.Sections(set.Key))
            {
                var lines = section.Where(i => Count(i.Key) > 0).Select(i => $"{i.Emoji} {i.Name} ×{Count(i.Key)}").ToList();
                if (lines.Count > 0)
                    embed.AddField($"{set.Emoji} {set.Name}{(label.Length > 0 ? $" ({label})" : "")}", string.Join("\n", lines), inline: true);
            }

        var discovered = ItemCatalog.Collectibles.Count(i => byKey.ContainsKey(i.Key));
        embed.AddField("📖 Collection",
            $"{discovered}/{ItemCatalog.Collectibles.Count()} objets découverts · {setsCompleted}/{ItemCatalog.Sets.Count} collections complètes");
        embed.WithFooter($"🪨 {PebbleEconomy.Cailloux(balance)}");
        return embed.Build();
    }
}
