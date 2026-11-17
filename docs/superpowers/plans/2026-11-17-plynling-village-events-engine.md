# Plynling Village Events — Engine Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Story files become playable Plynling events, run by the existing event engine, with stakes lines, mystery and locked options, flags, memories, marks, cooldowns and « once in a lifetime » — Arrival live as the first content.

**Architecture:** Approach C of the spec. At startup `StoryCatalog` loads and validates `ProjectSYNCS/Stories/` and `StoryCompiler` turns each scene into an `EventDef` (key `<arc>.<scene>`) that `PlynlingEvents.All` serves next to the C# catalog. The engine gains new effect and gate records, a `StoryKnowledge` on `EventContext`, two instance columns and two tables. Every rule that can be pure lives in `Helpers/` and is pinned in `tests/ProjectSYNCS.Checks`; service and card work is verified by building and on the dev guild.

**Tech Stack:** .NET 10, Discord.Net 3.20 (Components V2), EF Core on SQLite, the checks console (`tests/ProjectSYNCS.Checks`).

**Spec:** `docs/superpowers/specs/2026-11-17-plynling-village-events-design.md` (read it first; it builds on `docs/superpowers/specs/2026-10-29-plynling-story-file-format-design.md`). Also read `CLAUDE.md`, `docs/agents/plynling-events.md`, `docs/agents/plynling-storylines.md`, `docs/plynling-writing-style.md`.

## Global Constraints

- **The owner commits by hand.** No task commits; each task ends with `dotnet build` and the checks green, and the executor reports what changed.
- Stored keys are append-only and never renamed: arc, scene, option, ending, memory, epithet, keepsake, flag keys; `MarkKind` is an int, append at the end only.
- Every snowflake `.HasConversion<long>()`; no `DateTimeOffset` comparison in a LINQ-to-SQL query (filter in memory); no `HasMaxLength`.
- Every roll hashed with `StableRoll`, never `Random`.
- User-facing text in French, code/comments/logs in English, no « il »/« elle » in event text (agreements `{a:…|…}`).
- Discord caps: 5 buttons per row, 80-char labels, 40 components per V2 message, V2 messages re-assert the flag and send `AllowedMentions.None`.
- "Not playing is free": deciding alone never gains stress, a negative modifier, a coping trait, a trait swap or a `hurts` keepsake.
- The mascot (`PlynlingMascot.Is`) never gets cailloux, items or trait swaps; she gets titles, keepsakes and memories.
- Short events: 1–2 decisions per path, no mark, ≤ 30 cailloux per path. Long events: 3–5 decisions per path, 2–5 endings, ≤ 1 mark per ending, ≤ 100 cailloux per path; default `once: life`.
- A long drawn event: eligible only when the Plynling started no long event in the last **21 days** and **nothing waits** for it; while one runs, **no pulse is drawn**.
- Pace mapping: `now` → follow-up at 0 h; `next_day` → 20–28 h (hashed, `FollowUpAt`); `+Nh` → N h.
- Version: the batch adds a migration → **minor** bump in `ProjectSYNCS/config.yaml` (check `git diff`; if already bumped, don't bump again), with a `ProjectSYNCS/CHANGELOG.md` entry in English.

## Review Focus

1. **A memory or mark given twice** (the same event again after its cooldown, two endings racing, a mark already held) — expected: still one row, no unique-index crash that poisons every later sweep (the `HeldTraitKeysAsync` lesson). Pinned in Task 7 (read held keys including unsaved ones; unique indexes in Task 6).
2. **A forged or stale click on a locked option** — expected « plus disponible », nothing applied. Pinned in Task 5 (`Visible` stays the pick rule; `Shown` is display only).
3. **A story file edited while a run waits on it** (scene removed, retired, option retired) — expected: retired scene's run moves on; unknown key cancelled by the sweep as today. Pinned in Task 4 (compiler maps `retired -> x`; `ByKey` null for removed keys).
4. **A long event's run across a freeze, or decided alone at each step** — expected: flags carried, the 24 h clock paused, no penalty marks. Pinned in Task 5 (`AppliesWhenAlone`) and Task 7 (flags copied from the instance, never from memory).
5. **`{T}` in an event that reveals nothing** — expected: never empty bold text. Pinned in Task 2 (validator: `{T}` only in the first scene of an arc with an `adoption` or `stage` trigger).

---

### Task 1: Format — new header fields, removed words, `[mystery]`, `[end]`, `{T}`

**Files:**
- Modify: `ProjectSYNCS/Helpers/Plynling/Stories/StoryModel.cs`
- Modify: `ProjectSYNCS/Helpers/Plynling/Stories/StoryParser.cs`, `StoryParser.Scenes.cs`, `StoryTags.cs` (wherever the words are read)
- Modify: `tests/ProjectSYNCS.Checks/Stories/test_everything.story`, `ProjectSYNCS/Stories/Milestone/arrival.story`
- Modify: `tests/ProjectSYNCS.Checks/StoryChecks.cs` (`Valid` sample, `ParserHeader`, `ParserScenes`, `HandEdits`), `StoryReport.cs` / `StoryWalker.cs` only where they read removed fields

**Interfaces:**
- Produces:
  - `public enum StorySize { Short, Long }`
  - `StoryHeader(string Title, StoryFamily Family, IReadOnlyList<PlynlingStage> Stages, StoryTrigger Trigger, TimeSpan? Shelf, int Weight, IReadOnlyList<StoryWeightBonus> WeightBonuses, IReadOnlyList<StoryCondition> Requires, PlynlingBond? Other, StorySize Size, bool Once, TimeSpan? Cooldown, bool Locked)` — `Public` removed. `Once` is `true` when `once: life` is written **or** `Size == Long`.
  - `StoryScene(...)` without `Public` and `Bump`.
  - `StoryOption(...)` without `Public`, plus `bool Mystery`.
  - `StoryOutcome(IReadOnlyList<StoryParagraph> Prose, IReadOnlyList<StoryEffect> Effects, string? Next, bool End, StoryPos Pos)` — `End` true for `[end]`, then `Next` is null.

- [ ] **Step 1: Write the failing parser checks** in `StoryChecks.ParserHeader` / `ParserScenes` / `HandEdits`:
  - `size: short` → `Header.Size == StorySize.Short`; missing `size:` → error containing `"size"`; `size: medium` → error.
  - `once: life` → `Once`; `size: long` without `once:` → `Once == true`; `cooldown: 30d` → `Cooldown == TimeSpan.FromDays(30)`; `cooldown: 3h` → error (days only); `locked` line → `Locked`.
  - `public:` in the header, in a scene, `[public "…"]` on an option, and `bump` each → error containing `"removed"`.
  - `* go | Y aller  [mystery]` → `Mystery == true`.
  - An outcome ending in `[end]` → `End == true`, `Next == null`; `[end] [-> two]` together → error.
  - Update `Valid` (drop `public:` lines, add `size: long`) and the existing hand-edit cases that used `public:` as an anchor (anchor on `pace:` instead).
- [ ] **Step 2: Run** `dotnet run --project tests/ProjectSYNCS.Checks` — expected: the new story checks fail.
- [ ] **Step 3: Implement** the model and parser changes. `{T}` needs no parser change if the template-token check already lists tokens — add `{T}` to that list (validator rule in Task 2).
- [ ] **Step 4: Update the fixture and Arrival.** Fixture: drop `public`/`bump`, add `size: long`, `cooldown: 30d`, `locked` and one `[mystery]`. `[end]` is refused in a long arc, so add a **second fixture arc** `test_short` (`size: short`, `trigger: draw`, one scene, options ending in `[end]`) in a new file `tests/ProjectSYNCS.Checks/Stories/test_short.story`; the tag-coverage check reads both fixture files. Arrival: drop every `public:` line and the `interrupted` ending, add `size: long`, and in `first_steps` add one unconditional sentence naming the childhood trait with `{T}`, modelled on `on_welcome` (« Déjà, on devine un petit caractère : {T}. ») and written to `docs/plynling-writing-style.md`.
- [ ] **Step 5: Run** the checks — expected: parser checks pass (validator failures about `interrupted` are fixed in Task 2; note them, don't patch around them).
- [ ] **Step 6: Build** `dotnet build ProjectSYNCS` — expected: 0 errors.

### Task 2: Validator and walks per size

**Files:**
- Modify: `ProjectSYNCS/Helpers/Plynling/Stories/StoryValidator.cs`
- Modify: `tests/ProjectSYNCS.Checks/StoryWalker.cs`, `tests/ProjectSYNCS.Checks/StoryChecks.cs` (`Validator`, `Walks`, the tag-coverage check)

**Interfaces:**
- Consumes: Task 1's model.
- Produces: `public static readonly IReadOnlySet<string> StoryValidator.NotYetSupported` = `{ "handover", "trigger relation", "trigger death", "trigger sick", "trigger break", "trigger oldage" }` (the words the coverage check exempts); the walker treats an `[end]` outcome as reaching a pseudo-ending keyed `end`.

- [ ] **Step 1: Write the failing validator checks** (`ExpectInvalid(<message fragment>, <arc text>)`, as the file already does):
  - Removed: no `interrupted` ending → **valid**; a love arc with no `loss` → **valid**.
  - `handover` → `"not supported yet"`; each unsupported trigger → `"not supported yet"`.
  - Short: an `[epithet]`/`[keepsake]`/`[trait]`/`[unlock]` anywhere → `"short"`; `[end]` in a short arc → valid; `[end]` in a long arc → `"long"`.
  - Long: an ending with two marks → `"one mark"`; a `[trait x -> y]` in an arc whose `stage:` includes baby or teen → `"adult"`; a trait effect with no `->` (a gain) → `"swap"`; a swap whose two traits are in different exclusion groups (`PlynlingTraits`) → `"group"`.
  - Two `[mystery]` options in one scene → `"mystery"`.
  - `locked` arc never named by any `[unlock …]` in the library → `"never unlocked"`; `[unlock x]` where `x` is not `locked` → `"not locked"` (library-level: put it where cross-arc checks such as `ended` already run).
  - `{T}` outside the first scene, or in an arc whose trigger is not `adoption`/`stage` → `"{T}"`.
  - Keep the existing 2–4 live options rule (it already fits one button row).
- [ ] **Step 2: Write the failing walk checks** in `Walks`: short arc paths have 1–2 decisions (a 3-decision short arc fails with `"decisions"`); long arc paths have 3–5 and 2–5 endings (a 2-decision long arc fails); short ≤ 30 cailloux per path, long ≤ 100 (existing check, now per size). The tag-coverage check skips `NotYetSupported` and instead asserts each of them produces `"not supported yet"`.
- [ ] **Step 3: Run** the checks — expected: the new cases fail.
- [ ] **Step 4: Implement** in `StoryValidator` and `StoryWalker`. Remove the `interrupted`/`loss` requirements and the walker's loss-branch handling.
- [ ] **Step 5: Run** the checks — expected: all story checks pass, including Arrival (`dotnet run --project tests/ProjectSYNCS.Checks -- story arrival` writes its report).

### Task 3: Conditions and knowledge — what a story reads

**Files:**
- Create: `ProjectSYNCS/Helpers/Plynling/Stories/StoryKnowledge.cs`
- Create: `ProjectSYNCS/Helpers/Plynling/Stories/StoryConditions.cs`
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingEventCatalog.cs` (`EventContext`)
- Create: `tests/ProjectSYNCS.Checks/StoryEngineChecks.cs`; Modify: `tests/ProjectSYNCS.Checks/Program.cs` (call `StoryEngineChecks.Run(Fail)` after `StoryChecks.Run`, as section 50)

**Interfaces:**
- Produces:
  ```csharp
  public enum MarkKind { Epithet, Keepsake, Unlock }   // stored as int: append only

  public sealed record StoryKnowledge(
      IReadOnlySet<string> Flags,                         // the run's flags (the instance being decided or told)
      IReadOnlySet<string> Memories,                      // memory keys it holds
      IReadOnlySet<(MarkKind Kind, string Key)> Marks,
      IReadOnlySet<string> Ended,                         // "arc.ending" of finished runs
      IReadOnlySet<string> ArcFlags,                      // "arc.flag" left by finished runs
      IReadOnlyDictionary<string, int> Standing,          // villager key -> standing
      IReadOnlyDictionary<string, DateTimeOffset> LastStarted, // arc key -> its last run's start
      DateTimeOffset? LastLongStart,
      Season Season,
      DateTimeOffset Now)
  {
      public static StoryKnowledge Empty(DateTimeOffset now);
  }
  ```
  - `EventContext` gains a trailing optional `StoryKnowledge? Story = null` and `public StoryKnowledge Knowledge => Story ?? StoryKnowledge.Empty(DateTimeOffset.MinValue);`
  - `public static class StoryConditions { public static bool Holds(StoryCondition c, EventContext ctx); public static bool All(IEnumerable<StoryCondition> cs, EventContext ctx); }` — `Chain` is always false (expeditions later); `Flag` with a dot reads `ArcFlags`, without one reads `Flags`; `Gender` reads `ctx.Self.Gender`; `Passion` reads the taught passion the catalog recognises, else the innate one (as `PlynlingStats` does); `Relation` and `OtherTrait` read `ctx.Other` (false without one); `Negated` inverts.
  - `public static class StoryStanding { public static IReadOnlyDictionary<string,int> Compute(IEnumerable<string> heldMemoryKeys, IReadOnlyDictionary<string, MemoryDecl> decls, IEnumerable<string> revealed); }` — the format spec's formula: the holder gets the shift; a public (or revealed) memory gives every other cast member ±1 in its direction.
  - `public static string StoryText.Render(IReadOnlyList<StoryParagraph> prose, EventContext ctx)` (in `StoryConditions.cs`): lines whose conditions hold, joined with `\n`, paragraphs with `\n\n`; no template expansion (callers expand).

- [ ] **Step 1: Write the failing checks** in `StoryEngineChecks` (build `EventContext`s by hand with `PlynlingTraits`/`PlynlingStats` as section 34's walker does):
  - one `Holds` case per `ConditionKind` true and false, plus a negated one;
  - `Standing`: private `+3` held by lichen → `{lichen: 3}`; the same memory public → lichen 3, every other cast key +1; a negative public memory → others −1;
  - `Render`: a `[if flag met]` line appears only with `met` in `Flags`; paragraph join.
- [ ] **Step 2: Run** — expected: compile failure (types missing).
- [ ] **Step 3: Implement** the three files and the `EventContext` field.
- [ ] **Step 4: Run** the checks — expected: pass; `dotnet build ProjectSYNCS` — 0 errors.

### Task 4: The compiler and the catalog

**Files:**
- Create: `ProjectSYNCS/Helpers/Plynling/Stories/StoryCompiler.cs`
- Create: `ProjectSYNCS/Helpers/Plynling/Stories/StoryCatalog.cs`
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingEventCatalog.cs` (new records, `EventDef`/`EventOption` fields)
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingEvents/PlynlingEvents.cs` (`All` includes the compiled stories)
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingEventEngine.cs` (`PickTriggered`)
- Test: `tests/ProjectSYNCS.Checks/StoryEngineChecks.cs`

**Interfaces:**
- Consumes: Tasks 1–3.
- Produces (in `PlynlingEventCatalog.cs`):
  ```csharp
  public sealed record StoryGate(IReadOnlyList<StoryCondition> Needs, string? LockText, bool Secret) : EventGate;
  public sealed record SetFlag(string Key) : EventEffect;
  public sealed record ClearFlag(string Key) : EventEffect;
  public sealed record Remember(string MemoryKey) : EventEffect;
  public sealed record Forget(string MemoryKey) : EventEffect;
  public sealed record RevealMemory(string MemoryKey) : EventEffect;
  public sealed record GiveMark(MarkKind Kind, string Key, bool Hurts = false) : EventEffect;
  public sealed record SwapTrait(string From, string To) : EventEffect;
  public sealed record EndStory(string ArcKey, string EndingKey) : EventEffect;
  public sealed record StoryLink(StoryArc Arc, StoryScene Scene);
  ```
  - `EventDef` gains trailing `StoryLink? Story = null` and `Func<EventContext, int>? WeightBonus = null` (added to `Weight` in `WeightOf`, before trait/modifier factors).
  - `EventOption` gains trailing `StoryOption? Source = null` and `bool Mystery = false`.
  - `public static class StoryCompiler { public static IReadOnlyList<EventDef> Compile(StoryLoad load); public static string Key(string arc, string scene) => $"{arc}.{scene}"; }`
  - `public static class StoryCatalog { public static string Folder { get; set; } /* default Path.Combine(AppContext.BaseDirectory, "Stories"); set before first access */; public static IReadOnlyList<EventDef> Compiled { get; } /* lazy: StoryLibrary.Load(Folder, Folder), then StoryValidator, then StoryCompiler.Compile; throws StoryLoadException */; public static IReadOnlyDictionary<string, MemoryDecl> Memories { get; }; public static IReadOnlyDictionary<string, KeepsakeDecl> Keepsakes { get; }; public static IReadOnlyDictionary<string, EpithetDecl> Epithets { get; }; public static IReadOnlyDictionary<string, CastMember> Cast { get; } }` and `public sealed class StoryLoadException(IReadOnlyList<StoryError> errors) : Exception` whose message lists every error on its own line. `StoryLoad` is the record `StoryLibrary.Load` returns.

Mapping, per arc (`Compile`):
- Each live scene → `EventDef(Key(arc, scene), type, stages, title, scene's unconditional prose as Scene, options, ...)`. First scene: `Pulse` for `trigger: draw`; `Triggered` with `OnAction.Adopted` for `adoption`, `BecameTeen`/`BecameAdult` for `stage teen|adult`. Later scenes: `FollowUp` type. `Stages` from the header. `Target` = `Known` when `other:` is set, else `None`.
- `Condition` (first scene only) = `requires` hold **and** not `Once` with an entry in `LastStarted` **and** not within `Cooldown` of `LastStarted[arc]` **and** (`!Locked` or `Marks` has `(Unlock, arc)`) **and**, for a long `draw` arc, `LastLongStart` is null or ≥ 21 days before `Now`.
- `Weight` = header weight; `WeightBonus` = sum of the bonuses whose conditions hold.
- Options: label; `Gate` = `StoryGate(needs, lockText, secret)` when it has needs, else null; `[roll stat n]` → `EventChallenge`; `[lean]` → `Ai`; `[stress trait n]` → `StressCosts`; `[owner-only]` → `OwnerOnly`; `Source`, `Mystery`. `Outcome`/`FailOutcome` = the outcome's unconditional prose (the real text is rendered from `Source`, Task 8).
- Effects (success and failure lists): `grow`→`GrowStat`, `affinity`→`AffinityShift`, `stress n`→`StressChange`, `modifier`→`ApplyModifier`, `cailloux`→`GiveCailloux`, `item`→`GiveItem`, `lift`→`LiftNeed`, `flag`/`unflag`→`SetFlag`/`ClearFlag`, `remember`/`forget`/`reveal`→ the memory records; `[-> scene]` → `FollowUp(Key(arc, scene), h, h)` from the target scene's pace (Global Constraints); `[-> ending]` → `EndStory(arc, ending)` followed by the ending's marks (`epithet`/`keepsake`/`unlock` → `GiveMark`, `Hurts` from the keepsake declaration; `trait` → `SwapTrait`); `[end]` → nothing (the event simply ends).
- A retired scene `retired -> x` is an **alias**: `PlynlingEvents.ByKey(Key(arc, old))` returns `x`'s def, so a run waiting on it is offered `x` (its stored `EventKey` stays the old key; the story renders `x`). A retired option stays in `Options` with a new `public sealed record RetiredGate : EventGate;` (never shown, never picked — `Visible`'s existing `_ => false` arm and `Shown` both return false), so old stories still find it by key.

Catalog wiring:
- `PlynlingEvents.All` = the C# parts **plus** `StoryCatalog.Compiled`; `ByKeyMap` covers both. `StoryCatalog` loads on first access (the `Lazy` makes order irrelevant: whoever touches `All` first triggers the load).
- `PickTriggered`: when any **story** def is in the pool for that on-action, pick among story defs only (a story replaces the C# moment; Arrival replaces `on_welcome`).

- [ ] **Step 1: Write the failing checks.** First, the **first statement** of `tests/ProjectSYNCS.Checks/Program.cs` sets `StoryCatalog.Folder` to the repo's `ProjectSYNCS/Stories` (via the same repo-root lookup `StoryChecks.RepoRoot()` uses): many earlier sections touch `PlynlingEvents.All`, which now triggers the load. Then assert:
  - `PlynlingEvents.ByKey("arrival.first_steps")` is `Triggered`, `Trigger == OnAction.Adopted`, stage baby; `arrival.the_village` is `FollowUp`;
  - `arrival.first_steps` option `dig` has a `StoryGate` with a `trait rowdy` need, not secret; option `to_her` success effects contain `SetFlag("came_to_her")`, `Remember("lichen.came_first")` and a `FollowUp("arrival.the_village", 20, 28)`;
  - `arrival.the_pebble` option `keep` effects contain `EndStory("arrival","kept_pebble")` then `GiveMark(MarkKind.Keepsake, "lichen_pebble", false)`;
  - compiling the `test_short` fixture: one `Pulse` def, `[end]` options with no `FollowUp`/`EndStory`;
  - `Condition` on a long draw arc: false with `LastLongStart = Now − 20 d`, true at `Now − 21 d`; false once `LastStarted` has the arc; a `cooldown: 30d` short arc false at 29 d, true at 30 d; a `locked` arc false until `Marks` holds `(Unlock, arc)`;
  - `PickTriggered(OnAction.Adopted, …)` over `PlynlingEvents.All` with a baby context returns `arrival.first_steps`, never `on_welcome`;
  - a folder with a broken file → accessing `Compiled` throws `StoryLoadException` whose message names file and line.
- [ ] **Step 2: Run** — expected: compile failure / failures.
- [ ] **Step 3: Implement** the records, the compiler, the catalog and the wiring. Keep `StoryCatalog` free of any DI or gateway.
- [ ] **Step 4: Run** the checks — expected: pass. Also confirm the existing event checks (sections touching `PlynlingEvents.All`) still pass: the story defs must not break catalog-wide rules (`AloneOptions` non-empty, no « il »/« elle »). If a catalog-wide check cannot hold for story defs (e.g. it reads `Scene` text), scope it to C# defs (`d.Story is null`) and say so in the step report.
- [ ] **Step 5: Build** — 0 errors.

### Task 5: Engine rules — shown vs pickable, alone, the long-event draw

**Files:**
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingEventEngine.cs`
- Test: `tests/ProjectSYNCS.Checks/StoryEngineChecks.cs`

**Interfaces:**
- Consumes: Task 4's records.
- Produces:
  - `public static bool Shown(EventOption o, EventContext ctx)` — `StoryGate { Secret: true }` → its needs hold; `StoryGate { Secret: false }` → true; old gates → same as `Visible`.
  - `Visible` keeps meaning "may be picked": `StoryGate` → its needs hold (every existing caller — the pick, `AloneOptions` — stays correct unchanged).
  - `AppliesWhenAlone`: `SwapTrait` → false; `GiveMark { Hurts: true }` → false; other new records → true.
  - `public static bool IsLong(EventDef d) => d.Story?.Arc.Header.Size == StorySize.Long;`
  - `public static bool BlocksPulse(IEnumerable<EventDef> openDefs)` — true when any open instance's def `IsLong`.
  - `PickPulse` gains a parameter `bool allowLong`; when false, long defs are out of the pool.

- [ ] **Step 1: Write the failing checks:** a locked non-secret option is `Shown` but not `Visible`; a secret unmet one is neither; `AloneOptions` never contains a locked option; `AppliesWhenAlone(new SwapTrait("brave","craven"), def)` false, `GiveMark(Keepsake, k, Hurts: true)` false, `GiveMark(Epithet, k)` true; `BlocksPulse` true with an open long def; `PickPulse(..., allowLong: false)` never returns a long def (use a hand-made two-def list).
- [ ] **Step 2: Run** — fail. **Step 3: Implement.** **Step 4: Run** — pass; build — 0 errors.

### Task 6: Data — columns, tables, migration

**Files:**
- Modify: `ProjectSYNCS/Models/Plynling/PlynlingEventInstance.cs` (`string? Flags`, `string? EndingKey`)
- Create: `ProjectSYNCS/Models/Plynling/PlynlingMemory.cs`, `ProjectSYNCS/Models/Plynling/PlynlingMark.cs`
- Modify: `ProjectSYNCS/Data/AppDbContext.cs`
- Create: the migration `AddStoryEvents` (via `dotnet ef migrations add AddStoryEvents` in `ProjectSYNCS/`)

**Interfaces:**
- Produces:
  ```csharp
  public class PlynlingMemory { int Id; int PlynlingId; string MemoryKey; DateTimeOffset AcquiredAt; DateTimeOffset? RevealedAt; int? InstanceId; }
  public class PlynlingMark   { int Id; int PlynlingId; MarkKind Kind; string Key; DateTimeOffset AcquiredAt; int? InstanceId; }
  ```
  `DbSet`s `PlynlingMemories`, `PlynlingMarks`; both cascade with the Plynling; unique indexes `(PlynlingId, MemoryKey)` and `(PlynlingId, Kind, Key)`; `InstanceId` a plain column (no FK — instances are history, never deleted on their own). `Flags` is `key;key` (empty → null).

- [ ] **Step 1: Add the models and mapping**, following the `PlynlingEventInstance` block's comments style.
- [ ] **Step 2: Generate the migration** — expected: schema only (two tables, two columns, indexes). Read it; no data statements.
- [ ] **Step 3: Build** — 0 errors. **Step 4: Run** the bot once against a copy of a dev database (`dotnet run` in `ProjectSYNCS/`) — expected: migration applied, startup log clean.

### Task 7: The service — knowledge, effects, flags, endings, the draw

**Files:**
- Modify: `ProjectSYNCS/Services/Plynling/PlynlingService.Events.cs`
- Create: `ProjectSYNCS/Services/Plynling/PlynlingService.Stories.cs` (the partial for story reads and writes; keeps `Events.cs` under 40 KB)

**Interfaces:**
- Consumes: Tasks 3–6.
- Produces:
  - `private async Task<StoryKnowledge> StoryKnowledgeAsync(Plynling p, PlynlingEventInstance? inst, DateTimeOffset now)` — memories, marks, `Ended`/`ArcFlags` from this Plynling's instances with an `EndingKey` (arc = the key's part before the dot), `LastStarted` and `LastLongStart` from instances of each arc's first scene (`CreatedAt`, filtered in memory), `Standing` via `StoryStanding.Compute` with `StoryCatalog.Memories`, `Flags` from `inst?.Flags`, `Season` from `ItemCatalog.SeasonAt(now)`.
  - `GetEventContextAsync(Plynling p, DateTimeOffset now, Plynling? other = null, PlynlingEventInstance? inst = null)` — fills `Story`.

- [ ] **Step 1: Context everywhere it is built.** `PickEventOnceAsync`, `ResolveAloneAsync`, `TickEventsAsync` (the pulse draw, `inst: null`), `FlushOnActionsOnceAsync`, and `PlynlingEventHandler` (the choice card) pass the instance being decided or shown.
- [ ] **Step 2: Apply the new effects in `ApplyEventAsync`.** Compute the run's flags first: `inst.Flags` ∪ `SetFlag` − `ClearFlag` of the applied list. Then:
  - `FollowUp` to a story def: `later.Flags = flags` (C# follow-ups unchanged).
  - `Remember` / `Forget` / `RevealMemory`: idempotent — check held keys **including entities added in this unit of work** (`ChangeTracker`, as `QueueBreaksAsync` does) before adding; `Forget` removes; `RevealMemory` sets `RevealedAt` if null.
  - `EndStory`: `inst.EndingKey = ending; inst.Flags = flags`.
  - `GiveMark`: idempotent as above; never fails if already held.
  - `SwapTrait` when `!PlynlingMascot.Is(p)` and it holds `From` and not `To`: remove the `From` row, add `To` (`TraitKind.Personality`, `AcquiredAt = now`), `AddMomentAsync(p, JournalKind.TraitGained, To, now)`, `inst.GainedTraitKey = To`, refresh the stress cache as `GainCoping` does.
  - `GiveCailloux`/`GiveItem` already skip the mascot.
- [ ] **Step 3: The draw.** In `TickEventsAsync`: skip the pulse when `PlynlingEventEngine.BlocksPulse(open defs)`; call `PickPulse(..., allowLong: open has no unresolved, uncancelled instance at all)` — "nothing waits" includes not-yet-available follow-ups. `PulseKeys` (the recent window) must include story `Pulse` defs: it is computed from `PlynlingEvents.All`, so it already does once Task 4 lands — confirm.
- [ ] **Step 4: The story read.** `GetEventStoryAsync` builds an `EventContext` with `StoryKnowledgeAsync(self, inst, now)` (plain reads, no settle — use `GetTraitsAsync`, not the settling reads) and passes it to `PlynlingEventStory.Build` (Task 8 adds the parameter).
- [ ] **Step 5: Build** — 0 errors; checks green.

### Task 8: Cards and stories — the stakes line, buttons, locked options, story prose, titles

**Files:**
- Create: `ProjectSYNCS/Helpers/Plynling/PlynlingEventStakes.cs`
- Modify: `ProjectSYNCS/Commands/Plynling/PlynlingEventCards.cs`
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingEventStory.cs`
- Modify: `ProjectSYNCS/Commands/Plynling/PlynlingModule/PlynlingModule.Rendering.cs` (the card's name line), `ProjectSYNCS/Interactions/Components/PlynlingPersonalityHandler.cs` (titles, keepsakes, memories)
- Test: `tests/ProjectSYNCS.Checks/StoryEngineChecks.cs`

**Interfaces:**
- Consumes: Tasks 3–5, 7.
- Produces:
  - `public static string PlynlingEventStakes.Line(EventOption o, EventDef def, EventContext ctx, PlynlingGender g, string? otherName)` — the text after the label: costs first, exactly as `Details` prints them today (trait/stat need, 🎲 odds, 😣 stress), then gains; `🔒 <LockText or the needs in words>` for a shown-but-locked option (and nothing else); `❔ on verra bien` in place of the gains for a `Mystery` option (costs still shown). Gains: `🪙 n` · `🎁 <item clear name>` · `❤️ <byname> s'en souviendra` (positive memory) / `<byname> s'en souviendra` (negative) · `<modifier emoji> <modifier name>` · `<stat emoji> <stat name>` for a lesson · for an option whose effects contain `EndStory`: `🏷️ un titre` / `🎁 un souvenir à garder` / `✨ un trait change` per mark. A roll prints `🎲 <Stat> n % : <success gains>` and ` · sinon <failure gains>` only when the failure gains differ and are non-empty. Bynames come from `Stories/village.cast` (expose `StoryCatalog.Cast`). Parts joined with ` · `.
  - `public static string PlynlingEventStory.SceneText(EventDef def, EventContext? ctx)` and `OutcomeProse(EventDef def, EventOption o, bool success, EventContext? ctx)` — story defs render `Source` prose with `StoryText.Render` (null ctx → unconditional lines only), C# defs return `Scene`/`Outcome` as today. When the outcome's effects hold an `EndStory`, append the ending's title (bold) and prose.
  - `PlynlingEventStory.Build(..., EventContext? ctx = null)`.

- [ ] **Step 1: Write the failing checks** on `PlynlingEventStakes.Line` with hand-built options: a `GiveCailloux(15)` plain option → contains `🪙 15`; a roll with `GiveItem` success and nothing on failure → `🎲`, the item name, no `sinon`; a mystery option with a roll → odds shown, `❔ on verra bien`, no item name; a `Remember` of a `+2` memory held by `courge` → `❤️ la Courge s'en souviendra` (whatever the cast says); a locked story option → starts with `🔒`, no gains; the Arrival `the_pebble` `keep` option → contains `🎁 un souvenir à garder`. And `SceneText` for `arrival.the_village` with `came_to_her` in `Flags` contains the `[if flag came_to_her]` line, without it does not.
- [ ] **Step 2: Run** — fail. **Step 3: Implement `PlynlingEventStakes` and the story text helpers.**
- [ ] **Step 4: The choice card.** `BuildChoice`: options = `Shown`; each line `**label** · <Line>`; scene text from `SceneText(def, ctx)`; buttons plain (`ButtonEmoji` returns the trait icon only for a `TraitGate`, or a `StoryGate` whose needs include a `Trait` condition — never 🎲); a locked option's button is `.WithDisabled(true)` (its custom-id stays unique: it carries its own option key).
- [ ] **Step 5: The story.** `Build`/`OutcomeText` use `SceneText`/`OutcomeProse` with the passed `ctx`; add lines for what applied: `-# 🏷️ **{name}** est désormais *{title}*` per `GiveMark(Epithet)`, `-# 🎁 **{keepsake name}** — gardé` per keepsake, `-# {emoji} {From} → **{To}**` for a swap (from `GainedTraitKey`), all expanded with the Plynling's gender. A trait reveal on a story's first scene uses `{T}` as today (`IsReveal` must also be true for a `Triggered` story def with `Adopted`/`BecameTeen`/`BecameAdult`).
- [ ] **Step 6: Titles on the card and in « Personnalité ».** The card's name line shows the most recent epithet after the name (`Brindille, la Nez-en-l'air`); « Personnalité » gains a section listing titles, keepsakes (name + story) and memories (wording), cut before the 1024-character field cap as « État » is. Load them through a new `PlynlingService` read (plain reads). Measure the card's component count before and after (CLAUDE.md: 40 max).
- [ ] **Step 7: Run** the checks — pass; build — 0 errors.

### Task 9: Startup, `/debug event`, the path report

**Files:**
- Modify: `ProjectSYNCS/Program.cs`
- Modify: `ProjectSYNCS/Commands/Admin/DebugModule/DebugModule.Tools.cs`
- Modify: `tests/ProjectSYNCS.Checks/StoryReport.cs`

- [ ] **Step 1: Fail loudly at startup.** In `Program.cs`, right after `.Build()` and before `MigrateAsync`, touch `StoryCatalog.Compiled` inside a `try`; on `StoryLoadException` log every error at `Critical` and rethrow (the bot stops). Confirm `ProjectSYNCS.csproj`'s `Stories\**\*` item copies to the publish output used by the `Dockerfile` (read `docs/agents/deployment.md`).
- [ ] **Step 2: `/debug event`.** The key list already reads `PlynlingEvents.All`; it now has dozens more keys — list only first scenes of stories plus C# keys, and keep the reply under 2000 characters (cut with « … »). Forcing a later scene starts it with empty flags: say so in the reply.
- [ ] **Step 3: The path report** shows, under each option, its stakes line as the card renders it (compile the arc with `StoryCompiler`, call `PlynlingEventStakes.Line` with a neutral context), and marks mystery options « (mystère) ». Add a `Reports` assertion that Arrival's report contains `🎁 un souvenir à garder`.
- [ ] **Step 4: Verify on the dev guild:** adopt a new Plynling → Arrival's first scene appears with the trait named; play it through the three days (`/debug event` with `mode` to move fast); the ending gives the keepsake or the title; the story page shows the title line; « Personnalité » lists it; a forged click on a locked option answers « plus disponible ». Then let one step expire and check it is decided alone with no penalty.
- [ ] **Step 5: Run** `dotnet run --project tests/ProjectSYNCS.Checks` — pass; build — 0 errors.

### Task 10: Docs, version, changelog

**Files:**
- Modify: `docs/agents/plynling-storylines.md` (becomes the notes for the new engine: loading, key scheme, the mapping, flags, knowledge, the retired-scene choice made in Task 4, the per-size rules), `docs/agents/plynling-events.md` (a pointer, and the `Shown`/`Visible` split), `CLAUDE.md` (the table row for storylines: add "the story engine"), `docs/superpowers/specs/2026-10-28-plynling-storylines-design.md` (a first line: superseded by `2026-11-17-plynling-village-events-design.md`)
- Modify: `ProjectSYNCS/config.yaml` (minor bump), `ProjectSYNCS/CHANGELOG.md`

- [ ] **Step 1: Update the docs** — rules and reasons, not history (CLAUDE.md). Keep each file under 40 KB.
- [ ] **Step 2: Bump** per Global Constraints and add the changelog entry (e.g. "Plynling events can now come from story files: a stakes line shows what each choice brings, locked and mystery options, titles and keepsakes; the new Plynling welcome is the first one"). No member names, no easter eggs.
- [ ] **Step 3: Final run** — checks green, build 0 errors with `-warnaserror` if the CI build uses it (read `.github/`). Report to the owner: version number, what to commit.

---

## Not in this plan (later plans)

The first short-event wave and the rewritten best premises (content sessions with the owner), long drawn events and their rarity tuning (the 21-day rule is built here; the weights are set when the first long drawn event exists), retiring old stages, handed-over decisions and `[other remember]`, growing-up arcs, other triggers.
