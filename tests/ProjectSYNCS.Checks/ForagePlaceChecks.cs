using System.Text.RegularExpressions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

// 60. Forage places (ForagePlaces): every collectible is found somewhere, every place has an
// all-year common in every season, the typed names match the right place and no alias is shared,
// a placeless forage is exactly today's draw, and aiming tilts the finds toward the place.
public static class ForagePlaceChecks
{
    private static Action<string> _fail = _ => { };

    private static void Expect(bool ok, string what)
    {
        if (!ok) _fail("forage places: " + what);
    }

    // One instant in each season (Paris calendar), mid-month, noon UTC.
    public static readonly DateTimeOffset[] Seasons =
        new[] { 4, 7, 10, 1 }.Select(m => new DateTimeOffset(2026, m, 15, 12, 0, 0, TimeSpan.Zero)).ToArray();

    public static void Run(Action<string> fail)
    {
        _fail = fail;
        EveryItemHasAPlace();
        EveryPlaceHasACommon();
        Matching();
        PlacelessIsToday();
        TiltWorks();
        Lines();
        Typed();
        Book();
    }

    // Each collection's book page says where its items are found; Saisons names each place it uses.
    private static void Book()
    {
        var none = new List<ProjectSYNCS.Models.InventoryItem>();
        var done = new List<ProjectSYNCS.Models.CollectionCompletion>();
        foreach (var set in ItemCatalog.Sets)
        {
            var (page, _) = ProjectSYNCS.Commands.InventoryModule.BuildCollectionPage(1, none, done, set.Key, ProjectSYNCS.Commands.CollectionFilter.All);
            foreach (var place in ItemCatalog.InSet(set.Key).SelectMany(ForagePlaces.Of).Distinct())
                Expect(page.Description.Contains(ForagePlaces.At(place)), $"book page {set.Key} does not say « {ForagePlaces.At(place)} »");
            // The book hides what was never found (« ??? »): the where-line must not name it.
            foreach (var item in ItemCatalog.InSet(set.Key))
                Expect(!page.Description.Contains(item.Name), $"book page {set.Key} names the unfound {item.Name}");
        }
    }

    // ;forage and ;pl forage take a place, forgivingly; an unknown one is flagged, never a forage.
    private static void Typed()
    {
        var cases = new (string Text, Shortcut? Want)[]
        {
            (";forage", new Shortcut(ShortcutKind.Forage)),
            (";pl forage", new Shortcut(ShortcutKind.Forage)),
            (";forage riviere", new Shortcut(ShortcutKind.Forage, Place: "sommeille")),
            (";FORAGE La Lisière", new Shortcut(ShortcutKind.Forage, Place: "lisiere")),
            (";forage la place du puits", new Shortcut(ShortcutKind.Forage, Place: "puits")),
            (";pl forage marais", new Shortcut(ShortcutKind.Forage, Place: "mouilles")),
            (";pl FORAGE le Communal", new Shortcut(ShortcutKind.Forage, Place: "communal")),
            (";forage blabla", new Shortcut(ShortcutKind.Forage, BadPlace: true)),
            (";pl forage blabla", new Shortcut(ShortcutKind.Forage, BadPlace: true)),
            (";foragex", null),
            (";work x", new Shortcut(ShortcutKind.Unknown)),
        };
        foreach (var (text, want) in cases)
            Expect(PlynlingShortcut.Parse(text) == want, $"Parse(\"{text}\") = {PlynlingShortcut.Parse(text)}, want {want}");
        foreach (var place in ForagePlaces.All)
            Expect(PlynlingShortcut.Where.Contains(place.Aliases[0]) || PlynlingShortcut.Where.Contains(place.Aliases[1]),
                $"the « où ça ? » refusal does not name {place.Key}");
    }

    private static void EveryItemHasAPlace()
    {
        foreach (var i in ItemCatalog.Collectibles)
            Expect(ForagePlaces.Of(i).Count >= 1, $"{i.Key} has no place");
        foreach (var i in ItemCatalog.All.Where(i => i.Kind != ItemKind.Collectible))
            Expect(ForagePlaces.Of(i).Count == 0, $"{i.Key} is not a collectible but has a place");
    }

    private static void EveryPlaceHasACommon()
    {
        foreach (var place in ForagePlaces.All)
            foreach (var now in Seasons)
                Expect(ForagePlaces.ItemsAt(place).Any(i => i.Rarity == ItemRarity.Common && ItemCatalog.Findable(i, now)),
                    $"{place.Key} has no findable common on {now:yyyy'-'MM}");
    }

    private static void Matching()
    {
        var seen = new Dictionary<string, string>();
        foreach (var place in ForagePlaces.All)
            foreach (var alias in place.Aliases)
            {
                Expect(!seen.TryGetValue(alias, out var other) || other == place.Key, $"alias {alias} in {other} and {place.Key}");
                seen[alias] = place.Key;
                Expect(ForagePlaces.Match(alias)?.Key == place.Key, $"alias {alias} does not match {place.Key}");
            }

        var cases = new (string Text, string? Key)[]
        {
            ("Sommeille", "sommeille"), ("la Sommeille", "sommeille"), ("RIVIÈRE", "sommeille"),
            ("la Lisière", "lisiere"), ("lisiere", "lisiere"), ("marais", "mouilles"), ("les Mouilles", "mouilles"),
            ("le Communal", "communal"), ("la place du Puits", "puits"), ("place du puits", "puits"), ("puits", "puits"),
            ("pre", null), ("blabla", null), ("", null), ("   ", null),
            ("marais !", "mouilles"), ("à l'étang", "mouilles"), ("près de l'étang", "mouilles"), ("Pont-Vieux", "sommeille"),
            ("le pont-vieux", "sommeille"), ("la rivière.", "sommeille"),
        };
        foreach (var (text, key) in cases)
            Expect(ForagePlaces.Match(text)?.Key == key, $"Match(\"{text}\") = {ForagePlaces.Match(text)?.Key ?? "null"}, want {key ?? "null"}");
    }

    private static void PlacelessIsToday()
    {
        foreach (var now in Seasons)
            for (var s = 0; s < 1000; s++)
            {
                var today = ItemCatalog.DrawForage(new Random(s), now);
                var (item, place) = ForagePlaces.Draw(new Random(s), now, null);
                if (item.Key != today.Key) { Expect(false, $"placeless seed {s} on {now:MM}: {item.Key}, today {today.Key}"); return; }
                var places = ForagePlaces.Of(item);
                Expect(places.Count == 0 ? ForagePlaces.All.Contains(place) : places.Contains(place),
                    $"placeless {item.Key} named {place.Key}");
            }
    }

    private static void TiltWorks()
    {
        foreach (var place in ForagePlaces.All)
            foreach (var now in Seasons)
            {
                var rng = new Random(60);
                int collectibles = 0, here = 0;
                for (var n = 0; n < 20_000; n++)
                {
                    var (item, at) = ForagePlaces.Draw(rng, now, place);
                    Expect(at == place, $"aimed at {place.Key}, named {at.Key}");
                    if (item.Kind != ItemKind.Collectible) continue;
                    collectibles++;
                    if (ForagePlaces.Of(item).Contains(place)) here++;
                }
                var share = (double)here / collectibles;
                Expect(share >= 0.70, $"{place.Key} on {now:MM}: only {share:P0} of finds are from there");
            }
    }

    // Every place has its forage lines, in both genders, each naming the Plynling and the find once
    // and formatting without throwing.
    // BotResponses is internal: read through reflection, like the other sections.
    private static IReadOnlyDictionary<string, GenderedLines> Foraged =>
        (IReadOnlyDictionary<string, GenderedLines>)typeof(ChatFacts).Assembly.GetType("ProjectSYNCS.Services.BotResponses")!
            .GetField("PlynlingForaged")!.GetValue(null)!;

    private static void Lines()
    {
        foreach (var place in ForagePlaces.All)
        {
            if (!Foraged.TryGetValue(place.Key, out var lines)) { Expect(false, $"no lines for {place.Key}"); continue; }
            foreach (var (half, pool) in new[] { ("M", lines.M), ("F", lines.F) })
            {
                Expect(pool.Length >= 4, $"{place.Key} {half}: {pool.Length} lines, want 4+");
                foreach (var l in pool)
                {
                    Expect(l.Contains("{0}") && l.Contains("{1}"), $"{place.Key} {half}: {{0}} or {{1}} missing: {l}");
                    Expect(Regex.Matches(l, @"{[^}]*}").All(m => m.Value is "{0}" or "{1}"), $"{place.Key} {half}: stray brace: {l}");
                    try { _ = string.Format(l, "Pépin", "🐚 **Coquillage** (rare)"); }
                    catch (FormatException) { Expect(false, $"{place.Key} {half}: does not format: {l}"); }
                }
            }
        }
    }
}
