using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Commands;

namespace ProjectSYNCS.Interactions.Components;

// The two pages of /plynling help. No database: answered in place, no defer needed.
public class PlynlingHelpHandler : InteractionModuleBase<SocketInteractionContext>
{
    [ComponentInteraction("plyn:help:*", ignoreGroupNames: true)]
    public async Task OnPageAsync(string pageStr)
    {
        var page = pageStr == "1" ? 1 : 0;
        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(m =>
        {
            m.Embed = PlynlingModule.BuildHelpEmbed(page);
            m.Components = PlynlingModule.BuildHelpButtons(page);
        });
    }
}
