using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// The event catalog. Keys (events and options) are stored: **never rename one**; append new events
/// at the end. Situations are inspired by Crusader Kings III and rewritten for the village — see
/// docs/plynling-writing-style.md. No « il »/« elle » in event text: the harness bans them, because
/// one text serves every gender pair.
/// </summary>
public static partial class PlynlingEvents
{
    private static readonly PlynlingStage[] Baby = { PlynlingStage.Baby };
    private static readonly PlynlingStage[] Teen = { PlynlingStage.Teen };
    private static readonly PlynlingStage[] Grown = { PlynlingStage.Adult, PlynlingStage.Elder };
    private static readonly PlynlingStage[] Elder = { PlynlingStage.Elder };
    private static readonly PlynlingStage[] AnyStage = Enum.GetValues<PlynlingStage>();

    private static readonly IReadOnlyList<EventEffect> Nothing = Array.Empty<EventEffect>();
    private static IReadOnlyList<EventEffect> E(params EventEffect[] effects) => effects;
    private static Dictionary<string, int> Stress(params (string Trait, int Amount)[] costs) => costs.ToDictionary(c => c.Trait, c => c.Amount);
    private static Dictionary<AiAxis, int> Ai(params (AiAxis Axis, int W)[] w) => w.ToDictionary(x => x.Axis, x => x.W);
    private static readonly Dictionary<string, int> NoStress = new();

    // A plain option (no challenge): the same outcome whatever happens.
    private static EventOption Plain(string key, string label, string outcome, IReadOnlyList<EventEffect> effects,
        Dictionary<AiAxis, int> ai, Dictionary<string, int>? stress = null, EventGate? gate = null) =>
        new(key, label, outcome, null, gate, null, effects, Nothing, stress ?? NoStress, ai);

    private static EventOption Try(string key, string label, EventChallenge challenge, string success, string failure,
        IReadOnlyList<EventEffect> onSuccess, IReadOnlyList<EventEffect> onFailure, Dictionary<AiAxis, int> ai,
        Dictionary<string, int>? stress = null, EventGate? gate = null) =>
        new(key, label, success, failure, gate, challenge, onSuccess, onFailure, stress ?? NoStress, ai);

    // An option that asks the other Plynling's owner (AskTarget). Owner-only unless said otherwise:
    // deciding alone never risks a refusal.
    private static EventOption Ask(string key, string label, string outcome, string responseKey, Dictionary<AiAxis, int> ai,
        bool ownerOnly = true) =>
        new(key, label, outcome, null, null, null, E(new AskTarget(responseKey)), Nothing, NoStress, ai, OwnerOnly: ownerOnly);

    // A response's answer: Stance leans it when the responder decides alone.
    private static EventOption Answer(string key, string label, string outcome, Stance stance, IReadOnlyList<EventEffect> effects,
        Dictionary<AiAxis, int> ai) =>
        new(key, label, outcome, null, null, null, effects, Nothing, NoStress, ai, Stance: stance);

    // The catalog, one method per file in PlynlingEvents/, concatenated in catalog order. Each part is a
    // method rather than a field: static field order across partial files is undefined.
    public static readonly IReadOnlyList<EventDef> All = new[]
    {
        Basics(), Breaks(), Moments(), Wave1(), Wave2(), Wave3(),
        Wave4(), Wave5Young(), Wave5Grown(), Wave7Young(), Wave7Grown(), Wave8(),
        Wave8Stories(), Wave8Echoes(), Wave8Batch2Young(), Wave8Batch2Grown(), Wave8Batch2Late(), ShortBaby(),
        ShortTeen(), ShortGrown(), ShortElder(),
    }.SelectMany(part => part).ToArray();

    private static readonly Dictionary<string, EventDef> ByKeyMap = All.ToDictionary(e => e.Key);

    public static EventDef? ByKey(string key) => ByKeyMap.GetValueOrDefault(key);

    // Event text, expanded: names in bold, agreements resolved, {T} the new trait(s) in bold. Names must
    // already be safe (PlynlingCardUi.SafeName). Without a target, {B} never appears in the text. {T} is
    // replaced first, so a trait name is never read as a template.
    public static string Expand(string template, string a, PlynlingGender ga, string? b = null, PlynlingGender gb = PlynlingGender.Male,
        string? traits = null) =>
        PlynlingVisitStory.Expand(template.Replace("{T}", traits is null ? "" : $"**{traits}**"), a, ga, b ?? "", gb);
}
