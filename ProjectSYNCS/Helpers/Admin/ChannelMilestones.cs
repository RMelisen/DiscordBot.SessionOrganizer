using System.Globalization;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// The message counts a channel celebrates (<c>ChannelMilestoneService</c>): 1 000, 10 000,
/// 100 000, then every 100 000 (the owner's numbers). Pure, so the sequence can be checked
/// without a gateway.
/// </summary>
public static class ChannelMilestones
{
    private const long Step = 100_000;

    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    /// <summary>Whether reaching <paramref name="count"/> messages is a milestone.</summary>
    public static bool IsMilestone(long count) =>
        count is 1_000 or 10_000 || (count >= Step && count % Step == 0);

    /// <summary>The first milestone above <paramref name="count"/>.</summary>
    public static long NextAfter(long count) => count switch
    {
        < 1_000 => 1_000,
        < 10_000 => 10_000,
        _ => (count / Step + 1) * Step,
    };

    /// <summary>
    /// The next milestone still to reach: above <paramref name="count"/> and not in
    /// <paramref name="reached"/> (deletions can take a count back under one already reached).
    /// </summary>
    public static long NextUnreached(long count, IReadOnlySet<long> reached)
    {
        var next = NextAfter(count);
        while (reached.Contains(next)) next = NextAfter(next);
        return next;
    }

    /// <summary>The number written the French way, digits grouped by thousands (« 10 000 »).</summary>
    public static string Format(long n) => n.ToString("N0", Fr);
}
