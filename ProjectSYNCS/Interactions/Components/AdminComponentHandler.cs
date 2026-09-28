using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// /admin dashboard's window buttons. The dashboard is ephemeral, so only the staff member who
// opened it can click — IsStaff is re-checked anyway, since a demoted moderator keeps the message.
public class AdminComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly EconomyDashboardService _dashboard;

    public AdminComponentHandler(EconomyDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    [ComponentInteraction("dash:win:*", ignoreGroupNames: true)]
    public async Task OnWindowAsync(string windowStr)
    {
        if (!SessionPermissions.IsStaff(Context.User))
        {
            await RespondAsync("Cette commande est réservée aux administrateurs et aux modérateurs. Bien tenté (˶ᵔ ᵕ ᵔ˶)", ephemeral: true);
            return;
        }
        if (!Enum.TryParse<DashboardWindow>(windowStr, out var window)) window = DashboardWindow.Week;
        var data = await _dashboard.GetAsync(Context.Guild.Id, window, DateTimeOffset.UtcNow);
        var (embed, components) = AdminCards.BuildDashboard(data);
        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(m =>
        {
            m.Embed = embed;
            m.Components = components;
            m.AllowedMentions = AllowedMentions.None;
        });
    }
}
