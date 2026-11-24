using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

// 64. Auras, the fifth slot: the three entries and the slot order, worn with an accessory (each
// try-on and take-off keeps the other), the shop's existing weekly draw unchanged by the new slot,
// and losing the last copy takes it off.
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
        EveryAuraHasItsFiles(Path.Combine(StoryChecks.RepoRoot(), "assets", "plynlings"));
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

    private static string FileOf(string url) => url[PlynlingArt.BaseUrl.Length..];

    // Every species, stage, card face, dirt, accessory look (or none) and aura: on an aura face the file
    // exists; on any other face the URL is exactly the one without the aura.
    private static void EveryAuraHasItsFiles(string assets)
    {
        var missing = new List<string>();
        var checkedFiles = 0;
        var looks = new List<(string? Key, string? Style)> { (null, null) };
        foreach (var acc in CosmeticCatalog.InSlot(CosmeticSlot.Accessory))
        {
            IEnumerable<string?> styles = new string?[] { null };
            foreach (var (label, options) in AccessoryStyles.Choices(acc.Key))       // the old pictures have no sides
            {
                if (label is "Côté" or "Main") continue;
                styles = styles.SelectMany(s => options.Select(o => (string?)(s is null ? o.Key : s + "_" + o.Key)));
            }
            looks.AddRange(styles.Select(s => ((string?)acc.Key, s)));
        }
        foreach (var species in Enum.GetValues<PlynlingSpecies>())
        foreach (var stage in new[] { PlynlingStage.Baby, PlynlingStage.Adult })
        foreach (var mood in Enum.GetValues<PlynlingMood>())
        foreach (var dirty in new[] { false, true })
        {
            if (!File.Exists(Path.Combine(assets, FileOf(PlynlingArt.Sprite(species, stage, mood, dirty))))) continue;   // no art
            if (dirty && FileOf(PlynlingArt.Sprite(species, stage, mood, true)) == FileOf(PlynlingArt.Sprite(species, stage, mood))) continue;
            if (stage == PlynlingStage.Baby && !PlynlingArt.StagedSpecies.Contains(species)) continue;
            foreach (var (acc, style) in looks)
            foreach (var aura in CosmeticCatalog.InSlot(CosmeticSlot.Aura))
            {
                var url = PlynlingArt.Sprite(species, stage, mood, dirty, acc, style, aura.Key);
                if (!AuraMoods.Contains(mood))
                {
                    var bare = PlynlingArt.Sprite(species, stage, mood, dirty, acc, style);
                    if (url != bare) { Expect(false, $"{mood} wearing {aura.Key} names {FileOf(url)}, not {FileOf(bare)}"); return; }
                    continue;
                }
                checkedFiles++;
                if (!File.Exists(Path.Combine(assets, FileOf(url)))) missing.Add(FileOf(url));
            }
        }
        Expect(checkedFiles == 21672, $"{checkedFiles} aura pictures named, expected 21 672");
        Expect(missing.Count == 0, $"{missing.Count} aura picture(s) missing, e.g. {string.Join(", ", missing.Take(3))}");
    }

    private static void SpriteOfReadsTheAura()
    {
        var now = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
        string Of(Plynling p, PlynlingMood mood = PlynlingMood.Content, Outfit? outfit = null) => PlynlingArt.SpriteOf(p, now, mood, outfit);
        var v = $"_v{PlynlingArt.Version}.webp";

        Expect(Of(Living(aura: "cos.aura.bulles")).EndsWith("_content_aura_bulles" + v), "bulles: " + Of(Living(aura: "cos.aura.bulles")));
        Expect(Of(Living("cos.accessory.noeud", "bleu", "cos.aura.bulles")).EndsWith("_with_noeud_bleu_aura_bulles" + v),
            "bulles with the nœud bleu: " + Of(Living("cos.accessory.noeud", "bleu", "cos.aura.bulles")));
        Expect(!Of(Living(aura: "cos.aura.nope")).Contains("_aura_"), "unknown aura: " + Of(Living(aura: "cos.aura.nope")));
        Expect(!Of(Living(aura: "cos.accessory.couronne")).Contains("_aura_"), "an accessory key in AuraKey: " + Of(Living(aura: "cos.accessory.couronne")));
        foreach (var mood in new[] { PlynlingMood.Sick, PlynlingMood.Frozen, PlynlingMood.Angry })
            Expect(!Of(Living("cos.accessory.couronne", aura: "cos.aura.scintillements"), mood).Contains("_aura_"), $"{mood} shows the aura");
        var p = Living(aura: "cos.aura.bulles");
        Expect(Of(p, outfit: Outfit.Of(p).With(CosmeticCatalog.ByKey("cos.aura.petales")!)).Contains("_aura_petales_"), "try-on of Pétales");
        Expect(!Of(p, outfit: Outfit.Of(p).Without(CosmeticSlot.Aura)).Contains("_aura_"), "try-on without the aura still shows it");
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
        Entry("cos.aura.petales", CosmeticSource.Crafted, ItemRarity.Common, 300, true, "Pétales");
        Expect(CosmeticCatalog.InSlot(CosmeticSlot.Aura).Count() == 3, $"{CosmeticCatalog.InSlot(CosmeticSlot.Aura).Count()} auras, want 3");
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
                for (var i = 0; i < Math.Min(CosmeticCatalog.RotatingPerSlot, pool.Count); i++)
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
