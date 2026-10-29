using System.Globalization;
using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

// Scenes, options and endings. Indentation is ignored: structure comes from '*' (an option), '?'
// (a roll's outcome) and '##' (a section). Inside an option, tag-only lines before its prose are its
// attributes; after it, they are its effects and its [-> next].
public static partial class StoryParser
{
    private static readonly Regex RetiredLine = new(@"^retired\s*->\s*(\S+)$", RegexOptions.Compiled);
    private static readonly Regex OptionLine = new(@"^\*\s*([^|]*?)\s*\|\s*(.*)$", RegexOptions.Compiled);
    private static readonly Regex HoursPace = new(@"^\+(\d+)h$", RegexOptions.Compiled);

    // "publik: …", "Pace: now", a bare "handvoer": prose never starts like that. French prose may
    // start « Silence : … » (a space before the colon, a capital), which stays prose.
    private static readonly Regex LooksLikeField = new(@"^([a-z_]+\s*:|[A-Za-z_]+:|[a-z_-]+$|retired\s*->)", RegexOptions.Compiled);

    private static readonly HashSet<string> AttributeWords = new() { "needs", "roll", "lean", "owner-only", "secret", "public", "retired" };

    // "[stress rowdy 10]" is a cost (an attribute); "[stress 10]" is an effect.
    private static bool IsAttribute(IReadOnlyList<StoryWord> words) =>
        words.Count > 0 && (AttributeWords.Contains(words[0].Text) || (words[0].Text == "stress" && words.Count == 3));

    private static StoryScene? ParseScene(string key, Src head, List<Src> body, string file, List<StoryError> errors)
    {
        StoryPos At(Src l) => new(file, l.No);
        void Error(Src l, string message) => errors.Add(new StoryError(At(l), message));

        string? title = null, publicLine = null, retired = null;
        StoryPace? pace = null;
        bool bump = false, handover = false;
        var seen = new HashSet<string>();
        var i = 0;
        for (; i < body.Count; i++)
        {
            var t = body[i].Text;
            if (t.Length == 0) { if (title is null && pace is null && retired is null) continue; break; }
            var rm = RetiredLine.Match(t);
            var fm = FieldLine.Match(t);
            var field = t is "bump" or "handover" ? t : rm.Success ? "retired" : fm.Success ? fm.Groups[1].Value : null;
            if (field is not ("bump" or "handover" or "retired" or "title" or "pace" or "public")) break;
            if (!seen.Add(field)) { Error(body[i], $"'{field}' given twice"); continue; }
            if (t == "bump") { bump = true; continue; }
            if (t == "handover") { handover = true; continue; }
            if (rm.Success) { retired = rm.Groups[1].Value; continue; }
            var value = fm.Groups[2].Value.Trim();
            switch (fm.Groups[1].Value)
            {
                case "title": title = NonEmpty(body[i], "title", value, Error); break;
                case "public": publicLine = NonEmpty(body[i], "public", value, Error); break;
                case "pace":
                    pace = value switch
                    {
                        "next_day" => new StoryPace(PaceKind.NextDay, 0),
                        "now" => new StoryPace(PaceKind.Now, 0),
                        _ when HoursPace.Match(value) is { Success: true } hm && int.Parse(hm.Groups[1].Value, CultureInfo.InvariantCulture) is >= 1 and <= 48 =>
                            new StoryPace(PaceKind.Hours, int.Parse(hm.Groups[1].Value, CultureInfo.InvariantCulture)),
                        _ => null,
                    };
                    if (pace is null) Error(body[i], $"pace '{value}': write next_day, now, or +Nh with N from 1 to 48");
                    break;
            }
        }
        var rest = body.Skip(i).ToList();

        if (retired is not null)
        {
            foreach (var l in rest.Where(l => l.Text.Length > 0)) Error(l, "a retired scene holds only its 'retired -> <key>' line");
            return new StoryScene(key, title ?? "", pace ?? new StoryPace(PaceKind.NextDay, 0), publicLine, bump, handover, retired,
                Array.Empty<StoryParagraph>(), Array.Empty<StoryOption>(), null, At(head));
        }
        if (title is null) Error(head, $"scene '{key}' has no 'title:'");
        if (pace is null) Error(head, $"scene '{key}' has no 'pace:'");

        // The scene's own prose runs until the first option.
        var firstOption = rest.FindIndex(l => l.Text.StartsWith('*'));
        var proseLines = firstOption < 0 ? rest : rest.Take(firstOption).ToList();
        var (prose, effects, next) = ParseBlock(proseLines, file, errors, allowNext: true);
        StoryOutcome? narration = null;
        var options = new List<StoryOption>();
        if (firstOption < 0)
        {
            if (next is null) Error(head, $"scene '{key}' has no options and no final [-> …]");
            else narration = new StoryOutcome(Array.Empty<StoryParagraph>(), effects, next, At(proseLines.Last(l => l.Text.Length > 0)));
        }
        else
        {
            if (effects.Count > 0 || next is not null)
                Error(rest[firstOption], "effects and [-> …] belong to an option or an ending, not to the scene's text");
            var chunks = new List<List<Src>>();
            foreach (var l in rest.Skip(firstOption))
                if (l.Text.StartsWith('*')) chunks.Add(new List<Src> { l });
                else chunks[^1].Add(l);
            foreach (var chunk in chunks)
                if (ParseOption(chunk, file, errors) is { } o) options.Add(o);
        }
        return new StoryScene(key, title ?? "", pace ?? new StoryPace(PaceKind.NextDay, 0), publicLine, bump, handover, null,
            prose, options, narration, At(head));
    }

    private static StoryOption? ParseOption(List<Src> chunk, string file, List<StoryError> errors)
    {
        StoryPos At(Src l) => new(file, l.No);
        void Error(Src l, string message) => errors.Add(new StoryError(At(l), message));

        var head = chunk[0];
        var parts = StoryTags.Split(head.Text);
        if (parts.Problem is not null) { Error(head, parts.Problem); return null; }
        var m = OptionLine.Match(parts.Text);
        if (!m.Success || parts.Leading.Count > 0) { Error(head, "an option line is '* <key> | <label>', with its tags after the label"); return null; }
        var key = m.Groups[1].Value;
        var label = m.Groups[2].Value.Trim();
        if (!StoryVocabulary.IsKey(key) || key.Contains('.')) { Error(head, $"bad option key '{key}'"); return null; }
        if (label.Length == 0) Error(head, $"option '{key}' has no label");

        var needs = new List<StoryCondition>();
        string? lockText = null, publicLine = null;
        StoryRoll? roll = null;
        var lean = new Dictionary<AiAxis, int>();
        var stress = new Dictionary<string, int>();
        bool ownerOnly = false, secret = false, retired = false;

        void Attribute(Src l, string tag)
        {
            var w = StoryTags.Words(tag);
            var t = w.Select(x => x.Text).ToList();
            void Bad(string message) => Error(l, $"[{tag}]: {message}");
            switch (t[0])
            {
                case "needs":
                {
                    var condWords = w.Skip(1).ToList();
                    if (condWords.Count > 0 && condWords[^1].Quoted) { lockText = condWords[^1].Text; condWords.RemoveAt(condWords.Count - 1); }
                    if (StoryVocabulary.Condition(condWords, false, At(l), errors) is { } c) needs.Add(c);
                    break;
                }
                case "roll":
                    if (roll is not null) Bad("one roll per option");
                    else if (t.Count == 3 && StoryVocabulary.TryEnum<PlynlingStat>(t[1], out var stat)
                             && int.TryParse(t[2], NumberStyles.None, CultureInfo.InvariantCulture, out var diff))
                        roll = new StoryRoll(stat, diff);
                    else Bad($"write [roll <stat> <difficulty>], stat one of {StoryVocabulary.Choices<PlynlingStat>()}");
                    break;
                case "lean":
                    if (t.Count == 3 && StoryVocabulary.TryEnum<AiAxis>(t[1], out var axis)
                        && int.TryParse(t[2], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var n))
                    {
                        if (!lean.TryAdd(axis, n)) Bad($"'{t[1]}' leaned twice");
                    }
                    else Bad($"write [lean <axis> ±n], axis one of {StoryVocabulary.Choices<AiAxis>()}");
                    break;
                case "stress":
                    if (StoryVocabulary.IsKey(t[1]) && int.TryParse(t[2], NumberStyles.None, CultureInfo.InvariantCulture, out var cost))
                    {
                        if (!stress.TryAdd(t[1], cost)) Bad($"'{t[1]}' costs twice");
                    }
                    else Bad("write [stress <trait> n]");
                    break;
                case "public":
                    if (w.Count == 2 && w[1].Quoted && w[1].Text.Length > 0) publicLine = w[1].Text;
                    else Bad("write [public \"…\"]");
                    break;
                case "owner-only" when t.Count == 1: ownerOnly = true; break;
                case "secret" when t.Count == 1: secret = true; break;
                case "retired" when t.Count == 1: retired = true; break;
                default: Bad("takes no words after it"); break;
            }
        }

        foreach (var tag in parts.Trailing)
        {
            if (IsAttribute(StoryTags.Words(tag))) Attribute(head, tag);
            else Error(head, $"[{tag}]: only attributes go on the option line (needs, roll, lean, stress, owner-only, secret, public, retired)");
        }

        // Attribute lines, then the outcome: plain, or split by '? success' / '? failure'.
        var i = 1;
        for (; i < chunk.Count; i++)
        {
            var l = chunk[i];
            if (l.Text.Length == 0) continue;
            var p = StoryTags.Split(l.Text);
            if (p.Problem is not null || p.Text.Length > 0 || p.Leading.Count == 0 || !IsAttribute(StoryTags.Words(p.Leading[0]))) break;
            foreach (var tag in p.Leading)
            {
                if (IsAttribute(StoryTags.Words(tag))) Attribute(l, tag);
                else Error(l, $"[{tag}]: an attribute line holds only attributes");
            }
        }
        var rest = chunk.Skip(i).ToList();
        var success = rest.FindIndex(l => l.Text == "? success");
        var failure = rest.FindIndex(l => l.Text == "? failure");
        foreach (var l in rest.Where(l => l.Text.StartsWith('?') && l.Text is not ("? success" or "? failure")))
            Error(l, $"'{l.Text}': write '? success' or '? failure'");

        StoryOutcome? Outcome(List<Src> lines, Src fallback)
        {
            var (prose, effects, next) = ParseBlock(lines, file, errors, allowNext: true);
            var start = lines.FirstOrDefault(l => l.Text.Length > 0) ?? fallback;
            if (next is null && !retired) Error(start, $"option '{key}': this outcome has no [-> …]");
            return new StoryOutcome(prose, effects, next, At(start));
        }

        StoryOutcome? plain = null, onSuccess = null, onFailure = null;
        if (success < 0 && failure < 0)
        {
            if (roll is not null) Error(head, $"option '{key}' has a roll: it needs '? success' and '? failure'");
            if (rest.Any(l => l.Text.Length > 0) || !retired) plain = Outcome(rest, head);
        }
        else
        {
            if (roll is null) Error(head, $"option '{key}' has '? success' or '? failure' but no [roll …]");
            if (success < 0 || failure < 0) Error(head, $"option '{key}' needs both '? success' and '? failure'");
            var cut = new[] { success, failure }.Where(x => x >= 0).Min();
            if (rest.Take(cut).Any(l => l.Text.Length > 0))
                Error(rest.Take(cut).First(l => l.Text.Length > 0), $"option '{key}': with a roll, all its text goes under '? success' and '? failure'");
            List<Src> Part(int from) => rest.Skip(from + 1).TakeWhile(l => l.Text is not ("? success" or "? failure")).ToList();
            if (success >= 0) onSuccess = Outcome(Part(success), rest[success]);
            if (failure >= 0) onFailure = Outcome(Part(failure), rest[failure]);
        }
        return new StoryOption(key, label, needs, lockText, secret, roll, lean, stress, ownerOnly, retired, publicLine,
            plain, onSuccess, onFailure, At(head));
    }

    private static StoryEnding? ParseEnding(string key, Src head, List<Src> body, string file, List<StoryError> errors)
    {
        string? title = null;
        var i = 0;
        for (; i < body.Count; i++)
        {
            if (body[i].Text.Length == 0) { if (title is null) continue; break; }
            var fm = FieldLine.Match(body[i].Text);
            if (!fm.Success || fm.Groups[1].Value != "title") break;
            title = NonEmpty(body[i], "title", fm.Groups[2].Value.Trim(), (l, m) => errors.Add(new(new(file, l.No), m)));
        }
        if (title is null) errors.Add(new(new(file, head.No), $"ending '{key}' has no 'title:'"));
        var (prose, marks, next) = ParseBlock(body.Skip(i).ToList(), file, errors, allowNext: false);
        return new StoryEnding(key, title ?? "", prose, marks, new(file, head.No));
    }

    // Prose then tag-only lines: paragraphs (a blank line closes one), effects, and the [-> …].
    private static (List<StoryParagraph> Prose, List<StoryEffect> Effects, string? Next) ParseBlock(List<Src> lines, string file,
        List<StoryError> errors, bool allowNext)
    {
        var paragraphs = new List<StoryParagraph>();
        var current = new List<StoryLine>();
        var effects = new List<StoryEffect>();
        string? next = null;
        var inEffects = false;
        void Close() { if (current.Count > 0) { paragraphs.Add(new StoryParagraph(current)); current = new List<StoryLine>(); } }

        foreach (var l in lines)
        {
            var at = new StoryPos(file, l.No);
            void Error(string message) => errors.Add(new StoryError(at, message));
            if (l.Text.Length == 0) { Close(); continue; }
            var p = StoryTags.Split(l.Text);
            if (p.Problem is not null) { Error(p.Problem); continue; }
            if (p.Text.Length == 0)
            {
                inEffects = true;
                Close();
                foreach (var tag in p.Leading)
                {
                    var w = StoryTags.Words(tag);
                    if (w.Count > 0 && w[0].Text == "->")
                    {
                        if (!allowNext) Error($"[{tag}]: an ending leads nowhere");
                        else if (w.Count != 2 || !StoryVocabulary.IsKey(w[1].Text) || w[1].Text.Contains('.')) Error($"[{tag}]: write [-> <scene or ending>]");
                        else if (next is not null) Error($"[{tag}]: one [-> …] per outcome");
                        else next = w[1].Text;
                    }
                    else if (IsAttribute(w)) Error($"[{tag}]: an option's attributes go before its text");
                    else if (StoryVocabulary.Effect(w, at, errors) is { } e) effects.Add(e);
                }
                continue;
            }
            if (inEffects) { Error("prose after the effects; the effects and [-> …] come last"); continue; }
            // A misspelt or misplaced field would otherwise reach players as prose.
            if (p.Leading.Count == 0 && LooksLikeField.IsMatch(p.Text))
            {
                Error($"'{p.Text}' looks like a scene or ending field; fields go right after the '##' line, before any blank line or text");
                continue;
            }
            if (p.Trailing.Count > 0) { Error("tags at the end of a prose line; put conditions first ([if …]) and effects on their own line"); continue; }
            var when = new List<StoryCondition>();
            foreach (var tag in p.Leading)
                if (Conditional(tag, at, errors) is { } c) when.Add(c);
            current.Add(new StoryLine(p.Text, when, at));
        }
        Close();
        return (paragraphs, effects, next);
    }
}
