using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// The three cosmetics screens — the shop, the recipes, the wardrobe — as static, Context-free
// builders, so their sizes are checkable without a gateway. All three are ephemeral messages
// that a pick in their own select redraws in place, with a one-line notice of what happened.
//
// One verb per select: `cos:buy:{week}`, `cos:craft`, `cos:wear:{slot}` — the four wardrobe
// selects differ by slot, so no two ids on a message can collide.
public static class CosmeticCards
{
    // ---- the shop ------------------------------------------------------------------------------

    public static (Embed Embed, MessageComponent Components) BuildShop(DateTimeOffset now, IReadOnlySet<string> owned, long balance, string? notice)
    {
        var shop = CosmeticCatalog.Shop(now);
        string Line(CosmeticInfo c) =>
            $"{c.Emoji} **{CosmeticCatalog.Label(c)}** · {SourceLabel(c)} · {PebbleEconomy.Cailloux(c.Price)}" + (owned.Contains(c.Key) ? " ✅" : "");

        var embed = new EmbedBuilder()
            .WithTitle("Boutique de cosmétiques")
            .WithColor(Color.Purple)
            .WithDescription((notice is null ? "" : notice + "\n\n") +
                             $"Tu as **{PebbleEconomy.Cailloux(balance)}** · nouvelle sélection <t:{CosmeticCatalog.NextRotation(now).ToUnixTimeSeconds()}:R>")
            .AddField("Toujours", string.Join("\n", shop.Where(c => c.Source == CosmeticSource.Basic).Select(Line)))
            .AddField("Cette semaine", string.Join("\n", shop.Where(c => c.Source == CosmeticSource.Rotating).Select(Line)));
        var seasonal = shop.Where(c => c.Source == CosmeticSource.Seasonal).ToList();
        if (seasonal.Count > 0) embed.AddField("De saison", string.Join("\n", seasonal.Select(Line)));
        embed.WithFooter("Ce que tu achètes t'appartient : tu pourras en habiller tous tes Plynlings (/plynling wardrobe).");

        var components = new ComponentBuilder();
        var buyable = shop.Where(c => !owned.Contains(c.Key)).ToList();
        if (buyable.Count > 0)
        {
            var menu = new SelectMenuBuilder()
                .WithCustomId($"cos:buy:{CosmeticCatalog.WeekKey(now)}")
                .WithPlaceholder("Acheter…");
            foreach (var c in buyable)
                menu.AddOption($"{CosmeticCatalog.Label(c)} — {PebbleEconomy.Cailloux(c.Price)}", c.Key,
                    $"{CosmeticCatalog.SlotLabel(c.Slot)} · {SourceLabel(c)}", new Emoji(c.Emoji));
            components.WithSelectMenu(menu);
        }
        return (embed.Build(), components.Build());
    }

    private static string SourceLabel(CosmeticInfo c) => c.Source switch
    {
        CosmeticSource.Basic => "basique",
        CosmeticSource.Seasonal => $"de saison ({ItemCatalog.SeasonLabel(c.Season)})",
        CosmeticSource.Crafted => "à fabriquer",
        _ => ItemCatalog.RarityLabel(c.Rarity),
    };

    // ---- the recipes ---------------------------------------------------------------------------

    public static (Embed Embed, MessageComponent Components) BuildCraft(IReadOnlyDictionary<string, int> held, IReadOnlySet<string> owned, long balance, string? notice)
    {
        var recipes = CosmeticCatalog.All.Where(c => c.Source == CosmeticSource.Crafted).ToList();
        var embed = new EmbedBuilder()
            .WithTitle("Fabrication")
            .WithColor(Color.Purple)
            .WithDescription((notice is null ? "" : notice + "\n\n") +
                             "Transforme tes objets de collection en cosmétiques. Les objets utilisés disparaissent, " +
                             "mais restent découverts dans ton carnet.");
        foreach (var c in recipes)
        {
            var lines = c.Recipe.Select(r =>
            {
                var item = ItemCatalog.ByKey(r.ItemKey)!;
                var have = held.GetValueOrDefault(r.ItemKey);
                return $"{item.Emoji} {ItemCatalog.ClearName(item)} {Math.Min(have, r.Count)}/{r.Count} {(have >= r.Count ? "✅" : "❌")}";
            }).ToList();
            lines.Add($"🪨 Cailloux {Math.Min(balance, c.Price)}/{c.Price} {(balance >= c.Price ? "✅" : "❌")}");
            embed.AddField($"{c.Emoji} {CosmeticCatalog.Label(c)}" + (owned.Contains(c.Key) ? " · déjà à toi ✅" : ""), string.Join("\n", lines), inline: true);
        }

        var components = new ComponentBuilder();
        var craftable = recipes.Where(c => !owned.Contains(c.Key)).ToList();
        if (craftable.Count > 0)
        {
            var menu = new SelectMenuBuilder().WithCustomId("cos:craft").WithPlaceholder("Fabriquer…");
            foreach (var c in craftable)
            {
                var ready = c.Recipe.All(r => held.GetValueOrDefault(r.ItemKey) >= r.Count) && balance >= c.Price;
                menu.AddOption(CosmeticCatalog.Label(c), c.Key, ready ? "Tout est prêt ✅" : "Il manque quelque chose", new Emoji(c.Emoji));
            }
            components.WithSelectMenu(menu);
        }
        return (embed.Build(), components.Build());
    }

    // ---- the wardrobe --------------------------------------------------------------------------

    public const string NoneValue = "none";

    public static (Embed Embed, MessageComponent Components) BuildWardrobe(Plynling p, IReadOnlyList<CosmeticInfo> owned, string? notice)
    {
        string Shown(CosmeticInfo c) => c.Slot switch
        {
            CosmeticSlot.Title => CosmeticCatalog.TitleFor(c, p.Gender),
            _ => CosmeticCatalog.ShortName(c),
        };

        var embed = new EmbedBuilder()
            .WithTitle($"Garde-robe de {PlynlingCardUi.SafeName(p.Name)}")
            .WithColor(Color.Purple)
            .WithDescription((notice is null ? "" : notice + "\n\n") +
                             "Choisis ce que ton Plynling porte. Tes cosmétiques restent à toi, même s'il part.");
        var components = new ComponentBuilder();
        var row = 0;
        foreach (var slot in Enum.GetValues<CosmeticSlot>())
        {
            var worn = CosmeticSlots.Worn(p, slot);
            embed.AddField(CosmeticCatalog.SlotLabel(slot), worn is null ? "—" : $"{worn.Emoji} {Shown(worn)}", inline: true);

            var menu = new SelectMenuBuilder()
                .WithCustomId($"cos:wear:{slot}")
                .WithPlaceholder(CosmeticCatalog.SlotLabel(slot))
                .AddOption("Aucun", NoneValue, isDefault: worn is null);
            foreach (var c in owned.Where(c => c.Slot == slot))
                menu.AddOption(Shown(c), c.Key, emote: new Emoji(c.Emoji), isDefault: c.Key == worn?.Key);
            components.WithSelectMenu(menu, row++);
        }
        return (embed.Build(), components.Build());
    }

    // The one-line notice after a buy, a craft or a change of clothes.
    public static string Notice(CosmeticOutcome outcome, CosmeticInfo? c, long price = 0) => outcome switch
    {
        CosmeticOutcome.Done => $"✅ {c?.Emoji} **{(c is null ? "" : CosmeticCatalog.Label(c))}** est à toi !",
        CosmeticOutcome.AlreadyOwned => "Tu l'as déjà.",
        CosmeticOutcome.TooPoor => $"❌ Il te faut {PebbleEconomy.Cailloux(price)}.",
        CosmeticOutcome.NotInShop => "❌ Cet article n'est plus en boutique — rouvre `/inventory cosmetics`.",
        CosmeticOutcome.MissingIngredients => "❌ Il te manque des objets pour cette recette.",
        CosmeticOutcome.NotOwned => "❌ Tu ne l'as pas (ou plus).",
        CosmeticOutcome.NoPlynling => PlynlingText.NoPlynling,
        _ => PlynlingText.UnknownItem,
    };
}
