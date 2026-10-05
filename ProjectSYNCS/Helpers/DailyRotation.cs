namespace ProjectSYNCS.Helpers;

// One line per calendar day from a pool, without ResponsePicker's in-memory history:
// the pool is walked through a shuffled order, one step a day. Nothing repeats until
// every line has been used, and since the order is computed from the date alone it
// survives restarts with nothing stored.
//
// Each pass over the pool gets its own shuffle, so the order doesn't loop identically,
// and the seam between two passes keeps any line at least a quarter of the pool's
// length of days away from its previous use. Adding or
// removing a line reshuffles everything from that day on, so a line said recently may
// come back once after a pool edit.
public static class DailyRotation
{
    /// <summary>
    /// The pool's line for <paramref name="dayNumber"/> (see <see cref="AppTime.DayNumber"/>).
    /// <paramref name="salt"/> gives each pool its own order: two pools of the same
    /// length would otherwise move in lockstep.
    /// </summary>
    public static T Pick<T>(IReadOnlyList<T> pool, int dayNumber, ulong salt) =>
        pool[IndexFor(pool.Count, dayNumber, salt)];

    public static int IndexFor(int count, int dayNumber, ulong salt)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (count <= 2) return dayNumber % count;

        var pass = dayNumber / count;
        var order = Shuffle(count, salt, pass);
        if (pass > 0)
            SpreadSeam(order, Shuffle(count, salt, pass - 1), Math.Max(1, count / 4));

        return order[dayNumber % count];
    }

    // Keeps the previous pass's last `seam` lines out of this pass's first `seam` days,
    // so a line is never said twice within `seam` days across the join. Only positions
    // before count - seam are touched: every pass's tail stays its raw shuffle, which is
    // what lets the previous pass be recomputed here without recursing. With seam at
    // most count / 3, the middle always has enough non-tail lines to swap in.
    private static void SpreadSeam(int[] order, int[] previous, int seam)
    {
        var tail = new HashSet<int>(previous[^seam..]);
        var j = seam;
        for (var i = 0; i < seam; i++)
        {
            if (!tail.Contains(order[i])) continue;
            while (tail.Contains(order[j])) j++;
            (order[i], order[j]) = (order[j], order[i]);
            j++;
        }
    }

    // Fisher–Yates driven by SplitMix64 rather than System.Random: a seeded Random's
    // sequence is not guaranteed to stay the same across .NET versions, and a runtime
    // upgrade must not reshuffle the rotation.
    private static int[] Shuffle(int count, ulong salt, int pass)
    {
        var order = new int[count];
        for (var i = 0; i < count; i++) order[i] = i;

        var state = salt ^ ((ulong)pass * 0x9E3779B97F4A7C15UL);
        for (var i = count - 1; i > 0; i--)
        {
            var j = (int)(Next(ref state) % (ulong)(i + 1));
            (order[i], order[j]) = (order[j], order[i]);
        }
        return order;
    }

    private static ulong Next(ref ulong state)
    {
        var z = state += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}
