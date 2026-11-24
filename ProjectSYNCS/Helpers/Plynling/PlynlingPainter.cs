using System.Diagnostics;
using Discord;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// Draws a dressed Plynling (accessories, aura) from the layers tools/plynling-art exports into
/// Assets/Plynlings (`python export.py layers`), as the 8× animated lossless WebP the pre-rendered
/// pictures are — pixel-identical to them for one accessory. A singleton by being static: one
/// bounded, lock-guarded cache of encoded pictures keyed by the look, reset on restart.
/// </summary>
/// <remarks>
/// Every layer is a loop on the shared 125 ms tick (motion.py FRAME_MS). The encoder merges identical
/// consecutive frames, so a layer may have fewer frames than the loop (a still is one frame): frames
/// are expanded to ticks by their delay, composed tick by tick, and merged back on encode. Each frame
/// is written whole (Source blending, RestoreToBackground disposal), so a pixel going transparent stays so.
/// The largest canvas in play wins (an adult in a hat: the 320 px hat canvas) and every layer sits at
/// its foot. Back to front: the base, the accessories (their pixels replace what's under them), the
/// mood marks repainted over an accessory (only with a right-hand item, which may cover them), then
/// the aura, whose particles land only where the picture is nearly empty (auras.py SHADOW_ALPHA).
/// </remarks>
public static class PlynlingPainter
{
    private const int Tick = 125;
    private const byte ShadowAlpha = 90;
    private const int CacheSize = 256;
    private const int SlowDrawMs = 500;
    private const string AccessoryPrefix = "cos.accessory.";

    /// <summary>The layers' root: base/, acc/, aura/, marks/. The checks point it at the repo copy.</summary>
    public static string AssetsRoot { get; set; } = Path.Combine(AppContext.BaseDirectory, "Assets", "Plynlings");

    /// <summary>Where a failed draw is reported, once per look (the checks capture it).</summary>
    public static Action<string> Log { get; set; } = m => Console.Error.WriteLine(m);

    private static readonly object Gate = new();
    private static readonly Dictionary<PlynlingLook, byte[]> Cache = new();
    private static readonly Queue<PlynlingLook> Order = new();
    // Failures aren't cached (the art may appear after a redeploy); this only keeps the log quiet.
    private static readonly BoundedSet<PlynlingLook> Reported = new(CacheSize);

    /// <summary>
    /// The look as an animated WebP, from the cache or drawn now; null when a layer is missing or
    /// anything throws (logged once per look). The array is shared: never write to it.
    /// </summary>
    public static byte[]? Draw(PlynlingLook look)
    {
        lock (Gate)
            if (Cache.TryGetValue(look, out var hit)) return hit;
        byte[] bytes;
        var timer = Stopwatch.StartNew();
        try
        {
            bytes = Paint(look);
        }
        catch (Exception ex)
        {
            bool first;
            lock (Gate) first = Reported.Add(look);
            if (first) Log($"[PlynlingPainter] could not draw {look.FileName}: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
        // The first draw after a restart JIT-compiles the codec (no ReadyToRun on the Pi): worth a line.
        if (timer.ElapsedMilliseconds > SlowDrawMs) Log($"[PlynlingPainter] drew {look.FileName} in {timer.ElapsedMilliseconds} ms");
        lock (Gate)
        {
            if (Cache.TryGetValue(look, out var raced)) return raced;
            Cache[look] = bytes;
            Order.Enqueue(look);
            if (Order.Count > CacheSize) Cache.Remove(Order.Dequeue());
        }
        return bytes;
    }

    /// <summary>
    /// One throwaway draw so the codec is compiled before the first real card (the Pi has no
    /// ReadyToRun). Never throws: <see cref="Draw"/> reports its own failures, and anything else is logged.
    /// </summary>
    public static void WarmUp()
    {
        try
        {
            Draw(new PlynlingLook("amanite", "content", false, new WornAccessory(AccessoryPrefix + "couronne", null), null, null));
        }
        catch (Exception ex)
        {
            Log($"[PlynlingPainter] warm-up failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// The pictures a card shows by <c>attachment://pl__…</c> (thumbnails, galleries, files), drawn or
    /// from the cache, one per name. Fresh streams each call: the caller disposes them. A name that
    /// can't be drawn is left out (logged by <see cref="Draw"/>).
    /// </summary>
    public static IReadOnlyList<FileAttachment> FilesFor(MessageComponent components)
    {
        var files = new List<FileAttachment>();
        foreach (var name in NamesIn(components))
        {
            if (PlynlingLook.Parse(name) is not { } look || Draw(look) is not { } bytes) continue;
            files.Add(new FileAttachment(new MemoryStream(bytes, writable: false), name));
        }
        return files;
    }

    /// <summary>The distinct <c>pl__…</c> picture names a card references by <c>attachment://</c>.</summary>
    public static IReadOnlyList<string> NamesIn(MessageComponent components)
    {
        const string scheme = "attachment://pl__";
        return Urls(components.Components).Distinct(StringComparer.Ordinal)
            .Where(u => u.StartsWith(scheme, StringComparison.Ordinal))
            .Select(u => u["attachment://".Length..]).ToList();
    }

    private static IEnumerable<string> Urls(IEnumerable<IMessageComponent> components)
    {
        foreach (var c in components)
        {
            switch (c)
            {
                case ContainerComponent x:
                    foreach (var u in Urls(x.Components)) yield return u;
                    break;
                case SectionComponent x:
                    foreach (var u in Urls(x.Components)) yield return u;
                    if (x.Accessory is not null)
                        foreach (var u in Urls(new[] { x.Accessory })) yield return u;
                    break;
                case ActionRowComponent x:
                    foreach (var u in Urls(x.Components)) yield return u;
                    break;
                case ThumbnailComponent { Media.Url: { } u }:
                    yield return u;
                    break;
                case MediaGalleryComponent x:
                    foreach (var item in x.Items)
                        if (item.Media.Url is { } u) yield return u;
                    break;
                case FileComponent { File.Url: { } u }:
                    yield return u;
                    break;
            }
        }
    }

    private static string Name(WornAccessory item) =>
        item.Key.StartsWith(AccessoryPrefix, StringComparison.Ordinal) ? item.Key[AccessoryPrefix.Length..] : item.Key;

    // Held in a hand: the only items whose right-hand side covers the mood marks.
    private static bool RightHand(WornAccessory item) =>
        AccessoryFit.Category(item.Key) == AccessoryCategory.Held && item.Style is { } s && (s == "droite" || s.EndsWith("_droite", StringComparison.Ordinal));

    private static byte[] Paint(PlynlingLook look)
    {
        // Under to over: neck, hands, eyes, hat, head, held (unknown keys last, ties by key).
        var items = AccessoryFit.InDrawOrder(look.Items);
        var names = new[] { look.Shape, look.Face, look.Aura }.Concat(items.Select(Name)).Concat(items.Select(i => i.Style));
        if (!PlynlingLook.IsToken(look.Shape) || !PlynlingLook.IsToken(look.Face)
            || names.Any(n => n is not null && !PlynlingLook.IsToken(n)))
            throw new ArgumentException("a part of the look isn't a plain name");

        string Layer(string folder, string name) => Path.Combine(AssetsRoot, folder, name + ".webp");
        var shapeFace = $"{look.Shape}_{look.Face}";
        var accessories = items.Select(i => Layer("acc", $"{shapeFace}_{Name(i)}{(i.Style is null ? "" : "_" + i.Style)}")).ToList();
        // No marks file is normal for a face without mood extras (content, sad).
        var marks = items.Any(RightHand) ? Layer("marks", shapeFace) : null;
        if (marks is not null && !File.Exists(marks)) marks = null;
        string? Aura(bool tall) => look.Aura is null ? null : Layer("aura", $"{shapeFace}_{look.Aura}{(tall ? "_tall" : "")}");
        var picture = Compose(Layer("base", shapeFace + (look.Dirty ? "_dirty" : "")), accessories, marks, Aura);
        if (look.Visit) picture = OnVisitCanvas(picture);
        return Encode(picture.W, picture.H, picture.Frames, picture.Delays);
    }

    // The visit canvas, in sprite cells: the 32-cell sprite centred on 40 (VISIT_CANVAS in
    // tools/plynling-art/export.py), so a dressed visit picture sits where the bare one does.
    private const int SpriteCells = 32, VisitCells = 40;

    /// <summary>
    /// The picture moved onto the square visit canvas: centred across, its foot where the bare
    /// sprite's is. A hat that would poke out of the top pushes the whole picture down just enough.
    /// </summary>
    private static Picture OnVisitCanvas(Picture p)
    {
        var size = p.BareW * VisitCells / SpriteCells;
        if (p.W > size) throw new InvalidOperationException($"a {p.W} px picture is wider than the {size} px visit canvas");
        var top = p.H;
        foreach (var f in p.Frames)
            for (var i = 0; i < f.Length && i / p.W < top; i++)
                if (f[i].A != 0) top = i / p.W;
        var foot = Math.Max(size - (size - p.BareH) / 2, p.H - top);
        if (foot > size) throw new InvalidOperationException($"a {p.H - top} px tall picture doesn't fit the {size} px visit canvas");
        int ox = (size - p.W) / 2, oy = foot - p.H;
        var frames = p.Frames.Select(f =>
        {
            var o = new Rgba32[size * size];
            for (var y = top; y < p.H; y++)
                f.AsSpan(y * p.W, p.W).CopyTo(o.AsSpan((y + oy) * size + ox, p.W));
            return o;
        }).ToList();
        return p with { W = size, H = size, Frames = frames };
    }

    /// <summary>
    /// The base with each accessory layer over it in order, the marks repainted where an accessory
    /// covered them, then the aura. Throws on a missing file. The checks use it to draw the marks alone.
    /// </summary>
    public static byte[] Compose(string basePath, IReadOnlyList<string> accessoryPaths, string? marksPath, string? auraPath)
    {
        var p = Compose(basePath, accessoryPaths, marksPath, _ => auraPath);
        return Encode(p.W, p.H, p.Frames, p.Delays);
    }

    // The composed frames before encoding, with the bare sprite's size (the visit canvas is measured from it).
    private sealed record Picture(int W, int H, int BareW, int BareH, List<Rgba32[]> Frames, List<int> Delays);

    // `aura(tall)`: the aura layer for the canvas in play (the 320 px hat canvas has its own).
    private static Picture Compose(string basePath, IReadOnlyList<string> accessoryPaths, string? marksPath, Func<bool, string?> aura)
    {
        var bare = Load(basePath);
        var accessories = accessoryPaths.Select(Load).ToList();
        int w = accessories.Select(l => l.W).Append(bare.W).Max(), h = accessories.Select(l => l.H).Append(bare.H).Max();
        var marks = marksPath is null ? null : Load(marksPath);
        var auraPath = aura(h > bare.H);
        var particles = auraPath is null ? null : Load(auraPath);

        var all = accessories.Append(bare).Append(marks).Append(particles).OfType<Frames>().ToList();
        var length = all.Max(l => l.Ticks.Count);
        if (all.Any(l => l.Ticks.Count != length && l.Ticks.Count != 1))
            throw new InvalidOperationException($"loop lengths differ: {string.Join(", ", all.Select(l => l.Ticks.Count))} ticks");
        if (all.Any(l => l.W > w || l.H > h))
            throw new InvalidOperationException("a layer is larger than the canvas");

        // Identical consecutive ticks become one frame (as Pillow does): a tick that repeats the last
        // frame only lengthens it, and its canvas is reused for the next tick, as is `covered`.
        var frames = new List<Rgba32[]>();
        var delays = new List<int>();
        var covered = marks is null ? null : new bool[w * h];
        Rgba32[]? o = null;
        for (var t = 0; t < length; t++)
        {
            if (o is null) o = new Rgba32[w * h];
            else Array.Clear(o);                       // the base may not fill a hat canvas
            if (covered is not null) Array.Clear(covered);
            Place(bare, t, w, o, covered, Pass.Base);
            foreach (var layer in accessories) Place(layer, t, w, o, covered, Pass.Accessory);
            if (marks is not null) Place(marks, t, w, o, covered, Pass.Marks);
            if (particles is not null) Place(particles, t, w, o, covered, Pass.Aura);
            for (var i = 0; i < o.Length; i++)
                if (o[i].A == 0) o[i] = default;
            if (frames.Count > 0 && o.AsSpan().SequenceEqual(frames[^1]))
            {
                delays[^1] += Tick;
                continue;
            }
            frames.Add(o);
            delays.Add(Tick);
            o = null;
        }
        return new Picture(w, h, bare.W, bare.H, frames, delays);
    }

    private enum Pass { Base, Accessory, Marks, Aura }

    // Lays the layer's tick `t` (a still: its one frame) at its foot on the canvas `o` (`w` wide).
    private static void Place(Frames layer, int t, int w, Rgba32[] o, bool[]? covered, Pass pass)
    {
        var px = layer.Ticks[layer.Ticks.Count == 1 ? 0 : t];
        int ox = (w - layer.W) / 2, oy = o.Length / w - layer.H;
        for (var y = 0; y < layer.H; y++)
        {
            var row = (y + oy) * w + ox;
            if (pass == Pass.Base)
            {
                px.AsSpan(y * layer.W, layer.W).CopyTo(o.AsSpan(row, layer.W));
                continue;
            }
            for (var x = 0; x < layer.W; x++)
            {
                var s = px[y * layer.W + x];
                if (s.A == 0) continue;
                var i = row + x;
                switch (pass)
                {
                    case Pass.Accessory:
                        o[i] = s;
                        if (covered is not null) covered[i] = true;
                        break;
                    // Elsewhere the base already carries the mark: repaint only what an accessory covered.
                    case Pass.Marks:
                        if (covered![i]) o[i] = Over(s, o[i]);
                        break;
                    // Each pixel is visited once, so o[i].A is still the picture's own alpha under the aura.
                    case Pass.Aura:
                        if (o[i].A < ShadowAlpha) o[i] = s;
                        break;
                }
            }
        }
    }

    // Source over destination, straight alpha.
    private static Rgba32 Over(Rgba32 s, Rgba32 d)
    {
        if (s.A == 255) return s;
        float sa = s.A / 255f, da = d.A / 255f, oa = sa + da * (1 - sa);
        if (oa <= 0) return default;
        byte C(byte sc, byte dc) => (byte)Math.Round((sc * sa + dc * da * (1 - sa)) / oa);
        return new Rgba32(C(s.R, d.R), C(s.G, d.G), C(s.B, d.B), (byte)Math.Round(oa * 255));
    }

    // A loop as one pixel array per tick (a still keeps its single frame). Ticks of one frame share an array.
    private sealed record Frames(int W, int H, List<Rgba32[]> Ticks);

    private static Frames Load(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException($"no layer {Path.GetRelativePath(AssetsRoot, path)}", path);
        using var img = Image.Load<Rgba32>(path);
        var ticks = new List<Rgba32[]>();
        for (var f = 0; f < img.Frames.Count; f++)
        {
            var delay = (int)img.Frames[f].Metadata.GetWebpMetadata().FrameDelay;
            if (delay % Tick != 0) throw new InvalidDataException($"{Path.GetFileName(path)}: frame delay {delay} ms is not a multiple of {Tick}");
            var px = new Rgba32[img.Width * img.Height];
            img.Frames[f].CopyPixelDataTo(px);
            if (img.Frames.Count == 1) { ticks.Add(px); break; }
            for (var t = 0; t < Math.Max(1, delay / Tick); t++) ticks.Add(px);
        }
        return new Frames(img.Width, img.Height, ticks);
    }

    // The frames with their delays, lossless, looping forever.
    private static byte[] Encode(int w, int h, List<Rgba32[]> frames, List<int> delays)
    {
        using var image = Image.LoadPixelData<Rgba32>(frames[0], w, h);
        for (var f = 1; f < frames.Count; f++) image.Frames.AddFrame(frames[f]);
        for (var f = 0; f < frames.Count; f++)
        {
            var meta = image.Frames[f].Metadata.GetWebpMetadata();
            meta.FrameDelay = (uint)delays[f];
            meta.BlendMethod = WebpBlendMethod.Source;
            meta.DisposalMethod = WebpDisposalMethod.RestoreToBackground;
        }
        image.Metadata.GetWebpMetadata().RepeatCount = 0;
        using var ms = new MemoryStream();
        image.Save(ms, new WebpEncoder { FileFormat = WebpFileFormatType.Lossless, Method = WebpEncodingMethod.Level0, Quality = 100 });
        return ms.ToArray();
    }
}
