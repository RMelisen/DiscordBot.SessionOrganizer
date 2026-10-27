using System.Globalization;
using System.Text;
using System.Text.Json;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
/// <para>One class over several files: this one has its state, the loop and the body's lifecycle; the others are
/// named for what they hold (<c>.Events</c>, <c>.Orders</c>, <c>.Places</c>, <c>.AtOnce</c>, <c>.Outcomes</c>,
/// <c>.Talk</c>, <c>.Discord</c>). A field lives here, with every other field, whichever file uses it.</para>
/// </remarks>
public sealed partial class MinecraftBodyService : BackgroundService
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
    private readonly IServiceProvider _services;
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
    // What the body last said she's doing (its `doing` event): her own life, which only the body knows. Null until then.
    private McBodyDoing? _doing;
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
    // Her camp (McCamp), kept in the state file. Guarded by _gate.
    private McCamp? _camp;
    // Asked where she is / to remember a spot: the body answers with a position, and these say to whom and how.
    // Of: whose place it will be (« retiens ici : maison de Rodhengard »), null for the player's own.
    private readonly Dictionary<string, bool> _askedWhere = new(StringComparer.OrdinalIgnoreCase);
    // Asked to put her armour on or take it off: the body says how it went, and this says how they asked.
    private readonly Dictionary<string, bool> _dressing = new(StringComparer.OrdinalIgnoreCase);
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
    // For /debug minecraft: the body's last log lines (kept across bodies, so the ones before a crash stay), how the
    // last body ended, and the reason its connection gave (the body's `end` event). Guarded by _gate.
    private readonly Queue<(DateTimeOffset At, string Line)> _bodyLog = new();
    private const int BodyLogKept = 8;
    private const int BodyLogLineMax = 180;
    private (DateTimeOffset At, bool Planned, string? Reason)? _lastExit;
    private string? _lastEnd;

    public MinecraftBodyService(DiscordSocketClient client, ResponsePicker picker, PiHealthService health, IServiceProvider services,
        IConfiguration configuration,
        ILogger<MinecraftBodyService> logger)
    {
        _client = client;
        _picker = picker;
        _health = health;
        _services = services;
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
        _camp = state?.Camp;
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
        Dictionary<string, Dictionary<string, McPlace>>? PlacesByPlayer = null, McCamp? Camp = null);
    // Her camp, the one there is: where it stands and the blocks she placed for it, which « démonte le camp » sends back
    // to the body (it breaks only those, still what she placed). Kept across restarts.
    private sealed record McCamp(int X, int Y, int Z, string Dimension, List<McCampBlock> Blocks);
    private sealed record McCampBlock(int X, int Y, int Z, string Name);
    private sealed record McSpot(int X, int Y, int Z, string Dimension);
    // Name: as the player wrote it (McCommand.PlaceName), for her lines. By: who saved it, who may move or forget it
    // besides the player it belongs to and family.
    private sealed record McPlace(string Name, int X, int Y, int Z, string Dimension, string By);

    private MinecraftState StateLocked() =>
        new(_allowed, new Dictionary<string, McSpot>(_seen),
            PlacesByPlayer: _places.Where(p => p.Value.Count > 0).ToDictionary(p => p.Key, p => new Dictionary<string, McPlace>(p.Value)),
            Camp: _camp);

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
            var now = DateTimeOffset.UtcNow;
            lock (_gate) CountTimeLocked(now);
            if (now - _lastFlush >= StatsFlush)
            {
                _lastFlush = now;
                await FlushStatsAsync();
            }
            await Task.Delay(Interval, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        lock (_gate) CountTimeLocked(DateTimeOffset.UtcNow);
        await FlushStatsAsync();
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
            // Invited (/minecraft invite): wanted in for the hour, whatever the count, as long as the server answers.
            var wanted = verdict.InGame || (status is not null && _allowed && now < _invitedUntil);
            join = !inGame && wanted && now >= _retryAfter && now >= _holdUntil;
            leave = inGame && !wanted && !_leaving;
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
                    OnEvent, OnBodyLog, OnExit);
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
            _doing = null;
            _fewSince = null;
            _order = null;
            _pending = null;
            _seeking = null;
            _lastEnd = null;
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
            CountTimeLocked(DateTimeOffset.UtcNow);
            _spawned = false;
            _leaving = false;
            _inGameSince = null;
            _players = new List<string>();
            _inventory = null;
            _doing = null;
            _fewSince = null;
            _order = null;
            _pending = null;
            _seeking = null;
            _hits.Clear();
            _familyHits.Clear();
            _defending.Clear();
            ForgetBodyLocked();
            if (!planned) MarkFailureLocked(DateTimeOffset.UtcNow);
            _lastExit = (DateTimeOffset.UtcNow, planned, _lastEnd);
            _logger.LogInformation("Minecraft body exited ({How}).", planned ? "left" : "unplanned");
        }
    }

    // A line of the body's log (stderr, or stdout that isn't an event): to the logger, and kept for /debug minecraft.
    private void OnBodyLog(string line)
    {
        _logger.LogInformation("Minecraft body: {Line}", line);
        lock (_gate)
        {
            _bodyLog.Enqueue((DateTimeOffset.UtcNow, line.Length > BodyLogLineMax ? line[..BodyLogLineMax] + "…" : line));
            while (_bodyLog.Count > BodyLogKept) _bodyLog.Dequeue();
        }
    }

    // What only made sense for the body that's going: reminders, questions waiting for its answer.
    private void ForgetBodyLocked()
    {
        _timers.Clear();
        _marking.Clear();
        _askedWhere.Clear();
        _dressing.Clear();
        _whereFromDiscord = null;
        _debugAnswer = null;
    }

    private void MarkFailureLocked(DateTimeOffset now)
    {
        _failures++;
        _retryAfter = now + Backoff[Math.Min(_failures, Backoff.Length) - 1];
    }
}
