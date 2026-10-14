using System.Text.Json;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// Her body, read for real: the Pi's CPU temperature (sampled into PiThermalDay, shown in her
// status when it runs hot or cool, announced when she breaks her heat record), how her last run
// ended (a proper stop, a power cut, a crash — AmbientService picks her wake line from it), and
// the gateway going dark for a while, which she blames on the router whether or not it was.
//
// Registered as a singleton and as the hosted service, like AmbientService: AmbientService,
// PresenceService and /health read this one instance. Hooks the client's Connected and
// Disconnected itself — connection lifecycle, like the Ready hooks in AmbientService — since no
// one else in BotService's fan-out cares.
public sealed class PiHealthService : BackgroundService
{
    // Its own interval, shared with no other loop (CLAUDE.md). Also the heartbeat's grain: a
    // power cut's measured length is off by at most this much.
    public static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(2);

    // How long to wait for the main channel after a reconnect before giving up the router line.
    private static readonly TimeSpan ChannelWait = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ChannelPollInterval = TimeSpan.FromSeconds(10);

    // How far back she reads the main channel to count what she missed.
    private const int MissedHistoryDepth = 100;

    // /health reads the database and a dozen files: once per person per half-minute.
    private static readonly TimeSpan HealthCooldown = TimeSpan.FromSeconds(30);

    // How many uptime events /health lists, and the window its tally covers.
    private const int RecentOutages = 5;
    private static readonly TimeSpan OutageTallyWindow = TimeSpan.FromDays(30);

    private const ulong RouterSalt = 0x5359_4E43_524F_5554;      // "SYNCROUT"
    private const ulong RouterQuietSalt = 0x5359_4E43_5155_4954; // "SYNCQUIT"
    private const ulong RecordSalt = 0x5359_4E43_4845_4154;      // "SYNCHEAT"

    private readonly DiscordSocketClient _client;
    private readonly IServiceProvider _services;
    private readonly GuildConfigService _config;
    private readonly BreakdownService _breakdown;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<PiHealthService> _logger;
    private readonly string _dbPath;
    private readonly string _statePath;
    private readonly CooldownGate<ulong> _healthChecks = new(HealthCooldown, forget: TimeSpan.FromMinutes(10));

    // Guards everything below.
    private readonly object _gate = new();
    private bool _classified;
    private StopKind _lastStop;
    private DateTimeOffset? _wentDarkAt;
    private DateTimeOffset _startedAt;
    private HealthState _state = new(false, DateTimeOffset.UtcNow);

    private int? _currentMilli;
    private DateTimeOffset? _outageStart;
    // Every gateway drop this run, Discord's ordinary reconnects included.
    private int _disconnects;

    // The record to beat today (every other day's maximum) and how many days of history stand
    // behind it, read once a day.
    private int _recordCheckedDay;
    private int? _recordToBeat;
    private int _historyDays;

    public PiHealthService(
        DiscordSocketClient client,
        IServiceProvider services,
        GuildConfigService guildConfig,
        BreakdownService breakdown,
        IHostApplicationLifetime lifetime,
        IConfiguration config,
        ILogger<PiHealthService> logger)
    {
        _lifetime = lifetime;
        _client = client;
        _services = services;
        _config = guildConfig;
        _breakdown = breakdown;
        _logger = logger;

        // Next to the database, so it lives under /data in production and survives updates.
        _dbPath = Path.GetFullPath(config["Database:Path"] ?? "ProjectSYNCS.db");
        _statePath = Path.Combine(Path.GetDirectoryName(_dbPath) ?? ".", "health-state.json");
    }

    /// <summary>How the previous run ended. Settled on first use, before the file is overwritten.</summary>
    public StopKind LastStop
    {
        get { EnsureClassified(); lock (_gate) return _lastStop; }
    }

    /// <summary>How long she was gone before this start, when the last run ended abruptly and it can be measured.</summary>
    public TimeSpan? Downtime
    {
        get
        {
            EnsureClassified();
            lock (_gate)
                return _wentDarkAt is { } dark && _startedAt > dark ? _startedAt - dark : null;
        }
    }

    /// <summary>The last temperature read, in thousandths of a degree; null off a Pi or before the first tick.</summary>
    public int? CurrentMilli
    {
        get { lock (_gate) return _currentMilli; }
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        EnsureClassified();
        // A first reading straight away, so the status line has a number before the first tick.
        lock (_gate) _currentMilli = PiHardware.TryReadCpuMilliCelsius();
        _client.Disconnected += OnDisconnectedAsync;
        _client.Connected += OnConnectedAsync;
        // A proper stop is recorded the moment it is asked for (SIGTERM from Home Assistant),
        // not once every service has finished stopping: the Supervisor kills an add-on that
        // takes longer than its timeout (10 s by default), and a stop killed half-way is still
        // a stop someone asked for, not a crash.
        _lifetime.ApplicationStopping.Register(MarkCleanExit);
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _client.Disconnected -= OnDisconnectedAsync;
        _client.Connected -= OnConnectedAsync;
        // Normally already done by ApplicationStopping; harmless twice.
        MarkCleanExit();
        return base.StopAsync(cancellationToken);
    }

    // The next start won't take this one for a power cut. Later heartbeats keep the flag.
    private void MarkCleanExit()
    {
        Update(s => s with { CleanExit = true, LastAliveAt = DateTimeOffset.UtcNow });
        _logger.LogInformation("Pi health: clean stop recorded.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // The stop found on start goes on record for the monthly report.
        try { await RecordAbruptStopAsync(); }
        catch (Exception ex) { _logger.LogWarning(ex, "Pi health: could not record the last stop."); }

        while (!stoppingToken.IsCancellationRequested)
        {
            // Each step in its own try: an exception escaping a hosted loop stops the bot.
            try { Heartbeat(); }
            catch (Exception ex) { _logger.LogWarning(ex, "Pi health: heartbeat failed."); }

            try { await SampleAsync(); }
            catch (Exception ex) { _logger.LogWarning(ex, "Pi health: temperature sample failed."); }

            await Task.Delay(TickInterval, stoppingToken);
        }
    }

    /// <summary>
    /// Claims a /health check for <paramref name="userId"/>; false while their last one is under
    /// half a minute old, with <paramref name="readyAt"/> saying when they may ask again.
    /// </summary>
    public bool TryClaimHealthCheck(ulong userId, out DateTimeOffset readyAt) =>
        _healthChecks.TryClaim(userId, out readyAt);

    /// <summary>
    /// For /health: everything she knows about her body right now. <paramref name="guild"/> is the
    /// server the command was run in, for its member cache.
    /// </summary>
    public async Task<HealthReport> SnapshotAsync(SocketGuild? guild)
    {
        var now = DateTimeOffset.UtcNow;
        var hostUptime = PiHardware.TryReadHostUptime();
        int? current;
        DateTimeOffset? outageSince;
        DateTimeOffset startedAt;
        int disconnects;
        lock (_gate)
        {
            current = _currentMilli;
            outageSince = _outageStart;
            startedAt = _startedAt;
            disconnects = _disconnects;
        }

        var todayKey = AppTime.DayKey(now);
        PiThermalDay? today;
        List<(int Day, int MaxMilli)> maxima;
        List<UptimeEvent> events;
        await using (var scope = _services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            today = await db.PiThermalDays.AsNoTracking().FirstOrDefaultAsync(d => d.Day == todayKey);
            // One row per day and a handful of events a month: both small enough to read whole,
            // and the events' window is on DateTimeOffset, which SQLite can't compare.
            maxima = (await db.PiThermalDays.Select(d => new { d.Day, d.MaxMilli }).ToListAsync())
                .Select(d => (d.Day, d.MaxMilli)).ToList();
            events = await db.UptimeEvents.AsNoTracking().ToListAsync();
        }

        var weekStart = AppTime.KeyDaysAgo(6);
        var week = maxima.Where(d => d.Day >= weekStart).ToList();
        var month = events.Where(e => now - e.EndedAt < OutageTallyWindow).ToList();

        using var process = System.Diagnostics.Process.GetCurrentProcess();

        return new HealthReport(
            now,
            new PiReading(
                hostUptime is { } up ? now - up : null,
                PiHardware.TryReadLoadAverage(),
                PiHardware.TryReadMemory(),
                PiHardware.TryReadCpuMegahertz(),
                PiHardware.TryReadPower()),
            new ProcessReading(
                startedAt,
                AppInfo.Version,
                System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                process.WorkingSet64,
                GC.GetGCMemoryInfo().HeapSizeBytes,
                process.Threads.Count,
                LastStop,
                Downtime),
            new GatewayReading(
                _client.ConnectionState == ConnectionState.Connected,
                _client.Latency,
                disconnects,
                outageSince,
                guild?.DownloadedMemberCount,
                guild?.MemberCount),
            ReadStorage(),
            new TemperatureReading(
                current,
                today,
                week.Count > 0 ? week.MaxBy(d => d.MaxMilli) : null,
                maxima.Count > 0 ? maxima.MaxBy(d => d.MaxMilli) : null,
                maxima.Count),
            new OutageReading(
                events.OrderByDescending(e => e.StartedAt).Take(RecentOutages).ToList(),
                month.GroupBy(e => e.Kind).ToDictionary(g => g.Key, g => g.Count()),
                TimeSpan.FromTicks(month.Sum(e => Math.Max(0, (e.EndedAt - e.StartedAt).Ticks)))),
            _statePath);
    }

    // The database, its write-ahead log, and the disk they live on. Each read on its own: a
    // missing -wal (no write since the last checkpoint) is ordinary.
    private StorageReading ReadStorage()
    {
        static long? Size(string path)
        {
            try { return File.Exists(path) ? new FileInfo(path).Length : null; }
            catch { return null; }
        }

        long? free = null, total = null;
        try
        {
            // statvfs on Linux takes any path on the filesystem; Windows wants the drive root.
            var dir = Path.GetDirectoryName(_dbPath) ?? ".";
            var drive = new DriveInfo(OperatingSystem.IsWindows() ? Path.GetPathRoot(dir) ?? dir : dir);
            free = drive.AvailableFreeSpace;
            total = drive.TotalSize;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Pi health: could not read the disk's free space.");
        }

        return new StorageReading(Size(_dbPath), Size(_dbPath + "-wal"), free, total);
    }

    // Read the previous run's state once, before anything overwrites it, then mark this run as
    // running. Called from StartAsync and from the getters: AmbientService may ask first, since
    // the client is already starting when the hosted services after BotService start.
    private void EnsureClassified()
    {
        lock (_gate)
        {
            if (_classified) return;
            _classified = true;

            var now = DateTimeOffset.UtcNow;
            var previous = LoadState();
            _lastStop = PiHealth.Classify(previous, now, PiHardware.TryReadHostUptime());
            _wentDarkAt = PiHealth.IsAbrupt(_lastStop) ? previous?.LastAliveAt : null;
            _startedAt = now;
            _state = new HealthState(false, now, previous?.LastRecordDay ?? 0, previous?.LastRouterDay ?? 0);
            SaveState(_state);
            _logger.LogInformation("Pi health: last stop was {Kind}.", _lastStop);
        }
    }

    private async Task RecordAbruptStopAsync()
    {
        var kind = LastStop;
        if (!PiHealth.IsAbrupt(kind)) return;

        DateTimeOffset start, end;
        lock (_gate)
        {
            if (_wentDarkAt is not { } dark || dark >= _startedAt) return;
            start = dark;
            end = _startedAt;
        }

        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.UptimeEvents.Add(new UptimeEvent
        {
            Kind = kind == StopKind.PowerCut ? UptimeEventKind.PowerCut : UptimeEventKind.Crash,
            StartedAt = start,
            EndedAt = end,
        });
        await db.SaveChangesAsync();
    }

    private void Heartbeat() => Update(s => s with { LastAliveAt = DateTimeOffset.UtcNow });

    private async Task SampleAsync()
    {
        var milli = PiHardware.TryReadCpuMilliCelsius();
        lock (_gate) _currentMilli = milli;
        if (milli is not { } t) return;

        var now = DateTimeOffset.UtcNow;
        var day = AppTime.DayKey(now);
        var hotMinutes = t >= PiHealth.ThrottleMilli ? (int)TickInterval.TotalMinutes : 0;

        int todayMax;
        await using (var scope = _services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var row = await db.PiThermalDays.FindAsync(day);
            if (row is null)
            {
                row = new PiThermalDay { Day = day, MinMilli = t, MaxMilli = t, MaxAt = now };
                db.PiThermalDays.Add(row);
            }
            else
            {
                row.MinMilli = Math.Min(row.MinMilli, t);
                if (t > row.MaxMilli)
                {
                    row.MaxMilli = t;
                    row.MaxAt = now;
                }
            }
            row.SumMilli += t;
            row.Samples++;
            row.HotMinutes += hotMinutes;
            await db.SaveChangesAsync();
            todayMax = row.MaxMilli;

            bool check;
            lock (_gate) check = _recordCheckedDay != day;
            if (check)
            {
                // Every other day's maximum, once a day: the record today has to beat.
                var others = await db.PiThermalDays.Where(d => d.Day != day).Select(d => d.MaxMilli).ToListAsync();
                lock (_gate)
                {
                    _recordCheckedDay = day;
                    _historyDays = others.Count;
                    _recordToBeat = others.Count > 0 ? others.Max() : null;
                }
            }
        }

        await TryHeatRecordAsync(now, day, todayMax);
    }

    // A new all-time high, once there is history enough for it to mean something: one line in
    // the main channel, daytime, once a day (the day is kept in the state file, so a restart
    // doesn't announce the same record twice).
    private async Task TryHeatRecordAsync(DateTimeOffset now, int day, int todayMax)
    {
        int old;
        lock (_gate)
        {
            if (_recordToBeat is not { } toBeat || _historyDays < PiHealth.RecordMinDays) return;
            if (todayMax < toBeat + PiHealth.RecordMarginMilli) return;
            if (_state.LastRecordDay == day || !PiHealth.IsRecordHours(now)) return;
            old = toBeat;
        }

        var channel = await ResolveMainChannelAsync();
        if (channel is null || _breakdown.IsActive(channel.Id)) return;

        var line = string.Format(
            DailyRotation.Pick(BotResponses.HeatRecordLines, AppTime.DayNumber(now), RecordSalt),
            PiHealth.Celsius(todayMax), PiHealth.Celsius(old));
        var sent = await BotChat.PostWithTypingAsync(channel, line, _logger, "heat record", AllowedMentions.None);
        if (sent is null) return;

        Update(s => s with { LastRecordDay = day });
        _logger.LogInformation("Pi health: heat record announced ({New} over {Old}).", todayMax, old);
    }

    private Task OnDisconnectedAsync(Exception _)
    {
        lock (_gate)
        {
            _outageStart ??= DateTimeOffset.UtcNow;
            _disconnects++;
        }
        return Task.CompletedTask;
    }

    // The gateway is back. A short gap is Discord's ordinary reconnect; a long one is the
    // router, as far as she's concerned. Off the gateway handler: it reads history and waits.
    private Task OnConnectedAsync()
    {
        DateTimeOffset? start;
        lock (_gate)
        {
            start = _outageStart;
            _outageStart = null;
        }

        var now = DateTimeOffset.UtcNow;
        if (start is { } s && now - s >= PiHealth.OutageMin)
            _ = Task.Run(() => HandleOutageAsync(s, now));
        return Task.CompletedTask;
    }

    private async Task HandleOutageAsync(DateTimeOffset start, DateTimeOffset end)
    {
        _logger.LogInformation("Pi health: back after {Minutes:0} min without the gateway.", (end - start).TotalMinutes);

        try
        {
            await using var scope = _services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.UptimeEvents.Add(new UptimeEvent { Kind = UptimeEventKind.NetworkOutage, StartedAt = start, EndedAt = end });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Pi health: could not record the outage.");
        }

        try
        {
            var day = AppTime.DayKey(end);
            lock (_gate)
                if (_state.LastRouterDay == day) return;

            var channel = await WaitForMainChannelAsync();
            if (channel is null || _breakdown.IsActive(channel.Id)) return;

            // What was said in the main channel while she was gone.
            var recent = (await channel.GetMessagesAsync(MissedHistoryDepth).FlattenAsync()).ToList();
            var missed = recent.Count(m => !m.Author.IsBot && m.Timestamp >= start && m.Timestamp <= end);
            // A full page whose oldest message is still inside the outage: there was more.
            bool more = recent.Count >= MissedHistoryDepth && recent.Min(m => m.Timestamp) > start;

            var dayNumber = AppTime.DayNumber(end);
            var duration = PiHealth.Duration(end - start);
            var line = missed > 0
                ? string.Format(DailyRotation.Pick(BotResponses.RouterReturnLines, dayNumber, RouterSalt),
                    duration, PiHealth.Missed(missed, more))
                : string.Format(DailyRotation.Pick(BotResponses.RouterReturnQuietLines, dayNumber, RouterQuietSalt), duration);

            var sent = await BotChat.PostWithTypingAsync(channel, line, _logger, "router line", AllowedMentions.None);
            if (sent is null) return;

            Update(s => s with { LastRouterDay = day });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Pi health: router line failed.");
        }
    }

    private async Task<IMessageChannel?> WaitForMainChannelAsync()
    {
        var deadline = DateTimeOffset.UtcNow + ChannelWait;
        IMessageChannel? channel;
        while ((channel = await ResolveMainChannelAsync()) is null)
        {
            // In the dev guild the channel never appears; this just runs out.
            if (DateTimeOffset.UtcNow >= deadline) return null;
            await Task.Delay(ChannelPollInterval);
        }
        return channel;
    }

    private async Task<IMessageChannel?> ResolveMainChannelAsync() =>
        _client.ConnectionState == ConnectionState.Connected
            ? _client.GetChannel(await MorningGreetingService.MainChannelIdAsync(_config)) as IMessageChannel
            : null;

    // Every change goes through here: the state and its file move together under the lock, so
    // two writers (the tick, a router line) can't put an older copy on disk after a newer one.
    private void Update(Func<HealthState, HealthState> change)
    {
        lock (_gate)
        {
            _state = change(_state);
            SaveState(_state);
        }
    }

    private HealthState? LoadState()
    {
        try
        {
            if (File.Exists(_statePath))
                return JsonSerializer.Deserialize<HealthState>(File.ReadAllText(_statePath));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Pi health: could not read {Path}; treating the last stop as unknown.", _statePath);
        }
        return null;
    }

    private void SaveState(HealthState state)
    {
        try
        {
            File.WriteAllText(_statePath, JsonSerializer.Serialize(state));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Pi health: could not write {Path}.", _statePath);
        }
    }
}
