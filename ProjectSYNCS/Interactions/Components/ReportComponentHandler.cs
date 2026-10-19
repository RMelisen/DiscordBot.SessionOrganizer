using Discord;
using Discord.Interactions;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// /report's period buttons: report:win:{kind}:{period}:0 (StatsPeriodUi.AddFilterRow). The report is
// ephemeral, so only the staff member who opened it can click — IsStaff is re-checked anyway, since
// a demoted moderator keeps the message. Deferred first: the database can outrun the 3 s.
public class ReportComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ReportService _reports;

    public ReportComponentHandler(ReportService reports)
    {
        _reports = reports;
    }

    [ComponentInteraction("report:win:*:*:*", ignoreGroupNames: true)]
    public async Task OnPeriodAsync(string kindStr, string periodStr, string _)
    {
        if (!SessionPermissions.IsStaff(Context.User))
        {
            await RespondAsync("Mes rapports sont réservés aux administrateurs et aux modérateurs. Bien tenté (˶ᵔ ᵕ ᵔ˶)", ephemeral: true);
            return;
        }
        if (!Enum.TryParse<ReportKind>(kindStr, out var kind)) return;
        if (!Enum.TryParse<StatsPeriod>(periodStr, out var period)) period = ReportModule.DefaultPeriod;

        await DeferAsync();
        var window = ReportWindow.From(period, DateTimeOffset.UtcNow);
        var embed = await _reports.BuildAsync(kind, Context.Guild.Id, window, names: true);
        await ModifyOriginalResponseAsync(m =>
        {
            m.Embed = embed;
            m.Components = ReportCards.PeriodRow(kind, period);
            m.AllowedMentions = AllowedMentions.None;
        });
    }
}
