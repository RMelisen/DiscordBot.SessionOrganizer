using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

// 61. Crafting (Recipes, CosmeticService.CraftAsync, the recipe catalog): « any commons of a set »
// ingredients — what counts, which stacks a craft takes, how a lack is named — and crafting
// against a real in-memory SQLite, so every query must translate.
public static class CraftingChecks
{
    private static Action<string> _fail = _ => { };
    private const ulong G = 1000, Me = 42;

    private static void Expect(bool ok, string what)
    {
        if (!ok) _fail("crafting: " + what);
    }

    // A fresh database per scenario; the connection keeps the in-memory database alive.
    private sealed class Db : IDisposable
    {
        private readonly Microsoft.Data.Sqlite.SqliteConnection _conn = new("DataSource=:memory:");
        private readonly DbContextOptions<AppDbContext> _opts;

        public Db()
        {
            _conn.Open();
            _opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
            using var c = New();
            c.Database.EnsureCreated();
        }

        public AppDbContext New() => new(_opts);

        public void Give(long balance, IReadOnlyDictionary<string, int> items)
        {
            using var c = New();
            c.PebbleWallets.Add(new PebbleWallet { GuildId = G, UserId = Me, Balance = balance });
            foreach (var (key, n) in items)
                c.InventoryItems.Add(new InventoryItem { GuildId = G, UserId = Me, Key = key, Quantity = n, DiscoveredAt = DateTimeOffset.UnixEpoch });
            c.SaveChanges();
        }

        public Dictionary<string, int> Held()
        {
            using var c = New();
            return c.InventoryItems.Where(i => i.GuildId == G && i.UserId == Me).ToList().ToDictionary(i => i.Key, i => i.Quantity);
        }

        public void Dispose() => _conn.Dispose();
    }

    public static async Task Run(Action<string> fail)
    {
        _fail = fail;
        AnyCommon();
        try { await CraftNamedAsync(); }
        catch (Exception ex) { Expect(false, "named craft threw: " + ex.Message); }
        Catalog();
        Bands();
        Rarities();
        Collections();
        NowReady();
        try { await CraftAnyCommonAsync(); }
        catch (Exception ex) { Expect(false, "any-common craft threw: " + ex.Message); }
    }

    private static IEnumerable<CosmeticInfo> Crafted => CosmeticCatalog.All.Where(c => c.Source == CosmeticSource.Crafted);

    // The six tiers (trade offers, item lines and the shop print them; a Chef-d'œuvre once read
    // « commun »). Mythique is the Chefs-d'œuvre's and nothing else's; an Ouvrage is peu commun; a
    // seasonal item picks peu commun or rare and costs its tier; collectibles keep four tiers.
    private static void Rarities()
    {
        foreach (var c in CosmeticCatalog.All)
        {
            Expect(ItemCatalog.ByKey(c.Key)?.Rarity == c.Rarity, $"{c.Key}: the item's rarity differs from the cosmetic's");
            Expect(c.Rarity == ItemRarity.Mythic == (c.Source == CosmeticSource.Crafted && c.Trophy), $"{c.Key}: mythique is the Chefs-d'œuvre's tier");
            switch (c.Source)
            {
                case CosmeticSource.Crafted when !c.Trophy:
                    Expect(c.Rarity == ItemRarity.Uncommon, $"{c.Key}: an Ouvrage is peu commun, not {c.Rarity}");
                    break;
                case CosmeticSource.Seasonal:
                    Expect(c.Rarity is ItemRarity.Uncommon or ItemRarity.Rare, $"{c.Key}: a seasonal item is peu commun or rare, not {c.Rarity}");
                    Expect(c.Price == CosmeticCatalog.TierPrice(c.Rarity), $"{c.Key}: price {c.Price}, its tier says {CosmeticCatalog.TierPrice(c.Rarity)}");
                    break;
                case CosmeticSource.Rotating:
                    Expect(c.Price == CosmeticCatalog.TierPrice(c.Rarity), $"{c.Key}: price {c.Price}, its tier says {CosmeticCatalog.TierPrice(c.Rarity)}");
                    break;
            }
        }
        Expect(ItemCatalog.Collectibles.All(i => i.Rarity is not (ItemRarity.Epic or ItemRarity.Mythic)), "a collectible is épique or mythique");
        Expect(Enum.GetValues<CosmeticSlot>().All(slot => CosmeticCatalog.Shop(DateTimeOffset.UtcNow).Count(c => c.Slot == slot && c.Source == CosmeticSource.Rotating)
                == Math.Min(CosmeticCatalog.RotatingCount(slot), CosmeticCatalog.All.Count(c => c.Slot == slot && c.Source == CosmeticSource.Rotating))),
            "the shop's rotating count per slot");
        // Two basics in every slot but the Aura, so a newcomer has a choice from day one.
        foreach (var slot in new[] { CosmeticSlot.Theme, CosmeticSlot.Title, CosmeticSlot.Accessory, CosmeticSlot.Grave })
        {
            var basics = CosmeticCatalog.All.Count(c => c.Source == CosmeticSource.Basic && c.Slot == slot);
            Expect(basics == 2, $"{slot}: {basics} basic(s), want 2");
        }
    }

    // Cosmetic collections (a label on the inventory's garde-robe page): real keys, each cosmetic in
    // at most one, names unique, at least two pieces each, and the progress line reads right.
    private static void Collections()
    {
        var all = CosmeticCollections.All;
        foreach (var c in all)
        {
            Expect(c.Keys.Count >= 2, $"collection {c.Name}: {c.Keys.Count} piece(s)");
            foreach (var key in c.Keys)
                Expect(CosmeticCatalog.ByKey(key) is not null, $"collection {c.Name}: {key} is not a cosmetic");
        }
        foreach (var twice in all.SelectMany(c => c.Keys).GroupBy(k => k).Where(g => g.Count() > 1))
            Expect(false, $"{twice.Key} is in {twice.Count()} collections");
        Expect(all.Select(c => c.Name).Distinct().Count() == all.Count, "two collections share a name");
        Expect(CosmeticCollections.Of("cos.accessory.bonnet")?.Name == "Grand-Froid" && CosmeticCollections.Of("cos.accessory.casquette") is null,
            "Of: the bonnet is Grand-Froid, the casquette in none");
        // Grand Illusionniste became Gentleman: the magician's pieces left, the gentleman's came in.
        var gentleman = all.FirstOrDefault(c => c.Name == "Gentleman")?.Keys ?? Array.Empty<string>();
        foreach (var key in new[] { "cos.title.mysterieux", "cos.title.fripon", "cos.accessory.haut_de_forme",
                     "cos.theme.velours", "cos.title.distingue", "cos.title.magnifique", "cos.grave.roses",
                     "cos.accessory.noeud_papillon", "cos.accessory.monocle", "cos.accessory.canne", "cos.accessory.montre_gousset" })
            Expect(gentleman.Contains(key), $"collection Gentleman: {key} missing");
        Expect(!gentleman.Contains("cos.grave.colombes") && all.All(c => c.Name != "Grand Illusionniste"), "collection Gentleman: the magician is still there");

        var froid = all.First(c => c.Name == "Grand-Froid");
        Expect(CosmeticCollections.Progress(_ => false) is null, "progress shown with nothing owned");
        var some = CosmeticCollections.Progress(k => k == froid.Keys[0] || k == froid.Keys[1]);
        Expect(some == $"Grand-Froid 2/{froid.Keys.Count}", "progress for two pieces: " + some);
        var full = CosmeticCollections.Progress(froid.Keys.Contains);
        Expect(full == $"**Grand-Froid {froid.Keys.Count}/{froid.Keys.Count}**", "progress for a complete one: " + full);
    }

    // A Chef-d'œuvre's cailloux follow its effort: 200 under 90 aimed forages, 300 up to 115, 400 above.
    // A recipe retuned across a line moves its price with it.
    private static long TrophyPrice(double forages) => forages < 90 ? 200 : forages <= 115 ? 300 : 400;

    // Two tiers: 14 Ouvrages (each from one place, every place served, la Lisière's from any common
    // mushrooms) and 14 trophies. The Aura slot has no Ouvrage: its three effects were fixed by
    // design, and its one recipe is a trophy (Pétales).
    private static readonly Dictionary<CosmeticSlot, int> OuvragesPerSlot = new()
    {
        [CosmeticSlot.Theme] = 2, [CosmeticSlot.Title] = 5, [CosmeticSlot.Accessory] = 3, [CosmeticSlot.Grave] = 4, [CosmeticSlot.Aura] = 0,
    };

    private static void Catalog()
    {
        var ouvrages = Crafted.Where(c => !c.Trophy).ToList();
        Expect(Crafted.Count() == 28 && ouvrages.Count == 14, $"{Crafted.Count()} recipes, {ouvrages.Count} Ouvrages; want 28 and 14");
        foreach (var slot in Enum.GetValues<CosmeticSlot>())
            Expect(ouvrages.Count(c => c.Slot == slot) == OuvragesPerSlot[slot],
                $"{slot}: {ouvrages.Count(c => c.Slot == slot)} Ouvrages, want {OuvragesPerSlot[slot]}");

        foreach (var c in Crafted)
        {
            foreach (var (key, count) in c.Recipe)
                Expect(count > 0 && Recipes.Items(key).Count > 0 && Recipes.Items(key).All(i => i.Kind == ItemKind.Collectible),
                    $"{c.Key}: ingredient {key} is not a collectible");
            Expect(c.Trophy ? c.Price is 200 or 300 or 400 : c.Price == 100, $"{c.Key}: price {c.Price}");
            Expect(ItemCatalog.All.Count(i => i.Name == c.Name) == 0 && CosmeticCatalog.All.Count(o => o.Name == c.Name) == 1,
                $"{c.Key}: name « {c.Name} » is used twice");
        }

        foreach (var c in ouvrages)
        {
            var places = c.Recipe.Select(r => Recipes.Items(r.ItemKey).SelectMany(ForagePlaces.Of).Distinct().ToList()).ToList();
            var shared = places.Aggregate((a, b) => a.Intersect(b).ToList());
            Expect(shared.Count > 0, $"{c.Key}: its ingredients share no place");
            var named = c.Recipe.Where(r => !Recipes.IsAnyCommon(r.ItemKey, out _)).Select(r => ItemCatalog.ByKey(r.ItemKey)!).ToList();
            Expect(named.All(i => i.Rarity is ItemRarity.Common or ItemRarity.Uncommon) && named.Count(i => i.Rarity == ItemRarity.Uncommon) <= 1,
                $"{c.Key}: an Ouvrage takes commons and at most one peu commun");
            if (shared.Any(p => p.Key == "lisiere"))
                Expect(c.Recipe.Any(r => r.ItemKey == Recipes.AnyCommon("champignons")), $"{c.Key}: la Lisière's Ouvrage must take any common mushrooms");
        }
        foreach (var place in ForagePlaces.All)
            Expect(ouvrages.Any(c => c.Recipe.All(r => Recipes.Items(r.ItemKey).Any(i => ForagePlaces.Of(i).Contains(place)))),
                $"{place.Key} has no Ouvrage");
    }

    // How long each recipe takes a player who aims: the bands keep every recipe reachable.
    private static void Bands()
    {
        foreach (var c in Crafted)
        {
            var mean = ExpectedForages(c);
            var (lo, hi) = c.Trophy ? (60, 150) : (10, 25);
            Console.WriteLine($"recipe {c.Key}: {mean:0} forages ({(c.Trophy ? "trophy" : "ouvrage")})");
            Expect(mean >= lo && mean <= hi, $"{c.Key}: {mean:0} forages aimed, want {lo}–{hi}");
            if (c.Trophy)
                Expect(c.Price == TrophyPrice(mean), $"{c.Key}: {mean:0} forages costs {TrophyPrice(mean)} cailloux, not {c.Price}");
        }
    }

    /// <summary>
    /// The mean number of forages to gather a recipe's items from nothing, aiming each time at the
    /// place whose items cover the most still-missing units (ties: ForagePlaces.All order), with the
    /// real draw, in the season its seasonal ingredients share (mid-April when none).
    /// </summary>
    public static double ExpectedForages(CosmeticInfo recipe, int runs = 300, int seed = 61)
    {
        var seasons = recipe.Recipe.SelectMany(r => Recipes.Items(r.ItemKey)).Select(i => i.Season).Where(s => s != Season.None).Distinct().ToList();
        var month = seasons.FirstOrDefault() switch { Season.Summer => 7, Season.Autumn => 10, Season.Winter => 1, _ => 4 };
        var now = new DateTimeOffset(2026, month, 15, 12, 0, 0, TimeSpan.Zero);
        var rng = new Random(seed);
        var total = 0;
        for (var run = 0; run < runs; run++)
        {
            var held = new Dictionary<string, int>();
            var n = 0;
            while (!Recipes.ItemsReady(recipe, held) && n < 2000)
            {
                var place = ForagePlaces.All.MaxBy(p => recipe.Recipe.Sum(r =>
                    Recipes.Items(r.ItemKey).Any(i => ForagePlaces.Of(i).Contains(p)) ? Math.Max(0, r.Count - Recipes.Have(r.ItemKey, held)) : 0))!;
                var (item, _) = ForagePlaces.Draw(rng, now, place);
                held[item.Key] = held.GetValueOrDefault(item.Key) + 1;
                n++;
            }
            total += n;
        }
        return (double)total / runs;
    }

    // « Il a tout pour fabriquer… »: only the find that completes a recipe's items, once, never for one owned.
    private static void NowReady()
    {
        var named = Crafted.First(c => !c.Trophy && c.Recipe.All(r => !Recipes.IsAnyCommon(r.ItemKey, out _)));
        var full = named.Recipe.ToDictionary(r => r.ItemKey, r => r.Count);
        var (lastKey, _) = named.Recipe[0];
        var oneShort = new Dictionary<string, int>(full) { [lastKey] = full[lastKey] - 1 };
        Expect(Recipes.NowReady(oneShort, lastKey).Contains(named), $"{named.Key}: the last ingredient found does not announce it");
        Expect(!Recipes.NowReady(full, lastKey).Contains(named), $"{named.Key}: announced again when already ready");
        Expect(!Recipes.NowReady(new Dictionary<string, int>(oneShort) { [named.Key] = 1 }, lastKey).Contains(named), $"{named.Key}: announced though owned");
        Expect(!Recipes.NowReady(oneShort, "col.meteorite").Contains(named), $"{named.Key}: announced for an unrelated find");

        var any = Crafted.First(c => c.Recipe.Any(r => r.ItemKey == Recipes.AnyCommon("champignons")) && c.Recipe.Count == 1);
        var need = any.Recipe[0].Count;
        var almost = new Dictionary<string, int> { ["col.champignon_paris"] = need - 1 };
        Expect(Recipes.NowReady(almost, "col.enoki").Contains(any), $"{any.Key}: a common mushroom completing it does not announce it");
        Expect(!Recipes.NowReady(almost, "col.girolle").Contains(any), $"{any.Key}: an uncommon mushroom announced it");

        // The reply's line: nothing when nothing, the first by name, the rest counted, agreed.
        Expect(PlynlingText.NowReady(PlynlingGender.Male, null) == "" && PlynlingText.NowReady(PlynlingGender.Male, Array.Empty<CosmeticInfo>()) == "",
            "ready line shown with nothing ready");
        var one = PlynlingText.NowReady(PlynlingGender.Female, new[] { named });
        Expect(one.Contains("Elle a tout") && one.Contains(CosmeticCatalog.Label(named)) && !one.Contains("autre"), "ready line for one: " + one);
        var three = PlynlingText.NowReady(PlynlingGender.Male, new[] { named, any, named });
        Expect(three.Contains("Il a tout") && three.Contains("(et 2 autres)"), "ready line for three: " + three);
    }

    // La Lisière's Ouvrage, from any common mushrooms: the biggest stacks go first.
    private static async Task CraftAnyCommonAsync()
    {
        var recipe = Crafted.FirstOrDefault(c => !c.Trophy && c.Recipe.Any(r => r.ItemKey == Recipes.AnyCommon("champignons")));
        if (recipe is null) { Expect(false, "no Ouvrage takes any common mushrooms"); return; }
        var need = recipe.Recipe.First(r => r.ItemKey == Recipes.AnyCommon("champignons")).Count;
        var now = new DateTimeOffset(2026, 4, 15, 12, 0, 0, TimeSpan.Zero);
        var others = recipe.Recipe.Where(r => !Recipes.IsAnyCommon(r.ItemKey, out _)).ToDictionary(r => r.ItemKey, r => r.Count);

        using (var db = new Db())
        {
            // need + 2 commons: Paris holds need − 1, Énoki 2, Shimeji 1 → Paris levels down to Énoki's.
            var items = new Dictionary<string, int>(others) { ["col.champignon_paris"] = need - 1, ["col.enoki"] = 2, ["col.shimeji"] = 1 };
            db.Give(recipe.Price, items);
            CosmeticResult r;
            using (var c = db.New()) r = await new CosmeticService(c).CraftAsync(G, Me, recipe.Key, now);
            var held = db.Held();
            var mushrooms = held.GetValueOrDefault("col.champignon_paris") + held.GetValueOrDefault("col.enoki") + held.GetValueOrDefault("col.shimeji");
            Expect(r.Outcome == CosmeticOutcome.Done && held.GetValueOrDefault(recipe.Key) == 1, $"any-common craft: {r.Outcome}");
            Expect(mushrooms == 2 && held.GetValueOrDefault("col.shimeji") == 1, $"any-common craft left Paris {held.GetValueOrDefault("col.champignon_paris")}, Énoki {held.GetValueOrDefault("col.enoki")}, Shimeji {held.GetValueOrDefault("col.shimeji")}; want two left, Shimeji kept");
        }
        using (var db = new Db())
        {
            var items = new Dictionary<string, int>(others) { ["col.champignon_paris"] = need - 1 };
            db.Give(recipe.Price, items);
            CosmeticResult r;
            using (var c = db.New()) r = await new CosmeticService(c).CraftAsync(G, Me, recipe.Key, now);
            Expect(r.Outcome == CosmeticOutcome.MissingIngredients && db.Held().GetValueOrDefault("col.champignon_paris") == need - 1,
                $"any-common craft one short: {r.Outcome}");
        }
    }

    private static void AnyCommon()
    {
        var any = Recipes.AnyCommon("champignons");
        Expect(Recipes.IsAnyCommon(any, out var set) && set == "champignons", $"IsAnyCommon({any})");
        Expect(!Recipes.IsAnyCommon("col.galet", out _), "a named item read as any-common");

        var held = new Dictionary<string, int> { ["col.champignon_paris"] = 5, ["col.enoki"] = 2, ["col.girolle"] = 9 };
        Expect(Recipes.Have(any, held) == 7, $"Have: {Recipes.Have(any, held)}, want 7 (the uncommon Girolle does not count)");
        Expect(Recipes.Have("col.girolle", held) == 9, "Have of a named item");

        var take = Recipes.Take(any, 6, held);
        Expect(take.SequenceEqual(new[] { ("col.champignon_paris", 5), ("col.enoki", 1) }),
            "Take 6: " + string.Join(", ", take.Select(t => $"{t.ItemKey}×{t.Count}")));
        Expect(Recipes.Take("col.galet", 3, held).SequenceEqual(new[] { ("col.galet", 3) }), "Take of a named item");

        // Levelling: {5, 4, 1} giving 8 leaves one copy each of the two biggest-but-last stacks.
        var three = new Dictionary<string, int> { ["col.champignon_paris"] = 5, ["col.enoki"] = 4, ["col.shimeji"] = 1 };
        var left = new Dictionary<string, int>(three);
        foreach (var (k, n) in Recipes.Take(any, 8, three)) left[k] -= n;
        Expect(left["col.champignon_paris"] == 0 && left["col.enoki"] == 1 && left["col.shimeji"] == 1,
            $"Take 8 of {{5,4,1}} leaves {left["col.champignon_paris"]},{left["col.enoki"]},{left["col.shimeji"]}, want 0,1,1");

        var recipe = new CosmeticInfo("test", CosmeticSlot.Theme, CosmeticSource.Crafted, ItemRarity.Common, Season.None, "🍄", "Test", null, null,
            0, new[] { (any, 8) });
        Expect(Recipes.Lacking(recipe, held).SequenceEqual(new[] { "1 champignon commun" }), "Lacking 1: " + string.Join(", ", Recipes.Lacking(recipe, held)));
        var fewer = new Dictionary<string, int> { ["col.champignon_paris"] = 5 };
        Expect(Recipes.Lacking(recipe, fewer).SequenceEqual(new[] { "3 champignons communs" }), "Lacking 3: " + string.Join(", ", Recipes.Lacking(recipe, fewer)));
        Expect(!Recipes.ItemsReady(recipe, held) && Recipes.ItemsReady(recipe, three), "ItemsReady");
    }

    // A recipe of named items still crafts exactly as before.
    private static async Task CraftNamedAsync()
    {
        var recipe = CosmeticCatalog.All.First(c => c.Source == CosmeticSource.Crafted && c.Recipe.All(r => !Recipes.IsAnyCommon(r.ItemKey, out _)));
        var now = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
        using (var db = new Db())
        {
            db.Give(recipe.Price, recipe.Recipe.ToDictionary(r => r.ItemKey, r => r.Count));
            CosmeticResult r;
            using (var c = db.New()) r = await new CosmeticService(c).CraftAsync(G, Me, recipe.Key, now);
            var held = db.Held();
            Expect(r.Outcome == CosmeticOutcome.Done && r.Balance == 0, $"named craft: {r.Outcome}, balance {r.Balance}");
            Expect(recipe.Recipe.All(p => held.GetValueOrDefault(p.ItemKey) == 0) && held.GetValueOrDefault(recipe.Key) == 1, "named craft: items not taken or cosmetic not given");
            using (var c = db.New()) r = await new CosmeticService(c).CraftAsync(G, Me, recipe.Key, now);
            Expect(r.Outcome == CosmeticOutcome.AlreadyOwned, $"second craft: {r.Outcome}");
        }
        using (var db = new Db())
        {
            var shortOne = recipe.Recipe.ToDictionary(r => r.ItemKey, r => r.Count);
            shortOne[recipe.Recipe[0].ItemKey]--;
            db.Give(recipe.Price, shortOne);
            CosmeticResult r;
            using (var c = db.New()) r = await new CosmeticService(c).CraftAsync(G, Me, recipe.Key, now);
            var held = db.Held();
            Expect(r.Outcome == CosmeticOutcome.MissingIngredients, $"short craft: {r.Outcome}");
            Expect(shortOne.All(kv => held.GetValueOrDefault(kv.Key) == kv.Value), "short craft took something");
        }
    }
}
