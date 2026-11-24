using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

// 62. Accessories drawn on the sprite: every picture the bot can name for a worn accessory exists
// under assets/plynlings/ (each species, stage, card face, dirty or not, accessory and style, the
// side stripped) and as a layer under Assets/Plynlings/acc/ (every shape, face, style and side), the
// bot's styles are exactly the drawn ones, SpriteOf reads the worn or tried-on accessory and its
// style (an unknown one falls back), the style resets with the accessory, the retired accessories
// are gone, and the Garde-robe offers the style picker inside the caps.
public static class AccessoryArtChecks
{
    private static Action<string> _fail = _ => { };

    private static void Expect(bool ok, string what)
    {
        if (!ok) _fail("accessory art: " + what);
    }

    private static readonly string[] Retired = { "cos.accessory.sac_a_dos", "cos.accessory.casque_audio" };

    public static void Run(Action<string> fail)
    {
        _fail = fail;
        var assets = Path.Combine(StoryChecks.RepoRoot(), "assets", "plynlings");
        EveryLookHasItsFiles(assets);
        StylesMatchTheArt();
        SpriteOfReadsTheOutfit();
        RetiredAreGone();
        RetireMigrationRefunds();
        StyleResets();
        WardrobeStylePicker();
    }

    private static IEnumerable<CosmeticInfo> Accessories() => CosmeticCatalog.All.Where(c => c.Slot == CosmeticSlot.Accessory);

    // Every style of an accessory: the product of its choices' options, joined by « _ » in choice
    // order; a single null for an accessory without choices.
    private static IEnumerable<string?> StylesOf(string key)
    {
        IEnumerable<string?> styles = new string?[] { null };
        foreach (var (_, options) in AccessoryStyles.Choices(key))
            styles = styles.SelectMany(s => options.Select(o => (string?)(s is null ? o.Key : s + "_" + o.Key)));
        return styles;
    }

    private static string FileOf(string url) => url[PlynlingArt.BaseUrl.Length..];

    private static void EveryLookHasItsFiles(string assets)
    {
        var missing = new List<string>();
        var checkedFiles = 0;
        foreach (var species in Enum.GetValues<PlynlingSpecies>())
        foreach (var stage in new[] { PlynlingStage.Baby, PlynlingStage.Adult })
        foreach (var mood in Enum.GetValues<PlynlingMood>())
        foreach (var dirty in new[] { false, true })
        {
            var plain = PlynlingArt.Sprite(species, stage, mood, dirty);
            if (!File.Exists(Path.Combine(assets, FileOf(plain)))) continue;          // no art (the sunflowers)
            foreach (var acc in Accessories())
            foreach (var style in StylesOf(acc.Key))
            {
                var url = PlynlingArt.Sprite(species, stage, mood, dirty, acc.Key, style);
                if (mood == PlynlingMood.Angry)
                {
                    Expect(url == plain, $"angry wearing {acc.Key} names {FileOf(url)}, not the plain angry picture");
                    continue;
                }
                checkedFiles++;
                if (!File.Exists(Path.Combine(assets, FileOf(url)))) missing.Add(FileOf(url));
            }
        }
        Expect(checkedFiles > 0, "no accessory picture was checked (assets folder not found?)");
        Expect(missing.Count == 0, $"{missing.Count} accessory picture(s) missing, e.g. {string.Join(", ", missing.Take(3))}");
        EveryLayerExists();
    }

    // The runtime layers the painter can ask for: every shape, every non-angry card face, every
    // accessory with every style and side (acc/<shape>_<face>_<accessory>[_<style>].webp).
    private static void EveryLayerExists()
    {
        var layers = Path.Combine(StoryChecks.RepoRoot(), "ProjectSYNCS", "Assets", "Plynlings", "acc");
        var shapes = PlynlingArt.StagedSpecies.Select(PlynlingArt.Key).SelectMany(k => new[] { k, k + "_baby" });
        var faces = Enum.GetValues<PlynlingMood>().Where(m => m != PlynlingMood.Angry).Select(m => m.ToString().ToLowerInvariant());
        var missing = new List<string>();
        var count = 0;
        foreach (var shape in shapes)
        foreach (var face in faces)
        foreach (var acc in Accessories())
        foreach (var style in StylesOf(acc.Key))
        {
            var name = $"{shape}_{face}_{acc.Key["cos.accessory.".Length..]}{(style is null ? "" : "_" + style)}.webp";
            count++;
            if (!File.Exists(Path.Combine(layers, name))) missing.Add(name);
        }
        Expect(count > 1000, $"only {count} accessory layers were checked (layers folder not found?)");
        Expect(missing.Count == 0, $"{missing.Count} accessory layer(s) missing, e.g. {string.Join(", ", missing.Take(3))}");
    }

    // The looks drawn for the content Amanite, read back from the filenames, against the bot's.
    // (the layers, not the old pre-rendered pictures: those have no sides).
    private static void StylesMatchTheArt()
    {
        var layers = Path.Combine(StoryChecks.RepoRoot(), "ProjectSYNCS", "Assets", "Plynlings", "acc");
        const string head = "amanite_content_";
        var drawn = Directory.Exists(layers)
            ? Directory.GetFiles(layers, head + "*.webp").Select(f => Path.GetFileNameWithoutExtension(f)[head.Length..]).ToHashSet()
            : new HashSet<string>();
        var bot = Accessories().SelectMany(a => StylesOf(a.Key).Select(s => a.Key["cos.accessory.".Length..] + (s is null ? "" : "_" + s))).ToHashSet();
        Expect(drawn.SetEquals(bot),
            $"drawn looks and the bot's disagree — only drawn: [{string.Join(", ", drawn.Except(bot))}], only in the bot: [{string.Join(", ", bot.Except(drawn))}]");
    }

    private static Plynling Living(string? accessory, string? style = null)
    {
        var now = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
        return new Plynling
        {
            Name = "Pépin", Species = PlynlingSpecies.Amanite, AdoptedAt = now.AddDays(-60), NeedsAsOf = now,
            Hygiene = 1.0, AccessoryKey = accessory, AccessoryStyle = style,
        };
    }

    private static void SpriteOfReadsTheOutfit()
    {
        var now = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
        string Of(Plynling p, Outfit? outfit = null) => PlynlingArt.SpriteOf(p, now, PlynlingMood.Content, outfit);

        Expect(Of(Living("cos.accessory.couronne")).EndsWith($"_with_couronne_v{PlynlingArt.Version}.webp"), "couronne: " + Of(Living("cos.accessory.couronne")));
        Expect(Of(Living("cos.accessory.noeud")).Contains("_with_noeud_rose_"), "nœud, no style: " + Of(Living("cos.accessory.noeud")));
        Expect(Of(Living("cos.accessory.noeud", "bleu")).Contains("_with_noeud_bleu_"), "nœud bleu: " + Of(Living("cos.accessory.noeud", "bleu")));
        Expect(Of(Living("cos.accessory.noeud", "vert")).Contains("_with_noeud_rose_"), "nœud, unknown style: " + Of(Living("cos.accessory.noeud", "vert")));
        Expect(Of(Living("cos.accessory.echarpe")).Contains("_with_echarpe_rouge_haut_"), "écharpe, no style: " + Of(Living("cos.accessory.echarpe")));
        Expect(Of(Living("cos.accessory.echarpe", "verte_bas")).Contains("_with_echarpe_verte_bas_"), "écharpe verte_bas: " + Of(Living("cos.accessory.echarpe", "verte_bas")));
        Expect(Of(Living("cos.accessory.echarpe", "verte_nulle")).Contains("_with_echarpe_verte_haut_"), "écharpe, half-known style: " + Of(Living("cos.accessory.echarpe", "verte_nulle")));
        Expect(!Of(Living("cos.accessory.nope")).Contains("_with_"), "unknown accessory: " + Of(Living("cos.accessory.nope")));
        Expect(!Of(Living(null)).Contains("_with_"), "no accessory: " + Of(Living(null)));

        var p = Living("cos.accessory.noeud", "bleu");
        var tried = Outfit.Of(p).With(CosmeticCatalog.ByKey("cos.accessory.haut_de_forme")!);
        Expect(Of(p, tried).Contains("_with_haut_de_forme_"), "try-on: " + Of(p, tried));
        Expect(Of(p, Outfit.Of(p)).Contains("_with_noeud_bleu_"), "its own outfit keeps the style: " + Of(p, Outfit.Of(p)));
        Expect(!Of(p, Outfit.Of(p).Without(CosmeticSlot.Accessory)).Contains("_with_"), "try-on without the accessory still shows one");
    }

    private static void RetiredAreGone()
    {
        foreach (var key in Retired)
        {
            Expect(CosmeticCatalog.ByKey(key) is null, key + " is still in the catalogue");
            Expect(ItemCatalog.ByKey(key) is null, key + " is still an item");
            for (var week = 0; week < 60; week++)
            {
                var when = new DateTimeOffset(2026, 1, 5, 12, 0, 0, TimeSpan.Zero).AddDays(7 * week);
                if (CosmeticCatalog.Shop(when).Any(c => c.Key == key)) { _fail($"accessory art: {key} is in the shop of {when:yyyy-MM-dd}"); break; }
            }
        }
    }

    // The data migration on a real SQLite: the schema up to the migration before it, someone holding
    // two sacs à dos and a casque (wearing the casque) and someone else's lunettes, then the retirement.
    private static void RetireMigrationRefunds()
    {
        using var conn = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        conn.Open();
        var opts = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ProjectSYNCS.Data.AppDbContext>().UseSqlite(conn).Options;
        using var db = new ProjectSYNCS.Data.AppDbContext(opts);
        var migrator = db.GetService<IMigrator>();
        migrator.Migrate("AddAccessoryStyle");
        var at = new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);
        db.PebbleWallets.Add(new PebbleWallet { GuildId = 1, UserId = 7, Balance = 100 });
        db.PebbleWallets.Add(new PebbleWallet { GuildId = 1, UserId = 8, Balance = 50 });
        db.InventoryItems.Add(new InventoryItem { GuildId = 1, UserId = 7, Key = "cos.accessory.sac_a_dos", Quantity = 2, DiscoveredAt = at });
        db.InventoryItems.Add(new InventoryItem { GuildId = 1, UserId = 7, Key = "cos.accessory.casque_audio", Quantity = 1, DiscoveredAt = at });
        db.InventoryItems.Add(new InventoryItem { GuildId = 1, UserId = 8, Key = "cos.accessory.lunettes", Quantity = 1, DiscoveredAt = at });
        // given one, never had a wallet
        db.InventoryItems.Add(new InventoryItem { GuildId = 1, UserId = 9, Key = "cos.accessory.casque_audio", Quantity = 1, DiscoveredAt = at });
        db.SaveChanges();
        db.ChangeTracker.Clear();
        // The Plynlings go in by SQL: the EF model has every later column (AuraKey…), this schema doesn't.
        InsertAtOldSchema(conn, "Plynlings", new() { ["GuildId"] = 1, ["OwnerId"] = 7, ["Name"] = "Pépin", ["AccessoryKey"] = "cos.accessory.casque_audio", ["AccessoryStyle"] = "x" });
        InsertAtOldSchema(conn, "Plynlings", new() { ["GuildId"] = 1, ["OwnerId"] = 8, ["Name"] = "Mousse", ["AccessoryKey"] = "cos.accessory.lunettes" });

        migrator.Migrate();
        var wallets = db.PebbleWallets.ToDictionary(w => w.UserId, w => w.Balance);
        Expect(wallets[7] == 100 + 3 * 1200, $"retirement: the holder of 3 retired accessories has {wallets[7]}, expected {100 + 3 * 1200}");
        Expect(wallets[8] == 50, $"retirement: someone without one was paid ({wallets[8]})");
        Expect(wallets.GetValueOrDefault(9UL) == 1200, $"retirement: a holder with no wallet got {wallets.GetValueOrDefault(9UL)}, expected 1200 in a new wallet");
        var logged = db.EconomyDailyStats.Where(s => s.GuildId == 1 && s.Metric == EconomyLog.EarnAdmin).Sum(s => s.Value);
        Expect(logged == 4 * 1200, $"retirement: the economy log records {logged} cailloux refunded, expected {4 * 1200}");
        var keys = db.InventoryItems.Select(i => i.Key).ToList();
        Expect(!keys.Any(Retired.Contains) && keys.Contains("cos.accessory.lunettes"), "retirement: inventory rows left " + string.Join(", ", keys));
        var worn = db.Plynlings.Select(p => new { p.OwnerId, p.AccessoryKey, p.AccessoryStyle }).ToList()
            .ToDictionary(p => p.OwnerId, p => (p.AccessoryKey, p.AccessoryStyle));
        Expect(worn[7] == (null, null), $"retirement: Pépin still wears {worn[7]}");
        Expect(worn[8].AccessoryKey == "cos.accessory.lunettes", "retirement: Mousse lost her lunettes");
    }

    // One row into a table at whatever schema the database is at: the given values, and a zero (or
    // empty text) for every other NOT NULL column without a default.
    private static void InsertAtOldSchema(Microsoft.Data.Sqlite.SqliteConnection conn, string table, Dictionary<string, object> values)
    {
        var columns = new List<(string Name, string Type)>();
        using (var info = conn.CreateCommand())
        {
            info.CommandText = $"PRAGMA table_info(\"{table}\")";
            using var r = info.ExecuteReader();
            while (r.Read())
                if (r.GetInt32(5) == 0 && (values.ContainsKey(r.GetString(1)) || (r.GetInt32(3) == 1 && r.IsDBNull(4))))
                    columns.Add((r.GetString(1), r.GetString(2)));
        }
        using var insert = conn.CreateCommand();
        insert.CommandText = $"INSERT INTO \"{table}\" ({string.Join(", ", columns.Select(c => $"\"{c.Name}\""))}) " +
                             $"VALUES ({string.Join(", ", columns.Select((_, i) => "$p" + i))})";
        for (var i = 0; i < columns.Count; i++)
            insert.Parameters.AddWithValue("$p" + i, values.TryGetValue(columns[i].Name, out var v) ? v
                : columns[i].Type.Equals("TEXT", StringComparison.OrdinalIgnoreCase) ? "" : 0);
        insert.ExecuteNonQuery();
    }

    private static void StyleResets()
    {
        // A style belongs to its accessory: another one shows its own (saved or default), and the first comes back as it was.
        var p = Living("cos.accessory.echarpe", "bleue_bas");
        CosmeticSlots.Set(p, CosmeticSlot.Accessory, "cos.accessory.echarpe");
        Expect(p.AccessoryStyle == "bleue_bas", "wearing the same accessory again lost its style");
        CosmeticSlots.Set(p, CosmeticSlot.Accessory, "cos.accessory.noeud");
        Expect(p.AccessoryStyle == "rose_gauche", "wearing another accessory kept the old style: " + p.AccessoryStyle);
        CosmeticSlots.Set(p, CosmeticSlot.Accessory, null);
        Expect(p.AccessoryKey is null && p.AccessoryStyle is null, "taking the accessory off kept it");
        CosmeticSlots.Set(p, CosmeticSlot.Accessory, "cos.accessory.echarpe");
        Expect(p.AccessoryStyle == "bleue_bas", "wearing the écharpe again did not restore « bleue_bas »: " + p.AccessoryStyle);
        CosmeticSlots.Set(p, CosmeticSlot.Theme, "cos.theme.prairie");
        p.AccessoryStyle = "bleue_haut";
        CosmeticSlots.Set(p, CosmeticSlot.Theme, "cos.theme.ocean");
        Expect(p.AccessoryStyle == "bleue_haut", "changing the thème touched the accessory's style");
    }

    // On the Accessoire slot, wearing the écharpe: one select per choice, the current option picked.
    private static void WardrobeStylePicker()
    {
        var now = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
        var p = Living("cos.accessory.echarpe", "bleue_bas");
        var held = new Dictionary<string, int> { ["cos.accessory.echarpe"] = 1 };
        var card = CosmeticCards.Build(new CosmeticView(CosmeticTab.Wardrobe, CosmeticSlot.Accessory), new CosmeticScreenData(p, held, 0), now, null);
        var selects = Selects(card.Components).Where(s => s.CustomId.StartsWith("cosm:style:", StringComparison.Ordinal)).ToList();
        Expect(selects.Count == 2, $"écharpe: {selects.Count} style select(s), expected 2 (couleur, position)");
        if (selects.Count == 2)
        {
            Expect(selects[0].CustomId == "cosm:style:0:cos.accessory.echarpe" && selects[0].Options.Count == 3, "écharpe colour select: " + selects[0].CustomId);
            Expect(selects[1].CustomId == "cosm:style:1:cos.accessory.echarpe" && selects[1].Options.Count == 2, "écharpe position select: " + selects[1].CustomId);
            Expect(selects[0].Options.Single(o => o.IsDefault == true).Value == "bleue", "écharpe colour select: the worn colour is not the selected option");
            Expect(selects[1].Options.Single(o => o.IsDefault == true).Value == "bas", "écharpe position select: the worn place is not the selected option");
        }

        var plain = CosmeticCards.Build(new CosmeticView(CosmeticTab.Wardrobe, CosmeticSlot.Accessory),
            new CosmeticScreenData(Living("cos.accessory.couronne"), new Dictionary<string, int> { ["cos.accessory.couronne"] = 1 }, 0), now, null);
        Expect(!Selects(plain.Components).Any(s => s.CustomId.StartsWith("cosm:style:", StringComparison.Ordinal)), "couronne: a style select for an accessory without styles");
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
}
