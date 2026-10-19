namespace ProjectSYNCS.Helpers;

/// <summary>Whether she should be in game after this look, and since when the server has been nearly empty.</summary>
public readonly record struct McPresenceVerdict(bool InGame, DateTimeOffset? FewSince);

/// <summary>
/// When SYNCS joins and leaves the Minecraft server — the owner's rule, pure so the checks can
/// walk it. She joins when <see cref="JoinAt"/> players are on, or the owner is. Once in, she
/// stays until <see cref="LeaveAtOrBelow"/> or fewer others have been on for longer than
/// <see cref="Linger"/>; while the owner is on that rule is suspended, or she would join for the owner,
/// leave three minutes later and come straight back. Three players is a dead band: she stays if
/// she's already in, but doesn't join for three.
/// </summary>
public static class McPresence
{
    public const int JoinAt = 4;
    public const int LeaveAtOrBelow = 2;
    public static readonly TimeSpan Linger = TimeSpan.FromMinutes(3);

    /// <param name="reachable">The server answered its status ping.</param>
    /// <param name="others">Who else is on: the ping's count and sample before she joins, her own player list once in.</param>
    /// <param name="othersCount">How many others; can exceed <paramref name="others"/> (the ping's sample is capped and anonymised).</param>
    /// <param name="owner">The owner's in-game name, or empty when not configured.</param>
    /// <param name="inGame">She is in game now.</param>
    /// <param name="fewSince">When the "≤2 others" stretch began, or null if it isn't running.</param>
    public static McPresenceVerdict Decide(bool reachable, IReadOnlyCollection<string> others, int othersCount, string? owner,
        bool inGame, DateTimeOffset? fewSince, DateTimeOffset now)
    {
        if (!reachable) return new(false, null);

        var ownerOn = !string.IsNullOrWhiteSpace(owner) && others.Contains(owner.Trim(), StringComparer.OrdinalIgnoreCase);
        if (!inGame) return new(ownerOn || othersCount >= JoinAt, null);

        if (ownerOn || othersCount > LeaveAtOrBelow) return new(true, null);
        var since = fewSince ?? now;
        return new(now - since <= Linger, since);
    }
}
