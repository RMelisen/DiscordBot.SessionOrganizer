using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /inventory shop: every line of ItemCatalog.Shop on one ephemeral card, static and Context-free so
// its size is checkable without a gateway. Picking a line in the select redraws the card with that
// line's quantity buttons; each purchase redraws it again with a one-line notice.
//
// Verbs: `shop:pick` (the select) and `shop:buy:{key}:{quantity}` — the quantities differ, so the
// three buttons can never collide.
public static class ShopCards
{
    public static readonly int[] Quantities = { 1, 5, 10 };

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
            .WithFooter("Tout va dans ton garde-manger : la carte de ton Plynling puise dedans.");

        var menu = new SelectMenuBuilder()
            .WithCustomId("shop:pick")
            .WithPlaceholder("Choisir un article…");
        foreach (var (item, price) in ItemCatalog.Shop)
            menu.AddOption($"{item.Name} — {PebbleEconomy.Cailloux(price)}", item.Key, Effect(item),
                EmoteMarkup.Parse(item.Emoji), isDefault: item.Key == picked);
        var components = new ComponentBuilder().WithSelectMenu(menu, row: 0);

        if (picked is not null && ItemCatalog.ShopUnitPrice(picked) is { } unit)
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
}
