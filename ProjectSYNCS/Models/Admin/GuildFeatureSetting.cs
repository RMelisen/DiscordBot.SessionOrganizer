namespace ProjectSYNCS.Models;

// A feature staff switched on or off on a server (Helpers/Admin/ServerFeatures). One row per (guild,
// feature), only once someone chose: no row means the default (on at home, off elsewhere, the
// welcome on everywhere), so a server nobody configured behaves as before.
public class GuildFeatureSetting
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }

    /// <summary>A <c>ServerFeatures</c> key: stored, so append-only.</summary>
    public string Feature { get; set; } = "";

    public bool Enabled { get; set; }
}
