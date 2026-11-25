using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// The cosmetics screen's three tabs.
public enum CosmeticTab { Wardrobe, Shop, Craft }

// Where the screen stands: its tab, the wardrobe's slot, and the item picked in the shop or the
// recipes — tried on in the preview, nothing bought — and, after a page turn, which of the tab's
// selects (PagedSelect, by its order on the tab; -1 none) shows which page. All of it rides in the
// custom-ids and option values, so a restart loses nothing.
public sealed record CosmeticView(CosmeticTab Tab, CosmeticSlot Slot = CosmeticSlot.Theme, string? Picked = null,
    int PagedSelect = -1, int Page = 0);

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
// cosm:pick:{tab}:{week} (the shop's select; the wardrobe's ends in 0, the recipes' two in 0 and 1;
// « Aucun » is none:{slot}), cosm:buy:{week}:{key},
// cosm:buyw:{week}:{key}, cosm:make:{key}, cosm:makew:{key}, cosm:wear:{tab}:{key}, cosm:back:{tab},
// cosm:style:{choice}:{key} (the worn accessory's colour or position).
// The prefix is new (the old screens used cos:), so no handler here answers an old screen's select.
// Every list select is paged (SelectPages): past 25 options its « ◀ » / « ▶ » options turn the page
// (value page:{page}:{slot}:{picked}, read by PageTurn), so a select never splits in two.
public static class CosmeticCards
{
    public const string NonePrefix = "none:";
    public const string PagePrefix = "page:";

    // The air between the screen's blocks: a gap, no line.
    private static SeparatorBuilder Gap() => new(false, SeparatorSpacingSize.Large);

    // A page-turn option's value: the page, plus the slot and the try-on, which the redraw keeps.
    private static string PageValue(CosmeticView view, int page) => $"{PagePrefix}{page}:{view.Slot}:{view.Picked}";

    /// <summary>The view a page-turn option asks for (select: its order on the tab); null for any other value.</summary>
    public static CosmeticView? PageTurn(CosmeticTab tab, int select, string value)
    {
        if (!value.StartsWith(PagePrefix, StringComparison.Ordinal)) return null;
        var parts = value[PagePrefix.Length..].Split(':', 3);
        if (parts.Length < 2 || !int.TryParse(parts[0], out var page) || !Enum.TryParse<CosmeticSlot>(parts[1], out var slot))
            return new CosmeticView(tab);
        return new CosmeticView(tab, slot, parts.Length > 2 && parts[2].Length > 0 ? parts[2] : null, select, page);
    }

    // A list select on one page or several (SelectPages): it opens on the page holding its default
    // option, unless this select (index: its order on the tab) is the one whose page was turned.
    private static SelectMenuBuilder Paged(string customId, string placeholder, List<SelectMenuOptionBuilder> options, CosmeticView view, int index)
    {
        var pages = SelectPages.Count(options.Count);
        var page = SelectPages.Clamp(view.PagedSelect == index ? view.Page
            : SelectPages.PageOf(options.FindIndex(o => o.IsDefault == true), options.Count), options.Count);
        var (start, length) = SelectPages.Range(page, options.Count);
        var shown = options.GetRange(start, length);
        if (page > 0)
            shown.Insert(0, new SelectMenuOptionBuilder("◀ Page précédente", PageValue(view, page - 1), $"Page {page} sur {pages}"));
        if (page < pages - 1)
            shown.Add(new SelectMenuOptionBuilder("Page suivante ▶", PageValue(view, page + 1), $"Page {page + 2} sur {pages}"));
        return new SelectMenuBuilder()
            .WithCustomId(customId)
            .WithPlaceholder(pages > 1 ? $"{placeholder} (page {page + 1}/{pages})" : placeholder)
            .WithOptions(shown);
    }

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
            case CosmeticTab.Wardrobe: WardrobeBody(container, view, data); break;
            case CosmeticTab.Shop: ShopBody(container, view, shop, picked, data, now); break;
            default: CraftBody(container, view, picked, data); break;
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
        var saved = SavedStyles.Parse(p.SavedAccessoryStyles);
        var tried = picked is null ? worn : worn.TryOn(picked, saved);       // an accessory goes where it fits, in its remembered style
        var container = PlynlingModule.CardHead(p, now, data.PartnerName, data.Epithet, tried);
        if (picked is not null && !Wears(worn, picked))
            container.AddComponent(new TextDisplayBuilder(
                $"-# Essai : {picked.Emoji} {Shown(picked, p)} — {(data.Owns(picked) ? "à toi, pas encore porté" : "pas encore à toi")}" +
                (AccessoryNotices.Of(worn.AccessoryChangeFor(picked, saved), picked, preview: true) is { } displaced ? $"\n-# {displaced}" : "")));
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

    private static void WardrobeBody(ContainerBuilder container, CosmeticView view, CosmeticScreenData data)
    {
        var slot = view.Slot;
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
        var outfit = Outfit.Of(p);
        var worn = outfit.In(slot);
        var caption = $"**{CosmeticCatalog.SlotLabel(slot)}** · {CosmeticCatalog.SlotBlurb(slot)}";
        if (owned.Count == 0)
        {
            container.AddComponent(new TextDisplayBuilder(
                caption + "\nRien à toi ici pour l'instant : la **Boutique** et la **Fabrication** en ont."));
            return;
        }

        container.AddComponent(new TextDisplayBuilder(caption));
        // The accessory slot has one picker per accessory worn plus one for the next, each followed by
        // its own style selects and set apart from the next by a gap; every other slot one.
        if (slot != CosmeticSlot.Accessory)
        {
            Picker(container, owned, slot, worn, $"cosm:pick:{CosmeticTab.Wardrobe}", CosmeticCatalog.SlotLabel(slot), p, view, 0);
            return;
        }
        var items = outfit.Items;
        for (var i = 0; i < AccessoryFit.MaxWorn && i <= items.Count; i++)     // the next one needs the ones before
        {
            var item = i < items.Count ? items[i].Item : null;
            var style = i < items.Count ? items[i].Style : null;
            if (i > 0) container.AddComponent(Gap());
            Picker(container, owned, slot, item, AccessoryPickerIds[i], AccessoryPickerLabels[i], p, view, i);
            StyleSelects(container, item, style);
        }
    }

    // One custom-id verb per accessory picker (CosmeticComponentHandler): cosm:pick:Wardrobe, cosm:pick2, cosm:pick3.
    public static readonly string[] AccessoryPickerIds = [$"cosm:pick:{CosmeticTab.Wardrobe}", "cosm:pick2", "cosm:pick3"];
    private static readonly string[] AccessoryPickerLabels = ["Premier accessoire", "Deuxième accessoire", "Troisième accessoire"];

    // « Aucun » then everything owned, on one paged select (titles and cadres pass 25). The id's last
    // segment is always 0 now; it stays so the handlers' patterns hold. index: the picker's order on the tab.
    private static void Picker(ContainerBuilder container, List<CosmeticInfo> owned, CosmeticSlot slot, CosmeticInfo? worn, string idPrefix,
        string label, Plynling p, CosmeticView view, int index)
    {
        var options = owned.Select(c => new SelectMenuOptionBuilder(Shown(c, p), c.Key, emote: EmoteMarkup.Parse(c.Emoji), isDefault: c.Key == worn?.Key))
            .Prepend(new SelectMenuOptionBuilder("Aucun", NonePrefix + slot, isDefault: worn is null))
            .ToList();
        container.AddComponent(new ActionRowBuilder().WithSelectMenu(Paged($"{idPrefix}:0", $"{label}…", options, view, index)));
    }

    // A worn accessory's styles: one select per choice (« Couleur », « Position », « Côté »), free, worn at once.
    // The key in the id tells the two accessories' selects apart.
    private static void StyleSelects(ContainerBuilder container, CosmeticInfo? accessory, string? storedStyle)
    {
        if (accessory is null || AccessoryStyles.Resolve(accessory.Key, storedStyle) is not { } style) return;
        var current = style.Split('_');
        var choices = AccessoryStyles.Choices(accessory.Key);
        for (var i = 0; i < choices.Count; i++)
        {
            var styles = new SelectMenuBuilder()
                .WithCustomId($"cosm:style:{i}:{accessory.Key}")
                .WithPlaceholder($"{choices[i].Label}…");
            foreach (var o in choices[i].Options)
                styles.AddOption($"{choices[i].Label} : {o.Name}", o.Key, isDefault: o.Key == current[i]);
            container.AddComponent(new ActionRowBuilder().WithSelectMenu(styles));
        }
    }

    // ---- the shop ------------------------------------------------------------------------------

    // The shop's sections, in this order; each lists its items by slot (SlotOrder).
    private static readonly (CosmeticSource Source, string Title)[] ShopSections =
        [(CosmeticSource.Basic, "Toujours"), (CosmeticSource.Rotating, "Cette semaine"), (CosmeticSource.Seasonal, "De saison")];

    private static void ShopBody(ContainerBuilder container, CosmeticView view, IReadOnlyList<CosmeticInfo> shop, CosmeticInfo? picked,
        CosmeticScreenData data, DateTimeOffset now)
    {
        var sections = ShopSections
            .Select(s => (s.Title, Items: shop.Where(c => c.Source == s.Source).OrderBy(c => CosmeticCatalog.SlotRank(c.Slot)).ToList()))
            .Where(s => s.Items.Count > 0)
            .ToList();
        // The basics need no tier; this week's and the season's items name theirs, which sets their price.
        string Line(CosmeticInfo c) =>
            $"{c.Emoji} {CosmeticCatalog.Label(c)} · " + (c.Source != CosmeticSource.Basic ? $"{ItemCatalog.RarityLabel(c.Rarity)} · " : "") +
            PebbleEconomy.Cailloux(c.Price) + (data.Owns(c) ? $" {UiIcons.Check}" : "");
        string Title(string title, List<CosmeticInfo> items) =>
            items[0].Source == CosmeticSource.Seasonal ? $"{title} · {ItemCatalog.SeasonLabel(items[0].Season)}" : title;

        container.AddComponent(new TextDisplayBuilder(
            $"Tu as **{PebbleEconomy.Cailloux(data.Balance)}** · nouvelle sélection <t:{CosmeticCatalog.NextRotation(now).ToUnixTimeSeconds()}:R>"));
        foreach (var (title, items) in sections)
        {
            container.AddComponent(Gap());
            container.AddComponent(new TextDisplayBuilder($"**{Title(title, items)}**\n" + string.Join("\n", items.Select(Line))));
        }

        var week = CosmeticCatalog.WeekKey(now);
        var options = sections.SelectMany(s => s.Items)
            .Select(c => new SelectMenuOptionBuilder($"{CosmeticCatalog.Label(c)} — {PebbleEconomy.Cailloux(c.Price)}", c.Key,
                $"{CosmeticCatalog.SlotLabel(c.Slot)} · {(data.Owns(c) ? "déjà à toi" : SourceLabel(c))}", EmoteMarkup.Parse(c.Emoji),
                isDefault: c.Key == picked?.Key))
            .ToList();
        container.AddComponent(Gap());
        container.AddComponent(new ActionRowBuilder().WithSelectMenu(Paged($"cosm:pick:{CosmeticTab.Shop}:{week}",
            data.Plynling is null ? "Voir un article…" : "Essayer un article…", options, view, 0)));

        if (picked is null) return;
        container.AddComponent(Gap());
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

    private static void CraftBody(ContainerBuilder container, CosmeticView view, CosmeticInfo? picked, CosmeticScreenData data)
    {
        // « Ouvrages » (duplicates) first, then « Chefs-d'œuvre » (aimed hunts), each by slot
        // (SlotOrder); the selects follow the same order.
        var recipes = CosmeticCatalog.All.Where(c => c.Source == CosmeticSource.Crafted)
            .OrderBy(c => c.Trophy).ThenBy(c => CosmeticCatalog.SlotRank(c.Slot)).ToList();
        // The overview lists every recipe, so it stays short per line (a title in its masculine form,
        // an owned one just ticked) and gives way to the picked recipe: 36 lines plus a recipe's view
        // passed the 4 000 characters of a Components V2 message.
        string Line(CosmeticInfo c) =>
            $"{c.Emoji} {(c.Slot == CosmeticSlot.Title ? $"Titre « {c.Name} »" : CosmeticCatalog.Label(c))} · " +
            (data.Owns(c) ? UiIcons.Check : RecipeStatus(c, data, mark: true));
        string Group(bool trophy) => string.Join("\n", recipes.Where(c => c.Trophy == trophy).Select(Line));
        if (picked is null)
        {
            container.AddComponent(new TextDisplayBuilder(
                "Transforme tes objets de collection en cosmétiques. Les objets utilisés disparaissent, " +
                "mais restent découverts dans ton carnet. Envoie ton Plynling fouiller au bon endroit : `/plynling forage place:`."));
            container.AddComponent(Gap());
            container.AddComponent(new TextDisplayBuilder($"**Ouvrages**\n{Group(false)}"));
            container.AddComponent(Gap());
            container.AddComponent(new TextDisplayBuilder($"**Chefs-d'œuvre**\n{Group(true)}"));
            container.AddComponent(Gap());
        }

        // One select per group, each paged. The last segment tells them apart (the handler reads it
        // only for a page turn), and is the select's index for the page.
        foreach (var trophy in new[] { false, true })
        {
            var index = trophy ? 1 : 0;
            var options = recipes.Where(c => c.Trophy == trophy)
                .Select(c => new SelectMenuOptionBuilder(CosmeticCatalog.Label(c), c.Key, $"{CosmeticCatalog.SlotLabel(c.Slot)} · {RecipeStatus(c, data, mark: false)}",
                    EmoteMarkup.Parse(c.Emoji), isDefault: c.Key == picked?.Key))
                .ToList();
            container.AddComponent(new ActionRowBuilder().WithSelectMenu(Paged($"cosm:pick:{CosmeticTab.Craft}:{index}",
                trophy ? "Voir un chef-d'œuvre…" : "Voir un ouvrage…", options, view, index)));
        }

        if (picked is null) return;
        container.AddComponent(Gap());
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

    // mark: the small check after « déjà à toi » / « prêt » — in text only, a select's description
    // can't show a custom emoji.
    private static string RecipeStatus(CosmeticInfo c, CosmeticScreenData data, bool mark)
    {
        var check = mark ? $" {UiIcons.Check}" : "";
        if (data.Owns(c)) return "déjà à toi" + check;
        var missing = Missing(c, data).Count;
        return missing == 0 ? "prêt" + check : $"il manque {missing} {(missing > 1 ? "choses" : "chose")}";
    }

    // ---- shared pieces -------------------------------------------------------------------------

    // An owned item's line in the item view: worn already, or waiting in the wardrobe.
    private static string Ownership(CosmeticInfo c, CosmeticScreenData data) =>
        data.Plynling is { } p && Wears(Outfit.Of(p), c)
            ? $"À toi, et {PlynlingCardUi.SafeName(p.Name)} le porte."
            : "Déjà à toi.";

    // Worn in its slot — an accessory in any of its slots.
    private static bool Wears(Outfit outfit, CosmeticInfo c) => outfit.Wears(c);

    private static void AddWear(ActionRowBuilder row, CosmeticTab tab, CosmeticInfo c, CosmeticScreenData data)
    {
        if (data.Plynling is { } p && !Wears(Outfit.Of(p), c))
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
        CosmeticSource.Seasonal => $"{ItemCatalog.RarityLabel(c.Rarity)}, de saison ({ItemCatalog.SeasonLabel(c.Season)})",
        CosmeticSource.Crafted => $"{ItemCatalog.RarityLabel(c.Rarity)}, à fabriquer",
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
