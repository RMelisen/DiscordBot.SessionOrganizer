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

    // The adult filename deliberately carries no stage segment: it is the file every species
    // already had, so adding the baby invalidated nothing Discord had cached. The dirty version
    // adds « _dirty » after the face, a worn accessory « _with_<name>[_<style>] » after that
    // (tools/plynling-art/accessories.py) — new filenames, so no version bump. An accessory key the
    // catalogue doesn't know (retired, mistyped) shows the plain picture; angry is a visit face and
    // has no accessory pictures; an unknown style shows the default one (AccessoryStyles.Resolve).
    // A worn aura adds « _aura_<name> » last (tools/plynling-art/auras.py), on the faces in AuraMoods
    // only; elsewhere, or for a key that isn't a known aura, the picture is the same as without one.
    public static string Sprite(PlynlingSpecies species, PlynlingStage stage, PlynlingMood mood, bool dirty = false,
        string? accessory = null, string? style = null, string? aura = null) =>
        $"{BaseUrl}plynling_{Key(species)}{StageSegment(species, stage)}_{mood.ToString().ToLowerInvariant()}" +
        $"{(dirty && DirtyMoods.Contains(mood) ? "_dirty" : "")}{AccessorySegment(mood, accessory, style)}{AuraSegment(mood, aura)}_v{Version}.webp";

    // The card faces drawn with an aura. Must match AURA_STATES in tools/plynling-art/auras.py (checks
    // section 64): the ice covers it, a sick Plynling's sparkle goes out, angry is a visit face.
    private static readonly IReadOnlySet<PlynlingMood> AuraMoods = new HashSet<PlynlingMood>
    {
        PlynlingMood.Happy, PlynlingMood.Content, PlynlingMood.Sad, PlynlingMood.Hungry,
        PlynlingMood.Starving, PlynlingMood.Sleeping,
    };

    private const string AuraPrefix = "cos.aura.";

    private static string AuraSegment(PlynlingMood mood, string? aura) =>
        AuraMoods.Contains(mood) && CosmeticCatalog.ByKey(aura) is { Slot: CosmeticSlot.Aura } worn
            && worn.Key.StartsWith(AuraPrefix, StringComparison.Ordinal)
            ? "_aura_" + worn.Key[AuraPrefix.Length..]
            : "";

    private const string AccessoryPrefix = "cos.accessory.";

    private static string AccessorySegment(PlynlingMood mood, string? accessory, string? style)
    {
        if (mood == PlynlingMood.Angry || CosmeticCatalog.ByKey(accessory) is not { Slot: CosmeticSlot.Accessory } worn
            || !worn.Key.StartsWith(AccessoryPrefix, StringComparison.Ordinal))
            return "";
        var resolved = AccessoryStyles.Resolve(worn.Key, style);
        return $"_with_{worn.Key[AccessoryPrefix.Length..]}{(resolved is null ? "" : "_" + resolved)}";
    }

    /// <summary>
    /// The one way to picture a living Plynling on its own: its species, its stage, its mood (or
    /// the one given), whether it is « sale », the accessory it wears in its style and its aura — or,
    /// for a try-on, the ones <paramref name="outfit"/> puts on. Never frozen and dirty — the ice covers it.
    /// </summary>
    public static string SpriteOf(Plynling p, DateTimeOffset now, PlynlingMood? mood = null, Outfit? outfit = null)
    {
        var worn = outfit ?? Outfit.Of(p);
        return Sprite(p.Species, PlynlingLife.Stage(p, now), mood ?? PlynlingLife.Mood(p, now),
            p.FrozenAt is null && PlynlingLife.IsDirty(p, now), worn.Accessory?.Key, worn.AccessoryStyle, worn.Aura?.Key);
    }

    /// <summary>
    /// The picture a visit story shows (happy, content, sad or angry): the same animation on a larger
    /// transparent canvas, so the two side-by-side Plynlings are a little smaller than the card's
    /// (see VISIT_CANVAS in tools/plynling-art/export.py). Any other mood shows the content face.
    /// </summary>
    public static string VisitSprite(PlynlingSpecies species, PlynlingStage stage, PlynlingMood mood) =>
        $"{BaseUrl}plynling_{Key(species)}{StageSegment(species, stage)}_" +
        $"{(mood is PlynlingMood.Happy or PlynlingMood.Sad or PlynlingMood.Angry ? mood : PlynlingMood.Content).ToString().ToLowerInvariant()}_visit_v{Version}.webp";

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
