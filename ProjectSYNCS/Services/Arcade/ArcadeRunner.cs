using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>
/// Runs the /arcade games past the click that started them: the timers (a duel nobody answers, a
/// heist's departure), the reveal and the settlement. Singleton: it holds no state of its own —
/// that's <see cref="ArcadeTables"/> — and opens a scope around each settlement, like every
/// singleton here. Timers are fire-and-forget tasks that catch everything: a restart simply drops
/// them along with the games.
/// </summary>
public sealed class ArcadeRunner
{
    // Between two beats of a heist's narration: long enough to read, and well clear of the
    // message-edit rate limit.
    private static readonly TimeSpan BeatPause = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan DuelWatch = TimeSpan.FromSeconds(30);

    private readonly ArcadeTables _tables;
    private readonly IServiceProvider _services;
    private readonly DiscordSocketClient _client;
    private readonly ResponsePicker _picker;
    private readonly ILogger<ArcadeRunner> _logger;
    private readonly BadgeAwarder _badges;

    public ArcadeRunner(ArcadeTables tables, IServiceProvider services, DiscordSocketClient client,
        ResponsePicker picker, ILogger<ArcadeRunner> logger, BadgeAwarder badges)
    {
        _badges = badges;
        _tables = tables;
        _services = services;
        _client = client;
        _picker = picker;
        _logger = logger;
    }

    // ---- Duels ------------------------------------------------------------------------------------

    /// <summary>
    /// Watches a duel until it ends: unanswered after <see cref="Duel.AcceptWindow"/>, it expires;
    /// accepted but a move still missing after <see cref="Duel.PickWindow"/>, it's called off.
    /// Nobody pays either way.
    /// </summary>
    public void WatchDuel(int id) => _ = Task.Run(async () =>
    {
        try
        {
            while (true)
            {
                await Task.Delay(DuelWatch);
                var (gone, expired, accepted) = _tables.WithDuel(id, d =>
                {
                    if (d is null) return (true, false, false);
                    var window = d.Accepted ? Duel.PickWindow : Duel.AcceptWindow;
                    return (false, DateTimeOffset.UtcNow - d.PhaseStartedAt > window, d.Accepted);
                });
                if (gone) return;
                if (!expired) continue;

                var duel = _tables.GetDuel(id);
                if (duel is null || !_tables.EndDuel(id)) return;
                var line = _picker.Pick(accepted ? BotResponses.DuelAbandonedLines : BotResponses.DuelExpiredLines);
                await RepostDuelAsync(duel, ArcadeCards.DuelClosedEmbed(duel, line), null);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Duel {DuelId} watcher failed.", id);
        }
    });

    /// <summary>
    /// Settles a finished duel and redraws its card. The caller has already taken it off the table
    /// (<see cref="ArcadeTables.EndDuel"/>), so this runs once.
    /// </summary>
    public async Task FinishDuelAsync(DuelGame duel, DuelMove challengerMove, DuelMove opponentMove)
    {
        var verdict = duel.AgainstHer ? -1 : Duel.Compare(challengerMove, opponentMove);
        var winner = verdict > 0 ? duel.ChallengerId : duel.OpponentId;
        var loser = verdict > 0 ? duel.OpponentId : duel.ChallengerId;

        await using var scope = _services.CreateAsyncScope();
        var arcade = scope.ServiceProvider.GetRequiredService<ArcadeService>();

        string line, outcome;
        if (duel.AgainstHer)
        {
            var settled = await arcade.SettleAsync(duel.GuildId, new[] { duel.ChallengerId }, duel.Stake,
                payers => payers.ToDictionary(p => p, _ => -duel.Stake));
            line = string.Format(_picker.Pick(BotResponses.DuelHerWinLines), $"<@{duel.ChallengerId}>");
            outcome = settled.Payers.Count == 1
                ? $"-# <@{duel.ChallengerId}> perd **{PebbleEconomy.Cailloux(duel.Stake)}**."
                : $"-# <@{duel.ChallengerId}> n'avait plus de quoi payer. Je te fais grâce, pour cette fois.";
        }
        else
        {
            var gain = Duel.WinnerGain(duel.Stake);
            var settled = await arcade.SettleAsync(duel.GuildId, new[] { winner, loser }, duel.Stake,
                payers => payers.Count < 2
                    ? new Dictionary<ulong, long>()
                    : new Dictionary<ulong, long> { [winner] = gain, [loser] = -duel.Stake });
            if (settled.Payers.Count < 2)
            {
                line = _picker.Pick(BotResponses.DuelBrokeLines);
                outcome = $"-# {string.Join(" et ", settled.Dropped.Select(id => $"<@{id}>"))} n'a plus la mise. Duel annulé, personne ne paie.";
            }
            else
            {
                _badges.Increment(duel.GuildId, winner, MemberBadges.Keys.Arcade);   // settled: a real duel with a winner
                line = string.Format(_picker.Pick(BotResponses.DuelWinLines), $"<@{winner}>", $"<@{loser}>");
                outcome = $"-# <@{winner}> gagne **{PebbleEconomy.Cailloux(gain)}** · <@{loser}> perd " +
                          $"**{PebbleEconomy.Cailloux(duel.Stake)}** · l'arbitre garde {PebbleEconomy.Cailloux(Duel.Commission(duel.Stake))}.";
            }
        }

        await RepostDuelAsync(duel, ArcadeCards.DuelResultEmbed(duel, challengerMove, opponentMove, line, outcome), null);
    }

    /// <summary>
    /// Every step of a duel (accepted, a move played, a tie, the end) moves its card to the bottom of
    /// the channel: a new message, then the old one deleted, so the duel never ends up buried under
    /// the conversation. A new message also notifies, which an edit never does: <paramref name="ping"/>
    /// is who gets pinged (users only; nobody by default). Swallows and logs — a lost repost leaves
    /// the old card, which still works.
    /// </summary>
    public async Task RepostDuelAsync(DuelGame duel, Embed embed, MessageComponent? components,
        string? content = null, IReadOnlyCollection<ulong>? ping = null)
    {
        await duel.CardGate.WaitAsync();
        try
        {
            if (_client.GetGuild(duel.GuildId)?.GetTextChannel(duel.ChannelId) is not { } channel) return;
            var old = duel.MessageId;
            var message = await channel.SendMessageAsync(content, embed: embed,
                components: components ?? new ComponentBuilder().Build(),
                allowedMentions: ping is null ? AllowedMentions.None : new AllowedMentions { UserIds = ping.ToList() });
            duel.MessageId = message.Id;
            if (old != 0)
            {
                try
                {
                    await channel.DeleteMessageAsync(old);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex, "Old duel card {MessageId} not deleted.", old);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to repost duel {DuelId}'s card.", duel.Id);
        }
        finally
        {
            duel.CardGate.Release();
        }
    }

    // ---- Heists -----------------------------------------------------------------------------------

    // The departure timer. The organizer's button may launch it first; TryLaunchHeist decides.
    public void ScheduleHeist(HeistGame heist) => _ = Task.Run(async () =>
    {
        try
        {
            var wait = heist.EndsAt - DateTimeOffset.UtcNow;
            if (wait > TimeSpan.Zero) await Task.Delay(wait);
            await LaunchHeistAsync(heist.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Heist {HeistId} timer failed.", heist.Id);
        }
    });

    // The organizer's « Lancer le casse »: off the click, which has already been answered.
    public void LaunchHeistNow(int id) => _ = Task.Run(async () =>
    {
        try
        {
            await LaunchHeistAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Heist {HeistId} launch failed.", id);
        }
    });

    /// <summary>
    /// Runs the heist once (the timer and the button race; the loser does nothing): three beats on
    /// the card, then the roll, the settlement, the result, and a reply pinging the crew — they've
    /// waited up to 25 minutes and may have wandered off.
    /// </summary>
    public async Task LaunchHeistAsync(int id)
    {
        var heist = _tables.WithHeist(id, h => h);
        var crew = _tables.TryLaunchHeist(id);
        if (heist is null || crew is null) return;

        try
        {
            foreach (var beat in new[] { BotResponses.HeistBeatOneLines, BotResponses.HeistBeatTwoLines, BotResponses.HeistBeatThreeLines })
            {
                await EditAsync(heist.GuildId, heist.ChannelId, heist.MessageId,
                    ArcadeCards.HeistStoryEmbed(_picker.Pick(beat), crew), new ComponentBuilder().Build());
                await Task.Delay(BeatPause);
            }

            bool success = false;
            ulong? blamed = null;
            ArcadeSettlement settled;
            await using (var scope = _services.CreateAsyncScope())
            {
                var arcade = scope.ServiceProvider.GetRequiredService<ArcadeService>();
                settled = await arcade.SettleAsync(heist.GuildId, crew, Heist.Stake, payers =>
                {
                    if (payers.Count == 0) return new Dictionary<ulong, long>();
                    success = Random.Shared.NextDouble() < Heist.SuccessChance(payers.Count);
                    var net = success ? Heist.Payout(payers.Count) - Heist.Stake : -Heist.Stake;
                    if (!success) blamed = payers[Random.Shared.Next(payers.Count)];
                    return payers.ToDictionary(p => p, _ => (long)net);
                });
            }

            // Settled (saved) above: a successful heist is a win for each crew member who paid in.
            if (success)
                foreach (var member in settled.Payers) _badges.Increment(heist.GuildId, member, MemberBadges.Keys.Arcade);

            var story = new System.Text.StringBuilder();
            if (settled.Payers.Count == 0)
                story.Append(_picker.Pick(BotResponses.HeistBrokeLines));
            else if (success)
                story.Append(string.Format(_picker.Pick(BotResponses.HeistSuccessLines),
                    PebbleEconomy.Cailloux(Heist.Payout(settled.Payers.Count))));
            else
                story.Append(string.Format(_picker.Pick(BotResponses.HeistFailLines), $"<@{blamed}>"));

            if (settled.Dropped.Count > 0)
                story.Append($"\n\n-# Restés à la maison faute de quoi payer leur cagoule : " +
                             $"{string.Join(", ", settled.Dropped.Select(d => $"<@{d}>"))}.");
            if (settled.Payers.Count > 0)
                story.Append(success
                    ? $"\n-# Chacun repart avec **{PebbleEconomy.Cailloux(Heist.Payout(settled.Payers.Count))}** (mise comprise)."
                    : $"\n-# Chacun perd sa mise de **{PebbleEconomy.Cailloux(Heist.Stake)}**.");
            story.Append($"\n-# Quokka renforce sa sécurité : prochain casse <t:{(DateTimeOffset.UtcNow + Heist.Cooldown).ToUnixTimeSeconds()}:R>.");

            await EditAsync(heist.GuildId, heist.ChannelId, heist.MessageId,
                ArcadeCards.HeistStoryEmbed(story.ToString(), settled.Payers, settled.Payers.Count == 0 ? null : success),
                new ComponentBuilder().Build());
            await PingCrewAsync(heist, settled.Payers, success);
        }
        finally
        {
            _tables.EndHeist(id);
        }
    }

    private async Task PingCrewAsync(HeistGame heist, IReadOnlyList<ulong> crew, bool success)
    {
        if (crew.Count == 0) return;
        try
        {
            if (_client.GetGuild(heist.GuildId)?.GetTextChannel(heist.ChannelId) is not { } channel) return;
            var mentions = string.Join(" ", crew.Select(id => $"<@{id}>"));
            await channel.SendMessageAsync(
                $"{mentions} {(success ? "Le casse a réussi, venez voir." : "Le casse est terminé. Venez voir les dégâts.")}",
                allowedMentions: new AllowedMentions { UserIds = crew.ToList() },
                messageReference: heist.MessageId == 0 ? null : new MessageReference(heist.MessageId));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to ping heist {HeistId}'s crew.", heist.Id);
        }
    }

    // ---- Shared -----------------------------------------------------------------------------------

    /// <summary>
    /// Fire-and-forget for a game's timers and long narrations: off the click that started them,
    /// and catching everything, so a failure is logged and never escapes onto the gateway.
    /// </summary>
    public void Run(string what, Func<Task> work) => _ = Task.Run(async () =>
    {
        try
        {
            await work();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Arcade task failed: {What}.", what);
        }
    });

    /// <summary><see cref="ArcadeService.SettleAsync(ulong, IReadOnlyList{ValueTuple{ulong, long}}, Func{IReadOnlyList{ulong}, IReadOnlyDictionary{ulong, long}})"/> in its own scope.</summary>
    public async Task<ArcadeSettlement> SettleAsync(ulong guildId, IReadOnlyList<(ulong Player, long Stake)> stakes,
        Func<IReadOnlyList<ulong>, IReadOnlyDictionary<ulong, long>> decide)
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ArcadeService>().SettleAsync(guildId, stakes, decide);
    }

    /// <summary>
    /// A new message in the game's channel (an edit never notifies), pinging exactly
    /// <paramref name="ping"/> — users only. Swallows and logs.
    /// </summary>
    public async Task PostAsync(ArcadeGame game, string text, IReadOnlyCollection<ulong> ping)
    {
        try
        {
            if (_client.GetGuild(game.GuildId)?.GetTextChannel(game.ChannelId) is not { } channel) return;
            await channel.SendMessageAsync(text,
                allowedMentions: new AllowedMentions { UserIds = ping.ToList() },
                messageReference: game.MessageId == 0 ? null : new MessageReference(game.MessageId));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to post for arcade game {GameId}.", game.Id);
        }
    }

    /// <summary>Redraws a game's card; same contract as the duel and heist overload.</summary>
    public Task EditAsync(ArcadeGame game, Embed embed, MessageComponent? components) =>
        EditAsync(game.GuildId, game.ChannelId, game.MessageId, embed, components);

    public ResponsePicker Picker => _picker;

    // Her own id: she can own a racer (Ping-Qilin) but never collects a bonus.
    public ulong BotId => _client.CurrentUser?.Id ?? 0;

    // Discord side effects never break a game: a failed edit skips the frame and is logged.
    public async Task EditAsync(ulong guildId, ulong channelId, ulong messageId, Embed embed, MessageComponent? components)
    {
        if (messageId == 0) return;
        try
        {
            if (_client.GetGuild(guildId)?.GetTextChannel(channelId) is not { } channel) return;
            if (await channel.GetMessageAsync(messageId) is not IUserMessage message) return;
            await message.ModifyAsync(p =>
            {
                p.Embed = embed;
                p.Components = components ?? new ComponentBuilder().Build();
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to edit arcade card {MessageId}.", messageId);
        }
    }
}
