using System.Globalization;

namespace ProjectSYNCS.Helpers;

/// <summary>What a goal reached unlocks, in rotation. Not stored: the order only decides the next one.</summary>
public enum JarReward { DoubleDaily, Feast, Giveaway }

/// <summary>
/// The jar's rules (/jar): rising goals, the rotation of rewards, and the bonus days. Pure, so they
/// are checkable without a database.
/// </summary>
/// <remarks>
/// The timed reward (double <c>/daily</c>) starts tomorrow, Paris time: a <c>/daily</c> already
/// claimed today couldn't be doubled. It keeps a range of days, so two unlocks make two days rather
/// than overwriting one.
/// </remarks>
public static class Jar
{
    public const long MinGive = 10;
    public const long MaxGive = 100_000;

    public const int GiveawayWinners = 3;
    public const long GiveawayPrize = 150;
    public static readonly TimeSpan GiveawayFor = TimeSpan.FromHours(24);
    public const string GiveawayPrizeText = "150 cailloux chacun";

    public const PlynlingFood FeastFood = PlynlingFood.Truffle;

    /// <summary>The goal after <paramref name="level"/> goals reached: 1500, 2000, … up to 5000 for good.</summary>
    public static long GoalFor(int level) => Math.Min(1500 + 500L * level, 5000);

    /// <summary>The reward for reaching goal number <paramref name="level"/> (0-based).</summary>
    public static JarReward RewardFor(int level) => (JarReward)(level % Enum.GetValues<JarReward>().Length);

    /// <summary>Pours <paramref name="amount"/> in: the overflow carries into the next goal, so one gift can reach several.</summary>
    public static (int Level, long Filled) Fill(int level, long filled, long amount)
    {
        filled += amount;
        while (filled >= GoalFor(level))
        {
            filled -= GoalFor(level);
            level++;
        }
        return (level, filled);
    }

    /// <summary>The Paris day after <paramref name="dayKey"/> (yyyymmdd).</summary>
    public static int NextDay(int dayKey)
    {
        var date = DateOnly.ParseExact(dayKey.ToString(CultureInfo.InvariantCulture), "yyyyMMdd", CultureInfo.InvariantCulture);
        return int.Parse(date.AddDays(1).ToString("yyyyMMdd", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// One more bonus day: tomorrow when nothing is pending, else the day after the pending range.
    /// </summary>
    public static (int From, int Until) Extend(int from, int until, int tomorrow) =>
        until < tomorrow ? (tomorrow, tomorrow) : (from, NextDay(until));

    public static bool Covers(int from, int until, int day) => from != 0 && from <= day && day <= until;
}
