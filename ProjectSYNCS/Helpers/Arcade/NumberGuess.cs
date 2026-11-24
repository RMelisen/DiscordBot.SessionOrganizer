namespace ProjectSYNCS.Helpers;

/// <summary>
/// /arcade guess: plus ou moins for a stake. She thinks of a number from 1 to 100; six tries.
/// Halving the range every time finds it only 63 times in 100 (seven tries always would), so a
/// win pays ×1.4: a perfect player gets back ~88 % on average, like the rest of the arcade.
/// </summary>
public static class NumberGuess
{
    public const int Min = 1;
    public const int Max = 100;
    public const int Tries = 6;
    public const int MaxStake = 500;
    public const double Payout = 1.4;
    public static readonly TimeSpan IdleLimit = TimeSpan.FromMinutes(5);

    // What she answers a guess: her number is higher, lower, or that.
    public enum Hint { Higher, Lower, Correct }

    public static Hint Judge(int guess, int secret) =>
        guess < secret ? Hint.Higher : guess > secret ? Hint.Lower : Hint.Correct;

    /// <summary>What the player's wallet moves by: the gain over the stake on a win (rounded down), minus the stake on a loss.</summary>
    public static long Net(bool won, long stake) => won ? (long)Math.Floor(stake * Payout) - stake : -stake;

    /// <summary>
    /// Whether a game feeds the arcade badge: a win that actually gains cailloux. A free game, or a
    /// stake of 1–2 (×1.4 rounds back to the stake), could be replayed endlessly to farm it.
    /// </summary>
    public static bool CountsForBadge(bool won, long stake) => won && Net(won, stake) > 0;

    /// <summary>A typed guess, or null when it isn't a whole number from 1 to 100.</summary>
    public static int? Parse(string text) =>
        int.TryParse(text.Trim(), out var n) && n is >= Min and <= Max ? n : null;
}
