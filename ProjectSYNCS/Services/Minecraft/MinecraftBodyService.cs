using System.Text;
using System.Text.Json;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>
/// Her body on the Minecraft server. Every minute (its own interval) she pings the server and
/// decides, by <see cref="McPresence"/>, whether she should be in game; joining starts the Node
/// body (<see cref="MinecraftBodyProcess"/>), leaving sends it a goodbye and a quit. While she's in,
/// the body reports what happens and she answers in Minecraft's chat with lines from
/// <see cref="BotResponses"/>: the body itself holds no words and no policy.
/// </summary>
/// <remarks>
/// Off unless <c>Minecraft:Host</c> is set (the add-on option <c>minecraft_host</c>), and she joins
/// nothing until the owner allows it with /debug minecraft: that switch is the one thing kept across
/// restarts (minecraft-state.json, next to the database), and it starts off. Singleton for
/// both roles, like the other services /debug reaches: the host runs the loop, /debug minecraft
/// reads the state and forces a join or a leave. Everything here is in memory and resets on restart.
/// Public because the public DebugModule injects it.
/// </remarks>
public sealed class MinecraftBodyService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan PingTimeout = TimeSpan.FromSeconds(5);
    // How long she waits after spawning before her hello, so the client is fully in.
    private static readonly TimeSpan ArrivalPause = TimeSpan.FromSeconds(3);
    // How long a quit may take before the process is killed.
    private static readonly TimeSpan QuitGrace = TimeSpan.FromSeconds(10);
    // After an unplanned end (crash, kick, refused login): wait 2, then 5, then 15 minutes.
    private static readonly TimeSpan[] Backoff = { TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15) };
    // /debug minecraft leave keeps her out this long, or the next tick would send her straight back.
    private static readonly TimeSpan ForcedLeaveHold = TimeSpan.FromMinutes(30);

    // Her in-game remarks: one per kind every 30 s, and never two within 8 s of each other. Two gates,
    // two policies; neither is shared with anything else.
    private readonly CooldownGate<string> _kindGate = new(TimeSpan.FromSeconds(30));
    private readonly CooldownGate<int> _anyGate = new(TimeSpan.FromSeconds(8));
    // Orders are answered, not rationed like remarks: only a player sending a second one within the same
    // second is ignored (a double send), never a stop, and her « oui ? » to a bare call is said to the same
    // player once every 10 seconds at most.
    private readonly CooldownGate<string> _orderGate = new(TimeSpan.FromSeconds(1), TimeSpan.FromHours(1));
    private readonly CooldownGate<string> _callGate = new(TimeSpan.FromSeconds(10), TimeSpan.FromHours(1));
    // « Je suis avec X » to a player she's refusing, once every 10 seconds at most.
    private readonly CooldownGate<string> _busyGate = new(TimeSpan.FromSeconds(10), TimeSpan.FromHours(1));
    // After her hello, before she walks up to Papa or Tata: her player list has arrived by then.
    private static readonly TimeSpan SeekPause = TimeSpan.FromSeconds(2);
    // After a bare call (« syncs »), that player's next message within this long is read as if it carried
    // her name (« suis moi »). One message only, whatever it turns out to be. Guarded by _gate.
    private static readonly TimeSpan CallWindow = TimeSpan.FromSeconds(30);
    private readonly Dictionary<string, DateTimeOffset> _calledAt = new();
    private const double NightChance = 0.5;
    private const double AdvancementChance = 0.6;

    private readonly DiscordSocketClient _client;
    private readonly GuildConfigService _config;
    private readonly PlynlingAnnouncer _announcer;
    private readonly ResponsePicker _picker;
    private readonly ILogger<MinecraftBodyService> _logger;

    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _owner;
    // Tata's in-game name, for her lines only (McFamily). Empty: not configured.
    private readonly string _tata;
    private readonly string _node;
    private readonly string? _script;
    private readonly string _statePath;

    private readonly object _gate = new();
    // Whether the owner allows her on the server at all. Persisted, and off until allowed.
    private bool _allowed;
    private MinecraftBodyProcess? _body;
    private bool _spawned;
    private bool _leaving;
    private DateTimeOffset? _inGameSince;
    private List<string> _players = new();
    private DateTimeOffset? _fewSince;
    private int _failures;
    private DateTimeOffset _retryAfter = DateTimeOffset.MinValue;
    private DateTimeOffset _holdUntil = DateTimeOffset.MinValue;
    private ServerStatus? _lastStatus;
    private DateTimeOffset? _lastPingAt;
    private string? _resolved;
    // Orders and who gave them, whispered or not (the outcome is answered the same way). _order is the one
    // under way: she works for that player (McClaim). _pending was sent and not yet confirmed by the body;
    // one that fails at once leaves _order alone, as the body leaves her task. Robbed: who she was taken
    // from, told once the new order is under way.
    private sealed record McJob(McOrder Order, string Player, bool Private, string Detail, DateTimeOffset Since,
        string? Robbed = null, bool RobbedPrivate = false);
    private McJob? _order;
    private McJob? _pending;
    // The family member she's walking up to after arriving (McFamily), until the body says how it went.
    private string? _seeking;
    // Where she last saw Papa and Tata, by in-game name. Persisted: they log back in where they logged out.
    private readonly Dictionary<string, McSpot> _seen;
    private (string Server, string Using, bool Fallback)? _version;
    private string? _lastKick;
    private DateTimeOffset? _lastKickAt;

    public MinecraftBodyService(DiscordSocketClient client, GuildConfigService config, PlynlingAnnouncer announcer,
        ResponsePicker picker, IConfiguration configuration, ILogger<MinecraftBodyService> logger)
    {
        _client = client;
        _config = config;
        _announcer = announcer;
        _picker = picker;
        _logger = logger;

        _host = configuration["Minecraft:Host"]?.Trim() ?? "";
        _port = int.TryParse(configuration["Minecraft:Port"], out var port) && port is > 0 and < 65536 ? port : 25565;
        _username = configuration["Minecraft:Username"] is { Length: > 0 } name ? name.Trim() : "SYNCS";
        _owner = configuration["Minecraft:Owner"]?.Trim() ?? "";
        _tata = configuration["Minecraft:Tata"]?.Trim() ?? "";
        _node = configuration["Minecraft:Node"] is { Length: > 0 } node ? node : "node";
        _script = ResolveScript(configuration["Minecraft:BodyScript"]);

        var dbPath = Path.GetFullPath(configuration["Database:Path"] ?? "ProjectSYNCS.db");
        _statePath = Path.Combine(Path.GetDirectoryName(dbPath) ?? ".", "minecraft-state.json");
        var state = LoadState();
        _allowed = state?.Allowed ?? false;
        _seen = new Dictionary<string, McSpot>(state?.Seen ?? new Dictionary<string, McSpot>(), StringComparer.OrdinalIgnoreCase);
    }

    // What survives a restart: the owner's switch, and where she last saw her family. Everything else here
    // is in memory on purpose.
    private sealed record MinecraftState(bool Allowed, Dictionary<string, McSpot>? Seen = null);
    private sealed record McSpot(int X, int Y, int Z, string Dimension);

    private MinecraftState StateLocked() => new(_allowed, new Dictionary<string, McSpot>(_seen));

    private MinecraftState? LoadState()
    {
        try
        {
            if (File.Exists(_statePath))
                return JsonSerializer.Deserialize<MinecraftState>(File.ReadAllText(_statePath));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Minecraft: could not read {Path}.", _statePath);
        }
        return null;
    }

    private void SaveState(MinecraftState state)
    {
        try
        {
            File.WriteAllText(_statePath, JsonSerializer.Serialize(state));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Minecraft: could not write {Path}.", _statePath);
        }
    }

    public bool Enabled => _host.Length > 0;

    // The add-on image puts the body at /app/body; a dev run (`dotnet run` from the project folder)
    // finds it in MinecraftBody/ beside the sources.
    private static string? ResolveScript(string? configured)
    {
        var candidates = new[]
        {
            configured,
            Path.Combine(AppContext.BaseDirectory, "body", "index.js"),
            Path.Combine(Directory.GetCurrentDirectory(), "MinecraftBody", "index.js"),
        };
        return candidates.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Enabled)
        {
            _logger.LogInformation("Minecraft body off: no Minecraft:Host configured.");
            return;
        }
        if (_script is null)
        {
            _logger.LogWarning("Minecraft body off: MinecraftBody/index.js not found.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Minecraft body tick failed.");
            }
            await Task.Delay(Interval, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        MinecraftBodyProcess? body;
        lock (_gate) body = _body;
        if (body is null) return;
        // No goodbye on a shutdown: just leave cleanly, and make sure no orphan stays in the server.
        body.Send(new { op = "quit" });
        await Task.Delay(TimeSpan.FromSeconds(2), CancellationToken.None);
        body.Kill();
    }

    private async Task TickAsync(CancellationToken ct)
    {
        // A bare name on the default port may point elsewhere through SRV (McAddress); the body is
        // given the address as configured and does the same lookup itself.
        var (pingHost, pingPort) = await McAddress.ResolveAsync(_host, _port, ct);
        var status = await ServerListPing.QueryAsync(pingHost, pingPort, PingTimeout, ct);
        var now = DateTimeOffset.UtcNow;

        bool join, leave;
        lock (_gate)
        {
            _lastStatus = status;
            _lastPingAt = now;
            _resolved = (pingHost, pingPort) == (_host, _port) ? null : $"{pingHost}:{pingPort}";
            var inGame = _body is not null;

            // Once she's in, her own player list is exact; before that, the ping's count and sample.
            IReadOnlyCollection<string> others;
            int othersCount;
            if (_spawned)
            {
                others = _players;
                othersCount = _players.Count;
            }
            else
            {
                others = status?.Sample.Where(n => !n.Equals(_username, StringComparison.OrdinalIgnoreCase)).ToList()
                         ?? new List<string>();
                var selfListed = status?.Sample.Contains(_username, StringComparer.OrdinalIgnoreCase) == true;
                othersCount = Math.Max(0, (status?.Online ?? 0) - (selfListed ? 1 : 0));
            }

            // Not allowed: she still watches the server (so /debug shows it) but never goes in.
            var verdict = McPresence.Decide(status is not null && _allowed, others, othersCount, _owner, inGame, _fewSince, now);
            _fewSince = verdict.FewSince;
            join = !inGame && verdict.InGame && now >= _retryAfter && now >= _holdUntil;
            leave = inGame && !verdict.InGame && !_leaving;
        }

        if (join) StartBody();
        else if (leave) Leave();
    }

    // ---- The body's lifecycle -------------------------------------------------------------------

    private bool StartBody()
    {
        lock (_gate)
        {
            if (_body is not null || _script is null) return false;
            try
            {
                _body = MinecraftBodyProcess.Start(_node, _script,
                    new Dictionary<string, string>
                    {
                        ["MC_HOST"] = _host,
                        ["MC_PORT"] = _port.ToString(),
                        ["MC_USERNAME"] = _username,
                    },
                    OnEvent, line => _logger.LogInformation("Minecraft body: {Line}", line), OnExit);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Couldn't start the Minecraft body ({Node} {Script}).", _node, _script);
                MarkFailureLocked(DateTimeOffset.UtcNow);
                return false;
            }
            _spawned = false;
            _leaving = false;
            _players = new List<string>();
            _fewSince = null;
            _order = null;
            _pending = null;
            _seeking = null;
            _logger.LogInformation("Minecraft body started (pid {Pid}) for {Host}:{Port}.", _body.Id, _host, _port);
            return true;
        }
    }

    /// <summary>Says goodbye (if she got in) and quits; kills the body if it lingers.</summary>
    private void Leave()
    {
        MinecraftBodyProcess body;
        lock (_gate)
        {
            if (_body is null || _leaving) return;
            body = _body;
            _leaving = true;
            if (_spawned) body.Send(new { op = "say", text = _picker.Pick(BotResponses.McLeavingLines) });
        }
        body.Send(new { op = "quit" });
        _ = Task.Run(async () =>
        {
            await Task.Delay(QuitGrace);
            body.Kill();
        });
    }

    private void OnExit(MinecraftBodyProcess body)
    {
        bool wasSpawned;
        lock (_gate)
        {
            if (!ReferenceEquals(_body, body)) return;
            wasSpawned = _spawned;
            var planned = _leaving;
            _body = null;
            _spawned = false;
            _leaving = false;
            _inGameSince = null;
            _players = new List<string>();
            _fewSince = null;
            _order = null;
            _pending = null;
            _seeking = null;
            if (!planned) MarkFailureLocked(DateTimeOffset.UtcNow);
            _logger.LogInformation("Minecraft body exited ({How}).", planned ? "left" : "unplanned");
        }
        if (wasSpawned) _ = PostAsync(BotResponses.McLeavePostLines, "Minecraft leave post");
    }

    private void MarkFailureLocked(DateTimeOffset now)
    {
        _failures++;
        _retryAfter = now + Backoff[Math.Min(_failures, Backoff.Length) - 1];
    }

    // ---- What the body reports ------------------------------------------------------------------

    private void OnEvent(string line)
    {
        // The body keeps stdout for events; anything else that slips through is log, not protocol.
        if (!line.StartsWith('{'))
        {
            _logger.LogInformation("Minecraft body: {Line}", line);
            return;
        }
        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            switch (root.GetProperty("ev").GetString())
            {
                case "spawned":
                    OnSpawned();
                    break;
                case "version":
                    var version = (root.GetProperty("server").GetString() ?? "", root.GetProperty("using").GetString() ?? "",
                        root.TryGetProperty("fallback", out var fb) && fb.ValueKind == JsonValueKind.True);
                    lock (_gate) _version = version;
                    break;
                case "players":
                    var names = root.GetProperty("names").EnumerateArray().Select(n => n.GetString() ?? "")
                        .Where(n => n.Length > 0 && !n.Equals(_username, StringComparison.OrdinalIgnoreCase)).ToList();
                    lock (_gate) _players = names;
                    break;
                case "system":
                    OnSystem(root.GetProperty("key").GetString() ?? "",
                        root.GetProperty("args").EnumerateArray()
                            .Select(a => new McArg(a.GetProperty("text").GetString() ?? "",
                                a.TryGetProperty("key", out var k) && k.ValueKind == JsonValueKind.String ? k.GetString() : null))
                            .ToList());
                    break;
                case "chat":
                    OnChat(root.GetProperty("player").GetString() ?? "", root.GetProperty("text").GetString() ?? "",
                        root.TryGetProperty("private", out var priv) && priv.ValueKind == JsonValueKind.True);
                    break;
                case "moving":
                    OnMoving(root.GetProperty("player").GetString() ?? "", root.GetProperty("kind").GetString() ?? "");
                    break;
                case "arrived":
                    OnArrived(root.GetProperty("player").GetString() ?? "",
                        root.TryGetProperty("kind", out var arrivedKind) ? arrivedKind.GetString() ?? "" : "");
                    break;
                case "cantReach":
                    OnOutcome(root.GetProperty("player").GetString() ?? "", root.GetProperty("why").GetString() switch
                    {
                        "notVisible" => BotResponses.McTooFarLines,
                        "lost" => BotResponses.McLostLines,
                        "tooFar" => BotResponses.McTripTooFarLines,
                        _ => BotResponses.McNoPathLines,
                    });
                    break;
                case "killed":
                    Remark("kill", BotResponses.McGuardKillLines, root.GetProperty("player").GetString());
                    break;
                case "collecting":
                    OnCollecting(root.GetProperty("player").GetString() ?? "", root.GetProperty("resource").GetString() ?? "",
                        root.GetProperty("count").GetInt32());
                    break;
                case "collected":
                    OnCollected(root.GetProperty("player").GetString() ?? "", root.GetProperty("resource").GetString() ?? "",
                        root.GetProperty("count").GetInt32(), root.GetProperty("delivered").GetBoolean());
                    break;
                case "cantCollect":
                    OnOutcome(root.GetProperty("player").GetString() ?? "",
                        root.GetProperty("why").GetString() == "noTool" ? BotResponses.McCollectNoToolLines : BotResponses.McCollectNoneLines,
                        "", ResourceName(root.GetProperty("resource").GetString() ?? ""));
                    break;
                case "sleeping":
                    OnSlept(root.GetProperty("player").GetString() ?? "");
                    break;
                case "cantSleep":
                    var whyNot = root.GetProperty("why").GetString();
                    OnOutcome(root.GetProperty("player").GetString() ?? "", whyNot switch
                    {
                        "day" => BotResponses.McNotNightLines,
                        "noBed" => BotResponses.McNoBedLines,
                        _ => BotResponses.McBedFailedLines,
                    });
                    // Every bed refused her after she'd dropped what she was doing: she works for nobody now,
                    // unless it was a farm, which the body goes back to.
                    if (whyNot == "failed") lock (_gate) if (_order?.Order != McOrder.Farm) _order = null;
                    break;
                case "harvesting":
                    var (harvestPrivate, _) = Confirm(root.GetProperty("player").GetString() ?? "");
                    Reply(root.GetProperty("player").GetString() ?? "", harvestPrivate, BotResponses.McHarvestAckLines,
                        root.GetProperty("count").GetInt32(), CropName(root.GetProperty("crop").GetString() ?? ""));
                    break;
                case "harvested":
                    var harvested = root.GetProperty("count").GetInt32();
                    OnOutcome(root.GetProperty("player").GetString() ?? "",
                        harvested == 0 ? BotResponses.McHarvestEmptyLines
                        : root.GetProperty("delivered").GetBoolean() ? BotResponses.McHarvestDoneLines
                        : BotResponses.McHarvestKeptLines,
                        harvested, CropName(root.GetProperty("crop").GetString() ?? ""));
                    break;
                case "cantHarvest":
                    OnOutcome(root.GetProperty("player").GetString() ?? "", BotResponses.McHarvestNoneLines,
                        "", CropName(root.GetProperty("crop").GetString() ?? ""));
                    break;
                case "farming":
                    var farmer = root.GetProperty("player").GetString() ?? "";
                    Reply(farmer, Confirm(farmer).Whispered, BotResponses.McFarmAckLines);
                    break;
                case "cantFarm":
                    OnOutcome(root.GetProperty("player").GetString() ?? "",
                        root.GetProperty("why").GetString() == "noChest" ? BotResponses.McFarmNoChestLines : BotResponses.McFarmNoneLines);
                    break;
                case "seek":
                    OnSeek(root.GetProperty("player").GetString() ?? "", root.GetProperty("outcome").GetString() == "met");
                    break;
                case "seen":
                    OnSeen(root.GetProperty("player").GetString() ?? "", new McSpot(root.GetProperty("x").GetInt32(),
                        root.GetProperty("y").GetInt32(), root.GetProperty("z").GetInt32(), root.GetProperty("dimension").GetString() ?? ""));
                    break;
                case "hurt":
                    if (root.TryGetProperty("by", out var by) && by.ValueKind == JsonValueKind.String && by.GetString() is { Length: > 0 } hitter)
                        Remark("hit", Kin(hitter, BotResponses.McHitByPlayerLines,
                            BotResponses.McHitByPlayerLinesOwner, BotResponses.McHitByPlayerLinesTata), hitter);
                    break;
                case "night":
                    if (Random.Shared.NextDouble() < NightChance) Remark("night", BotResponses.McNightLines);
                    break;
                case "kicked":
                    var reason = root.GetProperty("reason").GetString() ?? "";
                    lock (_gate)
                    {
                        _lastKick = reason;
                        _lastKickAt = DateTimeOffset.UtcNow;
                    }
                    _logger.LogWarning("Minecraft body kicked: {Reason}", reason);
                    break;
                case "end":
                    _logger.LogInformation("Minecraft body disconnected: {Reason}", root.GetProperty("reason").GetString());
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Unreadable Minecraft body event ({Error}): {Line}", ex.Message, line);
        }
    }

    private void OnSpawned()
    {
        MinecraftBodyProcess? body;
        lock (_gate)
        {
            body = _body;
            if (body is null) return;
            _spawned = true;
            _inGameSince = DateTimeOffset.UtcNow;
            _failures = 0;
        }
        _logger.LogInformation("Minecraft body spawned on {Host}:{Port}.", _host, _port);
        _ = Task.Run(async () =>
        {
            await Task.Delay(ArrivalPause);
            lock (_gate)
                if (ReferenceEquals(_body, body) && !_leaving)
                    body.Send(new { op = "say", text = _picker.Pick(BotResponses.McArrivalLines) });
            await Task.Delay(SeekPause);
            lock (_gate)
                if (ReferenceEquals(_body, body) && !_leaving)
                    SeekFamilyLocked(body);
        });
        _ = PostAsync(BotResponses.McJoinPostLines, "Minecraft join post");
    }

    private void OnSystem(string key, IReadOnlyList<McArg> args)
    {
        List<string> players;
        lock (_gate) players = _players;

        if (McDeath.Classify(key, args, players, _username) is { } death)
        {
            var pool = death.Kind == McDeathKind.Self ? BotResponses.McDeathLines[death.Kind]
                : Kin(death.Victim, BotResponses.McDeathLines[death.Kind], BotResponses.McDeathLinesOwner, BotResponses.McDeathLinesTata);
            Remark($"death:{death.Kind}", pool, death.Victim);
            return;
        }
        if (McDeath.AdvancementEarner(key, args, players) is { } earner && Random.Shared.NextDouble() < AdvancementChance)
            Remark("advancement", Kin(earner, BotResponses.McAdvancementLines,
                BotResponses.McAdvancementLinesOwner, BotResponses.McAdvancementLinesTata), earner);
    }

    // ---- Orders ---------------------------------------------------------------------------------

    private void OnChat(string player, string text, bool whispered)
    {
        if (player.Length == 0) return;
        bool called;
        lock (_gate) called = _calledAt.Remove(player, out var calledAt) && DateTimeOffset.UtcNow - calledAt <= CallWindow;
        var request = McCommand.Parse(text, [_username, .. McCommand.HerNames], called);
        switch (request.Order)
        {
            case McOrder.None:
                return;
            case McOrder.Unknown:
                lock (_gate) _calledAt[player] = DateTimeOffset.UtcNow;
                if (_callGate.TryClaim(player))
                    Reply(player, whispered, Kin(player, BotResponses.McCalledLines, BotResponses.McCalledLinesOwner, BotResponses.McCalledLinesTata));
                return;
        }
        if (request.Order != McOrder.Stay && !_orderGate.TryClaim(player)) return;

        object command = request.Order switch
        {
            McOrder.Come => new { op = "come", player },
            McOrder.Follow => new { op = "follow", player },
            McOrder.GoTo => request.Coordinates! is [var x, var z]
                ? new { op = "goto", player, x, y = (int?)null, z }
                : new { op = "goto", player, x = request.Coordinates![0], y = (int?)request.Coordinates[1], z = request.Coordinates[2] },
            McOrder.Guard => new { op = "guard", player },
            McOrder.Collect => new { op = "collect", player, resource = request.Resource!.Value.ToString().ToLowerInvariant(), count = request.Count },
            McOrder.Sleep => new { op = "sleep", player },
            McOrder.Harvest => new { op = "harvest", player, crop = (request.Crop ?? McCrop.Any).ToString().ToLowerInvariant() },
            McOrder.Farm => new { op = "farm", player },
            _ => new { op = "stay" },
        };
        var detail = request.Coordinates is { } coordinates ? string.Join(" ", coordinates) : "";
        var now = DateTimeOffset.UtcNow;
        string? busyWith = null;
        McJob? robbed = null;
        lock (_gate)
        {
            if (_body is not { } body || !_spawned || _leaving) return;
            // She works for whoever gave the order under way: only they can stop her or send her elsewhere,
            // unless their claim ran out or family outranks them (McClaim).
            if (_order is { } holder
                && McClaim.Decide(holder.Player, KinOf(holder.Player), holder.Since, player, KinOf(player), now, LastingLocked(holder))
                == McVerdict.Busy)
                busyWith = holder.Player;
            else
            {
                // Taken from someone before their order was done: they hear why, once the new one is under way.
                robbed = _order is { } taken && !Same(taken.Player, player) && (LastingLocked(taken) || now - taken.Since < McClaim.Expiry)
                    ? taken
                    : null;
                _seeking = null;
                if (request.Order == McOrder.Stay)
                {
                    _order = null;
                    _pending = null;
                }
                else _pending = new McJob(request.Order, player, whispered, detail, now, robbed?.Player, robbed?.Private ?? false);
                body.Send(command);
            }
        }
        if (busyWith is not null)
        {
            if (_busyGate.TryClaim(player)) Reply(player, whispered, BotResponses.McBusyLines, Who(busyWith));
            return;
        }
        // Everything but staying is acknowledged when the body reports it under way ("moving",
        // "collecting", "sleeping"): the player may be out of sight or there may be nothing to gather,
        // and « j'arrive » followed by « je te vois pas » reads as a broken promise.
        if (request.Order == McOrder.Stay)
        {
            if (robbed is not null) Reply(robbed.Player, robbed.Private, BotResponses.McTakenLines, Who(player));
            Reply(player, whispered, BotResponses.McStayAckLines);
        }
    }

    // A farm holds her while its keeper is on the server; every other claim runs out (McClaim.Expiry).
    private bool LastingLocked(McJob job) =>
        job.Order == McOrder.Farm && _players.Contains(job.Player, StringComparer.OrdinalIgnoreCase);

    private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private McKin KinOf(string player) => McFamily.Of(player, _owner, _tata);

    // How she calls a player in her lines: « Papa » and « Tata » for family, their in-game name otherwise.
    private string Who(string player) => KinOf(player) switch
    {
        McKin.Papa => "Papa",
        McKin.Tata => "Tata",
        _ => player,
    };

    // The body says this player's order is under way: the pending order becomes hers to work on, and
    // whoever she was taken from hears it. Returns how to answer (whispered, detail).
    private (bool Whispered, string Detail) Confirm(string player)
    {
        McJob? job;
        lock (_gate)
        {
            if (_pending is { } pending && Same(pending.Player, player))
            {
                _order = pending with { Since = DateTimeOffset.UtcNow, Robbed = null };
                _pending = null;
                job = pending;
            }
            else job = _order is { } order && Same(order.Player, player) ? order : null;
        }
        if (job is null) return (false, "");
        if (job.Robbed is { } robbed) Reply(robbed, job.RobbedPrivate, BotResponses.McTakenLines, Who(player));
        return (job.Private, job.Detail);
    }

    // How this player's order went, which ends it: a pending one that failed at once (her previous order
    // goes on), or the one under way.
    private (bool Whispered, string Detail) Finish(string player)
    {
        lock (_gate)
        {
            if (_pending is { } pending && Same(pending.Player, player))
            {
                _pending = null;
                return (pending.Private, pending.Detail);
            }
            if (_order is { } order && Same(order.Player, player))
            {
                _order = null;
                return (order.Private, order.Detail);
            }
            return (false, "");
        }
    }

    private void OnMoving(string player, string kind)
    {
        var (whispered, detail) = Confirm(player);
        switch (kind)
        {
            case "goto":
                Reply(player, whispered, BotResponses.McGoToAckLines, detail);
                break;
            case "guard":
                Reply(player, whispered, Kin(player, BotResponses.McGuardAckLines, BotResponses.McGuardAckLinesOwner, BotResponses.McGuardAckLinesTata));
                break;
            case "follow":
                Reply(player, whispered, Kin(player, BotResponses.McFollowAckLines, BotResponses.McFollowAckLinesOwner, BotResponses.McFollowAckLinesTata));
                break;
            default:
                Reply(player, whispered, Kin(player, BotResponses.McComeAckLines, BotResponses.McComeAckLinesOwner, BotResponses.McComeAckLinesTata));
                break;
        }
    }

    // Papa's or Tata's twin of a pool when the player is one of them (McFamily), the shared pool otherwise.
    private string[] Kin(string player, string[] anyone, string[] papa, string[] tata) =>
        KinOf(player) switch
        {
            McKin.Papa => papa,
            McKin.Tata => tata,
            _ => anyone,
        };

    // How an order went (arrived, out of sight, no way there…): answered to whoever gave it, the way they gave it.
    private void OnOutcome(string player, string[] pool, params object[] extra)
    {
        var (whispered, detail) = Finish(player);
        Reply(player, whispered, pool, extra.Length > 0 ? extra : new object[] { detail });
    }

    // In bed, asked or because a player went to bed: whatever she was doing is over, whoever it was for,
    // except a farm: the body goes back to it when she wakes, and it stays its keeper's.
    private void OnSlept(string player)
    {
        McJob? farm;
        lock (_gate) farm = _order is { Order: McOrder.Farm } kept ? kept : null;
        var (whispered, _) = Confirm(player);
        lock (_gate) _order = farm;
        Reply(player, whispered, BotResponses.McSleepLines);
    }

    // After arriving she walks up to Papa, or else Tata, when either is on: in sight, or where she last saw them.
    private void SeekFamilyLocked(MinecraftBodyProcess body)
    {
        var target = _players.FirstOrDefault(p => KinOf(p) == McKin.Papa) ?? _players.FirstOrDefault(p => KinOf(p) == McKin.Tata);
        if (target is null || _order is not null || _pending is not null) return;
        _seeking = target;
        var spot = _seen.TryGetValue(target, out var s) ? new { x = s.X, y = s.Y, z = s.Z, dimension = s.Dimension } : null;
        body.Send(new { op = "seek", player = target, spot });
    }

    private void OnSeek(string player, bool met)
    {
        lock (_gate)
        {
            if (_seeking is null || !Same(_seeking, player)) return;
            _seeking = null;
            // Logged out while she was on her way: nobody to talk to.
            if (!_players.Contains(player, StringComparer.OrdinalIgnoreCase)) return;
        }
        var kin = KinOf(player);
        if (kin == McKin.Anyone) return;
        Reply(player, false, (kin, met) switch
        {
            (McKin.Papa, true) => BotResponses.McSeekMetLinesOwner,
            (McKin.Papa, false) => BotResponses.McSeekMissLinesOwner,
            (_, true) => BotResponses.McSeekMetLinesTata,
            _ => BotResponses.McSeekMissLinesTata,
        });
    }

    // Where she last saw someone. Only her family's spots are kept, and written straight away.
    private void OnSeen(string player, McSpot spot)
    {
        if (KinOf(player) == McKin.Anyone || spot.Dimension.Length == 0) return;
        MinecraftState state;
        lock (_gate)
        {
            _seen[player] = spot;
            state = StateLocked();
        }
        SaveState(state);
    }

    private void OnArrived(string player, string kind)
    {
        if (kind == "goto") OnOutcome(player, BotResponses.McGoToArrivedLines);
        else OnOutcome(player, BotResponses.McArrivedLines);
    }

    private void OnCollecting(string player, string resource, int count)
    {
        var (whispered, _) = Confirm(player);
        Reply(player, whispered, BotResponses.McCollectAckLines, count, ResourceName(resource));
    }

    private void OnCollected(string player, string resource, int count, bool delivered)
    {
        var pool = count == 0 ? BotResponses.McCollectEmptyLines
            : delivered ? Kin(player, BotResponses.McCollectDoneLines, BotResponses.McCollectDoneLinesOwner, BotResponses.McCollectDoneLinesTata)
            : BotResponses.McCollectKeptLines;
        OnOutcome(player, pool, count, ResourceName(resource));
    }

    private static string CropName(string crop) =>
        Enum.TryParse<McCrop>(crop, ignoreCase: true, out var parsed) && BotResponses.McCropNames.TryGetValue(parsed, out var name)
            ? name
            : crop;

    private static string ResourceName(string resource) =>
        Enum.TryParse<McResource>(resource, ignoreCase: true, out var parsed) && BotResponses.McResourceNames.TryGetValue(parsed, out var name)
            ? name
            : resource;

    /// <summary>
    /// A line to one player: in the chat, or whispered back if they whispered. {0} is the player (Who: Papa and Tata by title);
    /// <paramref name="extra"/> fills {1}, {2}… as the pool documents.
    /// </summary>
    private void Reply(string player, bool whispered, string[] pool, params object[] extra)
    {
        var text = string.Format(_picker.Pick(pool), [Who(player), .. extra]);
        lock (_gate)
            if (_body is { } body && _spawned && !_leaving)
                body.Send(whispered ? new { op = "whisper", player, text } : (object)new { op = "say", text });
    }

    /// <summary>One of her lines in Minecraft's chat, rationed per kind and overall.</summary>
    private void Remark(string kind, string[] pool, string? name = null)
    {
        if (!_kindGate.TryClaim(kind)) return;
        if (!_anyGate.TryClaim(0))
        {
            _kindGate.Release(kind);
            return;
        }
        var text = string.Format(_picker.Pick(pool), name is null ? "" : Who(name));
        lock (_gate)
            if (_body is { } body && _spawned && !_leaving)
                body.Send(new { op = "say", text });
    }

    private async Task PostAsync(string[] pool, string what)
    {
        try
        {
            if (_client.ConnectionState != ConnectionState.Connected) return;
            var channel = await BotsChannel.ResolveAsync(_client, _config, _announcer, HomeGuild.Id);
            if (channel is null) return;
            await BotChat.PostWithTypingAsync(channel, _picker.Pick(pool), _logger, what, AllowedMentions.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to post the {What}.", what);
        }
    }

    // ---- /debug minecraft -----------------------------------------------------------------------

    /// <summary>The state, for the owner. Player names and the kick reason are someone else's text: sanitized.</summary>
    public string StatusText()
    {
        if (!Enabled) return "🔧 Minecraft désactivé : aucune adresse configurée (`minecraft_host`).";
        if (_script is null) return "🔧 Minecraft désactivé : `MinecraftBody/index.js` introuvable.";

        var sb = new StringBuilder();
        lock (_gate)
        {
            sb.AppendLine($"🔧 **Minecraft** — {Code($"{_host}:{_port}")}, en tant que {Code(_username)}"
                          + (_owner.Length > 0 ? $", propriétaire {Code(_owner)}" : ", aucun propriétaire configuré")
                          + (_tata.Length > 0 ? $", Tata {Code(_tata)}" : ""));
            sb.AppendLine(_allowed
                ? "Autorisée à jouer : **oui**"
                : "Autorisée à jouer : **non** — elle surveille le serveur mais n'y entre pas (action « L'autoriser à jouer »)");
            if (_resolved is { } resolved) sb.AppendLine($"Adresse réelle (SRV) : {Code(resolved)}");
            var ping = _lastPingAt is { } at ? $" (vérifié {Stamp(at)})" : "";
            sb.AppendLine(_lastStatus is { } s
                ? $"Serveur : en ligne, {s.Online}/{s.Max} joueurs, {Code(s.Version)}{ping}"
                : $"Serveur : injoignable{ping}");
            if (_version is { } v)
                sb.AppendLine(!v.Fallback
                    ? $"Version : {Code(v.Server)}"
                    : $"Version : le serveur parle {Code(v.Server)}, elle se connecte en {Code(v.Using)} (il faut ViaBackwards sur le serveur)");
            sb.AppendLine(_body is null ? "SYNCS : absente"
                : _leaving ? "SYNCS : en train de partir"
                : _spawned ? $"SYNCS : en jeu depuis {Stamp(_inGameSince!.Value)}"
                : "SYNCS : en connexion…");
            if (_spawned)
                sb.AppendLine(_players.Count == 0 ? "Avec elle : personne"
                    : $"Avec elle ({_players.Count}) : {string.Join(", ", _players.Select(Format.Sanitize))}");
            if (_fewSince is { } few) sb.AppendLine($"Peu de monde depuis {Stamp(few)} (départ après {McPresence.Linger.TotalMinutes:0} min)");
            var now = DateTimeOffset.UtcNow;
            if (_retryAfter > now) sb.AppendLine($"Nouvel essai {Stamp(_retryAfter)} ({_failures} échec(s) d'affilée)");
            if (_holdUntil > now) sb.AppendLine($"Tenue à l'écart jusqu'à {Stamp(_holdUntil)}");
            if (_lastKick is { } kick)
                sb.AppendLine($"Dernière expulsion {Stamp(_lastKickAt!.Value)} : {Format.Sanitize(kick.Length > 300 ? kick[..300] + "…" : kick)}");
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>Allows or forbids her on the server, for good (persisted). Forbidding takes her out now.</summary>
    public string SetAllowed(bool allowed)
    {
        if (!Enabled || _script is null) return StatusText();
        bool inGame;
        MinecraftState state;
        lock (_gate)
        {
            _allowed = allowed;
            inGame = _body is not null;
            state = StateLocked();
        }
        SaveState(state);
        if (allowed) return "🔧 Elle est autorisée à jouer. Elle rejoindra selon sa règle (4 joueurs, ou toi), au prochain passage (une minute au plus).";
        if (inGame) Leave();
        return inGame
            ? "🔧 Elle n'est plus autorisée à jouer : elle quitte le serveur et n'y reviendra pas avant que tu l'autorises."
            : "🔧 Elle n'est plus autorisée à jouer : elle n'ira pas sur le serveur avant que tu l'autorises.";
    }

    /// <summary>Sends her in now, past the rule, the backoff and a forced leave. The rule still applies once she's in.</summary>
    public string ForceJoin()
    {
        if (!Enabled || _script is null) return StatusText();
        lock (_gate)
        {
            if (!_allowed) return "🔧 Elle n'est pas autorisée à jouer : utilise d'abord l'action « L'autoriser à jouer ».";
            if (_body is not null) return "🔧 Elle est déjà sur le serveur (ou en route).";
            _retryAfter = DateTimeOffset.MinValue;
            _holdUntil = DateTimeOffset.MinValue;
            _failures = 0;
        }
        return StartBody()
            ? "🔧 Elle part sur le serveur. Si elle est seule avec 2 joueurs ou moins (et sans toi), elle repartira au bout de 3 minutes."
            : "🔧 Le corps n'a pas pu démarrer : voir les logs.";
    }

    /// <summary>Takes her out now and keeps her out for half an hour.</summary>
    public string ForceLeave()
    {
        lock (_gate) _holdUntil = DateTimeOffset.UtcNow + ForcedLeaveHold;
        bool inGame;
        lock (_gate) inGame = _body is not null;
        if (inGame) Leave();
        return (inGame ? "🔧 Elle quitte le serveur." : "🔧 Elle n'y était pas.")
               + $" Pas de retour avant {Stamp(DateTimeOffset.UtcNow + ForcedLeaveHold)}.";
    }

    private static string Stamp(DateTimeOffset at) => $"<t:{at.ToUnixTimeSeconds()}:R>";

    // Inside a code span nothing is markdown, so Format.Sanitize would show its backslashes: only a
    // backtick can break out of one.
    private static string Code(string text) => $"`{text.Replace('`', '\'')}`";
}
