namespace ProjectSYNCS.Models;

// One /admin warn. Never deleted: /admin unwarn sets RevokedAt, and an old warning simply stops
// counting once it is older than WarningRules.Lifetime. The record stays either way, so staff
// can still read the history in /admin warnings.
public class MemberWarning
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }

    /// <summary>
    /// Who gave it. Shown to staff in /admin warnings, never to the person warned: the DM says
    /// it comes from the team.
    /// </summary>
    public ulong ModeratorId { get; set; }

    public string Reason { get; set; } = "";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>Who revoked it. Zero while <see cref="RevokedAt"/> is null.</summary>
    public ulong RevokedById { get; set; }
}
