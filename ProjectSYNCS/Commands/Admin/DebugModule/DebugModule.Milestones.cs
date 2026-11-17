using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /debug milestones: the current channel's message count and the milestones it reached
// (ChannelMilestoneService), only for the owner to see.
public partial class DebugModule
{
    [SlashCommand("milestones", "Les paliers de messages de ce salon, et qui les a atteints")]
    public async Task MilestonesAsync()
    {
        if (Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
            return;
        }
        if (Context.Channel is not SocketGuildChannel channel)
        {
            await RespondAsync("À utiliser dans un salon du serveur.", ephemeral: true);
            return;
        }

        // The milestones are read from the database: answer within 3 s first.
        await DeferAsync(ephemeral: true);
        var view = await _milestones.ViewAsync(channel.Id);
        if (view is null)
        {
            await FollowupAsync("Je ne compte pas ce salon : un fil, un salon vocal, un salon exclu de l'XP, "
                                + "ou un salon dont je ne peux pas lire l'historique.", ephemeral: true);
            return;
        }
        // The newest milestones first; older pages behind « Plus anciens » (MilestoneComponentHandler).
        await FollowupAsync(embed: MilestoneCards.Build(channel.Guild.Id, channel.Name, view, page: 0),
            components: MilestoneCards.BuildButtons(channel.Id, view.Reached.Count, page: 0), ephemeral: true);
    }
}
