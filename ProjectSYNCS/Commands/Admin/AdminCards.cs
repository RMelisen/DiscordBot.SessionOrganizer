using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
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
        (EconomyLog.EarnSale, "ventes"), (EconomyLog.EarnAdmin, "staff"), (EconomyLog.EarnEvent, "événements"), (EconomyLog.EarnQuiz, "quiz"),
        (EconomyLog.EarnArcade, "arcade"),
    };

    private static readonly (string Metric, string Label)[] SpendLabels =
    {
        (EconomyLog.SpendShop, "boutique"), (EconomyLog.SpendMeal, "repas"), (EconomyLog.SpendMealOther, "repas offerts"),
        (EconomyLog.SpendCosmetic, "cosmétiques"), (EconomyLog.SpendCraft, "fabrication"), (EconomyLog.SpendAdmin, "staff"),
        (EconomyLog.SpendMedicine, "médicaments"), (EconomyLog.SpendArcade, "arcade"),
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

    // Sparklines and trends are drawn by Helpers/Core/TextChart, shared with /report.
    private static string Sparkline(IReadOnlyList<long> values) => TextChart.Sparkline(values);

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
        string Trend(long current, long? previous) => TextChart.Trend(current, previous);
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

    // ---- /admin warn, warnings ----------------------------------------------------------------------

    /// <summary>
    /// How many warnings /admin warnings lists. Each can carry a full
    /// <see cref="InputCaps.WarnReason"/> quote plus its heading line (about 520 characters), and
    /// they share one embed description (4096).
    /// </summary>
    public const int MaxWarningsShown = 7;

    // "1 avertissement actif", "3 avertissements actifs".
    public static string ActiveWarnings(int n) =>
        n == 1 ? "**1** avertissement actif" : $"**{n}** avertissements actifs";

    /// <summary>
    /// The DM to the person warned: her line, the reason quoted, where they stand. No moderator
    /// name — it comes from the team.
    /// </summary>
    public static string BuildWarnDm(string line, string reason, int activeCount)
    {
        var standing = activeCount == 1
            ? "C'est ton premier avertissement actif."
            : $"Tu as maintenant {ActiveWarnings(activeCount)}.";
        return $"{line}\n\n**Raison :**\n{MessageFormat.Quote(reason, InputCaps.WarnReason)}\n\n" +
               $"{standing} Un avertissement expire au bout de {WarningRules.Lifetime.Days} jours. " +
               "Pour en parler, adresse-toi à l'équipe du serveur.";
    }

    /// <summary>The staff's confirmation after /admin warn, ephemeral.</summary>
    public static string BuildWarnConfirmation(MemberWarning warning, int activeCount, bool dmSent)
    {
        var dm = dmSent
            ? "Je lui ai transmis par DM."
            : "Ses messages privés sont fermés, donc pas de DM : l'avertissement compte quand même.";
        var text = $"⚠️ Avertissement **#{warning.Id}** enregistré pour <@{warning.UserId}> : " +
                   $"{ActiveWarnings(activeCount)}. {dm}";
        if (WarningRules.SuggestsMute(activeCount))
            text += "\nÇa commence à faire beaucoup. Une exclusion d'une heure ?";
        return text;
    }

    /// <summary>
    /// The one-hour mute offered from <see cref="WarningRules.MuteSuggestionThreshold"/> active
    /// warnings on; null below it. Handled by AdminComponentHandler.OnWarnMuteAsync.
    /// </summary>
    public static MessageComponent? BuildWarnMuteOffer(ulong userId, int activeCount) =>
        WarningRules.SuggestsMute(activeCount)
            ? new ComponentBuilder()
                .WithButton($"Exclure {WarningRules.SuggestedMuteMinutes / 60} h", $"warn:mute:{userId}", ButtonStyle.Danger, new Emoji("🔇"))
                .Build()
            : null;

    /// <summary>
    /// One person's warnings for staff, newest first: number, date, who gave it, the reason, and
    /// whether it still counts. Mentions in an embed render as names without pinging.
    /// </summary>
    public static Embed BuildWarnings(ulong userId, IReadOnlyList<MemberWarning> history, DateTimeOffset now)
    {
        var active = history.Count(w => WarningRules.IsActive(w, now));
        var lines = history.Take(MaxWarningsShown).Select(w =>
        {
            var status = w.RevokedAt is { } revoked
                ? $"révoqué le <t:{revoked.ToUnixTimeSeconds()}:d> par <@{w.RevokedById}>"
                : WarningRules.IsActive(w, now) ? "**actif**" : "expiré";
            return $"`#{w.Id}` · <t:{w.CreatedAt.ToUnixTimeSeconds()}:d> · par <@{w.ModeratorId}> · {status}\n" +
                   MessageFormat.Quote(w.Reason, InputCaps.WarnReason);
        });

        var builder = new EmbedBuilder()
            .WithTitle("Avertissements")
            .WithColor(active == 0 ? Color.LightGrey : Color.Orange)
            .WithDescription($"<@{userId}> · {ActiveWarnings(active)} sur {history.Count}\n\n" + string.Join("\n\n", lines));
        var footer = $"Un avertissement compte {WarningRules.Lifetime.Days} jours. /admin unwarn pour en révoquer un.";
        if (history.Count > MaxWarningsShown)
            footer = $"Et {history.Count - MaxWarningsShown} plus ancien(s) non affiché(s). " + footer;
        return builder.WithFooter(footer).Build();
    }
}
