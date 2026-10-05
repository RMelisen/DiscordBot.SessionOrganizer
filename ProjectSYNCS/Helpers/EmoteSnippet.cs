using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Discord;

namespace ProjectSYNCS.Helpers;

// Writes the Helpers/Emotes.cs entries for a guild's emotes, so adding one is a paste instead of
// copying a name and an id by hand (/debug emotes). Pure: takes plain values, so it can be checked
// without a gateway. Emotes already declared in Emotes.cs are skipped — matched by id, never by
// name, because a re-upload keeps the name and changes the id.
public static class EmoteSnippet
{
    private static readonly Regex _nonAlphanumeric = new("[^A-Za-z0-9]+", RegexOptions.Compiled);

    /// <summary>The snippet for every emote not yet in <see cref="Emotes"/>, and how many that is.</summary>
    public static (string Snippet, int Count) Build(IEnumerable<GuildEmote> emotes)
    {
        var fields = typeof(Emotes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral).ToList();
        var knownIds = fields.Where(f => f.Name.EndsWith("Id", StringComparison.Ordinal))
            .Select(f => (string)f.GetRawConstantValue()!).ToHashSet();
        var usedNames = fields.Select(f => f.Name).ToHashSet();

        var sb = new StringBuilder();
        var count = 0;
        foreach (var e in emotes.Where(e => !knownIds.Contains(e.Id.ToString())).OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
        {
            var name = IdentifierFor(e.Name);
            // A taken name gets the id's tail, so two emotes can never share a constant.
            if (usedNames.Contains(name) || usedNames.Contains(name + "Id"))
                name += e.Id.ToString()[^4..];
            usedNames.Add(name);
            usedNames.Add(name + "Id");

            var prefix = e.Animated ? "a" : "";
            sb.AppendLine($"    /// <summary>`:{e.Name}:`{(e.Animated ? " (animated)" : "")}</summary>");
            sb.AppendLine($"    public const string {name}Id = \"{e.Id}\";");
            sb.AppendLine($"    public const string {name} = $\"<{prefix}:{e.Name}:{{{name}Id}}>\";");
            sb.AppendLine();
            count++;
        }
        return (sb.ToString().TrimEnd(), count);
    }

    // `witch_sad` → `WitchSad`. A C# identifier cannot start with a digit (`10sur10`,
    // `1_zulana…`), so those get an `Emote` prefix; rename by hand if it reads badly.
    internal static string IdentifierFor(string emoteName)
    {
        var parts = _nonAlphanumeric.Split(emoteName).Where(p => p.Length > 0)
            .Select(p => char.ToUpperInvariant(p[0]) + p[1..]);
        var id = string.Concat(parts);
        if (id.Length == 0) return "Emote";
        return char.IsDigit(id[0]) ? "Emote" + id : id;
    }
}
