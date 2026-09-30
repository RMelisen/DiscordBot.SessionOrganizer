using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// Plays an accepted visit and writes its story: the once-a-day claim, the scene itself (decided and
// saved by PlynlingService.VisitAsync), the claim released if the scene could not happen, and the
// story kept in VisitStories for paging. Shared by the two ways a visit is accepted — « Accueillir »
// on a knock, and her own Plynling, whose door she opens herself — so they cannot drift apart.
// Transient: it wraps PlynlingService. Everything the two callers check *before* this (dead, frozen,
// asleep, sick) stays with them, since each words its refusals for how it was reached.
public class PlynlingVisitRunner
{
    private readonly PlynlingService _plynlings;
    private readonly PlynlingCooldowns _cooldowns;
    private readonly VisitStories _stories;
    private readonly ResponsePicker _picker;

    public PlynlingVisitRunner(PlynlingService plynlings, PlynlingCooldowns cooldowns, VisitStories stories, ResponsePicker picker)
    {
        _plynlings = plynlings;
        _cooldowns = cooldowns;
        _stories = stories;
        _picker = picker;
    }

    // The story, or the refusal to send privately.
    public async Task<(VisitStory? Story, string? Refusal)> RunAsync(Plynling visitor, Plynling host, ulong channelId, DateTimeOffset now)
    {
        var day = AppTime.DayKey(now);
        if (!_cooldowns.TryClaimVisit(visitor.OwnerId, host.OwnerId, day))
            return (null, PlynlingText.VisitedToday(visitor.OwnerId));

        var met = await _plynlings.VisitAsync(visitor.Id, host.Id, now);
        if (met is not { } pair)
        {
            _cooldowns.ReleaseVisit(visitor.OwnerId, host.OwnerId, day);
            return (null, PlynlingText.Unknown);
        }

        var story = _stories.Add(
            PlynlingVisitStory.Build(pair, PlynlingPlayCards.VisitOutcomeLines(pair), now, Random.Shared, pool => _picker.Pick(channelId, pool)),
            Random.Shared);
        return (story, null);
    }
}
