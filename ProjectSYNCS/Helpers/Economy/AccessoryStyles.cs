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

    private static Choice Colour(params (string Key, string Name)[] options) => Named("Couleur", options);

    private static Choice Named(string label, params (string Key, string Name)[] options) =>
        new(label, options.Select(o => new Option(o.Key, o.Name)).ToList());

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
        "cos.accessory.echarpe" => new[] { Colour(("rouge", "Rouge"), ("bleue", "Bleue"), ("verte", "Verte"), ("rose", "Rose"), ("violette", "Violette")), Place },
        "cos.accessory.casquette" => new[] { Colour(("bleue", "Bleue"), ("rouge", "Rouge"), ("verte", "Verte")) },
        "cos.accessory.ballon" => new[] { Colour(("rouge", "Rouge"), ("bleu", "Bleu"), ("jaune", "Jaune")), Hand },
        "cos.accessory.moufles" => new[] { Colour(("rouges", "Rouges"), ("bleues", "Bleues"), ("vertes", "Vertes"), ("roses", "Roses"), ("violettes", "Violettes")) },
        "cos.accessory.bonnet" => new[] { Colour(("rouge", "Rouge"), ("bleu", "Bleu"), ("vert", "Vert"), ("rose", "Rose"), ("violet", "Violet")) },
        "cos.accessory.collier_coquillages" or "cos.accessory.medaillon" => new[] { Place },
        "cos.accessory.kanzashi" => new[] { Colour(("sakura", "Sakura"), ("rouge", "Rouge"), ("indigo", "Indigo"), ("glycine", "Glycine")), Sides("Côté", false) },
        "cos.accessory.eventail" => new[] { Colour(("rouge", "Rouge"), ("indigo", "Indigo"), ("sakura", "Sakura"), ("glycine", "Glycine")), Hand },
        "cos.accessory.tulipe" => new[] { Colour(("rouge", "Rouge"), ("jaune", "Jaune"), ("violette", "Violette"), ("blanche", "Blanche")), Hand },
        "cos.accessory.glace" => new[] { Named("Parfum", ("fraise", "Fraise"), ("chocolat", "Chocolat"), ("vanille", "Vanille"), ("pistache", "Pistache")), Hand },
        "cos.accessory.sucette" => new[] { Colour(("rose", "Rose"), ("bleue", "Bleue"), ("verte", "Verte"), ("arcenciel", "Arc-en-ciel")), Hand },
        "cos.accessory.lanterne" => new[] { Colour(("ambree", "Ambrée"), ("rouge", "Rouge"), ("bleue", "Bleue")), Hand },
        "cos.accessory.noeud_papillon" => new[] { Colour(("marine", "Marine"), ("noir", "Noir"), ("rouge", "Rouge"), ("bordeaux", "Bordeaux")) },
        "cos.accessory.haut_de_forme" => new[] { Named("Ruban", ("rouge", "Rouge"), ("noir", "Noir"), ("bleu", "Bleu"), ("violet", "Violet")) },
        "cos.accessory.bonnet_nuit" => new[] { Colour(("nuit", "Bleu nuit"), ("rouge", "Rouge"), ("vert", "Vert")) },
        "cos.accessory.lunettes_coeur" => new[] { Colour(("roses", "Roses"), ("rouges", "Rouges"), ("violettes", "Violettes")) },
        "cos.accessory.plume_doree" or "cos.accessory.bocal" or "cos.accessory.canne_peche" or "cos.accessory.cafe"
            or "cos.accessory.canne" or "cos.accessory.montre_gousset"
            or "cos.accessory.onigiri" or "cos.accessory.katana" or "cos.accessory.doudou" => new[] { Hand },
        _ => Array.Empty<Choice>(),
    };

    /// <summary>
    /// The style to show: the stored options read in choice order, each kept where it is one of its
    /// choice's, a choice it skips taking its default — so a stale or unknown style still names a drawn
    /// picture, and a style stored before a choice was added in front keeps its later parts (a tulipe's
    /// « droite » from before it had colours reads « rouge_droite »). A part no remaining choice knows is
    /// dropped. Null for an accessory without styles.
    /// </summary>
    public static string? Resolve(string? accessoryKey, string? style)
    {
        var choices = Choices(accessoryKey);
        if (choices.Count == 0) return null;
        var parts = style?.Split('_') ?? Array.Empty<string>();
        var picked = new string[choices.Count];
        var p = 0;
        for (var i = 0; i < choices.Count; i++)
        {
            while (p < parts.Length && !choices.Skip(i).Any(c => Has(c, parts[p]))) p++;
            picked[i] = p < parts.Length && Has(choices[i], parts[p]) ? parts[p++] : choices[i].Options[0].Key;
        }
        return string.Join('_', picked);
    }

    private static bool Has(Choice choice, string option) => choice.Options.Any(o => o.Key == option);

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
