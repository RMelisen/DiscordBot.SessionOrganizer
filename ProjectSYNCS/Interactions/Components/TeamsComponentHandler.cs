using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// /teams' « Remélanger »: a fresh draw from whoever is in voice right now, redrawn in place.
// Everything it needs is in its custom-id, so it survives restarts.
public class TeamsComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly XpService _xp;
    private readonly ResponsePicker _picker;

    public TeamsComponentHandler(XpService xp, ResponsePicker picker)
    {
        _xp = xp;
        _picker = picker;
    }

    [ComponentInteraction("teams:roll:*:*:*")]
    public async Task OnRerollAsync(string channelStr, string teamsStr, string modeStr)
    {
        if (!ulong.TryParse(channelStr, out var channelId) || !int.TryParse(teamsStr, out var teams)
            || !int.TryParse(modeStr, out var mode) || Context.Guild.GetVoiceChannel(channelId) is not { } voice)
        {
            await RespondAsync("Ce salon vocal n'existe plus.", ephemeral: true);
            return;
        }

        var (embed, components, refusal) = await TeamsModule.DrawAsync(voice, teams, (TeamMode)mode, _xp, _picker);
        if (refusal is not null)
        {
            await RespondAsync(refusal, ephemeral: true);
            return;
        }

        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(p =>
        {
            p.Embed = embed;
            p.Components = components;
        });
    }
}
