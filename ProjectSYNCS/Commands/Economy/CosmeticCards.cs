using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// The cosmetics screen's three tabs.
public enum CosmeticTab { Wardrobe, Shop, Craft }

// Where the screen stands: its tab, the wardrobe's slot, and the item picked in the shop or the
// recipes — tried on in the preview, nothing bought. All of it rides in the custom-ids, so a
// restart loses nothing.
public sealed record CosmeticView(CosmeticTab Tab, CosmeticSlot Slot = CosmeticSlot.Theme, string? Picked = null);

// What the screen is drawn from: the person's living Plynling (null without one), everything they
// hold by key, their cailloux, and what the card's heading shows besides the Plynling.
public sealed record CosmeticScreenData(
    Plynling? Plynling, IReadOnlyDictionary<string, int> Held, long Balance, string? PartnerName = null, string? Epithet = null)
{
    public bool Owns(CosmeticInfo c) => Held.GetValueOrDefault(c.Key) > 0;
}

// The cosmetics screen — wardrobe, shop and recipes in one ephemeral Components V2 message, opened
// by /inventory cosmetics (on the shop) and /plynling wardrobe (on the wardrobe), redrawn in place by
// CosmeticComponentHandler. Its top is the Plynling's own card head (PlynlingModule.CardHead) with
// what is picked put on a copy of its Outfit: the try-on is the real card, never a look-alike.
// Static and Context-free, so the checks measure it.
//
// One verb per control, so no id repeats on a message: cosm:tab:{tab}, cosm:slot:{slot},
// cosm:pick:{tab}:{week} (the shop's select; the wardrobe and the recipes number theirs instead, as a
// slot or the recipes need more than one; « Aucun » is none:{slot}), cosm:buy:{week}:{key},
// cosm:buyw:{week}:{key}, cosm:make:{key}, cosm:makew:{key}, cosm:wear:{tab}:{key}, cosm:back:{tab},
// cosm:style:{choice}:{key} (the worn accessory's colour or position).
// The prefix is new (the old screens used cos:), so no handler here answers an old screen's select.
public static class CosmeticCards
{
    public const string NonePrefix = "none:";

    // Discord's cap on a select's options.
    private const int MaxOptions = 25;

    // The notice when « Aucun » takes a slot off, agreeing with the slot's label (« Aura » is feminine).
    public static string TakenOff(CosmeticSlot slot) =>
        $"{CosmeticCatalog.SlotLabel(slot)} {(slot == CosmeticSlot.Aura ? "retirée" : "retiré")}.";

    public static async Task<CosmeticScreenData> LoadAsync(PlynlingService plynlings, CosmeticService cosmetics, ulong guildId, ulong userId,
        DateTimeOffset now)
    {
        var p = await plynlings.GetCurrentAsync(guildId, userId, now);
        if (p?.DiedAt is not null) p = null;   // it starved since anyone looked: it comes back dead
        var (held, balance) = await cosmetics.HeldAsync(guildId, userId);
        return p is null
            ? new CosmeticScreenData(null, held, balance)
            : new CosmeticScreenData(p, held, balance, (await plynlings.GetPartnerAsync(p))?.Name, await plynlings.GetLatestTitleAsync(p));
    }

    public static MessageComponent Build(CosmeticView view, CosmeticScreenData data, DateTimeOffset now, string? notice)
    {
        var shop = CosmeticCatalog.Shop(now);
        // A pick only stands on its own tab, and only while it is still on offer there.
        var picked = CosmeticCatalog.ByKey(view.Picked) is { } c && view.Tab switch
        {
            CosmeticTab.Shop => shop.Contains(c),
            CosmeticTab.Craft => c.Source == CosmeticSource.Crafted,
            _ => false,
        } ? c : null;

        var container = Head(view, data, now, picked);
        container.AddComponent(new SeparatorBuilder());

        var tabs = new ActionRowBuilder();
        foreach (var tab in Enum.GetValues<CosmeticTab>())
            tabs.WithButton(TabLabel(tab), $"cosm:tab:{tab}", tab == view.Tab ? ButtonStyle.Primary : ButtonStyle.Secondary);
        container.AddComponent(tabs);

        switch (view.Tab)
        {
            case CosmeticTab.Wardrobe: WardrobeBody(container, view.Slot, data); break;
            case CosmeticTab.Shop: ShopBody(container, shop, picked, data, now); break;
            default: CraftBody(container, picked, data); break;
        }

        if (notice is not null)
            container.AddComponent(new TextDisplayBuilder(notice));
        return new ComponentBuilderV2().AddComponent(container).Build();
    }

    // ---- the preview ---------------------------------------------------------------------------

    private static ContainerBuilder Head(CosmeticView view, CosmeticScreenData data, DateTimeOffset now, CosmeticInfo? picked)
    {
        if (data.Plynling is not { } p)
        {
            var bare = new ContainerBuilder().WithAccentColor(new Color(picked?.Accent ?? 0x9B59B6));
            if (picked?.Banner is { } banner) bare.AddComponent(new TextDisplayBuilder(banner));
            return bare.AddComponent(new TextDisplayBuilder("## 👗 Cosmétiques\nAdopte un Plynling pour essayer tes cosmétiques sur lui."));
        }

        var worn = Outfit.Of(p);
        var tried = picked is null ? worn : worn.With(picked);
        var container = PlynlingModule.CardHead(p, now, data.PartnerName, data.Epithet, tried);
        if (picked is not null && worn.In(picked.Slot)?.Key != picked.Key)
            container.AddComponent(new TextDisplayBuilder(
                $"-# Essai : {picked.Emoji} {Shown(picked, p)} — {(data.Owns(picked) ? "à toi, pas encore porté" : "pas encore à toi")}"));
        // An aura isn't drawn on the sick and frozen faces (PlynlingArt.AuraMoods): say so, or the
        // try-on would look like nothing changed.
        if (tried.Aura is not null && PlynlingLife.Mood(p, now) is PlynlingMood.Sick or PlynlingMood.Frozen)
            container.AddComponent(new TextDisplayBuilder(
                $"-# Son aura ne se voit pas tant qu'{p.Gender.Agree("il est malade ou gelé", "elle est malade ou gelée")}."));
        // A cadre only shows on a grave: its preview is the grave's first line, as it would read.
        if (picked?.Slot == CosmeticSlot.Grave || (picked is null && view.Tab == CosmeticTab.Wardrobe && view.Slot == CosmeticSlot.Grave))
            container.AddComponent(new TextDisplayBuilder($"-# Sa tombe, le jour venu :\n{PlynlingCardUi.GraveFirstLine(p, tried.Grave)}"));
        return container;
    }

    // ---- the wardrobe --------------------------------------------------------------------------

    private static void WardrobeBody(ContainerBuilder container, CosmeticSlot slot, CosmeticScreenData data)
    {
        if (data.Plynling is not { } p)
        {
            var lines = CosmeticCatalog.SlotOrder
                .Select(s => (Slot: s, Owned: CosmeticCatalog.InSlot(s).Where(data.Owns).Select(c => $"{c.Emoji} {CosmeticCatalog.ShortName(c)}").ToList()))
                .Where(x => x.Owned.Count > 0)
                .Select(x => $"**{CosmeticCatalog.SlotLabel(x.Slot)}** : {string.Join(" · ", x.Owned)}")
                .ToList();
            container.AddComponent(new TextDisplayBuilder(lines.Count == 0
                ? "Aucun cosmétique pour l'instant. Va voir la **Boutique** !"
                : string.Join("\n", lines)));
            return;
        }

        var slots = new ActionRowBuilder();
        foreach (var s in CosmeticCatalog.SlotOrder)        // five buttons: a row's maximum
            slots.WithButton(CosmeticCatalog.SlotLabel(s), $"cosm:slot:{s}", s == slot ? ButtonStyle.Primary : ButtonStyle.Secondary);
        container.AddComponent(slots);

        var owned = CosmeticCatalog.InSlot(slot).Where(data.Owns).ToList();
        var worn = CosmeticSlots.Worn(p, slot);
        if (owned.Count == 0)
        {
            container.AddComponent(new TextDisplayBuilder(
                $"Rien à toi dans « {CosmeticCatalog.SlotLabel(slot)} » pour l'instant : la **Boutique** et la **Fabrication** en ont."));
            return;
        }

        container.AddComponent(new TextDisplayBuilder(
            $"**{CosmeticCatalog.SlotLabel(slot)}** · {owned.Count} à toi. Choisis : c'est porté tout de suite."));
        // « Aucun » then everything owned, 25 to a select (a slot can hold more than one select's worth:
        // titles and cadres do). The last segment numbers the selects; the handler ignores it here.
        var options = owned.Select(c => new SelectMenuOptionBuilder(Shown(c, p), c.Key, emote: EmoteMarkup.Parse(c.Emoji), isDefault: c.Key == worn?.Key))
            .Prepend(new SelectMenuOptionBuilder("Aucun", NonePrefix + slot, isDefault: worn is null))
            .Chunk(MaxOptions).ToList();
        for (var i = 0; i < options.Count; i++)
            container.AddComponent(new ActionRowBuilder().WithSelectMenu(new SelectMenuBuilder()
                .WithCustomId($"cosm:pick:{CosmeticTab.Wardrobe}:{i}")
                .WithPlaceholder($"{CosmeticCatalog.SlotLabel(slot)}…" + (options.Count > 1 ? $" ({i + 1}/{options.Count})" : ""))
                .WithOptions(options[i].ToList())));

        // The worn accessory's styles: one select per choice (« Couleur », « Position »), free, worn at once.
        if (slot == CosmeticSlot.Accessory && worn is not null && AccessoryStyles.Resolve(worn.Key, p.AccessoryStyle) is { } style)
        {
            var current = style.Split('_');
            var choices = AccessoryStyles.Choices(worn.Key);
            for (var i = 0; i < choices.Count; i++)
            {
                var styles = new SelectMenuBuilder()
                    .WithCustomId($"cosm:style:{i}:{worn.Key}")
                    .WithPlaceholder($"{choices[i].Label}…");
                foreach (var o in choices[i].Options)
                    styles.AddOption($"{choices[i].Label} : {o.Name}", o.Key, isDefault: o.Key == current[i]);
                container.AddComponent(new ActionRowBuilder().WithSelectMenu(styles));
            }
        }
    }

    // ---- the shop ------------------------------------------------------------------------------

    private static void ShopBody(ContainerBuilder container, IReadOnlyList<CosmeticInfo> shop, CosmeticInfo? picked, CosmeticScreenData data,
        DateTimeOffset now)
    {
        string Line(CosmeticInfo c) =>
            $"{c.Emoji} {CosmeticCatalog.Label(c)} · {SourceLabel(c)} · {PebbleEconomy.Cailloux(c.Price)}" + (data.Owns(c) ? " ✅" : "");
        string Section(string title, CosmeticSource source) =>
            shop.Any(c => c.Source == source) ? $"\n**{title}**\n" + string.Join("\n", shop.Where(c => c.Source == source).Select(Line)) : "";

        container.AddComponent(new TextDisplayBuilder(
            $"Tu as **{PebbleEconomy.Cailloux(data.Balance)}** · nouvelle sélection <t:{CosmeticCatalog.NextRotation(now).ToUnixTimeSeconds()}:R>" +
            Section("Toujours", CosmeticSource.Basic) + Section("Cette semaine", CosmeticSource.Rotating) + Section("De saison", CosmeticSource.Seasonal)));

        var week = CosmeticCatalog.WeekKey(now);
        var menu = new SelectMenuBuilder()
            .WithCustomId($"cosm:pick:{CosmeticTab.Shop}:{week}")
            .WithPlaceholder(data.Plynling is null ? "Voir un article…" : "Essayer un article…");
        foreach (var c in shop)
            menu.AddOption($"{CosmeticCatalog.Label(c)} — {PebbleEconomy.Cailloux(c.Price)}", c.Key,
                $"{CosmeticCatalog.SlotLabel(c.Slot)} · {(data.Owns(c) ? "déjà à toi" : SourceLabel(c))}", EmoteMarkup.Parse(c.Emoji),
                isDefault: c.Key == picked?.Key);
        container.AddComponent(new ActionRowBuilder().WithSelectMenu(menu));

        if (picked is null) return;
        var owned = data.Owns(picked);
        var poor = data.Balance < picked.Price;
        container.AddComponent(new TextDisplayBuilder(
            $"### {picked.Emoji} {CosmeticCatalog.Label(picked)}\n" +
            $"{CosmeticCatalog.SlotLabel(picked.Slot)} · {SourceLabel(picked)} · **{PebbleEconomy.Cailloux(picked.Price)}**\n" +
            (owned ? Ownership(picked, data) : poor ? $"Il te manque **{PebbleEconomy.Cailloux(picked.Price - data.Balance)}**." : "Tu peux te l'offrir.")));

        var row = new ActionRowBuilder();
        if (!owned)
        {
            row.WithButton("Acheter", $"cosm:buy:{week}:{picked.Key}", ButtonStyle.Success, disabled: poor);
            if (data.Plynling is not null)
                row.WithButton("Acheter et porter", $"cosm:buyw:{week}:{picked.Key}", ButtonStyle.Success, disabled: poor);
        }
        else AddWear(row, CosmeticTab.Shop, picked, data);
        row.WithButton("Retour", $"cosm:back:{CosmeticTab.Shop}", ButtonStyle.Secondary);
        container.AddComponent(row);
    }

    // ---- the recipes ---------------------------------------------------------------------------

    private static void CraftBody(ContainerBuilder container, CosmeticInfo? picked, CosmeticScreenData data)
    {
        // « Ouvrages » (duplicates) first, then « Chefs-d'œuvre » (aimed hunts); the select follows the same order.
        var recipes = CosmeticCatalog.All.Where(c => c.Source == CosmeticSource.Crafted).OrderBy(c => c.Trophy).ToList();
        string Group(bool trophy) => string.Join("\n", recipes.Where(c => c.Trophy == trophy)
            .Select(c => $"{c.Emoji} {CosmeticCatalog.Label(c)} · {RecipeStatus(c, data)}"));
        container.AddComponent(new TextDisplayBuilder(
            "Transforme tes objets de collection en cosmétiques. Les objets utilisés disparaissent, " +
            "mais restent découverts dans ton carnet. Envoie ton Plynling fouiller au bon endroit : `/plynling forage place:`.\n" +
            $"**Ouvrages**\n{Group(false)}\n**Chefs-d'œuvre**\n{Group(true)}"));

        // One select per group: together they hold more than a select's 25 options. The last segment
        // tells them apart; the handler ignores it here.
        foreach (var trophy in new[] { false, true })
        {
            var menu = new SelectMenuBuilder()
                .WithCustomId($"cosm:pick:{CosmeticTab.Craft}:{(trophy ? 1 : 0)}")
                .WithPlaceholder(trophy ? "Voir un chef-d'œuvre…" : "Voir un ouvrage…");
            foreach (var c in recipes.Where(c => c.Trophy == trophy))
                menu.AddOption(CosmeticCatalog.Label(c), c.Key, $"{CosmeticCatalog.SlotLabel(c.Slot)} · {RecipeStatus(c, data)}",
                    EmoteMarkup.Parse(c.Emoji), isDefault: c.Key == picked?.Key);
            container.AddComponent(new ActionRowBuilder().WithSelectMenu(menu));
        }

        if (picked is null) return;
        var owned = data.Owns(picked);
        var missing = Missing(picked, data);
        var lines = picked.Recipe.Select(r =>
        {
            var have = Recipes.Have(r.ItemKey, data.Held);
            var where = Recipes.Where(r.ItemKey);
            var name = Recipes.Name(r.ItemKey, r.Count);   // « champignons communs » starts a line here: capitalise it
            return $"{(have >= r.Count ? "✅" : "❌")} {Recipes.Emoji(r.ItemKey)} {char.ToUpperInvariant(name[0])}{name[1..]} {Math.Min(have, r.Count)}/{r.Count}" +
                   (where.Length > 0 ? $" — {where}" : "");
        }).Append($"{(data.Balance >= picked.Price ? "✅" : "❌")} 🪨 {PebbleEconomy.Cailloux(picked.Price)}");
        container.AddComponent(new TextDisplayBuilder(
            $"### {picked.Emoji} {CosmeticCatalog.Label(picked)}\n{CosmeticCatalog.SlotLabel(picked.Slot)}\n" +
            string.Join("\n", lines) + "\n" +
            (owned ? Ownership(picked, data) : missing.Count == 0 ? "Tout est prêt !" : $"Il te manque : {string.Join(", ", missing)}.")));

        var row = new ActionRowBuilder();
        if (!owned)
        {
            row.WithButton("Fabriquer", $"cosm:make:{picked.Key}", ButtonStyle.Success, disabled: missing.Count > 0);
            if (data.Plynling is not null)
                row.WithButton("Fabriquer et porter", $"cosm:makew:{picked.Key}", ButtonStyle.Success, disabled: missing.Count > 0);
        }
        else AddWear(row, CosmeticTab.Craft, picked, data);
        row.WithButton("Retour", $"cosm:back:{CosmeticTab.Craft}", ButtonStyle.Secondary);
        container.AddComponent(row);
    }

    /// <summary>What a recipe still lacks, named: « 2 × Améthyste », then the cailloux.</summary>
    public static List<string> Missing(CosmeticInfo recipe, CosmeticScreenData data)
    {
        var missing = Recipes.Lacking(recipe, data.Held);
        if (data.Balance < recipe.Price) missing.Add(PebbleEconomy.Cailloux(recipe.Price - data.Balance));
        return missing;
    }

    private static string RecipeStatus(CosmeticInfo c, CosmeticScreenData data)
    {
        if (data.Owns(c)) return "déjà à toi ✅";
        var missing = Missing(c, data).Count;
        return missing == 0 ? "prêt ✅" : $"il manque {missing} {(missing > 1 ? "choses" : "chose")}";
    }

    // ---- shared pieces -------------------------------------------------------------------------

    // An owned item's line in the item view: worn already, or waiting in the wardrobe.
    private static string Ownership(CosmeticInfo c, CosmeticScreenData data) =>
        data.Plynling is { } p && CosmeticSlots.Worn(p, c.Slot)?.Key == c.Key
            ? $"À toi, et {PlynlingCardUi.SafeName(p.Name)} le porte."
            : "Déjà à toi.";

    private static void AddWear(ActionRowBuilder row, CosmeticTab tab, CosmeticInfo c, CosmeticScreenData data)
    {
        if (data.Plynling is { } p && CosmeticSlots.Worn(p, c.Slot)?.Key != c.Key)
            row.WithButton("Porter", $"cosm:wear:{tab}:{c.Key}", ButtonStyle.Primary);
    }

    private static string TabLabel(CosmeticTab tab) => tab switch
    {
        CosmeticTab.Wardrobe => "👗 Garde-robe",
        CosmeticTab.Shop => "🛍️ Boutique",
        _ => "🔨 Fabrication",
    };

    // How it reads on this Plynling: a title in its gender, the rest by its short name.
    private static string Shown(CosmeticInfo c, Plynling p) =>
        c.Slot == CosmeticSlot.Title ? CosmeticCatalog.TitleFor(c, p.Gender) : CosmeticCatalog.ShortName(c);

    private static string SourceLabel(CosmeticInfo c) => c.Source switch
    {
        CosmeticSource.Basic => "basique",
        CosmeticSource.Seasonal => $"de saison ({ItemCatalog.SeasonLabel(c.Season)})",
        CosmeticSource.Crafted => "à fabriquer",
        _ => ItemCatalog.RarityLabel(c.Rarity),
    };

    // A bought cosmetic's receipt line, under ShopCards.Receipt's heading.
    public static string Bought(CosmeticInfo c, long balance, bool worn) =>
        $"{c.Emoji} **{CosmeticCatalog.Label(c)}** est à toi, pour {PebbleEconomy.Cailloux(c.Price)}. " +
        $"Il te reste {PebbleEconomy.Cailloux(balance)}." + (worn ? "\n-# Ton Plynling le porte déjà." : "");

    // The one-line notice after a buy, a craft or a change of clothes. wantedWorn: « …et porter » was
    // asked, so a Done that wore nothing says why (the Plynling is gone).
    public static string Notice(CosmeticOutcome outcome, CosmeticInfo? c, long price = 0, bool wantedWorn = false, bool worn = false) => outcome switch
    {
        CosmeticOutcome.Done when wantedWorn && !worn =>
            $"✅ {c?.Emoji} **{(c is null ? "" : CosmeticCatalog.Label(c))}** est à toi — mais tu n'as plus de Plynling pour le porter.",
        CosmeticOutcome.Done when worn => $"✅ {c?.Emoji} **{(c is null ? "" : CosmeticCatalog.Label(c))}** est à toi, et c'est porté !",
        CosmeticOutcome.Done => $"✅ {c?.Emoji} **{(c is null ? "" : CosmeticCatalog.Label(c))}** est à toi !",
        CosmeticOutcome.AlreadyOwned => "Tu l'as déjà.",
        CosmeticOutcome.TooPoor => $"❌ Il te faut {PebbleEconomy.Cailloux(price)}.",
        CosmeticOutcome.NotInShop => "❌ Cet article n'est plus en boutique : la sélection a changé.",
        CosmeticOutcome.MissingIngredients => "❌ Il te manque des objets pour cette recette.",
        CosmeticOutcome.NotOwned => "❌ Tu ne l'as pas (ou plus).",
        CosmeticOutcome.NoPlynling => PlynlingText.NoPlynling,
        _ => PlynlingText.UnknownItem,
    };
}
