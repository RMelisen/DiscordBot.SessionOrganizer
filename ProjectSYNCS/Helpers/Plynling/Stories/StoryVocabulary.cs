using System.Globalization;
using System.Text.RegularExpressions;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// The words of a story tag, read into conditions and effects. Only the shape is checked here (the
/// word count, numbers, enum words); whether a key exists is <see cref="StoryValidator"/>'s job. Every
/// refusal adds one error naming the tag and returns null.
/// </summary>
public static class StoryVocabulary
{
    private static readonly Regex KeyShape = new(@"^[a-z0-9_]+(\.[a-z0-9_]+)?$", RegexOptions.Compiled);
    private static readonly Regex DurationShape = new(@"^(\d+)([dh])$", RegexOptions.Compiled);

    // Enum words are the C# names lowercased: "teen", "bestfriends", "learning". Never a number.
    public static bool TryEnum<T>(string word, out T value) where T : struct, Enum
    {
        foreach (var v in Enum.GetValues<T>())
            if (v.ToString().ToLowerInvariant() == word) { value = v; return true; }
        value = default;
        return false;
    }

    public static string Choices<T>() where T : struct, Enum =>
        string.Join(", ", Enum.GetNames<T>().Where(n => n != "None").Select(n => n.ToLowerInvariant()));

    public static TimeSpan? Duration(string word)
    {
        var m = DurationShape.Match(word);
        if (!m.Success) return null;
        var n = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        return m.Groups[2].Value == "d" ? TimeSpan.FromDays(n) : TimeSpan.FromHours(n);
    }

    public static bool IsKey(string word) => KeyShape.IsMatch(word);

    public static StoryCondition? Condition(IReadOnlyList<StoryWord> words, bool negated, StoryPos pos, List<StoryError> errors)
    {
        var w = words.Select(x => x.Text).ToList();
        var said = string.Join(' ', w);
        StoryCondition? Fail(string message) { errors.Add(new(pos, $"[{said}]: {message}")); return null; }
        if (w.Count > 0 && w[0] == "not") { negated = !negated; w.RemoveAt(0); }
        if (w.Count == 0) return Fail("empty condition");
        if (w[0] == "other")
        {
            if (w.Count != 3 || w[1] != "trait") return Fail("'other' expects 'other trait <key>'");
            return IsKey(w[2]) ? new(ConditionKind.OtherTrait, w[2], negated, CompareOp.None, 0, pos) : Fail($"bad key '{w[2]}'");
        }
        StoryCondition Make(ConditionKind kind, string key) => new(kind, key, negated, CompareOp.None, 0, pos);
        switch (w[0])
        {
            case "stat":
            case "standing":
            {
                if (w.Count != 4 || (w[2] != ">=" && w[2] != "<") || !int.TryParse(w[3], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var n))
                    return Fail($"'{w[0]}' expects '{w[0]} <{(w[0] == "stat" ? "stat" : "character")}> >= n' or '< n'");
                if (w[0] == "stat" && !TryEnum<PlynlingStat>(w[1], out _)) return Fail($"'stat' expects one of: {Choices<PlynlingStat>()}");
                if (w[0] == "standing" && !IsKey(w[1])) return Fail($"bad key '{w[1]}'");
                return new(w[0] == "stat" ? ConditionKind.Stat : ConditionKind.Standing, w[1], negated,
                    w[2] == ">=" ? CompareOp.AtLeast : CompareOp.Below, n, pos);
            }
        }
        if (w.Count != 2) return Fail($"'{w[0]}' expects one word after it");
        var arg = w[1];
        switch (w[0])
        {
            case "trait": return IsKey(arg) ? Make(ConditionKind.Trait, arg) : Fail($"bad key '{arg}'");
            case "stage": return TryEnum<PlynlingStage>(arg, out _) ? Make(ConditionKind.Stage, arg) : Fail($"'stage' expects one of: {Choices<PlynlingStage>()}");
            case "relation": return TryEnum<PlynlingBond>(arg, out _) ? Make(ConditionKind.Relation, arg) : Fail($"'relation' expects one of: {Choices<PlynlingBond>()}");
            case "season":
                return TryEnum<Season>(arg, out var s) && s != Season.None ? Make(ConditionKind.Season, arg) : Fail($"'season' expects one of: {Choices<Season>()}");
            case "passion": return TryEnum<PlynlingPassion>(arg, out _) ? Make(ConditionKind.Passion, arg) : Fail($"'passion' expects one of: {Choices<PlynlingPassion>()}");
            case "gender": return arg is "m" or "f" ? Make(ConditionKind.Gender, arg) : Fail("'gender' expects m or f");
            case "flag": return IsKey(arg) ? Make(ConditionKind.Flag, arg) : Fail($"bad key '{arg}'");
            case "memory": return IsKey(arg) ? Make(ConditionKind.Memory, arg) : Fail($"bad key '{arg}'");
            case "keepsake": return IsKey(arg) ? Make(ConditionKind.Keepsake, arg) : Fail($"bad key '{arg}'");
            case "epithet": return IsKey(arg) ? Make(ConditionKind.Epithet, arg) : Fail($"bad key '{arg}'");
            case "chain": return IsKey(arg) ? Make(ConditionKind.Chain, arg) : Fail($"bad key '{arg}'");
            case "ended": return IsKey(arg) && arg.Contains('.') ? Make(ConditionKind.Ended, arg) : Fail("'ended' expects '<arc>.<ending>'");
            default: return Fail($"unknown condition '{w[0]}'");
        }
    }

    public static StoryEffect? Effect(IReadOnlyList<StoryWord> words, StoryPos pos, List<StoryError> errors)
    {
        var w = words.Select(x => x.Text).ToList();
        var said = string.Join(' ', w);
        StoryEffect? Fail(string message) { errors.Add(new(pos, $"[{said}]: {message}")); return null; }
        if (w.Count == 0) return Fail("empty effect");
        var other = false;
        if (w[0] == "other")
        {
            if (w.Count < 2 || w[1] != "remember") return Fail("'other' only goes with 'remember'");
            other = true;
            w.RemoveAt(0);
        }
        StoryEffect Make(EffectKind kind, string key, double amount = 0, string? to = null) => new(kind, key, to, amount, other, pos);

        switch (w[0])
        {
            case "trait":
                if (w.Count == 2) return IsKey(w[1]) ? Make(EffectKind.Trait, w[1]) : Fail($"bad key '{w[1]}'");
                if (w.Count == 4 && w[2] == "->") return IsKey(w[1]) && IsKey(w[3]) ? Make(EffectKind.Trait, w[1], to: w[3]) : Fail("bad trait key");
                return Fail("'trait' expects 'trait <key>' or 'trait <old> -> <new>'");
            case "lift":
                if (w.Count != 3) return Fail("'lift' expects 'lift <need> <amount>'");
                if (!TryEnum<Need>(w[1], out _)) return Fail($"'lift' expects one of: {Choices<Need>()}");
                return double.TryParse(w[2], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var x)
                    ? Make(EffectKind.Lift, w[1], x) : Fail($"bad amount '{w[2]}' (write 0.3)");
        }
        if (w.Count != 2) return Fail($"'{w[0]}' expects one word after it");
        var arg = w[1];
        StoryEffect? Keyed(EffectKind kind) => IsKey(arg) ? Make(kind, arg) : Fail($"bad key '{arg}'");
        StoryEffect? Number(EffectKind kind) =>
            int.TryParse(arg, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var n) ? Make(kind, "", n) : Fail($"bad number '{arg}'");
        return w[0] switch
        {
            "flag" => Keyed(EffectKind.Flag),
            "unflag" => Keyed(EffectKind.Unflag),
            "remember" => Keyed(EffectKind.Remember),
            "forget" => Keyed(EffectKind.Forget),
            "reveal" => Keyed(EffectKind.Reveal),
            "modifier" => Keyed(EffectKind.Modifier),
            "item" => Keyed(EffectKind.Item),
            "epithet" => Keyed(EffectKind.Epithet),
            "keepsake" => Keyed(EffectKind.Keepsake),
            "unlock" => Keyed(EffectKind.Unlock),
            "stress" => Number(EffectKind.Stress),
            "affinity" => Number(EffectKind.Affinity),
            "cailloux" => Number(EffectKind.Cailloux),
            "grow" => TryEnum<PlynlingStat>(arg, out _) ? Make(EffectKind.Grow, arg) : Fail($"'grow' expects one of: {Choices<PlynlingStat>()}"),
            _ => Fail($"unknown effect '{w[0]}'"),
        };
    }
}
