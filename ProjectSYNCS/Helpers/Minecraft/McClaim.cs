namespace ProjectSYNCS.Helpers;

/// <summary>Whether an order may take her from whoever she's working for.</summary>
public enum McVerdict
{
    /// <summary>She takes the order: she was free, it's the same player, the claim ran out, or family outranks.</summary>
    Take,
    /// <summary>She's with someone else and stays with them: a short refusal.</summary>
    Busy,
}

/// <summary>
/// Who she works for. The player whose order she's carrying out holds her: only they can stop her or
/// give her another order, and everyone else hears she's busy. Papa outranks Tata, who outranks
/// everyone, so family can always take her and nobody can take her from them. A claim runs out after
/// <see cref="Expiry"/>, so a forgotten « suis-moi » never locks her for the evening, except a farm she was
/// asked to keep (<c>lasting</c>), which holds while its keeper is on the server. Pure, so the checks walk it.
/// </summary>
public static class McClaim
{
    public static readonly TimeSpan Expiry = TimeSpan.FromMinutes(10);

    public static int Rank(McKin kin) => kin switch
    {
        McKin.Papa => 2,
        McKin.Tata => 1,
        _ => 0,
    };

    /// <param name="holder">Who she's working for, null when she's free.</param>
    /// <param name="since">When the holder's order got under way.</param>
    /// <param name="lasting">The holder's order doesn't run out: a farm, while its keeper is on the server.</param>
    public static McVerdict Decide(string? holder, McKin holderKin, DateTimeOffset since, string player, McKin kin,
        DateTimeOffset now, bool lasting = false) =>
        holder is null
        || string.Equals(holder, player, StringComparison.OrdinalIgnoreCase)
        || (!lasting && now - since >= Expiry)
        || Rank(kin) > Rank(holderKin)
            ? McVerdict.Take
            : McVerdict.Busy;
}
