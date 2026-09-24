using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// The Plynling card's two controls. A success rewrites the card in place, with her line
// on it, rather than posting a second message; a refusal goes privately to whoever
// pressed, and the card is left untouched.
public class PlynlingComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PlynlingCareService _care;

    public PlynlingComponentHandler(PlynlingCareService care)
    {
        _care = care;
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
