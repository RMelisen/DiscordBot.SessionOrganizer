namespace ProjectSYNCS.Helpers;

/// <summary>/arcade race's rules, pure: who runs, how fast, who's paid.</summary>
public static class Race
{
    public const int MinRunners = 3;
    public const int MaxRunners = 6;
    public const int Steps = 5;
    public const int Track = 14;            // cells on a progress bar
    public const double Commission = 0.10;
    public const long OwnerBonus = 15;

    // How much a point of strength is worth against the dice: enough that the strong are clear
    // favourites, not so much that an outsider never wins (12 vs 4 wins about three times in four).
    public const double StrengthWeight = 0.15;

    public static readonly TimeSpan BettingTime = TimeSpan.FromMinutes(30);
    public static readonly long[] Stakes = { 10, 25, 50, 100 };

    /// <summary>Courage, plus up to 4 for a happy Plynling.</summary>
    public static int Strength(int courage, double happiness) => courage + (int)Math.Round(Math.Clamp(happiness, 0, 1) * 4);

    /// <summary>
    /// Every runner's position after each step, from 0 to 1 (the finish). Each step adds a d8 roll
    /// plus a share of the strength, so the strong are favoured and nobody is safe.
    /// </summary>
    public static double[][] Simulate(IReadOnlyList<int> strengths, Random rng)
    {
        var totals = new double[strengths.Count];
        var steps = new double[Steps][];
        for (var s = 0; s < Steps; s++)
        {
            for (var i = 0; i < strengths.Count; i++)
                totals[i] += rng.Next(1, 9) + strengths[i] * StrengthWeight + rng.NextDouble() * 0.01;
            steps[s] = (double[])totals.Clone();
        }
        // Normalised on the leader at the end, so the winner crosses the line on the last frame.
        var best = totals.Max();
        return steps.Select(row => row.Select(t => Math.Min(1, t / best)).ToArray()).ToArray();
    }

    public static int Winner(double[][] frames) =>
        Array.IndexOf(frames[^1], frames[^1].Max());

    /// <summary>
    /// Pari-mutuel: the pool, minus the commission, goes to whoever backed the winner, in
    /// proportion to their bet. Nobody on the winner, and the whole pool is lost.
    /// </summary>
    public static Dictionary<ulong, long> Nets(IReadOnlyDictionary<ulong, (int Runner, long Stake)> bets, int winner)
    {
        var pool = bets.Values.Sum(b => b.Stake);
        var onWinner = bets.Where(b => b.Value.Runner == winner).Sum(b => b.Value.Stake);
        var pot = (long)Math.Floor(pool * (1 - Commission));
        return bets.ToDictionary(b => b.Key, b => b.Value.Runner == winner && onWinner > 0
            ? pot * b.Value.Stake / onWinner - b.Value.Stake
            : -b.Value.Stake);
    }
}

/// <summary>/arcade crash's rules, pure.</summary>
public static class Crash
{
    public const double Growth = 0.08;       // per second: ×2 after ~9 s, ×10 after ~29 s
    public const double MaxMultiplier = 20;
    public const double HouseEdge = 0.9;      // P(crash ≥ m) = 0.9 / m

    public static readonly TimeSpan LobbyTime = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan Frame = TimeSpan.FromSeconds(2);
    public static readonly long[] Stakes = { 10, 25, 50, 100 };

    /// <summary>Where it crashes. One time in ten it never takes off (×1.00).</summary>
    public static double CrashPoint(Random rng)
    {
        var u = 1 - rng.NextDouble();   // (0, 1]
        var m = HouseEdge / u;
        return m < 1 ? 1 : Math.Min(MaxMultiplier, Math.Floor(m * 100) / 100);
    }

    public static double MultiplierAt(TimeSpan flown) =>
        Math.Floor(Math.Exp(Growth * Math.Max(0, flown.TotalSeconds)) * 100) / 100;

    public static long Payout(long stake, double multiplier) => (long)Math.Floor(stake * multiplier);
}

/// <summary>/arcade bomb's rules, pure.</summary>
public static class Bomb
{
    public const long Stake = 20;
    public const int MinPlayers = 3;
    public const int MaxPlayers = 8;
    public const double Commission = 0.10;

    public static readonly TimeSpan LobbyTime = TimeSpan.FromMinutes(3);

    public static TimeSpan Fuse(Random rng) => TimeSpan.FromSeconds(rng.Next(20, 61));

    /// <summary>The holder loses the stake; the survivors share it, minus the commission.</summary>
    public static Dictionary<ulong, long> Nets(IReadOnlyList<ulong> players, ulong loser)
    {
        var survivors = players.Where(p => p != loser).ToList();
        var share = survivors.Count == 0 ? 0 : (long)Math.Floor(Stake * (1 - Commission)) / survivors.Count;
        return players.ToDictionary(p => p, p => p == loser ? -Stake : share);
    }
}

/// <summary>« Qui a dit ça ? » (/arcade quote), pure.</summary>
public static class QuoteRules
{
    public const long Reward = 15;
    public const int MinLength = 20;
    public const int MaxLength = 300;
    public const int Choices = 4;
    public const int Attempts = 5;

    public static readonly TimeSpan Window = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan MinAge = TimeSpan.FromDays(7);

    /// <summary>A message worth asking about: long enough to recognise, short enough to read, no links.</summary>
    public static bool Usable(string content) =>
        content.Length is >= MinLength and <= MaxLength
        && !content.Contains("http://", StringComparison.OrdinalIgnoreCase)
        && !content.Contains("https://", StringComparison.OrdinalIgnoreCase)
        && !content.Contains("discord.gg", StringComparison.OrdinalIgnoreCase);
}

/// <summary>/arcade temperature's rules, pure.</summary>
public static class TemperatureRules
{
    public const long Stake = 10;
    public const int MinPlayers = 2;
    public const double Commission = 0.10;
    public const double MinGuess = 0;
    public const double MaxGuess = 110;

    public static readonly TimeSpan Window = TimeSpan.FromMinutes(10);

    /// <summary>« 52 », « 52,5 », « 52.5 °C » → 52.5; null when it isn't a number in range.</summary>
    public static double? ParseGuess(string text)
    {
        var cleaned = new string(text.Where(c => char.IsDigit(c) || c is ',' or '.' or '-').ToArray()).Replace(',', '.');
        return double.TryParse(cleaned, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v)
               && v is >= MinGuess and <= MaxGuess
            ? Math.Round(v, 1)
            : null;
    }

    /// <summary>Everyone pays the stake; the closest (ties share) split the pot minus the commission.</summary>
    public static Dictionary<ulong, long> Nets(IReadOnlyDictionary<ulong, double> guesses, double actual, out List<ulong> winners)
    {
        var best = guesses.Values.Min(g => Math.Abs(g - actual));
        winners = guesses.Where(g => Math.Abs(Math.Abs(g.Value - actual) - best) < 1e-9).Select(g => g.Key).ToList();
        var pot = (long)Math.Floor(Stake * guesses.Count * (1 - Commission));
        var share = pot / winners.Count;
        var won = winners.ToHashSet();
        return guesses.Keys.ToDictionary(p => p, p => won.Contains(p) ? share - Stake : -Stake);
    }
}

/// <summary>Whack-a-Quokka's rules, pure: when he shows up, and what hitting him pays.</summary>
public static class QuokkaRaid
{
    public const int MaxPerDay = 4;
    public const int FirstHour = 12;   // slots between 12:00 and 23:00 Paris, one per hour at most
    public const int LastHour = 22;
    private const int Salt = 7331;

    public static readonly TimeSpan Stay = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan ActiveWithin = TimeSpan.FromMinutes(30);

    /// <summary>
    /// The day's raid times, Paris wall clock: 0 to 4, at least an hour apart (one per distinct
    /// hour). Computed from the date, nothing stored: a restart finds the same slots.
    /// </summary>
    public static IReadOnlyList<DateTimeOffset> SlotsFor(int dayKey)
    {
        var count = StableRoll.Pick(Enumerable.Range(0, MaxPerDay + 1).ToList(), dayKey, Salt, 0);
        var hours = Enumerable.Range(FirstHour, LastHour - FirstHour + 1)
            .OrderBy(h => StableRoll.Unit(dayKey, Salt + 1, h))
            .Take(count)
            .OrderBy(h => h);
        var date = AppTime.FromDayKey(dayKey);
        return hours.Select(h => AppTime.AtWallClock(date.AddHours(h).AddMinutes((int)(StableRoll.Unit(dayKey, Salt + 2, h) * 60))))
            .ToList();
    }

    public static long Reward(Random rng) => rng.Next(10, 31);
}
