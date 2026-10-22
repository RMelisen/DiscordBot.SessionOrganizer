namespace ProjectSYNCS.Models;

// A member badge that is stored rather than derived (Helpers/Xp/MemberBadges holds the catalog):
// the moments and secrets once earned, and the counters behind the two counter paliers (arcade,
// nanny). One row per (guild, user, badge), enforced by a unique index — which is what makes an
// award land once. Count is 1 for a moment or secret, the running counter for a counter palier.
public class MemberBadge
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }

    // MemberBadges.Keys value — stable, never renamed.
    public string Key { get; set; } = string.Empty;
    public long Count { get; set; } = 1;
    public DateTimeOffset EarnedAt { get; set; }
}
