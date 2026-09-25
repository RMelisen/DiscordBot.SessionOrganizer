namespace ProjectSYNCS.Models;

// A badge one Plynling earned (Helpers/PlynlingBadges holds the catalog). One row per
// (Plynling, badge), enforced by a unique index — which is what makes the reward paid once.
// Deleted with the Plynling (abandoned); kept when it dies, as part of its legacy.
public class PlynlingBadge
{
    public int Id { get; set; }
    public int PlynlingId { get; set; }

    // PlynlingBadges key — stable, never renamed.
    public string Key { get; set; } = string.Empty;
    public DateTimeOffset EarnedAt { get; set; }
}
