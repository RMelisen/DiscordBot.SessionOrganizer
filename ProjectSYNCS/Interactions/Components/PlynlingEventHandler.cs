using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// Events: « ✨ Événement » opens the oldest pending one privately; a pick resolves it and tells its
// story; ◀ ▶ page any told story, rebuilt from the database. Every path reads the database, so every
// path defers first.
// The public card the choice was opened from rides along in the choice's custom-ids (a private message
// cannot reach it otherwise), so a pick rewrites it: the « Événement (n) » count, stress, modifiers,
// needs and traits all change with the outcome.
public class PlynlingEventHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PlynlingService _plynlings;
    private readonly PlynlingAnnouncer _announcer;
    private readonly ILogger<PlynlingEventHandler> _logger;

    public PlynlingEventHandler(PlynlingService plynlings, PlynlingAnnouncer announcer, ILogger<PlynlingEventHandler> logger)
    {
        _plynlings = plynlings;
        _announcer = announcer;
        _logger = logger;
    }

    // From the card itself: the card is the message clicked. An older private result still carrying
    // this id is ephemeral, which the channel cannot edit — no card then.
    [ComponentInteraction("plyn:events:*", ignoreGroupNames: true)]
    public Task OnOpenAsync(string idStr)
    {
        var message = (Context.Interaction as SocketMessageComponent)?.Message;
        var cardId = message is null || (message.Flags ?? MessageFlags.None).HasFlag(MessageFlags.Ephemeral) ? 0UL : message.Id;
        return OpenAsync(idStr, cardId);
    }

    // « Événement suivant » on a private result: the card it started from comes in the id.
    [ComponentInteraction("plyn:evnext:*:*", ignoreGroupNames: true)]
    public Task OnNextEventAsync(string idStr, string cardStr) =>
        OpenAsync(idStr, ulong.TryParse(cardStr, out var cardId) ? cardId : 0);

    private async Task OpenAsync(string idStr, ulong cardId)
    {
        if (!int.TryParse(idStr, out var id))
        {
            await RespondAsync(PlynlingText.Unknown, ephemeral: true);
            return;
        }
        await DeferAsync(ephemeral: true);
        var now = DateTimeOffset.UtcNow;
        var p = await _plynlings.GetByIdAsync(id, now);
        if (p is null || p.DiedAt is not null)
        {
            await FollowupAsync(PlynlingText.Unknown, ephemeral: true);
            return;
        }
        if (p.OwnerId != Context.User.Id)
        {
            await FollowupAsync(PlynlingText.EventNotYours(p.Gender), ephemeral: true);
            return;
        }
        if (p.FrozenAt is not null)
        {
            // Nothing happens while frozen, its owner's choices included (an old card may still show the button).
            await FollowupAsync(PlynlingText.Frozen(p.Gender), ephemeral: true);
            return;
        }
        var inst = (await _plynlings.GetPendingEventsAsync(p, now)).FirstOrDefault();
        if (inst is null || PlynlingEvents.ByKey(inst.EventKey) is not { } def)
        {
            await FollowupAsync(PlynlingText.NoEventWaiting, ephemeral: true);
            return;
        }
        var target = inst.TargetPlynlingId is { } tid ? await _plynlings.GetByIdAsync(tid, now) : null;
        var card = PlynlingEventCards.BuildChoice(inst, def, await _plynlings.GetEventContextAsync(p, now, target),
            EventCast.Of(p, now), target is null ? null : EventCast.Of(target, now), cardId);
        await FollowupAsync(components: card, ephemeral: true, flags: MessageFlags.ComponentsV2, allowedMentions: AllowedMentions.None);
    }

    [ComponentInteraction("plev:choose:*:*:*", ignoreGroupNames: true)]
    public Task OnPickAsync(string idStr, string optionKey, string cardStr) =>
        PickAsync(idStr, optionKey, ulong.TryParse(cardStr, out var cardId) ? cardId : 0);

    // A choice opened before the card id rode along: picked as before, no card to refresh.
    [ComponentInteraction("plev:pick:*:*", ignoreGroupNames: true)]
    public Task OnLegacyPickAsync(string idStr, string optionKey) => PickAsync(idStr, optionKey, 0);

    private async Task PickAsync(string idStr, string optionKey, ulong cardId)
    {
        if (!int.TryParse(idStr, out var id))
        {
            await RespondAsync(PlynlingText.Unknown, ephemeral: true);
            return;
        }
        await DeferAsync();      // updates the ephemeral choice in place
        var now = DateTimeOffset.UtcNow;
        var pick = await _plynlings.PickEventAsync(id, optionKey, Context.User.Id, now);
        if (pick.Outcome != EventPickOutcome.Done)
        {
            await FollowupAsync(pick.Outcome switch
            {
                EventPickOutcome.Gone => PlynlingText.EventAlreadyDecided,
                EventPickOutcome.NotAvailable => PlynlingText.EventOptionGone,
                EventPickOutcome.Frozen => PlynlingText.Frozen(pick.Gender),
                _ => PlynlingText.Unknown,
            }, ephemeral: true);
            return;
        }
        var story = await _plynlings.GetEventStoryAsync(id, now);
        if (story is null) return;
        await ModifyOriginalResponseAsync(m =>
        {
            m.Components = PlynlingEventCards.BuildResult(story.Pages[^1], pick.PendingLeft, story.PlynlingId, cardId);
            m.Flags = MessageFlags.ComponentsV2;
            m.AllowedMentions = AllowedMentions.None;
        });
        await RefreshCardAsync(cardId, story.PlynlingId, now);
        // Every story the pick caused: the picked one, then any answer the mascot gave at once.
        await _announcer.TellAsync(_plynlings, pick.Told ?? new[] { id }, Context.Guild?.Id ?? 0, now, Context.Channel);
    }

    // Rewrites the public card after the outcome landed. Her last line is not stored, so the card comes
    // back without it. A side effect: a card deleted, or a channel the bot can no longer edit in, never
    // breaks the pick.
    private async Task RefreshCardAsync(ulong cardId, int plynlingId, DateTimeOffset now)
    {
        if (cardId == 0 || Context.Channel is null) return;
        try
        {
            var p = await _plynlings.GetByIdAsync(plynlingId, now);
            if (p is null) return;
            var card = await PlynlingModule.BuildCardAsync(_plynlings, p, now, null);
            await Context.Channel.ModifyMessageAsync(cardId, m =>
            {
                m.Components = card;
                m.Flags = MessageFlags.ComponentsV2;
                m.AllowedMentions = AllowedMentions.None;
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Plynling {Id}: card {Card} could not be refreshed after an event", plynlingId, cardId);
        }
    }

    [ComponentInteraction("evs:prev:*:*", ignoreGroupNames: true)]
    public Task OnPrevAsync(string id, string page) => PageAsync(id, page, s => s.page - 1);

    [ComponentInteraction("evs:next:*:*", ignoreGroupNames: true)]
    public Task OnNextAsync(string id, string page) => PageAsync(id, page, s => s.page + 1);

    [ComponentInteraction("evs:first:*:*", ignoreGroupNames: true)]
    public Task OnFirstAsync(string id, string page) => PageAsync(id, page, _ => 0);

    [ComponentInteraction("evs:last:*:*", ignoreGroupNames: true)]
    public Task OnLastAsync(string id, string page) => PageAsync(id, page, s => s.story.Pages.Count - 1);

    private async Task PageAsync(string idStr, string pageStr, Func<(EventStory story, int page), int> to)
    {
        if (!int.TryParse(idStr, out var id) || !int.TryParse(pageStr, out var page))
        {
            await RespondAsync(PlynlingText.Unknown, ephemeral: true);
            return;
        }
        await DeferAsync();
        var story = await _plynlings.GetEventStoryAsync(id, DateTimeOffset.UtcNow);
        if (story is null)
        {
            await FollowupAsync(PlynlingText.StoryGone, ephemeral: true);
            return;
        }
        var card = PlynlingEventCards.BuildStory(story, to((story, page)));
        await ModifyOriginalResponseAsync(m =>
        {
            m.Components = card;
            m.Flags = MessageFlags.ComponentsV2;
            m.AllowedMentions = AllowedMentions.None;
        });
    }
}
