using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Models;

// What two Plynlings are to each other (Helpers/PlynlingBonds holds the rules). One row per
// pair that has met, the lower id always first, so a pair has exactly one row whichever of the
// two visited — enforced by a unique index. Deleted with either Plynling (abandoned); kept when
// one dies, so the living one still remembers.
public class PlynlingRelation
{
    public int Id { get; set; }
    public int PlynlingAId { get; set; }
    public int PlynlingBId { get; set; }

    // −100…+100, moved by every visit's scene. The bond follows it, except a couple.
    public int Affinity { get; set; }
    public PlynlingBond Bond { get; set; }
    public int Meetings { get; set; }

    // When the current bond began.
    public DateTimeOffset Since { get; set; }
}
