using Discord;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>
/// Runs the newer /arcade games past the click that started them: lobbies that close, races
/// and rockets that move, fuses that burn, rounds that time out — and every settlement, through
/// ArcadeRunner's single path. Singleton with no state of its own (that's ArcadeTables). Each
/// ending is claimed with <see cref="ArcadeTables.End"/> first, so a timer and a click racing to
/// end the same game can't both settle it.
/// </summary>
public sealed class PartyRunner
{
    private static readonly TimeSpan RaceFrame = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan Watch = TimeSpan.FromSeconds(30);

    private readonly ArcadeTables _tables;
    private readonly ArcadeRunner _arcade;
    private readonly ResponsePicker _picker;
    private readonly BadgeAwarder _badges;

    public PartyRunner(ArcadeTables tables, ArcadeRunner arcade, ResponsePicker picker, BadgeAwarder badges)
    {
        _badges = badges;
        _tables = tables;
        _arcade = arcade;
        _picker = picker;
    }

    private string Pick(string[] pool, params object[] args) =>
        args.Length == 0 ? _picker.Pick(pool) : string.Format(_picker.Pick(pool), args);

    private static string Mentions(IEnumerable<ulong> ids) => string.Join(" ", ids.Select(id => $"<@{id}>"));

    // ---- Race -------------------------------------------------------------------------------------

    public void ScheduleRace(RaceGame race) => _arcade.Run($"race {race.Id}", async () =>
    {
        var wait = race.StartsAt - DateTimeOffset.UtcNow;
        if (wait > TimeSpan.Zero) await Task.Delay(wait);
        await RunRaceAsync(race.Id);
    });

    private async Task RunRaceAsync(int id)
    {
        // Snapshot the bets as the gates close: a late click is told the race has started.
        var snapshot = _tables.With<RaceGame, (RaceGame Race, Dictionary<ulong, (int Runner, long Stake)> Bets)?>(id, r =>
        {
            if (r is null || r.Started) return null;
            r.Started = true;
            return (r, new Dictionary<ulong, (int Runner, long Stake)>(r.Bets));
        });
        if (snapshot is not { } s) return;
        var race = s.Race;

        try
        {
            var frames = Race.Simulate(race.Runners.Select(x => x.Strength).ToList(), Random.Shared);
            await _arcade.EditAsync(race, PartyCards.RaceFrame(race, new double[race.Runners.Count], Pick(BotResponses.RaceStartLines)),
                new ComponentBuilder().Build());
            for (var f = 0; f < frames.Length - 1; f++)
            {
                await Task.Delay(RaceFrame);
                var leader = Array.IndexOf(frames[f], frames[f].Max());
                await _arcade.EditAsync(race, PartyCards.RaceFrame(race, frames[f], Pick(BotResponses.RaceLeadLines, race.Runners[leader].Name)), null);
            }
            await Task.Delay(RaceFrame);

            var winner = Race.Winner(frames);
            var champion = race.Runners[winner];
            var stakes = s.Bets.Select(b => (b.Key, b.Value.Stake)).ToList();
            var bonusTo = champion.OwnerId;
            var paysBonus = bonusTo != 0 && bonusTo != _arcade.BotId;
            if (paysBonus && !s.Bets.ContainsKey(bonusTo)) stakes.Add((bonusTo, 0));

            var settled = await _arcade.SettleAsync(race.GuildId, stakes, payers =>
            {
                var paying = s.Bets.Where(b => payers.Contains(b.Key)).ToDictionary(b => b.Key, b => b.Value);
                var nets = Race.Nets(paying, winner);
                if (paysBonus && payers.Contains(bonusTo)) nets[bonusTo] = nets.GetValueOrDefault(bonusTo) + Race.OwnerBonus;
                return nets;
            });

            var lines = settled.Nets.Where(n => s.Bets.ContainsKey(n.Key) && n.Value > 0)
                .Select(n => $"<@{n.Key}> +{PebbleEconomy.Cailloux(n.Value)}").ToList();
            // Bettors who came out ahead (not the owner's bonus alone), once the settlement is saved.
            foreach (var n in settled.Nets.Where(n => s.Bets.ContainsKey(n.Key) && n.Value > 0))
                _badges.Increment(race.GuildId, n.Key, MemberBadges.Keys.Arcade);
            var result = Pick(BotResponses.RaceWinLines, champion.Name, $"<@{champion.OwnerId}>") + "\n\n" +
                         (lines.Count > 0 ? "**Gagnants** : " + string.Join(" · ", lines) : "-# Personne n'avait parié sur le vainqueur. La cagnotte reste chez moi.") +
                         (paysBonus ? $"\n-# <@{champion.OwnerId}> reçoit {PebbleEconomy.Cailloux(Race.OwnerBonus)} pour son champion." : "");
            await _arcade.EditAsync(race, PartyCards.RaceFrame(race, frames[^1], result, finished: true, winner: winner), null);

            var ping = s.Bets.Keys.Append(champion.OwnerId).Where(u => u != _arcade.BotId).Distinct().ToList();
            await _arcade.PostAsync(race, $"🏁 {champion.Name} gagne la course ! {Mentions(ping)}", ping);
        }
        finally
        {
            _tables.End(id);
        }
    }

    // ---- Crash ------------------------------------------------------------------------------------

    public void ScheduleCrash(CrashGame crash) => _arcade.Run($"crash {crash.Id}", async () =>
    {
        var wait = crash.LobbyEndsAt - DateTimeOffset.UtcNow;
        if (wait > TimeSpan.Zero) await Task.Delay(wait);
        await FlyAsync(crash.Id);
    });

    public void LaunchCrashNow(int id) => _arcade.Run($"crash {id}", () => FlyAsync(id));

    private async Task FlyAsync(int id)
    {
        var crash = _tables.With<CrashGame, CrashGame?>(id, c =>
        {
            if (c is null || c.Phase != CrashPhase.Lobby) return null;
            c.Phase = CrashPhase.Flying;
            c.LaunchedAt = DateTimeOffset.UtcNow;
            return c;
        });
        if (crash is null) return;

        if (_tables.With<CrashGame, int>(id, c => c?.Bets.Count ?? 0) == 0)
        {
            if (_tables.End(id))
                await _arcade.EditAsync(crash, PartyCards.CrashBoom(crash, Pick(BotResponses.CrashEmptyLines)), null);
            return;
        }

        while (true)
        {
            var m = Crash.MultiplierAt(DateTimeOffset.UtcNow - crash.LaunchedAt);
            var everyoneOut = _tables.With<CrashGame, bool>(id, c => c is not null && c.Bets.Keys.All(c.CashedOut.ContainsKey));
            if (m >= crash.CrashPoint || everyoneOut) break;
            var card = _tables.With<CrashGame, Embed>(id, c => PartyCards.CrashFlight(c!, m));
            await _arcade.EditAsync(crash, card, PartyCards.CrashFlightComponents(crash));
            await Task.Delay(Crash.Frame);
        }

        if (!_tables.End(id)) return;
        var (bets, cashed) = _tables.With<CrashGame, (Dictionary<ulong, long>, Dictionary<ulong, double>)>(id, _ =>
            (new Dictionary<ulong, long>(crash.Bets), new Dictionary<ulong, double>(crash.CashedOut)));
        var settled = await _arcade.SettleAsync(crash.GuildId, bets.Select(b => (b.Key, b.Value)).ToList(), payers =>
            payers.ToDictionary(p => p, p => cashed.TryGetValue(p, out var mult) ? Crash.Payout(bets[p], mult) - bets[p] : -bets[p]));
        foreach (var n in settled.Nets.Where(n => n.Value > 0))
            _badges.Increment(crash.GuildId, n.Key, MemberBadges.Keys.Arcade);   // cashed out ahead of the crash

        var line = crash.CrashPoint <= 1 ? Pick(BotResponses.CrashGroundedLines) : Pick(BotResponses.CrashBoomLines, $"×{crash.CrashPoint:0.00}");
        await _arcade.EditAsync(crash, PartyCards.CrashBoom(crash, line), null);
    }

    /// <summary>Cash-out at the multiplier of this very instant. Null when it's too late or not theirs.</summary>
    public (double Multiplier, long Payout)? CashOut(int id, ulong user)
    {
        return _tables.With<CrashGame, (double, long)?>(id, c =>
        {
            if (c is null || c.Phase != CrashPhase.Flying || !c.Bets.TryGetValue(user, out var stake) || c.CashedOut.ContainsKey(user))
                return null;
            var m = Crash.MultiplierAt(DateTimeOffset.UtcNow - c.LaunchedAt);
            if (m >= c.CrashPoint) return null;
            c.CashedOut[user] = m;
            return (m, Crash.Payout(stake, m));
        });
    }

    // ---- Blackjack --------------------------------------------------------------------------------

    public void WatchBlackjack(int id) => _arcade.Run($"blackjack {id}", async () =>
    {
        while (true)
        {
            await Task.Delay(Watch);
            var idle = _tables.With<BlackjackGame, bool?>(id, g => g is null ? null : DateTimeOffset.UtcNow - g.LastActionAt > Blackjack.IdleLimit);
            if (idle is null) return;
            if (idle == true)
            {
                var g = _tables.With<BlackjackGame, BlackjackGame?>(id, x => x);
                if (g is not null && _tables.End(id))
                {
                    var (embed, _) = await FinishBlackjackAsync(g, bust: false);
                    await _arcade.EditAsync(g, embed, null);
                }
                return;
            }
        }
    });

    /// <summary>
    /// Plays her hand (unless the player bust), settles and returns the final card. The caller has
    /// already claimed the end.
    /// </summary>
    public async Task<(Embed Embed, MessageComponent Components)> FinishBlackjackAsync(BlackjackGame g, bool bust)
    {
        if (!bust) Blackjack.PlayDealer(g.Dealer, Random.Shared);
        var outcome = Blackjack.Judge(g.Player, g.Dealer);
        var net = Blackjack.Net(outcome, g.Stake);
        var settled = await _arcade.SettleAsync(g.GuildId, new[] { (g.PlayerId, g.Stake) }, p =>
            p.ToDictionary(x => x, _ => net));
        // A natural or a win, settled (a torn-up hand settles nothing and counts for nothing).
        if (settled.Payers.Count > 0 && outcome is Blackjack.Outcome.Natural or Blackjack.Outcome.Win)
            _badges.Increment(g.GuildId, g.PlayerId, MemberBadges.Keys.Arcade);

        var pool = outcome switch
        {
            Blackjack.Outcome.Natural => BotResponses.BlackjackNaturalLines,
            Blackjack.Outcome.Win => BotResponses.BlackjackWinLines,
            Blackjack.Outcome.Push => BotResponses.BlackjackPushLines,
            _ when Blackjack.Total(g.Player) > 21 => BotResponses.BlackjackBustLines,
            _ => BotResponses.BlackjackLoseLines,
        };
        var money = settled.Payers.Count == 0
            ? "-# Plus de quoi payer la mise : je déchire tes cartes, rien ne bouge."
            : net switch
            {
                > 0 => $"-# <@{g.PlayerId}> gagne **{PebbleEconomy.Cailloux(net)}**.",
                < 0 => $"-# <@{g.PlayerId}> perd **{PebbleEconomy.Cailloux(-net)}**.",
                _ => "-# Égalité : la mise est rendue.",
            };
        return (PartyCards.BlackjackTable(g, reveal: true, $"{Pick(pool, $"<@{g.PlayerId}>")}\n{money}"),
            PartyCards.BlackjackButtons(g, over: true));
    }

    // ---- Bomb -------------------------------------------------------------------------------------

    public void ScheduleBomb(BombGame bomb) => _arcade.Run($"bomb {bomb.Id}", async () =>
    {
        var wait = bomb.LobbyEndsAt - DateTimeOffset.UtcNow;
        if (wait > TimeSpan.Zero) await Task.Delay(wait);
        await LightAsync(bomb.Id);
    });

    public void LightNow(int id) => _arcade.Run($"bomb {id}", () => LightAsync(id));

    private async Task LightAsync(int id)
    {
        var bomb = _tables.With<BombGame, BombGame?>(id, b =>
        {
            if (b is null || b.Lit || b.Players.Count < Bomb.MinPlayers) return b is { Lit: false } ? b : null;
            b.Lit = true;
            b.Holder = b.Players[Random.Shared.Next(b.Players.Count)];
            return b;
        });
        if (bomb is null) return;

        if (!bomb.Lit)
        {
            // Not enough players when the lobby closed.
            if (_tables.End(id))
                await _arcade.EditAsync(bomb, PartyCards.BombOver(Pick(BotResponses.BombCancelLines), "-# Personne ne paie.", exploded: false), null);
            return;
        }

        var line = Pick(BotResponses.BombLitLines);
        var players = _tables.With<BombGame, List<ulong>>(id, b => b!.Players.ToList());
        await _arcade.EditAsync(bomb, _tables.With<BombGame, Embed>(id, b => PartyCards.BombLive(b!, line)), PartyCards.BombLiveComponents(bomb));
        await _arcade.PostAsync(bomb, $"💣 La mèche est allumée ! {Mentions(players)}", players);

        await Task.Delay(Bomb.Fuse(Random.Shared));
        if (!_tables.End(id)) return;

        var loser = _tables.With<BombGame, ulong>(id, _ => bomb.Holder);
        var settled = await _arcade.SettleAsync(bomb.GuildId, players.Select(p => (p, Bomb.Stake)).ToList(), payers =>
            payers.Contains(loser) ? Bomb.Nets(payers, loser) : new Dictionary<ulong, long>());
        if (settled.Payers.Contains(loser))   // settled: everyone who paid in and did not hold the bomb wins
            foreach (var survivor in settled.Payers.Where(p => p != loser))
                _badges.Increment(bomb.GuildId, survivor, MemberBadges.Keys.Arcade);
        var details = settled.Payers.Contains(loser)
            ? $"-# <@{loser}> perd **{PebbleEconomy.Cailloux(Bomb.Stake)}** · les autres gagnent " +
              $"**{PebbleEconomy.Cailloux(Bomb.Nets(settled.Payers, loser).Values.Max())}** chacun."
            : "-# Celui ou celle qui tenait la bombe n'avait plus de quoi payer. Personne ne paie.";
        await _arcade.EditAsync(bomb, PartyCards.BombOver(Pick(BotResponses.BombBoomLines, $"<@{loser}>"), details, exploded: true), null);
    }

    // ---- Quote ------------------------------------------------------------------------------------

    public void ScheduleQuote(QuoteRound q) => _arcade.Run($"quote {q.Id}", async () =>
    {
        await Task.Delay(QuoteRules.Window);
        if (!_tables.End(q.Id)) return;
        await _arcade.EditAsync(q, PartyCards.QuoteCard(q, Pick(BotResponses.QuoteTimeoutLines, $"<@{q.AuthorId}>")), PartyCards.QuoteButtons(q, over: true));
    });

    // ---- Temperature ------------------------------------------------------------------------------

    public void ScheduleTemperature(TemperatureRound t) => _arcade.Run($"temperature {t.Id}", async () =>
    {
        var wait = t.EndsAt - DateTimeOffset.UtcNow;
        if (wait > TimeSpan.Zero) await Task.Delay(wait);
        if (!_tables.End(t.Id)) return;

        var guesses = _tables.With<TemperatureRound, Dictionary<ulong, double>>(t.Id, _ => new Dictionary<ulong, double>(t.Guesses));
        var milli = PiHardware.TryReadCpuMilliCelsius();
        if (milli is null || guesses.Count < TemperatureRules.MinPlayers)
        {
            await _arcade.EditAsync(t, PartyCards.TemperatureResult(t, Pick(BotResponses.TempCancelLines) + "\n-# Personne ne paie.", null, Array.Empty<ulong>()), null);
            return;
        }

        var actual = Math.Round(milli.Value / 1000.0, 1);
        List<ulong> winners = new();
        var settled = await _arcade.SettleAsync(t.GuildId, guesses.Keys.Select(k => (k, TemperatureRules.Stake)).ToList(), payers =>
        {
            var paying = guesses.Where(g => payers.Contains(g.Key)).ToDictionary(g => g.Key, g => g.Value);
            return paying.Count == 0 ? new Dictionary<ulong, long>() : TemperatureRules.Nets(paying, actual, out winners);
        });
        foreach (var winner in winners.Where(settled.Payers.Contains))   // settled above
            _badges.Increment(t.GuildId, winner, MemberBadges.Keys.Arcade);
        var line = Pick(BotResponses.TempWinLines, $"{actual:0.#} °C", Mentions(winners));
        await _arcade.EditAsync(t, PartyCards.TemperatureResult(t, line, actual, winners), null);
        await _arcade.PostAsync(t, $"🌡️ {actual:0.#} °C. {Mentions(guesses.Keys)}", guesses.Keys.ToList());
    });

    // ---- Boards -----------------------------------------------------------------------------------

    /// <summary>
    /// A challenge nobody answers expires; an accepted game whose player to move stays idle
    /// for five minutes is lost by that player.
    /// </summary>
    public void WatchBoard(int id) => _arcade.Run($"board {id}", async () =>
    {
        while (true)
        {
            await Task.Delay(Watch);
            var state = _tables.With<BoardGame, (bool Gone, bool Expired, bool Idle, BoardGame? Game)>(id, g =>
                g is null ? (true, false, false, null)
                : (false, !g.Accepted && DateTimeOffset.UtcNow - g.LastMoveAt > Duel.AcceptWindow,
                    g.Accepted && DateTimeOffset.UtcNow - g.LastMoveAt > Blackjack.IdleLimit, g));
            if (state.Gone) return;
            if (state.Expired && _tables.End(id))
            {
                var g = state.Game!;
                await _arcade.EditAsync(g, PartyCards.BoardCard(g, Pick(BotResponses.BoardExpiredLines)), null);
                return;
            }
            if (state.Idle)
            {
                var g = state.Game!;
                var idle = g.PlayerFor(g.Turn);
                var (embed, components) = await FinishBoardAsync(g, winnerTurn: 3 - g.Turn, reason: Pick(BotResponses.BoardTimeoutLines, $"<@{idle}>"));
                if (embed is not null) await _arcade.EditAsync(g, embed, components);
                return;
            }
        }
    });

    /// <summary>
    /// Ends a board game — <paramref name="winnerTurn"/> 1 or 2, or 0 for a draw — settles it and
    /// returns the final card; null when someone else already ended it.
    /// </summary>
    public async Task<(Embed? Embed, MessageComponent? Components)> FinishBoardAsync(BoardGame g, int winnerTurn, string? reason = null)
    {
        if (!_tables.End(g.Id)) return (null, null);

        string line;
        if (winnerTurn == 0)
            line = g.AgainstHer ? Pick(BotResponses.BoardHerDrawLines, $"<@{g.Player1}>") : Pick(BotResponses.BoardDrawLines);
        else if (g.AgainstHer)
            line = winnerTurn == 2 ? Pick(BotResponses.BoardHerWinLines, $"<@{g.Player1}>") : Pick(BotResponses.BoardBeatHerLines, $"<@{g.Player1}>");
        else
            line = Pick(BotResponses.BoardWinLines, $"<@{g.PlayerFor(winnerTurn)}>", $"<@{g.PlayerFor(3 - winnerTurn)}>");
        if (reason is not null) line = $"{reason}\n{line}";

        var money = "";
        var settledOk = winnerTurn != 0;   // a stake nobody could cover cancels the game below
        if (g.Stake > 0 && winnerTurn != 0)
        {
            var winner = g.PlayerFor(winnerTurn);
            var loser = g.PlayerFor(3 - winnerTurn);
            var gain = Duel.WinnerGain(g.Stake);
            // Against her, only the human's wallet moves: she pays from nowhere, and keeps nothing.
            var players = g.AgainstHer ? new[] { g.Player1 } : new[] { winner, loser };
            var settled = await _arcade.SettleAsync(g.GuildId, players.Select(p => (p, g.Stake)).ToList(), payers =>
                payers.Count < players.Length ? new Dictionary<ulong, long>()
                : players.ToDictionary(p => p, p => p == winner ? gain : -g.Stake));
            settledOk = settled.Payers.Count == players.Length;
            money = settled.Payers.Count < players.Length
                ? "\n-# Quelqu'un n'avait plus la mise : personne ne paie."
                : g.AgainstHer
                    ? (winner == g.Player1 ? $"\n-# <@{winner}> gagne **{PebbleEconomy.Cailloux(gain)}**." : $"\n-# <@{loser}> perd **{PebbleEconomy.Cailloux(g.Stake)}**.")
                    : $"\n-# <@{winner}> gagne **{PebbleEconomy.Cailloux(gain)}** · <@{loser}> perd **{PebbleEconomy.Cailloux(g.Stake)}**.";
        }

        // The winning human, once settled (stake 0 has nothing to settle). Her own wins earn nothing.
        if (settledOk && !(g.AgainstHer && winnerTurn == 2))
            _badges.Increment(g.GuildId, g.PlayerFor(winnerTurn), MemberBadges.Keys.Arcade);

        return (PartyCards.BoardCard(g, line + money), PartyCards.BoardButtons(g, over: true));
    }

    // ---- Quokka -----------------------------------------------------------------------------------

    public void ScheduleQuokka(QuokkaVisit v) => _arcade.Run($"quokka {v.Id}", async () =>
    {
        await Task.Delay(QuokkaRaid.Stay);
        if (!_tables.End(v.Id)) return;
        await _arcade.EditAsync(v, PartyCards.QuokkaCard(v, Pick(BotResponses.QuokkaFledLines)), PartyCards.QuokkaButtons(v, over: true));
    });
}
