using System.Globalization;
using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Commands;

// /health — one Components V2 card: her line, then six sections. Static and Context-free, so its
// size is checkable without a gateway: 1 container + 1 title + 1 line + 6 × (separator + text)
// + 1 owner footer = 16 of the 40 components. Every section stays a few short lines: the whole
// card is far under the 4000 characters a V2 message may carry.
//
// Null is « illisible » throughout (off a Pi, a file missing), never a zero. A relative
// timestamp renders « il y a 3 jours »: never put « depuis » in front of one. Sent with
// AllowedMentions.None, like every V2 card.
public static class HealthCards
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");
    private const string Unreadable = "*illisible*";

    /// <param name="headline">Her line, already picked.</param>
    /// <param name="isOwner">Adds the state file's path, which means nothing to anyone else.</param>
    public static MessageComponent Build(HealthReport report, string headline, bool isOwner)
    {
        var container = new ContainerBuilder()
            .WithAccentColor(Color.Teal)
            .AddComponent(new TextDisplayBuilder("## Mon bilan de santé"))
            .AddComponent(new TextDisplayBuilder(headline));

        Section(container, "Température", Temperature(report.Temperature));
        Section(container, "Raspberry Pi", Pi(report.Pi));
        Section(container, "Processus", Process(report.Process));
        Section(container, "Connexion à Discord", Gateway(report.Gateway));
        Section(container, "Stockage", Storage(report.Storage));
        Section(container, "Pannes", Outages(report.Outages));

        if (isOwner)
            container.AddComponent(new TextDisplayBuilder($"-# `{report.StatePath}`"));

        return new ComponentBuilderV2().AddComponent(container).Build();
    }

    private static void Section(ContainerBuilder container, string title, IEnumerable<string> lines) =>
        container
            .AddComponent(new SeparatorBuilder())
            .AddComponent(new TextDisplayBuilder($"**{title}**\n" + string.Join('\n', lines)));

    private static IEnumerable<string> Temperature(TemperatureReading t)
    {
        yield return $"Maintenant : {(t.CurrentMilli is { } c ? PiHealth.Celsius(c) : Unreadable)}";

        if (t.Today is { } d)
            yield return $"Aujourd'hui : min {PiHealth.Celsius(d.MinMilli)}, max {PiHealth.Celsius(d.MaxMilli)} " +
                         $"à <t:{d.MaxAt.ToUnixTimeSeconds()}:t>, moyenne {PiHealth.Celsius((int)(d.SumMilli / Math.Max(1, d.Samples)))}" +
                         (d.HotMinutes > 0 ? $", {d.HotMinutes} min au-dessus de {PiHealth.Celsius(PiHealth.ThrottleMilli)}" : "");
        else
            yield return "Aujourd'hui : aucune mesure";

        if (t.WeekHottest is { } w)
            yield return $"7 derniers jours : max {PiHealth.Celsius(w.MaxMilli)} ({Day(w.Day)})";
        if (t.Record is { } r)
            yield return $"Record : {PiHealth.Celsius(r.MaxMilli)} ({Day(r.Day)}), sur {Plural(t.DaysMeasured, "jour")} de mesures";
    }

    private static IEnumerable<string> Pi(PiReading p)
    {
        yield return p.BootedAt is { } b
            ? $"Allumé <t:{b.ToUnixTimeSeconds()}:R>"
            : $"Allumé : {Unreadable}";
        yield return p.Load is { } l
            ? $"Charge : {Decimal(l.One, "0.00")} · {Decimal(l.Five, "0.00")} · {Decimal(l.Fifteen, "0.00")} (1, 5, 15 min)"
            : $"Charge : {Unreadable}";
        yield return p.Memory is { } m
            ? $"Mémoire disponible : {Bytes(m.Available)} sur {Bytes(m.Total)}"
            : $"Mémoire disponible : {Unreadable}";
        yield return $"Processeur : {(p.CpuMegahertz is { } mhz ? $"{mhz} MHz" : Unreadable)}";
        yield return $"Alimentation : {Power(p.Power)}";
    }

    // The firmware's flags in one phrase: what is wrong now first, then what went wrong since boot.
    private static string Power(PowerReading? power)
    {
        if (power is null) return Unreadable;

        var now = new List<string>();
        if (power.UnderVoltageNow) now.Add("tension trop basse");
        if (power.SlowedNow == true) now.Add("processeur bridé");
        var earlier = new List<string>();
        if (!power.UnderVoltageNow && power.UnderVoltageSinceBoot == true) earlier.Add("tension trop basse");
        if (power.SlowedNow != true && power.SlowedSinceBoot == true) earlier.Add("processeur bridé");

        if (now.Count == 0 && earlier.Count == 0) return "stable";
        var parts = new List<string>();
        if (now.Count > 0) parts.Add($"{string.Join(", ", now)} en ce moment");
        if (earlier.Count > 0) parts.Add($"{string.Join(", ", earlier)} depuis le démarrage");
        return string.Join(" ; ", parts);
    }

    private static IEnumerable<string> Process(ProcessReading p)
    {
        yield return $"Démarrée <t:{p.StartedAt.ToUnixTimeSeconds()}:R> · version {p.Version} · {p.Runtime}";
        yield return $"Mémoire : {Bytes(p.WorkingSetBytes)} (dont {Bytes(p.HeapBytes)} de tas) · {p.Threads} threads";
        yield return $"Avant ce démarrage : {PiHealth.StopLabel(p.LastStop)}" +
                     (PiHealth.IsAbrupt(p.LastStop) ? $", absente {PiHealth.Duration(p.Downtime)}" : "");
    }

    private static IEnumerable<string> Gateway(GatewayReading g)
    {
        yield return $"{(g.Connected ? "Connectée" : "Déconnectée")} · latence {g.LatencyMs} ms · " +
                     $"{Plural(g.Disconnects, "déconnexion")} depuis le démarrage";
        if (g.OutageSince is { } o)
            yield return $"Coupure en cours, commencée <t:{o.ToUnixTimeSeconds()}:R>";
        if (g.MembersCached is { } cached && g.MemberCount is { } count)
            yield return $"Membres en mémoire : {cached} sur {count}";
    }

    private static IEnumerable<string> Storage(StorageReading s)
    {
        yield return $"Base de données : {(s.DatabaseBytes is { } db ? Bytes(db) : Unreadable)}" +
                     (s.WalBytes is { } wal ? $" · journal {Bytes(wal)}" : "");
        yield return s.FreeBytes is { } free && s.TotalBytes is { } total
            ? $"Disque : {Bytes(free)} libres sur {Bytes(total)}"
            : $"Disque : {Unreadable}";
    }

    private static IEnumerable<string> Outages(OutageReading o)
    {
        if (o.Recent.Count == 0)
        {
            yield return "Aucune panne enregistrée.";
            yield break;
        }

        var tally = new[] { UptimeEventKind.PowerCut, UptimeEventKind.Crash, UptimeEventKind.NetworkOutage }
            .Where(o.MonthCounts.ContainsKey)
            .Select(k => $"{o.MonthCounts[k]} {KindLabel(k, o.MonthCounts[k])}")
            .ToList();
        yield return tally.Count == 0
            ? "30 derniers jours : aucune"
            : $"30 derniers jours : {string.Join(", ", tally)}, {PiHealth.Duration(o.MonthDown)} hors ligne";

        foreach (var e in o.Recent)
            yield return $"- {Capitalise(KindLabel(e.Kind, 1))}, <t:{e.StartedAt.ToUnixTimeSeconds()}:f>, " +
                         PiHealth.Duration(e.EndedAt - e.StartedAt);
    }

    private static string KindLabel(UptimeEventKind kind, int count) => (kind, count > 1) switch
    {
        (UptimeEventKind.PowerCut, false) => "coupure de courant",
        (UptimeEventKind.PowerCut, true) => "coupures de courant",
        (UptimeEventKind.Crash, false) => "plantage",
        (UptimeEventKind.Crash, true) => "plantages",
        (_, false) => "perte de réseau",
        (_, true) => "pertes de réseau",
    };

    private static string Day(int dayKey) => AppTime.FromDayKey(dayKey).ToString("d MMMM", Fr);

    private static string Plural(int n, string noun) => $"{n} {noun}{(n > 1 ? "s" : "")}";

    private static string Decimal(double value, string format) => value.ToString(format, Fr);

    private static string Capitalise(string text) => char.ToUpper(text[0], Fr) + text[1..];

    // French units: Mo up to a gigabyte, Go beyond, one decimal under ten.
    private static string Bytes(long bytes)
    {
        const double mega = 1024 * 1024, giga = mega * 1024;
        return bytes >= giga
            ? $"{Decimal(bytes / giga, bytes < 10 * giga ? "0.0" : "0")} Go"
            : $"{Decimal(bytes / mega, bytes < 10 * mega ? "0.0" : "0")} Mo";
    }
}
