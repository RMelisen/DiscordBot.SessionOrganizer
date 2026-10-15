using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// /wouldyourather's 🅰️ / 🅱️. The vote is answered privately (the split stays hidden until the
// end); the card's public count is then redrawn by the service, which never lets that redraw land
// after the reveal.
public class WouldYouRatherComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly WouldYouRatherService _rounds;

    public WouldYouRatherComponentHandler(WouldYouRatherService rounds)
    {
        _rounds = rounds;
    }

    [ComponentInteraction("wyr:pick:*:*", ignoreGroupNames: true)]
    public async Task OnPickAsync(string idStr, string sideStr)
    {
        var component = (SocketMessageComponent)Context.Interaction;
        if (!int.TryParse(idStr, out var id) || !int.TryParse(sideStr, out var side) || side is not (0 or 1)
            || _rounds.Vote(id, component.Message.Id, Context.User.Id, side) is not { } round)
        {
            await RespondAsync("Ce dilemme a expiré. Lance-en un autre avec /wouldyourather.", ephemeral: true);
            return;
        }

        await RespondAsync($"Noté : {(side == 0 ? "🅰️" : "🅱️")}. Tu peux encore changer d'avis jusqu'à la fin.",
            ephemeral: true);
        await _rounds.RefreshCountAsync(round, component.Message);
    }
}
