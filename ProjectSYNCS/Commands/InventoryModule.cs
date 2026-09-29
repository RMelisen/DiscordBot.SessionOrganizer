using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Interactions.Autocomplete;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// What a collection page lists: everything, what was found (held or not), or what never was.
public enum CollectionFilter { All, Found, Missing }

// /inventory view's two pages. One page no longer fits once every item has its icon: a custom
// emoji is ~35 characters where Unicode is 2, and someone holding everything passed Discord's 6000
// for a message's embeds (measured ≈ 7 100).
public enum InventoryPage { Items, Wardrobe }

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

    [SlashCommand("cosmetics", "La boutique de cosmétiques pour ton Plynling (nouvelle sélection chaque lundi)")]
    public async Task CosmeticsAsync()
    {
        var (held, balance) = await HeldAsync();
        var (embed, components) = CosmeticCards.BuildShop(DateTimeOffset.UtcNow, Owned(held), balance, null);
        await RespondAsync(embed: embed, components: components, ephemeral: true);
    }

    [SlashCommand("craft", "Fabriquer des cosmétiques avec tes objets de collection")]
    public async Task CraftAsync()
    {
        var (held, balance) = await HeldAsync();
        var (embed, components) = CosmeticCards.BuildCraft(held, Owned(held), balance, null);
        await RespondAsync(embed: embed, components: components, ephemeral: true);
    }

    private async Task<(Dictionary<string, int> Held, long Balance)> HeldAsync() =>
        ((await _inventory.GetAllAsync(Context.Guild.Id, Context.User.Id)).ToDictionary(i => i.Key, i => i.Quantity),
         await _inventory.BalanceAsync(Context.Guild.Id, Context.User.Id));

    // The cosmetics held — at least one; a row at 0 is only « discovered ».
    public static HashSet<string> Owned(IReadOnlyDictionary<string, int> held) =>
        held.Where(kv => kv.Value > 0 && CosmeticCatalog.ByKey(kv.Key) is not null).Select(kv => kv.Key).ToHashSet();

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
        var text = PlynlingText.Gave(Context.User.Id, user.Id, quantity, info.Emoji, ItemCatalog.ClearName(info));
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
            InventoryService.GiveOutcome.NotSellable => PlynlingText.NotSellable,
            _ => PlynlingText.NotEnoughItems,
        }, ephemeral: true);
    }

    [SlashCommand("view", "Ton inventaire : garde-manger, objets trouvés, cailloux")]
    public async Task InventoryAsync()
    {
        var held = await _inventory.GetAllAsync(Context.Guild.Id, Context.User.Id);
        var completions = await _inventory.GetCompletionsAsync(Context.Guild.Id, Context.User.Id);
        var balance = await _inventory.BalanceAsync(Context.Guild.Id, Context.User.Id);
        var (embed, components) = BuildInventoryPage(held, completions.Count, balance, InventoryPage.Items);
        await RespondAsync(embed: embed, components: components, ephemeral: true);
    }

    [SlashCommand("collection", "Le carnet de collection : ce que tu as trouvé et ce qui manque (le tien par défaut)")]
    public async Task CollectionAsync([Summary("user", "De qui (par défaut : toi)")] IUser? user = null)
    {
        var target = user ?? Context.User;
        var held = await _inventory.GetAllAsync(Context.Guild.Id, target.Id);
        var completions = await _inventory.GetCompletionsAsync(Context.Guild.Id, target.Id);
        var (embed, components) = BuildCollectionPage(target.Id, held, completions, OverviewPage, CollectionFilter.All);
        await RespondAsync(embed: embed, components: components, allowedMentions: AllowedMentions.None);
    }

    // ---- the collection book ---------------------------------------------------------------
    //
    // An overview page (every set's progress) and one page per set, picked from a menu — a menu
    // rather than a row of buttons because Discord allows 5 buttons a row, and the overview plus
    // six sets is already seven. A set's page carries a Tout / Trouvés / Manquants filter. The
    // state lives in the custom-ids (nothing secret: it is someone's public book), and every click
    // re-reads the inventory, so the page is never stale. Two verbs for the two controls —
    // `col:set:` and `col:fil:` — so their ids can never collide.

    public const string OverviewPage = "all";

    public static (Embed Embed, MessageComponent Components) BuildCollectionPage(ulong userId,
        IReadOnlyCollection<InventoryItem> held, IReadOnlyCollection<CollectionCompletion> completions,
        string page, CollectionFilter filter)
    {
        var quantities = held.ToDictionary(i => i.Key, i => i.Quantity);
        var done = completions.Select(c => c.SetKey).ToHashSet();
        var set = ItemCatalog.Sets.FirstOrDefault(s => s.Key == page);
        var embed = set is null
            ? BuildOverview(userId, quantities, done)
            : BuildSetPage(userId, set, quantities, done.Contains(set.Key), filter);

        var menu = new SelectMenuBuilder()
            .WithCustomId($"col:set:{userId}:{filter}")
            .AddOption("Aperçu", OverviewPage, "Toutes les collections", new Emoji("📖"), isDefault: set is null);
        foreach (var s in ItemCatalog.Sets)
        {
            var items = ItemCatalog.InSet(s.Key).ToList();
            var have = items.Count(i => quantities.ContainsKey(i.Key));
            menu.AddOption(s.Name, s.Key, done.Contains(s.Key) ? "Complète ✅" : $"{have}/{items.Count} trouvés",
                EmoteMarkup.Parse(s.Emoji), isDefault: s.Key == set?.Key);
        }
        var components = new ComponentBuilder().WithSelectMenu(menu, row: 0);
        if (set is not null)
            foreach (var f in Enum.GetValues<CollectionFilter>())
                components.WithButton(FilterLabel(f), $"col:fil:{userId}:{set.Key}:{f}",
                    f == filter ? ButtonStyle.Primary : ButtonStyle.Secondary, disabled: f == filter, row: 1);
        return (embed, components.Build());
    }

    public static string FilterLabel(CollectionFilter filter) => filter switch
    {
        CollectionFilter.Found => "Trouvés",
        CollectionFilter.Missing => "Manquants",
        _ => "Tout",
    };

    private static Embed BuildOverview(ulong userId, IReadOnlyDictionary<string, int> quantities, IReadOnlySet<string> done)
    {
        var found = ItemCatalog.Collectibles.Count(i => quantities.ContainsKey(i.Key));
        var lines = ItemCatalog.Sets.Select(s =>
        {
            var items = ItemCatalog.InSet(s.Key).ToList();
            var have = items.Count(i => quantities.ContainsKey(i.Key));
            var status = done.Contains(s.Key) ? "✅" : $"{have}/{items.Count} · +{PebbleEconomy.Cailloux(s.Reward)}";
            return $"**{s.Name}**\n`{LevelCardUi.ProgressBar(have, items.Count)}` {status}";
        });
        return new EmbedBuilder()
            .WithTitle("Carnet de collection")
            .WithColor(Color.Purple)
            .WithDescription($"<@{userId}> · **{found}/{ItemCatalog.Collectibles.Count()}** objets trouvés · " +
                             $"{done.Count}/{ItemCatalog.Sets.Count} collections complètes\n\n" + string.Join("\n", lines))
            .WithFooter("Trouve des objets de collection avec /plynling forage, les jeux, les visites " +
                        "ou en les échangeant avec des amis.")
            .Build();
    }

    private static Embed BuildSetPage(ulong userId, CollectionSet set, IReadOnlyDictionary<string, int> quantities,
        bool complete, CollectionFilter filter)
    {
        var items = ItemCatalog.InSet(set.Key).ToList();
        var have = items.Count(i => quantities.ContainsKey(i.Key));
        var status = complete ? "✅ complète" : $"complète : +{PebbleEconomy.Cailloux(set.Reward)}";
        var embed = new EmbedBuilder()
            .WithTitle(set.Name)
            .WithColor(Color.Purple)
            .WithDescription($"<@{userId}> · **{have}/{items.Count}** trouvés · {status}\n" +
                             $"`{LevelCardUi.ProgressBar(have, items.Count)}`");

        bool Shown(ItemInfo i) => filter switch
        {
            CollectionFilter.Found => quantities.ContainsKey(i.Key),
            CollectionFilter.Missing => !quantities.ContainsKey(i.Key),
            _ => true,
        };
        var any = false;
        foreach (var (label, section) in ItemCatalog.Sections(set.Key))
        {
            var lines = section.Where(Shown).Select(i => BookLine(i, quantities, withRarity: label.Length == 0)).ToList();
            if (lines.Count == 0) continue;
            any = true;
            var sectionHave = section.Count(i => quantities.ContainsKey(i.Key));
            var name = label.Length > 0 ? $"{char.ToUpper(label[0])}{label[1..]} — {sectionHave}/{section.Count}" : "Objets";
            embed.AddField(name, string.Join("\n", lines), inline: label.Length > 0);
        }
        if (!any)
            embed.AddField(FilterLabel(filter), filter == CollectionFilter.Missing ? "Rien ne manque ici ✨" : "Rien de trouvé ici pour l'instant.");
        return embed.Build();
    }

    // One item in the book. Found and held: its quantity; found once but traded or sold since:
    // « plus en stock », since it still counts for the set. Never found: « ??? » with only its
    // rarity and season, since that is when to look. A page split by rarity leaves the rarity out
    // of each line — the field says it, and the line is kept short enough to fit 12 in 1024.
    public static string BookLine(ItemInfo item, IReadOnlyDictionary<string, int> quantities, bool withRarity)
    {
        var rarity = withRarity ? $" · {ItemCatalog.RarityLabel(item.Rarity)}" : "";
        var season = item.Season == Season.None ? "" : $" · {ItemCatalog.SeasonLabel(item.Season)}";
        if (!quantities.TryGetValue(item.Key, out var count))
            return $"❔ ???{rarity}{season}";
        return count > 0
            ? $"{item.Emoji} {item.Name} ×{count}{rarity}"
            : $"{item.Emoji} {item.Name} · plus en stock{rarity}";
    }

    // Static and Context-free, like every other builder here, so its size is checkable. Both pages
    // share the title and the footer; the buttons carry one id per page (inv:page:{page}), so the
    // two can never collide, and the page on screen is the disabled one.
    public static (Embed Embed, MessageComponent Components) BuildInventoryPage(
        IReadOnlyCollection<InventoryItem> held, int setsCompleted, long balance, InventoryPage page)
    {
        var byKey = held.ToDictionary(i => i.Key);
        int Count(string key) => byKey.TryGetValue(key, out var row) ? row.Quantity : 0;

        var embed = new EmbedBuilder()
            .WithTitle(page == InventoryPage.Items ? "Ton inventaire" : "Ton inventaire — garde-robe")
            .WithColor(Color.Purple)
            .WithFooter($"🪨 {PebbleEconomy.Cailloux(balance)}");

        if (page == InventoryPage.Items)
        {
            var pantry = string.Join("\n", PlynlingCatalog.Foods.Select(f =>
            {
                var item = ItemCatalog.ByKey(ItemCatalog.FoodKey(f.Food))!;
                return $"{item.Emoji} {item.Name} : **{Count(item.Key)}**";
            }));
            embed.AddField("Garde-manger", pantry + "\n-# Nourrir puise ici d'abord : 1 pour ton Plynling, 2 pour celui d'un autre.");

            // One field per set (per rarity for a big one), holding only what is in hand.
            foreach (var set in ItemCatalog.Sets)
                foreach (var (label, section) in ItemCatalog.Sections(set.Key))
                {
                    var lines = section.Where(i => Count(i.Key) > 0).Select(i => $"{i.Emoji} {i.Name} ×{Count(i.Key)}").ToList();
                    if (lines.Count > 0)
                        embed.AddField($"{set.Name}{(label.Length > 0 ? $" ({label})" : "")}", string.Join("\n", lines), inline: true);
                }

            var discovered = ItemCatalog.Collectibles.Count(i => byKey.ContainsKey(i.Key));
            embed.AddField("Collection",
                $"{discovered}/{ItemCatalog.Collectibles.Count()} objets découverts · {setsCompleted}/{ItemCatalog.Sets.Count} collections complètes");
        }
        else
        {
            // What is held, one field per slot.
            var any = false;
            foreach (var slot in Enum.GetValues<CosmeticSlot>())
            {
                var owned = CosmeticCatalog.InSlot(slot).Where(c => Count(c.Key) > 0).Select(c => $"{c.Emoji} {CosmeticCatalog.ShortName(c)}").ToList();
                if (owned.Count == 0) continue;
                any = true;
                embed.AddField(CosmeticCatalog.SlotPlural(slot), string.Join(" · ", owned));
            }
            embed.WithDescription(any
                ? "Pour habiller ton Plynling : `/plynling wardrobe`."
                : "Aucun cosmétique pour l'instant. La boutique : `/inventory cosmetics`.");
        }

        var components = new ComponentBuilder()
            .WithButton("🎒 Objets", $"inv:page:{InventoryPage.Items}", page == InventoryPage.Items ? ButtonStyle.Primary : ButtonStyle.Secondary,
                disabled: page == InventoryPage.Items)
            .WithButton("👗 Garde-robe", $"inv:page:{InventoryPage.Wardrobe}", page == InventoryPage.Wardrobe ? ButtonStyle.Primary : ButtonStyle.Secondary,
                disabled: page == InventoryPage.Wardrobe)
            .Build();
        return (embed.Build(), components);
    }
}
