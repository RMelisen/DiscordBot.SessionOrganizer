using Discord.Interactions;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// The pages of /debug milestones (MilestoneCards): « Plus anciens » / « Plus récents ». Two verbs
// over one handler, since Discord refuses a message carrying the same custom-id twice. The
// milestones are read from the database, so the click is acknowledged first.
public class MilestoneComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ChannelMilestoneService _milestones;

    public MilestoneComponentHandler(ChannelMilestoneService milestones) => _milestones = milestones;

    [ComponentInteraction("milestones:older:*:*")]
    public Task OnOlderAsync(string channelId, string page) => ShowAsync(channelId, page);

    [ComponentInteraction("milestones:newer:*:*")]
    public Task OnNewerAsync(string channelId, string page) => ShowAsync(channelId, page);

    private async Task ShowAsync(string channelIdStr, string pageStr)
    {
        if (Context.User.Id != AvailabilityService.OwnerId || Context.Guild is null
            || !ulong.TryParse(channelIdStr, out var channelId) || !int.TryParse(pageStr, out var page))
        {
            await RespondAsync("Seul Rodhengard peut feuilleter ces paliers.", ephemeral: true);
            return;
        }

        await DeferAsync();
        var view = await _milestones.ViewAsync(channelId);
        if (view is null)
        {
            await ModifyOriginalResponseAsync(m => m.Content = "Je ne compte plus ce salon.");
            return;
        }
        var name = Context.Guild.GetChannel(channelId)?.Name ?? "salon";
        await ModifyOriginalResponseAsync(m =>
        {
            m.Embed = MilestoneCards.Build(Context.Guild.Id, name, view, page);
            m.Components = MilestoneCards.BuildButtons(channelId, view.Reached.Count, page);
        });
    }
}
