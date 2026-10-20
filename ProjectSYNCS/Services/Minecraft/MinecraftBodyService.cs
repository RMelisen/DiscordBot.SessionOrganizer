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
    // Each player's hits on her, for the escalation (McHitStreaks). Guarded by _gate.
    private readonly McHitStreaks _hits = new();
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
    // Resume: the activity (farm, fishing, a guarded place) an errand interrupted, which the body goes back to after
    // it and which holds her again once it's done.
    private sealed record McJob(McOrder Order, string Player, bool Private, string Detail, DateTimeOffset Since,
        string? Robbed = null, bool RobbedPrivate = false, McJob? Resume = null);
    private McJob? _order;
    private McJob? _pending;
    // The family member she's walking up to after arriving (McFamily), until the body says how it went.
    private string? _seeking;
    // Where she last saw Papa and Tata, by in-game name. Persisted: they log back in where they logged out.
    private readonly Dictionary<string, McSpot> _seen;
    // The places players asked her to remember, by key (McCommand.PlaceKey). Persisted: the map doesn't change overnight.
    private readonly Dictionary<string, McPlace> _places;
    // Asked where she is / to remember a spot: the body answers with a position, and these say to whom and how.
    private readonly Dictionary<string, bool> _askedWhere = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (string Key, string Name, bool Private)> _marking = new(StringComparer.OrdinalIgnoreCase);
    // Reminders running, per player (McCommand Timer), for this body only: they die with it.
    private readonly Dictionary<string, int> _timers = new(StringComparer.OrdinalIgnoreCase);
    private const int MaxTimers = 3;
    private static readonly TimeSpan MaxTimer = TimeSpan.FromHours(2);
    private static readonly TimeSpan MinTimer = TimeSpan.FromSeconds(10);
    // Fishing remarks, rolled on top of the kind gate: a fish is common, a treasure is not.
    private const double CatchChance = 0.3;
    private const double JunkChance = 0.5;
    // The names a home goes by, tried in this order for « rentre ».
    private static readonly string[] HomeKeys = { "maison", "base", "home", "chez nous", "chez moi", "qg" };
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
        _places = new Dictionary<string, McPlace>(state?.Places ?? new Dictionary<string, McPlace>());
    }

    // What survives a restart: the owner's switch, where she last saw her family, and the places she was asked to
    // remember. Everything else here is in memory on purpose.
    private sealed record MinecraftState(bool Allowed, Dictionary<string, McSpot>? Seen = null, Dictionary<string, McPlace>? Places = null);
    private sealed record McSpot(int X, int Y, int Z, string Dimension);
    // Name: as the player wrote it (McCommand.PlaceName), for her lines. By: who saved it, the only one besides
    // family who may move or forget it.
    private sealed record McPlace(string Name, int X, int Y, int Z, string Dimension, string By);

    private MinecraftState StateLocked() =>
        new(_allowed, new Dictionary<string, McSpot>(_seen), new Dictionary<string, McPlace>(_places));

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
            _hits.Clear();
            ForgetBodyLocked();
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
            _hits.Clear();
            ForgetBodyLocked();
            if (!planned) MarkFailureLocked(DateTimeOffset.UtcNow);
            _logger.LogInformation("Minecraft body exited ({How}).", planned ? "left" : "unplanned");
        }
        if (wasSpawned) _ = PostAsync(BotResponses.McLeavePostLines, "Minecraft leave post");
    }

    // What only made sense for the body that's going: reminders, questions waiting for its answer.
    private void ForgetBodyLocked()
    {
        _timers.Clear();
        _marking.Clear();
        _askedWhere.Clear();
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
                    var unreached = root.GetProperty("player").GetString() ?? "";
                    OnOutcome(unreached, root.GetProperty("why").GetString() switch
                    {
                        "notVisible" => BotResponses.McTooFarLines,
                        "lost" => JobOf(unreached) == McOrder.Guide ? BotResponses.McGuideLostLines : BotResponses.McLostLines,
                        "tooFar" => BotResponses.McTripTooFarLines,
                        "otherDimension" => BotResponses.McPlaceOtherDimensionLines,
                        _ => BotResponses.McNoPathLines,
                    });
                    break;
                case "waiting":
                    Remark("guideWait", BotResponses.McGuideWaitLines, root.GetProperty("player").GetString());
                    break;
                case "creeper":
                    Remark("creeper", BotResponses.McCreeperLines, root.GetProperty("player").GetString());
                    break;
                case "retreat":
                    Remark("retreat", BotResponses.McRetreatLines, root.GetProperty("player").GetString());
                    break;
                case "given":
                    OnGiven(root.GetProperty("player").GetString() ?? "", root.GetProperty("goods").GetString() ?? "",
                        root.GetProperty("count").GetInt32(), root.GetProperty("delivered").GetBoolean());
                    break;
                case "cantGive":
                    var noGoods = Goods(root.GetProperty("goods").GetString() ?? "");
                    OnOutcome(root.GetProperty("player").GetString() ?? "",
                        noGoods == McGoods.All ? BotResponses.McGiveEmptyLines : BotResponses.McGiveNoneLines,
                        "", noGoods is { } g ? BotResponses.McGoodsNames[g] : "");
                    break;
                case "stored":
                    var storer = root.GetProperty("player").GetString() ?? "";
                    Confirm(storer);
                    OnOutcome(storer, root.GetProperty("full").GetBoolean() ? BotResponses.McStoreFullLines : BotResponses.McStoreDoneLines,
                        root.GetProperty("count").GetInt32());
                    break;
                case "cantStore":
                    OnOutcome(root.GetProperty("player").GetString() ?? "",
                        root.GetProperty("why").GetString() == "empty" ? BotResponses.McStoreEmptyLines : BotResponses.McStoreNoChestLines);
                    break;
                case "fishing":
                    var fisher = root.GetProperty("player").GetString() ?? "";
                    Reply(fisher, Confirm(fisher).Whispered, BotResponses.McFishAckLines);
                    break;
                case "cantFish":
                    OnOutcome(root.GetProperty("player").GetString() ?? "", root.GetProperty("why").GetString() switch
                    {
                        "noRod" => BotResponses.McFishNoRodLines,
                        "broke" => BotResponses.McFishRodBrokeLines,
                        _ => BotResponses.McFishNoWaterLines,
                    });
                    break;
                case "caught":
                    var catchKind = root.GetProperty("kind").GetString();
                    if (catchKind == "treasure"
                        || Random.Shared.NextDouble() < (catchKind == "fish" ? CatchChance : JunkChance))
                        Remark("catch", catchKind switch
                        {
                            "treasure" => BotResponses.McFishTreasureLines,
                            "fish" => BotResponses.McFishCatchLines,
                            _ => BotResponses.McFishJunkLines,
                        }, root.GetProperty("player").GetString());
                    break;
                case "patrolling":
                    var keeper = root.GetProperty("player").GetString() ?? "";
                    var (patrolPrivate, patrolDetail) = Confirm(keeper);
                    Reply(keeper, patrolPrivate, patrolDetail.Length > 0 ? BotResponses.McPatrolPlaceAckLines : BotResponses.McPatrolAckLines,
                        patrolDetail);
                    break;
                case "cantPatrol":
                    OnOutcome(root.GetProperty("player").GetString() ?? "",
                        root.GetProperty("why").GetString() == "tooFar" ? BotResponses.McTripTooFarLines : BotResponses.McPlaceOtherDimensionLines);
                    break;
                case "smelting":
                    var cook = root.GetProperty("player").GetString() ?? "";
                    Reply(cook, Confirm(cook).Whispered, BotResponses.McSmeltAckLines, root.GetProperty("count").GetInt32(),
                        SmeltName(root.GetProperty("what").GetString() ?? ""));
                    break;
                case "smelted":
                    var cooked = root.GetProperty("count").GetInt32();
                    OnOutcome(root.GetProperty("player").GetString() ?? "",
                        cooked == 0 ? BotResponses.McSmeltEmptyLines
                        : root.GetProperty("delivered").GetBoolean() ? BotResponses.McSmeltDoneLines
                        : BotResponses.McSmeltKeptLines,
                        cooked, SmeltName(root.GetProperty("what").GetString() ?? ""));
                    break;
                case "cantSmelt":
                    OnOutcome(root.GetProperty("player").GetString() ?? "", root.GetProperty("why").GetString() switch
                    {
                        "noFuel" => BotResponses.McSmeltNoFuelLines,
                        "noFurnace" => BotResponses.McSmeltNoFurnaceLines,
                        "busy" => BotResponses.McSmeltBusyLines,
                        _ => BotResponses.McSmeltNothingLines,
                    }, "", SmeltName(root.GetProperty("what").GetString() ?? ""));
                    break;
                case "position":
                    var asker = root.GetProperty("player").GetString() ?? "";
                    bool whereWhispered;
                    lock (_gate)
                        if (!_askedWhere.Remove(asker, out whereWhispered)) break;
                    var dimension = root.GetProperty("dimension").GetString() ?? "";
                    Reply(asker, whereWhispered, BotResponses.McWhereLines,
                        $"{root.GetProperty("x").GetInt32()} {root.GetProperty("y").GetInt32()} {root.GetProperty("z").GetInt32()}",
                        BotResponses.McDimensionNames[DimensionOf(dimension)]);
                    break;
                case "marked":
                    OnMarked(root.GetProperty("player").GetString() ?? "", root.GetProperty("x").GetInt32(), root.GetProperty("y").GetInt32(),
                        root.GetProperty("z").GetInt32(), root.GetProperty("dimension").GetString() ?? "");
                    break;
                case "cantMark":
                    var unmarked = root.GetProperty("player").GetString() ?? "";
                    bool markWhispered;
                    lock (_gate)
                    {
                        if (!_marking.Remove(unmarked, out var lost)) break;
                        markWhispered = lost.Private;
                    }
                    Reply(unmarked, markWhispered, BotResponses.McTooFarLines);
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
                    // unless it was an activity, which the body goes back to.
                    if (whyNot == "failed") lock (_gate) if (!IsActivity(_order?.Order)) _order = null;
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
                        OnHit(hitter);
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
        string[] known;
        lock (_gate)
        {
            called = _calledAt.Remove(player, out var calledAt) && DateTimeOffset.UtcNow - calledAt <= CallWindow;
            known = _places.Keys.ToArray();
        }
        var request = McCommand.Parse(text, [_username, .. McCommand.HerNames], called, known);
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
        var stops = request.Order is McOrder.Stay or McOrder.Sit;
        if (!stops && !_orderGate.TryClaim(player)) return;
        if (AnswerAtOnce(player, whispered, text, request)) return;

        // A remembered place becomes coordinates here: the body only walks.
        McPlace? place = null;
        if (request.Order is McOrder.GoToPlace or McOrder.Guide || (request.Order == McOrder.Patrol && request.Place is not null))
        {
            place = FindPlace(request.Place ?? "", out var key);
            if (place is null)
            {
                Reply(player, whispered, BotResponses.McPlaceUnknownLines, Label(McCommand.PlaceName(text, key)));
                return;
            }
        }

        object command = request.Order switch
        {
            McOrder.Come => new { op = "come", player },
            McOrder.Follow => new { op = "follow", player },
            McOrder.GoTo => request.Coordinates! is [var x, var z]
                ? new { op = "goto", player, x, y = (int?)null, z }
                : new { op = "goto", player, x = request.Coordinates![0], y = (int?)request.Coordinates[1], z = request.Coordinates[2] },
            McOrder.GoToPlace => new { op = "goto", player, x = place!.X, y = (int?)place.Y, z = place.Z, dimension = place.Dimension },
            McOrder.Guide => new { op = "guide", player, x = place!.X, y = place.Y, z = place.Z, dimension = place.Dimension },
            McOrder.Guard => new { op = "guard", player },
            McOrder.Patrol => new { op = "patrol", player, spot = place is null ? null : new { x = place.X, y = place.Y, z = place.Z, dimension = place.Dimension } },
            McOrder.Collect => new { op = "collect", player, resource = request.Resource!.Value.ToString().ToLowerInvariant(), count = request.Count },
            McOrder.Sleep => new { op = "sleep", player },
            McOrder.Harvest => new { op = "harvest", player, crop = (request.Crop ?? McCrop.Any).ToString().ToLowerInvariant() },
            McOrder.Farm => new { op = "farm", player },
            McOrder.Give => new { op = "give", player, goods = request.Goods!.Value.ToString().ToLowerInvariant() },
            McOrder.Store => new { op = "store", player },
            McOrder.Fish => new { op = "fish", player },
            McOrder.Smelt => new { op = "smelt", player, what = (request.Smelt ?? McSmelt.Food).ToString().ToLowerInvariant() },
            McOrder.Sit => new { op = "sit" },
            _ => new { op = "stay" },
        };
        var detail = request.Coordinates is { } coordinates ? string.Join(" ", coordinates)
            : place is not null ? Label(place.Name)
            : request.Goods is { } goods ? BotResponses.McGoodsNames[goods]
            : "";
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
                // An errand (handing over, putting away) pauses an activity, which the body goes back to after: it
                // holds her again then, and its keeper isn't told she left.
                var resume = request.Order is McOrder.Give or McOrder.Store && _order is { } current && IsActivity(current.Order)
                    ? current
                    : null;
                // Taken from someone before their order was done: they hear why, once the new one is under way.
                robbed = resume is null && _order is { } taken && !Same(taken.Player, player)
                         && (LastingLocked(taken) || now - taken.Since < McClaim.Expiry)
                    ? taken
                    : null;
                _seeking = null;
                if (stops)
                {
                    _order = null;
                    _pending = null;
                }
                else _pending = new McJob(request.Order, player, whispered, detail, now, robbed?.Player, robbed?.Private ?? false, resume);
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
        if (stops)
        {
            if (robbed is not null) Reply(robbed.Player, robbed.Private, BotResponses.McTakenLines, Who(player));
            Reply(player, whispered, request.Order == McOrder.Sit ? BotResponses.McSitLines : BotResponses.McStayAckLines);
        }
    }

    // Orders that take her from nobody and change nothing she's doing: answered here, or by a body event (where she
    // is, a spot to remember). True when the request was one of them.
    private bool AnswerAtOnce(string player, bool whispered, string text, McRequest request)
    {
        switch (request.Order)
        {
            case McOrder.Timer:
                StartTimer(player, whispered, request.Seconds, request.Note);
                return true;
            case McOrder.Jump:
                if (Send(new { op = "jump" })) Reply(player, whispered, BotResponses.McJumpLines);
                return true;
            case McOrder.Wave:
                if (Send(new { op = "wave", player }))
                    Reply(player, whispered, Kin(player, BotResponses.McWaveLines, BotResponses.McWaveLinesOwner, BotResponses.McWaveLinesTata));
                return true;
            case McOrder.Where:
                lock (_gate) _askedWhere[player] = whispered;
                Send(new { op = "where", player });
                return true;
            case McOrder.Give when request.Goods is null:
                Reply(player, whispered, BotResponses.McGiveUnknownLines);
                return true;
            case McOrder.ListPlaces:
                ListPlaces(player, whispered);
                return true;
            case McOrder.WherePlace:
                if (FindPlace(request.Place ?? "", out var whereKey) is { } found)
                    Reply(player, whispered, BotResponses.McPlaceWhereLines, Spot(found.X, found.Y, found.Z, found.Dimension), Label(found.Name));
                else Reply(player, whispered, BotResponses.McPlaceUnknownLines, Label(McCommand.PlaceName(text, whereKey)));
                return true;
            case McOrder.ForgetPlace:
                ForgetPlace(player, whispered, text, request.Place ?? "");
                return true;
            case McOrder.SavePlace:
                if (request.Place is null)
                {
                    Reply(player, whispered, BotResponses.McPlaceNoNameLines);
                    return true;
                }
                lock (_gate) _marking[player] = (request.Place, McCommand.PlaceName(text, request.Place), whispered);
                Send(new { op = "mark", player });
                return true;
            default:
                return false;
        }
    }

    // A command to the body while she's in game. False when she isn't.
    private bool Send(object command)
    {
        lock (_gate)
        {
            if (_body is not { } body || !_spawned || _leaving) return false;
            body.Send(command);
            return true;
        }
    }

    // Farming, fishing and guarding a place last until someone says otherwise; errands and sleep only pause them.
    private static bool IsActivity(McOrder? order) => order is McOrder.Farm or McOrder.Fish or McOrder.Patrol;

    // A farm or a guarded place holds her while its keeper is on the server; every other claim runs out (McClaim.Expiry).
    private bool LastingLocked(McJob job) =>
        job.Order is McOrder.Farm or McOrder.Patrol && _players.Contains(job.Player, StringComparer.OrdinalIgnoreCase);

    // ---- Remembered places ----------------------------------------------------------------------

    // A place by key; an empty key is home (« rentre »), and a home word finds whichever home was saved. Key: what
    // was looked for, for the « I don't know it » line.
    private McPlace? FindPlace(string wanted, out string key)
    {
        lock (_gate)
        {
            key = wanted.Length > 0 ? wanted : HomeKeys[0];
            if (_places.TryGetValue(key, out var exact)) return exact;
            if (wanted.Length > 0 && !HomeKeys.Contains(wanted)) return null;
            foreach (var home in HomeKeys)
                if (_places.TryGetValue(home, out var found)) return found;
            return null;
        }
    }

    // How her lines show a place's name.
    private static string Label(string name) => $"« {name} »";

    // Coordinates for her lines, with the dimension when it isn't the overworld (« 12 40 -8 dans le Nether »).
    private static string Spot(int x, int y, int z, string dimension) =>
        DimensionOf(dimension) is var d && d == "overworld" ? $"{x} {y} {z}" : $"{x} {y} {z} {BotResponses.McDimensionNames[d]}";

    // « overworld », « nether » or « end », from however the server names its dimension (« minecraft:the_nether »).
    private static string DimensionOf(string dimension) =>
        dimension.Contains("nether", StringComparison.OrdinalIgnoreCase) ? "nether"
        : dimension.Contains("end", StringComparison.OrdinalIgnoreCase) ? "end"
        : "overworld";

    // Only whoever saved a place, or family, may move or forget it: offline mode lets anyone take any name, but a stranger
    // shouldn't be able to wipe the group's map.
    private bool MayChange(McPlace place, string player) => Same(place.By, player) || KinOf(player) != McKin.Anyone;

    private void OnMarked(string player, int x, int y, int z, string dimension)
    {
        (string Key, string Name, bool Private) mark;
        McPlace? existing;
        MinecraftState? state = null;
        string[] pool;
        lock (_gate)
        {
            if (!_marking.Remove(player, out mark)) return;
            _places.TryGetValue(mark.Key, out existing);
            if (existing is not null && !MayChange(existing, player)) pool = BotResponses.McPlaceNotYoursLines;
            else if (existing is null && _places.Count >= McCommand.MaxPlaces) pool = BotResponses.McPlaceFullLines;
            else
            {
                _places[mark.Key] = new McPlace(mark.Name, x, y, z, dimension, player);
                state = StateLocked();
                pool = existing is null ? BotResponses.McPlaceSavedLines : BotResponses.McPlaceMovedLines;
            }
        }
        if (state is not null) SaveState(state);
        Reply(player, mark.Private, pool, Label(existing?.Name ?? mark.Name), existing is null ? "" : Who(existing.By));
    }

    private void ForgetPlace(string player, bool whispered, string text, string key)
    {
        McPlace? place;
        MinecraftState? state = null;
        lock (_gate)
        {
            _places.TryGetValue(key, out place);
            if (place is not null && MayChange(place, player))
            {
                _places.Remove(key);
                state = StateLocked();
            }
        }
        if (state is not null) SaveState(state);
        if (place is null) Reply(player, whispered, BotResponses.McPlaceUnknownLines, Label(McCommand.PlaceName(text, key)));
        else if (state is null) Reply(player, whispered, BotResponses.McPlaceNotYoursLines, Label(place.Name), Who(place.By));
        else Reply(player, whispered, BotResponses.McPlaceForgottenLines, Label(place.Name));
    }

    // The places she knows, as many as fit a chat line.
    private void ListPlaces(string player, bool whispered)
    {
        List<string> names;
        lock (_gate) names = _places.Values.Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToList();
        if (names.Count == 0)
        {
            Reply(player, whispered, BotResponses.McPlaceListEmptyLines);
            return;
        }
        var list = new StringBuilder();
        foreach (var name in names)
        {
            var next = (list.Length > 0 ? ", " : "") + name;
            if (list.Length + next.Length > ListMax)
            {
                list.Append('…');
                break;
            }
            list.Append(next);
        }
        Reply(player, whispered, BotResponses.McPlaceListLines, list.ToString());
    }

    // How long the list of places may be in a line, so the longest line still fits Minecraft's 256.
    public const int ListMax = 120;

    // ---- Reminders ------------------------------------------------------------------------------

    private void StartTimer(string player, bool whispered, int seconds, string? note)
    {
        if (TimeSpan.FromSeconds(seconds) > MaxTimer)
        {
            Reply(player, whispered, BotResponses.McTimerTooLongLines);
            return;
        }
        seconds = Math.Max(seconds, (int)MinTimer.TotalSeconds);
        MinecraftBodyProcess? body;
        lock (_gate)
        {
            body = _spawned && !_leaving ? _body : null;
            if (body is null) return;
            var running = _timers.GetValueOrDefault(player);
            if (running >= MaxTimers) body = null;
            else _timers[player] = running + 1;
        }
        if (body is null)
        {
            Reply(player, whispered, BotResponses.McTimerFullLines);
            return;
        }
        Reply(player, whispered, BotResponses.McTimerSetLines, DurationText(seconds));
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds));
            bool fire;
            lock (_gate)
            {
                // Another body since: its timers were cleared, this one died with the old one.
                if (!ReferenceEquals(_body, body)) return;
                if (_timers.TryGetValue(player, out var left))
                {
                    if (left <= 1) _timers.Remove(player);
                    else _timers[player] = left - 1;
                }
                fire = _players.Contains(player, StringComparer.OrdinalIgnoreCase);
            }
            if (!fire) return;
            if (note is null) Reply(player, whispered, BotResponses.McTimerDoneLines, DurationText(seconds));
            else Reply(player, whispered, BotResponses.McTimerNoteLines, note);
        });
    }

    // « 30 secondes », « 1 minute », « 5 minutes », « 1 heure », « 1 h 30 ».
    private static string DurationText(int seconds)
    {
        if (seconds < 60) return $"{seconds} secondes";
        if (seconds < 3600)
        {
            var minutes = (int)Math.Round(seconds / 60.0);
            return minutes == 1 ? "1 minute" : $"{minutes} minutes";
        }
        var hours = seconds / 3600;
        var rest = (seconds % 3600) / 60;
        return rest > 0 ? $"{hours} h {rest:00}" : hours == 1 ? "1 heure" : $"{hours} heures";
    }

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
                // An errand done: the activity it paused holds her again, as the body went back to it.
                _order = order.Resume;
                return (order.Private, order.Detail);
            }
            return (false, "");
        }
    }

    // The order this player gave that's pending or under way, if any.
    private McOrder? JobOf(string player)
    {
        lock (_gate)
            return _pending is { } pending && Same(pending.Player, player) ? pending.Order
                : _order is { } order && Same(order.Player, player) ? order.Order
                : null;
    }

    private void OnMoving(string player, string kind)
    {
        var job = JobOf(player);
        var (whispered, detail) = Confirm(player);
        switch (kind)
        {
            case "goto" when job == McOrder.GoToPlace:
                Reply(player, whispered, BotResponses.McPlaceGoAckLines, detail);
                break;
            case "goto":
                Reply(player, whispered, BotResponses.McGoToAckLines, detail);
                break;
            case "guide":
                Reply(player, whispered, BotResponses.McGuideAckLines, detail);
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
    // except an activity (a farm, fishing, a guarded place): the body goes back to it when she wakes, and it stays
    // its keeper's.
    private void OnSlept(string player)
    {
        McJob? activity;
        lock (_gate) activity = _order is { } kept && IsActivity(kept.Order) ? kept : null;
        var (whispered, _) = Confirm(player);
        lock (_gate) _order = activity;
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
        if (kind == "guide") OnOutcome(player, BotResponses.McGuideArrivedLines);
        else if (kind == "goto" && JobOf(player) == McOrder.GoToPlace) OnOutcome(player, BotResponses.McPlaceArrivedLines);
        else if (kind == "goto") OnOutcome(player, BotResponses.McGoToArrivedLines);
        else OnOutcome(player, BotResponses.McArrivedLines);
    }

    // Handed over (the errand's outcome; the activity it paused is hers again, Finish). {1} = how many, {2} = what.
    private void OnGiven(string player, string goodsKey, int count, bool delivered)
    {
        Confirm(player);
        var goods = Goods(goodsKey) ?? McGoods.All;
        OnOutcome(player,
            !delivered ? BotResponses.McGiveLostLines
            : Kin(player, BotResponses.McGiveDoneLines, BotResponses.McGiveDoneLinesOwner, BotResponses.McGiveDoneLinesTata),
            count, BotResponses.McGoodsNames[goods]);
    }

    private static McGoods? Goods(string key) => Enum.TryParse<McGoods>(key, ignoreCase: true, out var goods) ? goods : null;

    private static string SmeltName(string what) =>
        Enum.TryParse<McSmelt>(what, ignoreCase: true, out var parsed) && BotResponses.McSmeltNames.TryGetValue(parsed, out var name)
            ? name
            : what;

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

    // A player hit her. The first hits get a remark; the one that makes a streak (McHitStreaks: the same
    // player, three times, no more than a minute apart) is when she understands they want to kill her, and
    // that line goes out at once, past the remark gates. Hits after it keep the same lines, rationed again.
    private void OnHit(string hitter)
    {
        McHitStage stage;
        lock (_gate) stage = _hits.Register(hitter, DateTimeOffset.UtcNow);
        if (stage == McHitStage.Hit)
        {
            Remark("hit", Kin(hitter, BotResponses.McHitByPlayerLines, BotResponses.McHitByPlayerLinesOwner,
                BotResponses.McHitByPlayerLinesTata), hitter);
            return;
        }
        Remark("hit", Kin(hitter, BotResponses.McHitAgainLines, BotResponses.McHitAgainLinesOwner, BotResponses.McHitAgainLinesTata),
            hitter, force: stage == McHitStage.Escalated);
    }

    /// <summary>
    /// One of her lines in Minecraft's chat, rationed per kind and overall. <paramref name="force"/> says it
    /// anyway, for the moments that can't wait, and still starts both cooldowns so nothing piles on after.
    /// </summary>
    private void Remark(string kind, string[] pool, string? name = null, bool force = false)
    {
        if (force)
        {
            _kindGate.TryClaim(kind);
            _anyGate.TryClaim(0);
        }
        else if (!_kindGate.TryClaim(kind)) return;
        else if (!_anyGate.TryClaim(0))
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
