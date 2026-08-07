namespace ProjectSYNCS.Helpers;

// The XP curve behind /level: how much XP each level costs, and the reverse lookup
// from a cumulative total back to a level. Pure and dependency-free on purpose, like
// AppTime/EmoteMarkup/StatsPeriodUi, so the math is reasoned about on its own.
//
// Deliberately the only source of truth for "what level is this total" — MemberXp
// stores just the cumulative TotalXp, never a cached Level column. ThresholdForLevel
// is closed-form and LevelForXp is a binary search over it (both O(log level), not a
// loop over levels), which is cheaper than the EF round-trip a cached column would
// need to stay in sync — unlike EmoteDailyStat/BotFeedbackDailyStat, which exist
// because a *date* genuinely cannot be reconstructed from a running total, a level
// always can be, instantly, so a second column here would just be a copy that drifts.
public static class LevelCurve
{
    // Highest level the search below will ever consider. An XP total this large would
    // take millions of years of continuous activity to reach; the cap exists only to
    // keep the search bounded and overflow-free, not because anyone will get near it.
    private const int MaxLevel = 1_000_000;

    /// <summary>XP needed to go from <paramref name="level"/> to <paramref name="level"/> + 1.</summary>
    public static long XpForLevel(int level)
    {
        long n = Math.Max(0, level);
        return 5 * n * n + 50 * n + 100;
    }

    /// <summary>Cumulative XP required to have reached <paramref name="level"/> from 0.</summary>
    public static long ThresholdForLevel(int level)
    {
        long n = Math.Max(0, level);

        // Closed form for sum_{i=0}^{n-1} XpForLevel(i). Both (n-1)*n*(2n-1) and
        // (n-1)*n are always exactly divisible (by 6 and 2 respectively — consecutive
        // integers and the standard sum-of-squares identity), so the integer division
        // loses nothing, and both terms are correctly 0 at n = 0 and n = 1.
        long sumOfSquares = (n - 1) * n * (2 * n - 1) / 6;
        long sumOfLevels = (n - 1) * n / 2;
        return 5 * sumOfSquares + 50 * sumOfLevels + 100 * n;
    }

    /// <summary>The current level for a cumulative XP total.</summary>
    public static int LevelForXp(long totalXp)
    {
        if (totalXp <= 0) return 0;

        // ThresholdForLevel is monotonic, so this finds the highest level whose
        // threshold is still <= totalXp in O(log MaxLevel).
        int lo = 0, hi = MaxLevel;
        while (lo < hi)
        {
            int mid = lo + (hi - lo + 1) / 2;
            if (ThresholdForLevel(mid) <= totalXp) lo = mid;
            else hi = mid - 1;
        }
        return lo;
    }

    /// <summary>How far into the current level this total already is.</summary>
    public static long XpIntoLevel(long totalXp) => totalXp - ThresholdForLevel(LevelForXp(totalXp));

    /// <summary>How much more XP is needed to reach the next level.</summary>
    public static long XpForNextLevel(long totalXp) =>
        ThresholdForLevel(LevelForXp(totalXp) + 1) - totalXp;
}
