using Discord;
using Discord.Interactions;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// Events: « ✨ Événement » opens the oldest pending one privately; a pick resolves it and tells its
// story; ◀ ▶ page any told story, rebuilt from the database. Every path reads the database, so every
// path defers first.
public class PlynlingEventHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PlynlingService _plynlings;
    private readonly PlynlingAnnouncer _announcer;

    public PlynlingEventHandler(PlynlingService plynlings, PlynlingAnnouncer announcer)
    {
        _plynlings = plynlings;
        _announcer = announcer;
    }

    [ComponentInteraction("plyn:events:*", ignoreGroupNames: true)]
    public async Task OnOpenAsync(string idStr)
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
        var inst = (await _plynlings.GetPendingEventsAsync(p, now)).FirstOrDefault();
        if (inst is null || PlynlingEvents.ByKey(inst.EventKey) is not { } def)
        {
            await FollowupAsync(PlynlingText.NoEventWaiting, ephemeral: true);
            return;
        }
        var target = inst.TargetPlynlingId is { } tid ? await _plynlings.GetByIdAsync(tid, now) : null;
        var card = PlynlingEventCards.BuildChoice(inst, def, await _plynlings.GetEventContextAsync(p, now, target),
            EventCast.Of(p, now), target is null ? null : EventCast.Of(target, now));
        await FollowupAsync(components: card, ephemeral: true, flags: MessageFlags.ComponentsV2, allowedMentions: AllowedMentions.None);
    }

    [ComponentInteraction("plev:pick:*:*", ignoreGroupNames: true)]
    public async Task OnPickAsync(string idStr, string optionKey)
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
                _ => PlynlingText.Unknown,
            }, ephemeral: true);
            return;
        }
        var story = await _plynlings.GetEventStoryAsync(id, now);
        if (story is null) return;
        await ModifyOriginalResponseAsync(m =>
        {
            m.Components = PlynlingEventCards.BuildResult(story.Pages[^1], pick.PendingLeft, story.PlynlingId);
            m.Flags = MessageFlags.ComponentsV2;
            m.AllowedMentions = AllowedMentions.None;
        });
        // Every story the pick caused: the picked one, then any answer the mascot gave at once.
        await _announcer.TellAsync(_plynlings, pick.Told ?? new[] { id }, Context.Guild?.Id ?? 0, now, Context.Channel);
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
