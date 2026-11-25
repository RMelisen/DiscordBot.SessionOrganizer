namespace ProjectSYNCS.Helpers;

/// <summary>The channel permissions she needs somewhere, as flags.</summary>
[Flags]
public enum ChannelNeed
{
    None = 0,
    View = 1,
    Send = 2,
    Embed = 4,
    Attach = 8,
    React = 16,
    ExternalEmojis = 32,
    History = 64,
}

/// <summary>A channel she is configured to use (or uses by default) on a server.</summary>
public enum HealthSlot { Game, Bots, LevelUp, Quiz, Confess, FreeGames, Main }

/// <summary>One slot's channel as seen from the gateway: 0 when nothing is set (nor defaults) here.</summary>
public readonly record struct SlotFacts(HealthSlot Slot, ulong ChannelId, bool Exists, ChannelNeed Have);

/// <summary>Her server-wide standing, as plain values.</summary>
/// <param name="PlynlingRoleProblem">Why she can't manage the configured Plynling role (RoleMirror.Problem), or null.</param>
/// <param name="QuizRoleProblem">Same, for the Grosse tête role.</param>
public readonly record struct GuildFacts(
    bool ManageEvents, bool PlynlingRoleSet, string? PlynlingRoleProblem, bool QuizRoleSet, string? QuizRoleProblem);

/// <summary>One line of the health check: ✅ or ⚠️, then what is fine or what to fix.</summary>
public readonly record struct HealthLine(bool Ok, string Text)
{
    public override string ToString() => (Ok ? "✅ " : "⚠️ ") + Text;
}

// The health check of /config show and /config setup's summary: for each channel she uses, can she
// do there what it needs; can she copy sessions into the Events tab; can she hand out the roles she
// mirrors. Pure — the gateway facts come in as plain values (Services/Admin/ServerHealthProbe), so
// the checks feed it made-up servers. Every permission is named as Discord's French UI names it, so
// the fix can be followed without translating.
public static class ServerHealth
{
    /// <summary>Discord's French names, in the order a fix lists them.</summary>
    public static readonly IReadOnlyList<(ChannelNeed Need, string Name)> PermissionNames = new[]
    {
        (ChannelNeed.View, "Voir le salon"),
        (ChannelNeed.Send, "Envoyer des messages"),
        (ChannelNeed.Embed, "Intégrer des liens"),
        (ChannelNeed.Attach, "Joindre des fichiers"),
        (ChannelNeed.React, "Ajouter des réactions"),
        (ChannelNeed.ExternalEmojis, "Utiliser des émojis externes"),
        (ChannelNeed.History, "Voir les anciens messages"),
    };

    public const string ManageEventsName = "Gérer les évènements";

    /// <summary>Where a missing channel permission is given, said once under the channel lines.</summary>
    public const string HowToGrant =
        "Pour donner une permission : clic droit sur le salon → Modifier le salon → Permissions → mon rôle.";

    /// <summary>What each slot's channel needs. Pictures (Plynlings, level cards) need files; her lines carry her own emotes.</summary>
    public static ChannelNeed NeedsOf(HealthSlot slot) => slot switch
    {
        HealthSlot.Game => ChannelNeed.View | ChannelNeed.Send | ChannelNeed.Embed | ChannelNeed.Attach | ChannelNeed.ExternalEmojis,
        HealthSlot.Bots => ChannelNeed.View | ChannelNeed.Send | ChannelNeed.Embed | ChannelNeed.Attach | ChannelNeed.ExternalEmojis,
        HealthSlot.LevelUp => ChannelNeed.View | ChannelNeed.Send | ChannelNeed.Embed | ChannelNeed.Attach | ChannelNeed.ExternalEmojis,
        HealthSlot.Quiz => ChannelNeed.View | ChannelNeed.Send | ChannelNeed.Embed | ChannelNeed.ExternalEmojis,
        HealthSlot.Confess => ChannelNeed.View | ChannelNeed.Send | ChannelNeed.Embed,
        HealthSlot.FreeGames => ChannelNeed.View | ChannelNeed.Send | ChannelNeed.Embed,
        // Her hello, her idle lines, her late « vu » reactions; she reads its recent history on start.
        HealthSlot.Main => ChannelNeed.View | ChannelNeed.Send | ChannelNeed.Embed | ChannelNeed.React
                           | ChannelNeed.ExternalEmojis | ChannelNeed.History,
        _ => ChannelNeed.View | ChannelNeed.Send,
    };

    public static string LabelOf(HealthSlot slot) => slot switch
    {
        HealthSlot.Game => "Salon de jeu",
        HealthSlot.Bots => "Salon des bots",
        HealthSlot.LevelUp => "Salon des niveaux",
        HealthSlot.Quiz => "Salon du quiz",
        HealthSlot.Confess => "Salon des confessions",
        HealthSlot.FreeGames => "Salon des jeux gratuits",
        HealthSlot.Main => "Salon principal",
        _ => slot.ToString(),
    };

    // The /config subgroup that sets each slot, for the fix.
    public static string CommandOf(HealthSlot slot) => slot switch
    {
        HealthSlot.Game => "game-channel",
        HealthSlot.Bots => "bots-channel",
        HealthSlot.LevelUp => "levelup-channel",
        HealthSlot.Quiz => "quiz-channel",
        HealthSlot.Confess => "confess-channel",
        HealthSlot.FreeGames => "freegames-channel",
        HealthSlot.Main => "main-channel",
        _ => "setup",
    };

    /// <summary>
    /// The French names of what <paramref name="have"/> lacks of <paramref name="need"/>, in order —
    /// only the ones to give first. Discord computes every other permission as denied in a channel she
    /// can't see, and links and files as denied where she can't write: naming those too would send
    /// staff after permissions that are already fine (and run the card past its text budget).
    /// </summary>
    public static IReadOnlyList<string> Missing(ChannelNeed need, ChannelNeed have)
    {
        if (need.HasFlag(ChannelNeed.View) && !have.HasFlag(ChannelNeed.View))
            return new[] { NameOf(ChannelNeed.View) };
        var hidden = need.HasFlag(ChannelNeed.Send) && !have.HasFlag(ChannelNeed.Send)
            ? ChannelNeed.Embed | ChannelNeed.Attach
            : ChannelNeed.None;
        return PermissionNames
            .Where(p => need.HasFlag(p.Need) && !have.HasFlag(p.Need) && !hidden.HasFlag(p.Need))
            .Select(p => p.Name).ToList();
    }

    private static string NameOf(ChannelNeed need) => PermissionNames.First(p => p.Need == need).Name;

    /// <summary>« « A » et « B » », « « A », « B » et « C » ».</summary>
    public static string JoinNames(IReadOnlyList<string> names)
    {
        var quoted = names.Select(n => $"« {n} »").ToList();
        return quoted.Count <= 1 ? string.Concat(quoted)
            : string.Join(", ", quoted.Take(quoted.Count - 1)) + " et " + quoted[^1];
    }

    /// <summary>
    /// The health lines: every set channel, then the announcements' fallback, the Events tab and the
    /// mirrored roles. Unset optional channels (quiz, confessions, free games) are no problem: off is
    /// a choice.
    /// </summary>
    public static IReadOnlyList<HealthLine> Judge(IReadOnlyList<SlotFacts> slots, GuildFacts guild)
    {
        var lines = new List<HealthLine>();
        bool anyMissing = false;

        foreach (var slot in slots.Where(s => s.ChannelId != 0))
        {
            var label = LabelOf(slot.Slot);
            if (!slot.Exists)
            {
                lines.Add(new(false, $"{label} : ce salon n'existe plus. Choisis-en un autre avec `/config {CommandOf(slot.Slot)} set`."));
                continue;
            }

            var missing = Missing(NeedsOf(slot.Slot), slot.Have);
            anyMissing |= missing.Count > 0;
            lines.Add(missing.Count == 0
                ? new(true, $"{label} <#{slot.ChannelId}>")
                : new(false, $"{label} <#{slot.ChannelId}> : il me manque {JoinNames(missing)}."));
        }

        // Said once, not on every line: the card has a 4000-character budget.
        if (anyMissing)
            lines.Add(new(false, HowToGrant));

        // Plynling news and Quokka's visits fall back game → bots, level-up cards levelup → bots → game.
        bool announcements = slots.Any(s => s.ChannelId != 0 && s.Exists
                                            && s.Slot is HealthSlot.Game or HealthSlot.Bots);
        if (!announcements)
            lines.Add(new(false, "Aucun salon de jeu ni des bots : mes annonces n'ont nulle part où aller, et les "
                                 + "cartes de niveau tombent là où l'XP est gagnée. Choisis-en un avec `/config setup`."));

        lines.Add(guild.ManageEvents
            ? new(true, $"« {ManageEventsName} » : les sessions sont copiées dans l'onglet Évènements")
            : new(false, $"Il me manque « {ManageEventsName} » : les sessions ne sont pas copiées dans l'onglet "
                         + "Évènements (le reste marche)."));

        if (guild.PlynlingRoleSet)
            lines.Add(guild.PlynlingRoleProblem is { } p ? new(false, $"Rôle Plynling : {p}") : new(true, "Rôle Plynling"));
        if (guild.QuizRoleSet)
            lines.Add(guild.QuizRoleProblem is { } q ? new(false, $"Rôle Grosse tête : {q}") : new(true, "Rôle Grosse tête"));

        return lines;
    }

    /// <summary>
    /// What is not set on this server yet, by name, for /config show's « Pas encore réglé » line: the
    /// channels with no channel here (configured or default) and the moderator role.
    /// </summary>
    public static IReadOnlyList<string> Unset(IReadOnlyList<SlotFacts> slots, bool moderatorRoleSet)
    {
        var unset = slots.Where(s => s.ChannelId == 0).Select(s => LabelOf(s.Slot).ToLowerInvariant()).ToList();
        if (!moderatorRoleSet) unset.Add("rôle modérateur");
        return unset;
    }
}
