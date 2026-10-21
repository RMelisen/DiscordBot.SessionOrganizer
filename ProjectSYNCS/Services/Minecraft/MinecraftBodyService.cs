using System.Globalization;
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

    // Her in-game remarks: one per kind every 2 minutes, and never two within 20 s of each other. Two gates,
    // two policies; neither is shared with anything else.
    private readonly CooldownGate<string> _kindGate = new(TimeSpan.FromMinutes(2));
    private readonly CooldownGate<int> _anyGate = new(TimeSpan.FromSeconds(20));
    // And at most 4 of them in any 10 minutes, so the chat stays the players'. Forced remarks (a creeper
    // behind someone, drowning) are neither counted nor held back. When each counted one was said; guarded by _gate.
    private const int RemarkBudget = 4;
    private static readonly TimeSpan RemarkBudgetWindow = TimeSpan.FromMinutes(10);
    private readonly Queue<DateTimeOffset> _remarksSaid = new();
    // Her lines come from small pools heard by the same few players in one evening: she picks among the 2 she said
    // least recently, so a line comes back only once the pool has nearly gone round.
    private const int LineChoices = 2;
    // Orders are answered, not rationed like remarks: only a player sending a second one within the same
    // second is ignored (a double send), never a stop, and her « oui ? » to a bare call is said to the same
    // player once every 10 seconds at most.
    private readonly CooldownGate<string> _orderGate = new(TimeSpan.FromSeconds(1), TimeSpan.FromHours(1));
    private readonly CooldownGate<string> _callGate = new(TimeSpan.FromSeconds(10), TimeSpan.FromHours(1));
    // But the same order from the same player (and to the same place, the same resource…) is acknowledged once every
    // 3 minutes: sent again, she does it without saying the same thing again. Refusals and outcomes are never held back.
    private readonly CooldownGate<string> _ackGate = new(TimeSpan.FromMinutes(3), TimeSpan.FromHours(1));
    // A hello, a goodbye, a « gg », a « merci » or a bare call from several players at once: the first gets her
    // words, the others within 30 s only her gesture, unless they're Papa or Tata. One gate per kind, across players.
    private readonly CooldownGate<McOrder> _burstGate = new(TimeSpan.FromSeconds(30));
    // « Je suis avec X » to a player she's refusing, once every 10 seconds at most.
    private readonly CooldownGate<string> _busyGate = new(TimeSpan.FromSeconds(10), TimeSpan.FromHours(1));
    // Asking for food when she has none: every 10 minutes at most, every 5 once she's too hungry to sprint.
    private readonly CooldownGate<int> _hungryGate = new(TimeSpan.FromMinutes(10));
    private readonly CooldownGate<int> _starvingGate = new(TimeSpan.FromMinutes(5));
    // Minecraft stops sprinting at 6 food points and lower: she can't run away any more.
    private const int StarvingFood = 6;
    // After her hello, before she walks up to Papa or Tata: her player list has arrived by then.
    private static readonly TimeSpan SeekPause = TimeSpan.FromSeconds(2);
    // After a bare call (« syncs »), that player's next message within this long is read as if it carried
    // her name (« suis moi »). One message only, whatever it turns out to be. Guarded by _gate.
    private static readonly TimeSpan CallWindow = TimeSpan.FromSeconds(30);
    private readonly Dictionary<string, DateTimeOffset> _calledAt = new();
    // After she answered a player in chat, within CallWindow: what they say without her name is still heard as talk
    // (an intent, Papa's pet name), however many messages they send meanwhile to someone else. Orders keep the
    // one-message _calledAt above. Guarded by _gate.
    private readonly Dictionary<string, DateTimeOffset> _answeredAt = new();
    // Each player's back-and-forth with her in chat (ConversationStreak, as on Discord): she notices the third message
    // she can only shrug at, ends it at the fifth and stays quiet with them for 10 minutes. Never Papa.
    private readonly ConversationStreak _talks = new();
    // Talked about rather than to: a word now and then, on top of the remark gates.
    private const double TalkedAboutChance = 0.5;
    // Each player's hits on her, for the escalation (McHitStreaks). Guarded by _gate.
    private readonly McHitStreaks _hits = new();
    // Each player's hits on Papa or Tata (a word, then one hit back, then a fight), and whom she's hitting back and
    // until when: their hits on her then are the fight, not a reason to complain or run. Guarded by _gate.
    private readonly McHitStreaks _familyHits = new();
    private readonly Dictionary<string, (DateTimeOffset Until, bool Fighting)> _defending = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan DefendHold = TimeSpan.FromSeconds(30);
    private const double NightChance = 0.25;
    private const double AdvancementChance = 0.3;
    // The other commonest remarks, rolled on top of the gates so the chat stays the players': an ordinary death
    // (hers, a player's kill and family's always get a word), a first hit from anyone but family (rare enough to keep
    // its odds), a guard kill.
    private const double DeathChance = 0.3;
    private const double HitChance = 0.6;
    private const double KillChance = 0.1;
    // Answers whose gesture already says it: she got where she was called, she hops.
    private const double ArrivedChance = 0.25;
    private const double JumpChance = 0.5;
    // What she starts on her own gets a rare word (the owner's call); getting her things back always does, and how it
    // went half the time.
    private const double RoutineChance = 0.25;
    private const double TagAlongChance = 0.2;
    private const double RetrievedChance = 0.5;
    private const double RetrieveLostChance = 0.5;
    // In the water: diving after someone, coming up for air, breathing again after nearly drowning, climbing into
    // someone's boat, shaking herself off. Rolled; running out of air is said at once (it can't wait).
    private const double DiveChance = 0.3;
    private const double SurfacingChance = 0.5;
    private const double BreathingChance = 0.5;
    private const double BoardChance = 0.5;
    private const double ShakeChance = 0.2;
    // A creeper behind someone: one warning per player every 15 seconds, past the remark gates (it can't wait).
    private readonly CooldownGate<string> _creeperGate = new(TimeSpan.FromSeconds(15), TimeSpan.FromHours(1));
    // A worn or broken tool, or something she lacks: once every 10 minutes per thing.
    private readonly CooldownGate<string> _needGate = new(TimeSpan.FromMinutes(10));

    private readonly DiscordSocketClient _client;
    private readonly ResponsePicker _picker;
    private readonly PiHealthService _health;
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
    // Her lines waiting to be « typed » (SayLocked), one after another.
    private Task _speech = Task.CompletedTask;
    private DateTimeOffset? _inGameSince;
    private List<string> _players = new();
    // What she carries, as the body last reported it (on spawn, then after each change), and when. Null until then.
    private McInventory? _inventory;
    private DateTimeOffset _inventoryAt;
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
    // The places players asked her to remember: whose they are (in-game name), then by key (McCommand.PlaceKey). Each
    // player has their own « maison ». Persisted: the map doesn't change overnight.
    private readonly Dictionary<string, Dictionary<string, McPlace>> _places;
    // Asked where she is / to remember a spot: the body answers with a position, and these say to whom and how.
    // Of: whose place it will be (« retiens ici : maison de Rodhengard »), null for the player's own.
    private readonly Dictionary<string, bool> _askedWhere = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (string Key, string Name, string? Of, bool Private)> _marking = new(StringComparer.OrdinalIgnoreCase);
    // Reminders running, per player (McCommand Timer), for this body only: they die with it.
    private readonly Dictionary<string, int> _timers = new(StringComparer.OrdinalIgnoreCase);
    private const int MaxTimers = 3;
    private static readonly TimeSpan MaxTimer = TimeSpan.FromHours(2);
    private static readonly TimeSpan MinTimer = TimeSpan.FromSeconds(10);
    // Fishing remarks, rolled on top of the kind gate: a fish is common, a treasure is not.
    private const double CatchChance = 0.15;
    private const double JunkChance = 0.25;
    private (string Server, string Using, bool Fallback)? _version;
    private string? _lastKick;
    private DateTimeOffset? _lastKickAt;

    public MinecraftBodyService(DiscordSocketClient client, ResponsePicker picker, PiHealthService health, IConfiguration configuration,
        ILogger<MinecraftBodyService> logger)
    {
        _client = client;
        _picker = picker;
        _health = health;
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
        _places = new Dictionary<string, Dictionary<string, McPlace>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (player, places) in state?.PlacesByPlayer ?? new Dictionary<string, Dictionary<string, McPlace>>())
            _places[player] = new Dictionary<string, McPlace>(places);
        // A file from when places were one shared map: each goes to whoever saved it.
        foreach (var (key, place) in state?.Places ?? new Dictionary<string, McPlace>())
        {
            if (!_places.TryGetValue(place.By, out var theirs)) _places[place.By] = theirs = new Dictionary<string, McPlace>();
            theirs.TryAdd(key, place);
        }
    }

    // What survives a restart: the owner's switch, where she last saw her family, and the places she was asked to
    // remember, by player. Everything else here is in memory on purpose. Places: the old shared map, only ever read.
    private sealed record MinecraftState(bool Allowed, Dictionary<string, McSpot>? Seen = null, Dictionary<string, McPlace>? Places = null,
        Dictionary<string, Dictionary<string, McPlace>>? PlacesByPlayer = null);
    private sealed record McSpot(int X, int Y, int Z, string Dimension);
    // Name: as the player wrote it (McCommand.PlaceName), for her lines. By: who saved it, who may move or forget it
    // besides the player it belongs to and family.
    private sealed record McPlace(string Name, int X, int Y, int Z, string Dimension, string By);

    private MinecraftState StateLocked() =>
        new(_allowed, new Dictionary<string, McSpot>(_seen),
            PlacesByPlayer: _places.Where(p => p.Value.Count > 0).ToDictionary(p => p.Key, p => new Dictionary<string, McPlace>(p.Value)));

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
            _inventory = null;
            _fewSince = null;
            _order = null;
            _pending = null;
            _seeking = null;
            _hits.Clear();
            _familyHits.Clear();
            _defending.Clear();
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
            if (_spawned) body.Send(new { op = "say", text = _picker.Pick(BotResponses.McLeavingLines, LineChoices) });
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
        lock (_gate)
        {
            if (!ReferenceEquals(_body, body)) return;
            var planned = _leaving;
            _body = null;
            _spawned = false;
            _leaving = false;
            _inGameSince = null;
            _players = new List<string>();
            _inventory = null;
            _fewSince = null;
            _order = null;
            _pending = null;
            _seeking = null;
            _hits.Clear();
            _familyHits.Clear();
            _defending.Clear();
            ForgetBodyLocked();
            if (!planned) MarkFailureLocked(DateTimeOffset.UtcNow);
            _logger.LogInformation("Minecraft body exited ({How}).", planned ? "left" : "unplanned");
        }
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
                    Ack(fisher, Confirm(fisher).Whispered, "fish", BotResponses.McFishAckLines);
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
                    Ack(keeper, patrolPrivate, $"patrol:{patrolDetail}",
                        patrolDetail.Length > 0 ? BotResponses.McPatrolPlaceAckLines : BotResponses.McPatrolAckLines, patrolDetail);
                    break;
                case "cantPatrol":
                    OnOutcome(root.GetProperty("player").GetString() ?? "",
                        root.GetProperty("why").GetString() == "tooFar" ? BotResponses.McTripTooFarLines : BotResponses.McPlaceOtherDimensionLines);
                    break;
                case "smelting":
                    var cook = root.GetProperty("player").GetString() ?? "";
                    var smelting = root.GetProperty("what").GetString() ?? "";
                    Ack(cook, Confirm(cook).Whispered, $"smelt:{smelting}", BotResponses.McSmeltAckLines, root.GetProperty("count").GetInt32(),
                        SmeltName(smelting));
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
                case "inventory":
                    var inventory = ReadInventory(root);
                    lock (_gate)
                    {
                        _inventory = inventory;
                        _inventoryAt = DateTimeOffset.UtcNow;
                    }
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
                    if (Random.Shared.NextDouble() < KillChance)
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
                    OnSlept(root.GetProperty("player").GetString() ?? "",
                        root.TryGetProperty("auto", out var auto) && auto.ValueKind == JsonValueKind.True);
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
                    var harvesting = root.GetProperty("crop").GetString() ?? "";
                    Ack(root.GetProperty("player").GetString() ?? "", harvestPrivate, $"harvest:{harvesting}", BotResponses.McHarvestAckLines,
                        root.GetProperty("count").GetInt32(), CropName(harvesting));
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
                    Ack(farmer, Confirm(farmer).Whispered, "farm", BotResponses.McFarmAckLines);
                    break;
                case "cantFarm":
                    OnOutcome(root.GetProperty("player").GetString() ?? "",
                        root.GetProperty("why").GetString() == "noChest" ? BotResponses.McFarmNoChestLines : BotResponses.McFarmNoneLines);
                    break;
                case "ranching":
                    var rancher = root.GetProperty("player").GetString() ?? "";
                    Ack(rancher, Confirm(rancher).Whispered, "ranch", BotResponses.McRanchAckLines);
                    break;
                case "cantRanch":
                    OnOutcome(root.GetProperty("player").GetString() ?? "",
                        root.GetProperty("why").GetString() == "noChest" ? BotResponses.McRanchNoChestLines : BotResponses.McRanchNoneLines);
                    break;
                case "routine":
                    if (RoutinePool(root.GetProperty("what").GetString()) is { } routinePool && Random.Shared.NextDouble() < RoutineChance)
                        Remark("routine", routinePool);
                    break;
                case "tagAlong":
                    OnTagAlong(root.GetProperty("player").GetString() ?? "");
                    break;
                case "retrieving":
                    Remark("retrieving", BotResponses.McRetrieveLines, force: true);
                    break;
                case "retrieved":
                    // Nothing left where she died always gets a word; what she got back, half the time.
                    var retrieved = root.GetProperty("count").GetInt32();
                    if (retrieved == 0) Remark("retrieved", BotResponses.McRetrieveEmptyLines, force: true);
                    else if (Random.Shared.NextDouble() < RetrievedChance) Remark("retrieved", BotResponses.McRetrievedLines, null, false, retrieved);
                    break;
                case "retrieveLost":
                    if (Random.Shared.NextDouble() < RetrieveLostChance) Remark("retrieved", BotResponses.McRetrieveLostLines);
                    break;
                case "creeperBehind":
                    var exposed = root.GetProperty("player").GetString() ?? "";
                    if (exposed.Length > 0 && _creeperGate.TryClaim(exposed))
                        Remark("creeperBehind", Kin(exposed, BotResponses.McCreeperBehindLines, BotResponses.McCreeperBehindLinesOwner,
                            BotResponses.McCreeperBehindLinesTata), exposed, force: true);
                    break;
                case "wornOut":
                    var wornTool = root.GetProperty("tool").GetString() ?? "";
                    OnNeed($"worn:{wornTool}", BotResponses.McToolWornLines, ToolName(wornTool));
                    break;
                case "broke":
                    var brokenTool = root.GetProperty("tool").GetString() ?? "";
                    OnNeed($"broke:{brokenTool}", BotResponses.McToolBrokeLines, ToolName(brokenTool));
                    break;
                case "lacking":
                    var lacked = root.GetProperty("what").GetString() ?? "";
                    if (NeedPool(lacked) is { } needPool) OnNeed(lacked, needPool);
                    break;
                case "diving":
                    var diver = root.GetProperty("player").GetString() ?? "";
                    if (Random.Shared.NextDouble() < DiveChance)
                        Remark("dive", Kin(diver, BotResponses.McDiveLines, BotResponses.McDiveLinesOwner, BotResponses.McDiveLinesTata), diver);
                    break;
                case "surfacing":
                    if (Random.Shared.NextDouble() < SurfacingChance) Remark("surfacing", BotResponses.McDiveAirLines);
                    break;
                case "drowning":
                    Remark("drowning", BotResponses.McDrowningLines, force: true);
                    break;
                case "breathing":
                    if (Random.Shared.NextDouble() < BreathingChance) Remark("breathing", BotResponses.McBreathingLines);
                    break;
                case "boarded":
                    var skipper = root.GetProperty("player").GetString() ?? "";
                    if (Random.Shared.NextDouble() < BoardChance)
                        Remark("boarded", Kin(skipper, BotResponses.McBoardLines, BotResponses.McBoardLinesOwner, BotResponses.McBoardLinesTata), skipper);
                    break;
                case "shook":
                    if (Random.Shared.NextDouble() < ShakeChance) Remark("shook", BotResponses.McShakeLines);
                    break;
                case "hungry":
                    OnHungry(root.GetProperty("food").GetInt32());
                    break;
                case "fed":
                    var feeder = root.GetProperty("by").GetString() ?? "";
                    if (feeder.Length > 0)
                        Remark("fed", Kin(feeder, BotResponses.McFedLines, BotResponses.McFedLinesOwner, BotResponses.McFedLinesTata), feeder,
                            force: true);
                    break;
                case "safe":
                    Remark("safe", BotResponses.McFleeSafeLines, root.GetProperty("player").GetString());
                    break;
                case "familyHurt":
                    OnFamilyHurt(root.GetProperty("victim").GetString() ?? "", root.GetProperty("by").GetString() ?? "");
                    break;
                case "defending":
                    OnDefending(root.GetProperty("player").GetString() ?? "", root.GetProperty("victim").GetString() ?? "",
                        root.GetProperty("swat").GetBoolean());
                    break;
                case "cantDefend":
                    // Busy with someone's order: words only.
                    Remark("familyHit", BotResponses.McFamilyHitLines, root.GetProperty("player").GetString(), false,
                        Who(root.GetProperty("victim").GetString() ?? ""));
                    break;
                case "cornered":
                    // Nowhere to run: said at once, like the escalation it follows.
                    Remark("cornered", BotResponses.McCorneredLines, root.GetProperty("player").GetString(), force: true);
                    break;
                case "dropped":
                    OnDropped(root.GetProperty("player").GetString() ?? "", root.GetProperty("by").GetString() ?? "");
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
            // Whose hits she watches for (Defending her family). Names only: offline mode lets anyone take one.
            body.Send(new { op = "family", names = new[] { _owner, _tata }.Where(n => n.Length > 0).ToArray() });
            SendRoutineLocked(body);
        }
        _logger.LogInformation("Minecraft body spawned on {Host}:{Port}.", _host, _port);
        _ = Task.Run(async () =>
        {
            await Task.Delay(ArrivalPause);
            lock (_gate)
                if (ReferenceEquals(_body, body) && !_leaving)
                {
                    var text = _picker.Pick(BotResponses.McArrivalLines, LineChoices);
                    SayLocked(body, new { op = "say", text }, text);
                }
            await Task.Delay(SeekPause);
            lock (_gate)
                if (ReferenceEquals(_body, body) && !_leaving)
                    SeekFamilyLocked(body);
        });
    }

    private void OnSystem(string key, IReadOnlyList<McArg> args)
    {
        List<string> players;
        lock (_gate) players = _players;

        if (McDeath.Classify(key, args, players, _username) is { } death)
        {
            var pool = death.Kind == McDeathKind.Self ? BotResponses.McDeathLines[death.Kind]
                : Kin(death.Victim, BotResponses.McDeathLines[death.Kind], BotResponses.McDeathLinesOwner, BotResponses.McDeathLinesTata);
            var ordinary = death.Kind is not (McDeathKind.Self or McDeathKind.Player) && KinOf(death.Victim) == McKin.Anyone;
            if (!ordinary || Random.Shared.NextDouble() < DeathChance) Remark($"death:{death.Kind}", pool, death.Victim);
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
        bool answered;
        string[] known;
        Dictionary<string, string> owners;
        lock (_gate)
        {
            called = _calledAt.Remove(player, out var calledAt) && DateTimeOffset.UtcNow - calledAt <= CallWindow;
            answered = _answeredAt.TryGetValue(player, out var answeredAt) && DateTimeOffset.UtcNow - answeredAt <= CallWindow;
            if (!answered) _answeredAt.Remove(player);
            known = _places.Values.SelectMany(p => p.Keys).Distinct().ToArray();
            owners = OwnerNamesLocked();
        }
        var request = McCommand.Parse(text, [_username, .. McCommand.HerNames], called, known, owners.Keys);
        if (request.Of is { } named) request = request with { Of = owners[named] };
        switch (request.Order)
        {
            case McOrder.None:
                // Not an order: maybe talk (to her, about her, or right after she answered them). A refused order stays silent.
                if (!request.Refused) Talk(player, whispered, text, call: false, listening: called || answered);
                return;
            case McOrder.Unknown:
                // Her name and something she reads as talk (« syncs ça va ? »): answered as such. Otherwise a call.
                if (Talk(player, whispered, text, call: true, listening: false)) return;
                lock (_gate) _calledAt[player] = DateTimeOffset.UtcNow;
                if (!_callGate.TryClaim(player)) return;
                // Called by several players at once: the others get a wave, so they still know she heard (the window opens).
                if (FirstOfBurst(player, McOrder.Unknown))
                    Reply(player, whispered, Kin(player, BotResponses.McCalledLines, BotResponses.McCalledLinesOwner, BotResponses.McCalledLinesTata));
                else Send(new { op = "wave", player });
                return;
        }
        var stops = request.Order is McOrder.Stay or McOrder.Sit;
        if (!stops && !_orderGate.TryClaim(player)) return;
        if (AnswerAtOnce(player, whispered, text, request)) return;

        // A remembered place becomes coordinates here: the body only walks.
        McPlace? place = null;
        var placeOwner = player;
        if (request.Order is McOrder.GoToPlace or McOrder.Guide || (request.Order == McOrder.Patrol && request.Place is not null))
        {
            if (FindPlace(player, request.Place ?? "", request.Of, out var key) is not { } found)
            {
                Reply(player, whispered, BotResponses.McPlaceUnknownLines, Label(McCommand.PlaceName(text, key), request.Of ?? player, player));
                return;
            }
            (place, placeOwner) = found;
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
            McOrder.Ranch => new { op = "ranch", player },
            McOrder.Give => new { op = "give", player, goods = request.Goods!.Value.ToString().ToLowerInvariant() },
            McOrder.Store => new { op = "store", player },
            McOrder.Fish => new { op = "fish", player },
            McOrder.Smelt => new { op = "smelt", player, what = (request.Smelt ?? McSmelt.Food).ToString().ToLowerInvariant() },
            McOrder.Sit => new { op = "sit" },
            _ => new { op = "stay" },
        };
        var detail = request.Coordinates is { } coordinates ? string.Join(" ", coordinates)
            : place is not null ? Label(place.Name, placeOwner, player)
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
            Ack(player, whispered, request.Order.ToString(), request.Order == McOrder.Sit ? BotResponses.McSitLines : BotResponses.McStayAckLines);
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
                if (Send(new { op = "jump" }) && Random.Shared.NextDouble() < JumpChance) Reply(player, whispered, BotResponses.McJumpLines);
                return true;
            case McOrder.Wave:
                if (Send(new { op = "wave", player }) && FirstOfBurst(player, McOrder.Wave))
                    Reply(player, whispered, Kin(player, BotResponses.McWaveLines, BotResponses.McWaveLinesOwner, BotResponses.McWaveLinesTata));
                return true;
            case McOrder.Bye:
                if (Send(new { op = "wave", player }) && FirstOfBurst(player, McOrder.Bye))
                    Reply(player, whispered, Kin(player, BotResponses.McByeLines, BotResponses.McByeLinesOwner, BotResponses.McByeLinesTata));
                return true;
            case McOrder.Praise:
                if (Send(new { op = "jump" }) && FirstOfBurst(player, McOrder.Praise))
                    Reply(player, whispered, Kin(player, BotResponses.McPraisedLines, BotResponses.McPraisedLinesOwner, BotResponses.McPraisedLinesTata));
                return true;
            case McOrder.Thanks:
                // A thank-you has no gesture of its own: the others get the wave.
                if (FirstOfBurst(player, McOrder.Thanks))
                    Reply(player, whispered, Kin(player, BotResponses.McThankedLines, BotResponses.McThankedLinesOwner, BotResponses.McThankedLinesTata));
                else Send(new { op = "wave", player });
                return true;
            case McOrder.Where:
                lock (_gate) _askedWhere[player] = whispered;
                Send(new { op = "where", player });
                return true;
            case McOrder.Give when request.Goods is null:
                Reply(player, whispered, BotResponses.McGiveUnknownLines);
                return true;
            case McOrder.ListPlaces:
                ListPlaces(player, whispered, request.Of);
                return true;
            case McOrder.Inventory:
            case McOrder.Have:
                TellInventory(player, whispered, request.Goods ?? McGoods.All);
                return true;
            case McOrder.WherePlace:
                if (FindPlace(player, request.Place ?? "", request.Of, out var whereKey) is var (found, owner))
                    Reply(player, whispered, BotResponses.McPlaceWhereLines, Spot(found.X, found.Y, found.Z, found.Dimension),
                        Label(found.Name, owner, player));
                else Reply(player, whispered, BotResponses.McPlaceUnknownLines, Label(McCommand.PlaceName(text, whereKey), request.Of ?? player, player));
                return true;
            case McOrder.ForgetPlace:
                ForgetPlace(player, whispered, text, request.Place ?? "", request.Of);
                return true;
            case McOrder.SavePlace:
                if (request.Place is null)
                {
                    Reply(player, whispered, BotResponses.McPlaceNoNameLines);
                    return true;
                }
                lock (_gate) _marking[player] = (request.Place, McCommand.PlaceName(text, request.Place), request.Of, whispered);
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

    // Farming, fishing, keeping animals and guarding a place last until someone says otherwise; errands and sleep only
    // pause them.
    private static bool IsActivity(McOrder? order) => order is McOrder.Farm or McOrder.Fish or McOrder.Patrol or McOrder.Ranch;

    // A farm, a pen or a guarded place holds her while its keeper is on the server; every other claim runs out
    // (McClaim.Expiry).
    private bool LastingLocked(McJob job) =>
        job.Order is McOrder.Farm or McOrder.Patrol or McOrder.Ranch && _players.Contains(job.Player, StringComparer.OrdinalIgnoreCase);

    // ---- Remembered places ----------------------------------------------------------------------

    // Whose places are looked through, in order, for a place named without its player: the player's own first (each
    // has their own « maison »), then Papa's, Tata's and everyone else's by name, so the group's map still works for
    // someone who saved nothing. Without a player (her own trips, « rentre »): the family's first.
    private List<string> SearchOrderLocked(string? player)
    {
        var order = new List<string>();
        void Add(string who)
        {
            if (_places.ContainsKey(who) && !order.Contains(who, StringComparer.OrdinalIgnoreCase)) order.Add(who);
        }
        if (player is not null) Add(player);
        Add(_owner);
        Add(_tata);
        foreach (var who in _places.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase)) Add(who);
        return order;
    }

    // Whose places may be named in chat (« la maison de Rodhengard »), by the name the parser looks for: everyone with a
    // place or on the server, and « Papa » and « Tata », as she calls them.
    private Dictionary<string, string> OwnerNamesLocked()
    {
        var owners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var who in _places.Keys.Concat(_players).Where(w => w.Length > 0 && !Same(w, _username))) owners.TryAdd(who, who);
        if (_owner.Length > 0) owners.TryAdd("Papa", _owner);
        if (_tata.Length > 0) owners.TryAdd("Tata", _tata);
        return owners;
    }

    // A place by key, and whose it is; an empty key is home (« rentre »: hers, the family's), and a home word finds
    // whichever home that player saved. Of: the player named (« la maison de Rodhengard »), only theirs then. Key: what
    // was looked for, for the « I don't know it » line.
    private (McPlace Place, string Owner)? FindPlace(string player, string wanted, string? of, out string key)
    {
        lock (_gate)
        {
            key = wanted.Length > 0 ? wanted : McRoutine.HomeKeys[0];
            var home = wanted.Length == 0 || McRoutine.HomeKeys.Contains(wanted);
            foreach (var owner in of is not null ? [of] : SearchOrderLocked(wanted.Length > 0 ? player : null))
            {
                if (!_places.TryGetValue(owner, out var theirs)) continue;
                if (theirs.TryGetValue(key, out var exact)) return (exact, owner);
                if (home)
                    foreach (var homeKey in McRoutine.HomeKeys)
                        if (theirs.TryGetValue(homeKey, out var found)) return (found, owner);
            }
            return null;
        }
    }

    // The places her routine is built on (McRoutine), as spots: sent on spawn and whenever a place is saved, moved or
    // forgotten. The body decides when to go where.
    private void SendRoutine()
    {
        lock (_gate)
            if (_body is { } body && _spawned && !_leaving)
                SendRoutineLocked(body);
    }

    // Her own day is built on her family's map first: each kind of place is Papa's, else Tata's, else anyone's.
    private void SendRoutineLocked(MinecraftBodyProcess body)
    {
        McPlace? farm = null, fish = null, ranch = null, home = null;
        foreach (var owner in SearchOrderLocked(null))
        {
            var theirs = _places[owner];
            var kinds = McRoutine.Classify(theirs.Keys);
            farm ??= kinds.Farm is { } f ? theirs[f] : null;
            fish ??= kinds.Fish is { } fi ? theirs[fi] : null;
            ranch ??= kinds.Ranch is { } r ? theirs[r] : null;
            home ??= kinds.Home is { } h ? theirs[h] : null;
        }
        object? At(McPlace? p) => p is null ? null : new { x = p.X, y = p.Y, z = p.Z, dimension = p.Dimension };
        body.Send(new { op = "routine", farm = At(farm), fish = At(fish), ranch = At(ranch), home = At(home) });
    }

    // How her lines show a place's name.
    private static string Label(string name) => $"« {name} »";

    // A place in her lines to this player: with whose it is, unless it's theirs (« « maison » de Rodhengard »).
    private string Label(string name, string owner, string player) =>
        Same(owner, player) ? Label(name) : $"{Label(name)} {OfWhom(owner)}";

    // « de Papa », « d'Ethan ».
    private string OfWhom(string owner) =>
        Who(owner) is var who && who.Length > 0 && "aeiouyAEIOUY".Contains(who[0]) ? $"d'{who}" : $"de {who}";

    // Coordinates for her lines, with the dimension when it isn't the overworld (« 12 40 -8 dans le Nether »).
    private static string Spot(int x, int y, int z, string dimension) =>
        DimensionOf(dimension) is var d && d == "overworld" ? $"{x} {y} {z}" : $"{x} {y} {z} {BotResponses.McDimensionNames[d]}";

    // « overworld », « nether » or « end », from however the server names its dimension (« minecraft:the_nether »).
    private static string DimensionOf(string dimension) =>
        dimension.Contains("nether", StringComparison.OrdinalIgnoreCase) ? "nether"
        : dimension.Contains("end", StringComparison.OrdinalIgnoreCase) ? "end"
        : "overworld";

    // A place may be moved or forgotten by the player it belongs to, whoever saved it, or family: offline mode lets anyone
    // take any name, but a stranger shouldn't be able to wipe the group's map. Adding a place to someone else's map is
    // open, like any first save.
    private bool MayChange(McPlace place, string owner, string player) =>
        Same(owner, player) || Same(place.By, player) || KinOf(player) != McKin.Anyone;

    private void OnMarked(string player, int x, int y, int z, string dimension)
    {
        (string Key, string Name, string? Of, bool Private) mark;
        McPlace? existing = null;
        MinecraftState? state = null;
        string[] pool;
        string owner;
        lock (_gate)
        {
            if (!_marking.Remove(player, out mark)) return;
            owner = mark.Of ?? player;
            _places.TryGetValue(owner, out var theirs);
            theirs?.TryGetValue(mark.Key, out existing);
            if (existing is not null && !MayChange(existing, owner, player)) pool = BotResponses.McPlaceNotYoursLines;
            else if (existing is null && theirs?.Count >= McCommand.MaxPlaces) pool = BotResponses.McPlaceFullLines;
            else
            {
                if (theirs is null) _places[owner] = theirs = new Dictionary<string, McPlace>();
                theirs[mark.Key] = new McPlace(mark.Name, x, y, z, dimension, player);
                state = StateLocked();
                pool = existing is null ? BotResponses.McPlaceSavedLines : BotResponses.McPlaceMovedLines;
            }
        }
        if (state is not null)
        {
            SaveState(state);
            SendRoutine();
        }
        Reply(player, mark.Private, pool, Label(existing?.Name ?? mark.Name, owner, player), existing is null ? "" : Who(existing.By));
    }

    // Forgetting: the place by its exact name, the player's own first like any other (« oublie la mine » finds Papa's
    // when they have none, and only family may then forget it).
    private void ForgetPlace(string player, bool whispered, string text, string key, string? of)
    {
        McPlace? place = null;
        var owner = of ?? player;
        MinecraftState? state = null;
        lock (_gate)
        {
            foreach (var who in of is not null ? [of] : SearchOrderLocked(player))
            {
                if (!_places.TryGetValue(who, out var theirs) || !theirs.TryGetValue(key, out place)) continue;
                owner = who;
                if (MayChange(place, owner, player))
                {
                    theirs.Remove(key);
                    if (theirs.Count == 0) _places.Remove(who);
                    state = StateLocked();
                }
                break;
            }
        }
        if (state is not null)
        {
            SaveState(state);
            SendRoutine();
        }
        if (place is null) Reply(player, whispered, BotResponses.McPlaceUnknownLines, Label(McCommand.PlaceName(text, key), owner, player));
        else if (state is null) Reply(player, whispered, BotResponses.McPlaceNotYoursLines, Label(place.Name, owner, player), Who(place.By));
        else Reply(player, whispered, BotResponses.McPlaceForgottenLines, Label(place.Name, owner, player));
    }

    // The places she knows for this player, as many as fit a chat line: their own first, then the others they can send
    // her to, with whose they are. Of: one player's only (« liste les lieux de Rodhengard »).
    private void ListPlaces(string player, bool whispered, string? of)
    {
        List<string> names;
        lock (_gate)
            names = (of is not null ? [of] : SearchOrderLocked(player))
                .Where(_places.ContainsKey)
                .SelectMany(owner => _places[owner].Values.Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal)
                    .Select(n => Same(owner, player) ? n : $"{n} {OfWhom(owner)}"))
                .ToList();
        if (names.Count == 0)
        {
            if (of is not null && !Same(of, player)) Reply(player, whispered, BotResponses.McPlaceListEmptyOfLines, Who(of));
            else Reply(player, whispered, BotResponses.McPlaceListEmptyLines);
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

    // ---- What she carries -----------------------------------------------------------------------

    // « t'as quoi sur toi ? », « t'as combien de fer ? »: read from the body's last report, nothing sent to it.
    private void TellInventory(string player, bool whispered, McGoods goods)
    {
        McInventory? inventory;
        lock (_gate) inventory = _inventory;
        if (inventory is null) return; // reported on spawn: only before it, when nobody can ask yet
        if (goods == McGoods.All)
        {
            if (inventory.Overview(ListMax) is { } all) Reply(player, whispered, BotResponses.McInventoryLines, all);
            else Reply(player, whispered, BotResponses.McInventoryEmptyLines);
            return;
        }
        if (inventory.Of(goods, ListMax) is { } some) Reply(player, whispered, BotResponses.McHaveLines, some);
        else Reply(player, whispered, BotResponses.McHaveNoneLines, BotResponses.McGoodsNames[goods]);
    }

    private static McInventory ReadInventory(JsonElement root)
    {
        static McStack? Stack(JsonElement e) => e.ValueKind != JsonValueKind.Object ? null
            : new McStack(e.GetProperty("name").GetString() ?? "", e.GetProperty("count").GetInt32(),
                e.GetProperty("goods").EnumerateArray().Select(g => g.GetString() ?? "").ToArray());
        static McStack[] Stacks(JsonElement root, string name) =>
            root.TryGetProperty(name, out var list) && list.ValueKind == JsonValueKind.Array
                ? list.EnumerateArray().Select(Stack).OfType<McStack>().ToArray()
                : [];
        return new McInventory(Stacks(root, "worn"), root.TryGetProperty("held", out var held) ? Stack(held) : null,
            root.TryGetProperty("offhand", out var off) ? Stack(off) : null, Stacks(root, "pockets"));
    }

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
                Ack(player, whispered, $"place:{detail}", BotResponses.McPlaceGoAckLines, detail);
                break;
            case "goto":
                Ack(player, whispered, $"goto:{detail}", BotResponses.McGoToAckLines, detail);
                break;
            case "guide":
                Ack(player, whispered, $"guide:{detail}", BotResponses.McGuideAckLines, detail);
                break;
            case "guard":
                Ack(player, whispered, "guard", Kin(player, BotResponses.McGuardAckLines, BotResponses.McGuardAckLinesOwner, BotResponses.McGuardAckLinesTata));
                break;
            case "follow":
                Ack(player, whispered, "follow", Kin(player, BotResponses.McFollowAckLines, BotResponses.McFollowAckLinesOwner, BotResponses.McFollowAckLinesTata));
                break;
            default:
                Ack(player, whispered, "come", Kin(player, BotResponses.McComeAckLines, BotResponses.McComeAckLinesOwner, BotResponses.McComeAckLinesTata));
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

    // Hungry with nothing to eat: she asks, rationed on its own clock; louder once she can't sprint. The asking
    // gate is given back when the remark gates swallow the line, or she'd wait ten minutes for nothing.
    private void OnHungry(int food)
    {
        var starving = food <= StarvingFood;
        var gate = starving ? _starvingGate : _hungryGate;
        if (!gate.TryClaim(0)) return;
        if (!Remark(starving ? "starving" : "hungry", starving ? BotResponses.McStarvingLines : BotResponses.McHungryLines))
            gate.Release(0);
    }

    // Something she lacks, a tool wearing out or one that broke: once every 10 minutes per thing, and the gate is given
    // back when the remark gates swallow the line, as for asking for food.
    private void OnNeed(string key, string[] pool, params object[] extra)
    {
        if (!_needGate.TryClaim(key)) return;
        if (!Remark($"need:{key}", pool, null, false, extra)) _needGate.Release(key);
    }

    private static string[]? NeedPool(string what) => what switch
    {
        "rod" => BotResponses.McNeedRodLines,
        "shears" => BotResponses.McNeedShearsLines,
        "breedFood" => BotResponses.McNeedBreedFoodLines,
        "arrows" => BotResponses.McNeedArrowsLines,
        "space" => BotResponses.McPocketsFullLines,
        _ => null,
    };

    private static string ToolName(string tool) => BotResponses.McToolNames.TryGetValue(tool, out var name) ? name : tool;

    // Where her routine takes her on her own (the body's own task): a word now and then.
    private static string[]? RoutinePool(string? what) => what switch
    {
        "farm" => BotResponses.McRoutineFarmLines,
        "fish" => BotResponses.McRoutineFishLines,
        "ranch" => BotResponses.McRoutineRanchLines,
        "home" => BotResponses.McRoutineHomeLines,
        _ => null,
    };

    // Nothing to do, so she keeps near Papa or Tata (only family: the body tags along with nobody else). Now and then a word.
    private void OnTagAlong(string player)
    {
        var pool = KinOf(player) switch
        {
            McKin.Papa => BotResponses.McTagAlongLinesOwner,
            McKin.Tata => BotResponses.McTagAlongLinesTata,
            _ => null,
        };
        if (pool is not null && Random.Shared.NextDouble() < TagAlongChance) Remark("tagAlong", pool, player);
    }

    // She ran from an attacker in the middle of someone's one-off order (come, go to, gather): it's over,
    // and they hear why, unless they're the attacker.
    private void OnDropped(string player, string attacker)
    {
        var (whispered, _) = Finish(player);
        if (player.Length == 0 || Same(player, attacker)) return;
        Reply(player, whispered, BotResponses.McFleeDroppedLines, Who(attacker));
    }

    // In bed, asked, because a player went to bed, or on her own at night (auto): whatever she was doing is over,
    // whoever it was for, except an activity (a farm, fishing, a guarded place): the body goes back to it when she
    // wakes, and it stays its keeper's. On her own she tells the server, so they know the night can pass.
    private void OnSlept(string player, bool auto)
    {
        McJob? activity;
        lock (_gate) activity = _order is { } kept && IsActivity(kept.Order) ? kept : null;
        if (auto)
        {
            lock (_gate) _order = activity;
            Remark("bedtime", BotResponses.McBedtimeLines);
            return;
        }
        var (whispered, _) = Confirm(player);
        lock (_gate) _order = activity;
        Ack(player, whispered, "sleep", BotResponses.McSleepLines);
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
        // Walking up to them says it; the order still ends either way.
        else if (Random.Shared.NextDouble() < ArrivedChance) OnOutcome(player, BotResponses.McArrivedLines);
        else Finish(player);
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
        Ack(player, whispered, $"collect:{resource}", BotResponses.McCollectAckLines, count, ResourceName(resource));
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
        var text = string.Format(_picker.Pick(pool, LineChoices), [Who(player), .. extra]);
        lock (_gate)
            if (_body is { } body && _spawned && !_leaving)
                SayLocked(body, whispered ? new { op = "whisper", player, text } : (object)new { op = "say", text }, text);
    }

    // ---- Talk -----------------------------------------------------------------------------------

    /// <summary>
    /// Chat that isn't an order, read the way Discord reads it (McTalkReader) and answered from her Discord pools made fit
    /// for the game (McChat.Game). An answer to someone isn't a remark: no gate, no budget, only the conversation streak.
    /// Talked about rather than to, she speaks on her own initiative: a remark like any other. Each answer opens the
    /// call window again, so the next message is heard without her name. True when it was dealt with, said or not;
    /// false when there was nothing to say (a call then gets its « oui ? »).
    /// </summary>
    private bool Talk(string player, bool whispered, string text, bool call, bool listening)
    {
        var kin = KinOf(player);
        var read = McTalkReader.Read(HerNameAsSyncs(text), kin, call, listening, whispered, McTalkReader.AskerId(player, kin),
            _health.CurrentMilli, _picker, Random.Shared);
        if (read is not { } talk) return false;
        var weekday = AppTime.Now.ToString("dddd", CultureInfo.GetCultureInfo("fr-FR"));

        if (talk.Remark)
        {
            if (Random.Shared.NextDouble() < TalkedAboutChance && McChat.Game(talk.Pool) is { Length: > 0 } lines)
                Remark("talkedAbout", lines, player, false, weekday, talk.A, talk.B);
            return true;
        }

        var pool = talk.Pool;
        object[] extra = [weekday, talk.A, talk.B];
        if (kin != McKin.Papa)
        {
            var now = DateTimeOffset.UtcNow;
            var key = McTalkReader.AskerId(player, kin);
            if (_talks.IsClosed(0, key, now)) return true;
            var count = _talks.Record(0, key, now);
            if (count >= ConversationStreak.EndAt)
            {
                _talks.Close(0, key, now);
                (pool, extra) = (BotResponses.StreakEndLines, [weekday, count]);
            }
            else if (count == ConversationStreak.NudgeAt && talk.Fallback)
                (pool, extra) = (BotResponses.StreakNudgeLines, [weekday, count]);
        }

        if (McChat.Game(pool, whispered) is not { Length: > 0 } game) return false;
        _logger.LogInformation("Minecraft: {Player} talked to her — {Kind}.", player, talk.Kind);
        Reply(player, whispered, game, extra);
        // She answered, unless she just ended the conversation: their next message is read as said to her (an order
        // needs no name), and for CallWindow what is plainly for her is answered, whatever else they say meanwhile.
        lock (_gate)
            if (!ReferenceEquals(pool, BotResponses.StreakEndLines))
            {
                var now = DateTimeOffset.UtcNow;
                _calledAt[player] = now;
                _answeredAt[player] = now;
            }
            else _answeredAt.Remove(player);
        return true;
    }

    // Her in-game name read as « syncs » by the chat cues, which only know that one (« SYNCS_bot ça va ? »).
    private string HerNameAsSyncs(string text) =>
        McTalkReader.IsHerName(_username) ? text
            : System.Text.RegularExpressions.Regex.Replace(text, $@"(?<![\w]){System.Text.RegularExpressions.Regex.Escape(_username)}(?![\w])", "syncs",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    // Whether this player's hello, goodbye, « gg », « merci » or call gets her words: the first of a burst does, and
    // Papa and Tata always do (the owner's call). Family still claims the gate, so the others after them get the gesture.
    private bool FirstOfBurst(string player, McOrder kind)
    {
        var first = _burstGate.TryClaim(kind);
        return first || KinOf(player) != McKin.Anyone;
    }

    // An order's acknowledgement (« j'arrive », « je te suis »), said once every 3 minutes for the same order from the
    // same player: <paramref name="what"/> names the order and whatever makes it another one (the place, the resource).
    private void Ack(string player, bool whispered, string what, string[] pool, params object[] extra)
    {
        if (_ackGate.TryClaim($"{player.ToLowerInvariant()}|{what}")) Reply(player, whispered, pool, extra);
    }

    // Her lines go out one after another, each after the time it takes to type it, as on Discord (BotChat): an
    // answer in the same instant as the question reads as a machine. The queue keeps them in order. Whatever can't
    // wait (a forced remark: a creeper behind someone, drowning) goes out at once, and so does her goodbye, which
    // the quit right behind it would cut off.
    private void SayLocked(MinecraftBodyProcess body, object command, string text, bool now = false)
    {
        if (now)
        {
            body.Send(command);
            return;
        }
        _speech = _speech.ContinueWith(async _ =>
        {
            await Task.Delay(TypingPause(text));
            lock (_gate)
                if (ReferenceEquals(_body, body) && _spawned && !_leaving)
                    body.Send(command);
        }, TaskScheduler.Default).Unwrap();
    }

    // Game chat is typed slower than Discord: a short « oui ? » takes under a second, a long line about three.
    private static TimeSpan TypingPause(string text) =>
        TimeSpan.FromMilliseconds(Math.Clamp(400 + text.Length * 40, 700, 3000));

    // A player hit her. The first hits get a remark; the one that makes a streak (McHitStreaks: the same
    // player, three times, no more than a minute apart) is when she understands they want to kill her, and
    // that line goes out at once, past the remark gates. Hits after it keep the same lines, rationed again.
    private void OnHit(string hitter)
    {
        // Someone she's hitting back for Papa or Tata: their hits are the fight, not a reason to complain or run.
        lock (_gate)
            if (_defending.TryGetValue(hitter, out var defence) && defence.Until > DateTimeOffset.UtcNow) return;
        McHitStage stage;
        lock (_gate) stage = _hits.Register(hitter, DateTimeOffset.UtcNow);
        if (stage == McHitStage.Hit)
        {
            if (KinOf(hitter) == McKin.Anyone && Random.Shared.NextDouble() >= HitChance) return;
            Remark("hit", Kin(hitter, BotResponses.McHitByPlayerLines, BotResponses.McHitByPlayerLinesOwner,
                BotResponses.McHitByPlayerLinesTata), hitter);
            return;
        }
        Remark("hit", Kin(hitter, BotResponses.McHitAgainLines, BotResponses.McHitAgainLinesOwner, BotResponses.McHitAgainLinesTata),
            hitter, force: stage == McHitStage.Escalated);
        // And she runs. Never from Papa: she stays, scared, and takes it.
        if (KinOf(hitter) == McKin.Papa) return;
        lock (_gate)
            if (_body is { } body && _spawned && !_leaving)
                body.Send(new { op = "flee", player = hitter });
    }

    // A player hit Papa or Tata (never family: the body filters them out). The first hit gets a word; again within
    // the streak's window, she hits them back once; after that she fights them (McHitStreaks.DefendStage). The body
    // says whether she could go (`defending`) or was busy with someone's order (`cantDefend`, words only).
    private void OnFamilyHurt(string victim, string attacker)
    {
        if (victim.Length == 0 || attacker.Length == 0 || KinOf(attacker) != McKin.Anyone) return;
        McDefendStage stage;
        lock (_gate) stage = McHitStreaks.DefendStage(_familyHits.Tally(attacker, DateTimeOffset.UtcNow));
        if (stage == McDefendStage.Warn)
        {
            Remark("familyHit", BotResponses.McFamilyHitLines, attacker, false, Who(victim));
            return;
        }
        lock (_gate)
            if (_body is { } body && _spawned && !_leaving)
                body.Send(new { op = "defend", player = attacker, victim, swat = stage == McDefendStage.Swat });
    }

    // She's hitting them back. The swat and the start of a fight are said at once (the hit needs its reason); the
    // fight's later hits are rationed.
    private void OnDefending(string attacker, string victim, bool swat)
    {
        bool fightStarts;
        lock (_gate)
        {
            fightStarts = !swat && !(_defending.TryGetValue(attacker, out var was) && was.Fighting && was.Until > DateTimeOffset.UtcNow);
            _defending[attacker] = (DateTimeOffset.UtcNow + DefendHold, !swat);
        }
        Remark(swat ? "familySwat" : "familyFight", swat ? BotResponses.McFamilySwatLines : BotResponses.McFamilyFightLines,
            attacker, swat || fightStarts, Who(victim));
    }

    /// <summary>
    /// One of her lines in Minecraft's chat, rationed per kind, overall and by the 10-minute budget.
    /// <paramref name="force"/> says it anyway, for the moments that can't wait, and still starts both cooldowns so
    /// nothing piles on after (it doesn't spend the budget). Returns whether it was said. <paramref name="extra"/>
    /// fills {1}, {2}… as the pool documents.
    /// </summary>
    private bool Remark(string kind, string[] pool, string? name = null, bool force = false, params object[] extra)
    {
        if (force)
        {
            _kindGate.TryClaim(kind);
            _anyGate.TryClaim(0);
        }
        else if (!_kindGate.TryClaim(kind)) return false;
        else if (!_anyGate.TryClaim(0))
        {
            _kindGate.Release(kind);
            return false;
        }
        lock (_gate)
            if (_body is { } body && _spawned && !_leaving)
            {
                if (!force && !SpendBudgetLocked())
                {
                    _kindGate.Release(kind);
                    _anyGate.Release(0);
                    return false;
                }
                var text = string.Format(_picker.Pick(pool, LineChoices), [name is null ? "" : Who(name), .. extra]);
                SayLocked(body, new { op = "say", text }, text, now: force);
                return true;
            }
        return false;
    }

    // Whether an unforced remark still fits in the last 10 minutes' budget, counting it if so.
    private bool SpendBudgetLocked()
    {
        var now = DateTimeOffset.UtcNow;
        while (_remarksSaid.TryPeek(out var said) && now - said >= RemarkBudgetWindow) _remarksSaid.Dequeue();
        if (_remarksSaid.Count >= RemarkBudget) return false;
        _remarksSaid.Enqueue(now);
        return true;
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

    /// <summary>
    /// What she carries, for the owner: what she wears, holds and has in her pockets, with full counts. An item she has
    /// no French for shows its game id. Item ids come from the server: in code spans.
    /// </summary>
    public string InventoryText()
    {
        McInventory? inventory;
        DateTimeOffset at;
        lock (_gate)
        {
            inventory = _spawned ? _inventory : null;
            at = _inventoryAt;
        }
        if (inventory is null) return "🎒 Elle n'est pas sur le serveur : pas d'inventaire à montrer.";

        string Item(string id, int count) => McItems.Name(id) is { } name ? name.Count(count) : $"{count} × {Code(id)}";
        string One(McStack? stack) => stack is null ? "rien" : Item(stack.Id, stack.Count);
        var sb = new StringBuilder();
        sb.AppendLine($"🎒 **Inventaire de SYNCS** (mis à jour {Stamp(at)})");
        sb.AppendLine($"Porte : {(inventory.Worn.Count == 0 ? "rien" : string.Join(", ", inventory.Worn.Select(w => One(w))))}");
        sb.AppendLine($"En main : {One(inventory.Held)} · Main gauche : {One(inventory.Offhand)}");
        var pockets = McInventory.Sum(inventory.Pockets);
        sb.AppendLine(pockets.Count == 0 ? "Poches : vides"
            : $"Poches ({inventory.Pockets.Count}/{PocketSlots} emplacements) :");
        foreach (var (id, count) in pockets)
        {
            var line = $"- {Item(id, count)}";
            // Discord's 2000; 36 slots never get close, but a modded server's ids could.
            if (sb.Length + line.Length > 1900)
            {
                sb.AppendLine("- …");
                break;
            }
            sb.AppendLine(line);
        }
        return sb.ToString().TrimEnd();
    }

    // Her pockets and hotbar: 27 + 9.
    private const int PocketSlots = 36;

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
