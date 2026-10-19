using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /report — her reports to the staff: how the sessions went, the polls and /tonight, the life of the
// server, and how she herself is doing. Private (they name people: no-shows, latecomers, the ones
// going quiet), staff only, opening on 30 days; the period buttons are handled by
// ReportComponentHandler, which re-checks IsStaff. The same embeds go to the owner once a month
// (MonthlyDigestService).
//
// Guild-only for the same reason as /admin: everything is scoped to Context.Guild.Id.
[CommandContextType(InteractionContextType.Guild)]
// Deliberately no [DefaultMemberPermissions]: a permission bit can't say "ManageGuild, plus the
// owner" (see AdminModule). SessionPermissions.IsStaff in every handler is the gate.
[Group("report", "Mes rapports : sessions, sondages, communauté, et moi (admins/modérateurs)")]
public class ReportModule : InteractionModuleBase<SocketInteractionContext>
{
    public const StatsPeriod DefaultPeriod = StatsPeriod.Month;

    private readonly ReportService _reports;

    public ReportModule(ReportService reports)
    {
        _reports = reports;
    }

    [SlashCommand("sessions", "Les sessions : présence, retards, absences, meilleurs créneaux")]
    public Task SessionsAsync() => ShowAsync(ReportKind.Sessions);

    [SlashCommand("polls", "Les sondages, mon sondage du mercredi et /tonight")]
    public Task PollsAsync() => ShowAsync(ReportKind.Polls);

    [SlashCommand("community", "La vie du serveur : membres actifs, arrivées et départs, émotes, quiz")]
    public Task CommunityAsync() => ShowAsync(ReportKind.Community);

    [SlashCommand("syncs", "Comment je vais : température, pannes, bavardage, réputation, vocal")]
    public Task SyncsAsync() => ShowAsync(ReportKind.Syncs);

    private async Task ShowAsync(ReportKind kind)
    {
        if (!SessionPermissions.IsStaff(Context.User))
        {
            await RespondAsync("Mes rapports sont réservés aux administrateurs et aux modérateurs. Bien tenté (˶ᵔ ᵕ ᵔ˶)", ephemeral: true);
            return;
        }
        // The database can outrun Discord's 3 s on the Pi.
        await DeferAsync(ephemeral: true);
        var window = ReportWindow.From(DefaultPeriod, DateTimeOffset.UtcNow);
        var embed = await _reports.BuildAsync(kind, Context.Guild.Id, window, names: true);
        await FollowupAsync(embed: embed, components: ReportCards.PeriodRow(kind, DefaultPeriod),
            ephemeral: true, allowedMentions: AllowedMentions.None);
    }
}
