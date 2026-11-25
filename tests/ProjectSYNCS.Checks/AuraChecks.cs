using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

// 64. Auras, the fifth slot: the six entries (one per season among them) and the slot order, worn with an accessory (each
// try-on and take-off keeps the other), the shop's existing weekly draw unchanged by the new slot,
// losing the last copy takes it off, and the bot's looks carry the aura on the aura faces only, with
// every aura layer they read under Assets/Plynlings.
public static class AuraChecks
{
    private static Action<string> _fail = _ => { };

    private static void Expect(bool ok, string what)
    {
        if (!ok) _fail("auras: " + what);
    }

    public static async Task Run(Action<string> fail)
    {
        _fail = fail;
        Catalogue();
        WornWithAnAccessory();
        ShopDrawUnchanged();
        try { await LosingTheLastTakesItOffAsync(); }
        catch (Exception ex) { Expect(false, "take-off threw: " + ex.Message); }
        SpriteOfReadsTheAura();
        Screens();
        IconsUpload();
        EveryAuraHasItsFiles();
    }

    private static IEnumerable<Discord.IMessageComponent> Flat(IEnumerable<Discord.IMessageComponent> components)
    {
        foreach (var c in components)
        {
            yield return c;
            var inner = c switch
            {
                Discord.ContainerComponent x => x.Components,
                Discord.SectionComponent x => x.Components,
                Discord.ActionRowComponent x => x.Components,
                _ => null,
            };
            if (inner is not null)
                foreach (var i in Flat(inner)) yield return i;
        }
    }

    private const string NoteM = "-# Son aura ne se voit pas tant qu'il est malade ou gelé.";
    private const string NoteF = "-# Son aura ne se voit pas tant qu'elle est malade ou gelée.";

    // The heading names the aura after the accessory; a sick or frozen Plynling's try-on says the aura
    // can't show; the Garde-robe's slots read in SlotOrder.
    private static void Screens()
    {
        var now = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
        var male = Living("cos.accessory.couronne", aura: "cos.aura.bulles");
        var heading = PlynlingCardUi.Heading(male, now);
        Expect(heading.Contains("\n🫧 Entouré de bulles") && heading.IndexOf("Porte ", StringComparison.Ordinal) < heading.IndexOf("Entouré", StringComparison.Ordinal),
            "male heading: " + heading);
        var female = Living(aura: "cos.aura.petales");
        female.Gender = PlynlingGender.Female;
        Expect(PlynlingCardUi.Heading(female, now).Contains("🌸 Entourée de pétales"), "female heading: " + PlynlingCardUi.Heading(female, now));
        Expect(!PlynlingCardUi.Heading(Living(), now).Contains("Entouré"), "a heading without an aura names one");
        // « de » elides before a vowel: « d'éclairs », never « de éclairs ».
        var stormy = Living(aura: "cos.aura.eclairs");
        Expect(PlynlingCardUi.Heading(stormy, now).Contains("Entouré d'éclairs"), "male heading, vowel: " + PlynlingCardUi.Heading(stormy, now));
        var starry = Living(aura: "cos.aura.etoiles_filantes");
        starry.Gender = PlynlingGender.Female;
        Expect(PlynlingCardUi.Heading(starry, now).Contains("Entourée d'étoiles filantes"), "female heading, vowel: " + PlynlingCardUi.Heading(starry, now));
        var koi = Living(aura: "cos.aura.koi");
        Expect(PlynlingCardUi.Heading(koi, now).Contains("Entouré de carpes koï"), "koï heading: " + PlynlingCardUi.Heading(koi, now));

        string Screen(Plynling p, string? picked = null) => string.Join("\n", Flat(CosmeticCards.Build(
            new CosmeticView(picked is null ? CosmeticTab.Wardrobe : CosmeticTab.Craft, CosmeticSlot.Aura, picked),
            new CosmeticScreenData(p, new Dictionary<string, int>(), 0), now, null).Components)
            .OfType<Discord.TextDisplayComponent>().Select(t => t.Content));

        var sick = Living(aura: "cos.aura.scintillements");
        sick.SickSince = now.AddDays(-1);
        Expect(Screen(sick).Contains(NoteM), "a sick Plynling wearing an aura: no note");
        var frozenF = Living();
        frozenF.Gender = PlynlingGender.Female;
        frozenF.FrozenAt = now.AddDays(-1);
        Expect(Screen(frozenF, "cos.aura.petales").Contains(NoteF), "a frozen Plynling trying Pétales: no feminine note");
        Expect(!Screen(Living(aura: "cos.aura.bulles")).Contains("ne se voit pas"), "a healthy Plynling gets the note");
        var sickBare = Living();
        sickBare.SickSince = now.AddDays(-1);
        Expect(!Screen(sickBare).Contains("ne se voit pas"), "a sick Plynling without an aura gets the note");

        var slots = Flat(CosmeticCards.Build(new CosmeticView(CosmeticTab.Wardrobe, CosmeticSlot.Aura),
                new CosmeticScreenData(Living(), new Dictionary<string, int>(), 0), now, null).Components)
            .OfType<Discord.ButtonComponent>().Where(b => b.CustomId?.StartsWith("cosm:slot:", StringComparison.Ordinal) == true)
            .Select(b => b.CustomId!["cosm:slot:".Length..]).ToList();
        Expect(slots.SequenceEqual(CosmeticCatalog.SlotOrder.Select(s => s.ToString())), "Garde-robe slot row: " + string.Join(", ", slots));

        // « Aucun » picked: the notice agrees with the slot's gender (« Aura » is feminine).
        Expect(CosmeticCards.TakenOff(CosmeticSlot.Aura) == "Aura retirée.", "aura take-off: " + CosmeticCards.TakenOff(CosmeticSlot.Aura));
        Expect(CosmeticCards.TakenOff(CosmeticSlot.Theme) == "Thème retiré." && CosmeticCards.TakenOff(CosmeticSlot.Grave) == "Cadre de tombe retiré.",
            "thème/cadre take-off: " + CosmeticCards.TakenOff(CosmeticSlot.Theme));
    }

    // Each aura ships an icon (tools/item-art), and the bot gives it an emoji name, or it is skipped
    // and never uploaded.
    private static void IconsUpload()
    {
        var root = StoryChecks.RepoRoot();
        var sources = ItemEmojis.Sources(Path.Combine(root, "ProjectSYNCS", "Assets", "Mushrooms"), Path.Combine(root, "ProjectSYNCS", "Assets", "Icons"))
            .ToDictionary(s => s.Key);
        foreach (var aura in CosmeticCatalog.InSlot(CosmeticSlot.Aura))
        {
            Expect(sources.TryGetValue(aura.Key, out var source), aura.Key + " has no icon file");
            Expect(source?.EmojiName == "au_" + aura.Key["cos.aura.".Length..], $"{aura.Key}: emoji name {source?.EmojiName ?? "null (never uploaded)"}");
        }
    }

    private static readonly PlynlingMood[] AuraMoods =
        { PlynlingMood.Happy, PlynlingMood.Content, PlynlingMood.Sad, PlynlingMood.Hungry, PlynlingMood.Starving, PlynlingMood.Sleeping };

    // Every shape with art, card face, dirt, accessory look (none, or one worn with every style and
    // side, as an adult in a hat is drawn tall) and aura: on an aura face the look carries the aura and
    // every layer it reads exists; on any other face it carries none (no look at all without an accessory).
    private static void EveryAuraHasItsFiles()
    {
        var now = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
        var accessories = new List<(CosmeticInfo? Accessory, string? Style)> { (null, null) };
        foreach (var acc in CosmeticCatalog.InSlot(CosmeticSlot.Accessory))
        {
            IEnumerable<string?> styles = new string?[] { null };
            foreach (var (_, options) in AccessoryStyles.Choices(acc.Key))
                styles = styles.SelectMany(s => options.Select(o => (string?)(s is null ? o.Key : s + "_" + o.Key)));
            accessories.AddRange(styles.Select(s => ((CosmeticInfo?)acc, s)));
        }
        var wanted = new HashSet<string>();
        var looks = 0;
        foreach (var species in PlynlingArt.StagedSpecies)
        foreach (var baby in new[] { false, true })
        foreach (var dirty in new[] { false, true })
        {
            var p = AccessoryArtChecks.Shaped(species, baby, dirty, now);
            foreach (var mood in Enum.GetValues<PlynlingMood>())
            foreach (var (acc, style) in accessories)
            foreach (var aura in CosmeticCatalog.InSlot(CosmeticSlot.Aura))
            {
                var look = PlynlingArt.LookOf(p, now, mood, new Outfit(null, null, acc, null, aura, style));
                var name = aura.Key["cos.aura.".Length..];
                if (!AuraMoods.Contains(mood))
                {
                    if (look?.Aura is not null) { Expect(false, $"{mood} wearing {aura.Key} draws it: {look.FileName}"); return; }
                    if (acc is null && look is not null) { Expect(false, $"{mood} wearing only {aura.Key} builds a look: {look.FileName}"); return; }
                    continue;
                }
                if (look?.Aura != name) { Expect(false, $"{mood} wearing {aura.Key} with {acc?.Key ?? "nothing"}: {look?.FileName ?? "no look"}"); return; }
                looks++;
                wanted.UnionWith(AccessoryArtChecks.LayersOf(look));
            }
        }
        var missing = wanted.Where(f => f.StartsWith("aura", StringComparison.Ordinal) && !File.Exists(Path.Combine(AccessoryArtChecks.LayersRoot, f)))
            .OrderBy(f => f).ToList();
        var auraLayers = wanted.Count(f => f.StartsWith("aura", StringComparison.Ordinal));
        // 7 shapes × 2 stages × 6 faces × every aura, and the adults' tall twins.
        var expectedLayers = 7 * 6 * 3 * CosmeticCatalog.InSlot(CosmeticSlot.Aura).Count();
        Expect(auraLayers == expectedLayers, $"{auraLayers} aura layers named, expected {expectedLayers}");
        Expect(looks > 20_000, $"only {looks} aura looks were built");
        Expect(missing.Count == 0, $"{missing.Count} aura layer(s) missing, e.g. {string.Join(", ", missing.Take(3))}");
    }

    private static void SpriteOfReadsTheAura()
    {
        var now = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
        PlynlingLook? Of(Plynling p, PlynlingMood mood = PlynlingMood.Content, Outfit? outfit = null) => PlynlingArt.LookOf(p, now, mood, outfit);

        Expect(Of(Living(aura: "cos.aura.bulles")) is { Aura: "bulles", First: null, Face: "content" }, "bulles: " + Of(Living(aura: "cos.aura.bulles"))?.FileName);
        Expect(Of(Living("cos.accessory.noeud", "bleu", "cos.aura.bulles")) is { Aura: "bulles", First: { Key: "cos.accessory.noeud", Style: "bleu_gauche" } },
            "bulles with the nœud bleu: " + Of(Living("cos.accessory.noeud", "bleu", "cos.aura.bulles"))?.FileName);
        Expect(Of(Living(aura: "cos.aura.nope")) is null, "unknown aura: " + Of(Living(aura: "cos.aura.nope"))?.FileName);
        Expect(Of(Living(aura: "cos.accessory.couronne")) is null, "an accessory key in AuraKey: " + Of(Living(aura: "cos.accessory.couronne"))?.FileName);
        foreach (var mood in new[] { PlynlingMood.Sick, PlynlingMood.Frozen })
            Expect(Of(Living("cos.accessory.couronne", aura: "cos.aura.scintillements"), mood) is { Aura: null }, $"{mood} shows the aura");
        Expect(Of(Living("cos.accessory.couronne", aura: "cos.aura.scintillements"), PlynlingMood.Angry) is null, "angry builds a look");
        var p = Living(aura: "cos.aura.bulles");
        Expect(Of(p, outfit: Outfit.Of(p).With(CosmeticCatalog.ByKey("cos.aura.petales")!))?.Aura == "petales", "try-on of Pétales");
        Expect(Of(p, outfit: Outfit.Of(p).Without(CosmeticSlot.Aura)) is null, "try-on without the aura still shows it");
        Expect(PlynlingArt.SpriteOf(p, now, PlynlingMood.Content).StartsWith("attachment://pl__", StringComparison.Ordinal),
            "an aura alone is not drawn: " + PlynlingArt.SpriteOf(p, now, PlynlingMood.Content));
    }

    private static Plynling Living(string? accessory = null, string? style = null, string? aura = null)
    {
        var now = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
        return new Plynling
        {
            Name = "Pépin", Species = PlynlingSpecies.Amanite, AdoptedAt = now.AddDays(-60), NeedsAsOf = now,
            Hygiene = 1.0, AccessoryKey = accessory, AccessoryStyle = style, AuraKey = aura,
        };
    }

    private static void Catalogue()
    {
        void Entry(string key, CosmeticSource source, ItemRarity rarity, long price, bool trophy, string name)
        {
            var c = CosmeticCatalog.ByKey(key);
            Expect(c is not null, key + " is missing");
            if (c is null) return;
            Expect(c.Slot == CosmeticSlot.Aura && c.Source == source && c.Rarity == rarity && c.Price == price && c.Trophy == trophy,
                $"{key}: {c.Slot} {c.Source} {c.Rarity} {c.Price} trophy={c.Trophy}");
            Expect(CosmeticCatalog.Label(c) == "Aura " + name && CosmeticCatalog.ShortName(c) == name, $"{key}: label « {CosmeticCatalog.Label(c)} »");
            Expect(ItemCatalog.ByKey(key)?.Kind == ItemKind.Cosmetic, key + " is not an item");
        }

        Entry("cos.aura.scintillements", CosmeticSource.Rotating, ItemRarity.Rare, 2400, false, "Scintillements");
        Entry("cos.aura.bulles", CosmeticSource.Rotating, ItemRarity.Uncommon, 1200, false, "Bulles");
        Entry("cos.aura.petales", CosmeticSource.Crafted, ItemRarity.Mythic, 300, true, "Pétales");
        Entry("cos.aura.lucioles", CosmeticSource.Seasonal, ItemRarity.Rare, 2400, false, "Lucioles d'été");
        Entry("cos.aura.feuilles", CosmeticSource.Seasonal, ItemRarity.Rare, 2400, false, "Feuilles mortes");
        Entry("cos.aura.flocons", CosmeticSource.Seasonal, ItemRarity.Rare, 2400, false, "Flocons");
        Entry("cos.aura.mouches", CosmeticSource.Rotating, ItemRarity.Common, 600, false, "Mouches");
        Entry("cos.aura.confettis", CosmeticSource.Rotating, ItemRarity.Uncommon, 1200, false, "Confettis");
        Entry("cos.aura.eclairs", CosmeticSource.Rotating, ItemRarity.Epic, 3600, false, "Éclairs");
        Entry("cos.aura.etoiles_filantes", CosmeticSource.Rotating, ItemRarity.Legendary, 5000, false, "Étoiles filantes");
        Entry("cos.aura.koi", CosmeticSource.Rotating, ItemRarity.Epic, 3600, false, "Carpes koï");
        Expect(CosmeticCatalog.InSlot(CosmeticSlot.Aura).Count() == 11, $"{CosmeticCatalog.InSlot(CosmeticSlot.Aura).Count()} auras, want 11");
        // One aura per season: Pétales crafted from spring flowers, the three others sold in their season only.
        foreach (var (key, season) in new[] { ("cos.aura.lucioles", Season.Summer), ("cos.aura.feuilles", Season.Autumn), ("cos.aura.flocons", Season.Winter) })
        {
            Expect(CosmeticCatalog.ByKey(key)?.Season == season, $"{key}: season {CosmeticCatalog.ByKey(key)?.Season}");
            foreach (var month in new[] { 1, 4, 7, 10 })
            {
                var at = new DateTimeOffset(2026, month, 15, 12, 0, 0, TimeSpan.Zero);
                var onSale = CosmeticCatalog.Shop(at).Any(c => c.Key == key);
                Expect(onSale == (ItemCatalog.SeasonAt(at) == season), $"{key} {(onSale ? "on sale" : "missing")} on {at:yyyy-MM-dd}");
            }
        }
        var petales = CosmeticCatalog.ByKey("cos.aura.petales");
        Expect(petales is not null && petales.Recipe.Select(r => r.ItemKey).OrderBy(k => k).SequenceEqual(new[] { "col.cerise", "col.primevere" }),
            "Pétales: the recipe is not primevère + cerise");
        Expect(CosmeticCatalog.SlotOrder.SequenceEqual(new[] { CosmeticSlot.Theme, CosmeticSlot.Title, CosmeticSlot.Accessory, CosmeticSlot.Aura, CosmeticSlot.Grave }),
            "slot order: " + string.Join(", ", CosmeticCatalog.SlotOrder));
        Expect(CosmeticCatalog.SlotLabel(CosmeticSlot.Aura) == "Aura" && CosmeticCatalog.SlotPlural(CosmeticSlot.Aura) == "auras", "aura labels");
    }

    private static void WornWithAnAccessory()
    {
        var bulles = CosmeticCatalog.ByKey("cos.aura.bulles")!;
        var scintillements = CosmeticCatalog.ByKey("cos.aura.scintillements")!;
        var p = Living("cos.accessory.noeud", "bleu", bulles.Key);
        var worn = Outfit.Of(p);
        Expect(worn.Aura?.Key == bulles.Key && worn.In(CosmeticSlot.Aura)?.Key == bulles.Key, "Outfit.Of does not read the aura");
        Expect(worn.In(CosmeticSlot.Grave) is null, "the aura reads as the cadre");

        var hat = worn.With(CosmeticCatalog.ByKey("cos.accessory.haut_de_forme")!);
        Expect(hat.Aura?.Key == bulles.Key, "trying an accessory took the aura off");
        var other = worn.With(scintillements);
        Expect(other.Aura?.Key == scintillements.Key && other.Accessory?.Key == "cos.accessory.noeud" && other.AccessoryStyle == "bleu",
            "trying an aura changed the accessory or its style");
        Expect(worn.Without(CosmeticSlot.Aura).Accessory?.Key == "cos.accessory.noeud" && worn.Without(CosmeticSlot.Aura).Aura is null,
            "taking the aura off");
        Expect(worn.Without(CosmeticSlot.Accessory).Aura?.Key == bulles.Key, "taking the accessory off took the aura too");

        CosmeticSlots.Set(p, CosmeticSlot.Aura, scintillements.Key);
        Expect(p.AuraKey == scintillements.Key && p.AccessoryStyle == "bleu" && p.GraveKey is null, "setting the aura touched another slot");
        Expect(CosmeticSlots.Get(p, CosmeticSlot.Aura) == scintillements.Key, "CosmeticSlots.Get(Aura)");
    }

    // The shop as it was drawn before the Aura slot: the existing slots' week must not change. The
    // seeds are pinned to the numbers the slots had (0–3), not read from the enum: a slot inserted
    // mid-list would shift both sides together and hide the reshuffle this guards against.
    private static void ShopDrawUnchanged()
    {
        var old = new[] { CosmeticSlot.Theme, CosmeticSlot.Title, CosmeticSlot.Accessory, CosmeticSlot.Grave };
        var seed = new Dictionary<CosmeticSlot, int>
        {
            [CosmeticSlot.Theme] = 0, [CosmeticSlot.Title] = 1, [CosmeticSlot.Accessory] = 2, [CosmeticSlot.Grave] = 3,
        };
        for (var week = 0; week < 60; week++)
        {
            var now = new DateTimeOffset(2026, 1, 5, 12, 0, 0, TimeSpan.Zero).AddDays(7 * week);
            var key = CosmeticCatalog.WeekKey(now);
            var expected = CosmeticCatalog.All.Where(c => c.Source == CosmeticSource.Basic && old.Contains(c.Slot)).ToList();
            foreach (var slot in old)
            {
                var pool = CosmeticCatalog.All.Where(c => c.Slot == slot && c.Source == CosmeticSource.Rotating).ToList();
                var rng = new Random(key * 10 + seed[slot]);
                for (var i = 0; i < Math.Min(CosmeticCatalog.RotatingCount(slot), pool.Count); i++)
                {
                    var j = rng.Next(i, pool.Count);
                    (pool[i], pool[j]) = (pool[j], pool[i]);
                    expected.Add(pool[i]);
                }
            }
            var season = ItemCatalog.SeasonAt(now);
            expected.AddRange(CosmeticCatalog.All.Where(c => c.Source == CosmeticSource.Seasonal && c.Season == season && old.Contains(c.Slot)).OrderBy(c => c.Slot));
            var shop = CosmeticCatalog.Shop(now).Where(c => c.Slot != CosmeticSlot.Aura).Select(c => c.Key).ToList();
            if (!shop.SequenceEqual(expected.Select(c => c.Key)))
            {
                Expect(false, $"the shop of {now:yyyy-MM-dd} changed for the existing slots");
                break;
            }
        }
    }

    // A gift or trade taking the last copy of the worn aura takes it off, like any slot.
    private static async Task LosingTheLastTakesItOffAsync()
    {
        using var conn = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        conn.Open();
        var opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
        var at = new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);
        using (var db = new AppDbContext(opts))
        {
            db.Database.EnsureCreated();
            db.InventoryItems.Add(new InventoryItem { GuildId = 1, UserId = 7, Key = "cos.aura.bulles", Quantity = 1, DiscoveredAt = at });
            db.Plynlings.Add(new Plynling
            {
                GuildId = 1, OwnerId = 7, Name = "Pépin", AdoptedAt = at, NeedsAsOf = at,
                AccessoryKey = "cos.accessory.noeud", AuraKey = "cos.aura.bulles",
            });
            db.SaveChanges();
        }
        using (var db = new AppDbContext(opts))
        {
            Expect(await InventoryService.TakeAsync(db, 1, 7, "cos.aura.bulles", 1), "could not take the aura");
            await db.SaveChangesAsync();
        }
        using (var db = new AppDbContext(opts))
        {
            var p = db.Plynlings.Single();
            Expect(p.AuraKey is null && p.AccessoryKey == "cos.accessory.noeud", $"after losing the last Bulles: aura {p.AuraKey}, accessory {p.AccessoryKey}");
        }
    }
}
