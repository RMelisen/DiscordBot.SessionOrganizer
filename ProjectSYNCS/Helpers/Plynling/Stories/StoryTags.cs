using System.Text;

namespace ProjectSYNCS.Helpers;

public sealed record StoryWord(string Text, bool Quoted);

// A line cut into the tags before its text, the text, and the tags after it. Problem is set when the
// brackets do not fit that shape (a tag between two pieces of text, an unclosed or stray bracket).
public sealed record StoryLineParts(IReadOnlyList<string> Leading, string Text, IReadOnlyList<string> Trailing, string? Problem);

/// <summary>
/// The bracket layer of a story line. A tag is <c>[…]</c>; inside one, <c>"…"</c> is a quoted word
/// that may hold any bracket; outside, <c>{…}</c> is a template token, never a tag.
/// </summary>
public static class StoryTags
{
    public static StoryLineParts Split(string line)
    {
        // Pieces in order: (isTag, content).
        var pieces = new List<(bool Tag, string Content)>();
        var current = new StringBuilder();
        var inTag = false;
        var inQuote = false;
        var braces = 0;
        foreach (var ch in line)
        {
            if (inTag)
            {
                if (ch == '"') inQuote = !inQuote;
                else if (!inQuote && ch == ']')
                {
                    pieces.Add((true, current.ToString().Trim()));
                    current.Clear();
                    inTag = false;
                    continue;
                }
                else if (!inQuote && ch == '[') return Broken(line, "'[' inside a tag");
                current.Append(ch);
                continue;
            }
            if (ch == '{') braces++;
            else if (ch == '}' && braces > 0) braces--;
            else if (braces == 0 && ch == '[')
            {
                pieces.Add((false, current.ToString()));
                current.Clear();
                inTag = true;
                continue;
            }
            else if (braces == 0 && ch == ']') return Broken(line, "']' without its '['");
            current.Append(ch);
        }
        if (inTag) return Broken(line, inQuote ? "unclosed '\"' in a tag" : "unclosed '['");
        pieces.Add((false, current.ToString()));

        var textAt = pieces.Select((p, i) => (p, i)).Where(x => !x.p.Tag && x.p.Content.Trim().Length > 0).Select(x => x.i).ToList();
        if (textAt.Count == 0)
            return new StoryLineParts(pieces.Where(p => p.Tag).Select(p => p.Content).ToList(), "", Array.Empty<string>(), null);
        int first = textAt[0], last = textAt[^1];
        if (pieces.Skip(first).Take(last - first).Any(p => p.Tag))
            return Broken(line, "a tag inside prose (tags go at the start or the end of a line)");
        var leading = pieces.Take(first).Where(p => p.Tag).Select(p => p.Content).ToList();
        var trailing = pieces.Skip(last + 1).Where(p => p.Tag).Select(p => p.Content).ToList();
        var text = string.Concat(pieces.Skip(first).Take(last - first + 1).Select(p => p.Content)).Trim();
        return new StoryLineParts(leading, text, trailing, null);
    }

    public static IReadOnlyList<StoryWord> Words(string tag)
    {
        var words = new List<StoryWord>();
        var current = new StringBuilder();
        var inQuote = false;
        void Flush()
        {
            if (current.Length == 0) return;
            var w = current.ToString();
            current.Clear();
            // "->target" reads as two words.
            if (w.StartsWith("->") && w.Length > 2) { words.Add(new("->", false)); words.Add(new(w[2..], false)); }
            else words.Add(new(w, false));
        }
        foreach (var ch in tag)
        {
            if (inQuote)
            {
                if (ch == '"') { words.Add(new(current.ToString(), true)); current.Clear(); inQuote = false; }
                else current.Append(ch);
            }
            else if (ch == '"') { Flush(); inQuote = true; }
            else if (char.IsWhiteSpace(ch)) Flush();
            else current.Append(ch);
        }
        if (inQuote) words.Add(new(current.ToString(), true));
        else Flush();
        return words;
    }

    private static StoryLineParts Broken(string line, string problem) =>
        new(Array.Empty<string>(), line.Trim(), Array.Empty<string>(), problem);
}
