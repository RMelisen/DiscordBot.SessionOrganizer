using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /debug affinity: the raw numbers behind /plynling relations — each pair's affinity, its hidden
// compatibility and where that compatibility draws it — only for the owner to see. Players see
// bonds, never points. `add` moves a pair's affinity to test bonds without waiting for visits.
// A group, not two top-level entries: /debug is at Discord's 25.
public partial class DebugModule
{
    [Group("affinity", "Les points d'affinité des Plynlings : les voir, les modifier (tests)")]
    public class AffinityModule : SyncsModuleBase
    {
        // A full swing of the −100…+100 gauge; more would only be clamped.
        private const int MaxShift = 200;

        private readonly PlynlingService _plynlings;

        public AffinityModule(PlynlingService plynlings)
        {
            _plynlings = plynlings;
        }

        [SlashCommand("show", "Les points d'affinité d'un Plynling avec chacun de ceux qu'il a rencontrés")]
        public async Task ShowAsync([Summary("user", "À qui est le Plynling (par défaut : toi)")] IUser? user = null)
        {
            if (!await GateAsync()) return;

            await DeferAsync(ephemeral: true);
            var p = await _plynlings.GetShownAsync(Context.Guild!.Id, (user ?? Context.User).Id, DateTimeOffset.UtcNow);
            if (p is null)
            {
                await FollowupAsync("🔧 Pas de Plynling, vivant ou mort.", ephemeral: true);
                return;
            }
            var relations = await _plynlings.GetRelationsAsync(p.Id);
            await FollowupAsync(embed: BuildAffinityEmbed(p, relations), ephemeral: true, allowedMentions: AllowedMentions.None);
        }

        [SlashCommand("add", "Ajouter (ou retirer, en négatif) des points d'affinité entre deux Plynlings vivants (tests)")]
        public async Task AddAsync(
            [Summary("other", "Le propriétaire de l'autre Plynling")] IUser other,
            [Summary("amount", "Points à ajouter, négatif pour en retirer (l'affinité reste entre −100 et +100)")]
            [MinValue(-MaxShift), MaxValue(MaxShift)] int amount,
            [Summary("user", "À qui est le premier Plynling (par défaut : toi)")] IUser? user = null)
        {
            if (!await GateAsync()) return;
            if (amount == 0)
            {
                await RespondAsync("🔧 Zéro point, ça ne change rien.", ephemeral: true);
                return;
            }

            await DeferAsync(ephemeral: true);
            var (outcome, r) = await _plynlings.DebugShiftAffinityAsync(
                Context.Guild!.Id, (user ?? Context.User).Id, other.Id, amount, DateTimeOffset.UtcNow);
            await FollowupAsync(outcome switch
                {
                    DebugAffinityOutcome.SameOwner => "🔧 Un Plynling n'a pas d'affinité avec lui-même : choisis deux propriétaires différents.",
                    DebugAffinityOutcome.NoFirst => "🔧 Pas de Plynling vivant pour le premier propriétaire.",
                    DebugAffinityOutcome.NoSecond => "🔧 Pas de Plynling vivant pour l'autre propriétaire.",
                    _ => ShiftLine(r!),
                },
                ephemeral: true, allowedMentions: AllowedMentions.None);
        }

        private async Task<bool> GateAsync()
        {
            if (Context.User.Id != AvailabilityService.OwnerId)
            {
                await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
                return false;
            }
            if (Context.Guild is null)
            {
                await RespondAsync("Sur un serveur, pas en message privé.", ephemeral: true);
                return false;
            }
            return true;
        }

        // « 🔧 Pipou ♀ et Gribouille ♂ : +12 → +42 ▰▰▰▰▰▰▰▱▱▱ · 🙂 connaissance → 🤝 ami (journalisé, sans score ni badge) »
        public static string ShiftLine(DebugAffinityResult r)
        {
            var bond = r.BondAfter == r.BondBefore
                ? $"{PlynlingBonds.Emoji(r.BondAfter)} {PlynlingBonds.Role(r.BondAfter, r.B.Gender)}, inchangé"
                : $"{PlynlingBonds.Emoji(r.BondBefore)} {PlynlingBonds.Role(r.BondBefore, r.B.Gender)} → " +
                  $"{PlynlingBonds.Emoji(r.BondAfter)} {PlynlingBonds.Role(r.BondAfter, r.B.Gender)} (journalisé, sans score ni badge)";
            return $"🔧 **{PlynlingCardUi.SafeName(r.A.Name)}** {r.A.Gender.Symbol()} et " +
                   $"**{PlynlingCardUi.SafeName(r.B.Name)}** {r.B.Gender.Symbol()} : " +
                   $"`{Signed(r.Before)}` → `{Signed(r.After)}` {Gauge(r.After)} · {bond}";
        }
    }

    // Leaves room under the embed description's 4096.
    private const int AffinityTextBudget = 3900;

    /// <summary>
    /// Every pair, highest affinity first: bond, affinity on a −100…+100 gauge, the hidden
    /// compatibility and the affinity it draws the pair toward (a couple feels no pull).
    /// Lines that would overflow the description are counted instead.
    /// </summary>
    public static Embed BuildAffinityEmbed(Plynling p, IReadOnlyList<(PlynlingRelation Relation, Plynling Other)> relations)
    {
        var ordered = relations
            .OrderByDescending(r => r.Relation.Affinity)
            .ThenBy(r => r.Other.Id)
            .ToList();

        var text = new System.Text.StringBuilder();
        var shown = 0;
        foreach (var (relation, other) in ordered)
        {
            var compat = PlynlingBonds.Compatibility(p.Id, other.Id);
            var target = relation.Bond == PlynlingBond.Lovers
                ? "couple"
                : $"≈ {Signed(Math.Clamp(17 + 4 * compat, -100, 100))}";
            var line = $"{PlynlingBonds.Emoji(relation.Bond)} **{PlynlingCardUi.SafeName(other.Name)}** {other.Gender.Symbol()}" +
                       $"{(other.DiedAt is null ? "" : " 🪦")} (à <@{other.OwnerId}>)\n" +
                       $"　`{Signed(relation.Affinity),4}` {Gauge(relation.Affinity)} · compat `{Signed(compat)}` → {target} — " +
                       $"{PlynlingBonds.Role(relation.Bond, other.Gender)}\n";
            if (text.Length + line.Length > AffinityTextBudget) break;
            text.Append(line);
            shown++;
        }
        if (shown < ordered.Count) text.Append($"*… et {ordered.Count - shown} autres*");

        var name = PlynlingCardUi.SafeName(p.Name);
        return new EmbedBuilder()
            .WithTitle($"🔧 Affinités de {name}{(p.DiedAt is null ? "" : " 🪦")}")
            .WithColor(new Color(PlynlingCatalog.Info(p.Species).Accent))
            .WithDescription(ordered.Count == 0 ? $"**{name}** n'a encore rencontré personne." : text.ToString())
            .WithFooter($"Affinité −100…+100 : ennemis ≤ {PlynlingBonds.EnemiesAtMost} < rivaux ≤ {PlynlingBonds.RivalsAtMost} " +
                        $"< connaissances < {PlynlingBonds.FriendsFrom} ≤ amis < {PlynlingBonds.BestFriendsFrom} ≤ meilleurs amis " +
                        $"(marge ±{PlynlingBonds.BondMargin}) · déclaration dès {PlynlingBonds.ConfessFrom}, " +
                        $"rupture sous {PlynlingBonds.BreakUpBelow} · compat −20…+20, tire vers ≈ 17 + 4 × compat")
            .Build();
    }

    private static string Signed(int n) => n > 0 ? $"+{n}" : n.ToString();

    // Ten cells over −100…+100, so the middle (0) is half full.
    private static string Gauge(int affinity)
    {
        var filled = (int)Math.Round((Math.Clamp(affinity, -100, 100) + 100) / 20.0);
        return new string('▰', filled) + new string('▱', 10 - filled);
    }
}
