using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Interactions.Modals;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// The Plynling card's two controls. A success rewrites the card in place, with her line
// on it, rather than posting a second message; a refusal goes privately to whoever
// pressed, and the card is left untouched.
public class PlynlingComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PlynlingCareService _care;
    private readonly PlynlingService _plynlings;
    private readonly PlynlingPlayService _play;
    private readonly ResponsePicker _picker;
    private readonly PlynlingCooldowns _cooldowns;

    public PlynlingComponentHandler(PlynlingCareService care, PlynlingService plynlings, PlynlingPlayService play,
        ResponsePicker picker, PlynlingCooldowns cooldowns)
    {
        _cooldowns = cooldowns;
        _care = care;
        _plynlings = plynlings;
        _play = play;
        _picker = picker;
    }

    // ---- /plynling visit: « Accueillir », pressed by the invited owner within the hour.
    [ComponentInteraction("plyn:visit:*:*:*", ignoreGroupNames: true)]
    public async Task OnVisitAcceptedAsync(string visitorStr, string hostStr, string expiresStr)
    {
        if (!int.TryParse(visitorStr, out var visitorId) || !ulong.TryParse(hostStr, out var hostOwnerId)
            || !long.TryParse(expiresStr, out var expiresUnix))
        {
            await RespondAsync(PlynlingText.Unknown, ephemeral: true);
            return;
        }
        if (Context.User.Id != hostOwnerId)
        {
            await RespondAsync(PlynlingText.NotYourInvite, ephemeral: true);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var component = (SocketMessageComponent)Context.Interaction;
        if (now > DateTimeOffset.FromUnixTimeSeconds(expiresUnix))
        {
            await component.UpdateAsync(m =>
            {
                m.Components = PlynlingPlayCards.BuildKnockClosed(PlynlingText.InviteExpired);
                m.Flags = MessageFlags.ComponentsV2;
                m.AllowedMentions = AllowedMentions.None;
            });
            return;
        }

        var visitor = await _plynlings.GetByIdAsync(visitorId, now);
        var host = await _plynlings.GetCurrentAsync(Context.Guild.Id, hostOwnerId, now);
        string? refusal =
            host is null || host.DiedAt is not null ? PlynlingText.NoPlynling
            : visitor is null || visitor.DiedAt is not null ? PlynlingText.VisitorGone
            : visitor.FrozenAt is not null || host.FrozenAt is not null ? PlynlingText.VisitFrozen
            : PlynlingLife.IsAsleep(now) ? PlynlingText.Asleep(host.Gender)
            : null;
        if (refusal is not null || visitor is null || host is null)
        {
            await RespondAsync(refusal, ephemeral: true);
            return;
        }

        var day = AppTime.DayKey(now);
        if (!_cooldowns.TryClaimVisit(visitor.OwnerId, hostOwnerId, day))
        {
            await RespondAsync(PlynlingText.VisitedToday(visitor.OwnerId), ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }
        var met = await _plynlings.VisitAsync(visitor.Id, host.Id, now);
        if (met is not { } pair)
        {
            _cooldowns.ReleaseVisit(visitor.OwnerId, hostOwnerId, day);
            await RespondAsync(PlynlingText.Unknown, ephemeral: true);
            return;
        }

        var line = string.Format(_picker.Pick(Context.Channel.Id, BotResponses.PlynlingVisitMeetLines.For(pair.Visitor.Gender)),
            PlynlingCardUi.SafeName(pair.Visitor.Name), PlynlingCardUi.SafeName(pair.Host.Name));
        foreach (var (who, badges) in new[] { (pair.Visitor, pair.VisitorBadges), (pair.Host, pair.HostBadges) })
            if (badges.Count > 0)
                line += "\n**" + PlynlingCardUi.SafeName(who.Name) + "** · " + PlynlingBadges.NewBadgeLines(badges, who.Gender);
        await component.UpdateAsync(m =>
        {
            m.Components = PlynlingPlayCards.BuildMeeting(pair.Visitor, pair.Host, line, now);
            m.Flags = MessageFlags.ComponentsV2;
            m.AllowedMentions = AllowedMentions.None;
        });
    }

    // ---- /plynling play: one handler per game's buttons, all through PlayMoveAsync.

    [ComponentInteraction("plyn:hide:*:*", ignoreGroupNames: true)]
    public Task OnHideAsync(string id, string rockStr) =>
        int.TryParse(rockStr, out var rock) && rock is >= 0 and < PlynlingGameState.Rocks
            ? PlayMoveAsync(id, s => s.Hide(rock, Random.Shared))
            : RespondAsync(PlynlingText.Unknown, ephemeral: true);

    [ComponentInteraction("plyn:rps:*:*", ignoreGroupNames: true)]
    public Task OnRpsAsync(string id, string throwStr) =>
        int.TryParse(throwStr, out var t) && Enum.IsDefined(typeof(RpsThrow), t)
            ? PlayMoveAsync(id, s => s.Throw((RpsThrow)t, Random.Shared))
            : RespondAsync(PlynlingText.Unknown, ephemeral: true);

    // « Deviner » opens the modal; the guess itself arrives in OnGuessAsync.
    [ComponentInteraction("plyn:guess:*", ignoreGroupNames: true)]
    public async Task OnGuessButtonAsync(string id)
    {
        var session = _play.Get(id, DateTimeOffset.UtcNow);
        if (session is null) await RespondAsync(PlynlingText.GameOver, ephemeral: true);
        else if (session.OwnerId != Context.User.Id) await RespondAsync(PlynlingText.NotYourGame, ephemeral: true);
        else await RespondWithModalAsync<GuessModal>($"plyn:guessm:{id}");
    }

    [ModalInteraction("plyn:guessm:*", ignoreGroupNames: true)]
    public Task OnGuessAsync(string id, GuessModal modal) =>
        int.TryParse(modal.Guess.Trim(), out var n) && n is >= PlynlingGameState.GuessMin and <= PlynlingGameState.GuessMax
            ? PlayMoveAsync(id, s => s.Guess(n))
            : RespondAsync(PlynlingText.GuessRange, ephemeral: true);

    // One move: checked (the game still running, the owner pressing), applied under the game's
    // lock — so a double click plays once, and exactly one move finishes it — then, if that move
    // ended the game, the reward is paid and her reaction added. The message is redrawn in place.
    private async Task PlayMoveAsync(string id, Action<PlynlingGameState> move)
    {
        var now = DateTimeOffset.UtcNow;
        var session = _play.Get(id, now);
        if (session is null)
        {
            await RespondAsync(PlynlingText.GameOver, ephemeral: true);
            return;
        }
        if (session.OwnerId != Context.User.Id)
        {
            await RespondAsync(PlynlingText.NotYourGame, ephemeral: true);
            return;
        }

        bool over, finished = false;
        lock (session.Gate)
        {
            over = session.State.Status != GameStatus.Playing;
            if (!over)
            {
                move(session.State);
                finished = session.State.Status != GameStatus.Playing;
                if (finished) _play.End(id);
            }
        }
        if (over)
        {
            await RespondAsync(PlynlingText.GameOver, ephemeral: true);
            return;
        }

        var plynling = await _plynlings.GetByIdAsync(session.PlynlingId, now);
        if (plynling is null)
        {
            await RespondAsync(PlynlingText.NoPlynling, ephemeral: true);   // abandoned mid-game
            return;
        }

        string? endLine = null;
        if (finished)
        {
            var won = session.State.Status == GameStatus.Won;
            var pebbles = won ? PlynlingLife.RollPlayPebbles(Random.Shared) : 0;
            var (after, balance, badges) = await _plynlings.FinishPlayAsync(plynling.Id, session.OwnerId, won, pebbles, now);
            if (after is not null)
            {
                plynling = after;
                var pool = (won ? BotResponses.PlynlingPlayPlayerWonLines : BotResponses.PlynlingPlayPlayerLostLines).For(plynling.Gender);
                endLine = string.Format(_picker.Pick(Context.Channel.Id, pool), PlynlingCardUi.SafeName(plynling.Name)) +
                          "\n" + PlynlingGameUi.Reward(won, pebbles, balance);
                if (badges.Count > 0) endLine += "\n" + PlynlingBadges.NewBadgeLines(badges, plynling.Gender);
            }
        }

        var card = PlynlingPlayCards.BuildGame(session, plynling, now, endLine);
        void Redraw(MessageProperties m)
        {
            m.Components = card;
            m.Flags = MessageFlags.ComponentsV2;      // re-asserted on every edit of a V2 message
            m.AllowedMentions = AllowedMentions.None;
        }
        if (Context.Interaction is SocketModal modal) await modal.UpdateAsync(Redraw);
        else await ((SocketMessageComponent)Context.Interaction).UpdateAsync(Redraw);
    }

    // /plynling list's pages. Two verbs for the two arrows; both land here and redraw the
    // list in place, re-read so a page always shows who is alive now.
    [ComponentInteraction("plyn:lprev:*", ignoreGroupNames: true)]
    public Task OnListPrevAsync(string pageStr) => ShowListAsync(pageStr);

    [ComponentInteraction("plyn:lnext:*", ignoreGroupNames: true)]
    public Task OnListNextAsync(string pageStr) => ShowListAsync(pageStr);

    private async Task ShowListAsync(string pageStr)
    {
        var now = DateTimeOffset.UtcNow;
        var living = await _plynlings.GetLivingAsync(Context.Guild.Id, now);
        var pages = PlynlingModule.ListPages(living.Count);
        var page = Math.Clamp(int.TryParse(pageStr, out var p) ? p : 0, 0, pages - 1);
        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(m =>
        {
            m.Embed = PlynlingModule.BuildListEmbed(living, page, now);
            m.Components = PlynlingModule.BuildListButtons(page, pages);
            m.AllowedMentions = AllowedMentions.None;
        });
    }

    [ComponentInteraction("plyn:pet:*", ignoreGroupNames: true)]
    public async Task OnPetAsync(string idStr)
    {
        if (!int.TryParse(idStr, out var id))
        {
            await RespondAsync(PlynlingText.Unknown, ephemeral: true);
            return;
        }
        await ApplyAsync(await _care.PetAsync(id, Context.User.Id, Context.Channel.Id, DateTimeOffset.UtcNow));
    }

    [ComponentInteraction("plyn:feed:*", ignoreGroupNames: true)]
    public async Task OnFeedAsync(string idStr, string[] selected)
    {
        if (!int.TryParse(idStr, out var id) || selected.Length == 0 || !Enum.TryParse<PlynlingFood>(selected[0], out var food))
        {
            await RespondAsync(PlynlingText.Unknown, ephemeral: true);
            return;
        }
        await ApplyAsync(await _care.FeedAsync(id, Context.User.Id, food, Context.Channel.Id, DateTimeOffset.UtcNow));
    }

    private async Task ApplyAsync(CareReply reply)
    {
        if (reply.Card is null)
        {
            await RespondAsync(reply.Refusal, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(m =>
        {
            m.Components = reply.Card;
            // Re-asserted on every edit: an update without it is rejected on a V2 message.
            m.Flags = MessageFlags.ComponentsV2;
            m.AllowedMentions = AllowedMentions.None;
        });
    }
}
