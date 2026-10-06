namespace ProjectSYNCS.Helpers;

public enum RollPurpose { Onset, Death, Recovery, Medicine }

// A roll in [0, 1). Settle uses PlynlingSickness.Roll; the checks pass scripted ones.
public delegate double SicknessRoll(int plynlingId, int dayKey, RollPurpose purpose);

/// <summary>
/// The sickness rules, pure. Every roll is a hash of (Plynling id, morning, purpose) — never a
/// Random — so the command that happens to settle a Plynling and the hourly sweep always reach the
/// same outcome for the same morning, and each morning is decided once and stored.
/// </summary>
public static class PlynlingSickness
{
    public const double OnsetFloor = 0.005;          // even a clean Plynling can catch something
    public const double OnsetPerPoint = 0.012;       // per point of hygiene below 33 %
    public const double IllnessDeathChance = 0.20;
    public const int GraceMornings = 2;               // its first two sick mornings carry no death roll
    public const int RecoveryDailyMin = 5, RecoveryDailyMax = 15;
    public const int RecoveryMedicineMin = 15, RecoveryMedicineMax = 25;
    public const int Healed = 100;
    public const double SickMealFactor = 0.5;

    public static double OnsetChance(double hygiene) =>
        OnsetFloor + OnsetPerPoint * Math.Max(0, (PlynlingLife.DirtyBelow - hygiene) * 100);

    // A whole number from min to max inclusive, from a roll in [0, 1).
    public static int Gain(double roll, int min, int max) => min + (int)(roll * (max - min + 1));

    // SplitMix64 over the three inputs (StableRoll): stable across runs and machines. The purpose
    // goes in as purpose + 1, exactly as before StableRoll existed — every stored morning depends on it.
    public static double Roll(int plynlingId, int dayKey, RollPurpose purpose) =>
        StableRoll.Unit(plynlingId, dayKey, (int)purpose + 1);
}
