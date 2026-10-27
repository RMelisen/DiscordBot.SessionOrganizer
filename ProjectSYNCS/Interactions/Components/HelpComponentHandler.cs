using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Commands;

namespace ProjectSYNCS.Interactions.Components;

// The pages of /help. No database: answered in place, no defer needed.
public class HelpComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    [ComponentInteraction("help:page:*")]
    public async Task OnPageAsync(string pageStr)
    {
        var page = int.TryParse(pageStr, out var p) && HelpModule.Pages.Contains(p) ? p : HelpModule.CommandsPage;
        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(m =>
        {
            m.Embed = HelpModule.BuildEmbed(page);
            m.Components = HelpModule.BuildButtons(page);
        });
    }
}
