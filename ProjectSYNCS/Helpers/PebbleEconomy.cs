namespace ProjectSYNCS.Helpers;

// The economy's numbers and arithmetic. Cailloux for users, "pebble" in code.
public static class PebbleEconomy
{
    public static readonly TimeSpan WorkCooldown = TimeSpan.FromHours(4);
    public const int WorkMin = 40;
    public const int WorkMax = 60;

    // "A normal day of /work" for the passive cap: three shifts, not the six that are
    // theoretically possible, because nobody works through the night.
    public const int ShiftsPerDay = 3;
    public const double PassiveShare = 0.30;

    // Passive income rides on XP grants, one caillou each; the cap is what makes it a
    // bonus rather than a second salary.
    public const long PassivePerGrant = 1;

    // 30% x 3 shifts x the average shift (50) = 45. Derived, so tuning /work moves it.
    public static long PassiveDailyCap => (long)Math.Round(PassiveShare * ShiftsPerDay * (WorkMin + WorkMax) / 2.0);

    public static long RollWorkPay() => Random.Shared.Next(WorkMin, WorkMax + 1);

    // Null when they have never worked, i.e. available now.
    public static DateTimeOffset? NextWorkAt(DateTimeOffset? lastWorkAt) => lastWorkAt + WorkCooldown;

    public static (long Granted, long NewTotal) Passive(long earnedToday, long amount)
    {
        var room = Math.Max(0, PassiveDailyCap - earnedToday);
        var granted = Math.Min(room, Math.Max(0, amount));
        return (granted, earnedToday + granted);
    }

    // "0 caillou", "1 caillou", "47 cailloux": the French plural is irregular (-oux), and 0
    // and 1 are singular, so every amount goes through here instead of appending an "s".
    public static string Cailloux(long n) => $"{LevelCardUi.Xp(n)} {(Math.Abs(n) <= 1 ? "caillou" : "cailloux")}";
}
