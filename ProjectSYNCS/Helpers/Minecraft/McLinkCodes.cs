using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// The codes that prove a Discord member is a player (/minecraft link): issued on Discord for one in-game name, whispered
/// to her in game by that player within <see cref="Lifetime"/>. One live code per member, used once. In memory only
/// (a restart forgets them, by design); the clock is passed in so the rules can be checked. Self-locking.
/// </summary>
public sealed partial class McLinkCodes
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private readonly object _lock = new();
    private readonly Dictionary<ulong, (string Player, string Code, DateTimeOffset Until)> _byUser = new();

    /// <summary>A new code for this member and player; the member's previous code stops working.</summary>
    public string Issue(ulong userId, string player, DateTimeOffset now)
    {
        var code = Random.Shared.Next(10000).ToString("D4");
        lock (_lock)
        {
            foreach (var stale in _byUser.Where(e => e.Value.Until <= now).Select(e => e.Key).ToList()) _byUser.Remove(stale);
            _byUser[userId] = (player, code, now + Lifetime);
        }
        return code;
    }

    /// <summary>The member a code proves, when <paramref name="player"/> whispered it in time; the code is spent. Null otherwise.</summary>
    public ulong? Redeem(string player, string code, DateTimeOffset now)
    {
        lock (_lock)
        {
            foreach (var (userId, pending) in _byUser)
                if (pending.Code == code && now < pending.Until
                    && string.Equals(pending.Player, player, StringComparison.OrdinalIgnoreCase))
                {
                    _byUser.Remove(userId);
                    return userId;
                }
            return null;
        }
    }

    /// <summary>The code in a whisper like « lien 4821 » or « Lien : 4821 »; null when it isn't one.</summary>
    public static string? ReadCode(string whisper) => CodeWhisper().Match(whisper.Trim()) is { Success: true } m ? m.Groups[1].Value : null;

    /// <summary>Whether a name can be a Java player name: 3 to 16 letters, digits or underscores.</summary>
    public static bool ValidName(string name) => PlayerName().IsMatch(name);

    [GeneratedRegex(@"^lien\W*(\d{4})$", RegexOptions.IgnoreCase)]
    private static partial Regex CodeWhisper();

    [GeneratedRegex(@"^[A-Za-z0-9_]{3,16}$")]
    private static partial Regex PlayerName();
}
