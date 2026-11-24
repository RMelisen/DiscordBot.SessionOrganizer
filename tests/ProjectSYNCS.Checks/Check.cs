using System.Collections;
using System.Reflection;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

// What the check files share: the failure count, and the bot's internals reached by reflection (the
// bot needs no InternalsVisibleTo). The files moved out of Program.cs read these through
// `using static Check;`; the older ones take `Fail` as a parameter.
public static class Check
{
    public static int Failures;
    public static void Fail(string m) { Failures++; Console.WriteLine("FAIL " + m); }

    public static readonly Assembly asm = typeof(ChatFacts).Assembly;
    public static readonly Type br = asm.GetType("ProjectSYNCS.Services.BotResponses")!;
    public static readonly Type cues = asm.GetType("ProjectSYNCS.Services.MessageCues")!;
    public static readonly Type intentType = asm.GetType("ProjectSYNCS.Services.ChatIntent")!;
    public static readonly MethodInfo readIntent = cues.GetMethod("ReadIntent", BindingFlags.Public | BindingFlags.Static)!;
    public static readonly MethodInfo readTopic = cues.GetMethod("ReadRequestTopic", BindingFlags.Public | BindingFlags.Static, new[] { typeof(string) })!;
    public static readonly MethodInfo readTopicAny = cues.GetMethod("ReadTopic", BindingFlags.Public | BindingFlags.Static)!;
    public static readonly MethodInfo addresses = cues.GetMethod("AddressesHer", BindingFlags.Public | BindingFlags.Static)!;
    public static readonly MethodInfo analyze = cues.GetMethod("Analyze", BindingFlags.Public | BindingFlags.Static)!;
    public static readonly MethodInfo analyzeClaim = analyze;

    // The longest name a report can write out: 32 characters (Discord's cap), every one escaped by Sanitize.
    public static string? LongestName(ulong id) => new string('*', 32);

    public static object Field(string n) => br.GetField(n, BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;

    public static IEnumerable<(string Name, string[] Lines)> Pools(string field)
    {
        var v = Field(field);
        if (v is string[] arr) yield return (field, arr);
        else if (v is TimedPool<string> t)
        {
            // Each part alone, and each joined pool as she picks it: rules on size and duplicates
            // hold for what she actually picks from.
            yield return ($"{field}.AnyTime", t.AnyTime);
            yield return ($"{field}.Day", t.Day);
            yield return ($"{field}.Evening", t.Evening);
            yield return ($"{field}@day", t.At(AppTime.AtWallClock(new DateTime(2026, 6, 1, 12, 0, 0))));
            yield return ($"{field}@evening", t.At(AppTime.AtWallClock(new DateTime(2026, 6, 1, 21, 0, 0))));
        }
        else if (v is IDictionary d)
            foreach (DictionaryEntry e in d) yield return ($"{field}[{e.Key}]", (string[])e.Value!);
    }

    // A dictionary pool has a line set for every value of the enum, but the ones excepted.
    public static void Covers(string field, Type enumType, params string[] except)
    {
        var d = (IDictionary)Field(field);
        foreach (var v in Enum.GetValues(enumType))
            if (!except.Contains(v.ToString()) && !d.Contains(v)) Fail($"{field} misses {v}");
    }

    public static void ShipIs(string text, string? a, string? b)
    {
        var p = ShipAsk.Read(text);
        if (p?.A != a || p?.B != b) Fail($"ShipAsk.Read(\"{text}\") = ({p?.A}, {p?.B}), expected ({a}, {b})");
    }

    // The mood MessageCues.Analyze reads in a message.
    public static string Mood(string s) { var m = analyze.Invoke(null, new object[] { s })!; return m.GetType().GetProperty("Emotion")!.GetValue(m)!.ToString()!; }
}
