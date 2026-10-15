namespace ProjectSYNCS.Helpers;

// A rock-paper-scissors move. The int is the last segment of the pick buttons' custom-ids.
public enum DuelMove
{
    Rock = 0,
    Paper = 1,
    Scissors = 2,
}

/// <summary>/arcade duel's rules, pure.</summary>
public static class Duel
{
    public const int MinStake = 10;
    public const int MaxStake = 500;

    // Three ties in a row and it's a draw: nobody pays.
    public const int MaxRounds = 4;

    public static readonly TimeSpan AcceptWindow = TimeSpan.FromMinutes(10);
    // Once accepted, how long both have to pick before the duel is called off (nobody pays: a
    // missing pick reveals nothing, so waiting is no exploit).
    public static readonly TimeSpan PickWindow = TimeSpan.FromMinutes(5);

    // « La commission de l'arbitre »: the winner takes the loser's stake minus this share.
    public const double CommissionShare = 0.10;

    public static long WinnerGain(long stake) => stake - Commission(stake);

    public static long Commission(long stake) => (long)Math.Ceiling(stake * CommissionShare);

    /// <summary>1 if <paramref name="a"/> wins, −1 if <paramref name="b"/> wins, 0 on a tie.</summary>
    public static int Compare(DuelMove a, DuelMove b) =>
        a == b ? 0 : Beats(a) == b ? 1 : -1;

    public static DuelMove Beats(DuelMove move) => move switch
    {
        DuelMove.Rock => DuelMove.Scissors,
        DuelMove.Paper => DuelMove.Rock,
        _ => DuelMove.Paper,
    };

    // What she plays against you. She has seen your move. She won't say so.
    public static DuelMove CounterTo(DuelMove move) => move switch
    {
        DuelMove.Rock => DuelMove.Paper,
        DuelMove.Paper => DuelMove.Scissors,
        _ => DuelMove.Rock,
    };

    // The Moai is the rock, at the owner's request.
    public static string Emoji(DuelMove move) => move switch
    {
        DuelMove.Rock => "🗿",
        DuelMove.Paper => "📄",
        _ => "✂️",
    };

    public static string Name(DuelMove move) => move switch
    {
        DuelMove.Rock => "Pierre",
        DuelMove.Paper => "Feuille",
        _ => "Ciseaux",
    };
}

/// <summary>
/// /arcade heist's odds, pure. A bigger crew is likelier to succeed but splits the loot: each
/// member's expected return is <see cref="ExpectedShare"/> of the stake whatever the crew size, so
/// the heist is a slight sink and crew size changes the variance, never the value.
/// </summary>
public static class Heist
{
    public const int Stake = 25;
    public const int MaxCrew = 8;
    public const double ExpectedShare = 0.9;

    public static readonly TimeSpan LobbyTime = TimeSpan.FromMinutes(25);
    public static readonly TimeSpan Cooldown = TimeSpan.FromHours(6);

    public static double SuccessChance(int crew) => Math.Min(0.75, 0.20 + 0.10 * crew);

    // What each member receives on a success, stake included: 75 alone, 30 from six up.
    public static long Payout(int crew) => (long)Math.Round(ExpectedShare * Stake / SuccessChance(crew));
}
