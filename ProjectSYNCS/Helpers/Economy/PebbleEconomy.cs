namespace ProjectSYNCS.Helpers;

// The economy's numbers and arithmetic. Cailloux for users, "pebble" in code.
public static class PebbleEconomy
{
    public static readonly TimeSpan WorkCooldown = TimeSpan.FromHours(4);
    public const int WorkMin = 25;
    public const int WorkMax = 40;

    // "A normal day of /work" for the passive cap: three shifts, not the six that are
    // theoretically possible, because nobody works through the night.
    public const int ShiftsPerDay = 3;
    public const double PassiveShare = 0.30;

    // Passive income rides on XP grants, one caillou each; the cap is what makes it a
    // bonus rather than a second salary.
    public const long PassivePerGrant = 1;

    // 30% x 3 shifts x the average shift (32.5) = 29. Derived, so tuning /work moves it.
    public static long PassiveDailyCap => (long)Math.Round(PassiveShare * ShiftsPerDay * (WorkMin + WorkMax) / 2.0);

    public static long RollWorkPay() => Random.Shared.Next(WorkMin, WorkMax + 1);

    // Null when they have never worked, i.e. available now.
    public static DateTimeOffset? NextWorkAt(DateTimeOffset? lastWorkAt) => lastWorkAt + WorkCooldown;

    // The /work DM reminder (WorkReminderService): due once the cooldown has ended, and only if no
    // reminder went out since that shift started — so each shift gets exactly one DM.
    public static bool WorkReminderDue(DateTimeOffset? lastWorkAt, DateTimeOffset? remindedAt, DateTimeOffset now) =>
        NextWorkAt(lastWorkAt) is { } next && next <= now && (remindedAt is null || remindedAt < lastWorkAt);

    // What WorkRemindedAt becomes when the reminder is switched on. With /work already available
    // the stamp is now, so a cooldown that ended before opting in never DMs; mid-cooldown it is left
    // alone, so this shift's DM still goes out.
    public static DateTimeOffset? ReminderStampOnEnable(DateTimeOffset? lastWorkAt, DateTimeOffset? remindedAt, DateTimeOffset now) =>
        NextWorkAt(lastWorkAt) is { } next && next > now ? remindedAt : now;

    public static (long Granted, long NewTotal) Passive(long earnedToday, long amount)
    {
        var room = Math.Max(0, PassiveDailyCap - earnedToday);
        var granted = Math.Min(room, Math.Max(0, amount));
        return (granted, earnedToday + granted);
    }

    // /pay: the tax is burned and paid by the sender on top of the amount, so the recipient gets exactly
    // what was typed. The cap is per sender and per Paris day; it blunts funnelling from alt accounts.
    public const long PayMin = 10;
    public const long PayDailyCap = 2000;
    public const double PayTaxRate = 0.10;

    // 10 %, rounded up: every pay costs at least one caillou.
    public static long PayTax(long amount) => (long)Math.Ceiling(amount * PayTaxRate);

    // What they may still send today. PaidToday counts only while PayDay is today.
    public static long PayRoom(int payDay, long paidToday, int today) => Math.Max(0, PayDailyCap - (payDay == today ? paidToday : 0));

    // "0 caillou", "1 caillou", "47 cailloux": the French plural is irregular (-oux), and 0
    // and 1 are singular, so every amount goes through here instead of appending an "s".
    public static string Cailloux(long n) => $"{LevelCardUi.Xp(n)} {(Math.Abs(n) <= 1 ? "caillou" : "cailloux")}";
}
