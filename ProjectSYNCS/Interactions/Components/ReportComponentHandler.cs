using Discord;
using Discord.Interactions;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// /report's period buttons: report:win:{kind}:{period}:0 (StatsPeriodUi.AddFilterRow), and the
// sleeping report's own row, report:sleep:{days}. The report is public, so anyone can click: IsStaff
// is checked on every click, and a refusal is private. Deferred first: the database can outrun the 3 s.
public class ReportComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ReportService _reports;
    private readonly ResponsePicker _picker;

    public ReportComponentHandler(ReportService reports, ResponsePicker picker)
    {
        _reports = reports;
        _picker = picker;
    }

    [ComponentInteraction("report:win:*:*:*", ignoreGroupNames: true)]
    public async Task OnPeriodAsync(string kindStr, string periodStr, string _)
    {
        if (!SessionPermissions.IsStaff(Context.User))
        {
            await RespondAsync(ReportModule.Refusal, ephemeral: true);
            return;
        }
        if (!Enum.TryParse<ReportKind>(kindStr, out var kind)) return;
        if (!Enum.TryParse<StatsPeriod>(periodStr, out var period)) period = ReportModule.DefaultPeriod;

        await DeferAsync();
        var window = ReportWindow.From(period, DateTimeOffset.UtcNow);
        var embed = ReportCards.WithHeadline(await _reports.BuildAsync(kind, Context.Guild.Id, window, names: true), Line());
        await ModifyOriginalResponseAsync(m =>
        {
            m.Embed = embed;
            m.Components = ReportCards.PeriodRow(kind, period);
            m.AllowedMentions = AllowedMentions.None;
        });
    }

    // /report sleeping's 30 / 60 / 90 buttons: report:sleep:{days}. Its own verb, so it can never
    // collide with a period row.
    [ComponentInteraction("report:sleep:*", ignoreGroupNames: true)]
    public async Task OnSleepAsync(string daysStr)
    {
        if (!SessionPermissions.IsStaff(Context.User))
        {
            await RespondAsync(ReportModule.Refusal, ephemeral: true);
            return;
        }
        if (!int.TryParse(daysStr, out var days) || !SleepingReport.Choices.Contains(days)) days = SleepingReport.Choices[0];

        await DeferAsync();
        var embed = ReportCards.WithHeadline(await _reports.BuildSleepingAsync(Context.Guild.Id, days), Line());
        await ModifyOriginalResponseAsync(m =>
        {
            m.Embed = embed;
            m.Components = ReportCards.SleepRow(days);
            m.AllowedMentions = AllowedMentions.None;
        });
    }

    // Her line for whoever clicked: the card is public, so any staff member may change its period,
    // and she answers that person.
    private string Line() => _picker.Pick(ReportCards.DutyPool(Context.User.Id));
}
