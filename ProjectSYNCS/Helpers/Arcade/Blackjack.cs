namespace ProjectSYNCS.Helpers;

/// <summary>A card: rank 1 (ace) to 13 (king), suit 0–3.</summary>
public readonly record struct Card(int Rank, int Suit)
{
    private static readonly string[] Ranks = { "", "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "V", "D", "R" };
    private static readonly string[] Suits = { "♠", "♥", "♦", "♣" };

    public int Value => Rank == 1 ? 11 : Math.Min(10, Rank);
    public override string ToString() => Ranks[Rank] + Suits[Suit];
}

/// <summary>
/// /arcade blackjack's rules, pure. An endless shoe (each card drawn independently), the dealer
/// stands on every 17, a natural pays 3:2, doubling only on the first two cards. Fair: she
/// teases, she doesn't cheat here.
/// </summary>
public static class Blackjack
{
    public const int MinStake = 10;
    public const int MaxStake = 200;

    public static readonly TimeSpan IdleLimit = TimeSpan.FromMinutes(5);

    public static Card Draw(Random rng) => new(rng.Next(1, 14), rng.Next(4));

    /// <summary>The best total, counting aces as 1 when 11 would bust.</summary>
    public static int Total(IReadOnlyList<Card> hand)
    {
        var total = hand.Sum(c => c.Value);
        var aces = hand.Count(c => c.Rank == 1);
        while (total > 21 && aces-- > 0) total -= 10;
        return total;
    }

    public static bool IsNatural(IReadOnlyList<Card> hand) => hand.Count == 2 && Total(hand) == 21;

    public static void PlayDealer(List<Card> dealer, Random rng)
    {
        while (Total(dealer) < 17) dealer.Add(Draw(rng));
    }

    public enum Outcome { Win, Lose, Push, Natural }

    public static Outcome Judge(IReadOnlyList<Card> player, IReadOnlyList<Card> dealer)
    {
        var p = Total(player);
        var d = Total(dealer);
        if (p > 21) return Outcome.Lose;
        if (IsNatural(player)) return IsNatural(dealer) ? Outcome.Push : Outcome.Natural;
        if (IsNatural(dealer)) return Outcome.Lose;
        if (d > 21 || p > d) return Outcome.Win;
        return p == d ? Outcome.Push : Outcome.Lose;
    }

    public static long Net(Outcome outcome, long stake) => outcome switch
    {
        Outcome.Natural => stake * 3 / 2,
        Outcome.Win => stake,
        Outcome.Push => 0,
        _ => -stake,
    };

    public static string Show(IReadOnlyList<Card> hand) => string.Join(" ", hand.Select(c => $"`{c}`"));
}
