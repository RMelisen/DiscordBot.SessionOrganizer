using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
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
    private readonly TradeOffers _trades;
    private readonly InventoryService _inventory;
    private readonly VisitStories _stories;
    private readonly PlynlingVisitRunner _visits;
    private readonly ILogger<PlynlingComponentHandler> _logger;

    // The pause between two beats of a visit's story — long enough to read one, short enough
    // that nobody wonders whether it is stuck.

    public PlynlingComponentHandler(PlynlingCareService care, PlynlingService plynlings, PlynlingPlayService play,
        ResponsePicker picker, PlynlingCooldowns cooldowns, TradeOffers trades, InventoryService inventory,
        VisitStories stories, PlynlingVisitRunner visits, ILogger<PlynlingComponentHandler> logger)
    {
        _stories = stories;
        _visits = visits;
        _logger = logger;
        _trades = trades;
        _inventory = inventory;
        _cooldowns = cooldowns;
        _care = care;
        _plynlings = plynlings;
        _play = play;
        _picker = picker;
    }

    // ---- /inventory trade: « Accepter » for the recipient only; « Refuser » for the recipient
    // (declining) or the proposer (withdrawing). The card is rewritten with the result and loses
    // its buttons; a refusal to anyone else is private and leaves it alone.
    [ComponentInteraction("plyn:tacc:*", ignoreGroupNames: true)]
    public async Task OnTradeAcceptAsync(string id)
    {
        var now = DateTimeOffset.UtcNow;
        var open = _trades.Get(id, now);
        if (open is null)
        {
            await CloseTradeCardAsync(PlynlingText.TradeGone);
            return;
        }
        if (Context.User.Id != open.ToId)
        {
            await RespondAsync(PlynlingText.TradeNotYours, ephemeral: true);
            return;
        }
        var offer = _trades.Take(id, now);
        if (offer is null)
        {
            await RespondAsync(PlynlingText.TradeGone, ephemeral: true);           // a double click
            return;
        }

        var (give, want) = InventoryModule.TradeSides(offer);
        var (outcome, fromSets, toSets) = await _inventory.TradeAsync(offer, now);
        switch (outcome)
        {
            case InventoryService.TradeOutcome.TargetLacks:
                _trades.Restore(offer);
                await RespondAsync(PlynlingText.TradeYouLack, ephemeral: true);
                return;
            case InventoryService.TradeOutcome.OfferorLacks:
                await CloseTradeCardAsync(PlynlingText.TradeFailed(offer.FromId, give, want));
                return;
        }
        var text = PlynlingText.TradeDone(offer.FromId, offer.ToId, give, want);
        foreach (var set in toSets) text += "\n" + PlynlingText.SetCompleted(offer.ToId, set);
        foreach (var set in fromSets) text += "\n" + PlynlingText.SetCompleted(offer.FromId, set);
        await CloseTradeCardAsync(text);
    }

    [ComponentInteraction("plyn:tdec:*", ignoreGroupNames: true)]
    public async Task OnTradeDeclineAsync(string id)
    {
        var now = DateTimeOffset.UtcNow;
        var open = _trades.Get(id, now);
        if (open is null)
        {
            await CloseTradeCardAsync(PlynlingText.TradeGone);
            return;
        }
        if (Context.User.Id != open.ToId && Context.User.Id != open.FromId)
        {
            await RespondAsync(PlynlingText.TradeNotYours, ephemeral: true);
            return;
        }
        if (_trades.Take(id, now) is not { } offer)
        {
            await RespondAsync(PlynlingText.TradeGone, ephemeral: true);
            return;
        }
        var (give, want) = InventoryModule.TradeSides(offer);
        await CloseTradeCardAsync(Context.User.Id == offer.FromId
            ? PlynlingText.TradeCancelled(offer.FromId, give, want)
            : PlynlingText.TradeDeclined(offer.FromId, offer.ToId, give, want));
    }

    // Rewrites the offer card without its buttons. Mentions stay inert: everyone concerned was
    // pinged when the offer was made.
    private Task CloseTradeCardAsync(string text) =>
        ((SocketMessageComponent)Context.Interaction).UpdateAsync(m =>
        {
            m.Content = text;
            m.Components = new ComponentBuilder().Build();
            m.AllowedMentions = AllowedMentions.None;
        });

    // ---- /plynling graveyard: the sort toggle and the pages, two verbs, one redraw.
    [ComponentInteraction("grave:page:*:*:*", ignoreGroupNames: true)]
    public Task OnGravePageAsync(string sort, string owner, string page) => ShowGraveyardAsync(sort, owner, page);

    [ComponentInteraction("grave:sort:*:*:*", ignoreGroupNames: true)]
    public Task OnGraveSortAsync(string sort, string owner, string page) => ShowGraveyardAsync(sort, owner, page);

    private async Task ShowGraveyardAsync(string sortStr, string ownerStr, string pageStr)
    {
        if (!Enum.TryParse<GraveSort>(sortStr, out var sort)) sort = GraveSort.Recent;
        ulong.TryParse(ownerStr, out var owner);
        int.TryParse(pageStr, out var page);

        var now = DateTimeOffset.UtcNow;
        var graves = await _plynlings.GetGraveyardAsync(Context.Guild.Id, owner == 0 ? null : owner, now);
        var components = PlynlingGraveyardCards.BuildPage(graves, sort, owner, page, now);

        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(m =>
        {
            m.Components = components;
            m.Flags = MessageFlags.ComponentsV2;
            m.AllowedMentions = AllowedMentions.None;
        });
    }

    // ---- /plynling journal's pages: four verbs, one redraw, re-read on every click.
    [ComponentInteraction("plyn:jfirst:*:*", ignoreGroupNames: true)]
    public Task OnJournalFirstAsync(string id, string page) => ShowJournalAsync(id, page);

    [ComponentInteraction("plyn:jprev:*:*", ignoreGroupNames: true)]
    public Task OnJournalPrevAsync(string id, string page) => ShowJournalAsync(id, page);

    [ComponentInteraction("plyn:jnext:*:*", ignoreGroupNames: true)]
    public Task OnJournalNextAsync(string id, string page) => ShowJournalAsync(id, page);

    [ComponentInteraction("plyn:jlast:*:*", ignoreGroupNames: true)]
    public Task OnJournalLastAsync(string id, string page) => ShowJournalAsync(id, page);

    private async Task ShowJournalAsync(string idStr, string pageStr)
    {
        var now = DateTimeOffset.UtcNow;
        var plynling = int.TryParse(idStr, out var id) ? await _plynlings.GetByIdAsync(id, now) : null;
        if (plynling is null)
        {
            await RespondAsync(PlynlingText.Unknown, ephemeral: true);   // abandoned since
            return;
        }
        var (badges, moments) = await _plynlings.GetJournalAsync(plynling.Id);
        var relations = await _plynlings.GetRelationsAsync(plynling.Id);
        var page = int.TryParse(pageStr, out var p) ? p : 0;
        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(m =>
        {
            m.Components = PlynlingJournalCards.BuildJournal(plynling, badges, moments, page, now, relations);
            m.Flags = MessageFlags.ComponentsV2;
            m.AllowedMentions = AllowedMentions.None;
        });
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

        // Everything below reads and writes the database before anything can be shown, and on the Pi
        // that outran Discord's 3 s: the visit was saved and claimed, the knock never closed, and the
        // host was told « déjà vus » on the next click. Deferring first buys the 15 minutes a
        // follow-up allows; refusals therefore go out as ephemeral follow-ups.
        await DeferAsync();

        var visitor = await _plynlings.GetByIdAsync(visitorId, now);
        var host = await _plynlings.GetCurrentAsync(Context.Guild.Id, hostOwnerId, now);
        string? refusal =
            host is null || host.DiedAt is not null ? PlynlingText.NoPlynling
            : visitor is null || visitor.DiedAt is not null ? PlynlingText.VisitorGone
            : visitor.FrozenAt is not null || host.FrozenAt is not null ? PlynlingText.VisitFrozen
            : PlynlingLife.IsAsleep(now) ? PlynlingText.Asleep(host.Gender)
            : PlynlingLife.IsSick(visitor) ? PlynlingText.VisitSick(PlynlingCardUi.SafeName(visitor.Name))
            : PlynlingLife.IsSick(host) ? PlynlingText.VisitSick(PlynlingCardUi.SafeName(host.Name))
            : null;
        if (refusal is not null || visitor is null || host is null)
        {
            await FollowupAsync(refusal, ephemeral: true);
            return;
        }

        var (story, visitRefusal) = await _visits.RunAsync(visitor, host, now);
        if (story is null)
        {
            await FollowupAsync(visitRefusal, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        // The visit is decided and saved; what follows is only its telling. The knock closes in
        // place, and the story is a follow-up — a new message at the bottom of the channel — opened
        // on its first step with ◀ ▶ already there: the reader pages through at their own pace,
        // nothing moves on its own.
        await component.ModifyOriginalResponseAsync(m =>
        {
            m.Components = PlynlingPlayCards.BuildKnockClosed(PlynlingText.VisitAccepted(story.Visitor.Name, story.Host.Name));
            m.Flags = MessageFlags.ComponentsV2;
            m.AllowedMentions = AllowedMentions.None;
        });
        try
        {
            await component.FollowupAsync(components: PlynlingPlayCards.BuildVisitStory(story, 0),
                flags: MessageFlags.ComponentsV2, allowedMentions: AllowedMentions.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Visit story {Story}: could not be posted", story.Id);
        }
    }

    // ◀ ▶ on a told story. Anyone may page — the card is public, like the story. After a restart
    // the story is gone: the card stays on its last beat and the one who clicked is told privately.
    [ComponentInteraction("vis:prev:*:*", ignoreGroupNames: true)]
    public Task OnVisitPrevAsync(string id, string beatStr) => PageStoryAsync(id, beatStr, -1);

    [ComponentInteraction("vis:next:*:*", ignoreGroupNames: true)]
    public Task OnVisitNextAsync(string id, string beatStr) => PageStoryAsync(id, beatStr, +1);

    // « ↺ Début », on the last step: back to the first.
    [ComponentInteraction("vis:first:*:*", ignoreGroupNames: true)]
    public Task OnVisitFirstAsync(string id, string beatStr) =>
        PageStoryAsync(id, beatStr, 0, to: _ => 0);

    // « ⏭ Fin »: straight to the last step, where the outcome is.
    [ComponentInteraction("vis:last:*:*", ignoreGroupNames: true)]
    public Task OnVisitLastAsync(string id, string beatStr) =>
        PageStoryAsync(id, beatStr, 0, to: story => story.Beats.Count - 1);

    private async Task PageStoryAsync(string id, string beatStr, int step, Func<VisitStory, int>? to = null)
    {
        var story = _stories.Get(id);
        if (story is null || !int.TryParse(beatStr, out var beat))
        {
            await RespondAsync(PlynlingText.StoryGone, ephemeral: true);
            return;
        }
        var card = PlynlingPlayCards.BuildVisitStory(story, to?.Invoke(story) ?? beat + step);
        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(m =>
        {
            m.Components = card;
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
            var (after, balance, badges, find) = await _plynlings.FinishPlayAsync(plynling.Id, session.OwnerId, won, pebbles, now);
            if (after is not null)
            {
                plynling = after;
                var pool = (won ? BotResponses.PlynlingPlayPlayerWonLines : BotResponses.PlynlingPlayPlayerLostLines).For(plynling.Gender);
                endLine = string.Format(_picker.Pick(pool), PlynlingCardUi.SafeName(plynling.Name)) +
                          "\n" + PlynlingGameUi.Reward(won, pebbles, balance);
                if (badges.Count > 0) endLine += "\n" + PlynlingBadges.NewBadgeLines(badges, plynling.Gender);
                if (find is not null)
                    endLine += "\n" + PlynlingText.FindLines(PlynlingText.PlayFind(PlynlingCardUi.SafeName(plynling.Name), find.Item), find, plynling.OwnerId);
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
        await ApplyAsync(await _care.PetAsync(id, Context.User.Id, DateTimeOffset.UtcNow));
    }

    [ComponentInteraction("plyn:bath:*", ignoreGroupNames: true)]
    public async Task OnBathAsync(string idStr)
    {
        if (!int.TryParse(idStr, out var id))
        {
            await RespondAsync(PlynlingText.Unknown, ephemeral: true);
            return;
        }
        await ApplyAsync(await _care.BathAsync(id, Context.User.Id, DateTimeOffset.UtcNow));
    }

    [ComponentInteraction("plyn:heal:*", ignoreGroupNames: true)]
    public async Task OnHealAsync(string idStr)
    {
        if (!int.TryParse(idStr, out var id))
        {
            await RespondAsync(PlynlingText.Unknown, ephemeral: true);
            return;
        }
        await ApplyAsync(await _care.MedicateAsync(id, Context.User.Id, DateTimeOffset.UtcNow));
    }

    [ComponentInteraction("plyn:feed:*", ignoreGroupNames: true)]
    public async Task OnFeedAsync(string idStr, string[] selected)
    {
        if (!int.TryParse(idStr, out var id) || selected.Length == 0 || !Enum.TryParse<PlynlingFood>(selected[0], out var food))
        {
            await RespondAsync(PlynlingText.Unknown, ephemeral: true);
            return;
        }
        await ApplyAsync(await _care.FeedAsync(id, Context.User.Id, food, DateTimeOffset.UtcNow));
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
