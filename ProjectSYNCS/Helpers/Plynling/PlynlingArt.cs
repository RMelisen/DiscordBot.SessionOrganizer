using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Where every Plynling image lives. The one place these URLs are built, so if the repo
// ever goes private and the images have to be attached instead, this file is the change.
//
// The images are GitHub raw files on main: they resolve only once assets/plynlings/ has
// been pushed. Discord caches by URL, so new art is a new Version (and new filenames from
// tools/plynling-art/export.py), never an overwrite.
//
// A living Plynling is an animated WebP (its idle loop), which a Components V2 thumbnail
// plays; a client that cannot animate shows frame 0, the still sprite. Memorials and foods
// do not move and stay PNG.
public static class PlynlingArt
{
    public const int Version = 5;

    // A species redrawn since Version: its bare and visit pictures take this version instead, so a new
    // drawing gets new URLs without renaming every other file. Must match REDRAWN in
    // tools/plynling-art/export.py. Coprin 6: the idle ink drop turned black.
    private static readonly IReadOnlyDictionary<string, int> Redrawn = new Dictionary<string, int> { ["coprin"] = 6 };

    /// <summary>The version in the bare and visit file names of the species keyed <paramref name="key"/> (<see cref="Key"/>).</summary>
    public static int VersionOf(string key) => Redrawn.GetValueOrDefault(key, Version);

    public const string BaseUrl =
        "https://raw.githubusercontent.com/RMelisen/DiscordBot.SessionOrganizer/main/assets/plynlings/";

    // Species whose bébé has its own art. Must match STAGED in tools/plynling-art/export.py —
    // artcheck compares the two. Every other species, and every other stage (ado and ancien
    // were tried and dropped), shows the adult picture.
    public static readonly IReadOnlySet<PlynlingSpecies> StagedSpecies =
        new HashSet<PlynlingSpecies>
        {
            PlynlingSpecies.Amanite, PlynlingSpecies.Cepe, PlynlingSpecies.Rose,
            PlynlingSpecies.Russule, PlynlingSpecies.Mystique, PlynlingSpecies.Dore,
            PlynlingSpecies.Coprin,
        };

    // The card faces that have a « _dirty » picture. Must match DIRTY_STATES in
    // tools/plynling-art/export.py. Frozen hides the dirt under the ice, angry is a visit face,
    // and visits stay clean.
    private static readonly IReadOnlySet<PlynlingMood> DirtyMoods = new HashSet<PlynlingMood>
    {
        PlynlingMood.Happy, PlynlingMood.Content, PlynlingMood.Sad, PlynlingMood.Hungry,
        PlynlingMood.Starving, PlynlingMood.Sleeping, PlynlingMood.Sick,
    };

    // The bare picture, as a link. The adult filename deliberately carries no stage segment: it is the
    // file every species already had, so adding the baby invalidated nothing Discord had cached. The
    // dirty version adds « _dirty » after the face. A dressed Plynling is drawn by the bot instead
    // (LookOf, PlynlingPainter).
    public static string Sprite(PlynlingSpecies species, PlynlingStage stage, PlynlingMood mood, bool dirty = false) =>
        $"{BaseUrl}plynling_{Key(species)}{StageSegment(species, stage)}_{mood.ToString().ToLowerInvariant()}" +
        $"{(dirty && DirtyMoods.Contains(mood) ? "_dirty" : "")}_v{VersionOf(Key(species))}.webp";

    // The card faces drawn with an aura. Must match AURA_STATES in tools/plynling-art/auras.py (checks
    // section 64): the ice covers it, a sick Plynling's sparkle goes out, angry is a visit face.
    private static readonly IReadOnlySet<PlynlingMood> AuraMoods = new HashSet<PlynlingMood>
    {
        PlynlingMood.Happy, PlynlingMood.Content, PlynlingMood.Sad, PlynlingMood.Hungry,
        PlynlingMood.Starving, PlynlingMood.Sleeping,
    };

    private const string AuraPrefix = "cos.aura.";
    private const string AccessoryPrefix = "cos.accessory.";

    /// <summary>
    /// What a dressed Plynling looks like (<see cref="PlynlingPainter"/> draws it): its shape, the face
    /// for its mood (or the one given), its dirt, the accessories <paramref name="outfit"/> (a try-on) or
    /// it wears, each in its style resolved (<see cref="AccessoryStyles.Resolve"/>) and in draw order,
    /// and its aura on the faces in <see cref="AuraMoods"/>. Null when there is nothing to draw on the
    /// bare picture: no accessory and no aura showing, a key the catalogue doesn't know, or angry (a
    /// visit face, never dressed).
    /// </summary>
    public static PlynlingLook? LookOf(Plynling p, DateTimeOffset now, PlynlingMood? mood = null, Outfit? outfit = null)
    {
        var face = mood ?? PlynlingLife.Mood(p, now);
        if (face == PlynlingMood.Angry) return null;
        var worn = outfit ?? Outfit.Of(p);
        // Under to over (AccessoryFit.InDrawOrder): the file name names them in the order they are drawn.
        var items = AccessoryFit.InDrawOrder(worn.Items
            .Where(w => w.Item is { Slot: CosmeticSlot.Accessory } c && c.Key.StartsWith(AccessoryPrefix, StringComparison.Ordinal))
            .Select(w => new WornAccessory(w.Item.Key, AccessoryStyles.Resolve(w.Item.Key, w.Style))));
        var aura = AuraMoods.Contains(face) && worn.Aura is { Slot: CosmeticSlot.Aura } a && a.Key.StartsWith(AuraPrefix, StringComparison.Ordinal)
            ? a.Key[AuraPrefix.Length..]
            : null;
        if (items.Count == 0 && aura is null) return null;
        var dirty = p.FrozenAt is null && PlynlingLife.IsDirty(p, now) && DirtyMoods.Contains(face);
        return new PlynlingLook(Key(p.Species) + StageSegment(p.Species, PlynlingLife.Stage(p, now)), face.ToString().ToLowerInvariant(),
            dirty, items.ElementAtOrDefault(0), items.ElementAtOrDefault(1), aura, items.ElementAtOrDefault(2));
    }

    /// <summary>
    /// The one way to picture a living Plynling on its own: its species, its stage, its mood (or
    /// the one given), whether it is « sale », and what it wears — or, for a try-on, what
    /// <paramref name="outfit"/> puts on. Bare, it is the link (<see cref="Sprite"/>); dressed, it is
    /// <c>attachment://pl__….webp</c>, drawn here first so the name is only ever handed out for a
    /// picture that exists: the sender attaches it (<see cref="PlynlingPainter.FilesFor"/>, the cache
    /// makes that free). A look that can't be drawn (no art, a missing layer) falls back to the link.
    /// Never frozen and dirty — the ice covers it.
    /// </summary>
    public static string SpriteOf(Plynling p, DateTimeOffset now, PlynlingMood? mood = null, Outfit? outfit = null)
    {
        var face = mood ?? PlynlingLife.Mood(p, now);
        return LookOf(p, now, face, outfit) is { } look && PlynlingPainter.Draw(look) is not null
            ? "attachment://" + look.FileName
            : Sprite(p.Species, PlynlingLife.Stage(p, now), face, p.FrozenAt is null && PlynlingLife.IsDirty(p, now));
    }

    /// <summary>
    /// The picture a visit story shows (happy, content, sad or angry): the same animation on a larger
    /// transparent canvas, so the two side-by-side Plynlings are a little smaller than the card's
    /// (see VISIT_CANVAS in tools/plynling-art/export.py). Any other mood shows the content face.
    /// </summary>
    public static string VisitSprite(PlynlingSpecies species, PlynlingStage stage, PlynlingMood mood) =>
        $"{BaseUrl}plynling_{Key(species)}{StageSegment(species, stage)}_{VisitFace(mood).ToString().ToLowerInvariant()}_visit_v{VersionOf(Key(species))}.webp";

    private static PlynlingMood VisitFace(PlynlingMood mood) =>
        mood is PlynlingMood.Happy or PlynlingMood.Sad or PlynlingMood.Angry ? mood : PlynlingMood.Content;

    /// <summary>
    /// What <paramref name="p"/> wears, as a clean look at <paramref name="stage"/> (its stage now if
    /// not given), for <see cref="DressedSprite"/> to put a face on; null when it wears nothing drawn.
    /// Captured once (<see cref="EventCast"/>) so a card's pictures are built without the Plynling.
    /// </summary>
    public static PlynlingLook? DressOf(Plynling p, DateTimeOffset now, PlynlingStage? stage = null) =>
        LookOf(p, now, PlynlingMood.Content) is { } look
            ? look with { Shape = Key(p.Species) + StageSegment(p.Species, stage ?? PlynlingLife.Stage(p, now)), Dirty = false }
            : null;

    /// <summary>
    /// A clean picture with the face <paramref name="mood"/>: <paramref name="dress"/> (<see cref="DressOf"/>)
    /// drawn, card-sized or, with <paramref name="visit"/>, on the visit canvas; else, or when it can't be
    /// drawn, the bare <see cref="Sprite"/> / <see cref="VisitSprite"/> link. Angry is never dressed, and
    /// on a face without an aura (<see cref="AuraMoods"/>) the aura is left off.
    /// </summary>
    public static string DressedSprite(PlynlingSpecies species, PlynlingStage stage, PlynlingLook? dress, PlynlingMood mood, bool visit)
    {
        var face = visit ? VisitFace(mood) : mood;
        if (dress is not null && face != PlynlingMood.Angry)
        {
            var look = dress with { Face = face.ToString().ToLowerInvariant(), Visit = visit, Aura = AuraMoods.Contains(face) ? dress.Aura : null };
            if (look.Items.Count + (look.Aura is null ? 0 : 1) > 0 && PlynlingPainter.Draw(look) is not null)
                return "attachment://" + look.FileName;
        }
        return visit ? VisitSprite(species, stage, face) : Sprite(species, stage, face);
    }

    /// <summary>The visit-sized picture of <paramref name="p"/> as it is now, dressed.</summary>
    public static string VisitSpriteOf(Plynling p, DateTimeOffset now, PlynlingMood mood)
    {
        var stage = PlynlingLife.Stage(p, now);
        return DressedSprite(p.Species, stage, DressOf(p, now, stage), mood, visit: true);
    }

    private static string StageSegment(PlynlingSpecies species, PlynlingStage stage) =>
        stage == PlynlingStage.Baby && StagedSpecies.Contains(species) ? "_baby" : "";

    public static string Memorial(PlynlingSpecies species, int tier) =>
        $"{BaseUrl}memorial_{Key(species)}_{tier}_v{Version}.png";

    public static string Food(PlynlingFood food) =>
        $"{BaseUrl}food_{food.ToString().ToLowerInvariant()}_v{Version}.png";

    // Must match the SPECIES / SUNFLOWERS keys in tools/plynling-art/common.py. Exhaustive
    // on purpose: it used to end in `_ => "dore"`, so a species added without a key would
    // silently have worn a Doré's pictures. Now it throws, and artcheck catches it.
    public static string Key(PlynlingSpecies species) => species switch
    {
        PlynlingSpecies.Amanite => "amanite",
        PlynlingSpecies.Cepe => "cepe",
        PlynlingSpecies.Rose => "rose",
        PlynlingSpecies.Russule => "russule",
        PlynlingSpecies.Mystique => "mystique",
        PlynlingSpecies.Dore => "dore",
        PlynlingSpecies.Coprin => "coprin",
        PlynlingSpecies.Tournesol => "tournesol",
        PlynlingSpecies.Citron => "tournesol_citron",
        PlynlingSpecies.Roux => "tournesol_roux",
        PlynlingSpecies.Ivoire => "tournesol_ivoire",
        PlynlingSpecies.Nocturne => "tournesol_nocturne",
        PlynlingSpecies.Solaire => "tournesol_solaire",
        _ => throw new ArgumentOutOfRangeException(nameof(species), species, "No art key for this species."),
    };
}
