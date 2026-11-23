using Discord;

namespace ProjectSYNCS.Helpers;

public enum ShortcutKind
{
    View,
    Work,
    Forage,
    Visit,
    Daily,
    // Starts with ;pl but is not one of the above: answered with the list of what works.
    Unknown,
}

/// <param name="UserId">The mentioned person (view, visit), or null for the author / none given.</param>
/// <param name="Place">A forage's place (a ForagePlaces key), or null for none named.</param>
/// <param name="BadPlace">A forage naming a place that matches none: answered with <see cref="PlynlingShortcut.Where"/>, never run.</param>
public readonly record struct Shortcut(ShortcutKind Kind, ulong? UserId = null, string? Place = null, bool BadPlace = false);

/// <summary>
/// The typed shortcuts for the game's routine commands: <c>;pl</c>, <c>;pl @x</c>,
/// <c>;pl visit @x</c>, <c>;forage</c>, <c>;work</c> and <c>;daily</c> (also under <c>;pl</c>). Pure, so the
/// grammar is checkable without a gateway; <c>PlynlingShortcutService</c> runs them.
/// </summary>
/// <remarks>
/// Only the first word decides whether a message is a shortcut at all, and it must be exactly
/// one of the four heads — so <c>;-;</c>, <c>;)</c> or <c>;plop</c> stay ordinary chatter.
/// </remarks>
public static class PlynlingShortcut
{
    // Shown for Unknown and under PlynlingShortcutService's refusals; kept short, it is a reminder.
    public const string Usage =
        "En texte : `;pl` (ta carte), `;pl @quelqu'un`, `;pl visit @quelqu'un`, `;forage [lieu]`, `;work` et `;daily`. Le reste passe par `/pl`.";

    public const string VisitWho = "Chez qui ? `;pl visit @quelqu'un`";

    // The answer to a forage naming no known place: every place with a word that reaches it. A
    // property: it reads ForagePlaces.All, a static field in another file (CLAUDE.md).
    public static string Where =>
        "Où ça ? " + string.Join(" · ", ForagePlaces.All.Select(p => $"{p.Emoji} `;forage {p.Aliases[0]}`")) +
        "\n-# Sans lieu, `;forage` l'envoie au hasard.";

    public static Shortcut? Parse(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var words = content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var head = words[0].ToLowerInvariant();
        var rest = words.AsSpan(1);

        return head switch
        {
            ";work" => rest.IsEmpty ? new Shortcut(ShortcutKind.Work) : new Shortcut(ShortcutKind.Unknown),
            ";forage" => Forage(rest),
            ";daily" => rest.IsEmpty ? new Shortcut(ShortcutKind.Daily) : new Shortcut(ShortcutKind.Unknown),
            ";pl" => ParsePl(rest),
            _ => null,
        };
    }

    // A forage, with whatever follows read as a place (« ;forage la place du puits »).
    private static Shortcut Forage(ReadOnlySpan<string> words)
    {
        if (words.IsEmpty) return new Shortcut(ShortcutKind.Forage);
        return ForagePlaces.Match(string.Join(' ', words.ToArray())) is { } place
            ? new Shortcut(ShortcutKind.Forage, Place: place.Key)
            : new Shortcut(ShortcutKind.Forage, BadPlace: true);
    }

    private static Shortcut ParsePl(ReadOnlySpan<string> words)
    {
        if (words.IsEmpty) return new Shortcut(ShortcutKind.View);
        if (words.Length == 1 && MentionUtils.TryParseUser(words[0], out var viewed))
            return new Shortcut(ShortcutKind.View, viewed);

        var verb = words[0].ToLowerInvariant();
        var args = words[1..];
        return verb switch
        {
            "view" or "v" when args.IsEmpty => new Shortcut(ShortcutKind.View),
            "view" or "v" when args.Length == 1 && MentionUtils.TryParseUser(args[0], out var id) => new Shortcut(ShortcutKind.View, id),
            "work" when args.IsEmpty => new Shortcut(ShortcutKind.Work),
            "forage" => Forage(args),
            "daily" when args.IsEmpty => new Shortcut(ShortcutKind.Daily),
            // A visit without anyone is still a visit: it is answered with who to name.
            "visit" when args.IsEmpty => new Shortcut(ShortcutKind.Visit),
            "visit" when args.Length == 1 && MentionUtils.TryParseUser(args[0], out var host) => new Shortcut(ShortcutKind.Visit, host),
            _ => new Shortcut(ShortcutKind.Unknown),
        };
    }
}
