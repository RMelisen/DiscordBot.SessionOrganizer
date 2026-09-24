using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Agreement for the short fixed words about one Plynling — "âgé/âgée", "gelé/gelée",
// "Mort/Morte". Anything long enough to need rewording goes in a GenderedLines pool
// instead; this is for single words, where two spellings side by side stay readable.
public static class PlynlingGrammar
{
    public static string Agree(this PlynlingGender gender, string masculine, string feminine) =>
        gender == PlynlingGender.Female ? feminine : masculine;

    public static string Symbol(this PlynlingGender gender) => gender.Agree("♂", "♀");
}
