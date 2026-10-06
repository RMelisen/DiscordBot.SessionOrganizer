using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// « 📜 Personnalité » on the Plynling card: its traits and stats, answered privately to whoever
// pressed. Its own module rather than PlynlingComponentHandler, which is already long; never on
// PlynlingModule (registered twice, /plynling and /pl).
public class PlynlingPersonalityHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PlynlingService _plynlings;

    public PlynlingPersonalityHandler(PlynlingService plynlings) => _plynlings = plynlings;

    [ComponentInteraction("plyn:traits:*", ignoreGroupNames: true)]
    public async Task OnTraitsAsync(string idStr)
    {
        if (!int.TryParse(idStr, out var id))
        {
            await RespondAsync(PlynlingText.Unknown, ephemeral: true);
            return;
        }
        // Two reads before anything can be shown: defer first (the Pi can outrun Discord's 3 s).
        await DeferAsync(ephemeral: true);
        var plynling = await _plynlings.GetByIdAsync(id, DateTimeOffset.UtcNow);
        if (plynling is null)
        {
            await FollowupAsync(PlynlingText.Unknown, ephemeral: true);
            return;
        }
        var traits = await _plynlings.GetTraitsAsync(plynling);
        await FollowupAsync(embed: PlynlingPersonality.DetailEmbed(plynling, traits), ephemeral: true,
            allowedMentions: AllowedMentions.None);
    }
}
