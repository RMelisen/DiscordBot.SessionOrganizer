# Plynling events — Phase 2 (event engine) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** About once a day, something happens to each Plynling: a short scene with 2–3 choices (some gated by a trait or a stat, some a challenge with a visible chance), played privately from « ✨ Événement » on its card, decided by the Plynling itself in character after 24 h, and told publicly as a paged story.

**Architecture:** Event definitions are C# data (`Helpers/PlynlingEvents`) over small catalog types (`Helpers/PlynlingEventCatalog`); every rule — pulse time, which event, gates, chances, the in-character choice, the 4th-trait weighting — is pure in `Helpers/PlynlingEventEngine`, rolled through `StableRoll` from ids so a restart or a second reader never changes an outcome. Instances live in one table (`PlynlingEventInstance`: pending and history). All database work sits in a new partial file of `PlynlingService` (`PlynlingService.Events.cs`), so an event's effects, its relation change and its journal moment share the one context and land in one save. Stories are **rebuilt from the stored instance** on every page turn.

**Tech Stack:** C# / .NET 10, EF Core SQLite, Discord.Net 3.20 (Components V2, ephemeral), the scratch harness from phase 1.

**Spec:** `docs/superpowers/specs/2026-10-06-plynling-events-design.md` — Phase 2 and cross-cutting. **Depends on phase 1** (`docs/superpowers/plans/2026-10-06-plynling-personality-phase1.md`) being implemented: `TraitInfo`, `PlynlingTraits.Draw`, `PlynlingStats`, `PlynlingService.GetTraitsAsync` / `EnsureTraitsAsync`, `StableRoll`, `BuildCard(…, traits:)`, `PlynlingPersonalityHandler`. If phase 1 landed with different names, update this plan's code first.

## Global Constraints

- **Never commit or push.** Each task ends by listing the files to commit.
- **Hard rule:** deciding alone only ever picks an option that is **visible (gate met) and has no stress cost for any trait the Plynling holds**; every event has **at least one ungated option with no stress cost at all** (harness invariant).
- **Pacing:** one pulse per Paris day at a time hashed between **08:00 and 20:00**; a pulse that finds **3 pulse events pending is skipped** (the day still counts). Pending events expire after **24 h**. No pulse and no expiry while frozen; a death cancels what is pending. The mascot decides at once and never has anything pending.
- **Challenge:** `chance = clamp(50 + 5 × (stat − difficulty), 5, 95)` %, the shown percentage is the rolled one, roll hashed from the instance id.
- **Deciding alone:** weight per eligible option `max(1, 100 + Σ_axis optionAi[axis] × axis[axis] / 10)`, hashed draw.
- **4th trait:** weight `max(0.25, 1 + leaning · traitAxes / 400) + 0.5 × (ado growth in stats the trait raises)`; uniform when there is no ado history.
- **Effects in this phase:** stat growth, affinity with the event's target (through the same rules as visits), a journal moment. **No cailloux, no items.** Stress costs are **stored in the catalog** and used for deciding alone, but **not shown** until phase 3 (a stress cost with no stress gauge would confuse players — a deliberate deviation from the spec's "displayed from phase 2").
- **Append-only:** event keys, option keys (unique within an event), `JournalKind` (new value at the end). `EventType`, `TargetKind`, `EventPickOutcome` are not stored; new values still at the end.
- Custom-ids: `plyn:events:{plynlingId}`, `plev:pick:{instanceId}:{optionKey}`, `evs:prev|next|first|last:{instanceId}:{page}`, `plyn:help:{page}`. Every button on a message has its own id.
- Every handler that touches the database **defers first**; handlers live in `Interactions/Components/` with `ignoreGroupNames: true`, never on `PlynlingModule`.
- Stories go to `PlynlingAnnouncer.GameChannelId` for its guild; **a choice made in another guild (the dev guild) posts its story in the channel where it was made**; a sweep resolution in another guild is logged and not posted. `AllowedMentions.None` everywhere; names through `PlynlingCardUi.SafeName`.
- Event text: `{A}`/`{B}` names, `{a:m|f}`/`{b:m|f}` agreements (expanded by `PlynlingVisitStory.Expand`); **no « il », « elle », « ils », « elles »** in event text (the harness bans them in every expansion) — write around them. Text follows `docs/plynling-writing-style.md`.
- Build with `dotnet build -warnaserror`.

`$REPO`, `$SCRATCH` as in phase 1; the harness is `$SCRATCH/personality/` (extended, not replaced).

## Review Focus

1. **Two readers, one outcome:** the sweep deciding an expired event while the owner clicks it — exactly one resolution lands; the loser is told « déjà décidé ». (Task 4: the click re-checks `ResolvedAt` inside its unit of work and the sweep re-reads; the dev-guild test races them by hand.)
2. **A target that vanished** (abandoned, dead, frozen) between creation and resolution: the event still resolves, the affinity effect is skipped, the story names nobody missing. (Task 4 code path; Task 5 harness builds a story with a null target.)
3. **An option gated by a trait the Plynling lost or never had** is never offered, never picked alone, and a forged click on it is refused. (Task 1 harness; Task 4 `NotAvailable`.)
4. **A restart between the pick and the story post**: the instance is resolved and its story can still be paged from any old card, because pages are rebuilt from the database. (Task 5 harness rebuilds a story from data only.)
5. **An event key or option key removed from the catalog** while instances exist: pending ones are cancelled, resolved ones still render a plain story without crashing. (Task 4 cancel path; Task 5 harness renders an unknown key.)

---

### Task 1: Catalog types and the pure engine

**Files:**
- Create: `ProjectSYNCS/Helpers/PlynlingEventCatalog.cs`
- Create: `ProjectSYNCS/Helpers/PlynlingEventEngine.cs`
- Modify: `ProjectSYNCS/Helpers/PlynlingStats.cs` (`AddGrowth`)
- Modify: `ProjectSYNCS/Helpers/PlynlingTraits.cs` (`Draw` takes an optional adult weighting)
- Test: `$SCRATCH/personality/Program.cs`

**Interfaces:**
- Consumes: `TraitInfo`, `AiAxis`, `PlynlingStat`, `StatLine`, `PlynlingStats.Compute`, `StableRoll.Unit`, `AppTime.Zone`, `PlynlingTraits.Draw`.
- Produces:
  - `enum EventType { Pulse, Triggered, FollowUp, Response }`, `enum TargetKind { None, Known, Hostile, Anyone }`
  - `abstract record EventGate` with `TraitGate(string TraitKey)`, `StatGate(PlynlingStat Stat, int AtLeast)`
  - `sealed record EventChallenge(PlynlingStat Stat, int Difficulty)`
  - `abstract record EventEffect` with `GrowStat(PlynlingStat Stat, int Amount = 1)`, `AffinityShift(int Delta)`
  - `sealed record EventOption(string Key, string Label, string Outcome, string? FailOutcome, EventGate? Gate, EventChallenge? Challenge, IReadOnlyList<EventEffect> OnSuccess, IReadOnlyList<EventEffect> OnFailure, IReadOnlyDictionary<string, int> StressCosts, IReadOnlyDictionary<AiAxis, int> Ai)`
  - `sealed record EventDef(string Key, EventType Type, IReadOnlyList<PlynlingStage> Stages, string Title, string Scene, IReadOnlyList<EventOption> Options, int Weight = 100, TargetKind Target = TargetKind.None, Func<EventContext, bool>? Condition = null, IReadOnlyDictionary<string, double>? WeightByTrait = null)`
  - `sealed record EventContext(Plynling Self, IReadOnlyList<TraitInfo> Traits, IReadOnlyList<StatLine> Stats, PlynlingStage Stage)` with `bool Has(string traitKey)`, `int Stat(PlynlingStat)`
  - `PlynlingEventEngine`: `MaxPendingPulses = 3`, `Lifetime = 24 h`, `RecentWindow = 14`, `PulseAt(int plynlingId, int dayKey) -> DateTimeOffset`, `Eligible(EventDef, EventContext) -> bool`, `PickPulse(int plynlingId, int dayKey, IEnumerable<EventDef>, EventContext, IReadOnlyCollection<string> recent, IReadOnlySet<TargetKind> targetable) -> EventDef?`, `Visible(EventOption, EventContext) -> bool`, `StressCost(EventOption, EventContext) -> int`, `Chance(EventChallenge, EventContext) -> int`, `Succeeds(int instanceId, int chance) -> bool`, `AloneOptions(EventDef, EventContext) -> IReadOnlyList<EventOption>`, `DecideAlone(EventDef, EventContext, int instanceId) -> EventOption`, `PickTarget(int plynlingId, int dayKey, IReadOnlyList<int> candidateIds) -> int?`, `AdoHistory(IEnumerable<(EventOption Option, bool Success)>) -> (IReadOnlyDictionary<AiAxis,int> Leaning, IReadOnlyDictionary<PlynlingStat,int> Growth)`, `AdultTraitWeight(TraitInfo, IReadOnlyDictionary<AiAxis,int>, IReadOnlyDictionary<PlynlingStat,int>) -> double`
  - `PlynlingStats.AddGrowth(Plynling, PlynlingStat, int)`
  - `PlynlingTraits.Draw(int, PlynlingStage, IReadOnlyCollection<string>, Func<TraitInfo, double>? adultWeight = null)`

- [ ] **Step 1: Add the failing checks**

In `$SCRATCH/personality/Program.cs`, insert before the final `Console.WriteLine`:

```csharp
// ==== PHASE 2 — engine ==============================================================================
EventContext Ctx(Plynling p, params string[] traitKeys)
{
    var ts = traitKeys.Select(k => PlynlingTraits.ByKey(k)!).ToList();
    return new EventContext(p, ts, PlynlingStats.Compute(p, ts), PlynlingStage.Adult);
}
EventOption Opt(string key, EventGate? gate = null, EventChallenge? ch = null, Dictionary<string, int>? stress = null, Dictionary<AiAxis, int>? ai = null) =>
    new(key, key, "ok", ch is null ? null : "raté", gate, ch, Array.Empty<EventEffect>(), Array.Empty<EventEffect>(),
        stress ?? new(), ai ?? new());

// pulse time: 08:00–20:00 Paris, stable
for (var id = 1; id <= 300; id++)
{
    var at = PlynlingEventEngine.PulseAt(id, 20261006);
    var local = AppTime.ToZoned(at);
    Check(local.Date == new DateTime(2026, 10, 6) && local.Hour >= 8 && local.Hour < 20, $"pulse {id} between 08:00 and 20:00 Paris");
    Check(at == PlynlingEventEngine.PulseAt(id, 20261006), $"pulse {id} is stable");
}

// gates, chance, stress cost
var pe = Fresh(41);
var gateBrave = Opt("g", gate: new TraitGate("brave"));
Check(!PlynlingEventEngine.Visible(gateBrave, Ctx(pe)) && PlynlingEventEngine.Visible(gateBrave, Ctx(pe, "brave")), "a trait gate shows only with the trait");
var cou = PlynlingStats.Compute(pe, Array.Empty<TraitInfo>()).Single(l => l.Stat == PlynlingStat.Courage).Total;
Check(PlynlingEventEngine.Visible(Opt("s", gate: new StatGate(PlynlingStat.Courage, cou)), Ctx(pe))
      && !PlynlingEventEngine.Visible(Opt("s", gate: new StatGate(PlynlingStat.Courage, cou + 1)), Ctx(pe)), "a stat gate is « at least »");
Check(PlynlingEventEngine.Chance(new EventChallenge(PlynlingStat.Courage, cou), Ctx(pe)) == 50, "stat = difficulty: 50 %");
Check(PlynlingEventEngine.Chance(new EventChallenge(PlynlingStat.Courage, cou + 2), Ctx(pe)) == 40, "5 points per stat point");
Check(PlynlingEventEngine.Chance(new EventChallenge(PlynlingStat.Courage, cou + 50), Ctx(pe)) == 5
      && PlynlingEventEngine.Chance(new EventChallenge(PlynlingStat.Courage, cou - 50), Ctx(pe)) == 95, "clamped to 5–95");
var costly = Opt("c", stress: new() { ["brave"] = 20, ["honest"] = 30 });
Check(PlynlingEventEngine.StressCost(costly, Ctx(pe, "honest")) == 30 && PlynlingEventEngine.StressCost(costly, Ctx(pe)) == 0, "stress cost counts held traits only");
var hits = Enumerable.Range(1, 2000).Count(i => PlynlingEventEngine.Succeeds(i, 65));
Check(hits is > 1200 and < 1400, $"a 65 % roll succeeds about 65 % of the time ({hits}/2000)");
Check(PlynlingEventEngine.Succeeds(7, 65) == PlynlingEventEngine.Succeeds(7, 65), "rolls are stable");

// deciding alone: never a hidden or stress-costing option
var def = new EventDef("t", EventType.Pulse, new[] { PlynlingStage.Adult }, "T", "S", new[]
{
    Opt("free"),
    Opt("gated", gate: new TraitGate("brave"), ai: new() { [AiAxis.Boldness] = 5 }),
    Opt("costly", stress: new() { ["calm"] = 20 }, ai: new() { [AiAxis.Rationality] = 5 }),
    Opt("bold", ai: new() { [AiAxis.Boldness] = 3 }),
});
for (var i = 1; i <= 500; i++)
{
    var choice = PlynlingEventEngine.DecideAlone(def, Ctx(pe, "calm"), i);
    Check(choice.Key is "free" or "bold", $"alone {i}: never gated-out or stress-costing ({choice.Key})");
}
var boldPicks = Enumerable.Range(1, 1000).Count(i => PlynlingEventEngine.DecideAlone(def, Ctx(pe, "calm", "brave"), i).Key is "bold" or "gated");
Check(boldPicks > 600, $"a brave Plynling leans to bold options ({boldPicks}/1000)");

// picking the pulse
var cheap = new EventDef("a", EventType.Pulse, new[] { PlynlingStage.Adult }, "A", "S", new[] { Opt("x") });
var other = new EventDef("b", EventType.Pulse, new[] { PlynlingStage.Adult }, "B", "S", new[] { Opt("x") });
var babyOnly = new EventDef("c", EventType.Pulse, new[] { PlynlingStage.Baby }, "C", "S", new[] { Opt("x") });
var social = new EventDef("d", EventType.Pulse, new[] { PlynlingStage.Adult }, "D", "S", new[] { Opt("x") }, Target: TargetKind.Known);
var none = new HashSet<TargetKind>();
for (var day = 20261001; day <= 20261028; day++)
{
    var picked = PlynlingEventEngine.PickPulse(41, day, new[] { cheap, other, babyOnly, social }, Ctx(pe), Array.Empty<string>(), none);
    Check(picked is not null && picked.Key is "a" or "b", $"day {day}: right stage, no social without a target");
    Check(PlynlingEventEngine.PickPulse(41, day, new[] { cheap, other }, Ctx(pe), new[] { "a" }, none)!.Key == "b", $"day {day}: a recent event is skipped");
}
Check(PlynlingEventEngine.PickPulse(41, 20261001, new[] { cheap }, Ctx(pe), new[] { "a" }, none) is null, "nothing eligible: no event");
Check(PlynlingEventEngine.PickTarget(41, 20261006, Array.Empty<int>()) is null, "no candidate, no target");
Check(PlynlingEventEngine.PickTarget(41, 20261006, new[] { 3, 9 }) is 3 or 9, "a target among the candidates");

// the 4th trait
var leaning = new Dictionary<AiAxis, int> { [AiAxis.Boldness] = 6 };
var noGrowth = new Dictionary<PlynlingStat, int>();
Check(PlynlingEventEngine.AdultTraitWeight(PlynlingTraits.ByKey("brave")!, leaning, noGrowth)
      > PlynlingEventEngine.AdultTraitWeight(PlynlingTraits.ByKey("craven")!, leaning, noGrowth), "a bold youth makes Courageux likelier");
Check(PlynlingEventEngine.AdultTraitWeight(PlynlingTraits.ByKey("craven")!, leaning, noGrowth) >= 0.25, "never below 0.25");
Check(PlynlingEventEngine.AdultTraitWeight(PlynlingTraits.ByKey("diligent")!, new Dictionary<AiAxis, int>(), new Dictionary<PlynlingStat, int> { [PlynlingStat.Learning] = 2 }) == 2.0,
    "growth in a raised stat adds 0.5 per point");
var (lean, grow) = PlynlingEventEngine.AdoHistory(new[]
{
    (new EventOption("o", "o", "", null, null, null, new EventEffect[] { new GrowStat(PlynlingStat.Courage) }, Array.Empty<EventEffect>(), new Dictionary<string, int>(), new Dictionary<AiAxis, int> { [AiAxis.Boldness] = 2 }), true),
    (new EventOption("p", "p", "", null, null, null, new EventEffect[] { new GrowStat(PlynlingStat.Courage) }, Array.Empty<EventEffect>(), new Dictionary<string, int>(), new Dictionary<AiAxis, int> { [AiAxis.Boldness] = 1 }), false),
});
Check(lean[AiAxis.Boldness] == 3 && grow.GetValueOrDefault(PlynlingStat.Courage) == 1, "ado history: leaning from every choice, growth only from what applied");
for (var id = 1; id <= 300; id++)
{
    var uniform = PlynlingTraits.Draw(id, PlynlingStage.Adult, Array.Empty<string>());
    var weighted = PlynlingTraits.Draw(id, PlynlingStage.Adult, Array.Empty<string>(), t => 1.0);
    Check(uniform.Take(3).Select(t => t.Key).SequenceEqual(weighted.Take(3).Select(t => t.Key)), $"{id}: weighting touches only the 4th trait");
    var bold = PlynlingTraits.Draw(id, PlynlingStage.Adult, Array.Empty<string>(), t => t.Key == "brave" ? 1000 : 0.25);
    Check(bold[3].Key == "brave" || bold.Take(3).Any(t => t.Group == "bravery"), $"{id}: a huge weight wins the 4th slot");
}

// growth
var g = Fresh(42);
PlynlingStats.AddGrowth(g, PlynlingStat.Learning, 2);
Check(g.GrowthLearning == 2 && PlynlingStats.Growth(g, PlynlingStat.Learning) == 2, "AddGrowth");
```

- [ ] **Step 2: Run it to see it fail**

Run: `cd "$SCRATCH/personality" && dotnet run`
Expected: build errors — `EventContext`, `EventOption`, `PlynlingEventEngine`, … do not exist.

- [ ] **Step 3: Create the catalog types**

`ProjectSYNCS/Helpers/PlynlingEventCatalog.cs`:

```csharp
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Not stored (the instance stores the event's key), but new values go at the end.
public enum EventType { Pulse, Triggered, FollowUp, Response }

// Who a social event involves. Known: a Plynling it has met and is not hostile with. Hostile:
// rivals or enemies. Anyone: any living Plynling of the guild.
public enum TargetKind { None, Known, Hostile, Anyone }

// What an option needs to be offered at all. A hidden option is never shown and never picked alone.
public abstract record EventGate;
public sealed record TraitGate(string TraitKey) : EventGate;
public sealed record StatGate(PlynlingStat Stat, int AtLeast) : EventGate;

public sealed record EventChallenge(PlynlingStat Stat, int Difficulty);

// What an option does. This phase: stats and affinity; stress, modifiers, follow-ups and responses
// are appended by later phases. Target-side effects are affinity only (anti-griefing).
public abstract record EventEffect;
public sealed record GrowStat(PlynlingStat Stat, int Amount = 1) : EventEffect;
public sealed record AffinityShift(int Delta) : EventEffect;

/// <summary>
/// One choice. <see cref="Key"/> is stored and never renamed. <see cref="Outcome"/> is told on
/// success (or always, without a challenge); <see cref="FailOutcome"/> on failure. Stress costs are
/// by trait key; an option with any cost for a trait the Plynling holds is never picked alone.
/// <see cref="Ai"/> leans the in-character choice (small numbers, about −3…+3).
/// </summary>
public sealed record EventOption(
    string Key, string Label, string Outcome, string? FailOutcome,
    EventGate? Gate, EventChallenge? Challenge,
    IReadOnlyList<EventEffect> OnSuccess, IReadOnlyList<EventEffect> OnFailure,
    IReadOnlyDictionary<string, int> StressCosts, IReadOnlyDictionary<AiAxis, int> Ai);

/// <summary>
/// One event. <see cref="Key"/> is stored and never renamed. Text is templated: {A}/{B} names,
/// {a:m|f}/{b:m|f} agreements (PlynlingVisitStory.Expand). Weight 100 is ordinary;
/// <see cref="WeightByTrait"/> multiplies it for a held trait.
/// </summary>
public sealed record EventDef(
    string Key, EventType Type, IReadOnlyList<PlynlingStage> Stages, string Title, string Scene,
    IReadOnlyList<EventOption> Options, int Weight = 100, TargetKind Target = TargetKind.None,
    Func<EventContext, bool>? Condition = null, IReadOnlyDictionary<string, double>? WeightByTrait = null);

// What the rules read about the Plynling an event happens to.
public sealed record EventContext(Plynling Self, IReadOnlyList<TraitInfo> Traits, IReadOnlyList<StatLine> Stats, PlynlingStage Stage)
{
    public bool Has(string traitKey) => Traits.Any(t => t.Key == traitKey);
    public int Stat(PlynlingStat stat) => Stats.Single(l => l.Stat == stat).Total;
}
```

- [ ] **Step 4: Create the engine**

`ProjectSYNCS/Helpers/PlynlingEventEngine.cs`:

```csharp
namespace ProjectSYNCS.Helpers;

/// <summary>
/// Every event rule, pure. Rolls are hashed (<see cref="StableRoll"/>) from the Plynling, the day or
/// the instance — never a <see cref="Random"/> — so the sweep and a click always agree, and a story
/// rebuilt later tells the same thing.
/// </summary>
public static class PlynlingEventEngine
{
    public const int MaxPendingPulses = 3;
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);
    public const int RecentWindow = 14;            // events not drawn again within the last 14 resolved

    private const int PulseTimeSalt = 300, PulsePickSalt = 301, TargetSalt = 302, ChallengeSalt = 310, AloneSalt = 311;
    private const int PulseFromMinute = 8 * 60, PulseSpanMinutes = 12 * 60;

    // Today's pulse: a minute between 08:00 and 20:00 Paris, hashed from the Plynling and the day.
    public static DateTimeOffset PulseAt(int plynlingId, int dayKey)
    {
        var minute = PulseFromMinute + (int)(StableRoll.Unit(plynlingId, dayKey, PulseTimeSalt) * PulseSpanMinutes);
        var wall = new DateTime(dayKey / 10000, dayKey / 100 % 100, dayKey % 100).AddMinutes(minute);
        return new DateTimeOffset(wall, AppTime.Zone.GetUtcOffset(wall));
    }

    public static bool Eligible(EventDef def, EventContext ctx) =>
        def.Stages.Contains(ctx.Stage) && (def.Condition?.Invoke(ctx) ?? true);

    private static double WeightOf(EventDef def, EventContext ctx)
    {
        double w = def.Weight;
        if (def.WeightByTrait is { } byTrait)
            foreach (var (trait, factor) in byTrait)
                if (ctx.Has(trait)) w *= factor;
        return Math.Max(0, w);
    }

    // The day's event among the pulse events it is eligible for, not seen recently, and — for a
    // social one — only when a target of that kind exists.
    public static EventDef? PickPulse(int plynlingId, int dayKey, IEnumerable<EventDef> defs, EventContext ctx,
        IReadOnlyCollection<string> recent, IReadOnlySet<TargetKind> targetable)
    {
        var pool = defs
            .Where(d => d.Type == EventType.Pulse && Eligible(d, ctx) && !recent.Contains(d.Key))
            .Where(d => d.Target == TargetKind.None || targetable.Contains(d.Target))
            .Select(d => (Def: d, W: WeightOf(d, ctx)))
            .Where(x => x.W > 0)
            .ToList();
        return pool.Count == 0 ? null : Weighted(pool, StableRoll.Unit(plynlingId, dayKey, PulsePickSalt));
    }

    public static int? PickTarget(int plynlingId, int dayKey, IReadOnlyList<int> candidateIds)
    {
        if (candidateIds.Count == 0) return null;
        var sorted = candidateIds.OrderBy(i => i).ToList();
        return sorted[Math.Min(sorted.Count - 1, (int)(StableRoll.Unit(plynlingId, dayKey, TargetSalt) * sorted.Count))];
    }

    public static bool Visible(EventOption option, EventContext ctx) => option.Gate switch
    {
        null => true,
        TraitGate t => ctx.Has(t.TraitKey),
        StatGate s => ctx.Stat(s.Stat) >= s.AtLeast,
        _ => false,
    };

    public static int StressCost(EventOption option, EventContext ctx) =>
        option.StressCosts.Where(kv => ctx.Has(kv.Key)).Sum(kv => kv.Value);

    public static int Chance(EventChallenge challenge, EventContext ctx) =>
        Math.Clamp(50 + 5 * (ctx.Stat(challenge.Stat) - challenge.Difficulty), 5, 95);

    public static bool Succeeds(int instanceId, int chance) =>
        StableRoll.Unit(instanceId, ChallengeSalt, 0) * 100 < chance;

    // What it may pick on its own: shown to it, and costing it no stress. Never empty for a catalog
    // event — the harness checks every event has an ungated option with no stress cost at all.
    public static IReadOnlyList<EventOption> AloneOptions(EventDef def, EventContext ctx) =>
        def.Options.Where(o => Visible(o, ctx) && StressCost(o, ctx) == 0).ToList();

    public static EventOption DecideAlone(EventDef def, EventContext ctx, int instanceId)
    {
        var axes = PlynlingPersonality.Axes(ctx.Traits);
        var pool = AloneOptions(def, ctx)
            .Select(o => (Def: o, W: Math.Max(1.0, 100 + o.Ai.Sum(kv => kv.Value * axes[kv.Key] / 10.0))))
            .ToList();
        return Weighted(pool, StableRoll.Unit(instanceId, AloneSalt, 0));
    }

    // What its ado years leaned toward: every choice's AI weights, and the growth that actually applied.
    public static (IReadOnlyDictionary<AiAxis, int> Leaning, IReadOnlyDictionary<PlynlingStat, int> Growth) AdoHistory(
        IEnumerable<(EventOption Option, bool Success)> choices)
    {
        var leaning = new Dictionary<AiAxis, int>();
        var growth = new Dictionary<PlynlingStat, int>();
        foreach (var (option, success) in choices)
        {
            foreach (var (axis, v) in option.Ai) leaning[axis] = leaning.GetValueOrDefault(axis) + v;
            foreach (var g in (success ? option.OnSuccess : option.OnFailure).OfType<GrowStat>())
                growth[g.Stat] = growth.GetValueOrDefault(g.Stat) + g.Amount;
        }
        return (leaning, growth);
    }

    public static double AdultTraitWeight(TraitInfo trait, IReadOnlyDictionary<AiAxis, int> leaning, IReadOnlyDictionary<PlynlingStat, int> growth)
    {
        var dot = trait.Axes.Sum(kv => kv.Value * leaning.GetValueOrDefault(kv.Key));
        var fromGrowth = trait.Stats.Where(kv => kv.Value > 0).Sum(kv => 0.5 * growth.GetValueOrDefault(kv.Key));
        return Math.Max(0.25, 1 + dot / 400.0) + fromGrowth;
    }

    private static T Weighted<T>(IReadOnlyList<(T Def, double W)> pool, double roll)
    {
        var target = roll * pool.Sum(x => x.W);
        foreach (var (item, w) in pool)
        {
            if (target < w) return item;
            target -= w;
        }
        return pool[^1].Def;
    }
}
```

- [ ] **Step 5: `AddGrowth`**

In `ProjectSYNCS/Helpers/PlynlingStats.cs`, after `Growth(...)`:

```csharp
    public static void AddGrowth(Plynling p, PlynlingStat stat, int amount)
    {
        switch (stat)
        {
            case PlynlingStat.Diplomacy: p.GrowthDiplomacy += amount; break;
            case PlynlingStat.Stewardship: p.GrowthStewardship += amount; break;
            case PlynlingStat.Learning: p.GrowthLearning += amount; break;
            case PlynlingStat.Intrigue: p.GrowthIntrigue += amount; break;
            case PlynlingStat.Courage: p.GrowthCourage += amount; break;
            default: throw new ArgumentOutOfRangeException(nameof(stat), stat, null);
        }
    }
```

- [ ] **Step 6: Weighted 4th trait in `Draw`**

In `ProjectSYNCS/Helpers/PlynlingTraits.cs`, change the `Draw` signature to

```csharp
    public static IReadOnlyList<TraitInfo> Draw(int plynlingId, PlynlingStage stage, IReadOnlyCollection<string> held,
        Func<TraitInfo, double>? adultWeight = null)
```

and inside the loop replace

```csharp
            drawn.Add(Pick(Personality.Where(t => !taken.Contains(t.Group)).ToArray(), plynlingId, PersonalitySalt + slot));
```

with

```csharp
            var pool = Personality.Where(t => !taken.Contains(t.Group)).ToArray();
            // The adulte trait (slot 2) leans toward what its ado years were like, when that is known.
            drawn.Add(slot == 2 && adultWeight is not null
                ? PickWeighted(pool, adultWeight, plynlingId, PersonalitySalt + slot)
                : Pick(pool, plynlingId, PersonalitySalt + slot));
```

and add after `Pick`:

```csharp
    private static TraitInfo PickWeighted(TraitInfo[] pool, Func<TraitInfo, double> weight, int plynlingId, int salt)
    {
        var weights = pool.Select(t => Math.Max(0, weight(t))).ToArray();
        var target = StableRoll.Unit(plynlingId, salt, 0) * weights.Sum();
        for (var i = 0; i < pool.Length; i++)
        {
            if (target < weights[i]) return pool[i];
            target -= weights[i];
        }
        return pool[^1];
    }
```

Update the `Draw` doc comment's last sentence to: « The adulte slot is uniform unless <paramref name="adultWeight"/> is given (the ado years, from the event history). »

- [ ] **Step 7: Run the harness**

Run: `cd "$SCRATCH/personality" && dotnet run`
Expected: `OK`

- [ ] **Step 8: Build**

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror`
Expected: `Build succeeded`, 0 warnings.

- [ ] **Step 9: Files for the owner to commit**

`ProjectSYNCS/Helpers/PlynlingEventCatalog.cs`, `ProjectSYNCS/Helpers/PlynlingEventEngine.cs`, `ProjectSYNCS/Helpers/PlynlingStats.cs`, `ProjectSYNCS/Helpers/PlynlingTraits.cs` — `Plynling events: catalog types and engine`.

---

### Task 2: The starter events

Six events, covering every mechanic once (trait gate, stat gate, challenge, success/failure, stress costs, a social target). **The rest of the spec's first batch (to 15–20) is written in the phase 5 content plan**, through writing sheets the owner reviews — content is not engine work.

**Files:**
- Create: `ProjectSYNCS/Helpers/PlynlingEvents.cs`
- Test: `$SCRATCH/personality/Program.cs`

**Interfaces:**
- Consumes: Task 1 types.
- Produces: `PlynlingEvents.All : IReadOnlyList<EventDef>`, `PlynlingEvents.ByKey(string) -> EventDef?`, `PlynlingEvents.Expand(string template, string a, PlynlingGender ga, string? b = null, PlynlingGender gb = PlynlingGender.Male) -> string`.

- [ ] **Step 1: Add the failing catalog checks**

In `$SCRATCH/personality/Program.cs`, insert before the final `Console.WriteLine`:

```csharp
// ==== PHASE 2 — the event catalog ==================================================================
var events = PlynlingEvents.All;
Check(events.Count >= 6, "the starter events exist");
Check(events.Select(e => e.Key).Distinct().Count() == events.Count, "event keys are unique");
var banned = new System.Text.RegularExpressions.Regex(@"\b(il|elle|ils|elles)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
foreach (var e in events)
{
    Check(e.Options.Count is >= 2 and <= 4, $"{e.Key}: 2 to 4 options");
    Check(e.Options.Select(o => o.Key).Distinct().Count() == e.Options.Count, $"{e.Key}: option keys unique");
    Check(e.Options.Any(o => o.Gate is null && o.StressCosts.Count == 0), $"{e.Key}: an ungated option with no stress cost (deciding alone)");
    Check(e.Options.All(o => o.Label.Length <= 80), $"{e.Key}: labels ≤ 80");
    Check(e.Options.All(o => $"plev:pick:{int.MaxValue}:{o.Key}".Length <= 100), $"{e.Key}: custom-ids ≤ 100");
    Check(e.Options.All(o => (o.Challenge is null) == (o.FailOutcome is null)), $"{e.Key}: a fail text exactly when there is a challenge");
    Check(e.Options.All(o => o.StressCosts.Keys.All(k => PlynlingTraits.ByKey(k) is not null)
                             && (o.Gate is not TraitGate tg || PlynlingTraits.ByKey(tg.TraitKey) is not null)), $"{e.Key}: trait keys exist");
    Check(e.Target != TargetKind.None || !e.Options.SelectMany(o => o.OnSuccess.Concat(o.OnFailure)).OfType<AffinityShift>().Any(), $"{e.Key}: no affinity without a target");
    foreach (var (ga, gb) in new[] { (PlynlingGender.Male, PlynlingGender.Male), (PlynlingGender.Male, PlynlingGender.Female), (PlynlingGender.Female, PlynlingGender.Male), (PlynlingGender.Female, PlynlingGender.Female) })
        foreach (var text in new[] { e.Scene }.Concat(e.Options.SelectMany(o => new[] { o.Outcome, o.FailOutcome }.OfType<string>())))
        {
            var x = PlynlingEvents.Expand(text, "Lila", ga, "Pwet", gb);
            Check(!x.Contains('{') && !x.Contains('}'), $"{e.Key}: no template left ({ga}/{gb})");
            Check(!banned.IsMatch(x), $"{e.Key}: no il/elle ({ga}/{gb}): {x}");
        }
}
```

- [ ] **Step 2: Run it to see it fail**

Run: `cd "$SCRATCH/personality" && dotnet run`
Expected: build error — `PlynlingEvents` does not exist.

- [ ] **Step 3: Write the events**

`ProjectSYNCS/Helpers/PlynlingEvents.cs`:

```csharp
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// The event catalog. Keys (events and options) are stored: **never rename one**; append new events
/// at the end. Situations are inspired by Crusader Kings III and rewritten for the village — see
/// docs/plynling-writing-style.md. No « il »/« elle » in event text: the harness bans them, because
/// one text serves every gender pair.
/// </summary>
public static class PlynlingEvents
{
    private static readonly PlynlingStage[] Baby = { PlynlingStage.Baby };
    private static readonly PlynlingStage[] Teen = { PlynlingStage.Teen };
    private static readonly PlynlingStage[] Grown = { PlynlingStage.Adult, PlynlingStage.Elder };

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

    public static readonly IReadOnlyList<EventDef> All = new[]
    {
        // ---- bébé
        new EventDef("baby_puddle", EventType.Pulse, Baby, "La première flaque",
            "{A} découvre une flaque. Une vraie, avec un ciel dedans. {A} se penche, et le ciel se penche aussi.",
            new[]
            {
                Try("jump", "Sauter dedans à pieds joints", new EventChallenge(PlynlingStat.Courage, 4),
                    "Splash. Le ciel éclate en mille morceaux, puis se recolle. {A} recommence onze fois.",
                    "{A} glisse sur le bord et s'assoit dedans. Le ciel, vexé, ne dit rien.",
                    E(new GrowStat(PlynlingStat.Courage)), Nothing,
                    Ai((AiAxis.Boldness, 2), (AiAxis.Energy, 1)), Stress(("craven", 20))),
                Plain("greet", "Saluer son reflet poliment",
                    "{A} fait une petite révérence. Le reflet aussi. C'est le début d'une grande amitié, au moins d'un côté.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 2), (AiAxis.Compassion, 1))),
                Plain("watch", "Attendre de voir si le ciel bouge",
                    "{A} attend une heure. Un nuage traverse la flaque. {A} repart {a:convaincu|convaincue} d'avoir vu le ciel de l'intérieur.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 2)), gate: new TraitGate("pensive")),
            }),

        new EventDef("baby_snail", EventType.Pulse, Baby, "L'escargot du village",
            "L'escargot du village avance vers {A} depuis ce matin. Arrivée prévue vers midi.",
            new[]
            {
                Plain("wait", "L'attendre sagement",
                    "{A} attend. Quand l'escargot arrive enfin, {A} dort. L'escargot repart sans bruit, par délicatesse.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, -1)), Stress(("rowdy", 20))),
                Plain("run", "Courir à sa rencontre",
                    "{A} court, trébuche, roule, et arrive pile devant l'escargot, la tête en bas. « Bonjour », dit l'escargot, qui en a vu d'autres.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Energy, 2), (AiAxis.Boldness, 1))),
                Try("race", "Lui proposer une course", new EventChallenge(PlynlingStat.Intrigue, 4),
                    "Pour gagner, {A} part avant le signal. Victoire d'une bonne longueur. L'escargot réclame une revanche pour la semaine prochaine.",
                    "{A} se fait doubler au dernier virage. Personne ne sait comment.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Nothing, Ai((AiAxis.Greed, 1), (AiAxis.Honor, -1))),
            }),

        // ---- ado
        new EventDef("teen_shortcut", EventType.Pulse, Teen, "Le raccourci interdit",
            "Le hérisson chef de gare a planté un panneau : « Raccourci fermé ». Derrière le panneau, le raccourci a l'air très ouvert.",
            new[]
            {
                Try("sneak", "Passer quand même", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "{A} passe, revient, et personne n'a rien vu. Sauf le héron, qui fait semblant de rien.",
                    "Le hérisson attendait derrière le premier buisson. {A} écope d'un sermon de vingt minutes, avec des schémas.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Nothing,
                    Ai((AiAxis.Boldness, 1), (AiAxis.Honor, -2)), Stress(("honest", 30), ("just", 20))),
                Try("ask", "Demander au hérisson pourquoi", new EventChallenge(PlynlingStat.Diplomacy, 6),
                    "Une famille de grenouilles y fait la sieste. {A} promet de chuchoter et obtient un laissez-passer.",
                    "Le hérisson répond « parce que » et retourne à ses horaires.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Nothing, Ai((AiAxis.Sociability, 1), (AiAxis.Honor, 1))),
                Plain("around", "Faire le grand tour",
                    "Le grand tour prend une heure. {A} y trouve trois glands, un caillou rond et une opinion très arrêtée sur les panneaux.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, -1)), Stress(("impatient", 20))),
            }),

        new EventDef("teen_contest", EventType.Pulse, Teen, "Le concours du moineau",
            "Le moineau organise son concours annuel de la plus belle pomme de pin. Celle de {A} est… unique.",
            new[]
            {
                Try("present", "La présenter fièrement", new EventChallenge(PlynlingStat.Diplomacy, 7),
                    "Le moineau la tourne, la retourne, et invente une catégorie sur mesure : « Pomme de pin avec du caractère ».",
                    "Le moineau note « intéressant » et passe à la suivante. {A} sait ce que veut dire « intéressant ».",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Nothing,
                    Ai((AiAxis.Sociability, 1), (AiAxis.Boldness, 1)), Stress(("shy", 20))),
                Try("swap", "L'échanger discrètement contre une plus jolie", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "Premier prix. {A} range la médaille au fond d'un tiroir et n'en parle jamais.",
                    "La plus jolie appartenait au moineau. Le silence qui suit dure longtemps.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Nothing,
                    Ai((AiAxis.Honor, -2), (AiAxis.Greed, 1)), Stress(("honest", 40), ("just", 20))),
                Plain("polish", "Aider les autres à cirer les leurs",
                    "{A} passe l'après-midi à faire briller les pommes de pin des autres. Personne ne gagne grâce à ça, mais tout le monde brille.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Compassion, 2), (AiAxis.Sociability, 1))),
            }),

        // ---- adulte et ancien
        new EventDef("grown_parcel", EventType.Pulse, Grown, "Le colis égaré",
            "Un colis attend devant la porte de {A}. Pas d'adresse, juste un champignon dessiné un peu de travers. Ça tinte quand on le secoue.",
            new[]
            {
                Try("open", "L'ouvrir", new EventChallenge(PlynlingStat.Courage, 7),
                    "Dedans : une clochette, et un mot. « Pour sonner quand tu as besoin d'aide. » Pas de signature. {A} la garde près de son lit.",
                    "Dedans : une clochette qui sonne toute seule, toute la nuit. {A} la rapporte au marché au matin, les yeux cernés.",
                    E(new GrowStat(PlynlingStat.Courage)), Nothing,
                    Ai((AiAxis.Boldness, 2)), Stress(("craven", 20), ("paranoid", 20))),
                Try("owner", "Chercher son propriétaire dans tout le village", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "Après quatorze portes, la tortue du café reconnaît son dessin, et offre un chocolat chaud pour la peine.",
                    "Personne ne le réclame. {A} rentre {a:fatigué|fatiguée}, le colis sous le bras.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Nothing,
                    Ai((AiAxis.Honor, 2), (AiAxis.Compassion, 1)), Stress(("greedy", 20))),
                Plain("shelf", "Le ranger pour plus tard",
                    "{A} pose le colis sur une étagère, entre le pot de miel et la boîte à biscuits. Le colis y est toujours.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, -1))),
            }),

        new EventDef("grown_scarf", EventType.Pulse, Grown, "L'écharpe prêtée",
            "Voilà trois semaines que {A} porte l'écharpe que {B} lui a prêtée. L'écharpe tient très chaud, et {B} n'a rien redemandé.",
            new[]
            {
                Plain("return", "La rendre, lavée et pliée",
                    "{A} rend l'écharpe pliée en quatre, avec un pot de confiture glissé dedans. {B} fait semblant de ne pas être {b:ému|émue}.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new AffinityShift(15)), Ai((AiAxis.Honor, 2), (AiAxis.Compassion, 1))),
                Plain("keep", "La garder encore un peu",
                    "{A} la garde. {B} la reconnaît de loin et plisse les yeux, sans rien dire. Pour l'instant.",
                    E(new GrowStat(PlynlingStat.Intrigue), new AffinityShift(-10)),
                    Ai((AiAxis.Greed, 2), (AiAxis.Honor, -1)), Stress(("honest", 20), ("generous", 20))),
                Try("share", "Proposer de la partager, un jour chacun", new EventChallenge(PlynlingStat.Diplomacy, 9),
                    "{B} accepte. L'écharpe change de cou chaque matin, et c'est devenu leur petite cérémonie.",
                    "{B} trouve l'idée étrange et reprend son écharpe sur-le-champ.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new AffinityShift(25)), E(new AffinityShift(-5)),
                    Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1)), gate: new StatGate(PlynlingStat.Diplomacy, 8)),
            },
            Target: TargetKind.Known),
    };

    private static readonly Dictionary<string, EventDef> ByKeyMap = All.ToDictionary(e => e.Key);

    public static EventDef? ByKey(string key) => ByKeyMap.GetValueOrDefault(key);

    // Event text, expanded: names in bold, agreements resolved. Names must already be safe
    // (PlynlingCardUi.SafeName). Without a target, {B} never appears in the text.
    public static string Expand(string template, string a, PlynlingGender ga, string? b = null, PlynlingGender gb = PlynlingGender.Male) =>
        PlynlingVisitStory.Expand(template, a, ga, b ?? "", gb);
}
```

- [ ] **Step 4: Run the harness**

Run: `cd "$SCRATCH/personality" && dotnet run`
Expected: `OK`. A banned-word failure prints the expanded text: reword that line (never relax the check).

- [ ] **Step 5: Build**

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror`
Expected: `Build succeeded`, 0 warnings.

- [ ] **Step 6: Files for the owner to commit**

`ProjectSYNCS/Helpers/PlynlingEvents.cs` — `Plynling events: six starter events`.

---

### Task 3: Storage, the migration, and a shared affinity helper

**Files:**
- Create: `ProjectSYNCS/Models/PlynlingEventInstance.cs`
- Modify: `ProjectSYNCS/Models/Plynling.cs` (`LastPulseDay`)
- Modify: `ProjectSYNCS/Data/AppDbContext.cs`
- Create: `ProjectSYNCS/Migrations/<timestamp>_AddPlynlingEvents.cs` (via `dotnet ef`)
- Modify: `ProjectSYNCS/Helpers/PlynlingJournalUi.cs` (`JournalKind.EventStory`)
- Modify: `ProjectSYNCS/Services/PlynlingService.cs` (`partial`; extract `ApplyBondChangeAsync`; add `ShiftAffinityAsync`)

**Interfaces:**
- Produces:
  - `class PlynlingEventInstance { int Id; int PlynlingId; string EventKey; int? TargetPlynlingId; int? ParentInstanceId; DateTimeOffset CreatedAt, AvailableAt, ExpiresAt; DateTimeOffset? ResolvedAt, CancelledAt; string? OptionKey; bool DecidedAlone; bool? ChallengeSucceeded; int? ChancePercent; PlynlingBond? BondBefore, BondAfter; }`
  - `Plynling.LastPulseDay` (int)
  - `AppDbContext.PlynlingEventInstances`
  - `JournalKind.EventStory` (detail = event key)
  - `PlynlingService` is `partial`; `PlynlingService.ShiftAffinityAsync(Plynling a, Plynling b, int delta, DateTimeOffset now) -> Task<(PlynlingBond Before, PlynlingBond After)>` (never saves; never creates or keeps a couple except as `BondFor` already does)

- [ ] **Step 1: The entity**

`ProjectSYNCS/Models/PlynlingEventInstance.cs`:

```csharp
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Models;

// One event that happened (or is waiting) for a Plynling — the queue and the history in one table.
// Pending = neither resolved nor cancelled, and AvailableAt reached. EventKey and OptionKey are
// PlynlingEvents keys: stored, never renamed. Everything a story needs that the catalog cannot give
// back later (the chance shown, the roll, the bond before and after) is stored here, so a story is
// rebuilt from this row alone, after any restart.
public class PlynlingEventInstance
{
    public int Id { get; set; }
    public int PlynlingId { get; set; }
    public string EventKey { get; set; } = string.Empty;

    // The other Plynling of a social event; set null if that one is abandoned (deleted).
    public int? TargetPlynlingId { get; set; }
    // The event this one follows from (follow-ups and responses, phase 4).
    public int? ParentInstanceId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset AvailableAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? OptionKey { get; set; }
    public bool DecidedAlone { get; set; }
    public bool? ChallengeSucceeded { get; set; }
    public int? ChancePercent { get; set; }
    public PlynlingBond? BondBefore { get; set; }
    public PlynlingBond? BondAfter { get; set; }
}
```

Check where `PlynlingBond` lives: `grep -rn "enum PlynlingBond" ProjectSYNCS` — adjust the `using` if it is not `ProjectSYNCS.Helpers`.

- [ ] **Step 2: The pulse day**

In `ProjectSYNCS/Models/Plynling.cs`, after `public int LastGiftDay { get; set; }`:

```csharp

    // The Paris day (AppTime.DayKey) of its last event pulse, drawn or skipped — one a day, stored so a
    // restart cannot pulse twice. 0 = never.
    public int LastPulseDay { get; set; }
```

- [ ] **Step 3: Register the table**

In `AppDbContext.cs`, add `public DbSet<PlynlingEventInstance> PlynlingEventInstances => Set<PlynlingEventInstance>();` after the `PlynlingTraits` DbSet, and after the `PlynlingTrait` entity block:

```csharp
        // Events: queue and history. Cascade with the Plynling it happened to; the target is only a
        // reference, nulled if that Plynling is abandoned.
        modelBuilder.Entity<PlynlingEventInstance>(e =>
        {
            e.HasOne<Plynling>().WithMany().HasForeignKey(x => x.PlynlingId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Plynling>().WithMany().HasForeignKey(x => x.TargetPlynlingId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.PlynlingId, x.ResolvedAt });
            // The sweep deciding an expired event and the owner clicking it can race: the UPDATE only
            // lands while ResolvedAt is still null, so the second save throws and its whole unit of
            // work (growth, relation, journal) rolls back — an event is never applied twice.
            e.Property(x => x.ResolvedAt).IsConcurrencyToken();
        });
```

- [ ] **Step 4: The journal kind**

In `PlynlingJournalUi.cs`, append after `TraitGained,`:

```csharp
    // detail: the event key (Helpers/PlynlingEvents)
    EventStory,
```

and after the `TraitGained` arm in `Line(...)`:

```csharp
        JournalKind.EventStory => PlynlingEvents.ByKey(detail ?? "") is { } evt ? $"📜 {evt.Title}." : "📜 Une petite aventure.",
```

- [ ] **Step 5: Make the service partial and share the bond change**

In `PlynlingService.cs`, change `public class PlynlingService` to `public partial class PlynlingService`.

Then extract the bond-change block of `VisitAsync` into a helper, **without changing behaviour**. Replace in `VisitAsync`:

```csharp
        var evt = BadgeEvent.None;
        if (after != before && BondMoment(before, after) is { } kind)
        {
            await AddMomentAsync(visitor, kind, host.Name, now);
            await AddMomentAsync(host, kind, visitor.Name, now);
            evt = after switch
            {
                PlynlingBond.Friends => BadgeEvent.BecameFriends,
                PlynlingBond.BestFriends => BadgeEvent.BecameBestFriends,
                PlynlingBond.Lovers => BadgeEvent.BecameLovers,
                _ => BadgeEvent.None,
            };
            if (Closeness(after) > Closeness(before)) evt = BadgeEvent.None;     // drifting apart earns nothing
        }
```

with

```csharp
        var evt = await ApplyBondChangeAsync(visitor, host, before, after, now);
```

and add right after `VisitAsync`:

```csharp
    // A change of bond between two Plynlings: both journal it, and getting closer may earn a badge.
    // Shared by visits and events so the two can never word or reward it differently. Never saves.
    private async Task<BadgeEvent> ApplyBondChangeAsync(Plynling a, Plynling b, PlynlingBond before, PlynlingBond after, DateTimeOffset now)
    {
        if (after == before || BondMoment(before, after) is not { } kind) return BadgeEvent.None;
        await AddMomentAsync(a, kind, b.Name, now);
        await AddMomentAsync(b, kind, a.Name, now);
        var evt = after switch
        {
            PlynlingBond.Friends => BadgeEvent.BecameFriends,
            PlynlingBond.BestFriends => BadgeEvent.BecameBestFriends,
            PlynlingBond.Lovers => BadgeEvent.BecameLovers,
            _ => BadgeEvent.None,
        };
        return Closeness(after) > Closeness(before) ? BadgeEvent.None : evt;     // drifting apart earns nothing
    }

    // An event moving two Plynlings' affinity, one-sided: the same relation row, clamp and bands as a
    // visit, but no scene roll and no confession — an event never makes a couple. Never saves.
    public async Task<(PlynlingBond Before, PlynlingBond After)> ShiftAffinityAsync(Plynling a, Plynling b, int delta, DateTimeOffset now)
    {
        var (lo, hi) = a.Id < b.Id ? (a.Id, b.Id) : (b.Id, a.Id);
        var relation = await _db_context.PlynlingRelations.FirstOrDefaultAsync(r => r.PlynlingAId == lo && r.PlynlingBId == hi);
        if (relation is null)
        {
            relation = new PlynlingRelation { PlynlingAId = lo, PlynlingBId = hi, Bond = PlynlingBond.Acquaintances, Since = now };
            _db_context.PlynlingRelations.Add(relation);
        }
        var before = relation.Bond;
        relation.Affinity = Math.Clamp(relation.Affinity + delta, -100, 100);
        var after = PlynlingBonds.BondFor(relation.Affinity, before);
        if (after != before)
        {
            relation.Bond = after;
            relation.Since = now;
        }
        var evt = await ApplyBondChangeAsync(a, b, before, after, now);
        await AwardAsync(a, now, evt);
        await AwardAsync(b, now, evt);
        return (before, after);
    }
```

- [ ] **Step 6: Build and generate the migration**

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror && dotnet ef migrations add AddPlynlingEvents`
Expected: `Build succeeded`, `Done.`

- [ ] **Step 7: Read the migration**

Expected in `Up`, and nothing else: `AddColumn<int>` `LastPulseDay` on `Plynlings` (default 0); `CreateTable` `PlynlingEventInstances` with both foreign keys (cascade on `PlynlingId`, set null on `TargetPlynlingId`); the index on (`PlynlingId`, `ResolvedAt`) and the FK index on `TargetPlynlingId`. No `Sql(...)`.

- [ ] **Step 8: Build and run the harness**

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror` then `cd "$SCRATCH/personality" && dotnet run`
Expected: `Build succeeded`; `OK`.

- [ ] **Step 9: Files for the owner to commit**

`ProjectSYNCS/Models/PlynlingEventInstance.cs`, `ProjectSYNCS/Models/Plynling.cs`, `ProjectSYNCS/Data/AppDbContext.cs`, the three migration files, `ProjectSYNCS/Helpers/PlynlingJournalUi.cs`, `ProjectSYNCS/Services/PlynlingService.cs` — `Plynling events: storage, shared bond change`.

---

### Task 4: The event service (pulse, expiry, picking, stories' data)

**Files:**
- Create: `ProjectSYNCS/Services/PlynlingService.Events.cs`
- Modify: `ProjectSYNCS/Services/PlynlingService.cs` (`EnsureTraitsAsync` weights the adulte slot)

**Interfaces:**
- Consumes: Tasks 1–3; `GetByIdAsync`, `GetTraitsAsync`, `FlushMomentsAsync`, `AddMomentAsync`, `ShiftAffinityAsync` (same class).
- Produces (all on `PlynlingService`):
  - `enum EventPickOutcome { Done, NotOwner, Gone, NotAvailable, Unknown }` and `sealed record EventPick(EventPickOutcome Outcome, int PendingLeft = 0)`
  - `GetEventContextAsync(Plynling p, DateTimeOffset now) -> Task<EventContext>`
  - `GetPendingEventsAsync(Plynling p, DateTimeOffset now) -> Task<List<PlynlingEventInstance>>` (oldest first)
  - `CountPendingEventsAsync(Plynling p, DateTimeOffset now) -> Task<int>`
  - `GetEventInstanceAsync(int instanceId) -> Task<PlynlingEventInstance?>`
  - `PickEventAsync(int instanceId, string optionKey, ulong actorId, DateTimeOffset now) -> Task<EventPick>` (saves)
  - `TickEventsAsync(Plynling p, DateTimeOffset now) -> Task<IReadOnlyList<int>>` — cancels if dead, decides expired ones, pulses; **saves**; returns the instance ids resolved now (to tell)
  - `CreateEventAsync(Plynling p, EventDef def, int? targetId, DateTimeOffset now) -> Task<PlynlingEventInstance>` (saves, so the id exists)
  - `ResolveAloneAsync(Plynling p, PlynlingEventInstance inst, DateTimeOffset now) -> Task` (never saves)

- [ ] **Step 1: Write the partial file**

`ProjectSYNCS/Services/PlynlingService.Events.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

public enum EventPickOutcome { Done, NotOwner, Gone, NotAvailable, Unknown }

public sealed record EventPick(EventPickOutcome Outcome, int PendingLeft = 0);

// Events, in the same class (and so the same AppDbContext) as the rest of the Plynling's state: an
// event's growth, its relation change, its badges and its journal moment land in one save. The rules
// are pure in Helpers/PlynlingEventEngine; this file only loads, applies and stores.
public partial class PlynlingService
{
    public async Task<EventContext> GetEventContextAsync(Plynling p, DateTimeOffset now)
    {
        var traits = await GetTraitsAsync(p);
        return new EventContext(p, traits, PlynlingStats.Compute(p, traits), PlynlingLife.Stage(p, now));
    }

    // Open = neither resolved nor cancelled (pending, or a follow-up not yet available). Dates are
    // compared in memory: SQLite cannot translate DateTimeOffset comparisons.
    private async Task<List<PlynlingEventInstance>> OpenEventsAsync(int plynlingId) =>
        await _db_context.PlynlingEventInstances
            .Where(i => i.PlynlingId == plynlingId && i.ResolvedAt == null && i.CancelledAt == null)
            .OrderBy(i => i.Id)
            .ToListAsync();

    public async Task<List<PlynlingEventInstance>> GetPendingEventsAsync(Plynling p, DateTimeOffset now) =>
        (await OpenEventsAsync(p.Id)).Where(i => i.AvailableAt <= now).ToList();

    public async Task<int> CountPendingEventsAsync(Plynling p, DateTimeOffset now) =>
        p.DiedAt is null ? (await GetPendingEventsAsync(p, now)).Count : 0;

    public Task<PlynlingEventInstance?> GetEventInstanceAsync(int instanceId) =>
        _db_context.PlynlingEventInstances.FirstOrDefaultAsync(i => i.Id == instanceId);

    // The owner's pick. Re-checks everything inside this unit of work: a sweep may have decided it
    // a moment ago, or the option may no longer be shown (a forged or stale click).
    public async Task<EventPick> PickEventAsync(int instanceId, string optionKey, ulong actorId, DateTimeOffset now)
    {
        var inst = await GetEventInstanceAsync(instanceId);
        if (inst is null) return new(EventPickOutcome.Unknown);
        var p = await GetByIdAsync(inst.PlynlingId, now);
        if (p is null) return new(EventPickOutcome.Unknown);
        if (p.OwnerId != actorId) return new(EventPickOutcome.NotOwner);
        if (inst.ResolvedAt is not null || inst.CancelledAt is not null || p.DiedAt is not null || inst.AvailableAt > now)
            return new(EventPickOutcome.Gone);
        if (PlynlingEvents.ByKey(inst.EventKey) is not { } def) return new(EventPickOutcome.Gone);

        var ctx = await GetEventContextAsync(p, now);
        var option = def.Options.FirstOrDefault(o => o.Key == optionKey);
        if (option is null || !PlynlingEventEngine.Visible(option, ctx)) return new(EventPickOutcome.NotAvailable);

        await ApplyEventAsync(p, inst, def, option, ctx, decidedAlone: false, now);
        await FlushMomentsAsync(p);
        try
        {
            await _db_context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(EventPickOutcome.Gone);
        }
        return new(EventPickOutcome.Done, await CountPendingEventsAsync(p, now));
    }

    // The sweep's turn, per Plynling: cancel what a death left pending, decide what expired, then the
    // day's pulse. Saves (an instance needs its id before it can be rolled). Returns the instances
    // resolved now, for the caller to tell after the save.
    public async Task<IReadOnlyList<int>> TickEventsAsync(Plynling p, DateTimeOffset now)
    {
        var told = new List<int>();
        var open = await OpenEventsAsync(p.Id);
        if (p.DiedAt is not null)
        {
            foreach (var inst in open) inst.CancelledAt = now;
            await _db_context.SaveChangesAsync();
            return told;
        }
        if (PlynlingLife.IsFrozen(p)) return told;

        foreach (var inst in open.Where(i => i.AvailableAt <= now && i.ExpiresAt <= now))
        {
            if (PlynlingEvents.ByKey(inst.EventKey) is null) { inst.CancelledAt = now; continue; }
            await ResolveAloneAsync(p, inst, now);
            told.Add(inst.Id);
        }

        var day = AppTime.DayKey(now);
        if (p.LastPulseDay != day && now >= PlynlingEventEngine.PulseAt(p.Id, day))
        {
            p.LastPulseDay = day;    // drawn or skipped, today's pulse is spent
            var pendingPulses = open.Count(i => i.ResolvedAt is null && i.CancelledAt is null && i.AvailableAt <= now
                                                && PlynlingEvents.ByKey(i.EventKey)?.Type == EventType.Pulse);
            if (pendingPulses < PlynlingEventEngine.MaxPendingPulses)
            {
                var ctx = await GetEventContextAsync(p, now);
                var targets = await TargetCandidatesAsync(p, now);
                var recent = await _db_context.PlynlingEventInstances
                    .Where(i => i.PlynlingId == p.Id && i.ResolvedAt != null)
                    .OrderByDescending(i => i.Id).Take(PlynlingEventEngine.RecentWindow)
                    .Select(i => i.EventKey).ToListAsync();
                var targetable = targets.Where(kv => kv.Value.Count > 0).Select(kv => kv.Key).ToHashSet();
                if (PlynlingEventEngine.PickPulse(p.Id, day, PlynlingEvents.All, ctx, recent, targetable) is { } def)
                {
                    var targetId = def.Target == TargetKind.None ? null : PlynlingEventEngine.PickTarget(p.Id, day, targets[def.Target]);
                    var inst = await CreateEventAsync(p, def, targetId, now);
                    if (PlynlingMascot.Is(p))
                    {
                        // She decides at once and never keeps anything waiting.
                        await ResolveAloneAsync(p, inst, now);
                        told.Add(inst.Id);
                    }
                }
            }
        }
        await _db_context.SaveChangesAsync();
        return told;
    }

    // Saves, so the instance has its id (rolls are hashed from it).
    public async Task<PlynlingEventInstance> CreateEventAsync(Plynling p, EventDef def, int? targetId, DateTimeOffset now)
    {
        var inst = new PlynlingEventInstance
        {
            PlynlingId = p.Id, EventKey = def.Key, TargetPlynlingId = targetId,
            CreatedAt = now, AvailableAt = now, ExpiresAt = now + PlynlingEventEngine.Lifetime,
        };
        _db_context.PlynlingEventInstances.Add(inst);
        await _db_context.SaveChangesAsync();
        return inst;
    }

    public async Task ResolveAloneAsync(Plynling p, PlynlingEventInstance inst, DateTimeOffset now)
    {
        var def = PlynlingEvents.ByKey(inst.EventKey)!;
        var ctx = await GetEventContextAsync(p, now);
        await ApplyEventAsync(p, inst, def, PlynlingEventEngine.DecideAlone(def, ctx, inst.Id), ctx, decidedAlone: true, now);
    }

    // The roll, the effects, the journal. Never saves. A target that is gone (abandoned, dead, frozen)
    // simply skips the affinity: the event still happened to this one.
    private async Task ApplyEventAsync(Plynling p, PlynlingEventInstance inst, EventDef def, EventOption option,
        EventContext ctx, bool decidedAlone, DateTimeOffset now)
    {
        // Loaded first: reading a Plynling settles it and may save, which must happen before this
        // unit of work starts changing anything.
        var target = inst.TargetPlynlingId is { } tid ? await GetByIdAsync(tid, now) : null;
        var success = true;
        if (option.Challenge is { } challenge)
        {
            inst.ChancePercent = PlynlingEventEngine.Chance(challenge, ctx);
            success = PlynlingEventEngine.Succeeds(inst.Id, inst.ChancePercent.Value);
            inst.ChallengeSucceeded = success;
        }
        foreach (var effect in success ? option.OnSuccess : option.OnFailure)
        {
            switch (effect)
            {
                case GrowStat g:
                    PlynlingStats.AddGrowth(p, g.Stat, g.Amount);
                    break;
                case AffinityShift a when target is { DiedAt: null, FrozenAt: null }:
                    var (before, after) = await ShiftAffinityAsync(p, target, a.Delta, now);
                    inst.BondBefore ??= before;
                    inst.BondAfter = after;
                    break;
            }
        }
        inst.ResolvedAt = now;
        inst.OptionKey = option.Key;
        inst.DecidedAlone = decidedAlone;
        await AddMomentAsync(p, JournalKind.EventStory, def.Key, now);
    }

    // Who a social event may involve, by kind: living, unfrozen Plynlings of the same guild, not
    // already in a social event with this one in the last 24 h (either direction).
    private async Task<Dictionary<TargetKind, IReadOnlyList<int>>> TargetCandidatesAsync(Plynling p, DateTimeOffset now)
    {
        var others = await _db_context.Plynlings
            .Where(x => x.GuildId == p.GuildId && x.Id != p.Id && x.DiedAt == null && x.FrozenAt == null)
            .Select(x => x.Id).ToListAsync();
        var recentPairs = (await _db_context.PlynlingEventInstances
                .Where(i => (i.PlynlingId == p.Id && i.TargetPlynlingId != null) || i.TargetPlynlingId == p.Id)
                .ToListAsync())
            .Where(i => now - i.CreatedAt < TimeSpan.FromHours(24))
            .Select(i => i.PlynlingId == p.Id ? i.TargetPlynlingId!.Value : i.PlynlingId)
            .ToHashSet();
        var relations = (await _db_context.PlynlingRelations
                .Where(r => r.PlynlingAId == p.Id || r.PlynlingBId == p.Id).ToListAsync())
            .ToDictionary(r => r.PlynlingAId == p.Id ? r.PlynlingBId : r.PlynlingAId, r => r.Bond);
        var free = others.Where(id => !recentPairs.Contains(id)).ToList();
        bool Hostile(PlynlingBond b) => b is PlynlingBond.Rivals or PlynlingBond.Enemies;
        return new Dictionary<TargetKind, IReadOnlyList<int>>
        {
            [TargetKind.Known] = free.Where(id => relations.TryGetValue(id, out var b) && !Hostile(b)).ToList(),
            [TargetKind.Hostile] = free.Where(id => relations.TryGetValue(id, out var b) && Hostile(b)).ToList(),
            [TargetKind.Anyone] = free,
        };
    }

    // The ado years, for the adulte trait: what it chose (or chose alone) between turning ado and now.
    private async Task<Func<TraitInfo, double>?> AdultWeightAsync(Plynling p, DateTimeOffset now)
    {
        var age = PlynlingLife.Age(p, now);
        var teenFrom = now - (age - PlynlingLife.StageStart(PlynlingStage.Teen));
        var resolved = (await _db_context.PlynlingEventInstances
                .Where(i => i.PlynlingId == p.Id && i.ResolvedAt != null && i.OptionKey != null).ToListAsync())
            .Where(i => i.ResolvedAt >= teenFrom)
            .Select(i => (Def: PlynlingEvents.ByKey(i.EventKey), i.OptionKey, Success: i.ChallengeSucceeded ?? true))
            .Where(x => x.Def is not null)
            .Select(x => (Option: x.Def!.Options.FirstOrDefault(o => o.Key == x.OptionKey), x.Success))
            .Where(x => x.Option is not null)
            .Select(x => (x.Option!, x.Success))
            .ToList();
        if (resolved.Count == 0) return null;
        var (leaning, growth) = PlynlingEventEngine.AdoHistory(resolved);
        return t => PlynlingEventEngine.AdultTraitWeight(t, leaning, growth);
    }
}
```

- [ ] **Step 2: Weight the adulte trait**

In `PlynlingService.EnsureTraitsAsync` (phase 1), change

```csharp
            : PlynlingTraits.Draw(p.Id, PlynlingLife.Stage(p, now), held);
```

to

```csharp
            : PlynlingTraits.Draw(p.Id, PlynlingLife.Stage(p, now), held, await AdultWeightAsync(p, now));
```

(`AdultWeightAsync` returns null without ado history — the draw stays uniform, as in phase 1.)

- [ ] **Step 3: Build**

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror`
Expected: `Build succeeded`, 0 warnings.

- [ ] **Step 4: Run the harness**

Run: `cd "$SCRATCH/personality" && dotnet run`
Expected: `OK`

- [ ] **Step 5: Files for the owner to commit**

`ProjectSYNCS/Services/PlynlingService.Events.cs`, `ProjectSYNCS/Services/PlynlingService.cs` — `Plynling events: pulse, expiry, picking`.

---

### Task 5: The cards — the choice and the story

**Files:**
- Create: `ProjectSYNCS/Helpers/PlynlingEventStory.cs` (pure: pages from stored data)
- Create: `ProjectSYNCS/Commands/PlynlingEventCards.cs` (static builders)
- Test: `$SCRATCH/personality/Program.cs`

**Interfaces:**
- Consumes: `EventDef`, `EventOption`, `EventContext`, `PlynlingEventEngine`, `PlynlingEvents.Expand`, `PlynlingEventInstance`, `PlynlingBonds.ChangeLine`, `PlynlingArt.Sprite`, `PlynlingArt.VisitSprite`, `PlynlingCatalog.Info`, `PlynlingCardUi.SafeName`, `PlynlingStats.Name/Emoji`.
- Produces:
  - `sealed record EventCast(string Name, PlynlingGender Gender, PlynlingSpecies Species, PlynlingStage Stage, ulong OwnerId)` with `static EventCast Of(Plynling p, DateTimeOffset now)`
  - `sealed record EventStory(int InstanceId, string Heading, IReadOnlyList<string> Pages, EventCast Self, EventCast? Target, uint Accent)`
  - `PlynlingEventStory.Build(PlynlingEventInstance inst, EventCast self, EventCast? target) -> EventStory`
  - `PlynlingEventStory.OutcomeText(PlynlingEventInstance inst, EventCast self, EventCast? target) -> string` (the last page, reused by the ephemeral result)
  - `PlynlingEventCards.BuildChoice(PlynlingEventInstance inst, EventDef def, EventContext ctx, EventCast self, EventCast? target) -> MessageComponent`
  - `PlynlingEventCards.BuildResult(string outcome, int pendingLeft, int plynlingId) -> MessageComponent`
  - `PlynlingEventCards.BuildStory(EventStory story, int page) -> MessageComponent`
  - id helpers `PickId(int instanceId, string optionKey)`, `StoryPrevId/NextId/FirstId/LastId(int instanceId, int page)`

- [ ] **Step 1: Add the failing checks**

In `$SCRATCH/personality/Program.cs`, insert before the final `Console.WriteLine`:

```csharp
// ==== PHASE 2 — cards ==================================================================================
var lila = new EventCast("Lila", PlynlingGender.Female, PlynlingSpecies.Cepe, PlynlingStage.Adult, 1);
var pwet = new EventCast("Pwet", PlynlingGender.Male, PlynlingSpecies.Amanite, PlynlingStage.Adult, 2);
var longName = new EventCast(new string('*', InputCaps.PlynlingName), PlynlingGender.Male, PlynlingSpecies.Rose, PlynlingStage.Teen, 3);
foreach (var e in PlynlingEvents.All)
{
    var holder = Fresh(50);
    var allTraits = new[] { "brave", "pensive", "honest", "shy" };
    var ctx = Ctx(holder, allTraits);
    holder.GrowthDiplomacy = 20;    // every stat gate open
    ctx = new EventContext(holder, ctx.Traits, PlynlingStats.Compute(holder, ctx.Traits), e.Stages[0]);
    var inst = new PlynlingEventInstance { Id = 123456, PlynlingId = 50, EventKey = e.Key, ExpiresAt = noon.AddHours(24),
        TargetPlynlingId = e.Target == TargetKind.None ? null : 51 };
    var target = e.Target == TargetKind.None ? null : pwet;
    PlynlingEventCards.BuildChoice(inst, e, ctx, longName, target);   // throws past 40 components
    foreach (var o in e.Options)
        foreach (var ok in new bool?[] { true, false })
        {
            if (o.Challenge is null && ok == false) continue;
            var done = new PlynlingEventInstance { Id = 777, PlynlingId = 50, EventKey = e.Key, OptionKey = o.Key, ResolvedAt = noon,
                DecidedAlone = ok == false, ChallengeSucceeded = o.Challenge is null ? null : ok, ChancePercent = o.Challenge is null ? null : 65,
                TargetPlynlingId = inst.TargetPlynlingId, BondBefore = target is null ? null : PlynlingBond.Acquaintances,
                BondAfter = target is null ? null : PlynlingBond.Friends };
            var story = PlynlingEventStory.Build(done, lila, target);
            Check(story.Pages.Count == (o.Challenge is null ? 3 : 4), $"{e.Key}/{o.Key}: 3 pages, 4 with a challenge");
            for (var page = 0; page < story.Pages.Count; page++)
            {
                var card = PlynlingEventCards.BuildStory(story, page);
                var ids = card.Components.OfType<ActionRowComponent>().SelectMany(r => r.Components.OfType<ButtonComponent>()).Select(b => b.CustomId).ToList();
                Check(ids.Distinct().Count() == ids.Count, $"{e.Key}/{o.Key} page {page}: no duplicated custom-id");
                Check(ids.All(id => id.Length <= 100), $"{e.Key}/{o.Key} page {page}: ids ≤ 100");
            }
            Check(!story.Pages.Any(p => p.Contains('{')), $"{e.Key}/{o.Key}: no template left in the story");
        }
}
// a story whose target is gone, and one whose event was removed from the catalog
var orphan = new PlynlingEventInstance { Id = 9, PlynlingId = 50, EventKey = "grown_scarf", OptionKey = "return", ResolvedAt = noon, TargetPlynlingId = null };
Check(PlynlingEventStory.Build(orphan, lila, null).Pages.Count == 3, "a story with its target gone still builds");
var removed = new PlynlingEventInstance { Id = 10, PlynlingId = 50, EventKey = "no_such_event", OptionKey = "x", ResolvedAt = noon };
Check(PlynlingEventStory.Build(removed, lila, null).Pages.Count >= 1, "a removed event still renders a plain story");
Check(PlynlingEventCards.BuildResult("Fini.", 2, 50).Components.OfType<ActionRowComponent>().Any(), "more pending: a « suivant » button");
Check(!PlynlingEventCards.BuildResult("Fini.", 0, 50).Components.OfType<ActionRowComponent>().Any(), "nothing pending: no button");
```

- [ ] **Step 2: Run it to see it fail**

Run: `cd "$SCRATCH/personality" && dotnet run`
Expected: build errors — `EventCast`, `PlynlingEventStory`, `PlynlingEventCards` do not exist.

- [ ] **Step 3: The story, pure**

`ProjectSYNCS/Helpers/PlynlingEventStory.cs`:

```csharp
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Who an event happened to, as the story shows them. Names are made safe here, once.
public sealed record EventCast(string Name, PlynlingGender Gender, PlynlingSpecies Species, PlynlingStage Stage, ulong OwnerId)
{
    public static EventCast Of(Plynling p, DateTimeOffset now) =>
        new(PlynlingCardUi.SafeName(p.Name), p.Gender, p.Species, PlynlingLife.Stage(p, now), p.OwnerId);
}

public sealed record EventStory(int InstanceId, string Heading, IReadOnlyList<string> Pages, EventCast Self, EventCast? Target, uint Accent);

/// <summary>
/// An event told as pages — the scene, the choice, the challenge if any, the outcome — built only
/// from the stored instance and the catalog, so any old card can page it after any restart. Line
/// picks are hashed from the instance id.
/// </summary>
public static class PlynlingEventStory
{
    // When nobody chose: a line, picked by the instance id.
    private static readonly string[] AloneLines =
    {
        "Personne n'est venu trancher. {A} a décidé {a:tout seul|toute seule}.",
        "{A} a attendu un conseil, puis a haussé les épaules et fait à sa façon.",
        "Faute d'avis, {A} a écouté son petit caractère.",
    };

    public static EventStory Build(PlynlingEventInstance inst, EventCast self, EventCast? target)
    {
        var accent = PlynlingCatalog.Info(self.Species).Accent;
        if (PlynlingEvents.ByKey(inst.EventKey) is not { } def
            || def.Options.FirstOrDefault(o => o.Key == inst.OptionKey) is not { } option)
            return new EventStory(inst.Id, "📜 Une petite aventure",
                new[] { $"**{self.Name}** a vécu une petite aventure. Les détails se sont perdus en route." }, self, target, accent);

        string X(string t) => PlynlingEvents.Expand(t, self.Name, self.Gender, target?.Name ?? "quelqu'un", target?.Gender ?? PlynlingGender.Male);
        var pages = new List<string> { X(def.Scene) };
        pages.Add(inst.DecidedAlone
            ? X(AloneLines[(int)(StableRoll.Unit(inst.Id, 320, 0) * AloneLines.Length) % AloneLines.Length])
            : $"<@{self.OwnerId}> a tranché pour **{self.Name}** : **{option.Label}**.");
        if (option.Challenge is { } c && inst.ChancePercent is { } chance)
            pages.Add($"🎲 **{PlynlingStats.Name(c.Stat)}** — {chance} % de chances… " +
                      (inst.ChallengeSucceeded == true ? "**réussi !**" : "**raté.**"));
        pages.Add(OutcomeText(inst, self, target));
        return new EventStory(inst.Id, $"📜 {def.Title}", pages, self, target, accent);
    }

    // The outcome and what it changed: growth, and a new bond if the band moved.
    public static string OutcomeText(PlynlingEventInstance inst, EventCast self, EventCast? target)
    {
        if (PlynlingEvents.ByKey(inst.EventKey) is not { } def
            || def.Options.FirstOrDefault(o => o.Key == inst.OptionKey) is not { } option)
            return $"**{self.Name}** a vécu une petite aventure.";
        string X(string t) => PlynlingEvents.Expand(t, self.Name, self.Gender, target?.Name ?? "quelqu'un", target?.Gender ?? PlynlingGender.Male);
        var success = inst.ChallengeSucceeded ?? true;
        var lines = new List<string> { X(success ? option.Outcome : option.FailOutcome ?? option.Outcome) };
        foreach (var g in (success ? option.OnSuccess : option.OnFailure).OfType<GrowStat>())
            lines.Add($"-# {PlynlingStats.Emoji(g.Stat)} {PlynlingStats.Name(g.Stat)} +{g.Amount} pour **{self.Name}**");
        if (target is not null && inst.BondBefore is { } before && inst.BondAfter is { } after && before != after)
            lines.Add(PlynlingBonds.ChangeLine(after, self.Name, self.Gender, target.Name, target.Gender));
        return string.Join("\n", lines);
    }
}
```

- [ ] **Step 4: The cards**

`ProjectSYNCS/Commands/PlynlingEventCards.cs`:

```csharp
using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Commands;

// The event's two cards: the private choice and the public story. Static and Context-free, so the
// harness measures them; every button carries its own verb (Discord rejects a duplicated id).
public static class PlynlingEventCards
{
    public static string PickId(int instanceId, string optionKey) => $"plev:pick:{instanceId}:{optionKey}";
    public static string StoryPrevId(int instanceId, int page) => $"evs:prev:{instanceId}:{page}";
    public static string StoryNextId(int instanceId, int page) => $"evs:next:{instanceId}:{page}";
    public static string StoryFirstId(int instanceId, int page) => $"evs:first:{instanceId}:{page}";
    public static string StoryLastId(int instanceId, int page) => $"evs:last:{instanceId}:{page}";

    // The choice, ephemeral: the scene, one line per option it can see (what it needs, its odds), and
    // one button each. Hidden options are simply absent.
    public static MessageComponent BuildChoice(PlynlingEventInstance inst, EventDef def, EventContext ctx, EventCast self, EventCast? target)
    {
        string X(string t) => PlynlingEvents.Expand(t, self.Name, self.Gender, target?.Name ?? "quelqu'un", target?.Gender ?? PlynlingGender.Male);
        var options = def.Options.Where(o => PlynlingEventEngine.Visible(o, ctx)).ToList();
        var lines = options.Select(o => $"**{o.Label}**{Details(o, ctx, self.Gender)}");
        var row = new ActionRowBuilder();
        foreach (var o in options)
            row.WithButton(o.Label, PickId(inst.Id, o.Key), ButtonStyle.Primary, ButtonEmoji(o));
        return new ComponentBuilderV2()
            .AddComponent(new ContainerBuilder()
                .WithAccentColor(new Color(PlynlingCatalog.Info(self.Species).Accent))
                .AddComponent(new SectionBuilder()
                    .WithAccessory(new ThumbnailBuilder().WithMedia(new UnfurledMediaItemProperties(
                        PlynlingArt.Sprite(self.Species, self.Stage, PlynlingMood.Content))))
                    .AddComponent(new TextDisplayBuilder($"## ✨ {def.Title}\n{X(def.Scene)}")))
                .AddComponent(new SeparatorBuilder())
                .AddComponent(new TextDisplayBuilder(string.Join("\n", lines) +
                    $"\n-# Sans choix de ta part, **{self.Name}** décidera {self.Gender.Agree("seul", "seule")} <t:{inst.ExpiresAt.ToUnixTimeSeconds()}:R>.")))
            .AddComponent(row)
            .Build();
    }

    private static string Details(EventOption o, EventContext ctx, PlynlingGender g)
    {
        var parts = new List<string>();
        if (o.Gate is TraitGate tg && PlynlingTraits.ByKey(tg.TraitKey) is { } trait) parts.Add($"{trait.Emoji} {trait.Name(g)}");
        if (o.Gate is StatGate sg) parts.Add($"{PlynlingStats.Emoji(sg.Stat)} {PlynlingStats.Name(sg.Stat)} {sg.AtLeast}+");
        if (o.Challenge is { } c) parts.Add($"🎲 {PlynlingStats.Name(c.Stat)} : {PlynlingEventEngine.Chance(c, ctx)} %");
        return parts.Count == 0 ? "" : " · " + string.Join(" · ", parts);
    }

    private static IEmote? ButtonEmoji(EventOption o) =>
        o.Gate is TraitGate tg && PlynlingTraits.ByKey(tg.TraitKey) is { } t ? new Emoji(t.Emoji)
        : o.Challenge is not null ? new Emoji("🎲")
        : null;

    // After a pick, in place of the choice: the outcome, where the story is told, and the next
    // event if one waits.
    public static MessageComponent BuildResult(string outcome, int pendingLeft, int plynlingId)
    {
        var builder = new ComponentBuilderV2()
            .AddComponent(new ContainerBuilder()
                .AddComponent(new TextDisplayBuilder($"{outcome}\n-# L'histoire est racontée dans le salon du jeu.")));
        if (pendingLeft > 0)
            builder.AddComponent(new ActionRowBuilder()
                .WithButton($"Événement suivant ({pendingLeft})", $"plyn:events:{plynlingId}", ButtonStyle.Primary, new Emoji("✨")));
        return builder.Build();
    }

    // One page of the public story: both Plynlings side by side for a social event, else its
    // picture; the page; « 2/4 »; ◀ ▶, « ⏭ Fin » before the last page, « ↺ Début » on it.
    public static MessageComponent BuildStory(EventStory story, int page)
    {
        page = Math.Clamp(page, 0, story.Pages.Count - 1);
        var container = new ContainerBuilder()
            .WithAccentColor(new Color(story.Accent))
            .AddComponent(new TextDisplayBuilder($"## {story.Heading}"));
        if (story.Target is { } target)
            container.AddComponent(new MediaGalleryBuilder()
                .AddItem(PlynlingArt.VisitSprite(story.Self.Species, story.Self.Stage, PlynlingMood.Content), story.Self.Name, false)
                .AddItem(PlynlingArt.VisitSprite(target.Species, target.Stage, PlynlingMood.Content), target.Name, false));
        else
            container.AddComponent(new MediaGalleryBuilder()
                .AddItem(PlynlingArt.VisitSprite(story.Self.Species, story.Self.Stage, PlynlingMood.Content), story.Self.Name, false));
        container
            .AddComponent(new SeparatorBuilder())
            .AddComponent(new TextDisplayBuilder($"{story.Pages[page]}\n-# {page + 1}/{story.Pages.Count}"));

        var builder = new ComponentBuilderV2().AddComponent(container);
        if (story.Pages.Count > 1)
        {
            var row = new ActionRowBuilder()
                .WithButton("◀", StoryPrevId(story.InstanceId, page), ButtonStyle.Secondary, disabled: page == 0);
            if (page == story.Pages.Count - 1)
                row.WithButton("↺ Début", StoryFirstId(story.InstanceId, page), ButtonStyle.Secondary);
            else
                row.WithButton("▶", StoryNextId(story.InstanceId, page), ButtonStyle.Secondary)
                   .WithButton("⏭ Fin", StoryLastId(story.InstanceId, page), ButtonStyle.Secondary);
            builder.AddComponent(row);
        }
        return builder.Build();
    }
}
```

Check the gallery API matches the visit story's usage (`MediaGalleryBuilder.AddItem(url, description, isSpoiler)`); `PlynlingArt.Sprite(species, stage, mood)` and `VisitSprite(species, stage, mood)` are the phase-0 signatures. `Agree` is the `PlynlingGrammar` extension (add `using ProjectSYNCS.Helpers;` — already there).

- [ ] **Step 5: Run the harness**

Run: `cd "$SCRATCH/personality" && dotnet run`
Expected: `OK`

- [ ] **Step 6: Build**

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror`
Expected: `Build succeeded`, 0 warnings.

- [ ] **Step 7: Files for the owner to commit**

`ProjectSYNCS/Helpers/PlynlingEventStory.cs`, `ProjectSYNCS/Commands/PlynlingEventCards.cs` — `Plynling events: choice and story cards`.

---

### Task 6: Wiring — the card button, the handlers, the announcer, the sweep

**Files:**
- Modify: `ProjectSYNCS/Commands/PlynlingModule.cs` (`BuildCard` gets `pendingEvents`; « ✨ Événement » in the last row; `view` passes the count)
- Modify: `ProjectSYNCS/Services/PlynlingCareService.cs` (four call sites pass the count)
- Modify: `ProjectSYNCS/Services/PlynlingAnnouncer.cs` (`PostEventStoryAsync`)
- Create: `ProjectSYNCS/Interactions/Components/PlynlingEventHandler.cs`
- Modify: `ProjectSYNCS/Services/PlynlingSweepService.cs`
- Test: `$SCRATCH/personality/Program.cs`

**Interfaces:**
- Consumes: Tasks 4–5.
- Produces: `PlynlingModule.BuildCard(…, IReadOnlyList<TraitInfo>? traits = null, int pendingEvents = 0)`; `PlynlingAnnouncer.PostEventStoryAsync(ulong guildId, MessageComponent story, IMessageChannel? fallback = null) -> Task`; `PlynlingService.GetEventStoryAsync(int instanceId, DateTimeOffset now) -> Task<EventStory?>`.

- [ ] **Step 1: Check the ids are free**

Run: `cd "$REPO/ProjectSYNCS" && grep -rn "plyn:events\|plev:\|\"evs:" --include=*.cs . | grep -v PlynlingEventCards`
Expected: no output.

- [ ] **Step 2: Add the failing card checks**

In `$SCRATCH/personality/Program.cs`, insert before the final `Console.WriteLine`:

```csharp
bool HasButton(MessageComponent card, string id) =>
    card.Components.OfType<ActionRowComponent>().Any(r => r.Components.OfType<ButtonComponent>().Any(b => b.CustomId == id));
Check(HasButton(PlynlingModule.BuildCard(awake, noon, null, traits: mascotTraits, pendingEvents: 2), "plyn:events:31"), "pending: « Événement » on the card");
Check(!HasButton(PlynlingModule.BuildCard(awake, noon, null, traits: mascotTraits), "plyn:events:31"), "nothing pending: no button");
Check(!HasButton(PlynlingModule.BuildCard(dead, noon, null, pendingEvents: 2), "plyn:events:33"), "dead: never");
```

Run: `cd "$SCRATCH/personality" && dotnet run` — Expected: build error (`pendingEvents` does not exist).

- [ ] **Step 3: The card**

In `BuildCard`, add the parameter `int pendingEvents = 0` after `traits`, and change the phase-1 personality row to:

```csharp
        // Its own row, the card's last: the care row is hidden at night and holds three buttons when
        // sick. Shown frozen too. « Personnalité » for anyone; « Événement » only while one waits —
        // offered to everyone like « Laver », refused in the handler for anyone but the owner.
        if (alive)
        {
            var personalityRow = new ActionRowBuilder()
                .WithButton("Personnalité", $"plyn:traits:{plynling.Id}", ButtonStyle.Secondary, new Emoji("📜"));
            if (pendingEvents > 0)
                personalityRow.WithButton($"Événement ({pendingEvents})", $"plyn:events:{plynling.Id}", ButtonStyle.Success, new Emoji("✨"));
            builder.AddComponent(personalityRow);
        }
```

At all five call sites (the `view` command and the four in `PlynlingCareService`), add `pendingEvents: await _plynlings.CountPendingEventsAsync(<that Plynling>, now)` next to `traits:`. Verify: `grep -rn "BuildCard(" --include=*.cs ProjectSYNCS | grep -v LevelModule | grep -v "static MessageComponent"` — five sites, each followed by `pendingEvents:` within its statement.

- [ ] **Step 4: Loading a story**

Append to `PlynlingService.Events.cs` (inside the class):

```csharp
    // A told event, rebuilt from its row. Null while it is still pending or if it no longer exists.
    public async Task<EventStory?> GetEventStoryAsync(int instanceId, DateTimeOffset now)
    {
        var inst = await GetEventInstanceAsync(instanceId);
        if (inst?.ResolvedAt is null) return null;
        var self = await _db_context.Plynlings.FirstOrDefaultAsync(x => x.Id == inst.PlynlingId);
        if (self is null) return null;
        var target = inst.TargetPlynlingId is { } tid ? await _db_context.Plynlings.FirstOrDefaultAsync(x => x.Id == tid) : null;
        return PlynlingEventStory.Build(inst, EventCast.Of(self, now), target is null ? null : EventCast.Of(target, now));
    }
```

(Plain reads, no settle: a story shows names and pictures only.)

- [ ] **Step 5: The announcer**

In `PlynlingAnnouncer.cs`, add after `AnnounceAbandonAsync`:

```csharp
    // An event's story. In the game channel's guild it goes there; elsewhere (the dev guild) it goes to
    // `fallback`, the channel the choice was made in, when there is one — a sweep resolution there is
    // logged and dropped, like every other announcement.
    public async Task PostEventStoryAsync(ulong guildId, MessageComponent story, IMessageChannel? fallback = null)
    {
        try
        {
            IMessageChannel? channel = _client.GetChannel(GameChannelId) is IMessageChannel game
                                       && game is IGuildChannel home && home.GuildId == guildId
                ? game
                : fallback;
            if (channel is null)
            {
                _logger.LogInformation("Event story in guild {GuildId} not posted: no game channel there.", guildId);
                return;
            }
            await channel.SendMessageAsync(components: story, flags: MessageFlags.ComponentsV2, allowedMentions: AllowedMentions.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to post an event story.");
        }
    }
```

- [ ] **Step 6: The handlers**

`ProjectSYNCS/Interactions/Components/PlynlingEventHandler.cs`:

```csharp
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// Events: « ✨ Événement » opens the oldest pending one privately; a pick resolves it and tells its
// story; ◀ ▶ page any told story, rebuilt from the database. Every path reads the database, so every
// path defers first.
public class PlynlingEventHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PlynlingService _plynlings;
    private readonly PlynlingAnnouncer _announcer;

    public PlynlingEventHandler(PlynlingService plynlings, PlynlingAnnouncer announcer)
    {
        _plynlings = plynlings;
        _announcer = announcer;
    }

    [ComponentInteraction("plyn:events:*", ignoreGroupNames: true)]
    public async Task OnOpenAsync(string idStr)
    {
        if (!int.TryParse(idStr, out var id))
        {
            await RespondAsync(PlynlingText.Unknown, ephemeral: true);
            return;
        }
        await DeferAsync(ephemeral: true);
        var now = DateTimeOffset.UtcNow;
        var p = await _plynlings.GetByIdAsync(id, now);
        if (p is null || p.DiedAt is not null)
        {
            await FollowupAsync(PlynlingText.Unknown, ephemeral: true);
            return;
        }
        if (p.OwnerId != Context.User.Id)
        {
            await FollowupAsync(PlynlingText.EventNotYours(p.Gender), ephemeral: true);
            return;
        }
        var inst = (await _plynlings.GetPendingEventsAsync(p, now)).FirstOrDefault();
        if (inst is null || PlynlingEvents.ByKey(inst.EventKey) is not { } def)
        {
            await FollowupAsync(PlynlingText.NoEventWaiting, ephemeral: true);
            return;
        }
        var target = inst.TargetPlynlingId is { } tid ? await _plynlings.GetByIdAsync(tid, now) : null;
        var card = PlynlingEventCards.BuildChoice(inst, def, await _plynlings.GetEventContextAsync(p, now),
            EventCast.Of(p, now), target is null ? null : EventCast.Of(target, now));
        await FollowupAsync(components: card, ephemeral: true, flags: MessageFlags.ComponentsV2, allowedMentions: AllowedMentions.None);
    }

    [ComponentInteraction("plev:pick:*:*", ignoreGroupNames: true)]
    public async Task OnPickAsync(string idStr, string optionKey)
    {
        if (!int.TryParse(idStr, out var id))
        {
            await RespondAsync(PlynlingText.Unknown, ephemeral: true);
            return;
        }
        await DeferAsync();      // updates the ephemeral choice in place
        var now = DateTimeOffset.UtcNow;
        var pick = await _plynlings.PickEventAsync(id, optionKey, Context.User.Id, now);
        if (pick.Outcome != EventPickOutcome.Done)
        {
            await FollowupAsync(pick.Outcome switch
            {
                EventPickOutcome.Gone => PlynlingText.EventAlreadyDecided,
                EventPickOutcome.NotAvailable => PlynlingText.EventOptionGone,
                _ => PlynlingText.Unknown,
            }, ephemeral: true);
            return;
        }
        var story = await _plynlings.GetEventStoryAsync(id, now);
        var inst = await _plynlings.GetEventInstanceAsync(id);
        if (story is null || inst is null) return;
        await ModifyOriginalResponseAsync(m =>
        {
            m.Components = PlynlingEventCards.BuildResult(story.Pages[^1], pick.PendingLeft, inst.PlynlingId);
            m.Flags = MessageFlags.ComponentsV2;
            m.AllowedMentions = AllowedMentions.None;
        });
        await _announcer.PostEventStoryAsync(Context.Guild?.Id ?? 0, PlynlingEventCards.BuildStory(story, 0), Context.Channel);
    }

    [ComponentInteraction("evs:prev:*:*", ignoreGroupNames: true)]
    public Task OnPrevAsync(string id, string page) => PageAsync(id, page, s => s.page - 1);

    [ComponentInteraction("evs:next:*:*", ignoreGroupNames: true)]
    public Task OnNextAsync(string id, string page) => PageAsync(id, page, s => s.page + 1);

    [ComponentInteraction("evs:first:*:*", ignoreGroupNames: true)]
    public Task OnFirstAsync(string id, string page) => PageAsync(id, page, _ => 0);

    [ComponentInteraction("evs:last:*:*", ignoreGroupNames: true)]
    public Task OnLastAsync(string id, string page) => PageAsync(id, page, s => s.story.Pages.Count - 1);

    private async Task PageAsync(string idStr, string pageStr, Func<(EventStory story, int page), int> to)
    {
        if (!int.TryParse(idStr, out var id) || !int.TryParse(pageStr, out var page))
        {
            await RespondAsync(PlynlingText.Unknown, ephemeral: true);
            return;
        }
        await DeferAsync();
        var story = await _plynlings.GetEventStoryAsync(id, DateTimeOffset.UtcNow);
        if (story is null)
        {
            await FollowupAsync(PlynlingText.StoryGone, ephemeral: true);
            return;
        }
        var card = PlynlingEventCards.BuildStory(story, to((story, page)));
        await ModifyOriginalResponseAsync(m =>
        {
            m.Components = card;
            m.Flags = MessageFlags.ComponentsV2;
            m.AllowedMentions = AllowedMentions.None;
        });
    }
}
```

- [ ] **Step 7: The refusal texts**

In `ProjectSYNCS/Helpers/PlynlingText.cs`, next to `StoryGone`:

```csharp
    public const string NoEventWaiting = "Rien ne l'attend pour l'instant. Reviens plus tard.";
    public const string EventAlreadyDecided = "Trop tard : c'est déjà décidé.";
    public const string EventOptionGone = "Ce choix n'est plus possible.";
    public static string EventNotYours(PlynlingGender g) =>
        $"Ce n'est pas ton Plynling : c'est à son propriétaire de choisir pour {g.Agree("lui", "elle")}.";
```

(Add `using ProjectSYNCS.Models;` if `PlynlingGender` is not in scope there.)

- [ ] **Step 8: The sweep**

In `PlynlingSweepService.SweepAsync`, change

```csharp
                await plynlings.ProgressAsync(plynling, now);      // time's badges and stage moments
```

to

```csharp
                await plynlings.ProgressAsync(plynling, now);      // time's badges and stage moments
                var told = await plynlings.TickEventsAsync(plynling, now);   // cancels, decides alone, pulses — saves
```

and right before the per-item `catch`, after the `if … else` chain that saves, add:

```csharp
                // Told after every save above; a failed post is logged by the announcer, never retried.
                foreach (var instanceId in told)
                    if (await plynlings.GetEventStoryAsync(instanceId, now) is { } story)
                        await _announcer.PostEventStoryAsync(plynling.GuildId, PlynlingEventCards.BuildStory(story, 0));
```

Add `using ProjectSYNCS.Commands;` at the top of the file.

- [ ] **Step 9: Run the harness and build**

Run: `cd "$SCRATCH/personality" && dotnet run` then `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror`
Expected: `OK`; `Build succeeded`, 0 warnings.

- [ ] **Step 10: Files for the owner to commit**

`ProjectSYNCS/Commands/PlynlingModule.cs`, `ProjectSYNCS/Services/PlynlingCareService.cs`, `ProjectSYNCS/Services/PlynlingAnnouncer.cs`, `ProjectSYNCS/Services/PlynlingService.Events.cs`, `ProjectSYNCS/Interactions/Components/PlynlingEventHandler.cs`, `ProjectSYNCS/Helpers/PlynlingText.cs`, `ProjectSYNCS/Services/PlynlingSweepService.cs` — `Plynling events: card button, handlers, sweep`.

---

### Task 7: `/debug event`, the two-page help, docs, version, dev-guild test

**Files:**
- Modify: `ProjectSYNCS/Commands/DebugModule.cs`
- Modify: `ProjectSYNCS/Commands/PlynlingModule.cs` (`BuildHelpEmbed(int page)`, `BuildHelpButtons(int page)`, `HelpAsync`)
- Create: `ProjectSYNCS/Interactions/Components/PlynlingHelpHandler.cs`
- Modify: `README.md`, `docs/agents/plynling-events.md`, `ProjectSYNCS/config.yaml`
- Test: `$SCRATCH/personality/Program.cs`

**Interfaces:**
- Produces: `/debug event key: [mode:]`; `PlynlingModule.BuildHelpEmbed(int page = 0) -> Embed`, `BuildHelpButtons(int page) -> MessageComponent`; custom-ids `plyn:help:0`, `plyn:help:1`.

- [ ] **Step 1: Owner tool**

In `DebugModule.cs`, add an enum next to `SickState`:

```csharp
    public enum EventMode
    {
        [ChoiceDisplay("En attente")] Pending,
        [ChoiceDisplay("Expiré (décidé au prochain passage)")] Expired,
        [ChoiceDisplay("Décidé tout de suite, raconté ici")] Now,
    }
```

and after `PlynlingAsync`:

```csharp
    // Events come once a day: the owner forces one on his own living Plynling — waiting, already
    // expired (the next sweep decides it alone), or decided at once with its story posted here.
    // Ignores the pulse, the cap and the stage; a social event takes any other living Plynling.
    [SlashCommand("event", "Forcer un événement sur ton propre Plynling (tests)")]
    public async Task EventAsync(
        [Summary("key", "Clé de l'événement")] string key,
        [Summary("mode", "Quand le décider")] EventMode mode = EventMode.Pending)
    {
        if (Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
            return;
        }
        if (Context.Guild is null)
        {
            await RespondAsync("Sur un serveur, pas en message privé.", ephemeral: true);
            return;
        }
        if (PlynlingEvents.ByKey(key) is not { } def)
        {
            await RespondAsync("Clés : " + string.Join(", ", PlynlingEvents.All.Select(e => $"`{e.Key}`")), ephemeral: true);
            return;
        }
        await DeferAsync(ephemeral: true);
        var now = DateTimeOffset.UtcNow;
        var p = await _plynlings.GetCurrentAsync(Context.Guild.Id, Context.User.Id, now);
        if (p is null)
        {
            await FollowupAsync(PlynlingText.NoPlynling, ephemeral: true);
            return;
        }
        var targetId = def.Target == TargetKind.None ? null : await _plynlings.AnyOtherLivingIdAsync(p);
        var inst = await _plynlings.CreateEventAsync(p, def, targetId, now);
        if (mode == EventMode.Expired) await _plynlings.ExpireEventNowAsync(inst.Id, now);
        if (mode == EventMode.Now)
        {
            await _plynlings.ResolveAloneAsync(p, inst, now);
            await _plynlings.SaveAsync();
            if (await _plynlings.GetEventStoryAsync(inst.Id, now) is { } story)
                await _announcer.PostEventStoryAsync(Context.Guild.Id, PlynlingEventCards.BuildStory(story, 0), Context.Channel);
        }
        await FollowupAsync($"🔧 `{def.Key}` créé (#{inst.Id}, {mode}).", ephemeral: true);
    }
```

Add `PlynlingAnnouncer _announcer` to `DebugModule`'s constructor (check the existing constructor and add the field and parameter the same way as `_plynlings`). Append to `PlynlingService.Events.cs`:

```csharp
    // /debug event only: any other living Plynling of the guild, for a forced social event.
    public async Task<int?> AnyOtherLivingIdAsync(Plynling p) =>
        await _db_context.Plynlings.Where(x => x.GuildId == p.GuildId && x.Id != p.Id && x.DiedAt == null)
            .Select(x => (int?)x.Id).FirstOrDefaultAsync();

    // /debug event only: make a pending event due now, so the next sweep decides it alone.
    public async Task ExpireEventNowAsync(int instanceId, DateTimeOffset now)
    {
        if (await GetEventInstanceAsync(instanceId) is { } inst)
        {
            inst.ExpiresAt = now;
            await _db_context.SaveChangesAsync();
        }
    }
```

- [ ] **Step 2: Help checks (failing)**

In `$SCRATCH/personality/Program.cs`, **replace** the phase-1 help block (the three `Check`s after `var help = PlynlingModule.BuildHelpEmbed();`) with:

```csharp
foreach (var page in new[] { 0, 1 })
{
    var h = PlynlingModule.BuildHelpEmbed(page);
    Check(h.Length <= 6000, $"/plynling help page {page} fits ({h.Length}/6000)");
    Check(h.Fields.All(f => f.Value.Length <= 1024), $"help page {page}: fields ≤ 1024");
    var ids = PlynlingModule.BuildHelpButtons(page).Components.OfType<ActionRowComponent>().SelectMany(r => r.Components.OfType<ButtonComponent>()).Select(b => b.CustomId).ToList();
    Check(ids.Distinct().Count() == ids.Count, $"help page {page}: distinct ids");
}
Check(PlynlingModule.BuildHelpEmbed(0).Fields[0].Value.Contains("première semaine"), "help page 0: stages");
Check(PlynlingModule.BuildHelpEmbed(1).Fields.Any(f => f.Name == "Événements"), "help page 1: events");
```

Run: `cd "$SCRATCH/personality" && dotnet run` — Expected: build error (`BuildHelpEmbed(int)` does not exist).

- [ ] **Step 3: Two pages**

In `PlynlingModule.cs`:
- remove the phase-1 line `"\n**Personnalité** (bouton de sa carte) — Ses 4 traits, gagnés en grandissant, et ses stats."` from the « Adopter & regarder » field (it moves to page 1), keeping `"…Le temps passé gelé ne compte pas.")` as the field's end;
- rename the current `BuildHelpEmbed()` to `private static Embed BuildCareHelp()` (body unchanged);
- add:

```csharp
    // /plynling help is two pages: the pet, and its personality and events. One embed could not hold
    // both under Discord's 6000-character cap. Each page is measured by the harness.
    public static Embed BuildHelpEmbed(int page = 0) => page == 1 ? BuildPersonalityHelp() : BuildCareHelp();

    public static MessageComponent BuildHelpButtons(int page) =>
        new ComponentBuilder()
            .WithButton("Le Plynling", "plyn:help:0", ButtonStyle.Secondary, new Emoji("🍄"), disabled: page == 0)
            .WithButton("Personnalité & événements", "plyn:help:1", ButtonStyle.Secondary, new Emoji("✨"), disabled: page == 1)
            .Build();

    private static Embed BuildPersonalityHelp() =>
        new EmbedBuilder()
            .WithTitle("🍄 Plynlings — personnalité et événements")
            .WithColor(new Color(0xCE323A))
            .AddField("Personnalité",
                "En grandissant, il gagne **4 traits** : un trait d'enfance bébé, deux de plus ado, un dernier adulte. " +
                "Ils façonnent ses **stats** (Diplomatie, Intendance, Sagesse, Ruse, Courage) et un petit titre. " +
                "**📜 Personnalité** (bouton de sa carte) détaille tout, pour n'importe quel Plynling.")
            .AddField("Événements",
                "À peu près une fois par jour, il lui arrive quelque chose : **✨ Événement** apparaît sur sa carte " +
                "(3 en attente au plus). Son propriétaire choisit pour lui, en privé.\n" +
                "Certains choix demandent un trait ou une stat ; d'autres sont des **défis**, avec leurs chances affichées. " +
                "Ses choix font grandir ses stats, et ce qu'il vit ado oriente son trait d'adulte.\n" +
                "Sans réponse en 24 h, il décide **tout seul**, selon son caractère — sans jamais y perdre quoi que ce soit.\n" +
                "Chaque histoire est racontée dans le salon du jeu, page par page.")
            .Build();
```

- change `HelpAsync` to:

```csharp
    public Task HelpAsync() => RespondAsync(embed: BuildHelpEmbed(0), components: BuildHelpButtons(0), ephemeral: true);
```

`ProjectSYNCS/Interactions/Components/PlynlingHelpHandler.cs`:

```csharp
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Commands;

namespace ProjectSYNCS.Interactions.Components;

// The two pages of /plynling help. No database: answered in place, no defer needed.
public class PlynlingHelpHandler : InteractionModuleBase<SocketInteractionContext>
{
    [ComponentInteraction("plyn:help:*", ignoreGroupNames: true)]
    public async Task OnPageAsync(string pageStr)
    {
        var page = pageStr == "1" ? 1 : 0;
        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(m =>
        {
            m.Embed = PlynlingModule.BuildHelpEmbed(page);
            m.Components = PlynlingModule.BuildHelpButtons(page);
        });
    }
}
```

Run: `cd "$SCRATCH/personality" && dotnet run` — Expected: `OK`.

- [ ] **Step 4: Docs**

`README.md`, after the phase-1 « Personality » bullet:

```markdown
- **Events:** about once a day, something happens to each Plynling — a little scene with two to four
  choices, some needing a trait or a stat, some a challenge with its odds shown. Its owner chooses
  privately from « ✨ Événement » on the card; after 24 h it decides on its own, in character, at no
  cost. Choices grow its stats, and its *ado* years lean its adult trait. Every outcome is told as a
  paged story in the game channel.
```

`docs/agents/plynling-events.md`, append:

```markdown
## Events — `Helpers/PlynlingEvents`, `PlynlingEventEngine`, `PlynlingService.Events.cs`

- **Catalog:** C# data. Event and option keys are stored — **never renamed**, append only. Every event
  needs an **ungated option with no stress cost** (deciding alone); no « il »/« elle » in event text
  (one text serves every gender pair). The harness checks both.
- **Every rule is pure and hashed** (`StableRoll`): pulse time (08:00–20:00 Paris), which event,
  the target, the challenge roll (from the instance id), the in-character choice. Never a `Random`.
- **Pacing:** one pulse a day, skipped when 3 pulse events wait; 24 h to choose; frozen = nothing
  happens; a death cancels what waits; the mascot decides at once.
- **Deciding alone** never picks a hidden option or one with a stress cost for a held trait — the
  code enforces the "no penalty for not playing" rule, not just the content.
- **One context:** event logic lives in the `PlynlingService` partial, so growth, the relation change
  (`ShiftAffinityAsync`, shared with visits through `ApplyBondChangeAsync`), badges and the journal
  land in one save. Events never make a couple.
- **Stories are rebuilt from the row** on every page turn: the row stores the chance shown, the roll,
  and the bond before/after. No in-memory story store. A removed event still renders a plain story.
- **Where stories go:** the game channel for its guild; a pick made elsewhere (the dev guild) posts in
  the channel it was made in; a sweep resolution elsewhere is dropped.
- **Testing:** `/debug event key: mode:` (owner only) forces one — pending, expired, or decided now.
- `/plynling help` is two pages (`plyn:help:0|1`); measure each after editing.
```

- [ ] **Step 5: Version and build**

In `config.yaml`, change `version: "5.12.0"` to `version: "5.13.0"`.

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror`
Expected: `Build succeeded`, 0 warnings.

- [ ] **Step 6: Dev-guild test (with the owner)**

Run the bot against the dev guild, then:

1. Startup applies `AddPlynlingEvents`.
2. `/debug event key:baby_puddle` (or any event; the stage is ignored) → the card (`/plynling view`) shows « ✨ Événement (1) ».
3. Press it → private choice card: the scene, option lines with odds (« 🎲 Courage : n % »), buttons. A gated option you lack is absent.
4. Choose → the private card turns into the outcome (+ « Événement suivant » if another waits); the story is posted **in this channel** (dev guild); ◀ ▶ ⏭ ↺ page it.
5. Restart the bot, then page the same story → still works.
6. `/debug event key:grown_parcel mode:Expired` → within an hour, the sweep decides it alone; check the journal (« 📜 Le colis égaré. ») and that no stress-costing option was taken.
7. `/debug event key:grown_scarf mode:Now` with a second Plynling in the guild → story with two pictures; if the bond changed band, the line shows on the last page and in both journals.
8. Someone else presses « ✨ Événement » on your card → refused privately.
9. Double-click race: open the same event in two clients, pick in both → one « Trop tard : c'est déjà décidé. ».
10. `/plynling help` → two pages, both buttons work.
11. Wait for a natural pulse (next day): « ✨ Événement » appears without `/debug`.

Report each result; failures go back to the owning task.

- [ ] **Step 7: Files for the owner to commit**

`ProjectSYNCS/Commands/DebugModule.cs`, `ProjectSYNCS/Commands/PlynlingModule.cs`, `ProjectSYNCS/Interactions/Components/PlynlingHelpHandler.cs`, `ProjectSYNCS/Services/PlynlingService.Events.cs`, `README.md`, `docs/agents/plynling-events.md`, `ProjectSYNCS/config.yaml` — `Plynling events: debug tool, two-page help, docs, 5.13.0`.
