using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// /recap's window buttons. The custom-id carries whose recap it is and the window
// (StatsPeriodUi appends « :{period}:0 »), so anyone can flip it and a restart breaks nothing.
public class RecapComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly RecapService _recap;
    private readonly ResponsePicker _picker;

    public RecapComponentHandler(RecapService recap, ResponsePicker picker)
    {
        _recap = recap;
        _picker = picker;
    }

    [ComponentInteraction("recap:win:*:*:*", ignoreGroupNames: true)]
    public async Task OnWindowAsync(string userStr, string periodStr, string _)
    {
        if (!ulong.TryParse(userStr, out var userId)) return;
        if (!Enum.TryParse<StatsPeriod>(periodStr, out var period)) period = RecapModule.DefaultPeriod;

        // The reads can outrun the 3 s: acknowledge first, then edit the card in place.
        await DeferAsync();
        var stats = await _recap.GetAsync(Context.Guild.Id, userId, period);
        var components = RecapModule.BuildCard(userId, Context.Guild.GetUser(userId), stats,
            RecapModule.Verdict(_picker, userId, stats));

        await ((SocketMessageComponent)Context.Interaction).ModifyOriginalResponseAsync(m =>
        {
            m.Components = components;
            // Re-asserted on every edit, as on every Components V2 card.
            m.Flags = MessageFlags.ComponentsV2;
            m.AllowedMentions = AllowedMentions.None;
        });
    }
}
