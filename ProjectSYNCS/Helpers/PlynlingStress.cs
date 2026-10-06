using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// The care that relieves stress.
public enum CareAct { Pet, Game, Visit, Bath, Meal }

/// <summary>
/// CK3's stress, pure. 0–400, three levels; each drains happiness faster and, from level 2, costs
/// stats. Only the owner's choices raise it (the event engine enforces it); care and the mornings
/// lower it. It never touches hunger or death.
/// </summary>
public static class PlynlingStress
{
    public const int Max = 400;
    public const int LevelStep = 100;
    public const int DailyDecay = 15;

    public static int Level(int stress) => Math.Clamp(stress / LevelStep, 0, 3);

    public static double HappinessFactor(int level) => level switch { 1 => 1.15, 2 => 1.35, 3 => 1.6, _ => 1.0 };

    public static int StatPenalty(int level) => level switch { 2 => -1, 3 => -2, _ => 0 };

    // CK3's gain multipliers (Méfiant ×2, Excentrique ×1.5, Capricieux ×0.5), multiplied together.
    public static double GainMultiplier(IEnumerable<TraitInfo> traits) => traits.Aggregate(1.0, (m, t) => m * t.StressGain);

    public static int Scaled(int amount, IEnumerable<TraitInfo> traits) => (int)Math.Round(amount * GainMultiplier(traits));

    // The traits' decay multiplier as a bonus percent, for Plynling.StressLossBonusPercent.
    public static int LossBonusPercent(IEnumerable<TraitInfo> traits) =>
        (int)Math.Round((traits.Aggregate(1.0, (m, t) => m * t.StressLoss) - 1) * 100);

    // How much a care act relieves, after its coping traits.
    public static int Relief(CareAct act, IReadOnlyList<TraitInfo> traits, bool closeBond = false)
    {
        bool Has(string key) => traits.Any(t => t.Key == key);
        double relief = act switch
        {
            CareAct.Pet => Has("contrite") ? 10 : 5,
            CareAct.Game => 15,
            CareAct.Visit => Has("improvident") ? 30 : 20,
            CareAct.Bath => Has("profligate") ? 10 : 3,
            CareAct.Meal => Has("comfort_eater") ? 10 : 0,
            _ => 0,
        };
        if (act == CareAct.Pet && Has("irritable")) relief *= 0.5;
        if (act == CareAct.Game && Has("athletic")) relief *= 2;
        if (act == CareAct.Game && Has("irritable")) relief *= 1.5;
        if (act == CareAct.Visit && Has("reclusive")) relief *= 0.5;
        if (act == CareAct.Visit && closeBond && Has("confider")) relief *= 2;
        return (int)Math.Round(relief);
    }

    // The morning's decay for this row: the base, its traits' cached multiplier, its modifiers.
    public static int DecayAt(Plynling p) =>
        (int)Math.Round(DailyDecay * (1 + p.StressLossBonusPercent / 100.0) * PlynlingModifiers.StressDecayFactor(p));
}
