namespace ProjectSYNCS.Helpers;

// How /teams splits a voice channel. Values are the [Choice] values of the command and the
// last segment of the « Remélanger » custom-id: append-only in spirit (an old card's button
// carries one).
public enum TeamMode
{
    Random = 0,
    Balanced = 1,
}

/// <summary>
/// The split behind /teams: pure, so it can be checked without a gateway. Team sizes never differ
/// by more than one. The balanced mode is not a snake draft: a snake draft is deterministic, so
/// « Remélanger » would deal the same teams again. It keeps the most even of many random deals
/// instead — every reroll differs, and each is as fair as the levels allow.
/// </summary>
public static class TeamDraw
{
    public const int MinTeams = 2;
    public const int MaxTeams = 4;

    // Enough tries that a channel of ten finds a near-perfect split; cheap at that size.
    private const int BalancedTries = 400;

    public static List<List<T>> Split<T>(IReadOnlyList<T> people, int teams, TeamMode mode,
        Func<T, int> weight, Random rng)
    {
        if (mode == TeamMode.Random) return Deal(people, teams, rng);

        List<List<T>>? best = null;
        var bestSpread = int.MaxValue;
        for (var i = 0; i < BalancedTries; i++)
        {
            var deal = Deal(people, teams, rng);
            var sums = deal.Select(t => t.Sum(weight)).ToList();
            var spread = sums.Max() - sums.Min();
            if (spread >= bestSpread) continue;
            best = deal;
            bestSpread = spread;
            if (spread == 0) break;
        }
        return best!;
    }

    // A shuffle dealt round-robin: sizes differ by at most one. The first team dealt is
    // shuffled too, so the bigger teams aren't always the first ones.
    private static List<List<T>> Deal<T>(IReadOnlyList<T> people, int teams, Random rng)
    {
        var shuffled = people.ToArray();
        rng.Shuffle(shuffled);
        var result = Enumerable.Range(0, teams).Select(_ => new List<T>()).ToList();
        var offset = rng.Next(teams);
        for (var i = 0; i < shuffled.Length; i++)
            result[(i + offset) % teams].Add(shuffled[i]);
        return result;
    }
}
