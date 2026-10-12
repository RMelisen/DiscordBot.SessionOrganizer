namespace ProjectSYNCS.Helpers;

// The one server her single-instance life belongs to: the morning hello and AmbientService
// keep one state each, so they live in one guild. /config reads this guild's main and idle
// channels for them, and refuses to store those settings anywhere else. Server-specific,
// listed in CLAUDE.md's "Hardcoded ids" (the same id the launch migrations name).
public static class HomeGuild
{
    public const ulong Id = 878305033995825164;
}
