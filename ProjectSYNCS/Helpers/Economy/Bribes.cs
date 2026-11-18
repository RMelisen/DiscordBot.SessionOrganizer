namespace ProjectSYNCS.Helpers;

/// <summary>
/// /bribe's rules. One price for both bribes (the chouchou and erasing a shame hit): 100, doubled by
/// every bribe the same person already paid this Paris ISO week, back to 100 on Monday.
/// </summary>
public static class Bribes
{
    public const long BasePrice = 100;

    /// <summary>How long one bribe keeps someone her chouchou; buying again extends from the end.</summary>
    public static readonly TimeSpan FavouriteFor = TimeSpan.FromHours(24);

    // 100 << 40 is still far from long's limit; nobody pays 40 bribes in a week anyway.
    private const int MaxDoublings = 40;

    /// <summary>The next bribe's price, given the wallet's stamp (<c>BribeWeek</c>, <c>BribesThisWeek</c>).</summary>
    public static long Price(int bribeWeek, int bribesThisWeek, int thisWeek) =>
        BasePrice << Math.Clamp(bribeWeek == thisWeek ? bribesThisWeek : 0, 0, MaxDoublings);
}
