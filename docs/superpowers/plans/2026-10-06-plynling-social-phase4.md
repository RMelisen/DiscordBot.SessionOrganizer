# Plynling social events and chains — Phase 4 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Events that reach other Plynlings and run over time: four **big moments** (déclaration, défi de rivalité, pacte de meilleurs amis, réconciliation) that ask the other Plynling's owner to answer, **follow-ups** that come back days later, and **on-actions** — adoption, growing up, a visit, a friend's death, falling sick, recovering — that bring their own event.

**Architecture:** Three new effect types carry it: `AskTarget` queues a `Response` instance on the other Plynling (its owner answers from their own card, or it answers in character through CK3-style acceptance), `FollowUp` queues an instance whose `AvailableAt` lies in the future, and the relation effects (`SetAffinityAtLeast`, `Couple`, `Heartbreak`) reuse the visit rules. On-actions are collected during a unit of work and created **after** its save (`FlushOnActionsAsync`), each in its own save, swallow-and-log. Everything stays in the `PlynlingService` partial, pure rules in the engine.

**Tech Stack:** as phases 1–3.

**Spec:** `docs/superpowers/specs/2026-10-06-plynling-events-design.md` — Phase 4. **Depends on phases 1–3** being implemented (`EventDef` with `BreakLevel`, `EventOption`, `EventContext`, `ApplyEventAsync` as rewritten in phase 3, `QueueEventAsync`, `CreateEventAsync`, `TickEventsAsync`, `PlynlingEventStory`, `PlynlingEventCards`, `AppliesWhenAlone`). If they landed with different names, update this plan's code first.

## Global Constraints

- **Never commit or push.** Each task ends by listing the files to commit.
- **Anti-griefing:** an event changes **another owner's** Plynling only through the relation (affinity, bond, couple). Never its stress, stats, modifiers or needs. **One exception, by design:** a refused **declaration** saddens the one who declared — whose owner chose to declare (declaring is **owner-only**, never picked alone).
- **Owner-only options** (`OwnerOnly`) are never picked alone. Declaring and challenging a rival are owner-only, so deciding alone never risks a refusal or a lost challenge.
- **Responses:** queued on the other Plynling, ignore the pulse cap, 24 h; decided alone by acceptance (affinity, hidden compatibility, the responder's axes); **the mascot answers at once**. A declaration follows the visit rules (a boy and a girl, best friends, affinity ≥ 80, neither in a living couple) — checked when asked **and** when answered.
- **Follow-ups:** `AvailableAt` = now + a hashed delay in the option's range; same target; `ParentInstanceId`; ignore the cap; cancelled by a death (phase 2's tick cancels every open instance, future ones included).
- **Social targets:** same guild, living, unfrozen, at most one social event per pair per 24 h (phase 2), plus the event's own **target condition** (bond, gender, couple).
- **On-actions** run after the owning save, in their own save, swallow-and-log: a failed event creation never breaks an adoption, a visit, a death or a sweep.
- **Append-only:** event and option keys, effect types and record fields at the end, `OnAction` values at the end.
- Text: `docs/plynling-writing-style.md`; no « il/elle/ils/elles » in event text; `{T}` is the new trait(s), only in on-action events.
- Build with `dotnet build -warnaserror`.

## Review Focus

1. **The responder vanishes** (abandoned, dead, frozen) before answering: the response is cancelled or skipped, the initiator's story never claims an answer that did not come. (Task 3 cancel path; Task 4 harness renders an asked story with no response.)
2. **A couple forms elsewhere** between the declaration and the answer: accepting no longer makes a couple — the story says it came too late. (Task 3 re-checks `CanConfess` at answer time.)
3. **Deciding alone, as initiator, never picks an owner-only option**, over every social event and many ids. (Task 1 harness.)
4. **An on-action that throws** (no eligible event, a database error) leaves the adoption, visit or sweep saved and logged. (Task 3: each flush item in its own `try`; dev-guild test with an event key removed.)
5. **Two follow-ups from one chain** cannot both fire if the Plynling died in between. (Phase 2 tick cancels open instances; Task 3 harness checks future instances are "open".)

---

### Task 1: Engine — new effects, owner-only, acceptance, target context

**Files:**
- Modify: `ProjectSYNCS/Helpers/PlynlingEventCatalog.cs`
- Modify: `ProjectSYNCS/Helpers/PlynlingEventEngine.cs`
- Test: `$SCRATCH/personality/Program.cs`

**Interfaces:**
- Produces:
  - `enum OnAction { Adopted, BecameTeen, BecameAdult, AfterVisit, Bereaved, FellSick, Recovered }`
  - `enum Stance { Neutral, Accept, Refuse }`
  - effects `FollowUp(string EventKey, double MinHours, double MaxHours)`, `AskTarget(string ResponseKey)`, `SetAffinityAtLeast(int Value)`, `Couple()`, `Heartbreak()`
  - `EventOption` gains trailing `bool OwnerOnly = false, Stance Stance = Stance.Neutral`
  - `EventChallenge` gains trailing `bool VsTarget = false`
  - `EventDef` gains trailing `OnAction? Trigger = null, Func<TargetInfo, bool>? TargetCondition = null`
  - `sealed record TargetInfo(Plynling Target, PlynlingBond Bond, int Affinity, int Compatibility, bool EitherInCouple)`
  - `EventContext` gains trailing `TargetInfo? Other = null, IReadOnlyList<StatLine>? OtherStats = null`
  - `PlynlingEventEngine`: `Chance` reads the target's stat when `VsTarget`; `AloneOptions` excludes `OwnerOnly`; `AcceptWeight(int affinity, int compatibility) -> double`; `DecideResponse(EventDef, EventContext, int instanceId) -> EventOption`; `FollowUpAt(int instanceId, FollowUp, DateTimeOffset now) -> DateTimeOffset`; `PickTriggered(OnAction, int plynlingId, int salt, IEnumerable<EventDef>, EventContext) -> EventDef?`; `PickPulse` gains trailing `Func<EventDef, bool>? hasTarget = null`; `AppliesWhenAlone` knows the new effects.

- [ ] **Step 1: Add the failing checks**

In `$SCRATCH/personality/Program.cs`, insert before the final `Console.WriteLine`:

```csharp
// ==== PHASE 4 — engine ======================================================================================
var ownerOnlyDef = new EventDef("oo", EventType.Pulse, new[] { PlynlingStage.Adult }, "OO", "S", new[]
{
    Opt("free"),
    new EventOption("declare", "declare", "ok", null, null, null, Array.Empty<EventEffect>(), Array.Empty<EventEffect>(),
        new Dictionary<string, int>(), new Dictionary<AiAxis, int> { [AiAxis.Sociability] = 50 }, OwnerOnly: true),
});
for (var i = 1; i <= 300; i++)
    Check(PlynlingEventEngine.DecideAlone(ownerOnlyDef, Ctx(Fresh(100), "gregarious"), i).Key == "free", $"alone {i}: never owner-only");

// acceptance
Check(PlynlingEventEngine.AcceptWeight(80, 15) > PlynlingEventEngine.AcceptWeight(0, 0)
      && PlynlingEventEngine.AcceptWeight(0, 0) > PlynlingEventEngine.AcceptWeight(-80, -15), "acceptance grows with affinity and compatibility");
Check(PlynlingEventEngine.AcceptWeight(-100, -20) >= 0.05, "never impossible");
EventOption StanceOpt(string key, Stance s) => new(key, key, "ok", null, null, null, Array.Empty<EventEffect>(), Array.Empty<EventEffect>(),
    new Dictionary<string, int>(), new Dictionary<AiAxis, int>(), Stance: s);
var resp = new EventDef("r", EventType.Response, new[] { PlynlingStage.Adult }, "R", "S", new[] { StanceOpt("yes", Stance.Accept), StanceOpt("no", Stance.Refuse) });
TargetInfo Rel(int affinity, int compat) => new(Fresh(101), PlynlingBond.Friends, affinity, compat, false);
int Yes(int affinity, int compat) => Enumerable.Range(1, 1000).Count(i =>
    PlynlingEventEngine.DecideResponse(resp, Ctx(Fresh(102)) with { Other = Rel(affinity, compat) }, i).Key == "yes");
Check(Yes(90, 15) > 750 && Yes(-90, -15) < 250, $"close pairs accept, hostile ones refuse ({Yes(90, 15)}, {Yes(-90, -15)})");

// stat against stat
var me = Fresh(103); var them = Fresh(104);
var vs = Ctx(me) with { Other = Rel(0, 0) with { Target = them }, OtherStats = PlynlingStats.Compute(them, Array.Empty<TraitInfo>()) };
var mine = vs.Stat(PlynlingStat.Courage);
var theirs = vs.OtherStats!.Single(l => l.Stat == PlynlingStat.Courage).Total;
Check(PlynlingEventEngine.Chance(new EventChallenge(PlynlingStat.Courage, 0, VsTarget: true), vs) == Math.Clamp(50 + 5 * (mine - theirs), 5, 95), "a challenge against the other's stat");

// follow-up delay
var fu = new FollowUp("x", 24, 48);
for (var i = 1; i <= 200; i++)
{
    var due = PlynlingEventEngine.FollowUpAt(i, fu, noon);
    Check(due >= noon.AddHours(24) && due <= noon.AddHours(48), $"follow-up {i} inside its range");
}

// alone and the new effects
var plain = PlynlingEvents.ByKey("grown_parcel")!;
Check(PlynlingEventEngine.AppliesWhenAlone(new FollowUp("x", 1, 2), plain) && PlynlingEventEngine.AppliesWhenAlone(new SetAffinityAtLeast(0), plain)
      && !PlynlingEventEngine.AppliesWhenAlone(new Heartbreak(), plain), "alone: follow-ups and relation lifts yes, heartbreak no");
var replyDef = new EventDef("rd", EventType.Response, new[] { PlynlingStage.Adult }, "R", "S", new[] { Opt("x") });
Check(PlynlingEventEngine.AppliesWhenAlone(new Heartbreak(), replyDef), "a response refused alone still breaks the declarer's heart");
```

Run — Expected: build errors.

- [ ] **Step 2: Catalog additions**

In `PlynlingEventCatalog.cs`:

```csharp
// What brings a triggered event (Helpers/PlynlingOnActions via PlynlingService.FlushOnActionsAsync).
// Not stored; new values at the end.
public enum OnAction { Adopted, BecameTeen, BecameAdult, AfterVisit, Bereaved, FellSick, Recovered }

// A response option's side, for deciding alone by acceptance.
public enum Stance { Neutral, Accept, Refuse }

// Brings a later event (same target), between MinHours and MaxHours from now, hashed.
public sealed record FollowUp(string EventKey, double MinHours, double MaxHours) : EventEffect;
// Asks the other Plynling's owner to answer with ResponseKey (a Response event on their card).
public sealed record AskTarget(string ResponseKey) : EventEffect;
// Lifts the pair's affinity to at least Value (never lowers it).
public sealed record SetAffinityAtLeast(int Value) : EventEffect;
// Makes the pair a couple — only if the visit rules still allow it when it resolves.
public sealed record Couple : EventEffect;
// A refused declaration: the one who declared (the event's target, in a response) is saddened and
// the pair loses PlynlingBonds.HeartbreakLoss.
public sealed record Heartbreak : EventEffect;

// Who the other Plynling is to this one, for target conditions and acceptance.
public sealed record TargetInfo(Plynling Target, PlynlingBond Bond, int Affinity, int Compatibility, bool EitherInCouple);
```

Append the new trailing members:
- `EventChallenge(PlynlingStat Stat, int Difficulty, bool VsTarget = false)`
- `EventOption(…, IReadOnlyDictionary<AiAxis, int> Ai, bool OwnerOnly = false, Stance Stance = Stance.Neutral)`
- `EventDef(…, int BreakLevel = 0, IReadOnlyDictionary<string, double>? WeightByModifier = null, OnAction? Trigger = null, Func<TargetInfo, bool>? TargetCondition = null)`
- `EventContext(Plynling Self, IReadOnlyList<TraitInfo> Traits, IReadOnlyList<StatLine> Stats, PlynlingStage Stage, TargetInfo? Other = null, IReadOnlyList<StatLine>? OtherStats = null)`

- [ ] **Step 3: Engine additions**

In `PlynlingEventEngine.cs`:

`Chance`:

```csharp
    public static int Chance(EventChallenge challenge, EventContext ctx)
    {
        var against = challenge.VsTarget && ctx.OtherStats is { } theirs
            ? theirs.Single(l => l.Stat == challenge.Stat).Total
            : challenge.Difficulty;
        return Math.Clamp(50 + 5 * (ctx.Stat(challenge.Stat) - against), 5, 95);
    }
```

`AloneOptions`: add `&& !o.OwnerOnly` to its `Where`.

`PickPulse`: add the trailing parameter `Func<EventDef, bool>? hasTarget = null` and change its target line to:

```csharp
            .Where(d => d.Target == TargetKind.None || (targetable.Contains(d.Target) && (hasTarget?.Invoke(d) ?? true)))
```

`AppliesWhenAlone`: add to its switch `Heartbreak => def.Type == EventType.Response,` — a responder refusing alone still breaks the heart of the one who declared, because *that* owner chose to declare (declaring is owner-only); anywhere else a heartbreak never applies alone. Everything else new applies.

Add:

```csharp
    private const int FollowUpSalt = 340, ResponseSalt = 341, TriggeredSalt = 342;

    // How willing a responder is: affinity (−100…100) and hidden compatibility (−20…20) — CK3's
    // acceptance. Applied to Accept options; its inverse to Refuse ones.
    public static double AcceptWeight(int affinity, int compatibility) =>
        Math.Max(0.05, 1 + affinity / 50.0 + compatibility / 20.0);

    // A response decided alone: its axes, as any choice, leaned by acceptance.
    public static EventOption DecideResponse(EventDef def, EventContext ctx, int instanceId)
    {
        var axes = PlynlingPersonality.Axes(ctx.Traits);
        var accept = ctx.Other is { } o ? AcceptWeight(o.Affinity, o.Compatibility) : 1;
        var pool = AloneOptions(def, ctx)
            .Select(o => (Def: o, W: Math.Max(1.0, 100 + o.Ai.Sum(kv => kv.Value * axes[kv.Key] / 10.0))
                                     * (o.Stance == Stance.Accept ? accept : o.Stance == Stance.Refuse ? 1 / accept : 1)))
            .ToList();
        return Weighted(pool, StableRoll.Unit(instanceId, ResponseSalt, 0));
    }

    public static DateTimeOffset FollowUpAt(int instanceId, FollowUp f, DateTimeOffset now) =>
        now.AddHours(f.MinHours + StableRoll.Unit(instanceId, FollowUpSalt, 0) * (f.MaxHours - f.MinHours));

    // The event an on-action brings: among those triggered by it and eligible for this Plynling.
    public static EventDef? PickTriggered(OnAction trigger, int plynlingId, int salt, IEnumerable<EventDef> defs, EventContext ctx)
    {
        var pool = defs.Where(d => d.Trigger == trigger && Eligible(d, ctx)).Select(d => (Def: d, W: WeightOf(d, ctx))).Where(x => x.W > 0).ToList();
        return pool.Count == 0 ? null : Weighted(pool, StableRoll.Unit(plynlingId, salt, TriggeredSalt));
    }
```

- [ ] **Step 4: Run the harness and build**

Run both. Expected: `OK`; `Build succeeded`.

- [ ] **Step 5: Files for the owner to commit**

`PlynlingEventCatalog.cs`, `PlynlingEventEngine.cs` — `Plynling events: social engine — responses, follow-ups, owner-only`.

---

### Task 2: The content — big moments, on-actions, a chain

**Files:**
- Modify: `ProjectSYNCS/Helpers/PlynlingEvents.cs`
- Test: `$SCRATCH/personality/Program.cs`

**Interfaces:**
- Produces 17 events (keys below). `{T}` is replaced by the new trait name(s) in on-action events.

- [ ] **Step 1: Add the failing invariants**

```csharp
// ==== PHASE 4 — content ======================================================================================
foreach (var e in PlynlingEvents.All)
{
    Check(e.Options.Any(o => !o.OwnerOnly && o.Gate is null && o.StressCosts.Count == 0), $"{e.Key}: an option deciding alone can take");
    foreach (var o in e.Options)
    {
        var effects = o.OnSuccess.Concat(o.OnFailure).ToList();
        foreach (var ask in effects.OfType<AskTarget>())
            Check(PlynlingEvents.ByKey(ask.ResponseKey) is { Type: EventType.Response }, $"{e.Key}/{o.Key}: asks a real response");
        foreach (var f in effects.OfType<FollowUp>())
            Check(PlynlingEvents.ByKey(f.EventKey) is { Type: EventType.FollowUp } && f.MinHours <= f.MaxHours, $"{e.Key}/{o.Key}: a real follow-up");
        Check(e.Target != TargetKind.None || !effects.Any(x => x is AskTarget or SetAffinityAtLeast or Couple or Heartbreak), $"{e.Key}/{o.Key}: relation effects need a target");
        Check(!effects.OfType<Heartbreak>().Any() || e.Type == EventType.Response, $"{e.Key}/{o.Key}: heartbreak only in a response");
        Check(!effects.OfType<AskTarget>().Any() || o.OwnerOnly || e.Key is "social_pact" or "social_mend", $"{e.Key}/{o.Key}: risky asks are owner-only");
    }
    if (e.Type == EventType.Response)
        Check(e.Options.Any(o => o.Stance == Stance.Accept) && e.Options.Any(o => o.Stance == Stance.Refuse), $"{e.Key}: a response can accept and refuse");
    Check((e.Trigger is null) || e.Type == EventType.Triggered, $"{e.Key}: on-action events are triggered");
    Check(!e.Scene.Contains("{T}") || e.Trigger is OnAction.Adopted or OnAction.BecameTeen or OnAction.BecameAdult, $"{e.Key}: {{T}} only in trait reveals");
}
foreach (var trigger in Enum.GetValues<OnAction>())
    Check(PlynlingEvents.All.Any(e => e.Trigger == trigger), $"an event for {trigger}");
```

Also extend the phase-2 expansion loop so `{T}` expands: in that loop change `PlynlingEvents.Expand(text, "Lila", ga, "Pwet", gb)` to `PlynlingEvents.Expand(text, "Lila", ga, "Pwet", gb, "Courageuse")`.

Run — Expected: build error (`Expand` has no trait parameter) and missing events.

- [ ] **Step 2: `Expand` takes the trait text**

In `PlynlingEvents.cs`:

```csharp
    // Event text, expanded: names in bold, agreements resolved, {T} the new trait(s) in bold. Names must
    // already be safe (PlynlingCardUi.SafeName).
    public static string Expand(string template, string a, PlynlingGender ga, string? b = null, PlynlingGender gb = PlynlingGender.Male,
        string? traits = null) =>
        PlynlingVisitStory.Expand(template.Replace("{T}", traits is null ? "" : $"**{traits}**"), a, ga, b ?? "", gb);
```

(`{T}` is replaced first, so a trait name is never read as a template.)

- [ ] **Step 3: Helpers for the content**

Add to `PlynlingEvents` (with `Plain` and `Try`):

```csharp
    private static EventOption Ask(string key, string label, string outcome, string responseKey, Dictionary<AiAxis, int> ai,
        bool ownerOnly = true, EventChallenge? challenge = null, string? failure = null,
        IReadOnlyList<EventEffect>? onSuccess = null, IReadOnlyList<EventEffect>? onFailure = null) =>
        new(key, label, outcome, failure, null, challenge, onSuccess ?? E(new AskTarget(responseKey)), onFailure ?? Nothing,
            NoStress, ai, OwnerOnly: ownerOnly);

    private static EventOption Answer(string key, string label, string outcome, Stance stance, IReadOnlyList<EventEffect> effects,
        Dictionary<AiAxis, int> ai) =>
        new(key, label, outcome, null, null, null, effects, Nothing, NoStress, ai, Stance: stance);

    private static readonly PlynlingStage[] Teen = { PlynlingStage.Teen };   // already declared in phase 2 — reuse it
```

(Do not redeclare `Teen` if it exists; the line is shown only to name it.)

- [ ] **Step 4: The events**

Append to `All`, after the mental breaks:

```csharp
        // ---- the four big moments: the ask (pulse, social) and its answer (response)
        new EventDef("social_declare", EventType.Pulse, Grown, "Le cœur qui bat",
            "Depuis quelque temps, {A} rougit chaque fois que {B} passe. Même de loin. Même de dos.",
            new[]
            {
                Ask("declare", "Tout avouer à {B}", "{A} prend son courage à deux mains et va tout dire à {B}.", "reply_declare",
                    Ai((AiAxis.Boldness, 2), (AiAxis.Sociability, 1))),
                Plain("wait", "Garder ça pour soi encore un peu",
                    "{A} garde le secret, bien au chaud. Certaines choses mûrissent mieux à l'ombre.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Boldness, -1))),
            },
            Target: TargetKind.Known,
            TargetCondition: t => t.Bond == PlynlingBond.BestFriends && t.Affinity >= PlynlingBonds.ConfessFrom && !t.EitherInCouple,
            Condition: ctx => true),

        new EventDef("reply_declare", EventType.Response, Grown, "Une déclaration",
            "{B} est venu{b:|e} tout avouer à {A}, les joues rouges et la voix qui tremble.",
            new[]
            {
                Answer("yes", "Dire oui",
                    "{A} dit oui. {B} en oublie de respirer, puis rit, puis respire. Tout le village est au courant avant le soir.",
                    Stance.Accept, E(new Couple()), Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
                Answer("no", "Dire non, doucement",
                    "{A} dit non, avec beaucoup de douceur. {B} hoche la tête et rentre par le chemin le plus long.",
                    Stance.Refuse, E(new Heartbreak()), Ai((AiAxis.Honor, 1))),
            }),

        new EventDef("social_rival", EventType.Pulse, Grown, "Le défi",
            "{B} s'est encore vanté{b:|e} d'être le plus rapide du village. Devant tout le monde. Devant {A}.",
            new[]
            {
                Ask("challenge", "Le défier à la course jusqu'au vieux pont", "{A} lance le défi, et {B} relève le menton.", "reply_rival",
                    Ai((AiAxis.Boldness, 2), (AiAxis.Vengefulness, 1))),
                Plain("ignore", "Hausser les épaules",
                    "{A} hausse les épaules si haut que ça devient un exercice. Personne n'a gagné, mais personne n'a perdu.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Hostile),

        new EventDef("reply_rival", EventType.Response, Grown, "Le défi relevé",
            "{B} défie {A} à la course jusqu'au vieux pont. Le héron fait semblant de ne pas regarder.",
            new[]
            {
                new EventOption("race", "Courir", "{A} gagne d'une moustache. Le héron, qui ne regardait pas, applaudit.",
                    "{B} gagne d'une moustache. {A} réclame une revanche avant même d'avoir repris son souffle.",
                    null, new EventChallenge(PlynlingStat.Courage, 0, VsTarget: true),
                    E(new GrowStat(PlynlingStat.Courage), new AffinityShift(-5)), E(new AffinityShift(-10)),
                    NoStress, Ai((AiAxis.Boldness, 2)), Stance: Stance.Accept),
                Answer("decline", "Refuser, avec un sourire en coin",
                    "{A} refuse, et sourit en coin. {B} ne sait plus si c'est une victoire ou une défaite.",
                    Stance.Refuse, E(new AffinityShift(-5)), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("social_pact", EventType.Pulse, Grown, "Le pacte",
            "{A} et {B} ont partagé leur dernier gâteau, leur dernier secret et une averse entière sous la même feuille.",
            new[]
            {
                Ask("pact", "Proposer un pacte de meilleurs amis", "{A} propose un pacte, le petit doigt tendu.", "reply_pact",
                    Ai((AiAxis.Sociability, 2), (AiAxis.Honor, 1)), ownerOnly: false),
                Plain("enjoy", "Profiter de l'amitié comme elle est",
                    "{A} ne propose rien. Ce qui marche n'a pas besoin de contrat.",
                    E(new AffinityShift(5)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Known,
            TargetCondition: t => t.Bond == PlynlingBond.Friends),

        new EventDef("reply_pact", EventType.Response, Grown, "Le petit doigt",
            "{B} tend le petit doigt à {A} : un pacte de meilleurs amis, pour la vie, ou au moins jusqu'à jeudi.",
            new[]
            {
                Answer("yes", "Serrer le petit doigt", "Les deux petits doigts se serrent. C'est officiel, et le moineau sert de témoin.",
                    Stance.Accept, E(new SetAffinityAtLeast(PlynlingBonds.BestFriendsFrom + 5)), Ai((AiAxis.Sociability, 2))),
                Answer("later", "Dire « pas encore »", "{A} replie le doigt de {B}, gentiment. « Pas encore. Mais garde-le tendu. »",
                    Stance.Refuse, E(new AffinityShift(-5)), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("social_mend", EventType.Pulse, Grown, "Le rameau d'olivier",
            "{A} croise {B} au marché. Les deux font semblant de ne pas se voir, avec beaucoup d'application.",
            new[]
            {
                Ask("mend", "Proposer de faire la paix", "{A} tend un rameau, un vrai, cueilli exprès.", "reply_mend",
                    Ai((AiAxis.Compassion, 2), (AiAxis.Vengefulness, -2)), ownerOnly: false),
                Plain("snub", "Continuer à faire semblant",
                    "{A} fait semblant si bien que {B} y croit presque. Le marchand de baies, lui, n'y croit pas du tout.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Vengefulness, 1))),
            },
            Target: TargetKind.Hostile),

        new EventDef("reply_mend", EventType.Response, Grown, "La paix ?",
            "{B} tend un rameau à {A}, cueilli exprès. Le marché entier retient son souffle.",
            new[]
            {
                Answer("yes", "Accepter le rameau", "{A} prend le rameau. On ne devient pas amis en un jour, mais on peut arrêter de se fâcher.",
                    Stance.Accept, E(new SetAffinityAtLeast(0)), Ai((AiAxis.Compassion, 2))),
                Answer("no", "Le laisser tomber", "Le rameau tombe dans la poussière. {B} le ramasse et repart avec, l'air digne.",
                    Stance.Refuse, E(new AffinityShift(-5)), Ai((AiAxis.Vengefulness, 2))),
            }),

        // ---- on-actions
        new EventDef("on_welcome", EventType.Triggered, Baby, "Bienvenue",
            "{A} ouvre les yeux sur sa nouvelle maison. Tout est grand, tout sent bon. Déjà, on devine un petit caractère : {T}.",
            new[]
            {
                Plain("explore", "Explorer chaque coin", "{A} inspecte chaque coin, renifle chaque meuble, et adopte officiellement le coussin du fond.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Boldness, 1))),
                Plain("nap", "Faire une première sieste", "{A} choisit un rayon de soleil et s'y endort, comme si l'endroit avait toujours été là pour ça.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Energy, -1))),
            },
            Trigger: OnAction.Adopted),

        new EventDef("on_teen", EventType.Triggered, Teen, "Plus tout à fait petit",
            "Un matin, {A} ne tient plus dans son ancien coin préféré. Il se passe quelque chose : {T}, voilà ce que devient {A}.",
            new[]
            {
                Plain("proud", "Bomber le torse", "{A} se mesure contre la porte et grave un trait, très haut, en trichant un peu.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Boldness, 1))),
                Plain("shy", "Faire comme si de rien n'était", "{A} garde ses anciennes habitudes encore un peu. Grandir, ça peut attendre l'après-midi.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Boldness, -1))),
            },
            Trigger: OnAction.BecameTeen),

        new EventDef("on_adult", EventType.Triggered, Grown, "Grand, maintenant",
            "{A} a fini de grandir. Sur ses traits se lit maintenant un dernier trait de caractère : {T}.",
            new[]
            {
                Plain("party", "Fêter ça au café", "La tortue du café offre une part de gâteau, et dit « déjà ? » trois fois.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 1))),
                Plain("plan", "Faire des projets", "{A} sort un carnet et y écrit « projets ». Puis, en dessous : « à voir ».",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1))),
            },
            Trigger: OnAction.BecameAdult),

        new EventDef("on_visit_forgot", EventType.Triggered, Any, "L'objet oublié",
            "Après la visite, {A} trouve un petit mouchoir brodé sous un coussin. C'est celui de {B}.",
            new[]
            {
                Plain("return", "Le rapporter tout de suite", "{A} court le rapporter. {B} ne s'était même pas rendu compte de l'oubli, et en est {b:touché|touchée}.",
                    E(new AffinityShift(5)), Ai((AiAxis.Honor, 1), (AiAxis.Energy, 1))),
                Plain("keep", "Le garder pour la prochaine fois", "{A} plie le mouchoir et le pose bien en vue. Il faudra bien une prochaine fois.",
                    E(new FollowUp("follow_forgot", 24, 72)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Anyone, Trigger: OnAction.AfterVisit),

        new EventDef("follow_forgot", EventType.FollowUp, Any, "Le mouchoir brodé",
            "Le mouchoir de {B} attend toujours sur l'étagère de {A}. Il commence à sentir la maison.",
            new[]
            {
                Plain("bring", "Le rendre, enfin", "{A} rend le mouchoir, plié avec soin, et une fleur glissée dedans. {B} fait comme si c'était prévu.",
                    E(new AffinityShift(10), new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Honor, 1))),
                Plain("adopt", "Décider qu'il est à soi maintenant", "{A} adopte le mouchoir. {B} le reconnaîtra un jour, mais ce jour n'est pas aujourd'hui.",
                    E(new GrowStat(PlynlingStat.Intrigue), new AffinityShift(-5)), Ai((AiAxis.Greed, 1))),
            },
            Target: TargetKind.Anyone),

        new EventDef("on_bereaved", EventType.Triggered, Any, "Une place vide",
            "La place de {B} est vide, et le village est un peu plus grand sans. {A} ne sait pas trop quoi faire de ses pattes.",
            new[]
            {
                Plain("remember", "Se souvenir des bons moments", "{A} raconte à voix haute leurs meilleures bêtises. Le héron écoute, pour une fois sans faire semblant.",
                    E(new StressChange(-10)), Ai((AiAxis.Sociability, 1))),
                Plain("plant", "Planter une graine en sa mémoire", "{A} plante une graine près du vieux pont. Au printemps, il y aura quelque chose.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Anyone, Trigger: OnAction.Bereaved),

        new EventDef("on_sick", EventType.Triggered, Any, "Le nez qui coule",
            "{A} éternue, renifle, et décide que le monde est injuste.",
            new[]
            {
                Plain("tea", "Réclamer une tisane", "La tortue du café envoie une tisane au miel, avec un mot : « Bois-la chaude. »",
                    E(new StressChange(-5)), Ai((AiAxis.Sociability, 1))),
                Plain("brave", "Faire comme si de rien n'était", "{A} fait comme si de rien n'était, entre deux éternuements spectaculaires.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Boldness, 1))),
            },
            Trigger: OnAction.FellSick),

        new EventDef("on_recovered", EventType.Triggered, Any, "Remis sur pattes",
            "{A} se réveille, respire par le nez — par les deux narines — et se sent {a:neuf|neuve}.",
            new[]
            {
                Plain("run", "Faire le tour du village en courant", "{A} court partout, juste pour vérifier que tout marche encore. Tout marche.",
                    E(new ApplyModifier("fired_up")), Ai((AiAxis.Energy, 2))),
                Plain("thank", "Remercier ceux qui ont aidé", "{A} distribue des remerciements, et une tisane à la tortue, pour changer.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Compassion, 1))),
            },
            Trigger: OnAction.Recovered),
```

Notes:
- `Any` is the all-stages array added in phase 3. `Grown` and `Baby` exist from phase 2.
- `social_declare` declares `Condition: ctx => true` only to show where a self-condition goes; drop it if the compiler warns.
- `{b:|e}` and `{b:touché|touchée}` agree with the other Plynling; `{a:neuf|neuve}` with this one.

- [ ] **Step 5: Run the harness**

Run: `cd "$SCRATCH/personality" && dotnet run`
Expected: `OK`. A banned-word failure prints the expanded text — reword it (e.g. « Il commence à sentir » → « Le mouchoir commence à sentir »).

- [ ] **Step 6: Build**

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror` — Expected: `Build succeeded`.

- [ ] **Step 7: Files for the owner to commit**

`ProjectSYNCS/Helpers/PlynlingEvents.cs` — `Plynling events: big moments, on-actions, a chain`.

---

### Task 3: The service — responses, follow-ups, relation effects, on-actions

**Files:**
- Modify: `ProjectSYNCS/Services/PlynlingService.cs` (logger; on-action hooks in `AdoptAsync`, `VisitAsync`, `ProgressAsync`, `GrieveForAsync`, `FlushMomentsAsync`; `InCoupleAsync` stays private and is reused)
- Modify: `ProjectSYNCS/Services/PlynlingService.Events.cs`
- Modify: `ProjectSYNCS/Services/PlynlingSweepService.cs`

**Interfaces:**
- Produces: `PlynlingService(AppDbContext db_context, ILogger<PlynlingService> logger)`; `FlushOnActionsAsync(DateTimeOffset now) -> Task<IReadOnlyList<int>>` (creates queued on-action events, each in its own save, swallow-and-log; returns ids resolved at once, for the mascot); `GetEventContextAsync(Plynling p, DateTimeOffset now, Plynling? other = null)`; `PickEventAsync` returns `EventPick(Outcome, PendingLeft, IReadOnlyList<int> Told)`; `TickEventsAsync` also resolves the mascot's pending responses and returns them.

- [ ] **Step 1: A logger for the service**

In `PlynlingService.cs`, change the constructor:

```csharp
    private readonly AppDbContext _db_context;
    private readonly ILogger<PlynlingService> _logger;

    public PlynlingService(AppDbContext db_context, ILogger<PlynlingService> logger)
    {
        _db_context = db_context;
        _logger = logger;
    }
```

(add `using Microsoft.Extensions.Logging;`). It is resolved by DI everywhere; `grep -rn "new PlynlingService(" ProjectSYNCS` must find nothing.

- [ ] **Step 2: The on-action queue**

Append to `PlynlingService.Events.cs`:

```csharp
    // On-actions noticed during a unit of work, created after its save (FlushOnActionsAsync): an event
    // must never be what breaks an adoption, a visit or a sweep.
    private readonly List<(OnAction Kind, int PlynlingId, int? TargetId, string? TraitText)> _onActions = new();

    private void QueueOnAction(OnAction kind, Plynling p, int? targetId = null, string? traitText = null) =>
        _onActions.Add((kind, p.Id, targetId, traitText));

    // Each queued on-action in its own save and its own try. Returns the instances the mascot answered at once.
    public async Task<IReadOnlyList<int>> FlushOnActionsAsync(DateTimeOffset now)
    {
        var told = new List<int>();
        var queued = _onActions.ToList();
        _onActions.Clear();
        foreach (var (kind, plynlingId, targetId, traitText) in queued)
        {
            try
            {
                var p = await GetByIdAsync(plynlingId, now);
                if (p is null || p.DiedAt is not null || p.FrozenAt is not null) continue;
                // A visit brings its event only sometimes (hashed, a third of the time).
                if (kind == OnAction.AfterVisit && StableRoll.Unit(p.Id, AppTime.DayKey(now), 350) >= 0.33) continue;
                var other = targetId is { } tid ? await GetByIdAsync(tid, now) : null;
                var ctx = await GetEventContextAsync(p, now, other);
                if (PlynlingEventEngine.PickTriggered(kind, p.Id, (int)(now.ToUnixTimeSeconds() % 1_000_000), PlynlingEvents.All, ctx) is not { } def) continue;
                var inst = await QueueEventAsync(p, def, def.Target == TargetKind.None ? null : targetId, now);
                inst.GainedTraitKey = traitText;
                await _db_context.SaveChangesAsync();
                if (PlynlingMascot.Is(p))
                {
                    await ResolveAloneAsync(p, inst, now);
                    await FlushMomentsAsync(p);
                    await _db_context.SaveChangesAsync();
                    told.Add(inst.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "On-action {Kind} for Plynling {PlynlingId} failed.", kind, plynlingId);
            }
        }
        return told;
    }
```

`GainedTraitKey` doubles as the `{T}` text for trait reveals (a comma-separated list of keys — see Task 4's story).

- [ ] **Step 3: The hooks**

In `PlynlingService.cs`:
- `AdoptAsync`, after the last save and before `return`: `QueueOnAction(OnAction.Adopted, plynling, traitText: await NewTraitKeysAsync(plynling, TraitKind.Childhood)); await FlushOnActionsAsync(now);`
- `VisitAsync`, after its save and before `return`: `QueueOnAction(OnAction.AfterVisit, visitor, host.Id); QueueOnAction(OnAction.AfterVisit, host, visitor.Id); await FlushOnActionsAsync(now);` (the 1-in-3 roll is per Plynling and day, inside the flush).
- `ProgressAsync`: where it writes a `GrewUp` moment for `Teen` or `Adult`, remember it; after `await EnsureTraitsAsync(p, now);`:

```csharp
        if (grewTo == PlynlingStage.Teen) QueueOnAction(OnAction.BecameTeen, p, traitText: await NewTraitKeysAsync(p, TraitKind.Personality, 2));
        if (grewTo == PlynlingStage.Adult) QueueOnAction(OnAction.BecameAdult, p, traitText: await NewTraitKeysAsync(p, TraitKind.Personality, 1));
```

  with `PlynlingStage? grewTo = null;` declared before the stage loop and `grewTo = stage;` set right after the `AddMomentAsync(p, JournalKind.GrewUp, …)` call (an older Plynling backfilling several stages at once keeps the last one).
- `GrieveForAsync`: add a parameter `bool died = true`; inside the loop, after `AddMomentAsync(mourner, JournalKind.Grieving, …)`: `if (died) QueueOnAction(OnAction.Bereaved, mourner, gone.Id);`. In the abandonment caller pass `died: false` (the row is deleted; no event may point at it).
- `FlushMomentsAsync`: inside the loop, `if (kind == JournalKind.FellSick) QueueOnAction(OnAction.FellSick, p); if (kind == JournalKind.Recovered) QueueOnAction(OnAction.Recovered, p);`.

Add the helper:

```csharp
    // The newest traits of a kind, as "key,key" — the {T} of a trait reveal. Counts traits added in
    // this unit of work. Null when there is none.
    private async Task<string?> NewTraitKeysAsync(Plynling p, TraitKind kind, int count = 1)
    {
        var keys = (await _db_context.PlynlingTraits.Where(t => t.PlynlingId == p.Id && t.Kind == kind).OrderBy(t => t.Id).Select(t => t.Key).ToListAsync())
            .Concat(_db_context.PlynlingTraits.Local.Where(t => t.PlynlingId == p.Id && t.Kind == kind && t.Id == 0).Select(t => t.Key))
            .Distinct().TakeLast(count).ToList();
        return keys.Count == 0 ? null : string.Join(",", keys);
    }
```

In `PlynlingSweepService.SweepAsync`, after the `foreach (var instanceId in told)` block (still inside the per-item `try`):

```csharp
                foreach (var instanceId in await plynlings.FlushOnActionsAsync(now))
                    if (await plynlings.GetEventStoryAsync(instanceId, now) is { } story)
                        await _announcer.PostEventStoryAsync(plynling.GuildId, PlynlingEventCards.BuildStory(story, 0));
```

(Stage-ups, sickness and deaths queue during the sweep's unit of work; this creates their events after its saves.)

- [ ] **Step 4: The context with the other Plynling**

Replace `GetEventContextAsync` with:

```csharp
    public async Task<EventContext> GetEventContextAsync(Plynling p, DateTimeOffset now, Plynling? other = null)
    {
        var traits = await GetTraitsAsync(p);
        var ctx = new EventContext(p, traits, PlynlingStats.Compute(p, traits), PlynlingLife.Stage(p, now));
        if (other is null) return ctx;
        var (lo, hi) = p.Id < other.Id ? (p.Id, other.Id) : (other.Id, p.Id);
        var relation = await _db_context.PlynlingRelations.FirstOrDefaultAsync(r => r.PlynlingAId == lo && r.PlynlingBId == hi);
        var info = new TargetInfo(other, relation?.Bond ?? PlynlingBond.Acquaintances, relation?.Affinity ?? 0,
            PlynlingBonds.Compatibility(lo, hi), await InCoupleAsync(p.Id, other.Id) || await InCoupleAsync(other.Id, p.Id));
        var otherTraits = await GetTraitsAsync(other);
        return ctx with { Other = info, OtherStats = PlynlingStats.Compute(other, otherTraits) };
    }
```

and pass the target wherever a context is built for an instance that has one: in `PickEventAsync` and `ResolveAloneAsync`, load `var other = inst.TargetPlynlingId is { } tid ? await GetByIdAsync(tid, now) : null;` first and call `GetEventContextAsync(p, now, other)`. In the handler's `OnOpenAsync` (phase 2), pass the target it already loads.

In `ResolveAloneAsync`, decide a response by acceptance:

```csharp
        var option = def.Type == EventType.Response
            ? PlynlingEventEngine.DecideResponse(def, ctx, inst.Id)
            : PlynlingEventEngine.DecideAlone(def, ctx, inst.Id);
        await ApplyEventAsync(p, inst, def, option, ctx, decidedAlone: true, now);
```

- [ ] **Step 5: Target conditions in the pulse**

In `TickEventsAsync`, replace the `PickPulse` call with one that checks each social event's condition against its candidates:

```csharp
                var infos = new Dictionary<int, TargetInfo>();
                foreach (var id in targets.Values.SelectMany(v => v).Distinct())
                    if (await GetByIdAsync(id, now) is { } t)
                        infos[id] = (await GetEventContextAsync(p, now, t)).Other!;
                IReadOnlyList<int> CandidatesFor(EventDef d) =>
                    targets[d.Target].Where(id => infos.TryGetValue(id, out var info) && (d.TargetCondition?.Invoke(info) ?? true)).ToList();
                if (PlynlingEventEngine.PickPulse(p.Id, day, defs, ctx, recent, targetable, d => CandidatesFor(d).Count > 0) is { } def)
                {
                    var targetId = def.Target == TargetKind.None ? null : PlynlingEventEngine.PickTarget(p.Id, day, CandidatesFor(def));
```

(the rest of the block is unchanged).

- [ ] **Step 6: The new effects in `ApplyEventAsync`**

Add to the `switch` in `ApplyEventAsync` (phase 3's version):

```csharp
                case FollowUp f when PlynlingEvents.ByKey(f.EventKey) is { } next:
                    var later = await QueueEventAsync(p, next, inst.TargetPlynlingId, now);
                    later.AvailableAt = PlynlingEventEngine.FollowUpAt(inst.Id, f, now);
                    later.ExpiresAt = later.AvailableAt + PlynlingEventEngine.Lifetime;
                    later.ParentInstanceId = inst.Id;
                    break;
                case AskTarget ask when target is { DiedAt: null, FrozenAt: null } && PlynlingEvents.ByKey(ask.ResponseKey) is { } reply:
                    // Queued on the other Plynling, pointing back at this one.
                    var asked = await QueueEventAsync(target, reply, p.Id, now);
                    asked.ParentInstanceId = inst.Id;
                    _askedMascot |= PlynlingMascot.Is(target);
                    break;
                case SetAffinityAtLeast lift when target is { DiedAt: null }:
                    var current = ctx.Other?.Affinity ?? 0;
                    if (lift.Value > current)
                    {
                        var (b0, b1) = await ShiftAffinityAsync(p, target, lift.Value - current, now);
                        inst.BondBefore ??= b0;
                        inst.BondAfter = b1;
                    }
                    break;
                case Couple when target is { DiedAt: null } && ctx.Other is { } rel
                                 && PlynlingBonds.CanConfess(rel.Bond, rel.Affinity, p, target, rel.EitherInCouple):
                    var (c0, c1) = await MakeCoupleAsync(p, target, now);
                    inst.BondBefore ??= c0;
                    inst.BondAfter = c1;
                    break;
                case Heartbreak when target is { DiedAt: null }:
                    // In a response: the one who declared — whose owner chose to — is saddened.
                    PlynlingLife.Sadden(target, now, PlynlingBonds.HeartbreakSadness);
                    await AddMomentAsync(target, JournalKind.Heartbroken, p.Name, now);
                    var (h0, h1) = await ShiftAffinityAsync(p, target, -PlynlingBonds.HeartbreakLoss, now);
                    inst.BondBefore ??= h0;
                    inst.BondAfter = h1;
                    break;
```

with a field and a helper:

```csharp
    // Set when an event asked the mascot: she answers right after the save (PickEventAsync, TickEventsAsync).
    private bool _askedMascot;

    // The visit's accepted confession, as an effect: lovers, both journals, the badge.
    private async Task<(PlynlingBond Before, PlynlingBond After)> MakeCoupleAsync(Plynling a, Plynling b, DateTimeOffset now)
    {
        var (lo, hi) = a.Id < b.Id ? (a.Id, b.Id) : (b.Id, a.Id);
        var relation = await _db_context.PlynlingRelations.FirstAsync(r => r.PlynlingAId == lo && r.PlynlingBId == hi);
        var before = relation.Bond;
        relation.Bond = PlynlingBond.Lovers;
        relation.Since = now;
        var evt = await ApplyBondChangeAsync(a, b, before, PlynlingBond.Lovers, now);
        await AwardAsync(a, now, evt);
        await AwardAsync(b, now, evt);
        return (before, PlynlingBond.Lovers);
    }
```

`Couple` only applies with a relation row, which `CanConfess` (best friends) guarantees. A response whose `Couple` no longer applies (someone coupled meanwhile) leaves `BondAfter` null — Task 4's story says « trop tard ».

- [ ] **Step 7: The mascot answers at once**

Change `EventPick` to `public sealed record EventPick(EventPickOutcome Outcome, int PendingLeft = 0, IReadOnlyList<int>? Told = null);` and, in `PickEventAsync` after its save:

```csharp
        var told = new List<int> { instanceId };
        told.AddRange(await AnswerForMascotAsync(instanceId, now));
        return new(EventPickOutcome.Done, await CountPendingEventsAsync(p, now), told);
```

Add:

```csharp
    // Responses queued on the mascot by this event: she answers in character, now. Saves.
    private async Task<IReadOnlyList<int>> AnswerForMascotAsync(int parentId, DateTimeOffset now)
    {
        if (!_askedMascot) return Array.Empty<int>();
        _askedMascot = false;
        var answered = new List<int>();
        foreach (var reply in await _db_context.PlynlingEventInstances.Where(i => i.ParentInstanceId == parentId && i.ResolvedAt == null).ToListAsync())
        {
            var mascot = await GetByIdAsync(reply.PlynlingId, now);
            if (mascot is null || !PlynlingMascot.Is(mascot)) continue;
            await ResolveAloneAsync(mascot, reply, now);
            answered.Add(reply.Id);
        }
        await _db_context.SaveChangesAsync();
        return answered;
    }
```

In `TickEventsAsync`, after each `ResolveAloneAsync(p, inst, now)` (expiry and the mascot's pulse), and after the final save, collect: `foreach (var id in told.ToList()) told.AddRange(await AnswerForMascotAsync(id, now));`.

In the phase-2 handler `OnPickAsync`, post every story in `pick.Told` (not only the clicked one):

```csharp
        foreach (var toldId in pick.Told ?? new[] { id })
            if (await _plynlings.GetEventStoryAsync(toldId, now) is { } s)
                await _announcer.PostEventStoryAsync(Context.Guild?.Id ?? 0, PlynlingEventCards.BuildStory(s, 0), Context.Channel);
```

(replacing its single post).

- [ ] **Step 8: A response whose asker is gone**

In `TickEventsAsync`'s expiry loop and in `PickEventAsync`, a `Response` instance whose `TargetPlynlingId` is null (the asker was abandoned) is cancelled rather than resolved: add before resolving — `if (def.Type == EventType.Response && inst.TargetPlynlingId is null) { inst.CancelledAt = now; continue; }` (in `PickEventAsync`: return `Gone` after setting `CancelledAt` and saving).

- [ ] **Step 9: Build and run the harness**

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror` then `cd "$SCRATCH/personality" && dotnet run`
Expected: `Build succeeded`; `OK`.

- [ ] **Step 10: Files for the owner to commit**

`PlynlingService.cs`, `PlynlingService.Events.cs`, `PlynlingSweepService.cs`, `Interactions/Components/PlynlingEventHandler.cs` — `Plynling events: responses, follow-ups, on-actions`.

---

### Task 4: Stories and cards for social events

**Files:**
- Modify: `ProjectSYNCS/Helpers/PlynlingEventStory.cs`
- Modify: `ProjectSYNCS/Commands/PlynlingEventCards.cs`
- Modify: `ProjectSYNCS/Services/PlynlingService.Events.cs` (`GetEventStoryAsync` passes the parent's title)
- Test: `$SCRATCH/personality/Program.cs`

**Interfaces:**
- Produces: `PlynlingEventStory.Build(PlynlingEventInstance inst, EventCast self, EventCast? target, string? parentTitle = null, bool answered = false)`; `{T}` expanded from `GainedTraitKey` for trait reveals; « Suite de … » on page 1; « ⏳ … attend la réponse de … » when asked and unanswered; « trop tard » when a `Couple` did not apply; « contre {B} » on a stat-against-stat chance.

- [ ] **Step 1: Add the failing checks**

```csharp
// ==== PHASE 4 — stories ======================================================================================
var ask = new PlynlingEventInstance { Id = 11, PlynlingId = 50, EventKey = "social_pact", OptionKey = "pact", ResolvedAt = noon, TargetPlynlingId = 51 };
Check(PlynlingEventStory.Build(ask, lila, pwet).Pages[^1].Contains("attend la réponse"), "an unanswered ask says so");
Check(!PlynlingEventStory.Build(ask, lila, pwet, answered: true).Pages[^1].Contains("attend la réponse"), "an answered one does not");
var late = new PlynlingEventInstance { Id = 12, PlynlingId = 51, EventKey = "reply_declare", OptionKey = "yes", ResolvedAt = noon, TargetPlynlingId = 50, ParentInstanceId = 11 };
Check(PlynlingEventStory.Build(late, pwet, lila, parentTitle: "Le cœur qui bat").Pages[^1].Contains("trop tard"), "a couple that could not form says it came too late");
Check(PlynlingEventStory.Build(late, pwet, lila, parentTitle: "Le cœur qui bat").Pages[0].Contains("Suite de"), "a follow-on names its origin");
var reveal = new PlynlingEventInstance { Id = 13, PlynlingId = 50, EventKey = "on_teen", OptionKey = "proud", ResolvedAt = noon, GainedTraitKey = "brave,shy" };
Check(PlynlingEventStory.Build(reveal, lila, null).Pages[0].Contains("Courageuse et Timide"), "{T}: the new traits, in her gender");
var race = new PlynlingEventInstance { Id = 14, PlynlingId = 51, EventKey = "reply_rival", OptionKey = "race", ResolvedAt = noon, TargetPlynlingId = 50,
    ChancePercent = 40, ChallengeSucceeded = false };
Check(PlynlingEventStory.Build(race, pwet, lila).Pages.Any(p => p.Contains("contre **Lila**")), "a stat duel names the rival");
```

Run — Expected: build error (the new `Build` parameters).

- [ ] **Step 2: The story**

In `PlynlingEventStory.cs`, change `Build`'s signature to `Build(PlynlingEventInstance inst, EventCast self, EventCast? target, string? parentTitle = null, bool answered = false)` and its local `X` to pass the traits:

```csharp
        var traitText = inst.GainedTraitKey is { } keys && def.Trigger is OnAction.Adopted or OnAction.BecameTeen or OnAction.BecameAdult
            ? string.Join(" et ", keys.Split(',').Select(PlynlingTraits.ByKey).OfType<TraitInfo>().Select(t => t.Name(self.Gender)))
            : null;
        string X(string t) => PlynlingEvents.Expand(t, self.Name, self.Gender, target?.Name ?? "quelqu'un", target?.Gender ?? PlynlingGender.Male, traitText);
        var pages = new List<string> { (parentTitle is null ? "" : $"-# Suite de « {parentTitle} »\n") + X(def.Scene) };
```

In the challenge page, name the rival on a duel:

```csharp
            pages.Add($"🎲 **{PlynlingStats.Name(c.Stat)}**{(c.VsTarget && target is not null ? $" contre **{target.Name}**" : "")} — {chance} % de chances… " +
                      (inst.ChallengeSucceeded == true ? "**réussi !**" : "**raté.**"));
```

Change the last line to `pages.Add(OutcomeText(inst, self, target, answered));`, and in `OutcomeText` (new parameter `bool answered = false`), before the return:

```csharp
        var effects = (success ? option.OnSuccess : option.OnFailure).ToList();
        if (effects.OfType<AskTarget>().Any() && target is not null && !answered)
            lines.Add($"-# ⏳ **{self.Name}** attend la réponse de **{target.Name}**.");
        if (effects.OfType<Couple>().Any() && inst.BondAfter != PlynlingBond.Lovers && target is not null)
            lines.Add($"-# Hélas, trop tard : l'un des deux a déjà quelqu'un.");
```

Pass `traitText` the same way in `OutcomeText`'s `X` (compute it the same way at its top).

- [ ] **Step 3: Loading a story with its context**

In `GetEventStoryAsync`:

```csharp
        var parentTitle = inst.ParentInstanceId is { } pid && await GetEventInstanceAsync(pid) is { } parent
            ? PlynlingEvents.ByKey(parent.EventKey)?.Title : null;
        var answered = await _db_context.PlynlingEventInstances.AnyAsync(i => i.ParentInstanceId == inst.Id && i.ResolvedAt != null);
        return PlynlingEventStory.Build(inst, EventCast.Of(self, now), target is null ? null : EventCast.Of(target, now), parentTitle, answered);
```

- [ ] **Step 4: The choice card**

In `PlynlingEventCards.BuildChoice`, labels may contain `{B}` (« Tout avouer à {B} »): expand them — the button text with `PlynlingEvents.Expand(o.Label, …)` stripped of `**` (`.Replace("**", "")`), truncated to 80 characters; the option line uses the expanded label as is. In `Details`, a duel reads `🎲 Courage : 40 % contre {B}` — append `contre {target.Name}` when `c.VsTarget` (pass `target` into `Details`). Owner-only options get no marker (they are the owner's to take; deciding alone simply never takes them).

Add to the phase-2 card harness loop, so it builds every event with a target name of maximal length (it already uses `longName` for `self`; pass `longName` as target too for social events) and checks every button label ≤ 80 after expansion:

```csharp
    foreach (var o in e.Options)
        Check(PlynlingEvents.Expand(o.Label, longName.Name, longName.Gender, longName.Name, longName.Gender).Replace("**", "").Length <= 80
              || e.Target != TargetKind.None, $"{e.Key}/{o.Key}: label fits (the card truncates social ones)");
```

- [ ] **Step 5: Run the harness and build**

Run both. Expected: `OK`; `Build succeeded`.

- [ ] **Step 6: Files for the owner to commit**

`PlynlingEventStory.cs`, `PlynlingEventCards.cs`, `PlynlingService.Events.cs` — `Plynling events: social stories`.

---

### Task 5: `/debug event` with a target, help, docs, version, dev-guild test

**Files:**
- Modify: `ProjectSYNCS/Commands/DebugModule.cs` (`target` option)
- Modify: `ProjectSYNCS/Commands/PlynlingModule.cs` (help page 1)
- Modify: `README.md`, `docs/agents/plynling-events.md`, `ProjectSYNCS/config.yaml`

- [ ] **Step 1: A chosen target**

In `DebugModule.EventAsync`, add a parameter `[Summary("target", "Le propriétaire de l'autre Plynling")] IUser? target = null`, and use it for social events: `var targetId = def.Target == TargetKind.None ? null : target is not null ? (await _plynlings.GetCurrentAsync(Context.Guild.Id, target.Id, now))?.Id : await _plynlings.AnyOtherLivingIdAsync(p);`. A `Response` key forced this way is created on **your** Plynling with the target as the asker — a quick way to test answering.

- [ ] **Step 2: Help page 1**

In `BuildPersonalityHelp`, extend the « Événements » field with one more sentence (measure with the harness):

```csharp
                "Certains concernent un autre Plynling : une déclaration, un défi, un pacte, une réconciliation — " +
                "son propriétaire reçoit alors la question sur sa propre carte.\n" +
```

inserted before « Sans réponse en 24 h… ».

Run: `cd "$SCRATCH/personality" && dotnet run` — Expected: `OK` (page 1 under 6000).

- [ ] **Step 3: Docs**

`README.md`, after « Stress and moods »:

```markdown
- **Together:** some events involve another Plynling — a declaration, a race challenge, a best-friend
  pact, making peace. The other owner answers from their own card (or their Plynling answers in
  character after a day). Events also follow life: a welcome on adoption, growing up, an object left
  behind after a visit, a friend's passing, falling sick, getting better — and some come back days later.
```

`docs/agents/plynling-events.md`, append:

```markdown
## Social events, follow-ups, on-actions

- **Responses** (`AskTarget`) are `Response` instances on the *other* Plynling, pointing back
  (`TargetPlynlingId` = the asker, `ParentInstanceId` = the ask). Decided alone by acceptance
  (`AcceptWeight`: affinity, compatibility). The mascot answers right after the save
  (`AnswerForMascotAsync`).
- **Anti-griefing is in the effect types:** another owner's Plynling is touched only through the
  relation. The one exception, `Heartbreak`, saddens the one who *declared* — declaring is
  `OwnerOnly`, so it is always that owner's choice. **`OwnerOnly` options are never picked alone.**
- **Couples** reuse the visit rules (`CanConfess`), checked when asked (target condition) and again
  when answered; a couple formed meanwhile makes the story say « trop tard ».
- **Follow-ups** are instances with a future `AvailableAt`; "open" includes them, so a death cancels them.
- **On-actions** are queued during a unit of work and created after its save by
  `FlushOnActionsAsync`, each in its own save and `try` — never what breaks an adoption, a visit or a
  sweep. `{T}` (trait reveals) comes from the instance's `GainedTraitKey` ("key,key").
- Social pulses check `TargetCondition` against each candidate before an event is drawn.
- Tests: `/debug event key: [mode:] [target:]`.
```

`config.yaml`: `version: "5.14.0"` → `version: "5.15.0"`.

- [ ] **Step 4: Build**

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror` — Expected: `Build succeeded`.

- [ ] **Step 5: Dev-guild test (with the owner and a second account or Ping-Qilin)**

1. `/plynling adopt` on a fresh account → « ✨ Événement (1) » « Bienvenue », naming the childhood trait.
2. `/debug event key:social_pact target:@SYNCS` (Ping-Qilin) → choose « Proposer un pacte » → two stories: the ask (no « attend » line, since she answered at once) and « Le petit doigt » with her answer.
3. `/debug event key:social_rival target:@other` → choose the challenge → the other account's card shows « ✨ Événement » « Le défi relevé »; they choose « Courir » → the chance reads « contre … »; the story names both.
4. Leave a response unanswered with `mode:Expired` on the responder side → within an hour the sweep answers it in character.
5. `/debug event key:on_visit_forgot target:@other` → « Le garder » → `/debug` nothing more: within 24–72 h « Le mouchoir brodé » appears, its story opening « Suite de « L'objet oublié » ».
6. Visit someone (`/plynling visit`) several days running → sometimes « L'objet oublié » follows.
7. `/debug plynling sick:Sick` then heal → « Le nez qui coule », later « Remis sur pattes ».
8. Abandon a Plynling that has a pending response → the response is cancelled, no error.
9. Rename an event key in a local build (do **not** commit) and trigger its on-action → the log shows the failure, the adoption/visit still succeeded.

Report each result; failures go back to the owning task.

- [ ] **Step 6: Files for the owner to commit**

`DebugModule.cs`, `PlynlingModule.cs`, `README.md`, `docs/agents/plynling-events.md`, `config.yaml` — `Plynling events: social testing, help, docs, 5.15.0`.
