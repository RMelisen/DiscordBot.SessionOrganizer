using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Commands;

namespace ProjectSYNCS.Interactions.Components;

// The pages of /minecraft help. No database: answered in place, no defer needed.
public class MinecraftHelpHandler : InteractionModuleBase<SocketInteractionContext>
{
    [ComponentInteraction("minecraft:help:*", ignoreGroupNames: true)]
    public async Task OnPageAsync(string pageStr)
    {
        var page = int.TryParse(pageStr, out var p) ? Math.Clamp(p, 0, MinecraftModule.HelpPages - 1) : 0;
        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(m =>
        {
            m.Embed = MinecraftModule.BuildHelp(page);
            m.Components = MinecraftModule.BuildHelpButtons(page);
        });
    }
}
