using Discord.Interactions;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;


// Computed, never stored. Sleeping is the time of day (PlynlingLife.IsAsleep), not a need.
// Angry is a visit face only: PlynlingLife.Mood never returns it, so a Plynling's own card
// never shows it. The lowercase name is the art filename segment.
public enum PlynlingMood { Content, Happy, Sad, Hungry, Starving, Frozen, Sleeping, Angry, Sick }

// Derived from age by PlynlingLife.Stage and never stored, so — unlike PlynlingSpecies —
// this is free to be reordered. The lowercase name is the art filename segment.
public enum PlynlingStage { Baby, Teen, Adult, Elder }

// The value is English (what /inventory shop sends and what the card's select option carries);
// ChoiceDisplay is what Discord shows, in French.
public enum PlynlingFood
{
    [ChoiceDisplay("Champignon")] Mushroom,
    [ChoiceDisplay("Shiitake")] Shiitake,
    [ChoiceDisplay("Morille")] Morel,
    [ChoiceDisplay("Truffe")] Truffle,
}

// What /plynling adopt offers. Not stored: a Plynling's family is its species' family, so
// there is nothing to keep in sync. A new family is a value here, its species appended to
// PlynlingSpecies, their rows below, and their art — no new text, since every line is
// family-neutral.
public enum PlynlingFamily
{
    [ChoiceDisplay("Champignon")] Mushroom,
    [ChoiceDisplay("Tournesol")] Sunflower,
}

// Accent is the card's colour strip — the species' identifying colour.
public sealed record SpeciesInfo(PlynlingSpecies Species, PlynlingFamily Family, string Name, uint Accent);

// WithArticle exists because the foods differ in gender ("un champignon", "une morille"):
// lines say "Tu donnes {1} à Rex" rather than guessing an article.
public sealed record FoodInfo(PlynlingFood Food, string Name, string WithArticle, long Price, double Hunger, double Happiness);

// Everything that is data rather than behaviour: species, foods, memorials.
public static class PlynlingCatalog
{
    // No rarity: every species of a family is equally likely at adoption.
    public static readonly IReadOnlyList<SpeciesInfo> Species = new[]
    {
        new SpeciesInfo(PlynlingSpecies.Amanite,   PlynlingFamily.Mushroom,  "Amanite",            0xCE323A),
        new SpeciesInfo(PlynlingSpecies.Cepe,      PlynlingFamily.Mushroom,  "Cèpe",               0x98623A),
        new SpeciesInfo(PlynlingSpecies.Rose,      PlynlingFamily.Mushroom,  "Rosé des prés",      0xEC929E),
        new SpeciesInfo(PlynlingSpecies.Russule,   PlynlingFamily.Mushroom,  "Russule verte",      0x62AA58),
        new SpeciesInfo(PlynlingSpecies.Mystique,  PlynlingFamily.Mushroom,  "Mycène",             0x7E52CC),
        new SpeciesInfo(PlynlingSpecies.Coprin,    PlynlingFamily.Mushroom,  "Coprin",             0x5A5A70),
        new SpeciesInfo(PlynlingSpecies.Dore,      PlynlingFamily.Mushroom,  "Girolle",            0xE0AA2A),
        new SpeciesInfo(PlynlingSpecies.Tournesol, PlynlingFamily.Sunflower, "Tournesol",          0xECB018),
        new SpeciesInfo(PlynlingSpecies.Citron,    PlynlingFamily.Sunflower, "Tournesol citron",   0xE8DA64),
        new SpeciesInfo(PlynlingSpecies.Roux,      PlynlingFamily.Sunflower, "Tournesol roux",     0xC4522A),
        new SpeciesInfo(PlynlingSpecies.Ivoire,    PlynlingFamily.Sunflower, "Tournesol ivoire",   0xE2D8BE),
        new SpeciesInfo(PlynlingSpecies.Nocturne,  PlynlingFamily.Sunflower, "Tournesol nocturne", 0x701E3A),
        new SpeciesInfo(PlynlingSpecies.Solaire,   PlynlingFamily.Sunflower, "Tournesol solaire",  0xF4B424),
    };

    // In price order, which is also the order the select menu shows them in.
    public static readonly IReadOnlyList<FoodInfo> Foods = new[]
    {
        new FoodInfo(PlynlingFood.Mushroom, "Champignon", "un champignon", 15, 0.25, 0.00),
        new FoodInfo(PlynlingFood.Shiitake, "Shiitake",   "un shiitake",   30, 0.60, 0.00),
        new FoodInfo(PlynlingFood.Morel,    "Morille",    "une morille",   40, 0.00, 0.40),
        new FoodInfo(PlynlingFood.Truffle,  "Truffe",     "une truffe",    80, 1.00, 0.30),
    };

    public static SpeciesInfo Info(PlynlingSpecies species) => Species.First(s => s.Species == species);
    public static FoodInfo Info(PlynlingFood food) => Foods.First(f => f.Food == food);

    public static IEnumerable<SpeciesInfo> InFamily(PlynlingFamily family) => Species.Where(s => s.Family == family);

    public static PlynlingFamily FamilyOf(PlynlingSpecies species) => Info(species).Family;

    // Pure given the roll, so the odds are checkable without randomness. Only the family's
    // own species take part: a sunflower adoption can never land on a mushroom.
    public static PlynlingSpecies PickSpecies(PlynlingFamily family, int roll) => InFamily(family).ElementAt(roll).Species;

    public static PlynlingSpecies RollSpecies(PlynlingFamily family) =>
        PickSpecies(family, Random.Shared.Next(InFamily(family).Count()));

    public static PlynlingGender RollGender() =>
        Random.Shared.Next(2) == 0 ? PlynlingGender.Male : PlynlingGender.Female;

    // By time actually lived. 4 weeks = 28 days; a month is taken as 30 days.
    public static int MemorialTier(TimeSpan lived) => lived.TotalDays switch
    {
        < 7 => 1,
        < 28 => 2,
        < 90 => 3,
        < 180 => 4,
        _ => 5,
    };

    public static string MemorialName(int tier) => tier switch
    {
        1 => "un cairn",
        2 => "une petite stèle",
        3 => "une stèle gravée",
        4 => "une urne",
        _ => "une statue",
    };
}
