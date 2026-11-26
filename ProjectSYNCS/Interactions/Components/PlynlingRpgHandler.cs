using Discord;
using Discord.Interactions;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// The RPG's point buttons (docs/agents/plynling-rpg.md), under a drill's private answer:
// rpg:point:{plynlingId}:{stat}. Its own module, never on PlynlingModule (registered twice, /plynling and /pl).
// The id carries no count: SpendPointAsync re-checks what is left, so a stale or double click finds none.
public class PlynlingRpgHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PlynlingService _plynlings;
    private readonly ResponsePicker _picker;

    public PlynlingRpgHandler(PlynlingService plynlings, ResponsePicker picker)
    {
        _plynlings = plynlings;
        _picker = picker;
    }

    [ComponentInteraction("rpg:point:*:*", ignoreGroupNames: true)]
    public async Task OnPointAsync(string idStr, string statStr)
    {
        if (!int.TryParse(idStr, out var id) || !Enum.TryParse<PlynlingStat>(statStr, out var stat) || !PlynlingStats.IsBody(stat))
        {
            await RespondAsync(PlynlingText.Unknown, ephemeral: true);
            return;
        }
        // Reads and saves before it can answer: defer first.
        await DeferAsync(ephemeral: true);
        var now = DateTimeOffset.UtcNow;
        var (outcome, p, badges) = await _plynlings.SpendPointAsync(id, Context.User.Id, stat, now);
        switch (outcome)
        {
            case PointOutcome.Done when p is not null:
                var (text, buttons) = PlynlingTrainCards.PointReply(p, await _plynlings.GetTraitsAsync(p), stat, _picker.Pick, badges);
                await FollowupAsync(text, components: buttons, ephemeral: true, allowedMentions: AllowedMentions.None);
                break;
            case PointOutcome.NotOwner:
                await FollowupAsync(PlynlingText.PointNotYours, ephemeral: true);
                break;
            case PointOutcome.NoPoint:
                await FollowupAsync(PlynlingText.PointNoneLeft, ephemeral: true);
                break;
            default:
                await FollowupAsync(PlynlingText.Unknown, ephemeral: true);
                break;
        }
    }
}
