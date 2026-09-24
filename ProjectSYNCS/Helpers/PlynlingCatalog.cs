using Discord.Interactions;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

public enum PlynlingRarity { Common, Uncommon, Rare, Legendary }

public enum PlynlingMood { Content, Happy, Sad, Hungry, Starving, Frozen }

// The value is English (what /plynling feed sends and what the select option carries);
// ChoiceDisplay is what Discord shows, in French.
public enum PlynlingFood
{
    [ChoiceDisplay("Champignon")] Mushroom,
    [ChoiceDisplay("Shiitake")] Shiitake,
    [ChoiceDisplay("Morille")] Morel,
    [ChoiceDisplay("Truffe")] Truffle,
}

// Accent is the card's colour strip — the species' identifying colour.
public sealed record SpeciesInfo(PlynlingSpecies Species, string Name, PlynlingRarity Rarity, int Weight, uint Accent);

// WithArticle exists because the foods differ in gender ("un champignon", "une morille"):
// lines say "Tu donnes {1} à Rex" rather than guessing an article.
public sealed record FoodInfo(PlynlingFood Food, string Name, string WithArticle, long Price, double Hunger, double Happiness);

// Everything that is data rather than behaviour: species, rarity odds, foods, memorials.
public static class PlynlingCatalog
{
    // Weights out of 300: each common exactly 70/300, so the three commons are 70%
    // together; 18% peu commun, 9% rare, 3% légendaire.
    public static readonly IReadOnlyList<SpeciesInfo> Species = new[]
    {
        new SpeciesInfo(PlynlingSpecies.Amanite,  "Amanite",       PlynlingRarity.Common,    70, 0xCE323A),
        new SpeciesInfo(PlynlingSpecies.Cepe,     "Cèpe",          PlynlingRarity.Common,    70, 0x98623A),
        new SpeciesInfo(PlynlingSpecies.Rose,     "Rosé des prés", PlynlingRarity.Common,    70, 0xEC929E),
        new SpeciesInfo(PlynlingSpecies.Russule,  "Russule verte", PlynlingRarity.Uncommon,  54, 0x62AA58),
        new SpeciesInfo(PlynlingSpecies.Mystique, "Mystique",      PlynlingRarity.Rare,      27, 0x7E52CC),
        new SpeciesInfo(PlynlingSpecies.Dore,     "Doré",          PlynlingRarity.Legendary,  9, 0xE0AA2A),
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

    public static int TotalWeight => Species.Sum(s => s.Weight);

    // Pure given the roll, so the odds are checkable without randomness.
    public static PlynlingSpecies PickSpecies(int roll)
    {
        foreach (var s in Species)
        {
            if (roll < s.Weight) return s.Species;
            roll -= s.Weight;
        }
        throw new ArgumentOutOfRangeException(nameof(roll));
    }

    public static PlynlingSpecies RollSpecies() => PickSpecies(Random.Shared.Next(TotalWeight));

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
