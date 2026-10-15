using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Interactions.Modals;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// The buttons of the newer /arcade games (race, crash, blackjack, bomb, quote, temperature,
// boards) and of Quokka's visits. The games are in memory (ArcadeTables): a button whose game is
// gone says so. Not a [Group] module, so no ignoreGroupNames. A click that reads a wallet does it
// before answering — one small read, like /work.
public class PartyComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private const string Gone = "Cette partie n'existe plus (terminée, ou j'ai redémarré entre-temps).";

    private readonly ArcadeTables _tables;
    private readonly ArcadeRunner _arcade;
    private readonly PartyRunner _party;
    private readonly ArcadeService _wallets;
    private readonly ResponsePicker _picker;

    public PartyComponentHandler(ArcadeTables tables, ArcadeRunner arcade, PartyRunner party, ArcadeService wallets, ResponsePicker picker)
    {
        _tables = tables;
        _arcade = arcade;
        _party = party;
        _wallets = wallets;
        _picker = picker;
    }

    private SocketMessageComponent Component => (SocketMessageComponent)Context.Interaction;

    private string Pick(string[] pool, params object[] args) =>
        args.Length == 0 ? _picker.Pick(pool) : string.Format(_picker.Pick(pool), args);

    private async Task<bool> CanPayAsync(ulong guildId, long stake)
    {
        var balance = await _wallets.BalanceAsync(guildId, Context.User.Id);
        if (balance >= stake) return true;
        await RespondAsync($"Il faut **{PebbleEconomy.Cailloux(stake)}**, et tu n'as que **{PebbleEconomy.Cailloux(balance)}**.", ephemeral: true);
        return false;
    }

    private Task UpdateCardAsync(Embed embed, MessageComponent components) =>
        Component.UpdateAsync(p =>
        {
            p.Embed = embed;
            p.Components = components;
        });

    // ---- Race -------------------------------------------------------------------------------------

    [ComponentInteraction("race:pick:*")]
    public async Task OnRacePickAsync(string idStr, string[] values)
    {
        if (!int.TryParse(idStr, out var id) || values.Length == 0 || !int.TryParse(values[0], out var runner)
            || _tables.With<RaceGame, RaceGame?>(id, r => r is { Started: false } ? r : null) is not { } race
            || runner < 0 || runner >= race.Runners.Count)
        {
            await RespondAsync("Les paris sont fermés.", ephemeral: true);
            return;
        }
        await RespondAsync($"Combien sur **{race.Runners[runner].Name}** ?", components: PartyCards.RaceStakeButtons(race, runner),
            ephemeral: true, allowedMentions: AllowedMentions.None);
    }

    [ComponentInteraction("race:bet:*:*:*")]
    public async Task OnRaceBetAsync(string idStr, string runnerStr, string stakeStr)
    {
        if (!int.TryParse(idStr, out var id) || !int.TryParse(runnerStr, out var runner) || !long.TryParse(stakeStr, out var stake)
            || !Race.Stakes.Contains(stake) || _tables.With<RaceGame, RaceGame?>(id, r => r) is not { } race)
        {
            await RespondAsync("Les paris sont fermés.", ephemeral: true);
            return;
        }
        if (!await CanPayAsync(race.GuildId, stake)) return;

        var placed = _tables.With<RaceGame, bool>(id, r =>
        {
            if (r is null || r.Started || runner < 0 || runner >= r.Runners.Count) return false;
            r.Bets[Context.User.Id] = (runner, stake);
            return true;
        });
        if (!placed)
        {
            await RespondAsync("Trop tard, ils sont partis !", ephemeral: true);
            return;
        }

        await Component.UpdateAsync(p =>
        {
            p.Content = $"Pari enregistré : **{PebbleEconomy.Cailloux(stake)}** sur **{race.Runners[runner].Name}**. " +
                        "Tu peux changer d'avis jusqu'au départ (un seul pari par personne).";
            p.Components = new ComponentBuilder().Build();
        });
        var card = _tables.With<RaceGame, Embed?>(id, r => r is null ? null : PartyCards.RaceLobby(r));
        if (card is not null) await _arcade.EditAsync(race, card, PartyCards.RaceLobbyComponents(race));
    }

    // ---- Crash ------------------------------------------------------------------------------------

    [ComponentInteraction("crash:bet:*:*")]
    public async Task OnCrashBetAsync(string idStr, string stakeStr)
    {
        if (!int.TryParse(idStr, out var id) || !long.TryParse(stakeStr, out var stake) || !Crash.Stakes.Contains(stake)
            || _tables.With<CrashGame, CrashGame?>(id, c => c) is not { } crash)
        {
            await RespondAsync(Gone, ephemeral: true);
            return;
        }
        if (!await CanPayAsync(crash.GuildId, stake)) return;

        var card = _tables.With<CrashGame, Embed?>(id, c =>
        {
            if (c is null || c.Phase != CrashPhase.Lobby) return null;
            c.Bets[Context.User.Id] = stake;
            return PartyCards.CrashLobby(c);
        });
        if (card is null)
        {
            await RespondAsync("Trop tard, la fusée a décollé.", ephemeral: true);
            return;
        }
        await UpdateCardAsync(card, PartyCards.CrashLobbyComponents(crash));
    }

    [ComponentInteraction("crash:go:*")]
    public async Task OnCrashGoAsync(string idStr)
    {
        if (!int.TryParse(idStr, out var id) || _tables.With<CrashGame, CrashGame?>(id, c => c) is not { } crash)
        {
            await RespondAsync(Gone, ephemeral: true);
            return;
        }
        if (crash.OrganizerId != Context.User.Id)
        {
            await RespondAsync($"C'est <@{crash.OrganizerId}> qui a les commandes.", ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }
        await DeferAsync();
        _party.LaunchCrashNow(id);
    }

    [ComponentInteraction("crash:cash:*")]
    public async Task OnCrashCashAsync(string idStr)
    {
        if (!int.TryParse(idStr, out var id) || _party.CashOut(id, Context.User.Id) is not { } cashed)
        {
            await RespondAsync("Trop tard, ou tu n'étais pas à bord.", ephemeral: true);
            return;
        }
        await RespondAsync($"Encaissé à **×{cashed.Multiplier:0.00}** : tu récupères **{PebbleEconomy.Cailloux(cashed.Payout)}** à l'atterrissage.",
            ephemeral: true);
    }

    // ---- Blackjack --------------------------------------------------------------------------------

    [ComponentInteraction("bj:hit:*")]
    public Task OnHitAsync(string idStr) => BlackjackAsync(idStr, BjAction.Hit);

    [ComponentInteraction("bj:stand:*")]
    public Task OnStandAsync(string idStr) => BlackjackAsync(idStr, BjAction.Stand);

    [ComponentInteraction("bj:double:*")]
    public Task OnDoubleAsync(string idStr) => BlackjackAsync(idStr, BjAction.Double);

    private enum BjAction { Hit, Stand, Double }

    private async Task BlackjackAsync(string idStr, BjAction action)
    {
        if (!int.TryParse(idStr, out var id) || _tables.With<BlackjackGame, BlackjackGame?>(id, g => g) is not { } game)
        {
            await RespondAsync(Gone, ephemeral: true);
            return;
        }
        if (game.PlayerId != Context.User.Id)
        {
            await RespondAsync("Ce n'est pas ta main. Lance la tienne avec `/arcade blackjack`.", ephemeral: true);
            return;
        }
        if (action == BjAction.Double && !await CanPayAsync(game.GuildId, game.Stake * 2)) return;

        // The draw and « is it over? » are one step.
        var over = _tables.With<BlackjackGame, (bool Over, bool Bust)?>(id, g =>
        {
            if (g is null) return null;
            g.LastActionAt = DateTimeOffset.UtcNow;
            switch (action)
            {
                case BjAction.Hit:
                    g.Player.Add(Blackjack.Draw(Random.Shared));
                    var total = Blackjack.Total(g.Player);
                    return (total >= 21, total > 21);
                case BjAction.Double when g.Player.Count == 2 && !g.Doubled:
                    g.Doubled = true;
                    g.Stake *= 2;
                    g.Player.Add(Blackjack.Draw(Random.Shared));
                    return (true, Blackjack.Total(g.Player) > 21);
                case BjAction.Double:
                    return (false, false);
                default:
                    return (true, false);
            }
        });
        if (over is null)
        {
            await RespondAsync(Gone, ephemeral: true);
            return;
        }

        if (!over.Value.Over)
        {
            await UpdateCardAsync(PartyCards.BlackjackTable(game, reveal: false), PartyCards.BlackjackButtons(game));
            return;
        }
        if (!_tables.End(id))
        {
            await RespondAsync("Cette main est déjà terminée.", ephemeral: true);
            return;
        }
        await DeferAsync();
        var (embed, components) = await _party.FinishBlackjackAsync(game, over.Value.Bust);
        await Component.ModifyOriginalResponseAsync(p =>
        {
            p.Embed = embed;
            p.Components = components;
        });
    }

    // ---- Bomb -------------------------------------------------------------------------------------

    [ComponentInteraction("bomb:join:*")]
    public async Task OnBombJoinAsync(string idStr)
    {
        if (!int.TryParse(idStr, out var id) || _tables.With<BombGame, BombGame?>(id, b => b) is not { } bomb)
        {
            await RespondAsync(Gone, ephemeral: true);
            return;
        }
        if (!await CanPayAsync(bomb.GuildId, Bomb.Stake)) return;

        var (refusal, embed) = _tables.With<BombGame, (string?, Embed?)>(id, b =>
        {
            if (b is null || b.Lit) return ("Trop tard, la mèche est allumée.", null);
            if (b.Players.Contains(Context.User.Id)) return ("Tu joues déjà.", null);
            if (b.Players.Count >= Bomb.MaxPlayers) return ("C'est complet.", null);
            b.Players.Add(Context.User.Id);
            return (null, PartyCards.BombLobby(b));
        });
        if (refusal is not null) await RespondAsync(refusal, ephemeral: true);
        else await UpdateCardAsync(embed!, PartyCards.BombLobbyComponents(bomb));
    }

    [ComponentInteraction("bomb:leave:*")]
    public async Task OnBombLeaveAsync(string idStr)
    {
        if (!int.TryParse(idStr, out var id) || _tables.With<BombGame, BombGame?>(id, b => b) is not { } bomb)
        {
            await RespondAsync(Gone, ephemeral: true);
            return;
        }
        var (refusal, embed) = _tables.With<BombGame, (string?, Embed?)>(id, b =>
        {
            if (b is null || b.Lit) return ("Trop tard, la mèche est allumée.", null);
            if (b.OrganizerId == Context.User.Id) return ("C'est ta bombe. Tu restes.", null);
            if (!b.Players.Remove(Context.User.Id)) return ("Tu ne jouais pas.", null);
            return (null, PartyCards.BombLobby(b));
        });
        if (refusal is not null) await RespondAsync(refusal, ephemeral: true);
        else await UpdateCardAsync(embed!, PartyCards.BombLobbyComponents(bomb));
    }

    [ComponentInteraction("bomb:go:*")]
    public async Task OnBombGoAsync(string idStr)
    {
        if (!int.TryParse(idStr, out var id) || _tables.With<BombGame, BombGame?>(id, b => b) is not { } bomb)
        {
            await RespondAsync(Gone, ephemeral: true);
            return;
        }
        if (bomb.OrganizerId != Context.User.Id)
        {
            await RespondAsync($"C'est <@{bomb.OrganizerId}> qui allume la mèche.", ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }
        if (_tables.With<BombGame, int>(id, b => b?.Players.Count ?? 0) < Bomb.MinPlayers)
        {
            await RespondAsync($"Il faut au moins {Bomb.MinPlayers} joueurs.", ephemeral: true);
            return;
        }
        await DeferAsync();
        _party.LightNow(id);
    }

    [ComponentInteraction("bomb:pass:*")]
    public async Task OnBombPassAsync(string idStr)
    {
        if (!int.TryParse(idStr, out var id))
        {
            await RespondAsync(Gone, ephemeral: true);
            return;
        }
        var (refusal, embed, bomb) = _tables.With<BombGame, (string?, Embed?, BombGame?)>(id, b =>
        {
            if (b is null || !b.Lit) return ("Trop tard. BOUM.", null, null);
            if (b.Holder != Context.User.Id) return ("Ce n'est pas toi qui as la bombe. Pour l'instant.", null, null);
            var others = b.Players.Where(p => p != b.Holder).ToList();
            var to = others[Random.Shared.Next(others.Count)];
            var from = b.Holder;
            b.Holder = to;
            b.Passes++;
            return (null, PartyCards.BombLive(b, Pick(BotResponses.BombPassLines, $"<@{from}>", $"<@{to}>")), b);
        });
        if (refusal is not null) await RespondAsync(refusal, ephemeral: true);
        else await UpdateCardAsync(embed!, PartyCards.BombLiveComponents(bomb!));
    }

    // ---- Quote ------------------------------------------------------------------------------------

    [ComponentInteraction("quote:guess:*:*")]
    public async Task OnQuoteGuessAsync(string idStr, string choiceStr)
    {
        if (!int.TryParse(idStr, out var id) || !ulong.TryParse(choiceStr, out var choice)
            || _tables.With<QuoteRound, QuoteRound?>(id, q => q) is not { } round)
        {
            await RespondAsync("Cette devinette est terminée.", ephemeral: true);
            return;
        }
        if (Context.User.Id == round.AuthorId)
        {
            await RespondAsync(Pick(BotResponses.QuoteSelfLines), ephemeral: true);
            return;
        }

        var verdict = _tables.With<QuoteRound, bool?>(id, q =>
        {
            if (q is null || q.Out.Contains(Context.User.Id)) return null;
            if (choice == q.AuthorId) return true;
            q.Out.Add(Context.User.Id);
            return false;
        });
        switch (verdict)
        {
            case null:
                await RespondAsync("Tu as déjà joué ta carte sur celle-là.", ephemeral: true);
                return;
            case false:
                await RespondAsync(Pick(BotResponses.QuoteWrongLines), ephemeral: true);
                return;
        }

        if (!_tables.End(id))
        {
            await RespondAsync("Quelqu'un a trouvé juste avant toi.", ephemeral: true);
            return;
        }
        await DeferAsync();
        await _arcade.SettleAsync(round.GuildId, new[] { (Context.User.Id, 0L) }, p => p.ToDictionary(x => x, _ => QuoteRules.Reward));
        var result = Pick(BotResponses.QuoteWinLines, $"<@{Context.User.Id}>", $"<@{round.AuthorId}>") +
                     $"\n-# +{PebbleEconomy.Cailloux(QuoteRules.Reward)} pour <@{Context.User.Id}>.";
        await Component.ModifyOriginalResponseAsync(p =>
        {
            p.Embed = PartyCards.QuoteCard(round, result);
            p.Components = PartyCards.QuoteButtons(round, over: true);
        });
    }

    // ---- Temperature ------------------------------------------------------------------------------

    [ComponentInteraction("temp:open:*")]
    public async Task OnTemperatureOpenAsync(string idStr)
    {
        if (!int.TryParse(idStr, out var id) || _tables.With<TemperatureRound, TemperatureRound?>(id, t => t) is not { } round)
        {
            await RespondAsync("Ce pari est terminé.", ephemeral: true);
            return;
        }
        if (!await CanPayAsync(round.GuildId, TemperatureRules.Stake)) return;
        await RespondWithModalAsync<TemperatureGuessModal>($"temp:guess:{id}");
    }

    [ModalInteraction("temp:guess:*")]
    public async Task OnTemperatureGuessAsync(string idStr, TemperatureGuessModal modal)
    {
        var guess = TemperatureRules.ParseGuess(modal.Guess);
        if (guess is null)
        {
            await RespondAsync($"Un nombre entre {TemperatureRules.MinGuess:0} et {TemperatureRules.MaxGuess:0}, s'il te plaît. Je suis une bot, pas une étoile.", ephemeral: true);
            return;
        }
        if (!int.TryParse(idStr, out var id))
        {
            await RespondAsync("Ce pari est terminé.", ephemeral: true);
            return;
        }
        var (round, card) = _tables.With<TemperatureRound, (TemperatureRound?, Embed?)>(id, t =>
        {
            if (t is null) return (null, null);
            t.Guesses[Context.User.Id] = guess.Value;
            return (t, PartyCards.TemperatureCard(t));
        });
        if (round is null)
        {
            await RespondAsync("Trop tard, j'ai déjà regardé mon thermomètre.", ephemeral: true);
            return;
        }
        await RespondAsync($"Pari enregistré : **{guess.Value:0.#} °C**. Tu peux le changer jusqu'au résultat.", ephemeral: true);
        await _arcade.EditAsync(round, card!, PartyCards.TemperatureButtons(round));
    }

    // ---- Boards -----------------------------------------------------------------------------------

    [ComponentInteraction("board:accept:*")]
    public async Task OnBoardAcceptAsync(string idStr)
    {
        if (!int.TryParse(idStr, out var id) || _tables.With<BoardGame, BoardGame?>(id, g => g) is not { } game)
        {
            await RespondAsync(Gone, ephemeral: true);
            return;
        }
        if (Context.User.Id != game.Player2)
        {
            await RespondAsync("Ce défi ne t'est pas adressé.", ephemeral: true);
            return;
        }
        if (game.Stake > 0 && !await CanPayAsync(game.GuildId, game.Stake)) return;

        var accepted = _tables.With<BoardGame, bool>(id, g =>
        {
            if (g is null || g.Accepted) return false;
            g.Accepted = true;
            g.LastMoveAt = DateTimeOffset.UtcNow;
            return true;
        });
        if (!accepted)
        {
            await RespondAsync("Cette partie a déjà commencé.", ephemeral: true);
            return;
        }
        await Component.UpdateAsync(p =>
        {
            p.Content = "";
            p.Embed = PartyCards.BoardCard(game);
            p.Components = PartyCards.BoardButtons(game);
        });
        try
        {
            await FollowupAsync($"<@{game.Player1}>, <@{game.Player2}> a accepté ! À toi de commencer.",
                allowedMentions: new AllowedMentions { UserIds = new List<ulong> { game.Player1 } });
        }
        catch
        {
            // The card already says so.
        }
    }

    [ComponentInteraction("board:decline:*")]
    public async Task OnBoardDeclineAsync(string idStr)
    {
        if (!int.TryParse(idStr, out var id) || _tables.With<BoardGame, BoardGame?>(id, g => g) is not { } game)
        {
            await RespondAsync(Gone, ephemeral: true);
            return;
        }
        if (!game.IsPlayer(Context.User.Id))
        {
            await RespondAsync("Ce défi ne te concerne pas.", ephemeral: true);
            return;
        }
        if (game.Accepted || !_tables.End(id))
        {
            await RespondAsync("Trop tard, la partie a commencé.", ephemeral: true);
            return;
        }
        await Component.UpdateAsync(p =>
        {
            p.Content = "";
            p.Embed = PartyCards.BoardCard(game, Pick(BotResponses.BoardDeclinedLines, $"<@{Context.User.Id}>"));
            p.Components = new ComponentBuilder().Build();
        });
    }

    [ComponentInteraction("board:quit:*")]
    public async Task OnBoardQuitAsync(string idStr)
    {
        if (!int.TryParse(idStr, out var id) || _tables.With<BoardGame, BoardGame?>(id, g => g) is not { Accepted: true } game
            || !game.IsPlayer(Context.User.Id))
        {
            await RespondAsync("Rien à abandonner ici.", ephemeral: true);
            return;
        }
        await DeferAsync();
        var quitterTurn = game.Player1 == Context.User.Id ? 1 : 2;
        var (embed, components) = await _party.FinishBoardAsync(game, 3 - quitterTurn,
            Pick(BotResponses.BoardForfeitLines, $"<@{Context.User.Id}>", $"<@{game.PlayerFor(3 - quitterTurn)}>"));
        if (embed is null) return;
        await Component.ModifyOriginalResponseAsync(p =>
        {
            p.Embed = embed;
            p.Components = components;
        });
    }

    [ComponentInteraction("c4:drop:*:*")]
    public Task OnConnect4Async(string idStr, string colStr) => BoardMoveAsync(idStr, colStr, BoardKind.Connect4);

    [ComponentInteraction("ttt:play:*:*")]
    public Task OnTicTacToeAsync(string idStr, string cellStr) => BoardMoveAsync(idStr, cellStr, BoardKind.TicTacToe);

    private async Task BoardMoveAsync(string idStr, string moveStr, BoardKind kind)
    {
        if (!int.TryParse(idStr, out var id) || !int.TryParse(moveStr, out var move))
        {
            await RespondAsync(Gone, ephemeral: true);
            return;
        }

        // The move, her answer and « is it over? » are one step under the lock.
        var outcome = _tables.With<BoardGame, (string? Refusal, int Result, BoardGame? Game)>(id, g =>
        {
            if (g is null || !g.Accepted || g.Kind != kind) return (Gone, -1, null);
            if (!g.IsPlayer(Context.User.Id)) return ("Tu ne joues pas dans cette partie.", -1, null);
            if (g.PlayerFor(g.Turn) != Context.User.Id) return ("Ce n'est pas ton tour.", -1, null);
            if (!Play(g, move)) return ("Ce coup n'est pas possible.", -1, null);
            var result = Result(g);
            if (result < 0 && g.AgainstHer && g.Turn == 2)
            {
                HerMove(g);
                result = Result(g);
                // One game in five of Puissance 4, she slips a second piece in. Nobody saw anything.
                if (result < 0 && g.CheatPending && g.Moves >= 6 && g.Turn == 1)
                {
                    g.CheatPending = false;
                    g.Turn = 2;
                    HerMove(g);
                    g.Line = Pick(BotResponses.BoardCheatLines);
                    result = Result(g);
                }
            }
            return (null, result, g);
        });

        if (outcome.Refusal is not null)
        {
            await RespondAsync(outcome.Refusal, ephemeral: true);
            return;
        }
        var game = outcome.Game!;
        if (outcome.Result < 0)
        {
            await UpdateCardAsync(PartyCards.BoardCard(game), PartyCards.BoardButtons(game));
            return;
        }

        await DeferAsync();
        var (embed, components) = await _party.FinishBoardAsync(game, outcome.Result);
        if (embed is null) return;
        await Component.ModifyOriginalResponseAsync(p =>
        {
            p.Embed = embed;
            p.Components = components;
        });
    }

    // Applies the mover's piece and hands the turn over; false for an illegal move.
    private static bool Play(BoardGame g, int move)
    {
        if (g.Kind == BoardKind.Connect4)
        {
            if (Connect4.Drop(g.Board, move, g.Turn) < 0) return false;
        }
        else
        {
            if (move is < 0 or > 8 || g.Board[move] != 0) return false;
            g.Board[move] = g.Turn;
        }
        g.Moves++;
        g.Turn = 3 - g.Turn;
        g.LastMoveAt = DateTimeOffset.UtcNow;
        return true;
    }

    private static void HerMove(BoardGame g)
    {
        var move = g.Kind == BoardKind.Connect4
            ? Connect4.BestMove(g.Board, 2, Random.Shared)
            : TicTacToe.BestMove(g.Board, 2, Random.Shared);
        if (move >= 0) Play(g, move);
    }

    // −1 while it goes on, 0 for a draw, else the winning side.
    private static int Result(BoardGame g)
    {
        var winner = g.Kind == BoardKind.Connect4 ? Connect4.Winner(g.Board) : TicTacToe.Winner(g.Board);
        if (winner != 0) return winner;
        var full = g.Kind == BoardKind.Connect4 ? Connect4.Full(g.Board) : TicTacToe.Full(g.Board);
        return full ? 0 : -1;
    }

    // ---- Quokka -----------------------------------------------------------------------------------

    [ComponentInteraction("quokka:hit:*")]
    public async Task OnQuokkaHitAsync(string idStr)
    {
        if (!int.TryParse(idStr, out var id) || _tables.With<QuokkaVisit, QuokkaVisit?>(id, v => v) is not { } visit
            || !_tables.End(id))
        {
            await RespondAsync(Pick(BotResponses.QuokkaTooLateLines), ephemeral: true);
            return;
        }
        await DeferAsync();
        await _arcade.SettleAsync(visit.GuildId, new[] { (Context.User.Id, 0L) }, p => p.ToDictionary(x => x, _ => visit.Reward));
        await Component.ModifyOriginalResponseAsync(p =>
        {
            p.Embed = PartyCards.QuokkaCard(visit, Pick(BotResponses.QuokkaHitLines, $"<@{Context.User.Id}>", PebbleEconomy.Cailloux(visit.Reward)));
            p.Components = PartyCards.QuokkaButtons(visit, over: true);
        });
    }
}
