namespace ProjectSYNCS.Helpers;

/// <summary>
/// The premium easter egg's numbers (PremiumPrankService): about one slash command in 500 is
/// answered with a fake paywall, edited into « je rigole » a few seconds later, and then run.
/// </summary>
public static class PremiumPrank
{
    public const int OneIn = 500;

    /// <summary>
    /// How long the paywall stands before the reveal, drawn in [Min, Max) milliseconds: long enough
    /// to read it and believe it.
    /// </summary>
    public static readonly (int Min, int Max) RevealDelayMs = (5000, 7000);

    public static bool Rolls(Random rng) => rng.Next(OneIn) == 0;
}
