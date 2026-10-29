using System.Globalization;
using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// Reads a story file into a <see cref="StoryArc"/> (the format is in
/// docs/superpowers/specs/2026-10-29-plynling-story-file-format-design.md). Every problem becomes a
/// <see cref="StoryError"/> with its file and line, and parsing goes on, so one run names them all.
/// Cross-file questions (does this key exist?) are <see cref="StoryValidator"/>'s.
/// </summary>
public static partial class StoryParser
{
    private sealed record Src(int No, string Text);

    private static readonly Regex KindLine = new(@"^#\s*([a-z_]+)\s*(?::\s*(\S*))?\s*$", RegexOptions.Compiled);
    private static readonly Regex SectionLine = new(@"^##\s*([a-z_]+)\s*(?::\s*(\S*))?\s*$", RegexOptions.Compiled);
    private static readonly Regex FieldLine = new(@"^([a-z_]+)\s*:\s*(.*)$", RegexOptions.Compiled);

    private static readonly Regex TrailingNote = new(@"(^|\s)//", RegexOptions.Compiled);

    // Lines that mean something: BOM stripped, CRLF or LF, trailing spaces trimmed, blank lines kept
    // (they close paragraphs), // author notes dropped. A note at the end of a line is an error (and
    // cut off), or it would reach players as text.
    private static List<Src> Read(string file, string text, List<StoryError> errors)
    {
        if (text.Length > 0 && text[0] == '﻿') text = text[1..];
        var raw = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var lines = new List<Src>();
        for (var i = 0; i < raw.Length; i++)
        {
            var t = raw[i].Trim();
            if (t.StartsWith("//")) continue;
            if (TrailingNote.Match(t) is { Success: true } note)
            {
                errors.Add(new StoryError(new(file, i + 1), "author notes (//) go on their own line"));
                t = t[..note.Index].TrimEnd();
            }
            lines.Add(new Src(i + 1, t));
        }
        return lines;
    }

    public static StoryArc? Parse(string file, string text, List<StoryError> errors)
    {
        var lines = Read(file, text, errors);
        StoryPos At(Src l) => new(file, l.No);
        void Error(Src l, string message) => errors.Add(new StoryError(At(l), message));

        var start = lines.FindIndex(l => l.Text.Length > 0);
        if (start < 0) { errors.Add(new(new(file, 1), "empty file; it must start with '# arc: <key>'")); return null; }
        var first = lines[start];
        var kind = KindLine.Match(first.Text);
        if (!kind.Success || first.Text.StartsWith("##")) { Error(first, "the file must start with '# arc: <key>'"); return null; }
        if (kind.Groups[1].Value != "arc") { Error(first, $"'{kind.Groups[1].Value}' files are not supported yet"); return null; }
        var key = kind.Groups[2].Value;
        if (!StoryVocabulary.IsKey(key) || key.Contains('.')) { Error(first, $"bad arc key '{key}'"); return null; }

        // Cut the rest into the header and the ## sections.
        var header = new List<Src>();
        var sections = new List<(Src Head, string Name, string Key, List<Src> Body)>();
        foreach (var l in lines.Skip(start + 1))
        {
            if (l.Text.StartsWith("##"))
            {
                var m = SectionLine.Match(l.Text);
                if (!m.Success) { Error(l, $"bad section line '{l.Text}'"); sections.Add((l, "", "", new())); continue; }
                sections.Add((l, m.Groups[1].Value, m.Groups[2].Value, new()));
            }
            else if (l.Text.StartsWith('#')) Error(l, "only one '# arc:' line per file");
            else if (sections.Count == 0) header.Add(l);
            else sections[^1].Body.Add(l);
        }

        var head = ParseHeader(header, first, At, Error);
        var memories = new List<MemoryDecl>();
        var epithets = new List<EpithetDecl>();
        var keepsakes = new List<KeepsakeDecl>();
        var scenes = new List<StoryScene>();
        var endings = new List<StoryEnding>();
        foreach (var (sh, name, skey, body) in sections)
        {
            if (name == "") continue;
            var keyed = name is "scene" or "ending";
            if (keyed && (!StoryVocabulary.IsKey(skey) || skey.Contains('.'))) { Error(sh, $"'## {name}:' needs a key, got '{skey}'"); continue; }
            if (!keyed && skey != "") { Error(sh, $"'## {name}' takes no key"); continue; }
            switch (name)
            {
                case "memories": ParseMemories(body, memories, At, Error); break;
                case "epithets": ParseEpithets(body, epithets, At, Error); break;
                case "keepsakes": ParseKeepsakes(body, keepsakes, At, Error); break;
                case "scene": if (ParseScene(skey, sh, body, file, errors) is { } sc) scenes.Add(sc); break;
                case "ending": if (ParseEnding(skey, sh, body, file, errors) is { } en) endings.Add(en); break;
                default: Error(sh, $"unknown section '{name}' (memories, epithets, keepsakes, scene, ending)"); break;
            }
        }
        return new StoryArc(key, At(first), head, memories, epithets, keepsakes, scenes, endings);
    }

    public static IReadOnlyList<CastMember> ParseCast(string file, string text, List<StoryError> errors)
    {
        var cast = new List<CastMember>();
        var lines = Read(file, text, errors).Where(l => l.Text.Length > 0).ToList();
        if (lines.Count == 0 || lines[0].Text != "# cast")
        {
            errors.Add(new(new(file, lines.Count > 0 ? lines[0].No : 1), "the cast file must start with '# cast'"));
            return cast;
        }
        foreach (var l in lines.Skip(1))
        {
            var cols = Columns(l.Text);
            if (cols.Length != 3 || cols.Any(c => c.Length == 0))
            {
                errors.Add(new(new(file, l.No), "a cast line has 3 columns: key | given name | byname"));
                continue;
            }
            if (!StoryVocabulary.IsKey(cols[0]) || cols[0].Contains('.')) { errors.Add(new(new(file, l.No), $"bad character key '{cols[0]}'")); continue; }
            cast.Add(new CastMember(cols[0], cols[1], cols[2], new(file, l.No)));
        }
        return cast;
    }

    // Split on '|', except inside a {a:…|…} token, which wordings and titles may carry.
    private static string[] Columns(string line)
    {
        var cols = new List<string>();
        var depth = 0;
        var from = 0;
        for (var i = 0; i < line.Length; i++)
        {
            if (line[i] == '{') depth++;
            else if (line[i] == '}' && depth > 0) depth--;
            else if (line[i] == '|' && depth == 0) { cols.Add(line[from..i].Trim()); from = i + 1; }
        }
        cols.Add(line[from..].Trim());
        return cols.ToArray();
    }

    private static StoryHeader ParseHeader(List<Src> lines, Src arcLine, Func<Src, StoryPos> at, Action<Src, string> error)
    {
        string? title = null, publicLine = null;
        StoryFamily? family = null;
        List<PlynlingStage>? stages = null;
        StoryTrigger? trigger = null;
        TimeSpan? shelf = null;
        int? weight = null;
        PlynlingBond? other = null;
        var bonuses = new List<StoryWeightBonus>();
        var requires = new List<StoryCondition>();
        var seen = new HashSet<string>();
        var errs = new List<StoryError>();

        foreach (var l in lines.Where(l => l.Text.Length > 0))
        {
            var m = FieldLine.Match(l.Text);
            if (!m.Success)
            {
                error(l, l.Text.StartsWith('*') ? "an option before any '## scene:'" : $"expected 'field: value' in the header, got '{l.Text}'");
                continue;
            }
            var (field, value) = (m.Groups[1].Value, m.Groups[2].Value.Trim());
            var isBonus = field == "weight" && value.StartsWith('+');
            if (!isBonus && !seen.Add(field)) { error(l, $"'{field}' given twice"); continue; }
            switch (field)
            {
                case "title": title = NonEmpty(l, field, value, error); break;
                case "public": publicLine = NonEmpty(l, field, value, error); break;
                case "family":
                    if (StoryVocabulary.TryEnum<StoryFamily>(value, out var f)) family = f;
                    else error(l, $"family '{value}': expected one of {StoryVocabulary.Choices<StoryFamily>()}");
                    break;
                case "stage":
                    stages = new List<PlynlingStage>();
                    foreach (var word in value.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
                        if (StoryVocabulary.TryEnum<PlynlingStage>(word, out var st)) stages.Add(st);
                        else error(l, $"stage '{word}': expected one of {StoryVocabulary.Choices<PlynlingStage>()}");
                    if (stages.Count == 0) error(l, "'stage' needs at least one stage");
                    break;
                case "trigger": trigger = ParseTrigger(l, value, error); break;
                case "shelf":
                    shelf = StoryVocabulary.Duration(value);
                    if (shelf is null) error(l, $"shelf '{value}': write a duration such as 7d or 36h");
                    break;
                case "other":
                    if (StoryVocabulary.TryEnum<PlynlingBond>(value, out var b)) other = b;
                    else error(l, $"other '{value}': expected one of {StoryVocabulary.Choices<PlynlingBond>()}");
                    break;
                case "weight":
                {
                    var parts = StoryTags.Split(value);
                    if (parts.Problem is not null) { error(l, parts.Problem); break; }
                    if (!int.TryParse(parts.Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var n))
                    { error(l, $"weight '{parts.Text}' is not a number"); break; }
                    if (!isBonus)
                    {
                        if (parts.Leading.Count + parts.Trailing.Count > 0) error(l, "the base weight takes no condition; add 'weight: +n [if …]' lines");
                        weight = n;
                        break;
                    }
                    var when = new List<StoryCondition>();
                    foreach (var tag in parts.Leading.Concat(parts.Trailing))
                        if (Conditional(tag, at(l), errs) is { } c) when.Add(c);
                    if (when.Count == 0) error(l, "a weight bonus needs at least one [if …]");
                    bonuses.Add(new StoryWeightBonus(n, when));
                    break;
                }
                case "requires":
                {
                    var parts = StoryTags.Split(value);
                    if (parts.Problem is not null || parts.Text.Length > 0) { error(l, "'requires' takes only tags: [stage adult] [not trait shy]"); break; }
                    foreach (var tag in parts.Leading)
                        if (StoryVocabulary.Condition(StoryTags.Words(tag), false, at(l), errs) is { } c) requires.Add(c);
                    break;
                }
                default: error(l, $"unknown header field '{field}'"); break;
            }
        }
        foreach (var e in errs) error(lines.First(l => l.No == e.Pos.Line), e.Message);

        foreach (var (name, value) in new (string, object?)[] { ("title", title), ("family", family), ("stage", stages), ("trigger", trigger), ("public", publicLine) })
            if (value is null && !seen.Contains(name)) error(arcLine, $"missing header field '{name}'");

        return new StoryHeader(title ?? "", family ?? StoryFamily.Milestone, stages ?? new List<PlynlingStage>(),
            trigger ?? new StoryTrigger(TriggerKind.Draw, null), shelf, weight ?? 0, bonuses, requires, other, publicLine ?? "");
    }

    private static string? NonEmpty(Src l, string field, string value, Action<Src, string> error)
    {
        if (value.Length > 0) return value;
        error(l, $"'{field}' is empty");
        return null;
    }

    private static StoryTrigger? ParseTrigger(Src l, string value, Action<Src, string> error)
    {
        var w = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        StoryTrigger? Bad() { error(l, $"trigger '{value}': expected adoption, draw, sick, old_age, stage <stage>, relation <bond>, death <bond> or break <1-3>"); return null; }
        if (w.Length == 1)
            return w[0] switch
            {
                "adoption" => new(TriggerKind.Adoption, null),
                "draw" => new(TriggerKind.Draw, null),
                "sick" => new(TriggerKind.Sick, null),
                "old_age" => new(TriggerKind.OldAge, null),
                _ => Bad(),
            };
        if (w.Length != 2) return Bad();
        return w[0] switch
        {
            "stage" when StoryVocabulary.TryEnum<PlynlingStage>(w[1], out _) => new(TriggerKind.Stage, w[1]),
            "relation" when StoryVocabulary.TryEnum<PlynlingBond>(w[1], out _) => new(TriggerKind.Relation, w[1]),
            "death" when StoryVocabulary.TryEnum<PlynlingBond>(w[1], out _) => new(TriggerKind.Death, w[1]),
            "break" when w[1] is "1" or "2" or "3" => new(TriggerKind.Break, w[1]),
            _ => Bad(),
        };
    }

    // An [if …] or [unless …] tag as a condition; anything else is an error.
    private static StoryCondition? Conditional(string tag, StoryPos pos, List<StoryError> errors)
    {
        var words = StoryTags.Words(tag);
        if (words.Count > 0 && words[0].Text is "if" or "unless")
            return StoryVocabulary.Condition(words.Skip(1).ToList(), words[0].Text == "unless", pos, errors);
        errors.Add(new(pos, $"[{tag}]: expected [if …] or [unless …] here"));
        return null;
    }

    private static void ParseMemories(List<Src> body, List<MemoryDecl> into, Func<Src, StoryPos> at, Action<Src, string> error)
    {
        foreach (var l in body.Where(l => l.Text.Length > 0))
        {
            var c = Columns(l.Text);
            if (c.Length != 5) { error(l, "a memory line has 5 columns: key | holder | private or public | ±n | wording"); continue; }
            if (!StoryVocabulary.IsKey(c[0])) { error(l, $"bad memory key '{c[0]}'"); continue; }
            if (!StoryVocabulary.IsKey(c[1]) || c[1].Contains('.')) { error(l, $"bad holder '{c[1]}'"); continue; }
            if (c[2] is not ("private" or "public")) { error(l, $"'{c[2]}': write private or public"); continue; }
            if (!int.TryParse(c[3], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var shift)) { error(l, $"standing shift '{c[3]}' is not a number"); continue; }
            if (c[4].Length == 0) { error(l, "the memory's wording is empty"); continue; }
            into.Add(new MemoryDecl(c[0], c[1], c[2] == "public", shift, c[4], at(l)));
        }
    }

    private static void ParseEpithets(List<Src> body, List<EpithetDecl> into, Func<Src, StoryPos> at, Action<Src, string> error)
    {
        foreach (var l in body.Where(l => l.Text.Length > 0))
        {
            var c = Columns(l.Text);
            if (c.Length != 2 || c[1].Length == 0) { error(l, "an epithet line has 2 columns: key | {a:masc|fem} title"); continue; }
            if (!StoryVocabulary.IsKey(c[0])) { error(l, $"bad epithet key '{c[0]}'"); continue; }
            into.Add(new EpithetDecl(c[0], c[1], at(l)));
        }
    }

    private static void ParseKeepsakes(List<Src> body, List<KeepsakeDecl> into, Func<Src, StoryPos> at, Action<Src, string> error)
    {
        foreach (var l in body.Where(l => l.Text.Length > 0))
        {
            var c = Columns(l.Text);
            if (c.Length is not (3 or 4) || c[1].Length == 0 || c[2].Length == 0) { error(l, "a keepsake line has 3 columns: key | name | story (and optionally | hurts)"); continue; }
            if (c.Length == 4 && c[3] != "hurts") { error(l, $"'{c[3]}': the 4th column can only be 'hurts'"); continue; }
            if (!StoryVocabulary.IsKey(c[0])) { error(l, $"bad keepsake key '{c[0]}'"); continue; }
            into.Add(new KeepsakeDecl(c[0], c[1], c[2], c.Length == 4, at(l)));
        }
    }
}
