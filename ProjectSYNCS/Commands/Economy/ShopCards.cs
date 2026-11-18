using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /inventory shop: every line of ItemCatalog.Shop on one ephemeral card, static and Context-free so
// its size is checkable without a gateway. Picking a line in the select redraws the card with that
// line's quantity buttons; each purchase redraws it again with a one-line notice.
//
// Verbs: `shop:pick` (the select), `shop:buy:{key}:{quantity}` — the quantities differ, so the
// three buttons can never collide — and `shop:pack:{quantity}` for the collection packs, which are
// opened at once rather than stored (ItemCatalog.PackKey is the select's value, never an item).
public static class ShopCards
{
    public static readonly int[] Quantities = { 1, 5, 10 };

    private const string PackEmoji = "🎁";
    private const string PackName = "Sachet de collection";
    private static readonly string PackEffect = $"{ItemCatalog.PackSize} objets de collection au hasard, ouverts tout de suite";

    public static (Embed Embed, MessageComponent Components) BuildShop(
        IReadOnlyDictionary<string, int> held, long balance, string? picked, string? notice)
    {
        int Held(string key) => held.GetValueOrDefault(key);
        string Line((ItemInfo Item, long Price) s) =>
            $"{(s.Item.Key == picked ? "▸ " : "")}{s.Item.Emoji} **{s.Item.Name}** · {PebbleEconomy.Cailloux(s.Price)} · " +
            $"tu en as **{Held(s.Item.Key)}**\n-# {Effect(s.Item)}";

        var embed = new EmbedBuilder()
            .WithTitle("Boutique")
            .WithColor(Color.Purple)
            .WithDescription((notice is null ? "" : notice + "\n\n") +
                             $"Tu as **{PebbleEconomy.Cailloux(balance)}** · −10 % dès 5")
            .AddField("Nourriture", string.Join("\n", ItemCatalog.Shop.Where(s => s.Item.Kind == ItemKind.Food).Select(Line)))
            .AddField("Soins", string.Join("\n", ItemCatalog.Shop.Where(s => s.Item.Kind == ItemKind.Care).Select(Line)))
            .AddField("Sachets",
                $"{(picked == ItemCatalog.PackKey ? "▸ " : "")}{PackEmoji} **{PackName}** · {PebbleEconomy.Cailloux(ItemCatalog.PackPrice)}\n" +
                $"-# {PackEffect}, sans remise")
            .WithFooter("La nourriture et les soins vont dans ton garde-manger ; les sachets s'ouvrent tout de suite.");

        var menu = new SelectMenuBuilder()
            .WithCustomId("shop:pick")
            .WithPlaceholder("Choisir un article…");
        foreach (var (item, price) in ItemCatalog.Shop)
            menu.AddOption($"{item.Name} — {PebbleEconomy.Cailloux(price)}", item.Key, Effect(item),
                EmoteMarkup.Parse(item.Emoji), isDefault: item.Key == picked);
        menu.AddOption($"{PackName} — {PebbleEconomy.Cailloux(ItemCatalog.PackPrice)}", ItemCatalog.PackKey, PackEffect,
            new Emoji(PackEmoji), isDefault: picked == ItemCatalog.PackKey);
        var components = new ComponentBuilder().WithSelectMenu(menu, row: 0);

        if (picked == ItemCatalog.PackKey)
            foreach (var quantity in ItemCatalog.PackQuantities)
            {
                var price = ItemCatalog.PackPrice * quantity;
                components.WithButton($"×{quantity} — {PebbleEconomy.Cailloux(price)}",
                    $"shop:pack:{quantity}", ButtonStyle.Success, disabled: price > balance, row: 1);
            }
        else if (picked is not null && ItemCatalog.ShopUnitPrice(picked) is { } unit)
            foreach (var quantity in Quantities)
            {
                var price = ItemCatalog.BulkPrice(unit, quantity);
                components.WithButton($"×{quantity} — {PebbleEconomy.Cailloux(price)}{(quantity >= 5 ? " (−10 %)" : "")}",
                    $"shop:buy:{picked}:{quantity}", ButtonStyle.Success, disabled: price > balance, row: 1);
            }

        return (embed.Build(), components.Build());
    }

    // What one is for, on its shop line and in its select option (100 characters at most there).
    private static string Effect(ItemInfo item) => item.Key switch
    {
        ItemCatalog.SoapKey => "Un bain de plus quand « Laver » attend son tour",
        ItemCatalog.MedicineKey => "« Soigner » quand il est malade, une dose par jour",
        _ when item.Food is { } food => PlynlingCardUi.FoodEffect(PlynlingCatalog.Info(food)),
        _ => "",
    };

    public static string Notice(InventoryService.ShopOutcome outcome, ItemInfo? item, int quantity, long price, long balance) => outcome switch
    {
        InventoryService.ShopOutcome.Bought =>
            PlynlingText.Bought(quantity, $"{item!.Emoji} {item.Name}", price, balance, discounted: quantity >= 5),
        InventoryService.ShopOutcome.TooPoor =>
            $"{PlynlingText.ShopTooPoor} ({PebbleEconomy.Cailloux(price)}, tu en as {PebbleEconomy.Cailloux(balance)})",
        _ => PlynlingText.UnknownItem,
    };

    // What opening packs found, the same item gathered as « 2 × », then each set a find completed.
    // In the embed's description, which holds 4 096: five packs are 15 finds at most.
    public static string PackNotice(PackResult result)
    {
        if (result.Outcome == PackOutcome.TooPoor)
            return $"{PlynlingText.ShopTooPoor} ({PebbleEconomy.Cailloux(result.Price)}, tu en as {PebbleEconomy.Cailloux(result.Balance)})";
        if (result.Outcome != PackOutcome.Opened) return PlynlingText.UnknownItem;

        var packs = result.Finds.Count / ItemCatalog.PackSize;
        var found = result.Finds
            .GroupBy(f => f.Item.Key)
            .Select(g => (g.Count() > 1 ? $"{g.Count()} × " : "") + PlynlingText.ItemLabel(g.First().Item));
        var sets = result.Finds.SelectMany(f => f.Completed).Select(set =>
            $"\nTu as complété la collection **{set.Emoji} {set.Name}** ! +{PebbleEconomy.Cailloux(set.Reward)} · " +
            $"badge {CollectionTiers.Badge(set, CollectionTier.Bronze)} **Bronze**");
        return $"{PackEmoji} Tu ouvres {(packs > 1 ? $"{packs} sachets" : "un sachet")} ({PebbleEconomy.Cailloux(result.Price)}) : " +
               string.Join(", ", found) + "." + string.Concat(sets) +
               $"\n-# Il te reste {PebbleEconomy.Cailloux(result.Balance)}.";
    }
}
