using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /report — her reports to the staff: how the sessions went, the polls and /tonight, the life of the
// server, and how she herself is doing, and the rest. Asked by staff only, posted in the open with
// every name (the owner's call: no-shows, latecomers and sleepers included), under her line for
// whoever asked (ReportCards.DutyPool); only the refusals stay private. Opens on 30 days; the period
// buttons are handled by ReportComponentHandler, which re-checks IsStaff. The same embeds go to the
// owner once a month (MonthlyDigestService), without the line.
//
// Guild-only for the same reason as /admin: everything is scoped to Context.Guild.Id.
[CommandContextType(InteractionContextType.Guild)]
// Deliberately no [DefaultMemberPermissions]: a permission bit can't say "ManageGuild, plus the
// owner" (see AdminModule). SessionPermissions.IsStaff in every handler is the gate.
[Group("report", "Mes rapports : sessions, sondages, communauté, et moi (admins/modérateurs)")]
public class ReportModule : InteractionModuleBase<SocketInteractionContext>
{
    public const StatsPeriod DefaultPeriod = StatsPeriod.Month;

    // Also ReportComponentHandler's, for a demoted moderator clicking an old report.
    public const string Refusal = "Mes rapports sont réservés aux administrateurs et aux modérateurs. Bien tenté (˶ᵔ ᵕ ᵔ˶)";

    private readonly ReportService _reports;
    private readonly ResponsePicker _picker;

    public ReportModule(ReportService reports, ResponsePicker picker)
    {
        _reports = reports;
        _picker = picker;
    }

    [SlashCommand("sessions", "Les sessions : présence, retards, absences, meilleurs créneaux")]
    public Task SessionsAsync() => ShowAsync(ReportKind.Sessions);

    [SlashCommand("polls", "Les sondages, mon sondage du mercredi et /tonight")]
    public Task PollsAsync() => ShowAsync(ReportKind.Polls);

    [SlashCommand("community", "La vie du serveur : membres actifs, arrivées et départs, émotes, quiz")]
    public Task CommunityAsync() => ShowAsync(ReportKind.Community);

    [SlashCommand("syncs", "Comment je vais : température, pannes, bavardage, réputation, vocal")]
    public Task SyncsAsync() => ShowAsync(ReportKind.Syncs);

    [SlashCommand("activity", "Heures de pointe, salons écrits et salons vocaux")]
    public Task ActivityAsync() => ShowAsync(ReportKind.Activity);

    [SlashCommand("commands", "Mes commandes : les plus utilisées, les erreurs, celles qu'on oublie")]
    public Task CommandsAsync() => ShowAsync(ReportKind.Commands);

    [SlashCommand("quiz", "Le quiz : trouvé ou pas, par catégorie, les questions qui résistent")]
    public Task QuizAsync() => ShowAsync(ReportKind.Quiz);

    [SlashCommand("plynlings", "Les Plynlings : naissances, décès, santé, événements, liens")]
    public Task PlynlingsAsync() => ShowAsync(ReportKind.Plynlings);

    [SlashCommand("emotes", "Les émotes du serveur : préférées, oubliées, tendances")]
    public Task EmotesAsync() => ShowAsync(ReportKind.Emotes);

    [SlashCommand("minecraft", "Minecraft : son temps de jeu, ses ordres, ses aventures")]
    public Task MinecraftAsync() => ShowAsync(ReportKind.Minecraft);

    // No period: a silence of 30, 60 or 90 days, chosen with its own buttons (report:sleep:{days}).
    [SlashCommand("sleeping", "Les membres sans signe de vie depuis 30, 60 ou 90 jours")]
    public async Task SleepingAsync()
    {
        if (!SessionPermissions.IsStaff(Context.User))
        {
            await RespondAsync(Refusal, ephemeral: true);
            return;
        }
        await DeferAsync();
        var days = SleepingReport.Choices[0];
        var embed = await _reports.BuildSleepingAsync(Context.Guild.Id, days);
        await FollowupAsync(embed: ReportCards.WithHeadline(embed, _picker.Pick(ReportCards.DutyPool(Context.User.Id))),
            components: ReportCards.SleepRow(days), allowedMentions: AllowedMentions.None);
    }

    private async Task ShowAsync(ReportKind kind)
    {
        if (!SessionPermissions.IsStaff(Context.User))
        {
            await RespondAsync(Refusal, ephemeral: true);
            return;
        }
        // The database can outrun Discord's 3 s on the Pi. Public: the report is posted in the open.
        await DeferAsync();
        var window = ReportWindow.From(DefaultPeriod, DateTimeOffset.UtcNow);
        var embed = await _reports.BuildAsync(kind, Context.Guild.Id, window, names: true);
        await FollowupAsync(embed: ReportCards.WithHeadline(embed, _picker.Pick(ReportCards.DutyPool(Context.User.Id))),
            components: ReportCards.PeriodRow(kind, DefaultPeriod), allowedMentions: AllowedMentions.None);
    }
}
