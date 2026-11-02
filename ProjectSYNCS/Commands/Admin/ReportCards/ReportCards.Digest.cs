using System.Globalization;
using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// The monthly digest's first message: « Le mois en bref », for Papa and for Tata (docs/agents/admin.md).
public static partial class ReportCards
{
    /// <summary>
    /// Papa's first digest message: her commands, the Pi, what she couldn't answer, the quiz and her hours
    /// in Minecraft, in a few figures, then « À regarder » (DigestBrief.Owner) or her quiet line when nothing is.
    /// </summary>
    public static Embed BuildOwnerBrief(Compared<CommandReportData> commands, Compared<SyncsReportData> syncs,
        Compared<QuizReportData> quiz, Compared<MinecraftReportData> minecraft, IReadOnlyList<DigestAlert> alerts, ReportWindow w,
        string quietLine, Compared<IncidentReportData>? incidents = null)
    {
        var embed = Start("📬 Le mois en bref", w);

        var c = commands.Current;
        embed.AddField("Commandes", c.Uses == 0
            ? "Aucune commande comptée."
            : $"**{LevelCardUi.Xp(c.Uses)}** {Plural(c.Uses, "utilisation", "utilisations")}{TextChart.Trend(c.Uses, commands.Previous?.Uses, w.Pace(commands.FirstDay))}{DaysCounted(w, commands.FirstDay)}" +
              (c.People.Count > 0 ? $" · par **{c.People.Count}** {Plural(c.People.Count, "personne", "personnes")}" : "") +
              $" · en erreur : **{c.Failures}** ({TextChart.Percent(c.Failures, c.Uses)})\n" +
              $"Pas utilisées : **{c.NeverUsed.Count}** · côté staff : **{c.NeverUsedStaff.Count}**");

        if (incidents is not null)
        {
            var i = incidents.Current;
            embed.AddField("Les incidents", incidents.FirstDay is null
                ? "Rien de compté pour l'instant."
                : $"En échec : **{LevelCardUi.Xp(i.Failed)}** · trop lentes : **{LevelCardUi.Xp(i.Late)}** · erreurs au journal : **{LevelCardUi.Xp(i.LogErrors)}**" +
                  (i.SqliteBusy > 0 ? $" · base occupée : **{LevelCardUi.Xp(i.SqliteBusy)}**" : "") + DaysCounted(w, incidents.FirstDay));
        }

        var s = syncs.Current;
        var outages = s.Outages.Sum(o => o.Count);
        embed.AddField("Le Pi", s.DaysMeasured == 0
            ? "Aucune mesure ce mois-ci."
            : (s.Uptime is { } up ? $"Disponibilité **{(up * 100).ToString("0.0", Fr)} %**{UptimeSpan(s, w)}" : "Disponibilité —") +
              $" · {Plural(outages, "panne", "pannes")} : **{outages}**" +
              (s.MaxMilli is { } max ? $"\nTempérature max : **{Temp(max)}** · {s.HotMinutes} min à {Temp(PiHealth.ThrottleMilli)} ou plus" : "") +
              (s.Body is { Database: { } database } body ? $"\nBase : **{HealthCards.Bytes(database)}**" +
                  (body.DiskFree is { } free && body.DiskTotal is { } total ? $" · disque libre : **{TextChart.Percent(free, total)}**" : "") : ""));

        var p = syncs.Previous;
        embed.AddField("Messages sans réponse précise", s.Answers == 0
            ? "Personne ne m'a parlé."
            : $"**{TextChart.Percent(s.Unrecognised, s.Answers)}** de mes réponses ({LevelCardUi.Xp(s.Unrecognised)} sur {LevelCardUi.Xp(s.Answers)})" +
              (p is { Answers: > 0 } ? $" · avant : {TextChart.Percent(p.Unrecognised, p.Answers)}" : "") + DaysCounted(w, s.AnswersSince));

        var q = quiz.Current;
        embed.AddField("Le quiz", q.Posted == 0
            ? "Aucune question posée."
            : $"**{q.Posted}** {Plural(q.Posted, "question", "questions")}{TextChart.Trend(q.Posted, quiz.Previous?.Posted, w.Pace(quiz.FirstDay))}{DaysCounted(w, quiz.FirstDay)}" +
              $" · trouvées : **{TextChart.Percent(q.Found, q.Posted)}** · sans réponse : **{q.Unanswered}**" +
              (q.ByDifficulty.Count > 0
                  ? "\nTrouvées par difficulté : " + string.Join(" · ", q.ByDifficulty.Select(l => $"{l.Label} {TextChart.Percent(l.Found, l.Posted)}"))
                  : ""));

        var g = minecraft.Current;
        embed.AddField("Minecraft", !g.Played
            ? "Pas joué ce mois-ci."
            : $"**{Duration(g.Minutes)}** en jeu{TextChart.Trend(g.Minutes, minecraft.Previous?.Minutes, w.Pace(minecraft.FirstDay))}" +
              $" · {g.DaysPlayed} {Plural(g.DaysPlayed, "jour", "jours")}{DaysCounted(w, minecraft.FirstDay)}");

        AddAlerts(embed, alerts, quietLine);
        return embed.WithFooter(Footer(w, "Le détail suit.")).Build();
    }

    /// <summary>
    /// Tata's first digest message: the members, the sessions, when the server talks, the most active
    /// and the forgotten emotes, then « À regarder » (DigestBrief.Tata) or the quiet line.
    /// </summary>
    public static Embed BuildTataBrief(Compared<CommunityReportData> community, Compared<SessionReportData> sessions,
        ActivityReportData activity, SleepingReportData sleeping, int unusedEmotes, IReadOnlyList<DigestAlert> alerts,
        ReportWindow w, string quietLine, Func<ulong, string?>? nameOf = null)
    {
        var embed = Start("📬 Le mois en bref", w);

        var m = community.Current;
        var never = sleeping.Sleepers.Count(s => s.LastDay is null);
        embed.AddField("Les membres",
            $"**{m.ActiveMembers}** {Plural(m.ActiveMembers, "membre actif", "membres actifs")}{TextChart.Trend(m.ActiveMembers, community.Previous?.ActiveMembers)}" +
            $" · arrivées : **{m.Joins}** · départs : **{m.Leaves}**\n" +
            $"Sans signe de vie depuis {sleeping.Days} jours : **{sleeping.Sleepers.Count}**" +
            (never > 0 ? $" (dont {never} {Plural(never, "jamais actif", "jamais actifs")})" : ""));

        var d = sessions.Current;
        var best = DigestBrief.BestSessionDay(d);
        embed.AddField("Les sessions", d.Held == 0
            ? "Aucune session ce mois-ci" + (sessions.Previous is { Held: > 0 } before ? $", contre **{before.Held}** le mois d'avant." : ".")
            : $"**{d.Held}** {Plural(d.Held, "session tenue", "sessions tenues")}{TextChart.Trend(d.Held, sessions.Previous?.Held, w.Pace(sessions.FirstDay))}" +
              (d.Expected > 0 ? $" · présence : **{TextChart.Percent(d.Came, d.Expected)}**" : "") +
              (best is { } b ? $"\nLe jour qui marche le mieux : **{Weekday(b.Slot)}** ({TextChart.Percent(b.Came, b.Expected)} présents)" : ""));

        var when = new List<string>();
        if (activity.BusiestWeekday is { } day) when.Add($"Jour le plus bavard : **{Weekday((int)day)}**");
        if (activity.BusiestHours.Count > 0) when.Add($"heures de pointe : {string.Join(" · ", activity.BusiestHours.Select(h => $"{h} h"))}");
        embed.AddField("Le serveur",
            (when.Count > 0 ? Capitalise(string.Join(" · ", when)) + "\n" : "") +
            $"Salons silencieux : **{activity.SilentText.Count}** · émotes du serveur pas utilisées ce mois-ci : **{unusedEmotes}**");

        var top = m.XpByUser.Where(x => x.Value > 0).OrderByDescending(x => x.Value).Take(3).ToList();
        if (top.Count > 0)
        {
            var lines = new List<string> { string.Join(" · ", top.Select(x => Who(x.Key, nameOf))) };
            if (CommunityReport.BusiestInChat(m, null) is [var chat, ..]) lines.Add($"💬 À l'écrit : {Who(chat.UserId, nameOf)}");
            if (CommunityReport.BusiestInVoice(m, null) is [var voice, ..]) lines.Add($"🎙️ En vocal : {Who(voice.UserId, nameOf)}");
            embed.AddField("Les plus actifs", Cap(string.Join('\n', lines)));
        }

        AddAlerts(embed, alerts, quietLine);
        return embed.WithFooter(Footer(w, "Le détail suit.")).Build();
    }

    private static void AddAlerts(EmbedBuilder embed, IReadOnlyList<DigestAlert> alerts, string quietLine) =>
        embed.AddField("À regarder", alerts.Count == 0 ? quietLine : Cap(string.Join('\n', alerts.Select(AlertLine))));

    public static string AlertLine(DigestAlert a) => a.Kind switch
    {
        DigestAlertKind.FailingCommand =>
            $"⚠️ `/{a.Subject}` a échoué **{a.Value}** fois sur {a.Of} ({TextChart.Percent(a.Value, a.Of)})",
        DigestAlertKind.PowerCuts => $"🔌 **{a.Value}** {Plural(a.Value, "coupure de courant", "coupures de courant")}",
        DigestAlertKind.Crashes => $"💥 **{a.Value}** {Plural(a.Value, "plantage", "plantages")}",
        DigestAlertKind.HeatRecord => $"🔥 Nouveau record de température : **{Temp((int)a.Value)}**",
        DigestAlertKind.UnansweredRising => $"💬 Plus de messages sans réponse précise : **{a.Value} %**, contre {a.Of} % le mois d'avant",
        DigestAlertKind.QuizTooEasy => $"❓ Les questions difficiles ont été trouvées à **{a.Value} %** : trop faciles ?",
        DigestAlertKind.QuizTooHard => $"❓ Les questions faciles n'ont été trouvées qu'à **{a.Value} %** : trop dures ?",
        DigestAlertKind.ActiveDrop => $"📉 **{a.Value}** membres actifs, contre {a.Of} le mois d'avant",
        DigestAlertKind.MoreLeaves => $"🚪 Plus de départs (**{a.Value}**) que d'arrivées (**{a.Of}**)",
        DigestAlertKind.InteractionFailures => $"🧩 **{a.Value}** boutons ou formulaires en échec",
        DigestAlertKind.LateAnswers => $"🐌 **{a.Value}** interactions sans réponse après 3 s",
        DigestAlertKind.LogErrors => $"📓 **{a.Value}** erreurs au journal",
        DigestAlertKind.SqliteBusy => $"🗄️ Base occupée **{a.Value}** fois (SQLITE_BUSY)",
        DigestAlertKind.DiskLow => $"💾 Disque libre : **{a.Value} %**" + (a.Of > 0 ? $", plein dans {a.Of} jours à ce rythme" : ""),
        DigestAlertKind.MemoryLow => $"🧠 Mémoire libre tombée à **{a.Value} %**",
        DigestAlertKind.UnderVoltage => $"🔋 **{a.Value} min** en sous-tension : l'alimentation ?",
        DigestAlertKind.VoiceWithoutSessions => $"🎧 **{a.Value} h** de vocal, mais aucune session tenue",
        _ => $"💤 **{a.Value}** membres actifs le mois d'avant, sans signe de vie depuis",
    };
}
