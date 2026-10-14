using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Everything /health shows, read once by PiHealthService.SnapshotAsync and rendered by
// HealthCards. Plain values, no gateway and no database, so the card can be built and measured
// without either. Null means « couldn't read it » (off a Pi, a file missing), never zero.
public sealed record HealthReport(
    DateTimeOffset TakenAt,
    PiReading Pi,
    ProcessReading Process,
    GatewayReading Gateway,
    StorageReading Storage,
    TemperatureReading Temperature,
    OutageReading Outages,
    string StatePath);

public sealed record PiReading(
    DateTimeOffset? BootedAt,
    (double One, double Five, double Fifteen)? Load,
    (long Total, long Available)? Memory,
    int? CpuMegahertz,
    PowerReading? Power);

public sealed record ProcessReading(
    DateTimeOffset StartedAt,
    string Version,
    string Runtime,
    long WorkingSetBytes,
    long HeapBytes,
    int Threads,
    StopKind LastStop,
    TimeSpan? Downtime);

public sealed record GatewayReading(
    bool Connected,
    int LatencyMs,
    int Disconnects,
    DateTimeOffset? OutageSince,
    // The server /health was run in: members in her cache against the server's count.
    int? MembersCached,
    int? MemberCount);

public sealed record StorageReading(
    long? DatabaseBytes,
    long? WalBytes,
    long? FreeBytes,
    long? TotalBytes);

/// <param name="Today">Today's row, or null before the first sample.</param>
/// <param name="WeekHottest">The hottest of the last 7 days, today included.</param>
/// <param name="Record">The hottest day ever measured, today included.</param>
public sealed record TemperatureReading(
    int? CurrentMilli,
    PiThermalDay? Today,
    (int Day, int MaxMilli)? WeekHottest,
    (int Day, int MaxMilli)? Record,
    int DaysMeasured);

/// <param name="Recent">The latest events, newest first.</param>
/// <param name="MonthCounts">Events over the last 30 days, by kind.</param>
/// <param name="MonthDown">Their summed length.</param>
public sealed record OutageReading(
    IReadOnlyList<UptimeEvent> Recent,
    IReadOnlyDictionary<UptimeEventKind, int> MonthCounts,
    TimeSpan MonthDown);
