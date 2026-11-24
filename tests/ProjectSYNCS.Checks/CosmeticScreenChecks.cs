using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

// 59. The cosmetics screen (CosmeticCards): every tab, slot and item view at its fullest — every
// icon uploaded (Program.cs gives every item a fake icon before this runs), the longest names, all owned or nothing, with and without a
// Plynling, in every season — inside Components V2's caps (40 components, 4000 characters, no
// custom-id twice, ids under 100, selects under 25). The try-on never writes to the Plynling, and a
// recipe names what it lacks.
public static class CosmeticScreenChecks
{
    private static Action<string> _fail = _ => { };

    private static void Expect(bool ok, string what)
    {
        if (!ok) _fail("cosmetics screen: " + what);
    }

    public static void Run(Action<string> fail)
    {
        _fail = fail;
        Caps();
        TryOnWritesNothing();
        MissingNamesItems();
        WhereToFind();
        SeveralAccessories();
        AccessoryNoticeWording();
    }

    // The Fabrication tab: two groups in order, a ✅ on ready recipes, and each ingredient says
    // where it is found (rarity, place, season).
    private static void WhereToFind()
    {
        var coquillage = ItemCatalog.ByKey("col.coquillage")!;
        var where = ForagePlaces.Whereabouts(coquillage);
        Expect(where.Contains("rare") && where.Contains("à la Sommeille") && where.Contains("été"), "Whereabouts(Coquillage): " + where);
        Expect(!ForagePlaces.Whereabouts(ItemCatalog.ByKey("col.galet")!).Contains(','.ToString() + ' ' + "l'"), "Whereabouts(Galet) names a season");
        Expect(Recipes.Where(Recipes.AnyCommon("champignons")) == "à la Lisière", "Where(any mushroom): " + Recipes.Where(Recipes.AnyCommon("champignons")));

        var now = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
        var ready = CosmeticCatalog.All.First(c => c.Source == CosmeticSource.Crafted && !c.Trophy);
        var held = ready.Recipe.SelectMany(r => Recipes.Items(r.ItemKey)).ToDictionary(i => i.Key, _ => 99);
        var data = new CosmeticScreenData(null, held, 999_999);
        var list = string.Join("\n", Texts(CosmeticCards.Build(new CosmeticView(CosmeticTab.Craft), data, now, null).Components));
        var o = list.IndexOf("Ouvrages", StringComparison.Ordinal);
        var t = list.IndexOf("Chefs-d'œuvre", StringComparison.Ordinal);
        Expect(o >= 0 && t > o, "Fabrication: no « Ouvrages » then « Chefs-d'œuvre » groups");
        Expect(list.Split('\n').Any(l => l.Contains(CosmeticCatalog.Label(ready)) && l.Contains("✅")), "Fabrication: a ready recipe has no ✅");

        var collier = CosmeticCatalog.ByKey("cos.accessory.collier_coquillages")!;
        var view = string.Join("\n", Texts(CosmeticCards.Build(new CosmeticView(CosmeticTab.Craft, Picked: collier.Key),
            new CosmeticScreenData(null, new Dictionary<string, int>(), 0), now, null).Components));
        Expect(view.Split('\n').Any(l => l.Contains("Coquillage") && l.Contains("0/") && l.Contains("à la Sommeille")),
            "recipe view: the Coquillage line does not say where to find it");

        var sousBois = CosmeticCatalog.ByKey("cos.theme.sous_bois")!;
        var anyView = string.Join("\n", Texts(CosmeticCards.Build(new CosmeticView(CosmeticTab.Craft, Picked: sousBois.Key),
            new CosmeticScreenData(null, new Dictionary<string, int>(), 0), now, null).Components));
        Expect(anyView.Split('\n').Any(l => l.Contains("Champignons communs 0/") && l.Contains("à la Lisière")),
            "recipe view: the any-common line is not « Champignons communs 0/… — à la Lisière »");
    }

    // Several accessories worn: the card line names them, the Garde-robe has a picker per slot with its
    // own style selects, and at the worst (écharpe + nœud + ballon, every select present) it stays inside
    // 40 components.
    private static void SeveralAccessories()
    {
        var now = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
        var noeud = CosmeticCatalog.ByKey("cos.accessory.noeud")!;
        var ballon = CosmeticCatalog.ByKey("cos.accessory.ballon")!;
        var echarpe = CosmeticCatalog.ByKey("cos.accessory.echarpe")!;
        var pair = new Plynling
        {
            Name = "Pomme", Species = PlynlingSpecies.Amanite, AdoptedAt = now.AddDays(-60), NeedsAsOf = now, Hygiene = 1.0,
            AccessoryKey = noeud.Key, Accessory2Key = ballon.Key,
        };
        var heading = PlynlingCardUi.Heading(pair, now);
        Expect(heading.Contains($"Porte {noeud.Emoji} {noeud.Name} et {ballon.Emoji} {ballon.Name}"), "card line with two accessories: " + heading);
        Expect(heading.Contains(" un nœud et ") && heading.Contains(" un ballon"), "card line wording: " + heading);
        pair.Accessory2Key = null;
        var oneHeading = PlynlingCardUi.Heading(pair, now);
        Expect(oneHeading.Contains($"Porte {noeud.Emoji} {noeud.Name}") && !oneHeading.Contains(" et "), "card line with one accessory changed: " + oneHeading);

        var worst = Fullest();
        worst.AccessoryKey = echarpe.Key; worst.AccessoryStyle = "bleue_bas"; worst.Accessory2Key = noeud.Key; worst.Accessory2Style = "dore_droite";
        worst.Accessory3Key = ballon.Key; worst.Accessory3Style = "jaune_droite";
        var held = ItemCatalog.All.ToDictionary(i => i.Key, _ => 9999);
        var data = new CosmeticScreenData(worst, held, 9_999_999_999, new string('W', 32), "La Chercheuse de trésors perdus");
        var card = CosmeticCards.Build(new CosmeticView(CosmeticTab.Wardrobe, CosmeticSlot.Accessory), data, now, new string('x', 200));
        var parts = card.Components.Select(EconomySinkChecks.Measure).ToList();
        var count = parts.Sum(p => p.Count);
        Console.WriteLine($"cosmetics screen, écharpe + nœud + ballon with every select: {count} components");
        Expect(count <= 40, $"three accessories: {count} components, over 40");

        var selects = Selects(card.Components).ToList();
        var ids = selects.Select(s => s.CustomId).Concat(parts.SelectMany(p => p.Ids)).ToList();
        Expect(ids.Count == ids.Distinct().Count(), "two accessories: a custom-id repeats: " + string.Join(", ", ids));
        Expect(ids.Contains("cosm:pick2:0") && ids.Contains("cosm:pick3:0"), "three accessories: a second or third picker is missing");
        Expect(ids.Contains($"cosm:style:0:{ballon.Key}") && ids.Contains($"cosm:style:1:{ballon.Key}"), "three accessories: the ballon's style selects are missing");
        Expect(selects.FirstOrDefault(s => s.CustomId == $"cosm:style:0:{ballon.Key}")?.Options.Single(o => o.IsDefault == true).Value == "jaune",
            "the third accessory's style selects read its own style");
        Expect(ids.Contains($"cosm:style:0:{echarpe.Key}") && ids.Contains($"cosm:style:1:{echarpe.Key}")
               && ids.Contains($"cosm:style:0:{noeud.Key}") && ids.Contains($"cosm:style:1:{noeud.Key}"), "two accessories: a style select is missing");
        Expect(selects.FirstOrDefault(s => s.CustomId == "cosm:pick:Wardrobe:0")?.Placeholder.StartsWith("Premier accessoire") == true, "first picker label");
        Expect(selects.FirstOrDefault(s => s.CustomId == "cosm:pick2:0")?.Placeholder.StartsWith("Deuxième accessoire") == true, "second picker label");
        Expect(selects.FirstOrDefault(s => s.CustomId == "cosm:pick3:0")?.Placeholder.StartsWith("Troisième accessoire") == true, "third picker label");
        var second = selects.FirstOrDefault(s => s.CustomId == $"cosm:style:0:{noeud.Key}");
        Expect(second?.Options.Single(o => o.IsDefault == true).Value == "dore", "the second accessory's style selects read its own style");
        var firstSide = selects.FirstOrDefault(s => s.CustomId == $"cosm:style:1:{echarpe.Key}");
        Expect(firstSide?.Options.Single(o => o.IsDefault == true).Value == "bas", "the first accessory's style selects read its own style");

        // Two accessories: a third picker offers a third. One: a second picker, no third. None: neither.
        worst.Accessory3Key = null; worst.Accessory3Style = null;
        var two = Selects(CosmeticCards.Build(new CosmeticView(CosmeticTab.Wardrobe, CosmeticSlot.Accessory), data, now, null).Components).ToList();
        Expect(two.Any(s => s.CustomId == "cosm:pick3:0"), "two accessories worn: the third picker should offer a third");
        worst.Accessory2Key = null; worst.Accessory2Style = null;
        var one = Selects(CosmeticCards.Build(new CosmeticView(CosmeticTab.Wardrobe, CosmeticSlot.Accessory), data, now, null).Components).ToList();
        Expect(one.Any(s => s.CustomId == "cosm:pick2:0"), "one accessory worn: the second picker should offer a second");
        Expect(!one.Any(s => s.CustomId.StartsWith("cosm:pick3:", StringComparison.Ordinal)), "one accessory worn: a third picker shows");
        worst.AccessoryKey = null; worst.AccessoryStyle = null;
        var none = Selects(CosmeticCards.Build(new CosmeticView(CosmeticTab.Wardrobe, CosmeticSlot.Accessory), data, now, null).Components).ToList();
        Expect(!none.Any(s => s.CustomId.StartsWith("cosm:pick2:", StringComparison.Ordinal)), "no accessory worn: a second picker shows");

        // The shop's try-on keeps the worn accessory and adds the tried one beside it.
        var withOne = Fullest();
        withOne.AccessoryKey = noeud.Key; withOne.Accessory2Key = null; withOne.Accessory3Key = null;
        var shopData = new CosmeticScreenData(withOne, new Dictionary<string, int>(), 0);
        var shop = CosmeticCatalog.Shop(now).FirstOrDefault(c => c.Slot == CosmeticSlot.Accessory && c.Key != noeud.Key);
        if (shop is not null)
        {
            var tried = string.Join("\n", Texts(CosmeticCards.Build(new CosmeticView(CosmeticTab.Shop, Picked: shop.Key), shopData, now, null).Components));
            Expect(tried.Contains($"Porte {noeud.Emoji} {noeud.Name} et {shop.Emoji} {shop.Name}") || CosmeticCatalog.Shop(now).Count(c => c.Slot == CosmeticSlot.Accessory) == 0,
                "try-on keeps the worn accessory beside the tried one: " + tried);
        }
    }

    private static void AccessoryNoticeWording()
    {
        const string P = "cos.accessory.";
        string? Notice(string? replaced, string? moved, string newKey, WornAccessory? first = null, WornAccessory? second = null) =>
            AccessoryNotices.Of(new AccessoryChange(new[] { first, second }.OfType<WornAccessory>().ToList(), replaced, moved), CosmeticCatalog.ByKey(P + newKey));
        string? R(string old, string neu) => Notice(P + old, null, neu);
        Expect(R("couronne", "bonnet") == "Le bonnet a remplacé la petite couronne.", "Replaced: " + R("couronne", "bonnet"));
        Expect(R("casquette", "echarpe") == "L'écharpe a remplacé la casquette.", "Replaced, elided: " + R("casquette", "echarpe"));
        Expect(R("echarpe", "lunettes") == "Les lunettes de soleil ont remplacé l'écharpe.", "Replaced, plural: " + R("echarpe", "lunettes"));
        Expect(R("bonnet", "haut_de_forme") == "Le haut-de-forme a remplacé le bonnet.", "Replaced, h aspiré: " + R("bonnet", "haut_de_forme"));
        var left = new WornAccessory(P + "fleur", "rose_gauche");
        var m1 = Notice(null, P + "fleur", "noeud", second: left);
        Expect(m1 == "La fleur est passée à gauche.", "Moved, feminine: " + m1);
        var right = new WornAccessory(P + "ballon", "rouge_droite");
        var m2 = Notice(null, P + "ballon", "tulipe", first: right);
        Expect(m2 == "Le ballon est passé à droite.", "Moved, masculine: " + m2);
        var both = Notice(P + "couronne", P + "ballon", "bonnet", first: right);
        Expect(both == "Le bonnet a remplacé la petite couronne. Le ballon est passé à droite.", "both notices: " + both);
        Expect(Notice(null, null, "bonnet") is null, "no change, no notice");
        var preview = AccessoryNotices.Of(new AccessoryChange([], P + "couronne", null), CosmeticCatalog.ByKey(P + "bonnet"), preview: true);
        Expect(preview == "Le bonnet remplacerait la petite couronne.", "try-on wording: " + preview);
    }

    private static Plynling Fullest() => new()
    {
        Name = new string('W', 32), Gender = PlynlingGender.Female, OwnerId = ulong.MaxValue,
        ThemeKey = "cos.theme.aurore_boreale", TitleKey = "cos.title.chercheur_tresors",
        AccessoryKey = "cos.accessory.echarpe", Accessory2Key = "cos.accessory.noeud", GraveKey = "cos.grave.couronne_fleurs",   // the écharpe: two style selects, the widest
        Accessory3Key = "cos.accessory.ballon",                    // and a third with two more
        AuraKey = "cos.aura.scintillements",                       // the aura line in the heading
        SickSince = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),   // and the « aura ne se voit pas » note
    };

    private static void Caps()
    {
        var everything = ItemCatalog.All.ToDictionary(i => i.Key, _ => 9999);
        var nothing = new Dictionary<string, int>();
        var worst = 0;
        // One instant in each season, so every seasonal item is in a shop once.
        var instants = new[] { 4, 7, 10, 1 }.Select(m => new DateTimeOffset(2026, m, 15, 12, 0, 0, TimeSpan.Zero));

        foreach (var now in instants)
        foreach (var plynling in new Plynling?[] { Fullest(), null })
        foreach (var held in new[] { everything, nothing })
        foreach (var balance in new long[] { 0, 9_999_999_999 })
        {
            var data = new CosmeticScreenData(plynling, held, balance,
                plynling is null ? null : new string('W', 32), plynling is null ? null : "La Chercheuse de trésors perdus");
            var views = Enum.GetValues<CosmeticSlot>().Select(s => new CosmeticView(CosmeticTab.Wardrobe, s))
                .Append(new CosmeticView(CosmeticTab.Shop))
                .Concat(CosmeticCatalog.Shop(now).Select(c => new CosmeticView(CosmeticTab.Shop, Picked: c.Key)))
                .Append(new CosmeticView(CosmeticTab.Craft))
                .Concat(CosmeticCatalog.All.Where(c => c.Source == CosmeticSource.Crafted).Select(c => new CosmeticView(CosmeticTab.Craft, Picked: c.Key)));
            foreach (var view in views)
            {
                var what = $"{view.Tab}/{view.Slot}/{view.Picked ?? "-"} ({now:MM}, {(plynling is null ? "no plynling" : "plynling")}, {held.Count} held, {balance})";
                Discord.MessageComponent card;
                try { card = CosmeticCards.Build(view, data, now, new string('x', 200)); }
                catch (Exception ex) { _fail($"cosmetics screen: {what} throws {ex.GetType().Name}: {ex.Message}"); continue; }
                var parts = card.Components.Select(EconomySinkChecks.Measure).ToList();
                var (text, count, ids) = (parts.Sum(p => p.Text), parts.Sum(p => p.Count), parts.SelectMany(p => p.Ids).ToList());
                worst = Math.Max(worst, count);
                Expect(text <= 4000 && count <= 40 && ids.Count == ids.Distinct().Count() && ids.All(i => i.Length <= 100),
                    $"{what}: {text} chars, {count} components, {ids.Count - ids.Distinct().Count()} duplicate id(s)");
                foreach (var select in Selects(card.Components))
                    Expect(select.Options.Count <= 25 && select.Options.All(o => o.Label.Length <= 100 && (o.Description?.Length ?? 0) <= 100),
                        $"{what}: select {select.CustomId} has {select.Options.Count} options or an over-long one");
            }
        }
        Console.WriteLine($"cosmetics screen at worst: {worst} components");
    }

    private static IEnumerable<Discord.SelectMenuComponent> Selects(IEnumerable<Discord.IMessageComponent> components)
    {
        foreach (var c in components)
            switch (c)
            {
                case Discord.SelectMenuComponent s: yield return s; break;
                case Discord.ContainerComponent x: foreach (var s in Selects(x.Components)) yield return s; break;
                case Discord.ActionRowComponent x: foreach (var s in Selects(x.Components)) yield return s; break;
            }
    }

    // Trying a thème on shows it in the preview and leaves what the Plynling wears untouched.
    private static void TryOnWritesNothing()
    {
        var p = Fullest();
        var data = new CosmeticScreenData(p, new Dictionary<string, int>(), 0);
        var now = new DateTimeOffset(2026, 4, 15, 12, 0, 0, TimeSpan.Zero);
        var basic = CosmeticCatalog.Shop(now).First(c => c.Slot == CosmeticSlot.Theme && c.Key != p.ThemeKey);
        var card = CosmeticCards.Build(new CosmeticView(CosmeticTab.Shop, Picked: basic.Key), data, now, null);
        Expect(p.ThemeKey == "cos.theme.aurore_boreale", "try-on wrote the thème onto the Plynling");
        var container = card.Components.OfType<Discord.ContainerComponent>().Single();
        Expect(container.AccentColor?.RawValue == basic.Accent, "try-on: the card is not in the tried thème's colour");
        Expect(Texts(card.Components).Any(t => t.StartsWith("-# Essai :")), "try-on: no « Essai » line");
    }

    private static IEnumerable<string> Texts(IEnumerable<Discord.IMessageComponent> components)
    {
        foreach (var c in components)
            switch (c)
            {
                case Discord.TextDisplayComponent t: yield return t.Content; break;
                case Discord.ContainerComponent x: foreach (var t in Texts(x.Components)) yield return t; break;
                case Discord.SectionComponent x: foreach (var t in Texts(x.Components)) yield return t; break;
            }
    }

    // A recipe names each missing ingredient with how many, then the cailloux; a ready one lacks nothing.
    private static void MissingNamesItems()
    {
        var recipe = CosmeticCatalog.All.First(c => c.Source == CosmeticSource.Crafted && c.Recipe.Count >= 2);
        var (first, need) = recipe.Recipe[0];
        var held = new Dictionary<string, int> { [first] = need - 1 };
        var missing = CosmeticCards.Missing(recipe, new CosmeticScreenData(null, held, 0));
        Expect(missing.Count == recipe.Recipe.Count + 1, $"missing: {missing.Count} entries for {recipe.Key}");
        Expect(missing[0] == $"1 × {ItemCatalog.ClearName(ItemCatalog.ByKey(first)!)}", "missing: first entry " + missing[0]);

        var ready = recipe.Recipe.ToDictionary(r => r.ItemKey, r => r.Count);
        Expect(CosmeticCards.Missing(recipe, new CosmeticScreenData(null, ready, recipe.Price)).Count == 0, "missing: a ready recipe lacks something");
    }
}
