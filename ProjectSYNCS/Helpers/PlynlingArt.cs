using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Where every Plynling image lives. The one place these URLs are built, so if the repo
// ever goes private and the images have to be attached instead, this file is the change.
//
// The images are GitHub raw files on main: they resolve only once assets/plynlings/ has
// been pushed. Discord caches by URL, so new art is a new Version (and new filenames from
// tools/plynling-art/export.py), never an overwrite.
public static class PlynlingArt
{
    public const int Version = 1;

    public const string BaseUrl =
        "https://raw.githubusercontent.com/RMelisen/DiscordBot.SessionOrganizer/main/assets/plynlings/";

    public static string Sprite(PlynlingSpecies species, PlynlingMood mood) =>
        $"{BaseUrl}plynling_{Key(species)}_{mood.ToString().ToLowerInvariant()}_v{Version}.png";

    public static string Memorial(PlynlingSpecies species, int tier) =>
        $"{BaseUrl}memorial_{Key(species)}_{tier}_v{Version}.png";

    public static string Food(PlynlingFood food) =>
        $"{BaseUrl}food_{food.ToString().ToLowerInvariant()}_v{Version}.png";

    // Must match the SPECIES keys in tools/plynling-art/common.py.
    public static string Key(PlynlingSpecies species) => species switch
    {
        PlynlingSpecies.Amanite => "amanite",
        PlynlingSpecies.Cepe => "cepe",
        PlynlingSpecies.Rose => "rose",
        PlynlingSpecies.Russule => "russule",
        PlynlingSpecies.Mystique => "mystique",
        _ => "dore",
    };
}
