using System.Globalization;
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
}
