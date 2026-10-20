namespace ProjectSYNCS.Helpers;

/// <summary>Who a player is to her in game. Not stored: free to reorder.</summary>
public enum McKin
{
    Anyone,
    /// <summary>The owner, by <c>Minecraft:Owner</c>.</summary>
    Papa,
    /// <summary>Tata, by <c>Minecraft:Tata</c>.</summary>
    Tata,
}

/// <summary>
/// Picks her family's lines in game. Offline mode lets anyone log in under any name, so this only
/// chooses how she talks to someone, never anything that matters.
/// </summary>
public static class McFamily
{
    /// <param name="owner">The owner's in-game name (empty: not configured), any case.</param>
    /// <param name="tata">Tata's in-game name (empty: not configured), any case.</param>
    public static McKin Of(string player, string owner, string tata) =>
        player.Length == 0 ? McKin.Anyone
        : owner.Length > 0 && string.Equals(player, owner, StringComparison.OrdinalIgnoreCase) ? McKin.Papa
        : tata.Length > 0 && string.Equals(player, tata, StringComparison.OrdinalIgnoreCase) ? McKin.Tata
        : McKin.Anyone;
}
