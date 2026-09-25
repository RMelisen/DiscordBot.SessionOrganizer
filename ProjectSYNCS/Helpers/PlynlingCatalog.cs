using Discord.Interactions;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

public enum PlynlingRarity { Common, Uncommon, Rare, Legendary }

// Computed, never stored. Sleeping is the time of day (PlynlingLife.IsAsleep), not a need.
public enum PlynlingMood { Content, Happy, Sad, Hungry, Starving, Frozen, Sleeping }

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
public sealed record SpeciesInfo(PlynlingSpecies Species, PlynlingFamily Family, string Name, PlynlingRarity Rarity, int Weight, uint Accent);

// WithArticle exists because the foods differ in gender ("un champignon", "une morille"):
// lines say "Tu donnes {1} à Rex" rather than guessing an article.
public sealed record FoodInfo(PlynlingFood Food, string Name, string WithArticle, long Price, double Hunger, double Happiness);

// Everything that is data rather than behaviour: species, rarity odds, foods, memorials.
public static class PlynlingCatalog
{
    // Weights out of 300 *per family*: each common exactly 70/300, so the three commons are
    // 70% together; 18% peu commun, 9% rare, 3% légendaire. Every family uses this ladder; a
    // tier may be shared (the mushrooms' 27 rare points are the Mycène's 14 and the Coprin's 13).
    public static readonly IReadOnlyList<SpeciesInfo> Species = new[]
    {
        new SpeciesInfo(PlynlingSpecies.Amanite,   PlynlingFamily.Mushroom,  "Amanite",            PlynlingRarity.Common,    70, 0xCE323A),
        new SpeciesInfo(PlynlingSpecies.Cepe,      PlynlingFamily.Mushroom,  "Cèpe",               PlynlingRarity.Common,    70, 0x98623A),
        new SpeciesInfo(PlynlingSpecies.Rose,      PlynlingFamily.Mushroom,  "Rosé des prés",      PlynlingRarity.Common,    70, 0xEC929E),
        new SpeciesInfo(PlynlingSpecies.Russule,   PlynlingFamily.Mushroom,  "Russule verte",      PlynlingRarity.Uncommon,  54, 0x62AA58),
        new SpeciesInfo(PlynlingSpecies.Mystique,  PlynlingFamily.Mushroom,  "Mycène",             PlynlingRarity.Rare,      14, 0x7E52CC),
        new SpeciesInfo(PlynlingSpecies.Coprin,    PlynlingFamily.Mushroom,  "Coprin",             PlynlingRarity.Rare,      13, 0x5A5A70),
        new SpeciesInfo(PlynlingSpecies.Dore,      PlynlingFamily.Mushroom,  "Girolle",            PlynlingRarity.Legendary,  9, 0xE0AA2A),
        new SpeciesInfo(PlynlingSpecies.Tournesol, PlynlingFamily.Sunflower, "Tournesol",          PlynlingRarity.Common,    70, 0xECB018),
        new SpeciesInfo(PlynlingSpecies.Citron,    PlynlingFamily.Sunflower, "Tournesol citron",   PlynlingRarity.Common,    70, 0xE8DA64),
        new SpeciesInfo(PlynlingSpecies.Roux,      PlynlingFamily.Sunflower, "Tournesol roux",     PlynlingRarity.Common,    70, 0xC4522A),
        new SpeciesInfo(PlynlingSpecies.Ivoire,    PlynlingFamily.Sunflower, "Tournesol ivoire",   PlynlingRarity.Uncommon,  54, 0xE2D8BE),
        new SpeciesInfo(PlynlingSpecies.Nocturne,  PlynlingFamily.Sunflower, "Tournesol nocturne", PlynlingRarity.Rare,      27, 0x701E3A),
        new SpeciesInfo(PlynlingSpecies.Solaire,   PlynlingFamily.Sunflower, "Tournesol solaire",  PlynlingRarity.Legendary,  9, 0xF4B424),
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

    public static string RarityLabel(PlynlingRarity rarity) => rarity switch
    {
        PlynlingRarity.Common => "commun",
        PlynlingRarity.Uncommon => "peu commun",
        PlynlingRarity.Rare => "rare",
        _ => "légendaire",
    };

    public static IEnumerable<SpeciesInfo> InFamily(PlynlingFamily family) => Species.Where(s => s.Family == family);

    public static PlynlingFamily FamilyOf(PlynlingSpecies species) => Info(species).Family;

    public static int TotalWeight(PlynlingFamily family) => InFamily(family).Sum(s => s.Weight);

    // Pure given the roll, so the odds are checkable without randomness. Only the family's
    // own species take part: a sunflower adoption can never land on a mushroom.
    public static PlynlingSpecies PickSpecies(PlynlingFamily family, int roll)
    {
        foreach (var s in InFamily(family))
        {
            if (roll < s.Weight) return s.Species;
            roll -= s.Weight;
        }
        throw new ArgumentOutOfRangeException(nameof(roll));
    }

    public static PlynlingSpecies RollSpecies(PlynlingFamily family) =>
        PickSpecies(family, Random.Shared.Next(TotalWeight(family)));

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
