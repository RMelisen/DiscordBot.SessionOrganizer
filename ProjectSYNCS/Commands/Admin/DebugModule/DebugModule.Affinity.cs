using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /debug affinity: the raw numbers behind /plynling relations — each pair's affinity, its hidden
// compatibility and where that compatibility draws it — only for the owner to see. Players see
// bonds, never points.
public partial class DebugModule
{
    [SlashCommand("affinity", "Les points d'affinité d'un Plynling avec chacun de ceux qu'il a rencontrés")]
    public async Task AffinityAsync([Summary("user", "À qui est le Plynling (par défaut : toi)")] IUser? user = null)
    {
        if (Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
            return;
        }
        if (Context.Guild is null)
        {
            await RespondAsync("Sur un serveur, pas en message privé.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);
        var p = await _plynlings.GetShownAsync(Context.Guild.Id, (user ?? Context.User).Id, DateTimeOffset.UtcNow);
        if (p is null)
        {
            await FollowupAsync("🔧 Pas de Plynling, vivant ou mort.", ephemeral: true);
            return;
        }
        var relations = await _plynlings.GetRelationsAsync(p.Id);
        await FollowupAsync(embed: BuildAffinityEmbed(p, relations), ephemeral: true, allowedMentions: AllowedMentions.None);
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
