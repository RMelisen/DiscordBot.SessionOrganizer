using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /admin stats' embed — static and Context-free, so its size is checkable without a gateway.
// Mentions inside an embed render as names without pinging.
public static class AdminCards
{
    public static Embed BuildStats(EconomyStats s)
    {
        var richest = s.Richest.Count == 0
            ? "Personne n'a encore de cailloux."
            : string.Join("\n", s.Richest.Select((r, i) => $"{i + 1}. <@{r.UserId}> — {PebbleEconomy.Cailloux(r.Balance)}"));
        var species = s.LivingBySpecies.Count == 0
            ? "Aucun Plynling vivant."
            : string.Join(" · ", s.LivingBySpecies.Select(x => $"{PlynlingCatalog.Info(x.Species).Name} {x.Count}"));
        var sets = string.Join("\n", s.Completions.Select(c => $"{c.Set.Emoji} {c.Set.Name} : {c.Completions}"));
        var popular = s.PopularCosmetics.Count == 0
            ? "Aucun pour l'instant."
            : string.Join("\n", s.PopularCosmetics.Select(p => $"{p.Cosmetic.Emoji} {CosmeticCatalog.Label(p.Cosmetic)} — {p.Holders} {(p.Holders > 1 ? "personnes" : "personne")}"));

        return new EmbedBuilder()
            .WithTitle("Statistiques du serveur")
            .WithColor(Color.Purple)
            .AddField("Cailloux",
                $"**{PebbleEconomy.Cailloux(s.TotalCailloux)}** en circulation · {s.Wallets} portefeuilles · " +
                $"{PebbleEconomy.Cailloux(s.AverageCailloux)} en moyenne\n{richest}")
            .AddField("Plynlings",
                $"**{s.Alive}** vivants (dont {s.Frozen} gelés) · **{s.Graves}** au cimetière\n{species}")
            .AddField("Collections complétées", $"{sets}\n**{s.Discoveries}** objets de collection découverts au total", inline: true)
            .AddField("Cosmétiques", $"**{s.CosmeticsHeld}** possédés · **{s.CosmeticsWorn}** portés en ce moment\n{popular}", inline: true)
            .WithFooter("Un instantané : les dépenses au fil du temps ne sont pas enregistrées.")
            .Build();
    }

    // ---- /admin dashboard -------------------------------------------------------------------------

    private static readonly (string Metric, string Label)[] EarnLabels =
    {
        (EconomyLog.EarnWork, "/work"), (EconomyLog.EarnPassive, "discussion"), (EconomyLog.EarnGame, "jeux"),
        (EconomyLog.EarnGift, "cadeaux"), (EconomyLog.EarnBadge, "badges"), (EconomyLog.EarnCollection, "collections"),
        (EconomyLog.EarnSale, "ventes"), (EconomyLog.EarnAdmin, "staff"), (EconomyLog.EarnEvent, "événements"),
    };

    private static readonly (string Metric, string Label)[] SpendLabels =
    {
        (EconomyLog.SpendShop, "boutique"), (EconomyLog.SpendMeal, "repas"), (EconomyLog.SpendMealOther, "repas offerts"),
        (EconomyLog.SpendCosmetic, "cosmétiques"), (EconomyLog.SpendCraft, "fabrication"), (EconomyLog.SpendAdmin, "staff"),
        (EconomyLog.SpendMedicine, "médicaments"),
    };

    private static readonly (string Metric, string Label)[] ActivityLabels =
    {
        (EconomyLog.ActMeal, "Repas"), (EconomyLog.ActPet, "Caresses"), (EconomyLog.ActGame, "Parties"),
        (EconomyLog.ActVisit, "Visites"), (EconomyLog.ActForage, "Balades"), (EconomyLog.ActTrade, "Échanges"),
        (EconomyLog.ActGive, "Objets offerts"), (EconomyLog.ActBath, "Bains"), (EconomyLog.ActMedicine, "Soins"),
    };

    private static readonly (string Metric, string Label)[] FindingLabels =
    {
        (EconomyLog.ItemFound, "Objets trouvés"), (EconomyLog.ItemSet, "Collections complétées"),
        (EconomyLog.CosBought, "Cosmétiques achetés"), (EconomyLog.CosCrafted, "Cosmétiques fabriqués"),
    };

    private const string Bars = "▁▂▃▄▅▆▇█";
    public const int SparklineMax = 30;

    /// <summary>
    /// One character per day, scaled to the series' own maximum; past <see cref="SparklineMax"/> days
    /// the days are summed into that many buckets, so « Tout » stays one line.
    /// </summary>
    public static string Sparkline(IReadOnlyList<long> values)
    {
        var n = values.Count;
        var buckets = Math.Min(SparklineMax, n);
        var sums = new long[buckets];
        for (var b = 0; b < buckets; b++)
            for (var i = b * n / buckets; i < (b + 1) * n / buckets; i++)
                sums[b] += values[i];
        var max = sums.Length == 0 ? 0 : sums.Max();
        return new string(sums.Select(v => max <= 0 ? Bars[0]
            : Bars[(int)Math.Round(v * 7.0 / max, MidpointRounding.ToZero)]).ToArray());
    }

    /// <summary>The trend against the previous window: « +12 % », « −8 % », « = », « nouveau », or nothing.</summary>
    public static string Delta(long current, long? previous)
    {
        if (previous is not { } p) return "";
        if (p == 0) return current > 0 ? "nouveau" : "";
        var pct = (long)Math.Round((current - p) * 100.0 / p);
        return pct == 0 ? "=" : pct > 0 ? $"+{pct} %" : $"−{-pct} %";
    }

    public static (Embed Embed, MessageComponent Components) BuildDashboard(DashboardData d)
    {
        long Sum(IEnumerable<(string Metric, string Label)> labels) => labels.Sum(l => d.Total(l.Metric));
        long? PrevSum(IEnumerable<(string Metric, string Label)> labels) =>
            d.Window == DashboardWindow.All ? null : labels.Sum(l => d.PreviousTotal(l.Metric) ?? 0);
        long[] Combined(IEnumerable<(string Metric, string Label)> labels)
        {
            var total = new long[d.Days.Count];
            foreach (var (metric, _) in labels)
            {
                var series = d.Series(metric);
                for (var i = 0; i < total.Length; i++) total[i] += series[i];
            }
            return total;
        }
        string Trend(long current, long? previous) => Delta(current, previous) is { Length: > 0 } t ? $" ({t})" : "";
        string Shares(IEnumerable<(string Metric, string Label)> labels, long total) => total <= 0 ? "—"
            : string.Join(" · ", labels.Select(l => (l.Label, V: d.Total(l.Metric))).Where(x => x.V > 0).OrderByDescending(x => x.V)
                .Select(x => $"{x.Label} {Math.Round(x.V * 100.0 / total)} %"));
        string Line((string Metric, string Label) l) =>
            $"{l.Label} : **{LevelCardUi.Xp(d.Total(l.Metric))}**{Trend(d.Total(l.Metric), d.PreviousTotal(l.Metric))} `{Sparkline(d.Series(l.Metric))}`";

        var earned = Sum(EarnLabels);
        var spent = Sum(SpendLabels);
        var net = earned - spent;
        var economy =
            $"Gagnés : **{PebbleEconomy.Cailloux(earned)}**{Trend(earned, PrevSum(EarnLabels))} `{Sparkline(Combined(EarnLabels))}`\n" +
            $"Dépensés : **{PebbleEconomy.Cailloux(spent)}**{Trend(spent, PrevSum(SpendLabels))} `{Sparkline(Combined(SpendLabels))}`\n" +
            $"Solde : **{(net >= 0 ? "+" : "−")}{PebbleEconomy.Cailloux(Math.Abs(net))}**\n" +
            $"-# Gagnés : {Shares(EarnLabels, earned)}\n" +
            $"-# Dépensés : {Shares(SpendLabels, spent)}";

        var title = d.Window switch
        {
            DashboardWindow.Week => "Tableau de bord — 7 derniers jours",
            DashboardWindow.Month => "Tableau de bord — 30 derniers jours",
            _ => "Tableau de bord — depuis le début",
        };
        var footer = d.FirstDay is { } first
            ? $"Données depuis le {EconomyDashboardService.FromKey(first):dd/MM/yyyy}" +
              (d.Window == DashboardWindow.All ? "." : " · tendance comparée à la période d'avant, de même durée.")
            : "Rien n'a encore été enregistré.";

        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithColor(Color.Purple)
            .AddField("Économie", economy)
            .AddField("Activité", string.Join("\n", ActivityLabels.Select(Line)))
            .AddField("Objets & cosmétiques", string.Join("\n", FindingLabels.Select(Line)))
            .WithFooter(footer)
            .Build();

        var row = new ComponentBuilder();
        foreach (var (w, label) in new[] { (DashboardWindow.Week, "7 jours"), (DashboardWindow.Month, "30 jours"), (DashboardWindow.All, "Tout") })
            row.WithButton(label, $"dash:win:{w}", w == d.Window ? ButtonStyle.Primary : ButtonStyle.Secondary, disabled: w == d.Window);
        return (embed, row.Build());
    }
}
