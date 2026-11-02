using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>One reading of her disk, memory and power, worth <paramref name="Minutes"/> minutes (a tick).</summary>
public sealed record PiBodySample(
    long? DatabaseBytes,
    long? WalBytes,
    long? DiskFreeBytes,
    long? DiskTotalBytes,
    long ProcessBytes,
    (long Total, long Available)? Memory,
    bool UnderVoltage,
    bool Slowed,
    int Minutes);

/// <param name="FirstDay">The first day measured in the window.</param>
/// <param name="DatabaseGrowth">The base's size on the last day measured, minus the first's.</param>
/// <param name="DaysUntilFull">At the window's pace, when the disk fills; null when it isn't filling or too few days were measured.</param>
public sealed record PiBodyReportData(
    int FirstDay,
    long? Database,
    long? DatabaseGrowth,
    long WalPeak,
    long? DiskFree,
    long? DiskTotal,
    int? DaysUntilFull,
    long ProcessPeak,
    long? MemoryMinAvailable,
    long? MemoryTotal,
    int UnderVoltageMinutes,
    int SlowedMinutes);

/// <summary>How her disk, memory and power went over one window, from PiBodyDay rows. Pure.</summary>
public static class PiBodyReport
{
    // Under this many days, a disk's pace is noise (a log rotation, an update).
    public const int MinDaysForPace = 7;

    /// <summary>Folds one reading into its day's row: sizes keep the last reading, peaks the worst.</summary>
    public static void Add(PiBodyDay row, PiBodySample s)
    {
        row.DatabaseBytes = s.DatabaseBytes ?? row.DatabaseBytes;
        row.WalPeakBytes = Math.Max(row.WalPeakBytes, s.WalBytes ?? 0);
        row.DiskFreeBytes = s.DiskFreeBytes ?? row.DiskFreeBytes;
        row.DiskTotalBytes = s.DiskTotalBytes ?? row.DiskTotalBytes;
        row.ProcessPeakBytes = Math.Max(row.ProcessPeakBytes, s.ProcessBytes);
        if (s.Memory is { } memory)
        {
            row.MemoryTotalBytes = memory.Total;
            row.MemoryMinAvailableBytes = row.MemoryMinAvailableBytes is { } low ? Math.Min(low, memory.Available) : memory.Available;
        }
        if (s.UnderVoltage) row.UnderVoltageMinutes += s.Minutes;
        if (s.Slowed) row.SlowedMinutes += s.Minutes;
    }

    /// <summary>Null when nothing was measured in the rows given.</summary>
    public static PiBodyReportData? Compute(IReadOnlyList<PiBodyDay> rows)
    {
        if (rows.Count == 0) return null;
        var days = rows.OrderBy(r => r.Day).ToList();

        var sized = days.Where(r => r.DatabaseBytes is not null).ToList();
        var disks = days.Where(r => r.DiskFreeBytes is not null).ToList();
        var lastDisk = disks.LastOrDefault();

        int? untilFull = null;
        if (disks.Count >= MinDaysForPace && lastDisk is not null)
        {
            var span = AppTime.FromDayKey(lastDisk.Day) - AppTime.FromDayKey(disks[0].Day);
            var lost = disks[0].DiskFreeBytes!.Value - lastDisk.DiskFreeBytes!.Value;
            if (lost > 0 && span.TotalDays >= 1)
                untilFull = (int)Math.Min(int.MaxValue, lastDisk.DiskFreeBytes.Value / (lost / span.TotalDays));
        }

        var memories = days.Where(r => r.MemoryMinAvailableBytes is not null).ToList();
        return new PiBodyReportData(
            days[0].Day,
            sized.LastOrDefault()?.DatabaseBytes,
            sized.Count >= 2 ? sized[^1].DatabaseBytes - sized[0].DatabaseBytes : null,
            days.Max(r => r.WalPeakBytes),
            lastDisk?.DiskFreeBytes,
            lastDisk?.DiskTotalBytes,
            untilFull,
            days.Max(r => r.ProcessPeakBytes),
            memories.Count == 0 ? null : memories.Min(r => r.MemoryMinAvailableBytes),
            memories.LastOrDefault()?.MemoryTotalBytes,
            days.Sum(r => r.UnderVoltageMinutes),
            days.Sum(r => r.SlowedMinutes));
    }
}
