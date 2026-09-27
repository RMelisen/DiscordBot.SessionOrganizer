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
}
