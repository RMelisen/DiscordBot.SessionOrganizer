namespace ProjectSYNCS.Helpers;

/// <summary>How a player's hit lands: the first ones, the one that tips her over, or the ones after.</summary>
public enum McHitStage
{
    /// <summary>One of the first <see cref="McHitStreaks.Threshold"/> - 1 hits: an ordinary remark.</summary>
    Hit,
    /// <summary>The hit that makes the streak: she understands they're trying to kill her. Said at once.</summary>
    Escalated,
    /// <summary>A hit after that, same streak: still the escalation lines, rationed like a remark.</summary>
    Again,
}

/// <summary>What she does about a player hitting Papa or Tata, by where their streak stands (<see cref="McHitStreaks.Tally"/>).</summary>
public enum McDefendStage
{
    /// <summary>The first hit: she tells them off.</summary>
    Warn,
    /// <summary>They did it again: she hits them back, once.</summary>
    Swat,
    /// <summary>They keep going: she fights them.</summary>
    Fight,
}

/// <summary>
/// Counts each player's hits on her. A streak is <see cref="Threshold"/> hits from the same player with
/// no more than <see cref="Window"/> between two of them; a longer pause starts over. Per player, so one
/// player's hits never count against another. Owns no lock: the service calls it under its own.
/// </summary>
public sealed class McHitStreaks
{
    public const int Threshold = 3;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private readonly Dictionary<string, (int Count, DateTimeOffset Last)> _streaks = new(StringComparer.OrdinalIgnoreCase);

    public McHitStage Register(string player, DateTimeOffset now)
    {
        var count = Tally(player, now);
        return count < Threshold ? McHitStage.Hit : count == Threshold ? McHitStage.Escalated : McHitStage.Again;
    }

    /// <summary>Counts the hit and returns where the streak stands: 1 for the first, 2 for the next within the window…</summary>
    public int Tally(string player, DateTimeOffset now)
    {
        var count = _streaks.TryGetValue(player, out var streak) && now - streak.Last <= Window ? streak.Count + 1 : 1;
        _streaks[player] = (count, now);
        // Old streaks go as new hits come in, so the map stays the size of one evening's brawlers.
        foreach (var gone in _streaks.Where(s => now - s.Value.Last > Window).Select(s => s.Key).ToList()) _streaks.Remove(gone);
        return count;
    }

    public static McDefendStage DefendStage(int count) =>
        count <= 1 ? McDefendStage.Warn : count == 2 ? McDefendStage.Swat : McDefendStage.Fight;

    public void Clear() => _streaks.Clear();
}
