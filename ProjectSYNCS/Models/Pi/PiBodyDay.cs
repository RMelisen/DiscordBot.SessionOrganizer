namespace ProjectSYNCS.Models;

// Her disk, memory and power over one Paris day, for /report syncs: what /health reads live, kept so a
// slow leak or a filling disk can be seen coming. The sizes are the day's last reading, the peaks the
// day's worst. Nullable where a reading can fail (off a Pi, a file missing). One row per day, like
// PiThermalDay; written by PiHealthService's tick.
public class PiBodyDay
{
    public int Day { get; set; }
    public long? DatabaseBytes { get; set; }
    public long WalPeakBytes { get; set; }
    public long? DiskFreeBytes { get; set; }
    public long? DiskTotalBytes { get; set; }
    public long ProcessPeakBytes { get; set; }
    public long? MemoryMinAvailableBytes { get; set; }
    public long? MemoryTotalBytes { get; set; }
    public int UnderVoltageMinutes { get; set; }
    public int SlowedMinutes { get; set; }
}
