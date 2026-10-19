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
/// worst-case data. Mentions in an embed render as names without pinging.
/// </summary>
/// <remarks>
/// <paramref name="names"/> false drops every field that names someone: absences are a count,
/// never names, wherever the report could be read by others (docs/agents/admin.md, /report).
/// </remarks>
public static class ReportCards
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    // Under 1024 per field; seven fields at most per embed keeps each one far below 6000.
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

    public static Embed BuildSessions(Compared<SessionReportData> c, ReportWindow w, bool names)
    {
        var d = c.Current;
        var p = c.Previous;
        var embed = Start("📋 Les sessions", w);

        if (d.Held == 0 && d.Cancelled == 0)
            return embed.WithDescription("Aucune session sur cette période.").WithFooter(Footer(w)).Build();

        var categories = string.Join(" · ", d.ByCategory.Select(x => $"{Category(x.Category)} {x.Count}"));
        embed.WithDescription(
            $"**{d.Held}** {Plural(d.Held, "session tenue", "sessions tenues")}{TextChart.Trend(d.Held, p?.Held)}" +
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
            AddPeople(embed, "Organisateurs", d.Organisers, t => $"{t.Count} {Plural(t.Count, "tenue", "tenues")} sur {t.Of}");
            AddPeople(embed, "Les plus fiables", d.Reliable, t => $"{t.Count}/{t.Of}");
            AddPeople(embed, "Absences", d.NoShows, t => $"{t.Count} sur {t.Of}");
            AddPeople(embed, "Retards", d.Latecomers, t => $"{t.Count} sur {t.Of} {Plural(t.Of, "venue", "venues")}");
        }

        return embed.WithFooter(Footer(w, "Présence : sessions de jeu et de film dont j'ai fait l'appel.")).Build();
    }

    // ---- /report polls ------------------------------------------------------------------------

    public static Embed BuildPolls(Compared<PollReportData> c, ReportWindow w, bool names)
    {
        var d = c.Current;
        var p = c.Previous;
        var embed = Start("🗳️ Les sondages et /tonight", w);
        var total = d.DatePolls + d.TextPolls;

        if (total == 0 && d.TonightCalls == 0)
            return embed.WithDescription("Rien de lancé sur cette période.").WithFooter(Footer(w)).Build();

        embed.WithDescription(
            $"**{total}** {Plural(total, "sondage lancé", "sondages lancés")}{TextChart.Trend(total, p is null ? null : p.DatePolls + p.TextPolls)}" +
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
            : $"Appels : **{d.TonightCalls}**{TextChart.Trend(d.TonightCalls, p?.TonightCalls)}" +
              $" · seuil atteint : **{d.TonightReached}** · {Plural(d.TonightBecameSession, "devenu session", "devenus sessions")} : **{d.TonightBecameSession}**\n" +
              $"« Chaud » en moyenne : **{Num(d.AverageTonightIn ?? 0)}** par appel");

        if (names)
        {
            AddPeople(embed, "Les plus votants", d.Voters, t => $"{t.Count} {Plural(t.Count, "sondage", "sondages")}");
            AddPeople(embed, "Les plus chauds (/tonight)", d.Keen, t => $"{t.Count} « chaud »");
        }

        return embed.WithFooter(Footer(w)).Build();
    }

    // ---- /report community --------------------------------------------------------------------

    public static Embed BuildCommunity(Compared<CommunityReportData> c, ReportWindow w, bool names)
    {
        var d = c.Current;
        var p = c.Previous;
        var embed = Start("👥 La communauté", w);

        embed.WithDescription(
            $"**{d.ActiveMembers}** {Plural(d.ActiveMembers, "membre actif", "membres actifs")}{TextChart.Trend(d.ActiveMembers, p?.ActiveMembers)}" +
            (d.DailyActive.Count > 1 ? $"\n`{TextChart.Sparkline(d.DailyActive)}`" : ""));

        embed.AddField("Activité",
            $"XP gagnée : **{LevelCardUi.Xp(d.Xp)}**{TextChart.Trend(d.Xp, p?.Xp)}\n" +
            $"Vocal : **{Duration(d.VoiceMinutes)}**{TextChart.Trend(d.VoiceMinutes, p?.VoiceMinutes)}\n" +
            $"Réactions : **{LevelCardUi.Xp(d.Reactions)}**{TextChart.Trend(d.Reactions, p?.Reactions)}");

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
            AddPeople(embed, "Les plus actifs", CommunityReport.Busiest(d, p),
                x => $"{LevelCardUi.Xp(x.Xp)} XP{TextChart.Trend(x.Xp, x.Before)}", x => x.UserId);
            AddPeople(embed, "Ça se calme", CommunityReport.Fading(d, p),
                x => $"rien cette fois, {LevelCardUi.Xp(x.Before)} XP avant", x => x.UserId);
        }

        return embed.WithFooter(Footer(w,
            "Actif : de l'XP, des réactions ou du vocal ce jour-là. Arrivées et départs comptés depuis la 7.5.0.")).Build();
    }

    // ---- /report syncs ------------------------------------------------------------------------

    public static Embed BuildSyncs(Compared<SyncsReportData> c, ReportWindow w, (int Day, int MaxMilli)? record, bool names)
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
                   (d.Uptime is { } up ? $" · disponibilité **{(up * 100).ToString("0.0", Fr)} %**" : "");
        embed.AddField("Pannes", outages);

        embed.AddField("Bavardage", d.Answers == 0
            ? "Personne ne m'a parlé."
            : $"Réponses données : **{LevelCardUi.Xp(d.Answers)}**{TextChart.Trend(d.Answers, p?.Answers)}\n" +
              $"Sans réponse précise : **{TextChart.Percent(d.Unrecognised, d.Answers)}**" +
              (p is { Answers: > 0 } ? $" · avant : {TextChart.Percent(p.Unrecognised, p.Answers)}" : ""));

        var verdicts = d.GoodBot + d.BadBot;
        embed.AddField("Réputation", verdicts == 0
            ? "Ni good bot, ni bad bot."
            : $"Good bot : **{d.GoodBot}** · bad bot : **{d.BadBot}** ({TextChart.Percent(d.GoodBot, verdicts)} de gentils)");

        var voice = $"**{Duration(d.VoiceMinutes)}** de compagnie{TextChart.Trend(d.VoiceMinutes, p?.VoiceMinutes)}";
        if (names && d.Companions.Count > 0)
            voice += "\n" + string.Join('\n', d.Companions.Select(t => $"<@{t.UserId}> — {Duration(t.Count)}"));
        embed.AddField("En vocal avec moi", Cap(voice));

        return embed.WithFooter(Footer(w, "Disponibilité : sur les jours où j'ai relevé ma température.")).Build();
    }

    // ---- /report activity ---------------------------------------------------------------------

    public static Embed BuildActivity(Compared<ActivityReportData> c, ReportWindow w, bool names)
    {
        var d = c.Current;
        var p = c.Previous;
        var embed = Start("📈 L'activité", w);
        if (d.Messages == 0 && d.VoiceMinutes == 0)
            return embed.WithDescription("Rien de compté sur cette période (comptage depuis la 7.6.0).").WithFooter(Footer(w)).Build();

        embed.WithDescription(
            $"**{LevelCardUi.Xp(d.Messages)}** messages{TextChart.Trend(d.Messages, p?.Messages)}" +
            $" · **{Duration(d.VoiceMinutes)}** de vocal cumulé{TextChart.Trend(d.VoiceMinutes, p?.VoiceMinutes)}");

        var peak = $"Messages `0h {TextChart.Sparkline(d.MessagesByHour)} 23h`\n" +
                   $"Vocal       `0h {TextChart.Sparkline(d.VoiceByHour)} 23h`";
        if (d.BusiestHours.Count > 0) peak += $"\nHeures de pointe : {string.Join(" · ", d.BusiestHours.Select(h => $"{h} h"))}";
        if (d.BusiestWeekday is { } day) peak += $"\nJour le plus bavard : {Weekday((int)day)}";
        embed.AddField("Heures de pointe", peak);

        embed.AddField("Salons écrits", d.TopText.Count == 0 ? "—" : Cap(string.Join('\n', d.TopText.Select(l =>
            $"{Mark(l)}<#{l.ChannelId}> — {LevelCardUi.Xp(l.Value)}{TextChart.Trend(l.Value, p?.MessagesByChannel.GetValueOrDefault(l.ChannelId))}"))));
        if (d.SilentText.Count > 0)
            embed.AddField($"Salons silencieux ({d.SilentText.Count})", Fit(d.SilentText.Select(l => $"{Mark(l)}<#{l.ChannelId}>").ToList(), " · "));

        embed.AddField("Salons vocaux", d.TopVoice.Count == 0 ? "Personne en vocal." : Cap(string.Join('\n', d.TopVoice.Select(l =>
            $"{Mark(l)}<#{l.ChannelId}> — {Duration(l.Value)} · jusqu'à {l.Peak} {Plural(l.Peak, "personne", "personnes")}"))));
        if (d.EmptyVoice.Count > 0)
            embed.AddField($"Vocaux inutilisés ({d.EmptyVoice.Count})", Fit(d.EmptyVoice.Select(l => $"{Mark(l)}<#{l.ChannelId}>").ToList(), " · "));

        return embed.WithFooter(Footer(w,
            "🚫 : salon exclu (rien n'y rapporte d'XP). Vocal cumulé : minutes × personnes présentes. Comptage depuis la 7.6.0.")).Build();
    }

    // ---- /report commands ---------------------------------------------------------------------

    public static Embed BuildCommands(Compared<CommandReportData> c, ReportWindow w, bool names)
    {
        var d = c.Current;
        var p = c.Previous;
        var embed = Start("⌨️ Mes commandes", w);
        embed.WithDescription(d.Uses == 0
            ? "Aucune commande utilisée sur cette période (comptage depuis la 7.6.0)."
            : $"**{LevelCardUi.Xp(d.Uses)}** {Plural(d.Uses, "utilisation", "utilisations")}{TextChart.Trend(d.Uses, p?.Uses)}" +
              (d.Failures > 0 ? $" · **{d.Failures}** en erreur ({TextChart.Percent(d.Failures, d.Uses)})" : ""));

        if (d.Top.Count > 0)
            embed.AddField("Les plus utilisées", Cap(string.Join('\n', d.Top.Select(l =>
                $"`/{l.Command}` — {LevelCardUi.Xp(l.Uses)}{TextChart.Trend(l.Uses, p?.UsesByCommand.GetValueOrDefault(l.Command))}" +
                (l.Failures > 0 ? $" · ⚠️ {l.Failures}" : "")))));
        if (d.Shortcuts.Count > 0)
            embed.AddField("Raccourcis tapés", Fit(d.Shortcuts.Select(l => $"`{l.Command}` {LevelCardUi.Xp(l.Uses)}").ToList(), " · "));
        if (d.NeverUsed.Count > 0)
            embed.AddField($"Jamais utilisées ({d.NeverUsed.Count})", Fit(d.NeverUsed.Select(x => $"`/{x}`").ToList(), " · "));

        return embed.WithFooter(Footer(w, "Les commandes en message privé ne comptent pas ici. Comptage depuis la 7.6.0.")).Build();
    }

    // ---- /report quiz -------------------------------------------------------------------------

    public static Embed BuildQuiz(Compared<QuizReportData> c, ReportWindow w, bool names)
    {
        var d = c.Current;
        var p = c.Previous;
        var embed = Start("❓ Le quiz", w);
        if (d.Posted == 0)
            return embed.WithDescription("Aucune question posée sur cette période.").WithFooter(Footer(w)).Build();

        embed.WithDescription(
            $"**{d.Posted}** {Plural(d.Posted, "question posée", "questions posées")}{TextChart.Trend(d.Posted, p?.Posted)}" +
            $" · trouvées : **{d.Found}** ({TextChart.Percent(d.Found, d.Posted)})" +
            (p is { Posted: > 0 } ? $" · avant : {TextChart.Percent(p.Found, p.Posted)}" : "") +
            $" · sans réponse : **{d.Unanswered}**" +
            (d.MedianSeconds is { } s ? $"\nTemps de réponse médian : **{Seconds(s)}**" : ""));
        embed.AddField("Par catégorie", Cap(Groups(d.ByCategory)), inline: true);
        embed.AddField("Par difficulté", Cap(Groups(d.ByDifficulty)), inline: true);
        if (d.UnfoundPrompts.Count > 0)
            embed.AddField("Personne n'a trouvé", Cap(string.Join('\n', d.UnfoundPrompts.Select(q => $"• {q}"))));

        if (names)
        {
            AddPeople(embed, "Les meilleurs", d.Winners, t => $"{t.Count} {Plural(t.Count, "victoire", "victoires")}");
            if (d.Fastest is { } f)
                embed.AddField("Le plus rapide", $"<@{f.UserId}> — " +
                    (f.Count < 60_000 ? $"{(f.Count / 1000.0).ToString("0.0", Fr)} s" : Seconds(f.Count / 1000)), inline: true);
        }
        return embed.WithFooter(Footer(w)).Build();
    }

    private static string Groups(IReadOnlyList<QuizGroupLine> lines) =>
        lines.Count == 0 ? "—" : string.Join('\n', lines.Select(l => $"{Capitalise(l.Label)} — {l.Found}/{l.Posted}"));

    private static string Seconds(int s) => s < 60 ? $"{s} s" : $"{s / 60} min {s % 60:00}";

    // ---- /report plynlings --------------------------------------------------------------------

    public static Embed BuildPlynlings(Compared<PlynlingReportData> c, ReportWindow w, bool names)
    {
        var d = c.Current;
        var p = c.Previous;
        var embed = Start("🌱 Les Plynlings", w);
        var deaths = d.Deaths.Sum(x => x.Count);
        embed.WithDescription(
            $"**{d.Living}** en vie · **{d.Born}** {Plural(d.Born, "adoption", "adoptions")}{TextChart.Trend(d.Born, p?.Born)}" +
            $" · **{deaths}** {Plural(deaths, "décès", "décès")}" +
            (deaths > 0 ? $" ({string.Join(", ", d.Deaths.Select(x => $"{DeathLabel(x.Cause)} : {x.Count}"))})" : "") +
            (d.AverageLifespan is { } life ? $"\nDurée de vie moyenne des disparus : **{Math.Max(1, (int)Math.Round(life.TotalDays))} jours**" : ""));

        embed.AddField("Les vivants, maintenant",
            $"Malades : **{d.Sick}** · très faim : **{d.Starving}** · faim : **{d.Hungry}**\n" +
            $"Sales : **{d.Dirty}** · gelés : **{d.Frozen}**\n" +
            (d.Stages.Count == 0 ? "—" : string.Join(" · ", d.Stages.Select(s => $"{StageLabel(s.Stage)} : {s.Count}"))));

        embed.AddField("Événements", d.EventsResolved == 0
            ? "Aucun événement résolu."
            : Cap($"Résolus : **{d.EventsResolved}**{TextChart.Trend(d.EventsResolved, p?.EventsResolved)}" +
                  $" · choisis seuls : **{d.DecidedAlone}** ({TextChart.Percent(d.DecidedAlone, d.EventsResolved)})\n" +
                  (d.Challenges > 0 ? $"Défis réussis : **{d.ChallengesWon}/{d.Challenges}**\n" : "") +
                  string.Join('\n', d.TopEvents.Select(e => $"• {e.Title} ×{e.Count}"))));

        embed.AddField("Liens et progrès",
            (d.Bonds.Count == 0 ? "Aucun lien noué." : string.Join(" · ", d.Bonds.Select(b => $"{BondLabel(b.Bond)} : {b.Count}"))) +
            $"\nBadges gagnés : **{d.Badges}** · traits acquis : **{d.Traits}**");

        if (names && d.Watch.Count > 0)
            embed.AddField("À surveiller", Cap(string.Join('\n', d.Watch.Select(x =>
                $"<@{x.OwnerId}> — {Format.Sanitize(x.Name)}{(x.Sick ? " · malade" : "")}{(x.Starving ? " · très faim" : "")}"))));

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
        var embed = Start("Les émotes", w);
        var total = d.Own + d.Foreign + d.Unicode;
        embed.WithDescription(total == 0
            ? "Aucune émote utilisée sur cette période."
            : $"**{LevelCardUi.Xp(total)}** émotes{TextChart.Trend(total, p is null ? null : p.Own + p.Foreign + p.Unicode)}" +
              $" · du serveur : **{TextChart.Percent(d.Own, total)}** · d'ailleurs : **{TextChart.Percent(d.Foreign, total)}**" +
              $" · emoji : **{TextChart.Percent(d.Unicode, total)}**");

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
            embed.AddField($"Jamais utilisées ({d.UnusedOwn.Count})", Fit(d.UnusedOwn, " "));
        if (d.TopForeign.Count > 0)
            embed.AddField("Venues d'autres serveurs", Fit(d.TopForeign.Select(e => $"{e.Markup} ×{LevelCardUi.Xp(e.Count)}").ToList(), " · "));

        return embed.WithFooter(Footer(w, "Écrites et en réaction.")).Build();
    }

    // ---- /report sleeping ---------------------------------------------------------------------

    public static Embed BuildSleeping(SleepingReportData d)
    {
        var embed = new EmbedBuilder()
            .WithTitle($"💤 Les endormis — rien depuis {d.Days} jours")
            .WithColor(Color.Purple)
            .WithDescription(d.Sleepers.Count == 0
                ? "Tout le monde a donné signe de vie. Pour l'instant."
                : $"**{d.Sleepers.Count}** sur **{d.Members}** {Plural(d.Members, "membre", "membres")}" +
                  $" ({TextChart.Percent(d.Sleepers.Count, d.Members)}) sans XP, réaction ni vocal depuis {d.Days} jours.");

        var lines = d.Sleepers.Select(s => $"<@{s.UserId}> — " + (s.LastDay is { } day
            ? $"dernier signe : {AppTime.FromDayKey(day):dd/MM/yyyy}"
            : $"jamais (depuis le {AppTime.FromDayKey(SleepingReport.ReliableSince):dd/MM/yyyy})")).ToList();
        var left = AddLineFields(embed, "Endormis", lines, maxFields: 2);

        var footer = $"Arrivés depuis moins de {d.Days} jours : pas comptés. L'activité dans les salons exclus ne compte pas : " +
                     "qui ne parle que là semble endormi.";
        if (left > 0) footer = $"… et {left} autres. " + footer;
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

    private static string Footer(ReportWindow w, string? note = null)
    {
        var trend = w.IsAllTime ? null
            : w.Period is null ? "Tendances : comparées au mois d'avant."
            : "Tendances : comparées à la période d'avant, de même durée.";
        return string.Join(" ", new[] { note, trend }.Where(s => s is not null));
    }

    private static void AddPeople(EmbedBuilder embed, string title, IReadOnlyList<PersonTally> people, Func<PersonTally, string> detail) =>
        AddPeople(embed, title, people, detail, t => t.UserId);

    private static void AddPeople<T>(EmbedBuilder embed, string title, IReadOnlyList<T> people, Func<T, string> detail, Func<T, ulong> id)
    {
        if (people.Count == 0) return;
        embed.AddField(title, Cap(string.Join('\n', people.Select(x => $"<@{id(x)}> — {detail(x)}"))), inline: true);
    }

    private static string Category(SessionCategory category) => category switch
    {
        SessionCategory.Game => "🎮 Jeu",
        SessionCategory.Activity => "🧑‍🤝‍🧑 Activité",
        SessionCategory.Movie => "🎬 Film",
        _ => "✨ Autre",
    };

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
        if (shown < items.Count) sb.Append($" … et {items.Count - shown} autres");
        return sb.Length == 0 ? "—" : sb.ToString();
    }
}
