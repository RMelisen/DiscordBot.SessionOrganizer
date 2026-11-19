using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// Reads a visit file into a <see cref="VisitScene"/>. It reuses the story format's line reader (BOM,
/// CRLF, <c>//</c> notes), its tag layer and its vocabulary; the shape is its own:
/// <code>
/// # visit: key            (or '# confession: key')
/// title: …
/// place: 🌳 …
/// bonds: friends rivals   (not in a confession)
/// hours: 8-20             (optional, Paris; 20-1 runs across midnight)
/// stages: teen adult      (optional, all four by default)
///
/// Intro prose.
///
/// * key | Label  [lean boldness +2]    ← the visitor's option
///   ? good                             ← '? yes' / '? no' in a confession
///   …
///   ? bad
///   …
/// > key | Label  [lean compassion +1]  ← the host's reaction
/// </code>
/// Every problem becomes a <see cref="StoryError"/> with its line, and parsing goes on. Ranges, counts,
/// lengths and cross-file rules are <see cref="VisitValidator"/>'s.
/// </summary>
public static class VisitParser
{
    private static readonly Regex ChoiceLine = new(@"^([*>])\s*([^|]*?)\s*\|\s*(.*)$", RegexOptions.Compiled);
    private static readonly Regex HoursValue = new(@"^(\d{1,2})\s*-\s*(\d{1,2})$", RegexOptions.Compiled);

    public static VisitScene? Parse(string file, string text, List<StoryError> errors)
    {
        var lines = StoryParser.Read(file, text, errors);
        StoryPos At(StoryParser.Src l) => new(file, l.No);
        void Error(StoryParser.Src l, string message) => errors.Add(new StoryError(At(l), message));

        var start = lines.FindIndex(l => l.Text.Length > 0);
        if (start < 0) { errors.Add(new(new(file, 1), "empty file; it must start with '# visit: <key>' or '# confession: <key>'")); return null; }
        var first = lines[start];
        var kind = StoryParser.KindLine.Match(first.Text);
        if (!kind.Success || first.Text.StartsWith("##") || kind.Groups[1].Value is not ("visit" or "confession"))
        {
            Error(first, "a visit file starts with '# visit: <key>' or '# confession: <key>'");
            return null;
        }
        var confession = kind.Groups[1].Value == "confession";
        var key = kind.Groups[2].Value;
        if (!StoryVocabulary.IsKey(key) || key.Contains('.')) { Error(first, $"bad scene key '{key}'"); return null; }

        // The header runs to the first blank line; the intro to the first choice.
        var rest = lines.Skip(start + 1).ToList();
        var blank = rest.FindIndex(l => l.Text.Length == 0);
        var header = blank < 0 ? rest : rest.Take(blank).ToList();
        var body = blank < 0 ? new List<StoryParser.Src>() : rest.Skip(blank).ToList();

        string title = "", place = "";
        var bonds = new HashSet<PlynlingBond>();
        int from = 0, to = 24;
        var stages = new HashSet<PlynlingStage>(Enum.GetValues<PlynlingStage>());
        var seen = new HashSet<string>();
        foreach (var l in header)
        {
            if (Structural(l, Error)) continue;
            var m = StoryParser.FieldLine.Match(l.Text);
            if (!m.Success) { Error(l, $"expected 'field: value' in the header, got '{l.Text}'"); continue; }
            var (field, value) = (m.Groups[1].Value, m.Groups[2].Value.Trim());
            if (!seen.Add(field)) { Error(l, $"'{field}' given twice"); continue; }
            switch (field)
            {
                case "title": title = value; break;
                case "place": place = value; break;
                case "bonds":
                    if (confession) { Error(l, "a confession takes no 'bonds:' (it is offered whatever the bond)"); break; }
                    foreach (var word in Words(value))
                        if (StoryVocabulary.TryEnum<PlynlingBond>(word, out var b)) bonds.Add(b);
                        else Error(l, $"bond '{word}': expected one of {StoryVocabulary.Choices<PlynlingBond>()}");
                    break;
                case "hours":
                {
                    var hm = HoursValue.Match(value);
                    var f = hm.Success ? int.Parse(hm.Groups[1].Value) : -1;
                    var t = hm.Success ? int.Parse(hm.Groups[2].Value) : -1;
                    if (f is >= 0 and <= 23 && t is >= 1 and <= 24 && f != t)
                        (from, to) = (f, t);
                    else Error(l, $"hours '{value}': write From-To in Paris hours, From 0-23, To 1-24, not equal (8-20, or 20-1 across midnight)");
                    break;
                }
                case "stages":
                    stages.Clear();
                    foreach (var word in Words(value))
                        if (StoryVocabulary.TryEnum<PlynlingStage>(word, out var st)) stages.Add(st);
                        else Error(l, $"stage '{word}': expected one of {StoryVocabulary.Choices<PlynlingStage>()}");
                    if (stages.Count == 0) stages.UnionWith(Enum.GetValues<PlynlingStage>());
                    break;
                default: Error(l, $"unknown header field '{field}' (title, place, {(confession ? "" : "bonds, ")}hours, stages)"); break;
            }
        }

        var firstChoice = body.FindIndex(l => l.Text.StartsWith('*') || l.Text.StartsWith('>'));
        var introLines = firstChoice < 0 ? body : body.Take(firstChoice).ToList();
        var intro = Intro(introLines, Error);

        var options = new List<VisitChoice>();
        var reactions = new List<VisitChoice>();
        if (firstChoice >= 0)
        {
            var chunks = new List<List<StoryParser.Src>>();
            foreach (var l in body.Skip(firstChoice))
                if (l.Text.StartsWith('*') || l.Text.StartsWith('>')) chunks.Add(new List<StoryParser.Src> { l });
                else chunks[^1].Add(l);
            foreach (var chunk in chunks)
                if (ParseChoice(chunk, confession, At, Error) is { } c)
                    (chunk[0].Text.StartsWith('*') ? options : reactions).Add(c);
        }
        return new VisitScene(key, confession, title, place, bonds, from, to, stages, intro, options, reactions, At(first));
    }

    private static string[] Words(string value) => value.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

    // A '#' or '##' line past the first: never part of a visit file. Reported and skipped.
    private static bool Structural(StoryParser.Src l, Action<StoryParser.Src, string> error)
    {
        if (l.Text.StartsWith("##")) { error(l, "a visit file has no '##' sections"); return true; }
        if (l.Text.StartsWith('#')) { error(l, "only one '#' line per visit file"); return true; }
        return false;
    }

    // Paragraphs separated by blank lines; a paragraph's lines kept as lines. No tags, no fields.
    private static string Intro(List<StoryParser.Src> lines, Action<StoryParser.Src, string> error)
    {
        var paragraphs = new List<List<string>>();
        var current = new List<string>();
        foreach (var l in lines)
        {
            if (l.Text.Length == 0) { if (current.Count > 0) { paragraphs.Add(current); current = new(); } continue; }
            if (Structural(l, error)) continue;
            if (StoryParser.LooksLikeField.IsMatch(l.Text)) { error(l, $"'{l.Text}' looks like a header field; the header ends at the first blank line"); continue; }
            if (l.Text.Contains('[') || l.Text.Contains(']')) { error(l, "no tags in the intro"); continue; }
            current.Add(l.Text);
        }
        if (current.Count > 0) paragraphs.Add(current);
        return string.Join("\n\n", paragraphs.Select(p => string.Join("\n", p)));
    }

    private static VisitChoice? ParseChoice(List<StoryParser.Src> chunk, bool confession, Func<StoryParser.Src, StoryPos> at,
        Action<StoryParser.Src, string> error)
    {
        var head = chunk[0];
        var parts = StoryTags.Split(head.Text);
        if (parts.Problem is not null) { error(head, parts.Problem); return null; }
        var m = ChoiceLine.Match(parts.Text);
        if (!m.Success || parts.Leading.Count > 0)
        {
            error(head, "a choice line is '* <key> | <label>' (an option) or '> <key> | <label>' (a reaction), its tags after the label");
            return null;
        }
        var key = m.Groups[2].Value;
        var label = m.Groups[3].Value.Trim();
        if (!StoryVocabulary.IsKey(key) || key.Contains('.')) { error(head, $"bad choice key '{key}'"); return null; }

        var lean = new Dictionary<AiAxis, int>();
        void Tag(StoryParser.Src l, string tag)
        {
            var words = StoryTags.Words(tag).Select(w => w.Text).ToList();
            if (words.Count == 0 || words[0] != "lean") { error(l, $"[{tag}]: visit choices take only [lean <axis> ±n]"); return; }
            if (!StoryVocabulary.TryLean(words, out var axis, out var n)) { error(l, $"[{tag}]: {StoryVocabulary.LeanShape}"); return; }
            if (!lean.TryAdd(axis, n)) error(l, $"[{tag}]: '{words[1]}' leaned twice");
        }
        foreach (var tag in parts.Trailing) Tag(head, tag);

        // Tag-only lines before the first half are more attributes; then the halves.
        var (goodWord, badWord) = confession ? ("? yes", "? no") : ("? good", "? bad");
        var (otherGood, otherBad) = confession ? ("? good", "? bad") : ("? yes", "? no");
        var good = new List<string>();
        var bad = new List<string>();
        List<string>? into = null;
        bool hasGood = false, hasBad = false;
        foreach (var l in chunk.Skip(1))
        {
            if (l.Text.Length == 0 || Structural(l, error)) continue;
            if (l.Text.StartsWith('?'))
            {
                var word = l.Text;
                if (word == otherGood || word == otherBad)
                {
                    error(l, confession
                        ? $"'{word}' is for visit scenes; a confession's halves are '? yes' and '? no'"
                        : $"'{word}' is for the confession; a visit's halves are '? good' and '? bad'");
                    word = word == otherGood ? goodWord : badWord;
                }
                if (word == goodWord)
                {
                    if (hasGood) { error(l, $"'{goodWord}' given twice"); into = null; continue; }
                    hasGood = true;
                    into = good;
                }
                else if (word == badWord)
                {
                    if (hasBad) { error(l, $"'{badWord}' given twice"); into = null; continue; }
                    hasBad = true;
                    into = bad;
                }
                else { error(l, $"'{l.Text}': write '{goodWord}' or '{badWord}'"); into = null; }
                continue;
            }
            if (into is null)
            {
                var p = StoryTags.Split(l.Text);
                if (!hasGood && !hasBad && p.Problem is null && p.Text.Length == 0 && p.Leading.Count > 0)
                {
                    foreach (var tag in p.Leading) Tag(l, tag);
                    continue;
                }
                if (!hasGood && !hasBad) error(l, $"choice '{key}': its text goes under '{goodWord}' and '{badWord}'");
                continue;
            }
            // A half's text: a face tag may open its first line; any other bracket is a stray tag.
            var (rest, faceTag) = PlynlingVisitStory.Untag(l.Text);
            if ((faceTag is not null && into.Count > 0) || rest.Contains('[') || rest.Contains(']'))
            {
                error(l, "a tag inside a half: only a face tag ([sad], [A:happy B:sad]) may open a half, and attributes go on the choice line");
                continue;
            }
            into.Add(l.Text);
        }
        if (!hasGood) error(head, $"choice '{key}' has no '{goodWord}'");
        if (!hasBad) error(head, $"choice '{key}' has no '{badWord}'");
        return new VisitChoice(key, label, lean, new VisitHalves(string.Join(' ', good), string.Join(' ', bad)), at(head));
    }
}
