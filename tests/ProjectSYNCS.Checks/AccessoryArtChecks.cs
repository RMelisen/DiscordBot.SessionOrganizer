using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

// 62. Accessories drawn on the sprite: every layer the bot can ask the painter for exists under
// Assets/Plynlings (every shape, card face, dirt, accessory, style and side, aura, tall or not, and
// the marks), the bot's styles are exactly the drawn ones, SpriteOf reads the worn or tried-on
// accessories and their styles (an unknown one falls back) and draws them or falls back to the link,
// the card and the Garde-robe attach exactly the picture they show, the style resets with the
// accessory, the retired accessories are gone, and the Garde-robe offers the style picker inside the caps.
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
        EveryLookHasItsFiles();
        StylesMatchTheArt();
        SpriteOfReadsTheOutfit();
        CardsAttachTheirPicture();
        GamesJournalVisitsAndPostsAttachTheirPicture();
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

    /// <summary>The painter's layers, as the checks read them (the repo copy, not bin's).</summary>
    public static string LayersRoot => Path.Combine(StoryChecks.RepoRoot(), "ProjectSYNCS", "Assets", "Plynlings");

    // The faces whose mood extras (heart, sweat, « z », ice) the art tool exports as a marks layer
    // (export.py prints the others as « no marks for »). The painter treats a missing one as normal, so
    // only this check notices one going missing.
    private static readonly string[] MarksFaces = { "happy", "hungry", "starving", "sleeping", "sick", "frozen" };

    /// <summary>
    /// A Plynling of <paramref name="species"/> at its picture's stage, clean or dirty, wearing nothing:
    /// what a look is built from (its outfit is passed to <see cref="PlynlingArt.LookOf"/>).
    /// </summary>
    public static Plynling Shaped(PlynlingSpecies species, bool baby, bool dirty, DateTimeOffset now) => new()
    {
        Name = "Pépin", Species = species, AdoptedAt = now.AddDays(baby ? -2 : -60), LiveSince = now.AddDays(baby ? -2 : -60),
        NeedsAsOf = now, Hygiene = dirty ? 0.1 : 1.0,
    };

    /// <summary>
    /// Every layer the painter reads for <paramref name="look"/> (PlynlingPainter.Paint): the base, each
    /// accessory, and the aura on both canvases an adult can be drawn on (a hat makes it tall). The
    /// marks are optional there; <see cref="EveryLookHasItsFiles"/> checks them by face.
    /// </summary>
    public static IEnumerable<string> LayersOf(PlynlingLook look)
    {
        var shapeFace = $"{look.Shape}_{look.Face}";
        yield return Path.Combine("base", shapeFace + (look.Dirty ? "_dirty" : "") + ".webp");
        foreach (var item in new[] { look.First, look.Second }.OfType<WornAccessory>())
            yield return Path.Combine("acc", $"{shapeFace}_{item.Key["cos.accessory.".Length..]}{(item.Style is null ? "" : "_" + item.Style)}.webp");
        if (look.Aura is null) yield break;
        yield return Path.Combine("aura", $"{shapeFace}_{look.Aura}.webp");
        if (!look.Shape.EndsWith("_baby", StringComparison.Ordinal))
            yield return Path.Combine("aura", $"{shapeFace}_{look.Aura}_tall.webp");
    }

    // Every look the bot can build for a shape with art (every staged species, adult and baby), each
    // card face, clean or dirty: each accessory in every style and side, and each aura. Angry builds
    // none (it shows the link); every layer those looks read exists, and so does each marks face.
    private static void EveryLookHasItsFiles()
    {
        var now = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
        var wanted = new HashSet<string>();
        var looks = 0;
        foreach (var species in PlynlingArt.StagedSpecies)
        foreach (var baby in new[] { false, true })
        foreach (var dirty in new[] { false, true })
        {
            var p = Shaped(species, baby, dirty, now);
            foreach (var mood in Enum.GetValues<PlynlingMood>())
            {
                var outfits = Accessories().SelectMany(acc => StylesOf(acc.Key).Select(s => new Outfit(null, null, acc, null, null, s)))
                    .Concat(CosmeticCatalog.InSlot(CosmeticSlot.Aura).Select(a => new Outfit(null, null, null, null, a)));
                foreach (var outfit in outfits)
                {
                    var look = PlynlingArt.LookOf(p, now, mood, outfit);
                    if (mood == PlynlingMood.Angry)
                    {
                        if (look is not null) { Expect(false, $"angry builds a look: {look.FileName}"); return; }
                        continue;
                    }
                    if (look is null) continue;          // an aura off its faces: section 64
                    looks++;
                    wanted.UnionWith(LayersOf(look));
                }
            }
            var shape = PlynlingArt.Key(species) + (baby ? "_baby" : "");
            foreach (var face in MarksFaces) wanted.Add(Path.Combine("marks", $"{shape}_{face}.webp"));
        }
        var missing = wanted.Where(f => !File.Exists(Path.Combine(LayersRoot, f))).OrderBy(f => f).ToList();
        Expect(looks > 10_000 && wanted.Count > 6_000, $"only {looks} looks and {wanted.Count} layers were checked");
        Expect(missing.Count == 0, $"{missing.Count} layer(s) missing, e.g. {string.Join(", ", missing.Take(3))}");
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
        PlynlingLook? Look(Plynling p, Outfit? outfit = null) => PlynlingArt.LookOf(p, now, PlynlingMood.Content, outfit);
        string Items(PlynlingLook? l) => l is null ? "no look" : string.Join(" + ", new[] { l.First, l.Second }.OfType<WornAccessory>().Select(i => $"{i.Key}:{i.Style}"));
        void Wears(Plynling p, string expected, string what, Outfit? outfit = null) =>
            Expect(Items(Look(p, outfit)) == expected, $"{what}: {Items(Look(p, outfit))}, expected {expected}");

        Wears(Living("cos.accessory.couronne"), "cos.accessory.couronne:", "couronne");
        Wears(Living("cos.accessory.noeud"), "cos.accessory.noeud:rose_gauche", "nœud, no style");
        Wears(Living("cos.accessory.noeud", "bleu"), "cos.accessory.noeud:bleu_gauche", "nœud bleu (an old style without a side)");
        Wears(Living("cos.accessory.noeud", "vert"), "cos.accessory.noeud:rose_gauche", "nœud, unknown style");
        Wears(Living("cos.accessory.echarpe"), "cos.accessory.echarpe:rouge_haut", "écharpe, no style");
        Wears(Living("cos.accessory.echarpe", "verte_bas"), "cos.accessory.echarpe:verte_bas", "écharpe verte_bas");
        Wears(Living("cos.accessory.echarpe", "verte_nulle"), "cos.accessory.echarpe:verte_haut", "écharpe, half-known style");
        Expect(Look(Living("cos.accessory.nope")) is null, "unknown accessory builds a look: " + Items(Look(Living("cos.accessory.nope"))));
        Expect(Look(Living(null)) is null, "no accessory builds a look");

        var p = Living("cos.accessory.noeud", "bleu");
        var tried = Outfit.Of(p).With(CosmeticCatalog.ByKey("cos.accessory.haut_de_forme")!);
        Wears(p, "cos.accessory.haut_de_forme:", "try-on", tried);
        Wears(p, "cos.accessory.noeud:bleu_gauche", "its own outfit keeps the style", Outfit.Of(p));
        Expect(Look(p, Outfit.Of(p).Without(CosmeticSlot.Accessory)) is null, "try-on without the accessory still shows one");

        // Two, read in draw order whichever slot holds which (a held item over a head ornament).
        var two = Living("cos.accessory.ballon", "bleu_droite");
        two.Accessory2Key = "cos.accessory.noeud";
        Wears(two, "cos.accessory.noeud:rose_gauche + cos.accessory.ballon:bleu_droite", "ballon then nœud");
        // Two of the same rank (nœud + fleur): the same file name whichever slot holds which.
        var bowFirst = Living("cos.accessory.noeud");
        bowFirst.Accessory2Key = "cos.accessory.fleur";
        var flowerFirst = Living("cos.accessory.fleur");
        flowerFirst.Accessory2Key = "cos.accessory.noeud";
        Expect(Look(bowFirst) is { } bf && Look(flowerFirst) is { } ff && bf.FileName == ff.FileName,
            $"same-rank items name two files: {Look(bowFirst)?.FileName} / {Look(flowerFirst)?.FileName}");
        var dirty = Living("cos.accessory.couronne");
        dirty.Hygiene = 0.1;
        Expect(Look(dirty) is { Dirty: true, Shape: "amanite", Face: "content" }, "a dirty Plynling's look: " + Look(dirty)?.FileName);
        var baby = Living("cos.accessory.couronne");
        baby.LiveSince = now.AddDays(-2);
        Expect(Look(baby)?.Shape == "amanite_baby", "a baby's look: " + Look(baby)?.FileName);

        // What SpriteOf hands the card: the link when nothing is drawn, the attachment when it is.
        string Of(Plynling pl, PlynlingMood mood = PlynlingMood.Content) => PlynlingArt.SpriteOf(pl, now, mood);
        Expect(Of(Living(null)) == PlynlingArt.Sprite(PlynlingSpecies.Amanite, PlynlingStage.Adult, PlynlingMood.Content), "bare: " + Of(Living(null)));
        var dressed = Living("cos.accessory.noeud");
        dressed.Accessory2Key = "cos.accessory.ballon";
        Expect(Of(dressed).StartsWith("attachment://pl__", StringComparison.Ordinal), "nœud + ballon: " + Of(dressed));
        Expect(Of(dressed) == "attachment://" + Look(dressed)?.FileName, "nœud + ballon names another file than its look: " + Of(dressed));
        Expect(Of(dressed, PlynlingMood.Angry) == PlynlingArt.Sprite(PlynlingSpecies.Amanite, PlynlingStage.Adult, PlynlingMood.Angry),
            "angry with a nœud: " + Of(dressed, PlynlingMood.Angry));
        var logged = new List<string>();
        var log = PlynlingPainter.Log;
        PlynlingPainter.Log = logged.Add;
        try
        {
            var sunflower = Living("cos.accessory.noeud");
            sunflower.Species = PlynlingSpecies.Tournesol;
            Expect(Of(sunflower) == PlynlingArt.Sprite(PlynlingSpecies.Tournesol, PlynlingStage.Adult, PlynlingMood.Content),
                "a sunflower (no art) with a nœud: " + Of(sunflower));
            // Only this check draws a dressed sunflower: its failed draw is logged once, then quiet.
            Expect(logged.Count == 1, $"a sunflower with a nœud logged {logged.Count} lines, expected 1: {string.Join(" | ", logged)}");
        }
        finally { PlynlingPainter.Log = log; }
    }

    // The card and the Garde-robe built for a dressed Plynling show its picture by attachment, and
    // FilesFor hands back exactly that file; a bare one shows the link and attaches nothing. A card
    // naming a picture FilesFor leaves out would be rejected by Discord, so this is where it's caught.
    private static void CardsAttachTheirPicture()
    {
        var now = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
        var dressed = Living("cos.accessory.noeud", "bleu_droite");
        dressed.Accessory2Key = "cos.accessory.ballon";
        dressed.AuraKey = "cos.aura.bulles";
        var held = new Dictionary<string, int> { ["cos.accessory.noeud"] = 1, ["cos.accessory.ballon"] = 1, ["cos.aura.bulles"] = 1 };

        AttachesCheck("the card", PlynlingModule.BuildCard(dressed, now, null), expectFile: true);
        AttachesCheck("the Garde-robe", CosmeticCards.Build(new CosmeticView(CosmeticTab.Wardrobe, CosmeticSlot.Accessory),
            new CosmeticScreenData(dressed, held, 0), now, null), expectFile: true);
        AttachesCheck("a try-on in the shop", CosmeticCards.Build(new CosmeticView(CosmeticTab.Shop, Picked: "cos.accessory.couronne"),
            new CosmeticScreenData(dressed, held, 0), now, null), expectFile: true);
        AttachesCheck("a bare card", PlynlingModule.BuildCard(Living(null), now, null), expectFile: false);
        AttachesCheck("a bare Garde-robe", CosmeticCards.Build(new CosmeticView(CosmeticTab.Wardrobe),
            new CosmeticScreenData(Living(null), new Dictionary<string, int>(), 0), now, null), expectFile: false);
    }

    // The card's attachment:// pictures are exactly the files FilesFor attaches (none when `expectFile` is false).
    private static void AttachesCheck(string what, Discord.MessageComponent card, bool expectFile)
    {
        var media = Media(card.Components).ToList();
        var named = media.Where(u => u.StartsWith("attachment://", StringComparison.Ordinal)).Distinct().ToList();
        var files = PlynlingPainter.FilesFor(card);
        try
        {
            var names = files.Select(f => f.FileName).ToList();
            if (expectFile)
                Expect(named.Count == 1 && named[0].StartsWith("attachment://pl__", StringComparison.Ordinal),
                    $"{what}: its picture is {string.Join(", ", media)}, not one attachment://pl__…");
            else
                Expect(named.Count == 0, $"{what}: names {string.Join(", ", named)}");
            Expect(names.Count == named.Count && named.All(u => names.Contains(u["attachment://".Length..])),
                $"{what}: the card names [{string.Join(", ", named)}], FilesFor attaches [{string.Join(", ", names)}]");
        }
        finally { foreach (var f in files) f.Dispose(); }
    }

    // The game, the journal (both pages), the visit's choice, knock and ending, and the announcements
    // for a dressed Plynling: whatever picture each shows by attachment://, FilesFor attaches it.
    private static void GamesJournalVisitsAndPostsAttachTheirPicture()
    {
        var now = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
        var dressed = Living("cos.accessory.noeud", "bleu_droite");
        dressed.Accessory2Key = "cos.accessory.ballon";
        var bare = Living(null);
        bare.Name = "Nu";
        dressed.OwnerId = 1;
        bare.OwnerId = 2;

        foreach (var (p, expectFile, tag) in new[] { (dressed, true, "dressed"), (bare, false, "bare") })
        {
            var session = new ProjectSYNCS.Services.PlynlingGameSession("abc", 1, 1, 1,
                new PlynlingGameState(PlynlingGame.HideAndSeek, new Random(1)), now);
            AttachesCheck($"a game ({tag})", PlynlingPlayCards.BuildGame(session, p, now, null), expectFile);
            AttachesCheck($"a game's end ({tag})", PlynlingPlayCards.BuildGame(session, p, now, "fin"), expectFile);
            foreach (var page in new[] { 0, 1 })
                AttachesCheck($"a journal page {page} ({tag})",
                    PlynlingJournalCards.BuildJournal(p, new List<PlynlingBadge>(), new List<PlynlingJournalEntry>(), page, now), expectFile);

            var scene = VisitCatalog.Scenes[0];
            var host = Living(null);
            host.OwnerId = 3;
            var expires = now.AddHours(2);
            AttachesCheck($"a visit's choice ({tag})", PlynlingVisitCards.BuildChoice(scene, p, host, 3, expires, false, now), expectFile);
            AttachesCheck($"a visit's knock ({tag})",
                PlynlingVisitCards.BuildKnock(scene, scene.Options[0], p, host, 3, expires, now), expectFile);
            var outcome = new ProjectSYNCS.Services.VisitOutcome(p, host, Array.Empty<BadgeInfo>(), Array.Empty<BadgeInfo>(), true,
                PlynlingBond.Acquaintances, PlynlingBond.Friends, Confession.None, 1.0);
            AttachesCheck($"a visit's ending ({tag})",
                PlynlingVisitCards.BuildEnding(scene, scene.Options[0], scene.Reactions[0], outcome, now), expectFile: false);

            AttachesCheck($"a resurrection post ({tag})",
                ProjectSYNCS.Services.PlynlingAnnouncer.BuildPost("x", PlynlingArt.SpriteOf(p, now)), expectFile);
            AttachesCheck($"an abandon post ({tag})",
                ProjectSYNCS.Services.PlynlingAnnouncer.BuildPost("x", PlynlingArt.SpriteOf(p, now, PlynlingMood.Sad)), expectFile);
        }
        AttachesCheck("a death post", ProjectSYNCS.Services.PlynlingAnnouncer.BuildPost("x",
            PlynlingArt.Memorial(PlynlingSpecies.Amanite, PlynlingCatalog.MemorialTier(TimeSpan.FromDays(60)))), expectFile: false);
    }

    // Every picture a card shows: thumbnails, gallery items and files, anywhere in the tree.
    private static IEnumerable<string> Media(IEnumerable<Discord.IMessageComponent> components)
    {
        foreach (var c in components)
            switch (c)
            {
                case Discord.ContainerComponent x: foreach (var u in Media(x.Components)) yield return u; break;
                case Discord.SectionComponent x:
                    foreach (var u in Media(x.Components)) yield return u;
                    if (x.Accessory is not null) foreach (var u in Media(new[] { x.Accessory })) yield return u;
                    break;
                case Discord.ActionRowComponent x: foreach (var u in Media(x.Components)) yield return u; break;
                case Discord.ThumbnailComponent { Media.Url: { } u }: yield return u; break;
                case Discord.MediaGalleryComponent x: foreach (var i in x.Items) if (i.Media.Url is { } u) yield return u; break;
                case Discord.FileComponent { File.Url: { } u }: yield return u; break;
            }
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
