namespace ProjectSYNCS.Helpers;

/// <summary>
/// The styles a worn accessory comes in: zero to two choices (« Couleur » or « Position », and for the
/// sided ones « Côté » or « Main », always last), each with its options, the first being the
/// default. A Plynling stores the picked options joined by « _ » in choice order
/// (<c>Plynling.AccessoryStyle</c>, « bleue_haut »), and the layer names carry the same string
/// (<see cref="PlynlingLook"/>, <c>Assets/Plynlings/acc/…</c>). Must match the styles drawn in
/// tools/plynling-art/accessories.py — the checks (section 62) compare them with the files on disk.
/// Option keys are stored: append-only, never renamed, never containing « _ ».
/// </summary>
public static class AccessoryStyles
{
    public sealed record Option(string Key, string Name);

    public sealed record Choice(string Label, IReadOnlyList<Option> Options);

    private static Choice Colour(params (string Key, string Name)[] options) =>
        new("Couleur", options.Select(o => new Option(o.Key, o.Name)).ToList());

    private static Choice Sides(string label, bool leftFirst)
    {
        var left = new Option("gauche", "À gauche");
        var right = new Option("droite", "À droite");
        return new Choice(label, leftFirst ? new[] { left, right } : new[] { right, left });
    }

    private static readonly Choice Hand = Sides("Main", true);

    private static readonly Choice Place = new("Position", new[] { new Option("haut", "En haut"), new Option("bas", "En bas") });

    /// <summary>The choices of an accessory (a cosmetic key); empty for one without styles.</summary>
    public static IReadOnlyList<Choice> Choices(string? accessoryKey) => accessoryKey switch
    {
        "cos.accessory.noeud" => new[] { Colour(("rose", "Rose"), ("bleu", "Bleu"), ("dore", "Doré"), ("blanc", "Blanc")), Sides("Côté", true) },
        "cos.accessory.fleur" => new[] { Colour(("blanche", "Blanche"), ("rose", "Rose"), ("bleue", "Bleue"), ("jaune", "Jaune")), Sides("Côté", false) },
        "cos.accessory.echarpe" => new[] { Colour(("rouge", "Rouge"), ("bleue", "Bleue"), ("verte", "Verte")), Place },
        "cos.accessory.casquette" => new[] { Colour(("bleue", "Bleue"), ("rouge", "Rouge"), ("verte", "Verte")) },
        "cos.accessory.ballon" => new[] { Colour(("rouge", "Rouge"), ("bleu", "Bleu"), ("jaune", "Jaune")), Hand },
        "cos.accessory.moufles" => new[] { Colour(("rouges", "Rouges"), ("bleues", "Bleues"), ("vertes", "Vertes")) },
        "cos.accessory.bonnet" => new[] { Colour(("rouge", "Rouge"), ("bleu", "Bleu"), ("vert", "Vert")) },
        "cos.accessory.collier_coquillages" or "cos.accessory.medaillon" => new[] { Place },
        "cos.accessory.kanzashi" => new[] { Sides("Côté", false) },
        "cos.accessory.eventail" => new[] { Colour(("rouge", "Rouge"), ("indigo", "Indigo"), ("sakura", "Sakura")), Hand },
        "cos.accessory.tulipe" or "cos.accessory.glace" or "cos.accessory.plume_doree"
            or "cos.accessory.bocal" or "cos.accessory.canne_peche" or "cos.accessory.lanterne" or "cos.accessory.cafe"
            or "cos.accessory.canne" or "cos.accessory.montre_gousset"
            or "cos.accessory.onigiri" or "cos.accessory.katana" => new[] { Hand },
        _ => Array.Empty<Choice>(),
    };

    /// <summary>
    /// The style to show: each stored option kept where it is one of its choice's, else that choice's
    /// default — so a stale or unknown style still names a drawn picture. Null for an accessory
    /// without styles.
    /// </summary>
    public static string? Resolve(string? accessoryKey, string? style)
    {
        var choices = Choices(accessoryKey);
        if (choices.Count == 0) return null;
        var parts = style?.Split('_') ?? Array.Empty<string>();
        return string.Join('_', choices.Select((c, i) =>
            i < parts.Length && c.Options.Any(o => o.Key == parts[i]) ? parts[i] : c.Options[0].Key));
    }

    /// <summary>The stored style with choice <paramref name="choice"/> set to <paramref name="option"/>.</summary>
    public static string? With(string? accessoryKey, string? style, int choice, string option)
    {
        var resolved = Resolve(accessoryKey, style);
        if (resolved is null) return null;
        var parts = resolved.Split('_');
        if (choice < 0 || choice >= parts.Length) return resolved;
        parts[choice] = option;
        return Resolve(accessoryKey, string.Join('_', parts));
    }
}
