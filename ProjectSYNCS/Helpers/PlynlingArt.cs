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
    public const int Version = 3;

    public const string BaseUrl =
        "https://raw.githubusercontent.com/RMelisen/DiscordBot.SessionOrganizer/main/assets/plynlings/";

    // Species whose bébé has its own art. Must match STAGED in tools/plynling-art/export.py —
    // artcheck compares the two. Every other species, and every other stage (ado and ancien
    // were tried and dropped), shows the adult picture.
    public static readonly IReadOnlySet<PlynlingSpecies> StagedSpecies =
        new HashSet<PlynlingSpecies> { PlynlingSpecies.Cepe };

    // The adult filename deliberately carries no stage segment: it is the file every species
    // already had, so adding the baby invalidated nothing Discord had cached.
    public static string Sprite(PlynlingSpecies species, PlynlingStage stage, PlynlingMood mood) =>
        $"{BaseUrl}plynling_{Key(species)}{StageSegment(species, stage)}_{mood.ToString().ToLowerInvariant()}_v{Version}.webp";

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
        PlynlingSpecies.Tournesol => "tournesol",
        PlynlingSpecies.Citron => "tournesol_citron",
        PlynlingSpecies.Roux => "tournesol_roux",
        PlynlingSpecies.Ivoire => "tournesol_ivoire",
        PlynlingSpecies.Nocturne => "tournesol_nocturne",
        PlynlingSpecies.Solaire => "tournesol_solaire",
        _ => throw new ArgumentOutOfRangeException(nameof(species), species, "No art key for this species."),
    };
}
