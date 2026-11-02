using Discord;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Commands;

// /report incidents: what went wrong, for Papa's digest and on demand. Names nobody, so names changes nothing.
public static partial class ReportCards
{
    public static Embed BuildIncidents(Compared<IncidentReportData> c, ReportWindow w, bool names)
    {
        var d = c.Current;
        var p = c.Previous;
        var pace = w.Pace(c.FirstDay);
        var embed = Start("🔧 Les incidents", w);
        if (c.FirstDay is null)
            return embed.WithDescription("Rien de compté pour l'instant (comptage depuis la 7.56.0).").WithFooter(Footer(w)).Build();

        embed.WithDescription(
            $"En échec : **{LevelCardUi.Xp(d.Failed)}**{TextChart.Trend(d.Failed, p?.Failed, pace)}" +
            $" · trop lentes : **{LevelCardUi.Xp(d.Late)}**{TextChart.Trend(d.Late, p?.Late, pace)}" +
            $" · erreurs au journal : **{LevelCardUi.Xp(d.LogErrors)}**{TextChart.Trend(d.LogErrors, p?.LogErrors, pace)}" +
            PartialNote(w, c.FirstDay));

        embed.AddField("Commandes, boutons et formulaires", Cap(
            $"Commandes : **{LevelCardUi.Xp(d.Slash)}** · boutons et menus : **{LevelCardUi.Xp(d.Buttons)}** · formulaires : **{LevelCardUi.Xp(d.Modals)}**" +
            Lines(d.TopFailed)));
        embed.AddField("Réponses trop lentes", Cap(
            $"Sans réponse après 3 s : **{LevelCardUi.Xp(d.Late)}**" + Lines(d.TopLate)));
        embed.AddField("Le journal", Cap(
            $"Erreurs : **{LevelCardUi.Xp(d.LogErrors)}** · avertissements : **{LevelCardUi.Xp(d.LogWarnings)}**" + Lines(d.TopLogs)));
        embed.AddField("Base de données", $"Base occupée (SQLITE_BUSY) : **{LevelCardUi.Xp(d.SqliteBusy)}**");

        var restarts = d.Restarts.Sum(r => r.Count);
        embed.AddField("Connexion et redémarrages",
            $"Connexions perdues : **{LevelCardUi.Xp(d.Disconnects)}**{TextChart.Trend(d.Disconnects, p?.Disconnects, pace)}\n" +
            $"Redémarrages : **{LevelCardUi.Xp(restarts)}**" +
            (restarts > 0 ? $" ({string.Join(", ", d.Restarts.Select(r => $"{StopLabel(r.Stop)} : {r.Count}"))})" : ""));

        return embed.WithFooter(Footer(w, "Le journal compte des lignes : une panne peut en écrire plusieurs. Comptage depuis la 7.56.0.")).Build();
    }

    private static string Lines(IReadOnlyList<IncidentLine> lines) =>
        string.Concat(lines.Select(l => $"\n`{l.Key}` ×{LevelCardUi.Xp(l.Count)}"));

    // A restart under how the run before it stopped (StopKind's name, stored).
    private static string StopLabel(string stop) => stop switch
    {
        nameof(StopKind.Clean) => "propres",
        nameof(StopKind.PowerCut) => "coupures de courant",
        nameof(StopKind.Crash) => "plantages",
        _ => "inconnus",
    };
}
