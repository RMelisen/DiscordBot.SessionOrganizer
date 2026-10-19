namespace ProjectSYNCS.Helpers;

/// <summary>How someone died in Minecraft, as far as her lines care. Not stored: free to reorder.</summary>
public enum McDeathKind
{
    /// <summary>Her own death, whatever killed her.</summary>
    Self,
    Creeper,
    Fall,
    Fire,
    Drown,
    Mob,
    Player,
    Other,
}

/// <summary>One argument of a translated chat message: its rendered text and, for an entity, its translate key.</summary>
public readonly record struct McArg(string Text, string? Key);

/// <summary>
/// Reads the server's death messages. They arrive as translate keys (<c>death.attack.creeper</c>…)
/// with the victim as the first argument and the killer, if any, as the second: the keys are the same
/// in every language, so this never parses English text.
/// </summary>
public static class McDeath
{
    /// <summary>
    /// The kind of death, and who died; null when it isn't a player's death we should talk about:
    /// not a death key, or the victim isn't on the server (a named pet's death goes to its owner
    /// only, and isn't ours to mourn).
    /// </summary>
    public static (McDeathKind Kind, string Victim)? Classify(string key, IReadOnlyList<McArg> args,
        IReadOnlyCollection<string> players, string selfName)
    {
        if (!key.StartsWith("death.", StringComparison.Ordinal) || args.Count == 0) return null;
        var victim = args[0].Text;
        if (string.Equals(victim, selfName, StringComparison.OrdinalIgnoreCase)) return (McDeathKind.Self, victim);
        if (!players.Contains(victim, StringComparer.OrdinalIgnoreCase)) return null;

        var killer = args.Count > 1 ? args[1] : (McArg?)null;
        if (killer is { Key: "entity.minecraft.creeper" }) return (McDeathKind.Creeper, victim);
        if (key.StartsWith("death.fell.", StringComparison.Ordinal) || key.StartsWith("death.attack.fall", StringComparison.Ordinal))
            return (McDeathKind.Fall, victim);
        if (key.StartsWith("death.attack.lava", StringComparison.Ordinal) || key.StartsWith("death.attack.inFire", StringComparison.Ordinal)
            || key.StartsWith("death.attack.onFire", StringComparison.Ordinal) || key.StartsWith("death.attack.hotFloor", StringComparison.Ordinal))
            return (McDeathKind.Fire, victim);
        if (key.StartsWith("death.attack.drown", StringComparison.Ordinal)) return (McDeathKind.Drown, victim);
        if (killer is { } k && k.Key is null && !string.Equals(k.Text, victim, StringComparison.OrdinalIgnoreCase)
            && players.Contains(k.Text, StringComparer.OrdinalIgnoreCase))
            return (McDeathKind.Player, victim);
        if (killer is { Key: { } entity } && entity.StartsWith("entity.minecraft.", StringComparison.Ordinal))
            return (McDeathKind.Mob, victim);
        return (McDeathKind.Other, victim);
    }

    /// <summary>Who earned an advancement (<c>chat.type.advancement.*</c>), or null if it isn't one, or isn't a player on the server.</summary>
    public static string? AdvancementEarner(string key, IReadOnlyList<McArg> args, IReadOnlyCollection<string> players)
    {
        if (!key.StartsWith("chat.type.advancement.", StringComparison.Ordinal) || args.Count == 0) return null;
        return players.Contains(args[0].Text, StringComparer.OrdinalIgnoreCase) ? args[0].Text : null;
    }
}
