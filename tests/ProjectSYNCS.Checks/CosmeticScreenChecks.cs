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
    }

    private static Plynling Fullest() => new()
    {
        Name = new string('W', 32), Gender = PlynlingGender.Female, OwnerId = ulong.MaxValue,
        ThemeKey = "cos.theme.aurore_boreale", TitleKey = "cos.title.chercheur_tresors",
        AccessoryKey = "cos.accessory.collier_coquillages", GraveKey = "cos.grave.couronne_fleurs",
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
