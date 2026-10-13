namespace ProjectSYNCS.Helpers;

/// <summary>What the sync needs to know about the configured role, as plain values.</summary>
public readonly record struct RoleFacts(bool Exists, bool IsEveryone, bool IsManaged, int Position, ulong RawPermissions)
{
    /// <summary>The configured id no longer resolves: the role was deleted in Discord.</summary>
    public static RoleFacts Missing => new(false, false, false, 0, 0);
}

/// <summary>What the sync needs to know about her own standing in the guild.</summary>
public readonly record struct BotFacts(bool CanManageRoles, int TopPosition);

// The rules of a role SYNCS mirrors (the Plynling owners, the quiz's Grosse tête), without a
// gateway: who should gain or lose it, and whether she can manage a given role at all. One Refusal
// backs `/config … set`, `/config show` and every pass's skip check, so they can never disagree
// about what is manageable. RoleMirror applies them.
//
// Strict mirror: holding the role means being in the set its owner computes, nothing else.
public static class RoleMirrorRules
{
    // A wrong owner list (a bug, a bad read) could otherwise strip the role from the whole guild in
    // one pass. Additions are uncapped: a wrong one is harmless here.
    public const int MaxRemovalsPerPass = 25;

    public const string NoManageRoles =
        "Il me manque la permission **Gérer les rôles** (Paramètres du serveur → Rôles → mon rôle).";

    /// <summary>
    /// Why she cannot manage <paramref name="role"/>, in French, or null when she can.
    /// <paramref name="adopting"/> is true for <c>set</c>: picking a role that carries
    /// permissions is refused, because the sync would then hand out and strip those permissions.
    /// The sync itself passes false — staff adding a permission to the role later is their call.
    /// <paramref name="command"/> is the <c>/config</c> subgroup that manages it (<c>plynling-role</c>).
    /// </summary>
    public static string? Refusal(RoleFacts role, BotFacts bot, bool adopting, string command)
    {
        if (!role.Exists)
            return $"Ce rôle n'existe plus. Recrée-le avec `/config {command} create`, ou choisis-en un autre avec `set`.";
        if (role.IsEveryone)
            return "`@everyone`, sérieusement ? Choisis un vrai rôle (¬‿¬)";
        if (role.IsManaged)
            return "Ce rôle appartient à une intégration (un bot, les boosts…) : je ne peux pas le distribuer.";
        if (!bot.CanManageRoles)
            return NoManageRoles;
        if (role.Position >= bot.TopPosition)
            return "Ce rôle est au-dessus du mien, ou au même niveau : monte mon rôle plus haut dans la liste (Paramètres du serveur → Rôles).";
        if (adopting && role.RawPermissions != 0)
            return "Ce rôle donne des permissions. Celui-ci doit rester décoratif : choisis-en un sans aucune permission, ou laisse-moi le créer avec `create`.";
        return null;
    }

    /// <summary>
    /// Who gains and who loses the role. Owners who are not in the guild are skipped (never
    /// retried, never an error). Both lists sorted by id, so a capped pass is deterministic.
    /// </summary>
    public static (IReadOnlyList<ulong> ToAdd, IReadOnlyList<ulong> ToRemove) Diff(
        IReadOnlySet<ulong> owners, IReadOnlySet<ulong> holders, IReadOnlySet<ulong> members)
    {
        var toAdd = owners.Where(id => members.Contains(id) && !holders.Contains(id)).Order().ToList();
        var toRemove = holders.Where(id => !owners.Contains(id)).Order().ToList();
        return (toAdd, toRemove);
    }

    /// <summary>« 4 personnes l'obtiendront, 2 le perdront. » — what a first sync is about to do.</summary>
    public static string Counts(int toAdd, int toRemove)
    {
        if (toAdd == 0 && toRemove == 0) return "Tout le monde l'a déjà comme il faut.";

        var parts = new List<string>();
        if (toAdd > 0) parts.Add(toAdd == 1 ? "1 personne l'obtiendra" : $"{toAdd} personnes l'obtiendront");
        if (toRemove > 0) parts.Add(toRemove == 1 ? "1 personne le perdra" : $"{toRemove} personnes le perdront");
        var text = string.Join(", ", parts) + ".";
        if (toRemove > MaxRemovalsPerPass)
            text += $" Je retire au plus {MaxRemovalsPerPass} rôles par heure, donc ça prendra un moment.";
        return text;
    }
}
