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
public class InventoryModule : SyncsModuleBase
{
    private readonly InventoryService _inventory;
    private readonly TradeOffers _trades;
    private readonly CosmeticService _cosmetics;
    private readonly PlynlingService _plynlings;

    public InventoryModule(InventoryService inventory, TradeOffers trades, CosmeticService cosmetics, PlynlingService plynlings)
    {
        _inventory = inventory;
        _trades = trades;
        _cosmetics = cosmetics;
        _plynlings = plynlings;
    }

    // The cosmetics screen (CosmeticCards), opened on the shop: wardrobe, shop and recipes in one
    // place, with a try-on. /plynling wardrobe opens the same screen on the wardrobe.
    [SlashCommand("cosmetics", "Cosmétiques : essaie, achète, fabrique et habille ton Plynling")]
    public async Task CosmeticsAsync()
    {
        await DeferAsync(ephemeral: true);
        var now = DateTimeOffset.UtcNow;
        var data = await CosmeticCards.LoadAsync(_plynlings, _cosmetics, Context.Guild.Id, Context.User.Id, now);
        await Context.Interaction.FollowupCardAsync(CosmeticCards.Build(new CosmeticView(CosmeticTab.Shop), data, now, null), ephemeral: true);
    }

    private async Task<(Dictionary<string, int> Held, long Balance)> HeldAsync() =>
        ((await _inventory.GetAllAsync(Context.Guild.Id, Context.User.Id)).ToDictionary(i => i.Key, i => i.Quantity),
         await _inventory.BalanceAsync(Context.Guild.Id, Context.User.Id));

    // The shop card: foods, soap and medicine, bought by picking a line then a quantity
    // (ShopCards, ShopComponentHandler).
    [SlashCommand("shop", "La boutique : nourriture, savon et médicaments pour ton garde-manger (−10 % dès 5)")]
    public async Task ShopAsync()
    {
        await DeferAsync(ephemeral: true);
        var (held, balance) = await HeldAsync();
        var (embed, components) = ShopCards.BuildShop(held, balance, null, null);
        await FollowupAsync(embed: embed, components: components, ephemeral: true);
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
        var g = Context.Guild.Id;
        var fromGive = 0;
        var toWant = 0;
        string? refusal =
            user.Id == Context.User.Id ? PlynlingText.TradeSelf
            : user.IsBot ? PlynlingText.GiveBot
            : ItemCatalog.ByKey(give) is null || ItemCatalog.ByKey(want) is null ? PlynlingText.UnknownItem
            : give == want ? PlynlingText.TradeSameItem
            : (fromGive = await _inventory.CountAsync(g, Context.User.Id, give)) < giveQuantity ? PlynlingText.NotEnoughItems
            : (toWant = await _inventory.CountAsync(g, user.Id, want)) < wantQuantity ? PlynlingText.TradeTheyLack(user.Id)
            : null;
        if (refusal is not null)
        {
            await RespondAsync(refusal, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        // What each side already holds of both items, as of the offer — the recipient's « do I
        // need it, can I spare mine » at a glance.
        var holdings = PlynlingText.TradeHolding(ItemCatalog.ByKey(give)!, user.Id, await _inventory.CountAsync(g, user.Id, give), Context.User.Id, fromGive)
                       + "\n" + PlynlingText.TradeHolding(ItemCatalog.ByKey(want)!, user.Id, toWant, Context.User.Id, await _inventory.CountAsync(g, Context.User.Id, want));

        var offer = _trades.Open(g, Context.User.Id, user.Id, give, giveQuantity, want, wantQuantity,
            DateTimeOffset.UtcNow, Random.Shared);
        var (giveText, wantText) = TradeSides(offer);
        var buttons = new ComponentBuilder()
            .WithButton("Accepter", $"plyn:tacc:{offer.Id}", ButtonStyle.Success, new Emoji("🤝"))
            .WithButton("Refuser", $"plyn:tdec:{offer.Id}", ButtonStyle.Danger)
            .Build();
        // Public, pinging only the person being asked.
        await RespondAsync(PlynlingText.TradeOffered(offer.FromId, offer.ToId, giveText, wantText, holdings, offer.ExpiresAt),
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

    //
    // A completed set's page also carries « Déposer » (`col:dep:`, a third verb): it spends one of
    // each item for another completion — the way up the tiers. It shows only when the page says a
    // turn-in is possible (completed, one of each held); the handler re-checks the clicker and
    // everything else.
    public static (Embed Embed, MessageComponent Components) BuildCollectionPage(ulong userId,
        IReadOnlyCollection<InventoryItem> held, IReadOnlyCollection<CollectionCompletion> completions,
        string page, CollectionFilter filter)
    {
        var quantities = held.ToDictionary(i => i.Key, i => i.Quantity);
        var done = completions.ToDictionary(c => c.SetKey, c => c.Count);
        var set = ItemCatalog.Sets.FirstOrDefault(s => s.Key == page);
        var embed = set is null
            ? BuildOverview(userId, quantities, done)
            : BuildSetPage(userId, set, quantities, done.GetValueOrDefault(set.Key), filter);

        var menu = new SelectMenuBuilder()
            .WithCustomId($"col:set:{userId}:{filter}")
            .AddOption("Aperçu", OverviewPage, "Toutes les collections", new Emoji("📖"), isDefault: set is null);
        foreach (var s in ItemCatalog.Sets)
        {
            var items = ItemCatalog.InSet(s.Key).ToList();
            var have = items.Count(i => quantities.ContainsKey(i.Key));
            var description = done.TryGetValue(s.Key, out var count)
                ? $"Complète · {CollectionTiers.Label(CollectionTiers.TierFor(count))}"
                : $"{have}/{items.Count} trouvés";
            menu.AddOption(s.Name, s.Key, description, EmoteMarkup.Parse(s.Emoji), isDefault: s.Key == set?.Key);
        }
        var components = new ComponentBuilder().WithSelectMenu(menu, row: 0);
        if (set is not null)
        {
            foreach (var f in Enum.GetValues<CollectionFilter>())
                components.WithButton(FilterLabel(f), $"col:fil:{userId}:{set.Key}:{f}",
                    f == filter ? ButtonStyle.Primary : ButtonStyle.Secondary, disabled: f == filter, row: 1);
            if (done.ContainsKey(set.Key) && CanTurnIn(set.Key, quantities))
                components.WithButton($"Déposer la collection (+{PebbleEconomy.Cailloux(set.Reward)})",
                    $"col:dep:{userId}:{set.Key}:{filter}", ButtonStyle.Primary, new Emoji("📥"), row: 2);
        }
        return (embed, components.Build());
    }

    // One of each item of the set in hand — what a turn-in spends.
    public static bool CanTurnIn(string setKey, IReadOnlyDictionary<string, int> quantities) =>
        ItemCatalog.InSet(setKey).All(i => quantities.GetValueOrDefault(i.Key) > 0);

    // A completed set's standing: badge, tier, completions and the next step up.
    public static string TierLine(CollectionSet set, int count)
    {
        var tier = CollectionTiers.TierFor(count);
        var next = CollectionTiers.Next(count) is { } n ? $" · prochain : {CollectionTiers.Label(n.Tier)} à {n.At}" : " · palier maximum";
        return $"{CollectionTiers.Badge(set, tier)} **{CollectionTiers.Label(tier)}** · complétée {count} fois{next}";
    }

    public static string FilterLabel(CollectionFilter filter) => filter switch
    {
        CollectionFilter.Found => "Trouvés",
        CollectionFilter.Missing => "Manquants",
        _ => "Tout",
    };

    private static Embed BuildOverview(ulong userId, IReadOnlyDictionary<string, int> quantities, IReadOnlyDictionary<string, int> done)
    {
        var found = ItemCatalog.Collectibles.Count(i => quantities.ContainsKey(i.Key));
        var lines = ItemCatalog.Sets.Select(s =>
        {
            var items = ItemCatalog.InSet(s.Key).ToList();
            var have = items.Count(i => quantities.ContainsKey(i.Key));
            if (!done.TryGetValue(s.Key, out var count))
                return $"**{s.Name}**\n{RewardBar(s, have, items.Count)}";
            return $"**{s.Name}**\n{TierLine(s, count)}\n{TurnInBar(s, items, quantities)}";
        });
        // A blank line between sets, so each reads as its own block.
        return new EmbedBuilder()
            .WithTitle("Carnet de collection")
            .WithColor(Color.Purple)
            .WithDescription($"<@{userId}> · **{found}/{ItemCatalog.Collectibles.Count()}** objets trouvés · " +
                             $"{done.Count}/{ItemCatalog.Sets.Count} collections complètes\n\n" + string.Join("\n\n", lines))
            .WithFooter("Trouve des objets de collection avec /plynling forage, les jeux, les visites " +
                        "ou en les échangeant avec des amis.")
            .Build();
    }

    // Where a set's items are found (ForagePlaces): the place holding most of them, then the others —
    // places only, never item names, since the book hides what was never found (« ??? »).
    // « -# 🌾 Se trouve surtout au Communal, aussi à la Sommeille et à la Lisière ».
    private static string WhereLine(IReadOnlyList<ItemInfo> items)
    {
        var places = items.SelectMany(ForagePlaces.Of).GroupBy(p => p).OrderByDescending(g => g.Count()).Select(g => g.Key).ToList();
        if (places.Count == 0) return "";
        var others = places.Skip(1).Select(ForagePlaces.At).ToList();
        var also = others.Count switch
        {
            0 => "",
            1 => $", aussi {others[0]}",
            _ => $", aussi {string.Join(", ", others.SkipLast(1))} et {others[^1]}",
        };
        return $"-# {places[0].Emoji} Se trouve {(others.Count > 0 ? "surtout " : "")}{ForagePlaces.At(places[0])}{also} — `/plynling forage place:`";
    }

    // « `██████░░` 5/8 · +100 cailloux »: how far along the next payout is.
    private static string RewardBar(CollectionSet set, int have, int total) =>
        $"`{LevelCardUi.ProgressBar(have, total)}` {have}/{total} · +{PebbleEconomy.Cailloux(set.Reward)}";

    // A completed set's bar counts what is in hand toward the next turn-in (one of each), not what
    // was ever found — that one is full for good. Kept past Diamant: a turn-in still pays.
    private static string TurnInBar(CollectionSet set, IReadOnlyList<ItemInfo> items,
        IReadOnlyDictionary<string, int> quantities) =>
        RewardBar(set, items.Count(i => quantities.GetValueOrDefault(i.Key) > 0), items.Count);

    private static Embed BuildSetPage(ulong userId, CollectionSet set, IReadOnlyDictionary<string, int> quantities,
        int completions, CollectionFilter filter)
    {
        var items = ItemCatalog.InSet(set.Key).ToList();
        var have = items.Count(i => quantities.ContainsKey(i.Key));
        // A blank line under the badge block, before where to look, so the header breathes.
        var header = completions > 0
            ? $"<@{userId}> · **{have}/{items.Count}** trouvés · complète\n{TierLine(set, completions)}\n" +
              TurnInBar(set, items, quantities)
            : $"<@{userId}> · **{have}/{items.Count}** trouvés · complète : +{PebbleEconomy.Cailloux(set.Reward)}\n" +
              $"`{LevelCardUi.ProgressBar(have, items.Count)}`";
        var embed = new EmbedBuilder()
            .WithTitle(set.Name)
            .WithColor(Color.Purple)
            .WithDescription(WhereLine(items) is { Length: > 0 } where ? $"{header}\n\n{where}" : header);

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
            embed.AddField(FilterLabel(filter), filter == CollectionFilter.Missing ? $"Rien ne manque ici {Emotes.Sparkle}" : "Rien de trouvé ici pour l'instant.");
        return embed.Build();
    }

    // One item in the book. Found: its quantity, « ×0 » once traded or sold, since it still counts
    // for the set. Never found: « ??? » with only its
    // rarity and season, since that is when to look. A page split by rarity leaves the rarity out
    // of each line — the field says it, and the line is kept short enough to fit 12 in 1024.
    public static string BookLine(ItemInfo item, IReadOnlyDictionary<string, int> quantities, bool withRarity)
    {
        var rarity = withRarity ? $" · {ItemCatalog.RarityLabel(item.Rarity)}" : "";
        var season = item.Season == Season.None ? "" : $" · {ItemCatalog.SeasonLabel(item.Season)}";
        if (!quantities.TryGetValue(item.Key, out var count))
            return $"❔ ???{rarity}{season}";
        return $"{item.Emoji} {item.Name} ×{count}{rarity}";
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
            foreach (var key in new[] { ItemCatalog.SoapKey, ItemCatalog.MedicineKey })
            {
                var care = ItemCatalog.ByKey(key)!;
                pantry += $"\n{care.Emoji} {care.Name} : **{Count(care.Key)}**";
            }
            embed.AddField("Garde-manger", pantry + "\n-# Nourrir puise ici d'abord : 1 pour ton Plynling, 2 pour celui d'un autre. " +
                "« Soigner » aussi, 1 médicament ; « Laver » prend 1 savon quand le bain gratuit attend.");

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
            var owned = CosmeticCatalog.SlotOrder
                .Select(slot => (Slot: slot, Items: CosmeticCatalog.InSlot(slot).Where(c => Count(c.Key) > 0).ToList()))
                .Where(s => s.Items.Count > 0)
                .ToList();
            var description = owned.Count > 0
                ? "Pour les essayer et habiller ton Plynling : `/inventory cosmetics`."
                : "Aucun cosmétique pour l'instant. La boutique : `/inventory cosmetics`.";
            // One line for the collections, not a tag per item.
            var collections = CosmeticCollections.Progress(key => Count(key) > 0);

            // What is held, one field per slot, cut into « (suite) » fields past 1024. A wardrobe too big
            // for one embed even so (every icon uploaded, ~100 cosmetics) keeps the icons, drops the names.
            List<(string Name, string Value)> Fields(bool names) => owned
                .SelectMany(s => WardrobeFields(CosmeticCatalog.SlotPlural(s.Slot),
                    s.Items.Select(c => names ? $"{c.Emoji} {CosmeticCatalog.ShortName(c)}" : c.Emoji), names ? " · " : " "))
                .ToList();
            var fields = Fields(names: true);
            var length = description.Length + fields.Sum(f => f.Name.Length + f.Value.Length)
                         + (collections is null ? 0 : "Collections".Length + collections.Length);
            if (length > WardrobeBudget) fields = Fields(names: false);

            foreach (var (name, value) in fields) embed.AddField(name, value);
            embed.WithDescription(description);
            if (collections is not null) embed.AddField("Collections", collections);
        }

        var components = new ComponentBuilder()
            .WithButton("🎒 Objets", $"inv:page:{InventoryPage.Items}", page == InventoryPage.Items ? ButtonStyle.Primary : ButtonStyle.Secondary,
                disabled: page == InventoryPage.Items)
            .WithButton("👗 Garde-robe", $"inv:page:{InventoryPage.Wardrobe}", page == InventoryPage.Wardrobe ? ButtonStyle.Primary : ButtonStyle.Secondary,
                disabled: page == InventoryPage.Wardrobe)
            .Build();
        return (embed.Build(), components);
    }

    // The garde-robe page's room for its fields and description: Discord's 6000 less the title and
    // the footer (« Ton inventaire — garde-robe », the balance), with margin.
    private const int WardrobeBudget = EmbedBuilder.MaxEmbedLength - 200;

    // One slot's entries as fields of at most 1024, the later ones named « titres (suite) ».
    private static IEnumerable<(string Name, string Value)> WardrobeFields(string name, IEnumerable<string> entries, string separator)
    {
        var value = "";
        var first = true;
        foreach (var entry in entries)
        {
            if (value.Length > 0 && value.Length + separator.Length + entry.Length > EmbedFieldBuilder.MaxFieldValueLength)
            {
                yield return (first ? name : $"{name} (suite)", value);
                first = false;
                value = "";
            }
            value = value.Length == 0 ? entry : value + separator + entry;
        }
        if (value.Length > 0) yield return (first ? name : $"{name} (suite)", value);
    }
}
