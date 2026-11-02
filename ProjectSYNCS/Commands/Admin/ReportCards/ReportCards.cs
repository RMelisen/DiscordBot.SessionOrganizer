using System.Globalization;
using System.Text;
using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

/// <summary>
/// What /report and the monthly digest render: one embed per report. Static and Context-free, so the
/// checks can measure every embed against Discord's caps (6000 per message, 1024 per field) with
/// worst-case data. Mentions in an embed never ping.
/// </summary>
/// <remarks>
/// <paramref name="names"/> false drops every field that names someone: absences are a count,
/// never names, wherever the report could be read by others (docs/agents/admin.md, /report).
/// A mention in an embed only shows a name if the reader's app already knows that member (in a DM,
/// the digest's sleepers came out as raw ids), so each person is written out by <c>nameOf</c> too.
/// </remarks>
public static partial class ReportCards
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    // Under 1024 per field; eight fields at most per embed keeps each one far below 6000.
    private const int MaxFieldLength = 900;

    /// <summary>
    /// Her line on top of a report, by who asked: Papa gets her on duty, Tata her warmth, any other
    /// staff member her suspicion. The digest has none: its own line opens the DM.
    /// </summary>
    public static string[] DutyPool(ulong askerId) =>
        askerId == AvailabilityService.OwnerId ? BotResponses.OwnerReportDutyLines
        : askerId == BotResponses.TataId ? BotResponses.TataReportLines
        : BotResponses.StaffReportLines;

    /// <summary>
    /// Her line first in the card, above the figures, as on /health. Left out rather than let a card
    /// pass Discord's caps (4096 per description, 6000 per message).
    /// </summary>
    public static Embed WithHeadline(Embed embed, string line)
    {
        var description = embed.Description ?? "";
        if (embed.Length + line.Length + 2 > 6000 || description.Length + line.Length + 2 > 4096) return embed;
        return embed.ToEmbedBuilder()
            .WithDescription(description.Length == 0 ? line : $"{line}\n\n{description}")
            .Build();
    }

    public static MessageComponent PeriodRow(ReportKind kind, StatsPeriod period) =>
        new ComponentBuilder().AddFilterRow($"report:win:{kind}", period).Build();

    // ---- /report sessions ---------------------------------------------------------------------

    public static Embed BuildSessions(Compared<SessionReportData> c, ReportWindow w, bool names, Func<ulong, string?>? nameOf = null)
    {
        var d = c.Current;
        var p = c.Previous;
        var pace = w.Pace(c.FirstDay);
        var embed = Start("📋 Les sessions", w);

        if (d.Held == 0 && d.Cancelled == 0)
            return embed.WithDescription("Aucune session sur cette période" + (p is { Held: > 0 } ? $", contre **{p.Held}** la période d'avant." : ".")).Build();

        var categories = string.Join(" · ", d.ByCategory.Select(x => $"{Category(x.Category)} {x.Count}"));
        embed.WithDescription(
            $"**{d.Held}** {Plural(d.Held, "session tenue", "sessions tenues")}{TextChart.Trend(d.Held, p?.Held, pace)}" +
            $" · **{d.Cancelled}** {Plural(d.Cancelled, "annulée", "annulées")}" +
            (categories.Length > 0 ? $"\n{categories}" : ""));

        if (d.Held > 0)
        {
            var lines = new List<string>
            {
                $"En moyenne **{Num(d.SignUps / (double)d.Held)}** {Plural(d.SignUps, "inscrit", "inscrits")} par session",
                $"« Peut-être » : **{TextChart.Percent(d.Maybes, d.SignUps + d.Maybes)}** des réponses",
            };
            if (d.FillRate is { } fill)
                lines.Add($"Remplissage : **{Math.Round(fill * 100)} %** des places (sessions avec une limite)");
            embed.AddField("Inscriptions", Cap(string.Join('\n', lines)));
        }

        embed.AddField("Présence", d.RollCalls == 0
            ? "Je n'ai fait l'appel sur aucune session de jeu ou de film de la période."
            : Cap($"Appel fait sur **{d.RollCalls}** {Plural(d.RollCalls, "session", "sessions")}\n" +
                  $"Venus : **{d.Came}/{d.Expected}** ({TextChart.Percent(d.Came, d.Expected)})" +
                  (p is { Expected: > 0 } ? $" · avant : {TextChart.Percent(p.Came, p.Expected)}" : "") + "\n" +
                  $"Absences : **{d.Missed}**\n" +
                  $"En retard : **{d.Late}**" + (d.MedianLateMinutes is { } m ? $" (médiane : +{m} min)" : "")));

        if (d.Weekdays.Count > 0)
        {
            var days = d.Weekdays.Select(s =>
                $"{Weekday(s.Slot)} — {s.Sessions} {Plural(s.Sessions, "session", "sessions")}" +
                (s.Expected > 0 ? $" · {TextChart.Percent(s.Came, s.Expected)} présents" : ""));
            var hours = string.Join(" · ", d.Hours.Select(s => $"{s.Slot} h ({s.Sessions})"));
            embed.AddField("Créneaux", Cap(string.Join('\n', days) + (hours.Length > 0 ? $"\n-# Heures : {hours}" : "")));
        }

        if (names)
        {
            AddPeople(embed, "Organisateurs", d.Organisers, t => $"{t.Count} {Plural(t.Count, "tenue", "tenues")} sur {t.Of}", nameOf);
            AddPeople(embed, "Les plus fiables", d.Reliable, t => $"{t.Count}/{t.Of}", nameOf);
            AddPeople(embed, "Absences", d.NoShows, t => $"{t.Count} sur {t.Of}", nameOf);
            AddPeople(embed, "Retards", d.Latecomers, t => $"{t.Count} sur {t.Of} {Plural(t.Of, "venue", "venues")}", nameOf);
        }

        return embed.WithFooter(Footer(w, "Présence : sessions de jeu et de film dont j'ai fait l'appel.")).Build();
    }

    // ---- /report polls ------------------------------------------------------------------------

    public static Embed BuildPolls(Compared<PollReportData> c, ReportWindow w, bool names, Func<ulong, string?>? nameOf = null)
    {
        var d = c.Current;
        var p = c.Previous;
        var pace = w.Pace(c.FirstDay);
        var embed = Start("🗳️ Les sondages et /tonight", w);
        var total = d.DatePolls + d.TextPolls;

        if (total == 0 && d.TonightCalls == 0)
            return embed.WithDescription("Rien de lancé sur cette période.").Build();

        embed.WithDescription(
            $"**{total}** {Plural(total, "sondage lancé", "sondages lancés")}{TextChart.Trend(total, p is null ? null : p.DatePolls + p.TextPolls, pace)}" +
            $" ({d.DatePolls} de dates · {d.TextPolls} de votes) · {d.Open} encore {Plural(d.Open, "ouvert", "ouverts")}");

        if (total > 0)
            embed.AddField("Sondages",
                $"Votants en moyenne : **{Num(d.AverageVoters ?? 0)}**\n" +
                $"Morts sans un vote : **{d.Dead}**");

        embed.AddField("Mon sondage du mercredi", d.WeekendPosted == 0
            ? "Pas posté sur cette période."
            : $"{Plural(d.WeekendPosted, "Posté", "Postés")} : **{d.WeekendPosted}** · avec un gagnant : **{d.WeekendWon}**" +
              $" · {Plural(d.WeekendBecameSession, "devenu session", "devenus sessions")} : **{d.WeekendBecameSession}**");

        embed.AddField("/tonight", d.TonightCalls == 0
            ? "Aucun appel."
            : $"Appels : **{d.TonightCalls}**{TextChart.Trend(d.TonightCalls, p?.TonightCalls, pace)}" +
              $" · seuil atteint : **{d.TonightReached}** · {Plural(d.TonightBecameSession, "devenu session", "devenus sessions")} : **{d.TonightBecameSession}**\n" +
              $"« Chaud » en moyenne : **{Num(d.AverageTonightIn ?? 0)}** par appel");

        if (names)
        {
            AddPeople(embed, "Les plus votants", d.Voters, t => $"{t.Count} {Plural(t.Count, "sondage", "sondages")}", nameOf);
            AddPeople(embed, "Les plus chauds (/tonight)", d.Keen, t => $"{t.Count} « chaud »", nameOf);
        }

        return embed.WithFooter(Footer(w)).Build();
    }

    // ---- /report community --------------------------------------------------------------------

    public static Embed BuildCommunity(Compared<CommunityReportData> c, ReportWindow w, bool names, Func<ulong, string?>? nameOf = null)
    {
        var d = c.Current;
        var p = c.Previous;
        var pace = w.Pace(c.FirstDay);
        var embed = Start("👥 La communauté", w);

        embed.WithDescription(
            $"**{d.ActiveMembers}** {Plural(d.ActiveMembers, "membre actif", "membres actifs")}{TextChart.Trend(d.ActiveMembers, p?.ActiveMembers)}" +
            (d.DailyActive.Count > 1 ? $"\n`{TextChart.Sparkline(d.DailyActive)}`" : "") +
            PartialNote(w, c.FirstDay));

        embed.AddField("Activité",
            $"XP gagnée : **{LevelCardUi.Xp(d.Xp)}**{TextChart.Trend(d.Xp, p?.Xp, pace)}\n" +
            $"Vocal : **{Duration(d.VoiceMinutes)}**{TextChart.Trend(d.VoiceMinutes, p?.VoiceMinutes, pace)}\n" +
            $"Réactions : **{LevelCardUi.Xp(d.Reactions)}**{TextChart.Trend(d.Reactions, p?.Reactions, pace)}");

        var net = d.Joins - d.Leaves;
        embed.AddField("Arrivées et départs",
            $"Arrivées : **{d.Joins}** · départs : **{d.Leaves}** · solde : **{(net > 0 ? "+" : net < 0 ? "−" : "")}{Math.Abs(net)}**");

        embed.AddField("Émotes", d.TopEmotes.Count == 0
            ? "—"
            : Cap(string.Join(" · ", d.TopEmotes.Select(e => $"{e.Markup} ×{LevelCardUi.Xp(e.Count)}"))));

        embed.AddField("Quiz", d.QuizRounds == 0
            ? "Aucune question posée."
            : $"Questions posées : **{d.QuizRounds}** · trouvées : **{d.QuizWon}**" +
              $" · par **{d.QuizWinners}** {Plural(d.QuizWinners, "personne", "personnes")}");

        if (names)
        {
            AddPeople(embed, "Les plus actifs, tout compris", CommunityReport.Busiest(d, p),
                x => $"{LevelCardUi.Xp(x.Value)} XP{TextChart.Trend(x.Value, x.Before, pace)} · {TextChart.Percent(x.Value, d.Xp)} du total", x => x.UserId, nameOf);
            var chatTotal = d.ChatXpByUser.Values.Sum();
            AddPeople(embed, "💬 À l'écrit", CommunityReport.BusiestInChat(d, p),
                x => $"{LevelCardUi.Xp(x.Value)} XP{TextChart.Trend(x.Value, x.Before, pace)} · {TextChart.Percent(x.Value, chatTotal)} du total", x => x.UserId, nameOf);
            AddPeople(embed, "🎙️ En vocal", CommunityReport.BusiestInVoice(d, p),
                x => $"{Duration(x.Value)}{TextChart.Trend(x.Value, x.Before, pace)} · {TextChart.Percent(x.Value, d.VoiceMinutes)} du total", x => x.UserId, nameOf);
            AddPeople(embed, "Ça se calme", CommunityReport.Fading(d, p),
                x => $"rien cette fois, {LevelCardUi.Xp(x.Before)} XP avant", x => x.UserId, nameOf);
        }

        return embed.WithFooter(Footer(w,
            "Actif : de l'XP, des réactions ou du vocal ce jour-là. Vocal : le temps qui rapporte de l'XP, à deux ou plus, micro ouvert. " +
            "À l'écrit : l'XP hors vocal, surtout les messages, comptés une fois par minute au plus. " +
            "Arrivées et départs comptés depuis le 19/10/2026.")).Build();
    }

    // ---- /report syncs ------------------------------------------------------------------------

    public static Embed BuildSyncs(Compared<SyncsReportData> c, ReportWindow w, (int Day, int MaxMilli)? record, bool names,
        Func<ulong, string?>? nameOf = null)
    {
        var d = c.Current;
        var p = c.Previous;
        var embed = Start("🍄 Moi", w);

        var body = d.DaysMeasured == 0
            ? "Aucun relevé de température sur cette période."
            : $"Moyenne **{Temp(d.AverageMilli ?? 0)}** · max **{Temp(d.MaxMilli ?? 0)}**" +
              (d.MaxDay is { } day ? $" le {AppTime.FromDayKey(day):dd/MM}" : "") +
              $" · **{d.HotMinutes}** min à {Temp(PiHealth.ThrottleMilli)} ou plus" +
              (d.DailyMax.Count > 1 ? $"\n`{TextChart.Sparkline(d.DailyMax)}`" : "");
        if (record is { } r)
            body += $"\n-# Record absolu : {Temp(r.MaxMilli)} le {AppTime.FromDayKey(r.Day):dd/MM/yyyy}";
        embed.WithDescription(body);

        var outages = d.Outages.Count == 0
            ? "Aucune panne."
            : string.Join('\n', d.Outages.Select(o => $"{OutageLabel(o.Kind, o.Count)} : **{o.Count}**"));
        outages += $"\nHors ligne : **{Duration((long)d.Downtime.TotalMinutes)}**" +
                   (d.Uptime is { } up ? $" · disponibilité **{(up * 100).ToString("0.0", Fr)} %**{UptimeSpan(d, w)}" : "");
        embed.AddField("Pannes", outages);

        if (d.Body is { } b)
            embed.AddField("Disque, mémoire et courant", BodyLines(b, w));

        embed.AddField("Bavardage", d.Answers == 0
            ? "Personne ne m'a parlé."
            : $"Réponses données : **{LevelCardUi.Xp(d.Answers)}**{TextChart.Trend(d.Answers, p?.Answers, w.Pace(d.AnswersSince))}{DaysCounted(w, d.AnswersSince)}\n" +
              $"Sans réponse précise : **{TextChart.Percent(d.Unrecognised, d.Answers)}**" +
              (p is { Answers: > 0 } ? $" · avant : {TextChart.Percent(p.Unrecognised, p.Answers)}" : ""));

        var verdicts = d.GoodBot + d.BadBot;
        embed.AddField("Réputation", verdicts == 0
            ? "Ni good bot, ni bad bot."
            : $"Good bot : **{d.GoodBot}** · bad bot : **{d.BadBot}** ({TextChart.Percent(d.GoodBot, verdicts)} de gentils)");

        var voice = $"**{Duration(d.VoiceMinutes)}** de compagnie{TextChart.Trend(d.VoiceMinutes, p?.VoiceMinutes, w.Pace(d.VoiceSince))}{DaysCounted(w, d.VoiceSince)}";
        if (names && d.Companions.Count > 0)
            voice += "\n" + string.Join('\n', d.Companions.Select(t => $"{Who(t.UserId, nameOf)} — {Duration(t.Count)}"));
        embed.AddField("En vocal avec moi", Cap(voice));

        return embed.WithFooter(Footer(w, "Disponibilité : depuis mon premier relevé de température de la période.")).Build();
    }

    // « sur 23 jours (depuis le 09/10) »: an uptime means little without what it was measured over.
    private static string UptimeSpan(SyncsReportData d, ReportWindow w) =>
        d.UptimeDays <= 0 ? ""
        : $" sur {d.UptimeDays} {Plural(d.UptimeDays, "jour", "jours")}" +
          (d.UptimeFromDay is { } from && from > w.FromDay ? $" (depuis le {AppTime.FromDayKey(from):dd/MM})" : "");

    private static string BodyLines(PiBodyReportData b, ReportWindow w)
    {
        var lines = new List<string>();
        if (b.Database is { } database)
            lines.Add($"Base : **{HealthCards.Bytes(database)}**" +
                (b.DatabaseGrowth is { } growth ? $" ({(growth < 0 ? "−" : "+")}{HealthCards.Bytes(Math.Abs(growth))})" : "") +
                $" · journal au plus haut : {HealthCards.Bytes(b.WalPeak)}");
        if (b.DiskFree is { } free && b.DiskTotal is { } total)
            lines.Add($"Disque libre : **{TextChart.Percent(free, total)}** ({HealthCards.Bytes(free)} sur {HealthCards.Bytes(total)})" +
                (b.DaysUntilFull is { } days && days <= DigestBrief.DiskFullShownDays ? $" · plein dans {days} {Plural(days, "jour", "jours")} à ce rythme" : ""));
        lines.Add($"Mémoire : pic à **{HealthCards.Bytes(b.ProcessPeak)}** pour moi" +
            (b.MemoryMinAvailable is { } low && b.MemoryTotal is { } all ? $" · {HealthCards.Bytes(low)} libres au plus bas sur {HealthCards.Bytes(all)}" : ""));
        lines.Add($"Sous-tension : **{Duration(b.UnderVoltageMinutes)}** · au ralenti : **{Duration(b.SlowedMinutes)}**");
        if (b.FirstDay > w.FromDay) lines.Add($"-# Mesuré depuis le {AppTime.FromDayKey(b.FirstDay):dd/MM}.");
        return string.Join('\n', lines);
    }

    // ---- /report activity ---------------------------------------------------------------------

    public static Embed BuildActivity(Compared<ActivityReportData> c, ReportWindow w, bool names)
    {
        var d = c.Current;
        var p = c.Previous;
        var pace = w.Pace(c.FirstDay);
        var embed = Start("📈 L'activité", w);
        if (d.Messages == 0 && d.VoiceMinutes == 0)
            return embed.WithDescription("Rien de compté sur cette période.").Build();

        embed.WithDescription(
            $"**{LevelCardUi.Xp(d.Messages)}** messages{TextChart.Trend(d.Messages, p?.Messages, pace)}" +
            $" · **{Duration(d.VoiceMinutes)}** de vocal cumulé{TextChart.Trend(d.VoiceMinutes, p?.VoiceMinutes, pace)}" +
            PartialNote(w, c.FirstDay));

        var peak = $"Messages `0h {TextChart.Sparkline(d.MessagesByHour)} 23h`\n" +
                   $"Vocal       `0h {TextChart.Sparkline(d.VoiceByHour)} 23h`";
        if (d.BusiestHours.Count > 0) peak += $"\nHeures de pointe : {string.Join(" · ", d.BusiestHours.Select(h => $"{h} h"))}";
        if (d.BusiestWeekday is { } day) peak += $"\nJour le plus bavard : {Weekday((int)day)}";
        embed.AddField("Heures de pointe", peak);

        embed.AddField("Salons écrits", d.TopText.Count == 0 ? "—" : Cap(string.Join('\n', d.TopText.Select(l =>
            $"{Mark(l)}<#{l.ChannelId}> — {LevelCardUi.Xp(l.Value)}{TextChart.Trend(l.Value, p?.MessagesByChannel.GetValueOrDefault(l.ChannelId), pace)}"))));
        if (d.SilentText.Count > 0)
            embed.AddField($"Salons silencieux ({d.SilentText.Count})", Fit(d.SilentText.Select(l => $"{Mark(l)}<#{l.ChannelId}>").ToList(), " · "));

        embed.AddField("Salons vocaux", d.TopVoice.Count == 0 ? "Personne en vocal." : Cap(string.Join('\n', d.TopVoice.Select(l =>
            $"{Mark(l)}<#{l.ChannelId}> — {Duration(l.Value)} · jusqu'à {l.Peak} {Plural(l.Peak, "personne", "personnes")}"))));
        if (d.EmptyVoice.Count > 0)
            embed.AddField($"Vocaux inutilisés ({d.EmptyVoice.Count})", Fit(d.EmptyVoice.Select(l => $"{Mark(l)}<#{l.ChannelId}>").ToList(), " · "));

        var marked = d.TopText.Concat(d.SilentText).Concat(d.TopVoice).Concat(d.EmptyVoice).Any(l => l.Excluded);
        return embed.WithFooter(Footer(w,
            (marked ? "🚫 : salon exclu (rien n'y rapporte d'XP). " : "") + "Vocal cumulé : minutes × personnes présentes, micro coupé compris.")).Build();
    }

    // ---- /report commands ---------------------------------------------------------------------

    public static Embed BuildCommands(Compared<CommandReportData> c, ReportWindow w, bool names)
    {
        var d = c.Current;
        var p = c.Previous;
        var pace = w.Pace(c.FirstDay);
        var embed = Start("⌨️ Mes commandes", w);
        embed.WithDescription(d.Uses == 0
            ? "Aucune commande utilisée sur cette période (comptage depuis la 7.6.0)." + DebugNote(d)
            : $"**{LevelCardUi.Xp(d.Uses)}** {Plural(d.Uses, "utilisation", "utilisations")}{TextChart.Trend(d.Uses, p?.Uses, pace)}" +
              (d.People.Count > 0 ? $" · par **{d.People.Count}** {Plural(d.People.Count, "personne", "personnes")}" : "") +
              (d.Failures > 0 ? $" · **{d.Failures}** en erreur ({TextChart.Percent(d.Failures, d.Uses)})" : "") +
              (CommandReport.Returning(c) is { } back ? $"\nDéjà là avant : **{back}** sur {d.People.Count}" : "") +
              PartialNote(w, c.FirstDay) + DebugNote(d));

        if (d.Top.Count > 0)
            embed.AddField("Les plus utilisées", Cap(string.Join('\n', d.Top.Select(l =>
                $"`/{l.Command}` — {LevelCardUi.Xp(l.Uses)}{TextChart.Trend(l.Uses, p?.UsesByCommand.GetValueOrDefault(l.Command), pace)}" +
                (d.PeopleByCommand.GetValueOrDefault(l.Command) is > 0 and var who ? $" · {who} {Plural(who, "personne", "personnes")}" : "") +
                (l.Failures > 0 ? $" · ⚠️ {l.Failures}" : "")))));
        if (d.Features.Count > 0)
            embed.AddField("Portée", Fit(d.Features.Select(f => $"`/{f.Feature}` {f.People}").ToList(), " · ") +
                (w.IsPartial(d.PeopleSince) && d.PeopleSince is { } since ? $"\n-# Personnes différentes, comptées depuis le {AppTime.FromDayKey(since):dd/MM}." : "\n-# Personnes différentes."));
        if (d.Shortcuts.Count > 0)
            embed.AddField("Raccourcis tapés", Fit(d.Shortcuts.Select(l => $"`{l.Command}` {LevelCardUi.Xp(l.Uses)}").ToList(), " · "));
        if (d.NeverUsed.Count > 0)
            embed.AddField($"Pas utilisées ({d.NeverUsed.Count})", Fit(d.NeverUsed.Select(x => $"`/{x}`").ToList(), " · "));
        if (d.NeverUsedStaff.Count > 0)
            embed.AddField($"Pas utilisées, côté staff ({d.NeverUsedStaff.Count})", Fit(d.NeverUsedStaff.Select(x => $"`/{x}`").ToList(), " · "));

        return embed.WithFooter(Footer(w, "Les commandes en message privé ne comptent pas ici. Comptage depuis la 7.6.0.")).Build();
    }

    private static string DebugNote(CommandReportData d) =>
        d.DebugUses > 0 ? $"\n-# Hors classement : `/debug`, {LevelCardUi.Xp(d.DebugUses)} fois." : "";

    // ---- /report quiz -------------------------------------------------------------------------

    public static Embed BuildQuiz(Compared<QuizReportData> c, ReportWindow w, bool names, Func<ulong, string?>? nameOf = null)
    {
        var d = c.Current;
        var p = c.Previous;
        var pace = w.Pace(c.FirstDay);
        var embed = Start("❓ Le quiz", w);
        if (d.Posted == 0)
            return embed.WithDescription("Aucune question posée sur cette période.").Build();

        embed.WithDescription(
            $"**{d.Posted}** {Plural(d.Posted, "question posée", "questions posées")}{TextChart.Trend(d.Posted, p?.Posted, pace)}" +
            $" · trouvées : **{d.Found}** ({TextChart.Percent(d.Found, d.Posted)})" +
            (p is { Posted: > 0 } ? $" · avant : {TextChart.Percent(p.Found, p.Posted)}" : "") +
            $" · sans réponse : **{d.Unanswered}**" +
            (d.MedianSeconds is { } s ? $"\nTemps de réponse médian : **{Seconds(s)}**" : "") +
            PartialNote(w, c.FirstDay));
        embed.AddField("Par catégorie", Cap(Groups(d.ByCategory)), inline: true);
        embed.AddField("Par difficulté", Cap(Groups(d.ByDifficulty)), inline: true);
        if (d.UnfoundPrompts.Count > 0)
            embed.AddField("Personne n'a trouvé", Cap(string.Join('\n', d.UnfoundPrompts.Select(q => $"• {q}"))));

        if (names)
        {
            AddPeople(embed, "Les meilleurs", d.Winners, t => $"{t.Count} {Plural(t.Count, "victoire", "victoires")}", nameOf);
            if (d.Fastest is { } f)
                embed.AddField("Le plus rapide", $"{Who(f.UserId, nameOf)} — " +
                    (f.Count < 60_000 ? $"{(f.Count / 1000.0).ToString("0.0", Fr)} s" : Seconds(f.Count / 1000)), inline: true);
        }
        return embed.WithFooter(Footer(w)).Build();
    }

    private static string Groups(IReadOnlyList<QuizGroupLine> lines) =>
        lines.Count == 0 ? "—" : string.Join('\n', lines.Select(l => $"{Capitalise(l.Label)} — {l.Found}/{l.Posted}"));

    private static string Seconds(int s) => s < 60 ? $"{s} s" : $"{s / 60} min {s % 60:00}";

    // ---- /report plynlings --------------------------------------------------------------------

    public static Embed BuildPlynlings(Compared<PlynlingReportData> c, ReportWindow w, bool names, Func<ulong, string?>? nameOf = null)
    {
        var d = c.Current;
        var p = c.Previous;
        var pace = w.Pace(c.FirstDay);
        var embed = Start("🌱 Les Plynlings", w);
        var deaths = d.Deaths.Sum(x => x.Count);
        embed.WithDescription(
            $"**{d.Living}** en vie · **{d.Born}** {Plural(d.Born, "adoption", "adoptions")}{TextChart.Trend(d.Born, p?.Born, pace)}" +
            $" · **{deaths}** {Plural(deaths, "décès", "décès")}" +
            (deaths > 0 ? $" ({string.Join(", ", d.Deaths.Select(x => $"{DeathLabel(x.Cause)} : {x.Count}"))})" : "") +
            (d.AverageLifespan is { } life ? $"\nDurée de vie moyenne des disparus : **{Math.Max(1, (int)Math.Round(life.TotalDays))} jours**" : ""));

        embed.AddField("Les vivants, maintenant",
            $"Malades : **{d.Sick}** · très faim : **{d.Starving}** · faim : **{d.Hungry}**\n" +
            $"Sales : **{d.Dirty}** · gelés : **{d.Frozen}**\n" +
            (d.Stages.Count == 0 ? "—" : string.Join(" · ", d.Stages.Select(s => $"{StageLabel(s.Stage)} : {s.Count}"))));

        embed.AddField("Événements", d.EventsResolved == 0
            ? "Aucun événement résolu."
            : Cap($"Résolus : **{d.EventsResolved}**{TextChart.Trend(d.EventsResolved, p?.EventsResolved, pace)}" +
                  $" · choisis seuls : **{d.DecidedAlone}** ({TextChart.Percent(d.DecidedAlone, d.EventsResolved)})\n" +
                  (d.Challenges > 0 ? $"Défis réussis : **{d.ChallengesWon}/{d.Challenges}**\n" : "") +
                  string.Join('\n', d.TopEvents.Select(e => $"• {e.Title} ×{e.Count}"))));

        embed.AddField("Liens et progrès",
            (d.Bonds.Count == 0 ? "Aucun lien noué." : string.Join(" · ", d.Bonds.Select(b => $"{BondLabel(b.Bond)} : {b.Count}"))) +
            $"\nBadges gagnés : **{d.Badges}** · traits acquis : **{d.Traits}**");

        if (names && d.Watch.Count > 0)
            embed.AddField("À surveiller", Cap(string.Join('\n', d.Watch.Select(x =>
                $"{Who(x.OwnerId, nameOf)} — {Format.Sanitize(x.Name)}{(x.Sick ? " · malade" : "")}{(x.Starving ? " · très faim" : "")}"))));

        return embed.WithFooter(Footer(w,
            "« Choisis seuls » : sans réponse de son maître, le Plynling a décidé. Ping-Qilin n'est pas compté.")).Build();
    }

    private static string DeathLabel(DeathCause cause) => cause switch
    {
        DeathCause.Illness => "maladie",
        _ => "faim",
    };

    private static string StageLabel(PlynlingStage stage) => stage switch
    {
        PlynlingStage.Baby => "Bébés",
        PlynlingStage.Teen => "Ados",
        PlynlingStage.Adult => "Adultes",
        _ => "Anciens",
    };

    private static string BondLabel(PlynlingBond bond) => bond switch
    {
        PlynlingBond.Friends => "Amitiés",
        PlynlingBond.BestFriends => "Meilleurs amis",
        PlynlingBond.Lovers => "Amours",
        PlynlingBond.Rivals => "Rivalités",
        PlynlingBond.Enemies => "Inimitiés",
        _ => "Connaissances",
    };

    // ---- /report emotes -----------------------------------------------------------------------

    public static Embed BuildEmotes(Compared<EmoteReportData> c, ReportWindow w, bool names)
    {
        var d = c.Current;
        var p = c.Previous;
        var pace = w.Pace(c.FirstDay);
        var embed = Start("Les émotes", w);
        var total = d.Own + d.Foreign + d.Unicode;
        embed.WithDescription(total == 0
            ? "Aucune émote utilisée sur cette période."
            : $"**{LevelCardUi.Xp(total)}** émotes{TextChart.Trend(total, p is null ? null : p.Own + p.Foreign + p.Unicode, pace)}" +
              $" · du serveur : **{TextChart.Percent(d.Own, total)}** · d'ailleurs : **{TextChart.Percent(d.Foreign, total)}**" +
              $" · emoji : **{TextChart.Percent(d.Unicode, total)}**" + PartialNote(w, c.FirstDay));

        if (d.TopOwn.Count > 0)
            embed.AddField("Les préférées du serveur", Fit(d.TopOwn.Select(e => $"{e.Markup} ×{LevelCardUi.Xp(e.Count)}").ToList(), " · "));
        if (p is not null)
        {
            var up = EmoteReport.Movers(d, p, rising: true);
            var down = EmoteReport.Movers(d, p, rising: false);
            if (up.Count + down.Count > 0)
                embed.AddField("Tendances",
                    (up.Count > 0 ? "En hausse : " + string.Join(" · ", up.Select(e => $"{e.Markup} +{e.Count}")) : "") +
                    (up.Count > 0 && down.Count > 0 ? "\n" : "") +
                    (down.Count > 0 ? "En baisse : " + string.Join(" · ", down.Select(e => $"{e.Markup} −{-e.Count}")) : ""));
        }
        if (d.UnusedOwn.Count > 0)
            embed.AddField($"Pas utilisées ({d.UnusedOwn.Count})", Fit(d.UnusedOwn, " "));
        if (d.TopForeign.Count > 0)
            embed.AddField("Venues d'autres serveurs", Fit(d.TopForeign.Select(e => $"{e.Markup} ×{LevelCardUi.Xp(e.Count)}").ToList(), " · "));

        return embed.WithFooter(Footer(w, "Écrites et en réaction.")).Build();
    }

    // ---- /report minecraft --------------------------------------------------------------------

    public static Embed BuildMinecraft(Compared<MinecraftReportData> c, ReportWindow w, bool names)
    {
        var d = c.Current;
        var p = c.Previous;
        var pace = w.Pace(c.FirstDay);
        var embed = Start("⛏️ Minecraft", w);
        if (!d.Played)
            return embed.WithDescription("Elle n'a pas joué sur cette période.").WithFooter(Footer(w, MinecraftNote)).Build();

        embed.WithDescription(
            $"**{Duration(d.Minutes)}** en jeu{TextChart.Trend(d.Minutes, p?.Minutes, pace)}" +
            $" sur **{d.DaysPlayed}** {Plural(d.DaysPlayed, "jour", "jours")}" + PartialNote(w, c.FirstDay));

        var orders = $"**{LevelCardUi.Xp(d.Orders)}** {Plural(d.Orders, "ordre", "ordres")}{TextChart.Trend(d.Orders, p?.Orders, pace)}";
        if (names && d.TopPlayers.Count > 0)
            orders += "\n" + string.Join('\n', d.TopPlayers.Select(l => $"**{Format.Sanitize(l.Name)}** — {LevelCardUi.Xp(l.Count)}"));
        embed.AddField("Ordres", Cap(orders));

        embed.AddField("Aventures",
            $"Morts : **{LevelCardUi.Xp(d.Deaths)}** · monstres tués : **{LevelCardUi.Xp(d.Kills)}**{TextChart.Trend(d.Kills, p?.Kills, pace)}\n" +
            $"Poissons : **{LevelCardUi.Xp(d.Fish)}** · trésors : **{LevelCardUi.Xp(d.Treasure)}**");

        var gathered = d.Gathered.Select(l => $"{ResourceLabel(l.Name)} ×{LevelCardUi.Xp(l.Count)}").ToList();
        embed.AddField("Récoltes",
            $"Cultures récoltées : **{LevelCardUi.Xp(d.Harvested)}**{TextChart.Trend(d.Harvested, p?.Harvested, pace)}" +
            (gathered.Count > 0 ? "\n" + Fit(gathered, " · ") : ""));

        return embed.WithFooter(Footer(w, MinecraftNote)).Build();
    }

    private const string MinecraftNote = "Ordres : ceux qu'elle a lancés. Comptage par jour depuis la 7.50.0.";

    // A resource as the body names it (« wood »), as she does without the partitive (« Bois »); the key itself if unknown.
    private static string ResourceLabel(string key)
    {
        if (!Enum.TryParse<McResource>(key, ignoreCase: true, out var resource) || !BotResponses.McResourceNames.TryGetValue(resource, out var name))
            return key;
        foreach (var article in new[] { "de la ", "de l'", "du ", "des " })
            if (name.StartsWith(article, StringComparison.Ordinal)) return Capitalise(name[article.Length..]);
        return Capitalise(name);
    }

    // ---- /report sleeping ---------------------------------------------------------------------

    public static Embed BuildSleeping(SleepingReportData d, Func<ulong, string?>? nameOf = null)
    {
        var never = d.Sleepers.Where(s => s.LastDay is null).ToList();
        var embed = new EmbedBuilder()
            .WithTitle($"💤 Les endormis — rien depuis {d.Days} jours")
            .WithColor(Color.Purple)
            .WithDescription(d.Sleepers.Count == 0
                ? "Tout le monde a donné signe de vie. Pour l'instant."
                : $"**{d.Sleepers.Count}** sur **{d.Members}** {Plural(d.Members, "membre", "membres")}" +
                  $" ({TextChart.Percent(d.Sleepers.Count, d.Members)}) sans XP, réaction ni vocal depuis {d.Days} jours" +
                  (never.Count > 0 ? $", dont **{never.Count}** {Plural(never.Count, "jamais actif", "jamais actifs")}." : "."));

        var lines = d.Sleepers.Where(s => s.LastDay is not null)
            .Select(s => $"{Who(s.UserId, nameOf)} — dernier signe : {AppTime.FromDayKey(s.LastDay!.Value):dd/MM/yyyy}").ToList();
        var left = AddLineFields(embed, "Endormis", lines, maxFields: 2);

        // On one line: they all say the same thing, and would push out the ones who just went quiet.
        if (never.Count > 0)
            embed.AddField($"Jamais actifs depuis le {AppTime.FromDayKey(SleepingReport.ReliableSince):dd/MM/yyyy} ({never.Count})",
                Fit(never.Select(s => Who(s.UserId, nameOf)).ToList(), " · "));

        var footer = $"Arrivés depuis moins de {d.Days} jours : pas comptés. L'activité dans les salons exclus ne compte pas : " +
                     "qui ne parle que là semble endormi.";
        if (left > 0) footer = $"{AndMore(left)}. " + footer;
        return embed.WithFooter(footer).Build();
    }

    public static MessageComponent SleepRow(int days)
    {
        var row = new ComponentBuilder();
        foreach (var choice in SleepingReport.Choices)
            row.WithButton($"{choice} jours", $"report:sleep:{choice}",
                choice == days ? ButtonStyle.Primary : ButtonStyle.Secondary, disabled: choice == days);
        return row.Build();
    }

    // Whole lines into up to maxFields fields; returns how many lines didn't fit.
    private static int AddLineFields(EmbedBuilder embed, string title, IReadOnlyList<string> lines, int maxFields)
    {
        var index = 0;
        for (var f = 0; f < maxFields && index < lines.Count; f++)
        {
            var sb = new StringBuilder();
            while (index < lines.Count && sb.Length + lines[index].Length + 1 <= MaxFieldLength - 30)
            {
                sb.Append(lines[index]).Append('\n');
                index++;
            }
            if (sb.Length == 0) break;
            embed.AddField(f == 0 ? title : "​", sb.ToString().TrimEnd());
        }
        return lines.Count - index;
    }

    // ---- Shared -------------------------------------------------------------------------------

    private static EmbedBuilder Start(string title, ReportWindow w) => new EmbedBuilder()
        .WithTitle($"{title} — {w.Label}")
        .WithColor(Color.Purple);

    // A line under the figures when a counter started inside the window, so a partial month can't pass
    // for a whole one. Nothing otherwise.
    private static string PartialNote(ReportWindow w, int? firstDay) =>
        w.IsPartial(firstDay) && firstDay is { } first
            ? $"\n-# {(w.Period is null ? "Mois partiel : compté" : "Compté")} depuis le {AppTime.FromDayKey(first):dd/MM}{DaysCounted(w, firstDay)}."
            : "";

    // The same, short enough to follow a figure: « (13 jours sur 31) ».
    private static string DaysCounted(ReportWindow w, int? firstDay)
    {
        if (!w.IsPartial(firstDay)) return "";
        var days = w.CountedDays(firstDay);
        return $" ({days} {Plural(days, "jour", "jours")} sur {w.TotalDays})";
    }

    private static string Footer(ReportWindow w, string? note = null)
    {
        var trend = w.IsAllTime ? null
            : w.Period is null ? "Tendances : par jour compté, comparées au mois d'avant."
            : "Tendances : par jour compté, comparées à la période d'avant, de même durée.";
        return string.Join(" ", new[] { note, trend }.Where(s => s is not null));
    }

    private static void AddPeople(EmbedBuilder embed, string title, IReadOnlyList<PersonTally> people, Func<PersonTally, string> detail,
        Func<ulong, string?>? nameOf) =>
        AddPeople(embed, title, people, detail, t => t.UserId, nameOf);

    private static void AddPeople<T>(EmbedBuilder embed, string title, IReadOnlyList<T> people, Func<T, string> detail, Func<T, ulong> id,
        Func<ulong, string?>? nameOf)
    {
        if (people.Count == 0) return;
        embed.AddField(title, Cap(string.Join('\n', people.Select(x => $"{Who(id(x), nameOf)} — {detail(x)}"))), inline: true);
    }

    /// <summary>
    /// Someone named in a report: their name written out, then the mention to click. The mention alone
    /// shows a raw id to a reader whose app hasn't loaded that member (the digest's DMs, mostly). With
    /// no name known (gateway down, a member long gone), the mention alone.
    /// </summary>
    public static string Who(ulong id, Func<ulong, string?>? nameOf) =>
        nameOf?.Invoke(id) is { Length: > 0 } name ? $"**{Format.Sanitize(name)}** (<@{id}>)" : $"<@{id}>";

    private static string Category(SessionCategory category) => SessionCategories.Label(category);

    // DayOfWeek numbering: Sunday is 0.
    private static string Weekday(int dayOfWeek) => Capitalise(Fr.DateTimeFormat.GetDayName((DayOfWeek)dayOfWeek));

    private static string OutageLabel(UptimeEventKind kind, int count) => kind switch
    {
        UptimeEventKind.PowerCut => Plural(count, "Coupure de courant", "Coupures de courant"),
        UptimeEventKind.Crash => Plural(count, "Plantage", "Plantages"),
        _ => Plural(count, "Coupure réseau (le routeur)", "Coupures réseau (le routeur)"),
    };

    private static string Temp(int milli) => $"{(milli / 1000.0).ToString("0.0", Fr)} °C";

    private static string Duration(long minutes) =>
        minutes < 60 ? $"{minutes} min" : $"{minutes / 60} h {minutes % 60:00}";

    private static string Num(double value) => value.ToString("0.#", Fr);

    private static string Plural(long n, string one, string many) => n is 0 or 1 ? one : many;

    private static string Capitalise(string s) => s.Length == 0 ? s : char.ToUpper(s[0], Fr) + s[1..];

    // Cut on a line break when there is one, so no row is shown half.
    private static string Cap(string text)
    {
        if (text.Length <= MaxFieldLength) return text;
        var cut = text.LastIndexOf('\n', MaxFieldLength - 2);
        return (cut > 0 ? text[..cut] : text[..(MaxFieldLength - 2)]) + "\n…";
    }

    private static string Mark(ChannelLine l) => l.Excluded ? "🚫 " : "";

    // Whole items only, up to a field's length: a list never cuts an emote's <:name:id> or a mention
    // in half. What doesn't fit is counted instead.
    private static string Fit(IReadOnlyList<string> items, string separator)
    {
        var sb = new StringBuilder();
        var shown = 0;
        foreach (var item in items)
        {
            var add = (shown == 0 ? "" : separator) + item;
            if (sb.Length + add.Length > MaxFieldLength - 30) break;
            sb.Append(add);
            shown++;
        }
        if (shown < items.Count) sb.Append(' ').Append(AndMore(items.Count - shown));
        return sb.Length == 0 ? "—" : sb.ToString();
    }

    private static string AndMore(int n) => $"… et {n} {Plural(n, "autre", "autres")}";
}
