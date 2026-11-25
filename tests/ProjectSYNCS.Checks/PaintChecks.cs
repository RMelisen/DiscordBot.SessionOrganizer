using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

// 70. Dressed pictures drawn from layers (PlynlingLook, PlynlingPainter): look names round-trip, the
// marks repaint changes nothing on its own, missing art is null, the cache hits, the visit canvas lines
// up with today's bare _visit files in assets/plynlings/, and FilesFor finds a card's pictures.
public static class PaintChecks
{
    private static Action<string> _fail = _ => { };

    private static void Expect(bool ok, string what)
    {
        if (!ok) _fail("paint: " + what);
    }

    private const int Tick = 125;
    private const string AccessoryPrefix = "cos.accessory.", AuraPrefix = "cos.aura.";

    private static readonly string[] Faces = Enum.GetValues<PlynlingMood>().Where(m => m != PlynlingMood.Angry)
        .Select(m => m.ToString().ToLowerInvariant()).ToArray();

    public static void Run(Action<string> fail)
    {
        _fail = fail;
        var root = StoryChecks.RepoRoot();
        PlynlingPainter.AssetsRoot = Path.Combine(root, "ProjectSYNCS", "Assets", "Plynlings");
        var old = Path.Combine(root, "assets", "plynlings");
        Expect(Auras().Length == 13, $"{Auras().Length} auras in the catalogue, expected 13");
        LookNamesRoundTrip();
        MarksRepaintIsInvisible();
        MarksBlendShows();
        MissingArtIsNull();
        CacheHits();
        VisitPictures(old);
        FilesForFindsPictures();
    }

    // The visit canvas (PlynlingLook.Visit): names round-trip, a bare visit look is pixel-exact against the
    // bare _visit file in assets/plynlings/ (so a dressed one sits where its partner does), and a hat fits whole.
    private static void VisitPictures(string old)
    {
        var dressed = new PlynlingLook("coprin_baby", "happy", false, new("cos.accessory.couronne", null), new("cos.accessory.canne_peche", "droite"),
            "bulles", new("cos.accessory.echarpe", "rouge_haut"), Visit: true);
        Expect(PlynlingLook.Parse(dressed.FileName) == dressed, $"visit look {dressed.FileName} doesn't round-trip");
        Expect(PlynlingLook.Parse((dressed with { Dirty = true }).FileName) == dressed with { Dirty = true }, "a dirty visit look doesn't round-trip");
        Expect(PlynlingLook.Parse(dressed.FileName.Replace("__visit", "")) == dressed with { Visit = false }, "a card look reads as a visit look");

        foreach (var (shape, face) in new[] { ("amanite", "content"), ("coprin_baby", "happy"), ("rose", "sad") })
        {
            var bare = PlynlingPainter.Draw(new PlynlingLook(shape, face, false, null, null, null, Visit: true));
            var oldPath = Path.Combine(old, $"plynling_{shape}_{face}_visit_v{PlynlingArt.VersionOf(shape.Replace("_baby", ""))}.webp");
            Expect(bare is not null && Differs(bare, oldPath) is null,
                $"bare {shape} {face} on the visit canvas differs from today's file: {(bare is null ? "not drawn" : Differs(bare, oldPath))}");
        }

        // A crown on an adult (the 320 px canvas): square, every pixel of the card picture kept.
        var hat = new PlynlingLook("amanite", "content", false, new("cos.accessory.couronne", null), null, "bulles");
        var (cw, ch, card) = Decode(PlynlingPainter.Draw(hat)!);
        var (vw, vh, visit) = Decode(PlynlingPainter.Draw(hat with { Visit = true })!);
        var size = 256 * 40 / 32;
        Expect(vw == size && vh == size, $"a hat on the visit canvas is {vw}x{vh}, not {size}x{size}");
        Expect(card.Count == visit.Count && card.Zip(visit).All(p => p.First.Count(c => c.A != 0) == p.Second.Count(c => c.A != 0)),
            $"a hat on the visit canvas loses pixels ({cw}x{ch} card)");
    }

    // Every (accessory key, colour/place style) pair, sides left out; no-style accessories as a single null.
    private static IEnumerable<(string Key, string? Style)> TodaysLooks()
    {
        foreach (var acc in CosmeticCatalog.All.Where(c => c.Slot == CosmeticSlot.Accessory))
        {
            IEnumerable<string?> styles = new string?[] { null };
            foreach (var (label, options) in AccessoryStyles.Choices(acc.Key))
            {
                if (label is "Côté" or "Main") continue;
                styles = styles.SelectMany(s => options.Select(o => (string?)(s is null ? o.Key : s + "_" + o.Key)));
            }
            foreach (var s in styles) yield return (acc.Key, s);
        }
    }


    // Every aura in the catalogue, by its short name.
    private static string[] Auras() => CosmeticCatalog.All.Where(c => c.Slot == CosmeticSlot.Aura)
        .Select(c => c.Key[AuraPrefix.Length..]).ToArray();

    private static readonly Regex Safe = new("^[A-Za-z0-9_.-]+$");

    private static void LookNamesRoundTrip()
    {
        var items = new List<WornAccessory?> { null };
        items.AddRange(TodaysLooks().Select(l => (WornAccessory?)new WornAccessory(l.Key, AccessoryStyles.Resolve(l.Key, l.Style))));
        // Both sides of a sided item, and every pair (a second only with a first).
        items.Add(new WornAccessory("cos.accessory.noeud", "rose_droite"));
        items.Add(new WornAccessory("cos.accessory.canne_peche", "droite"));
        var auras = Auras().Select(a => (string?)a).Prepend(null).ToArray();
        int checkedCount = 0, bad = 0;
        string? firstBad = null;
        foreach (var shape in new[] { "amanite_baby", "coprin" })
        foreach (var face in Faces)
        foreach (var dirty in new[] { false, true })
        foreach (var first in items)
        foreach (var second in first is null ? new List<WornAccessory?> { null } : items)
        foreach (var aura in auras)
        {
            var look = new PlynlingLook(shape, face, dirty, first, second, aura);
            var name = look.FileName;
            checkedCount++;
            if (PlynlingLook.Parse(name) != look || !Safe.IsMatch(name))
            {
                bad++;
                firstBad ??= name;
            }
        }
        // Every triple, on one face (a third only with a first and a second).
        foreach (var first in items.OfType<WornAccessory>())
        foreach (var second in items.OfType<WornAccessory>())
        foreach (var third in items.OfType<WornAccessory>())
        {
            var look = new PlynlingLook("coprin", "content", false, first, second, "bulles", third);
            checkedCount++;
            if (PlynlingLook.Parse(look.FileName) != look)
            {
                bad++;
                firstBad ??= look.FileName;
            }
        }
        Expect(checkedCount > 10_000, $"only {checkedCount} look names checked");
        Expect(bad == 0, $"{bad} look name(s) don't round-trip, e.g. {firstBad}");
        Expect(new PlynlingLook("amanite_baby", "content", true, new("cos.accessory.noeud", "rose_droite"),
                   new("cos.accessory.ballon", "rouge"), "bulles").FileName
               == "pl__amanite_baby__content__dirty__noeud-rose_droite__ballon-rouge__aura-bulles.webp",
            "the brief's example name");
        foreach (var junk in new[] { "", "pl__amanite.webp", "sprite.webp", "pl__amanite__content", "pl__../x__content.webp",
                     "pl__amanite__content__aura-bulles__noeud.webp", "pl__amanite__content__a__b__c__d.webp",
                     "pl__amanite__content____.webp", "pl__amanite__content__noeud-.webp", "PL__amanite__content.webp" })
            Expect(PlynlingLook.Parse(junk) is null, $"Parse(\"{junk}\") is not null");
    }

    // An animated WebP as one pixel array per 125 ms tick, fully transparent pixels zeroed.
    private static (int W, int H, List<Rgba32[]> Ticks) Ticks(Image<Rgba32> img)
    {
        var ticks = new List<Rgba32[]>();
        for (var f = 0; f < img.Frames.Count; f++)
        {
            var delay = (int)img.Frames[f].Metadata.GetWebpMetadata().FrameDelay;
            var px = new Rgba32[img.Width * img.Height];
            img.Frames[f].CopyPixelDataTo(px);
            for (var i = 0; i < px.Length; i++) if (px[i].A == 0) px[i] = default;
            for (var t = 0; t < Math.Max(1, delay / Tick); t++) ticks.Add(px);
        }
        return (img.Width, img.Height, ticks);
    }

    private static string? Differs(byte[] drawn, string oldPath)
    {
        using var a = Image.Load<Rgba32>(drawn);
        using var b = Image.Load<Rgba32>(oldPath);
        var (aw, ah, at) = Ticks(a);
        var (bw, bh, bt) = Ticks(b);
        if (aw != bw || ah != bh) return $"size {aw}x{ah} vs {bw}x{bh}";
        if (at.Count != bt.Count) return $"loop {at.Count} vs {bt.Count} ticks";
        var off = 0;
        for (var t = 0; t < at.Count; t++)
        {
            if (at[t].AsSpan().SequenceEqual(bt[t])) continue;
            for (var i = 0; i < at[t].Length; i++)
                if (at[t][i] != bt[t][i]) off++;
        }
        return off == 0 ? null : $"{off} px off";
    }

    // Base + marks repaint with no accessory = the base, for every shape and face with a marks file;
    // and a right-hand item on a marked face still draws.
    private static void MarksRepaintIsInvisible()
    {
        var marks = Path.Combine(PlynlingPainter.AssetsRoot, "marks");
        var files = Directory.Exists(marks) ? Directory.GetFiles(marks, "*.webp") : Array.Empty<string>();
        Expect(files.Length > 0, "no marks file found");
        var bad = new ConcurrentQueue<string>();
        Parallel.ForEach(files, path =>
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var basePath = Path.Combine(PlynlingPainter.AssetsRoot, "base", name + ".webp");
            var drawn = PlynlingPainter.Compose(basePath, Array.Empty<string>(), path, null);
            var why = Differs(drawn, basePath);
            if (why is not null) bad.Enqueue($"{name}: {why}");
        });
        Expect(bad.IsEmpty, $"{bad.Count} marks repaint(s) show on the bare base, e.g. {string.Join("; ", bad.Take(3))}");
        var right = new PlynlingLook("amanite", "sleeping", false, new("cos.accessory.canne_peche", "droite"), null, null);
        Expect(PlynlingPainter.Draw(right) is not null, "amanite sleeping with a right-hand canne_peche is not drawn");
    }

    private static (int W, int H, List<Rgba32[]> Ticks) Decode(byte[] bytes)
    {
        using var img = Image.Load<Rgba32>(bytes);
        return Ticks(img);
    }

    private static (int W, int H, List<Rgba32[]> Ticks) Decode(string path) => Decode(File.ReadAllBytes(path));

    // A layer's tick (a still: its one frame).
    private static Rgba32[] At(List<Rgba32[]> ticks, int t) => ticks[ticks.Count == 1 ? 0 : t];

    private static readonly string[] Sample = { "amanite", "coprin_baby" };

    // The repaint itself shows: base + a right-hand item with the marks pass differs from the same
    // without it somewhere, for each sample shape, and only where the marks layer and the item are
    // both drawn. Every marked face and every held item on its right side.
    private static void MarksBlendShows()
    {
        string Layer(string folder, string name) => Path.Combine(PlynlingPainter.AssetsRoot, folder, name + ".webp");
        // Each held look on its right side, as its layer is named: « ballon_rouge_droite », « canne_peche_droite ».
        var held = TodaysLooks().Select(l => (Name: l.Key[AccessoryPrefix.Length..], l.Style)).Where(l => AccessoryFit.Category(AccessoryPrefix + l.Name) == AccessoryCategory.Held)
            .Select(l => $"{l.Name}_{(l.Style is null ? "" : l.Style + "_")}droite").ToList();
        Expect(held.Count >= 8, $"only {held.Count} held items found");
        foreach (var shape in Sample)
        {
            var shown = 0;
            var bad = new ConcurrentQueue<string>();
            var faces = Faces.Where(f => File.Exists(Layer("marks", $"{shape}_{f}"))).ToList();
            Expect(faces.Contains("happy"), $"{shape}: no marks file for happy");
            Parallel.ForEach(faces.SelectMany(f => held.Select(h => (Face: f, Item: h))), job =>
            {
                var name = $"{shape}_{job.Face}";
                var (basePath, acc, marks) = (Layer("base", name), Layer("acc", $"{name}_{job.Item}"), Layer("marks", name));
                var (w, h, with) = Decode(PlynlingPainter.Compose(basePath, new[] { acc }, marks, null));
                var (_, _, without) = Decode(PlynlingPainter.Compose(basePath, new[] { acc }, null, null));
                var (aw, ah, accTicks) = Decode(acc);
                var (mw, mh, markTicks) = Decode(marks);
                if (aw != w || ah != h || mw != w || mh != h) { bad.Enqueue($"{name} {job.Item}: canvas sizes differ"); return; }
                var changed = 0;
                for (var t = 0; t < with.Count; t++)
                for (var i = 0; i < with[t].Length; i++)
                {
                    if (with[t][i] == without[t][i]) continue;
                    changed++;
                    if (At(accTicks, t)[i].A == 0 || At(markTicks, t)[i].A == 0)
                    {
                        bad.Enqueue($"{name} {job.Item}: tick {t} pixel {i % w},{i / w} changed outside item ∩ marks");
                        return;
                    }
                }
                if (changed > 0) Interlocked.Increment(ref shown);
            });
            Expect(bad.IsEmpty, $"{bad.Count} marks repaint(s) reach past the item and the marks, e.g. {string.Join("; ", bad.Take(3))}");
            Expect(shown > 0, $"{shape}: no right-hand item on any marked face shows the marks repaint");
        }
    }

    private static void MissingArtIsNull()
    {
        var logged = new List<string>();
        var log = PlynlingPainter.Log;
        PlynlingPainter.Log = logged.Add;
        try
        {
            var none = new PlynlingLook("tournesol", "content", false, new("cos.accessory.couronne", null), null, null);
            byte[]? a = null, b = null;
            try { a = PlynlingPainter.Draw(none); b = PlynlingPainter.Draw(none); }
            catch (Exception ex) { Expect(false, $"Draw threw on a shape without art: {ex.GetType().Name}"); }
            Expect(a is null && b is null, "a shape without art draws something");
            Expect(logged.Count == 1, $"a missing look logged {logged.Count} times, not once");
            var frozenDirty = new PlynlingLook("amanite", "frozen", true, null, null, null);
            Expect(PlynlingPainter.Draw(frozenDirty) is null, "a base that doesn't exist (frozen, dirty) draws something");
        }
        finally { PlynlingPainter.Log = log; }
    }

    private static void CacheHits()
    {
        var look = new PlynlingLook("coprin_baby", "happy", true, new("cos.accessory.couronne", null), null, "bulles");
        var a = PlynlingPainter.Draw(look);
        var b = PlynlingPainter.Draw(look with { First = new("cos.accessory.couronne", null) });
        Expect(a is not null && ReferenceEquals(a, b), "the same look drawn twice is not the same array");

        // Three items draw, and differ from the same look without the third.
        var three = new PlynlingLook("amanite", "content", false, new("cos.accessory.echarpe", "rouge_haut"),
            new("cos.accessory.couronne", null), "bulles", new("cos.accessory.ballon", "rouge_gauche"));
        var drawn = PlynlingPainter.Draw(three);
        var two = PlynlingPainter.Draw(three with { Third = null });
        Expect(drawn is { Length: > 0 } && two is not null && !drawn.AsSpan().SequenceEqual(two), "a look with three items doesn't draw its third");
    }

    private static void FilesForFindsPictures()
    {
        static MessageComponent Card(string media) => new ComponentBuilderV2()
            .AddComponent(new ContainerBuilder()
                .AddComponent(new SectionBuilder()
                    .WithAccessory(new ThumbnailBuilder().WithMedia(new UnfurledMediaItemProperties(media)))
                    .AddComponent(new TextDisplayBuilder("Pépin"))))
            .Build();

        var name = new PlynlingLook("amanite", "content", false, new("cos.accessory.noeud", "rose_gauche"),
            new("cos.accessory.canne_peche", "droite"), "petales").FileName;
        var files = PlynlingPainter.FilesFor(Card("attachment://" + name));
        try
        {
            Expect(files.Count == 1 && files[0].FileName == name && files[0].Stream.Length > 0,
                $"a card with attachment://{name} yields {files.Count} file(s): {string.Join(", ", files.Select(f => f.FileName))}");
        }
        finally { foreach (var f in files) f.Dispose(); }
        var linked = PlynlingPainter.FilesFor(Card(PlynlingArt.Sprite(PlynlingSpecies.Amanite, PlynlingStage.Adult, PlynlingMood.Content)));
        Expect(linked.Count == 0, $"a card with a link yields {linked.Count} file(s)");
    }
}
