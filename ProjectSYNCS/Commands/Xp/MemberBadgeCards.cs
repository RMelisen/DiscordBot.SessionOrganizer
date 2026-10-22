using Discord;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Commands;

public enum BadgeSection { Collections, Paliers, Moments, Secrets }

// The member badges as the /level card shows them: one icon line on the card, and the ephemeral
// « Succès » view behind its button. Static and Context-free so both can be measured without a
// gateway. The view is one page per section — all four together run past the 4000-character
// text cap of a Components V2 message, so the tabs are not a luxury.
public static class MemberBadgeCards
{
    public static string OpenId(ulong userId) => $"level:badges:{userId}";

    public static string TabId(ulong userId, BadgeSection section) => $"level:badgetab:{userId}:{section}";

    // ---- /level card ---------------------------------------------------------

    // Collection badges first (catalog order, unchanged rendering), then earned paliers at their
    // tier, moments and found secrets. Each completed set counts one succès, like each earned badge.
    // Null with nothing earned.
    public static string? CardLine(IReadOnlyCollection<Models.CollectionCompletion> completions, IReadOnlyList<BadgeStanding> standings)
    {
        var icons = CompletedSets(completions).Select(c => CollectionTiers.Badge(c.Set, c.Tier)).ToList();
        var earned = standings.Where(s => s.Earned).ToList();
        foreach (var kind in Enum.GetValues<MemberBadgeKind>())
            icons.AddRange(earned.Where(s => s.Info.Kind == kind).Select(MemberBadges.Icon));
        if (icons.Count == 0) return null;
        return $"**Succès** · {string.Join(" ", icons)}\n-# {icons.Count} succès";
    }

    // ---- « Succès » view -----------------------------------------------------

    public static MessageComponent BuildView(ulong userId, string displayName, BadgeSection section,
        IReadOnlyCollection<Models.CollectionCompletion> completions, IReadOnlyList<BadgeStanding> standings)
    {
        var body = section switch
        {
            BadgeSection.Collections => CollectionsText(completions),
            BadgeSection.Moments => MomentsText(standings),
            BadgeSection.Secrets => SecretsText(standings),
            _ => PaliersText(standings),
        };

        var tabs = new ActionRowBuilder();
        foreach (var tab in Enum.GetValues<BadgeSection>())
            tabs.WithButton(TabLabel(tab), TabId(userId, tab),
                tab == section ? ButtonStyle.Primary : ButtonStyle.Secondary, disabled: tab == section);

        return new ComponentBuilderV2()
            .AddComponent(new ContainerBuilder()
                .WithAccentColor(Color.Purple)
                .AddComponent(new TextDisplayBuilder($"## 🏅 Succès de {Format.Sanitize(displayName)}"))
                .AddComponent(new SeparatorBuilder())
                .AddComponent(new TextDisplayBuilder(body)))
            .AddComponent(tabs)
            .Build();
    }

    private static string TabLabel(BadgeSection section) => section switch
    {
        BadgeSection.Collections => "Collections",
        BadgeSection.Moments => "Moments",
        BadgeSection.Secrets => "Secrets",
        _ => "Paliers",
    };

    private static string CollectionsText(IReadOnlyCollection<Models.CollectionCompletion> completions)
    {
        var byKey = completions.ToDictionary(c => c.SetKey, c => c.Count);
        var lines = ItemCatalog.Sets.Select(set =>
        {
            var tier = byKey.TryGetValue(set.Key, out var n) ? CollectionTiers.TierFor(n) : CollectionTier.None;
            return tier == CollectionTier.None
                ? $"??? **{Format.Sanitize(set.Name)}**"
                : $"{CollectionTiers.Badge(set, tier)} **{Format.Sanitize(set.Name)}** · {CollectionTiers.Label(tier)} · complétée {n} fois";
        });
        return "### Collections\n" + string.Join("\n", lines);
    }

    private static string PaliersText(IReadOnlyList<BadgeStanding> standings)
    {
        var lines = standings.Where(s => s.Info.Kind == MemberBadgeKind.Palier).Select(s =>
        {
            var next = (int)s.Tier < s.Info.Thresholds.Count ? s.Info.Thresholds[(int)s.Tier] : (long?)null;
            var nextTier = CollectionTiers.Label((CollectionTier)((int)s.Tier + 1));
            var progress = next is { } at
                ? $"{Amount(s.Value, s.Info.Unit)} / {Amount(at, s.Info.Unit)} pour {nextTier}"
                : null;
            if (!s.Earned)
                return $"??? *{s.Info.Hint}* · {progress}";
            return $"{MemberBadges.Icon(s)} **{s.Info.Name}** · {CollectionTiers.Label(s.Tier)}"
                + (progress is null ? "" : $" · {progress}");
        });
        return "### Paliers\n" + string.Join("\n", lines);
    }

    private static string MomentsText(IReadOnlyList<BadgeStanding> standings)
    {
        var lines = standings.Where(s => s.Info.Kind == MemberBadgeKind.Moment).Select(s =>
            s.Earned
                ? $"{MemberBadges.Icon(s)} **{s.Info.Name}**{DateSuffix(s)}"
                : $"??? *{s.Info.Hint}*");
        return "### Moments\n" + string.Join("\n", lines);
    }

    private static string SecretsText(IReadOnlyList<BadgeStanding> standings)
    {
        var secrets = standings.Where(s => s.Info.Kind == MemberBadgeKind.Secret).ToList();
        var lines = secrets.Select(s =>
            s.Earned
                ? $"{MemberBadges.Icon(s)} **{s.Info.Name}** · *{s.Info.Hint}*{DateSuffix(s)}"
                : $"??? *{s.Info.Hint}*");
        return $"### Secrets · {secrets.Count(s => s.Earned)} / {secrets.Count} trouvés\n" + string.Join("\n", lines);
    }

    // Derived moments carry no date: they are computed from live data, not stored when they happened.
    private static string DateSuffix(BadgeStanding s) =>
        s.EarnedAt is { } at ? $" · <t:{at.ToUnixTimeSeconds()}:D>" : "";

    // Hours: the raw value is minutes. Days print as they are. Counts group like an XP figure.
    private static string Amount(long value, BadgeUnit unit) => unit switch
    {
        BadgeUnit.Hours => $"{LevelCardUi.Xp(value / 60)} h",
        BadgeUnit.Days => $"{LevelCardUi.Xp(value)} j",
        _ => LevelCardUi.Xp(value),
    };

    private static IEnumerable<(CollectionSet Set, CollectionTier Tier)> CompletedSets(IReadOnlyCollection<Models.CollectionCompletion> completions)
    {
        var byKey = completions.ToDictionary(c => c.SetKey, c => c.Count);
        foreach (var set in ItemCatalog.Sets)
            if (byKey.TryGetValue(set.Key, out var n) && CollectionTiers.TierFor(n) is var tier and not CollectionTier.None)
                yield return (set, tier);
    }
}
