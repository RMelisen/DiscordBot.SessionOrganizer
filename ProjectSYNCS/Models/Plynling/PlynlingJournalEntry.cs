using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Models;

// A moment in one Plynling's journal: what (Kind), when (At), and a detail for the wording
// (PlynlingJournalUi). The French is written at display, so it follows the Plynling's gender
// and any later rewording. At most PlynlingService.JournalCap per Plynling; the oldest go.
// Deleted with the Plynling (abandoned); kept when it dies.
public class PlynlingJournalEntry
{
    public int Id { get; set; }
    public int PlynlingId { get; set; }
    public DateTimeOffset At { get; set; }
    public JournalKind Kind { get; set; }
    public string? Detail { get; set; }
}
