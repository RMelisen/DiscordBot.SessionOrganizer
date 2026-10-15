using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// The /arcade cards' buttons. The games are in memory (ArcadeTables): a button whose game is gone
// — a restart, or it already ended — says so. Not a [Group] module, so no ignoreGroupNames.
public class ArcadeComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private const string Gone = "Cette partie n'existe plus (terminée, ou j'ai redémarré entre-temps). Personne n'a payé.";

    private readonly ArcadeTables _tables;
    private readonly ArcadeRunner _runner;
    private readonly ArcadeService _arcade;
    private readonly ResponsePicker _picker;

    public ArcadeComponentHandler(ArcadeTables tables, ArcadeRunner runner, ArcadeService arcade, ResponsePicker picker)
    {
        _tables = tables;
        _runner = runner;
        _arcade = arcade;
        _picker = picker;
    }

    private SocketMessageComponent Component => (SocketMessageComponent)Context.Interaction;

    // ---- Duel -------------------------------------------------------------------------------------

    [ComponentInteraction("duel:accept:*")]
    public async Task OnDuelAcceptAsync(string idStr)
    {
        if (!int.TryParse(idStr, out var id) || _tables.GetDuel(id) is not { } duel)
        {
            await RespondAsync(Gone, ephemeral: true);
            return;
        }
        if (Context.User.Id != duel.OpponentId)
        {
            await RespondAsync("Ce défi ne t'est pas adressé. Lance ton propre `/arcade duel`.", ephemeral: true);
            return;
        }

        var balance = await _arcade.BalanceAsync(duel.GuildId, Context.User.Id);
        if (balance < duel.Stake)
        {
            await RespondAsync($"Tu n'as que **{PebbleEconomy.Cailloux(balance)}**, la mise est de **{PebbleEconomy.Cailloux(duel.Stake)}**.",
                ephemeral: true);
            return;
        }

        var accepted = _tables.WithDuel(id, d =>
        {
            if (d is null || d.Accepted) return false;
            d.Accepted = true;
            d.PhaseStartedAt = DateTimeOffset.UtcNow;
            return true;
        });
        if (!accepted)
        {
            await RespondAsync("Ce duel a déjà commencé.", ephemeral: true);
            return;
        }

        var line = string.Format(_picker.Pick(BotResponses.DuelAcceptedLines), $"<@{duel.ChallengerId}>", $"<@{duel.OpponentId}>");
        await Component.UpdateAsync(p =>
        {
            p.Content = "";
            p.Embed = ArcadeCards.DuelEmbed(duel, line,
                $"Choisissez vos coups en secret · {Duel.PickWindow.TotalMinutes:0} minutes, sinon j'annule.");
            p.Components = ArcadeCards.DuelPickButton(duel);
        });
    }

    // The opponent refuses, or the challenger withdraws before an answer.
    [ComponentInteraction("duel:decline:*")]
    public async Task OnDuelDeclineAsync(string idStr)
    {
        if (!int.TryParse(idStr, out var id) || _tables.GetDuel(id) is not { } duel)
        {
            await RespondAsync(Gone, ephemeral: true);
            return;
        }
        if (!duel.IsPlayer(Context.User.Id))
        {
            await RespondAsync("Ce duel ne te concerne pas.", ephemeral: true);
            return;
        }
        if (duel.Accepted || !_tables.EndDuel(id))
        {
            await RespondAsync("Trop tard, le duel a commencé.", ephemeral: true);
            return;
        }

        var pool = Context.User.Id == duel.OpponentId ? BotResponses.DuelDeclinedLines : BotResponses.DuelWithdrawnLines;
        var line = string.Format(_picker.Pick(pool), $"<@{Context.User.Id}>");
        await Component.UpdateAsync(p =>
        {
            p.Content = "";
            p.Embed = ArcadeCards.DuelClosedEmbed(duel, line);
            p.Components = new ComponentBuilder().Build();
        });
    }

    // Opens the private row of moves.
    [ComponentInteraction("duel:pick:*")]
    public async Task OnDuelPickAsync(string idStr)
    {
        if (!int.TryParse(idStr, out var id) || _tables.GetDuel(id) is not { } duel)
        {
            await RespondAsync(Gone, ephemeral: true);
            return;
        }
        var (allowed, already) = _tables.WithDuel(id, d =>
            d is null ? (false, false) : (d.Accepted && d.IsPlayer(Context.User.Id), d.Picks.ContainsKey(Context.User.Id)));
        if (!allowed)
        {
            await RespondAsync("Ce duel ne te concerne pas.", ephemeral: true);
            return;
        }
        if (already)
        {
            await RespondAsync("Ton coup est déjà enregistré. On attend l'autre.", ephemeral: true);
            return;
        }

        await RespondAsync($"Manche {duel.Round} — ton coup, en secret :", components: ArcadeCards.DuelMoveButtons(duel), ephemeral: true);
    }

    [ComponentInteraction("duel:rps:*:*:*")]
    public async Task OnDuelMoveAsync(string idStr, string roundStr, string moveStr)
    {
        if (!int.TryParse(idStr, out var id) || !int.TryParse(roundStr, out var round)
            || !int.TryParse(moveStr, out var moveInt) || !Enum.IsDefined((DuelMove)moveInt))
        {
            await RespondAsync(Gone, ephemeral: true);
            return;
        }
        var move = (DuelMove)moveInt;

        // The pick and the check for « both have picked » are one step under the lock, so exactly
        // one click resolves the round.
        var outcome = _tables.WithDuel(id, d =>
        {
            if (d is null || !d.Accepted || !d.IsPlayer(Context.User.Id)) return MoveOutcome.Gone;
            if (d.Round != round) return MoveOutcome.StaleRound;
            if (!d.Picks.TryAdd(Context.User.Id, move)) return MoveOutcome.AlreadyPicked;
            if (d.AgainstHer) return MoveOutcome.Resolve;
            if (d.Picks.Count < 2) return MoveOutcome.Waiting;

            var a = d.Picks[d.ChallengerId];
            var b = d.Picks[d.OpponentId];
            if (Duel.Compare(a, b) != 0) return MoveOutcome.Resolve;

            // A tie: a new round, unless it was the last.
            d.LastTie = (a, b);
            if (d.Round >= Duel.MaxRounds) return MoveOutcome.Draw;
            d.Round++;
            d.Picks.Clear();
            d.PhaseStartedAt = DateTimeOffset.UtcNow;
            return MoveOutcome.Tie;
        });

        var duel = _tables.GetDuel(id);
        switch (outcome)
        {
            case MoveOutcome.Gone:
                await RespondAsync(Gone, ephemeral: true);
                return;
            case MoveOutcome.StaleRound:
                await RespondAsync("Cette manche est finie. Reclique sur « Choisir mon coup ».", ephemeral: true);
                return;
            case MoveOutcome.AlreadyPicked:
                await RespondAsync("Ton coup est déjà enregistré.", ephemeral: true);
                return;
        }

        await Component.UpdateAsync(p =>
        {
            p.Content = $"Coup enregistré : {Duel.Emoji(move)} **{Duel.Name(move)}**";
            p.Components = new ComponentBuilder().Build();
        });
        if (duel is null) return;

        switch (outcome)
        {
            case MoveOutcome.Waiting:
                return;

            case MoveOutcome.Tie:
                await _runner.EditAsync(duel.GuildId, duel.ChannelId, duel.MessageId,
                    ArcadeCards.DuelEmbed(duel, _picker.Pick(BotResponses.DuelTieLines)), ArcadeCards.DuelPickButton(duel));
                return;

            case MoveOutcome.Draw:
                if (!_tables.EndDuel(id)) return;
                await _runner.EditAsync(duel.GuildId, duel.ChannelId, duel.MessageId,
                    ArcadeCards.DuelClosedEmbed(duel, _picker.Pick(BotResponses.DuelDrawLines)), null);
                return;

            case MoveOutcome.Resolve:
                if (!_tables.EndDuel(id)) return;
                var challengerMove = duel.Picks[duel.ChallengerId];
                // Against her, she « picks » now. Having seen yours. She won't say so.
                var opponentMove = duel.AgainstHer ? Duel.CounterTo(challengerMove) : duel.Picks[duel.OpponentId];
                await _runner.FinishDuelAsync(duel, challengerMove, opponentMove);
                return;
        }
    }

    private enum MoveOutcome { Gone, StaleRound, AlreadyPicked, Waiting, Tie, Draw, Resolve }

    // ---- Heist ------------------------------------------------------------------------------------

    [ComponentInteraction("heist:join:*")]
    public async Task OnHeistJoinAsync(string idStr)
    {
        if (!int.TryParse(idStr, out var id) || _tables.WithHeist(id, h => h) is not { } heist)
        {
            await RespondAsync(Gone, ephemeral: true);
            return;
        }

        var balance = await _arcade.BalanceAsync(heist.GuildId, Context.User.Id);
        if (balance < Heist.Stake)
        {
            await RespondAsync($"Il faut **{PebbleEconomy.Cailloux(Heist.Stake)}** pour la cagoule, et tu n'as que **{PebbleEconomy.Cailloux(balance)}**.",
                ephemeral: true);
            return;
        }

        var (refusal, embed, components) = _tables.WithHeist<(string? Refusal, Embed? Embed, MessageComponent? Components)>(id, h =>
        {
            if (h is null || h.Launched) return ("Trop tard, l'équipe est déjà partie.", null, null);
            if (h.Crew.Contains(Context.User.Id)) return ("Tu fais déjà partie de l'équipe.", null, null);
            if (h.Crew.Count >= Heist.MaxCrew) return ("L'équipe est complète. Huit cagoules, pas une de plus.", null, null);
            h.Crew.Add(Context.User.Id);
            return (null, ArcadeCards.HeistEmbed(h), ArcadeCards.HeistButtons(h));
        });
        await ShowHeistAsync(refusal, embed, components);
    }

    [ComponentInteraction("heist:leave:*")]
    public async Task OnHeistLeaveAsync(string idStr)
    {
        if (!int.TryParse(idStr, out var id))
        {
            await RespondAsync(Gone, ephemeral: true);
            return;
        }

        var (refusal, embed, components) = _tables.WithHeist<(string? Refusal, Embed? Embed, MessageComponent? Components)>(id, h =>
        {
            if (h is null) return (Gone, null, null);
            if (h.Launched) return ("Trop tard, l'équipe est déjà partie.", null, null);
            if (h.OrganizerId == Context.User.Id) return ("C'est ton plan. Le cerveau ne se retire pas.", null, null);
            if (!h.Crew.Remove(Context.User.Id)) return ("Tu ne faisais pas partie de l'équipe.", null, null);
            return (null, ArcadeCards.HeistEmbed(h), ArcadeCards.HeistButtons(h));
        });
        await ShowHeistAsync(refusal, embed, components);
    }

    [ComponentInteraction("heist:go:*")]
    public async Task OnHeistGoAsync(string idStr)
    {
        if (!int.TryParse(idStr, out var id) || _tables.WithHeist(id, h => h) is not { } heist)
        {
            await RespondAsync(Gone, ephemeral: true);
            return;
        }
        if (heist.OrganizerId != Context.User.Id)
        {
            await RespondAsync($"C'est <@{heist.OrganizerId}> qui donne le signal.", ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        await DeferAsync();
        _runner.LaunchHeistNow(id);
    }

    private async Task ShowHeistAsync(string? refusal, Embed? embed, MessageComponent? components)
    {
        if (refusal is not null)
        {
            await RespondAsync(refusal, ephemeral: true);
            return;
        }
        await Component.UpdateAsync(p =>
        {
            p.Embed = embed;
            p.Components = components;
        });
    }
}
