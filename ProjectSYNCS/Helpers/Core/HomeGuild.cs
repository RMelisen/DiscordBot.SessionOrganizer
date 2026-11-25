namespace ProjectSYNCS.Helpers;

// The one server her single-instance life belongs to: her voice seat, Voyager's letters, the
// monthly digest, and the hardcoded people and channels (/shame's default voters, the real
// names and personal comebacks, the default channels). Everything else runs per server.
// Server-specific, listed in architecture.md's "Hardcoded ids" (the same id the launch
// migrations name).
public static class HomeGuild
{
    public const ulong Id = 878305033995825164;

    /// <summary>Whether this is the home server. A DM (null) is not.</summary>
    public static bool Is(ulong? guildId) => guildId == Id;
}
