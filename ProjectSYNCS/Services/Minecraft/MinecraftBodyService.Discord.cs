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

// What Discord asks of her Minecraft life: /minecraft link, stats, invite, status and where, and /debug minecraft.
// Part of MinecraftBodyService.
public sealed partial class MinecraftBodyService
{
    // ---- Links (/minecraft link) ----------------------------------------------------------------

    /// <summary>The codes members are given on Discord and whisper to her in game.</summary>
    public McLinkCodes LinkCodes { get; } = new();

    /// <summary>Her in-game name, for the whisper /minecraft link tells members to type.</summary>
    public string Username => _username;

    public bool IsSpawned
    {
        get { lock (_gate) return _spawned && !_leaving; }
    }

    /// <summary>Who a name is to her (Papa, Tata, anyone), by Minecraft:Owner and Minecraft:Tata.</summary>
    public McKin KinOfPlayer(string player) => KinOf(player);

    // A redeemed code: saved off the body's event thread, and confirmed to the player in a whisper. The name was checked
    // free when the code was issued; a member who took it in the meantime wins (the unique index refuses this one). The
    // code is spent either way, so a failure is said too: the player asks for a new one rather than wait for nothing.
    private async Task SaveLinkAsync(ulong userId, string player)
    {
        try
        {
            await using var scope = _services.CreateAsyncScope();
            var data = scope.ServiceProvider.GetRequiredService<MinecraftDataService>();
            if (await data.LinkOfPlayerAsync(player) is { } taken && taken.UserId != userId)
            {
                Reply(player, true, BotResponses.McLinkFailedLines);
                return;
            }
            await data.SaveLinkAsync(userId, player);
            _logger.LogInformation("Minecraft: {Player} linked to Discord member {UserId}.", player, userId);
            Reply(player, true, BotResponses.McLinkedLines);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not save the Minecraft link of {Player}.", player);
            Reply(player, true, BotResponses.McLinkFailedLines);
        }
    }

    // ---- Stats (/minecraft stats) ---------------------------------------------------------------

    // Counted from the body's events, written every StatsFlush by the loop (never a write per event); a crash loses at
    // most that much, a clean stop flushes. _countedUntil: how far her time in game is counted. Guarded by _gate.
    private readonly McTally _tally = new();
    private static readonly TimeSpan StatsFlush = TimeSpan.FromMinutes(5);
    private DateTimeOffset _lastFlush = DateTimeOffset.UtcNow;
    private DateTimeOffset? _countedUntil;

    /// <summary>What was counted since the last write: /minecraft stats adds it to the stored totals.</summary>
    public IReadOnlyDictionary<string, long> PendingStats() => _tally.Peek();

    /// <summary>How many places she remembers, everyone's.</summary>
    public int PlacesCount
    {
        get { lock (_gate) return _places.Values.Sum(p => p.Count); }
    }

    /// <summary>A gathered resource's French name (« wood » → its name in her lines), else the key as is.</summary>
    public static string ResourceLabel(string resource) => ResourceName(resource);

    // Her whole minutes in game since the last count.
    private void CountTimeLocked(DateTimeOffset now)
    {
        if (!_spawned || _inGameSince is not { } since) return;
        var from = _countedUntil is { } counted && counted > since ? counted : since;
        var minutes = (long)(now - from).TotalMinutes;
        if (minutes <= 0) return;
        _tally.Add(McStats.Minutes, minutes);
        _countedUntil = from + TimeSpan.FromMinutes(minutes);
    }

    // Writes what was counted. A failed write puts the counts back, for the next try.
    private async Task FlushStatsAsync()
    {
        var counts = _tally.Drain();
        if (counts.Count == 0) return;
        try
        {
            await using var scope = _services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<MinecraftDataService>().AddStatsAsync(counts, AppTime.DayKey(DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            foreach (var (key, n) in counts) _tally.Add(key, n);
            _logger.LogWarning(ex, "Could not write the Minecraft stats.");
        }
    }

    // ---- Invites (/minecraft invite) ------------------------------------------------------------

    // Until when an invite keeps her in whatever the presence rule says, and who invited her (in-game name), for her
    // arrival line; cleared once said. Forbidding her or the owner's forced leave ends an invite. Guarded by _gate.
    private DateTimeOffset _invitedUntil = DateTimeOffset.MinValue;
    private string? _invitedBy;

    /// <summary>A linked player asks her onto the server (McInvite: allowed, not held, not already there).</summary>
    public McInviteVerdict Invite(string player)
    {
        if (!Enabled || _script is null) return McInviteVerdict.NotAllowed;
        var now = DateTimeOffset.UtcNow;
        lock (_gate)
        {
            var verdict = McInvite.Decide(_allowed, _lastStatus is not null, _body is not null, _holdUntil, _invitedUntil, now);
            if (verdict != McInviteVerdict.Go) return verdict;
            (_invitedUntil, _invitedBy) = (now + McInvite.Length, player);
            _retryAfter = DateTimeOffset.MinValue;
            _failures = 0;
        }
        _logger.LogInformation("Minecraft: {Player} invited her for {Minutes} minutes.", player, McInvite.Length.TotalMinutes);
        if (!StartBody()) _logger.LogWarning("Minecraft: the invited body could not start.");
        return McInviteVerdict.Go;
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
            if (_lastExit is { } exit)
                sb.AppendLine($"Dernier arrêt du corps {Stamp(exit.At)} : {(exit.Planned ? "prévu" : "imprévu")}"
                              + (exit.Reason is { Length: > 0 } why ? $" ({Code(why.Length > 120 ? why[..120] + "…" : why)})" : ""));
            // The newest first, as many as fit in Discord's 2000 (each one capped at BodyLogLineMax when it came).
            if (_bodyLog.Count > 0)
            {
                sb.AppendLine("Derniers messages du corps, le plus récent d'abord :");
                foreach (var (logged, line) in _bodyLog.Reverse())
                {
                    var entry = $"- {Stamp(logged)} {Code(line)}";
                    if (sb.Length + entry.Length > 1900) break;
                    sb.AppendLine(entry);
                }
            }
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// /minecraft status: the server, who's on, and what she's doing, for anyone. Nothing of the owner's (address, kick
    /// reasons, the allow switch): that stays in /debug. Player names are someone else's text: sanitized.
    /// </summary>
    public string PublicStatusText()
    {
        if (!Enabled || _script is null) return "Elle n'a pas de serveur Minecraft configuré.";
        var sb = new StringBuilder();
        lock (_gate)
        {
            if (_lastStatus is not { } s) return "Le serveur ne répond pas.";
            sb.AppendLine($"Serveur : en ligne, {s.Online}/{s.Max} joueurs.");
            if (!_spawned)
            {
                sb.Append(_body is null ? "Elle n'est pas sur le serveur." : "Elle se connecte…");
                return sb.ToString();
            }
            sb.AppendLine($"Elle est en jeu depuis {Stamp(_inGameSince!.Value)}.");
            sb.AppendLine(_players.Count == 0 ? "Avec elle : personne."
                : $"Avec elle : {string.Join(", ", _players.Select(Format.Sanitize))}.");
            var job = _order ?? _pending;
            // On nobody's order, what she does on her own (« elle pêche (sa routine) »), as the body last said.
            var doing = job is null && McDoing.OwnLife(_doing, _doing is { Player.Length: > 0 } d ? Who(d.Player) : null, firstPerson: false) is { } own
                ? own
                : McDoing.ForStatus(job?.Order, job is null ? null : Who(job.Player), job?.Detail, job?.Private ?? false);
            sb.Append($"En ce moment, {Format.Sanitize(doing)}.");
        }
        return sb.ToString();
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

    /// <summary>
    /// /minecraft where: asks the body where she is and waits for its answer (<paramref name="timeout"/> at most). Callers
    /// at the same time share one question. Never throws; an answer that doesn't come says so.
    /// </summary>
    public async Task<string> WhereAsync(TimeSpan timeout)
    {
        Task<McSpot> answer;
        lock (_gate)
        {
            if (_body is not { } body || !_spawned || _leaving) return "Elle n'est pas sur le serveur.";
            if (_whereFromDiscord is null)
            {
                _whereFromDiscord = new TaskCompletionSource<McSpot>(TaskCreationOptions.RunContinuationsAsynchronously);
                body.Send(new { op = "where", player = DiscordAsker });
            }
            answer = _whereFromDiscord.Task;
        }
        if (await Task.WhenAny(answer, Task.Delay(timeout)) != answer)
        {
            lock (_gate) if (_whereFromDiscord?.Task == answer) _whereFromDiscord = null;
            return "Pas de réponse de sa part pour l'instant.";
        }
        var spot = answer.Result;
        return $"Elle est en `{spot.X} {spot.Y} {spot.Z}`, {BotResponses.McDimensionNames[DimensionOf(spot.Dimension)]}.";
    }

    // The asker of a « where » sent for Discord: no player name can hold U+0001, so the body's answer can't be mistaken
    // for a player's. Guarded by _gate.
    private const string DiscordAsker = "\u0001discord";
    private TaskCompletionSource<McSpot>? _whereFromDiscord;

    /// <summary>
    /// /debug minecraft « Voir son corps »: asks the body how it's doing (its <c>debug</c> event) and waits for the answer
    /// (<paramref name="timeout"/> at most). Callers at the same time share one question. Never throws.
    /// </summary>
    public async Task<string> DebugBodyAsync(TimeSpan timeout)
    {
        Task<JsonElement> answer;
        lock (_gate)
        {
            if (_body is not { } body || !_spawned || _leaving) return "🩺 Elle n'est pas sur le serveur : pas de corps à examiner.";
            if (_debugAnswer is null)
            {
                _debugAnswer = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
                body.Send(new { op = "debug" });
            }
            answer = _debugAnswer.Task;
        }
        if (await Task.WhenAny(answer, Task.Delay(timeout)) != answer)
        {
            lock (_gate) if (_debugAnswer?.Task == answer) _debugAnswer = null;
            return "🩺 Son corps ne répond pas (la boucle Node est peut-être bloquée : voir les derniers messages dans « Voir l'état »).";
        }
        return McBodyReport.Format(answer.Result);
    }

    // The question /debug minecraft « Voir son corps » is waiting on. Guarded by _gate.
    private TaskCompletionSource<JsonElement>? _debugAnswer;

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
            if (!allowed) (_invitedUntil, _invitedBy) = (DateTimeOffset.MinValue, null);
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
        lock (_gate)
        {
            _holdUntil = DateTimeOffset.UtcNow + ForcedLeaveHold;
            (_invitedUntil, _invitedBy) = (DateTimeOffset.MinValue, null);
        }
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
