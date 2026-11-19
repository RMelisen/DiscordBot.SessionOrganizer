using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Helpers;

public sealed record PassionInfo(PlynlingPassion Passion, string Emoji, string Label, string[] Keywords);

/// <summary>
/// One passion a Plynling has: a catalog one (rich, hand-written lines) or a custom text taught by
/// its owner (generic templates). Exactly one of the two is set.
/// </summary>
public sealed record Passion(PlynlingPassion? Catalog, string? Custom)
{
    // Two passions are the same when both are the same catalog one, or both custom with equal
    // normalised text ("Les trains !" = "les trains").
    public string Key => Catalog is { } c ? "cat:" + c : "txt:" + PlynlingPassions.Normalize(Custom ?? "");
    public bool SameAs(Passion other) => Key == other.Key;

    /// <summary>For {P} and SYNCS's lines: « la cuisine », or the typed text in « guillemets ».</summary>
    public string Render() => Catalog is { } c ? PlynlingPassions.Info(c).Label : $"« {PlynlingCardUi.SafeName(Custom ?? "")} »";

    /// <summary>For the card: the same as <see cref="Render"/>. The catalog emoji used to lead it;
    /// the owner took it off the card.</summary>
    public string Display() => Render();
}

/// <summary>
/// The passions. Pure: no database, no clock. An innate passion is one of the catalog below; a taught
/// one is free text, upgraded to a catalog passion when <see cref="Resolve"/> recognises it.
/// Here are its keywords; SYNCS quotes a passion on the card and in her care lines (PickLines, Thought).
/// Visits no longer use passions (since 8.2.0, a visit is a played scene: docs/agents/plynling.md).
/// </summary>
public static class PlynlingPassions
{
    public static readonly TimeSpan TeachCooldown = TimeSpan.FromHours(24);

    public static readonly IReadOnlyList<PassionInfo> All = new PassionInfo[]
    {
        new(PlynlingPassion.Cooking, "🍳", "la cuisine",
            new[] { "cuisine", "cuisiner", "recette", "recettes", "patisserie", "gateau", "gateaux", "crepe", "crepes", "cuisson", "chef" }),
        new(PlynlingPassion.Music, "🎵", "la musique",
            new[] { "musique", "chanson", "chansons", "chanter", "chant", "piano", "guitare", "violon", "flute", "batterie", "melodie" }),
        new(PlynlingPassion.Gaming, "🎮", "les jeux vidéo",
            new[] { "jeux video", "jeu video", "gaming", "console", "manette", "minecraft" }),
        new(PlynlingPassion.Astronomy, "🔭", "l'astronomie",
            new[] { "astronomie", "etoile", "etoiles", "planete", "planetes", "lune", "espace", "galaxie", "cosmos", "constellation", "constellations" }),
        new(PlynlingPassion.Gardening, "🌱", "le jardinage",
            new[] { "jardinage", "jardin", "jardiner", "plante", "plantes", "fleur", "fleurs", "potager", "graine", "graines" }),
        new(PlynlingPassion.Rocks, "🪨", "les cailloux",
            new[] { "caillou", "cailloux", "pierre", "pierres", "galet", "galets", "roche", "roches", "mineraux", "cristaux" }),
        new(PlynlingPassion.Stories, "📚", "les histoires",
            new[] { "histoire", "histoires", "lecture", "lire", "livre", "livres", "conte", "contes", "roman", "romans" }),
        new(PlynlingPassion.Dance, "💃", "la danse",
            new[] { "danse", "danser", "ballet", "valse", "tango" }),
        new(PlynlingPassion.Painting, "🎨", "la peinture",
            new[] { "peinture", "peindre", "dessin", "dessiner", "aquarelle", "tableau", "tableaux" }),
        new(PlynlingPassion.Sport, "🏃", "le sport",
            new[] { "sport", "course", "courir", "football", "foot", "natation", "nager", "velo", "escalade", "tennis" }),
        new(PlynlingPassion.Insects, "🐞", "les insectes",
            new[] { "insecte", "insectes", "fourmi", "fourmis", "coccinelle", "coccinelles", "papillon", "papillons", "scarabee", "abeille", "abeilles" }),
        new(PlynlingPassion.Naps, "😴", "les siestes",
            new[] { "sieste", "siestes", "dormir", "sommeil", "dodo", "roupiller" }),
    };

    public static PassionInfo Info(PlynlingPassion passion) => All[(int)passion];

    public static PlynlingPassion RollInnate(Random rng) => (PlynlingPassion)rng.Next(All.Count);

    private static readonly Regex LeadingArticle = new(@"^(de la |de l'|du |des |les |le |la |l'|un |une )", RegexOptions.Compiled);
    private static readonly Regex NotWord = new(@"[^a-z0-9' ]+", RegexOptions.Compiled);
    private static readonly Regex Spaces = new(@"\s+", RegexOptions.Compiled);

    /// <summary>Lowercase, no accents, no leading article, no punctuation, single spaces.</summary>
    public static string Normalize(string text)
    {
        var decomposed = text.Replace('’', '\'').ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var bare = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) bare.Append(ch);
        var s = Spaces.Replace(NotWord.Replace(bare.ToString(), " "), " ").Trim();
        s = LeadingArticle.Replace(s, "");          // the apostrophe is still there, so « l' » is caught
        return Spaces.Replace(s.Replace("'", " "), " ").Trim();
    }

    /// <summary>What gets stored: line breaks and runs of spaces collapsed, trimmed.</summary>
    public static string Clean(string? raw) => raw is null ? "" : Spaces.Replace(raw, " ").Trim();

    public static bool ContainsLink(string text)
    {
        var lower = text.ToLowerInvariant();
        return lower.Contains("http") || lower.Contains("www.") || lower.Contains("discord.gg");
    }

    /// <summary>The catalog passion a typed text names, if a keyword appears in it as a whole word.</summary>
    public static PlynlingPassion? Resolve(string text)
    {
        var norm = " " + Normalize(text) + " ";
        foreach (var info in All)
            foreach (var keyword in info.Keywords)
                if (norm.Contains(" " + keyword + " ")) return info.Passion;
        return null;
    }

    public static Passion FromText(string text) =>
        Resolve(text) is { } c ? new Passion(c, null) : new Passion(null, text);

    public static Passion? Taught(Plynling p) =>
        string.IsNullOrWhiteSpace(p.TaughtPassion) ? null : FromText(p.TaughtPassion);

    /// <summary>
    /// The taught passion when it stayed free text — one the catalog did not recognise. This is what
    /// gets quoted back, Tomodachi-style: SYNCS's card lines and the visits drop the owner's own words
    /// into ordinary moments (see <see cref="PickLines"/> and <see cref="PlynlingVisitStory"/>).
    /// </summary>
    public static Passion? Typed(Plynling p) => Taught(p) is { Catalog: null } t ? t : null;

    /// <summary>How often a card line quotes the typed passion back, when there is one.</summary>
    public const double TypedLineChance = 0.15;

    /// <summary>
    /// A card pool for <paramref name="p"/>: now and then the one quoting its typed passion, with the
    /// passion rendered (« … », sanitised) as the last format argument; otherwise the usual pool and "".
    /// Callers always pass the text, so a usual line simply ignores it.
    /// </summary>
    public static (string[] Pool, string Typed) PickLines(GenderedLines usual, GenderedLines typed, Plynling p, Random rng) =>
        Typed(p) is { } t && rng.NextDouble() < TypedLineChance ? (typed.For(p.Gender), t.Render()) : (usual.For(p.Gender), "");

    /// <summary>How often <c>/plynling view</c> shows what it is thinking about, when it has a typed passion.</summary>
    public const double ThoughtChance = 0.25;

    /// <summary>
    /// The thought bubble on <c>/plynling view</c>: now and then its typed passion, as a thought — or a
    /// dream while it sleeps — with the rendered passion as {1}. Null when there is nothing to show:
    /// no typed passion, dead, frozen, or the roll said no.
    /// </summary>
    public static (string[] Pool, string Typed)? Thought(Plynling p, DateTimeOffset now, Random rng) =>
        p.DiedAt is null && p.FrozenAt is null && Typed(p) is { } t && rng.NextDouble() < ThoughtChance
            ? ((PlynlingLife.IsAsleep(now) ? BotResponses.PlynlingDreamTypedLines : BotResponses.PlynlingThoughtTypedLines).For(p.Gender), t.Render())
            : null;

    /// <summary>Its passions: the innate one, then the taught one unless it is the same.</summary>
    public static IReadOnlyList<Passion> Of(Plynling p)
    {
        var innate = new Passion(p.Passion, null);
        return Taught(p) is { } taught && !taught.SameAs(innate) ? new[] { innate, taught } : new[] { innate };
    }
}
