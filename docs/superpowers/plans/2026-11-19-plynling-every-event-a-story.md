# Every Plynling event a story file — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend the story format so the last 50 C# events (Asks, Moments, GrowingUp, Breaks) become `.story` files, then delete the C# catalog, with no change a player can see.

**Architecture:** Approach C again: the new words compile onto today's `EventDef` (a handover scene → `EventType.Response`, `trigger: break 2` → `BreakLevel 2`, a delay range → `FollowUp(min, max)`). Old stored keys keep resolving through `was:` aliases in `PlynlingEvents.ByKey`. A frozen fingerprint of today's 50 defs, taken before anything moves, proves the compiled defs are the same.

**Tech Stack:** C# / .NET 10, the bot's own story parser, validator and compiler (`ProjectSYNCS/Helpers/Plynling/Stories/`), the console checks in `tests/ProjectSYNCS.Checks`.

**Spec:** `docs/superpowers/specs/2026-11-19-plynling-every-event-a-story-design.md`. Binding notes: `docs/agents/plynling-storylines.md`, `docs/agents/plynling-events.md`, `docs/plynling-writing-style.md`. Read all three before Task 2.

## Global Constraints

- **Behaviour is unchanged for players:** same texts, options and option keys, effects (in the same order), weights, targets, delays, leans, stances and gates. Texts are copied character for character from today's C#. Never reword. If a copied text trips a validator cap, stop and report it; don't shorten it.
- **Nothing stored is lost:** every one of the 50 old keys resolves through `PlynlingEvents.ByKey` to a def with the same option keys.
- **Stored keys are append-only:** never rename an option key, and never reuse a key from `PlynlingEvents.Removed`.
- **Commits:** none. The owner commits by hand. Every task ends with the work left uncommitted and `git status` reported.
- **Version:** one bump for the whole batch, `ProjectSYNCS/config.yaml` 7.74.2 → **7.74.3**, plus a CHANGELOG entry in English (Task 8 only).
- **Windows shell:** `sed -i` strips CRLF, so run `unix2dos` after it. Don't use Python. `dotnet` prints French.
- **Build:** `dotnet build ProjectSYNCS` → 0 errors, 0 warnings. **Checks:** `dotnet run --project tests/ProjectSYNCS.Checks` → exit code 0 and no failure lines.
- **No static field initializer reads a static field in another file** (CLAUDE.md). `PlynlingEvents.All` reads `StoryCatalog.Compiled`, which is a lazy property, and that is allowed.
- **Words added to the format** (spelling is binding):
  - scene fields: `was: <key>` and `handover`;
  - `pace: Nh | Nd | Nh-Mh | Nd-Md`;
  - header fields: `other: anyone | known | hostile | <bond>[, <bond>…]` and `other_if: [relation <bond>] [affinity >= n] [can_couple]`;
  - triggers: `trigger: break 1|2|3 | sick | recovered | visit | death`;
  - effects: `[coping]`, `[coping <trait>]`, `[couple]`, `[heartbreak]`, `[bond peace|friends|bestfriends]`;
  - roll: `[roll <stat> vs]`;
  - option attributes: `[accept]` and `[refuse]`.

## Review Focus

1. **An instance waiting under an old key when the update lands** (a 14-day dream echo, a `reply_declare` on someone's card) opens, resolves under its old option keys, and its chain goes on to the new scene. The test is in Task 7.
2. **The recent-pulse window and the « already waiting » set hold old keys:** a Plynling that just played `grown_campfire` must not draw `campfire.fire` the next day as if it were new. The test is in Task 7.
3. **A break or moment waiting under its old key** (`break_cloud`, `on_visit_forgot`) must stop a second copy being queued under the new key. The test is in Task 7.
4. **History read through old keys:** the adult personality (ado choices), journals and the owner report keep counting old instances as the same event. The test is in Task 7.
5. **The asker's story page never shows the other owner's answer, and the answer's page shows only the answer.** The test is in Task 4.

---

### Task 1: Freeze today's 50 events

The fingerprint is written once, from today's C#, before any event moves. Every later task is gated on it.

**Files:**
- Create: `tests/ProjectSYNCS.Checks/OldEventChecks.cs`
- Create (generated): `tests/ProjectSYNCS.Checks/OldEvents.txt`. Keep it out of `tests/ProjectSYNCS.Checks/Stories/`: the library reports any non-`.story` file there.
- Modify: `tests/ProjectSYNCS.Checks/Program.cs`. Call `OldEventChecks.Run(Fail, args)` right after `StoryEngineChecks.Run(Fail)`, near line 5424.

**Interfaces:**
- Produces:
  - `OldEventChecks.Fingerprint(EventDef def, Func<string, string> name) : string`
  - `OldEventChecks.Name(string key) : string`, which returns `PlynlingEvents.ByKey(key)?.Story?.Scene.Was ?? key`. `Scene.Was` arrives in Task 2; until then `Name` returns `key`.
  - `OldEventChecks.Run(Action<string> fail, string[] args)`

**Fingerprint format:** one line per fact, rendered under `CultureInfo.InvariantCulture` so the file is the same on every machine. It never contains the def's own key.
- `title`, `type`, `scene`, `weight`, `target`, `break` (BreakLevel), `trigger`.
- `stages`, only when the type is Pulse or Triggered.
- `who`, only when the type is Pulse or Triggered: `TargetCondition` probed over every `PlynlingBond` × affinity {−1, 0, 1} × `CanCouple` {false, true}, written as a 0/1 string, or `any` when null.
- Per option, in order:
  - key and label;
  - success text and failure text (`-` when null);
  - gate. A `TraitGate(k)` and a `StoryGate{Secret: true, Needs: [Trait k, not negated], LockText: null}` both render `trait k hidden`. Anything else renders its type and fields.
  - challenge: `stat difficulty`, plus `vs` when `VsTarget`. Write `-` for none.
  - the success effects and the failure effects. A `FollowUp` renders `follow <name(key)> <min>-<max>`. An `AskTarget` renders `ask {` + the reply def's own fingerprint, indented, + `}`. This lets a copied reply under a new key match. Every other effect renders as its record `ToString()`.
  - stress costs and leans, each sorted by key;
  - `OwnerOnly`, `Stance` and `Mystery`.
- It never renders `Condition` or `WeightBonus`, and never `stages` or `who` on a FollowUp or Response.

- [ ] **Step 1:** Write `Fingerprint`, `Name` and the write mode.
  - The write mode runs on `-- old-events write` and covers `PlynlingEvents.All.Where(d => d.Story is null)`, ordered by key.
  - Each def becomes a block headed `== <key>`, followed by its fingerprint.
  - The file then ends with the ado baseline: one line `ado <axis> <mean:F4>` per `AiAxis`, and one line `ado-grow <stat> <mean:F4>` per stat. Compute them the way `PlynlingEventEngine.AdoLeaningBaseline` and `AdoGrowthBaseline` do, over `PlynlingEventEngine.AdoPool(PlynlingEvents.All)`, with a challenge counted half won.
- [ ] **Step 2:** Write the check, which runs on every run.
  - Parse the blocks. Expect exactly **50**.
  - For each block, expect `PlynlingEvents.ByKey(key)` to be non-null, and expect `Fingerprint(def, Name)` to equal the stored text. On a mismatch, name the key and the first line that differs.
  - Expect no `was:` key in the catalog to be in `PlynlingEvents.Removed`.
  - Print `ado baseline: <axis> before → now` for every axis and stat. This is printed only and never fails.
- [ ] **Step 3:** Run `dotnet run --project tests/ProjectSYNCS.Checks -- old-events write`.
  - Expected: `OldEvents.txt` holds 50 blocks.
  - The list of blocks matches the 50 events in the spec's intent table: 11 in Asks, 15 in Moments, 12 in GrowingUp, 12 in Breaks.
- [ ] **Step 4:** Run `dotnet run --project tests/ProjectSYNCS.Checks`.
  - Expected: exit code 0, and every block matches, because nothing has moved yet.
  - Break one text in a C# file by hand, check that the run fails on that key, then undo the change.
- [ ] **Step 5:** Don't commit. Report `git status`.

### Task 2: Parse the new words

**Files:**
- Modify: `ProjectSYNCS/Helpers/Plynling/Stories/StoryModel.cs`, `StoryParser.cs`, `StoryParser.Scenes.cs` and `StoryVocabulary.cs`.
- Modify: every reader of `StoryHeader.Other`. Today that is `StoryCompiler.cs:70-76`, `StoryValidator.cs:81` and `tests/ProjectSYNCS.Checks/StoryChecks.cs:563`.
- Test: `tests/ProjectSYNCS.Checks/StoryChecks.cs` (sections `ParserHeader`, `ParserScenes`, `TagsAndVocabulary`).

**Interfaces** (all produced here). Enum values are appended, never inserted:
- `enum TriggerKind { …, OldAge, Recovered, Visit }`
- `enum PaceKind { NextDay, Now, Hours, Range }`, with `StoryPace(PaceKind Kind, int Hours, int MaxHours = 0)`. For `Range`, `Hours` is the minimum and `MaxHours` the maximum, both in hours.
- `enum ConditionKind { …, Gender, Affinity, CanCouple }`. Affinity uses `Op` and `Number`. `CanCouple` has an empty key.
- `enum EffectKind { …, Trait, Coping, Couple, Heartbreak, Bond }`:
  - `Coping` has an empty key, or a trait key.
  - `Bond` has the key `peace`, `friends` or `bestfriends`.
- `StoryRoll(PlynlingStat Stat, int Difficulty, bool Vs = false)`. A vs roll has `Difficulty` 0.
- `StoryOption(…, Pos, Stance Stance = Stance.Neutral)`, with the new parameter last.
- `StoryScene(…, Pos, string? Was = null)`, with the new parameter last.
- `enum OtherKind { Bonds, Known, Hostile, Anyone }` and `record StoryOther(OtherKind Kind, IReadOnlyList<PlynlingBond> Bonds)`. `Bonds` is empty unless `Kind` is `Bonds`.
- `StoryHeader.Other` becomes `StoryOther?`. `StoryHeader` gains a last parameter, `IReadOnlyList<StoryCondition> OtherIf`.

- [ ] **Step 1:** Write failing parser checks, one `Expect` each:
  - `other: friends, bestfriends` parses to `Bonds [Friends, BestFriends]`.
  - `other: anyone`, `known` and `hostile` each parse to their kind.
  - `other: lovers` still parses (to `Bonds [Lovers]`).
  - `other_if: [relation acquaintances] [affinity >= 0]` parses to two conditions, and `other_if: [can_couple]` to one.
  - Each trigger parses: `trigger: recovered`, `visit` and `death` (with a null arg), `break 2`, and `sick`.
  - `pace: 14d-16d` parses to `Range(336, 384)`, `24h-72h` to `Range(24, 72)`, and `2d` to `Range(48, 48)`.
  - These paces are parse errors: `pace: 14d-16h` (units differ), `pace: 3d-2d` (backwards), `pace: 31d` (over 720 h), and `pace: 0h`.
  - `+12h` still parses to `Hours`.
  - `was: social_declare` sets `Scene.Was`. A second `was:` in the same scene is an error (« given twice »).
  - `[roll courage vs]` parses to `StoryRoll(Courage, 0, true)`.
  - `[accept]` and `[refuse]` set `Stance`.
  - `[coping]` parses to key "", and `[coping journaller]` to key `journaller`.
  - `[couple]` and `[heartbreak]` parse. `[bond friends]` parses, and `[bond lovers]` is an error.
- [ ] **Step 2:** Run the checks. They should fail on exactly these new cases.
- [ ] **Step 3:** Implement.
  - Scene fields: add `was` to the field list in `ParseScene`.
  - `pace` grammar: `^(\d+)([hd])$` or `^(\d+)([hd])-(\d+)\2$`. Days are ×24, and the bounds are 1 h to 720 h. Name the bound in the error message.
  - `AttributeWords` gains `accept` and `refuse`.
  - `[roll <stat> vs]` is a third accepted shape of `roll`.
  - In `StoryVocabulary.Effect`, read the one-word effects (`couple`, `heartbreak`, `coping`) and `coping <trait>` before the « one word after it » rule.
  - In `StoryVocabulary.Condition`:
    - read `affinity >= n` / `affinity < n` like `stat`, but with no stat word;
    - read `can_couple` as a one-word condition.
  - Header fields:
    - `other:` takes a single word (`anyone`, `known`, `hostile`) or a comma list of bonds.
    - `other_if:` takes tags only, parsed like `requires:`.
  - Triggers: `recovered`, `visit` and bare `death` are one-word triggers. `death <bond>` still parses, and Task 3 refuses it.
  - Update the `trigger` error message to list every word.
- [ ] **Step 4:** Run the build and the checks. Expected: everything passes, including Task 1's 50 blocks.
- [ ] **Step 5:** Don't commit. Report `git status`.

### Task 3: Validate the new words, and walk handovers

**Files:**
- Modify: `ProjectSYNCS/Helpers/Plynling/Stories/StoryValidator.cs` and `tests/ProjectSYNCS.Checks/StoryWalker.cs`.
- Create: `tests/ProjectSYNCS.Checks/Stories/test_handover.story` and `tests/ProjectSYNCS.Checks/Stories/test_break.story`.
- Modify: `tests/ProjectSYNCS.Checks/Stories/test_everything.story`. Add one `was: test_everything_old` and one `pace: 2d-3d`.
- Test: `tests/ProjectSYNCS.Checks/StoryChecks.cs` (`Validator`, `Walks`).

**Why the fixtures are split:** the spec asks for every new word in `test_everything.story`. That arc is long, and a long arc may not end on `[end]`, which every handover option must. So the handover words go in `test_handover.story`, a short love arc, and the break words go in `test_break.story`. `test_everything` carries the words a long arc can hold (`was:` and the range pace). The other triggers are checked inline.

**Interfaces:**
- Consumes: the model from Task 2.
- Produces: `StoryValidator.NotYetSupported` = { `trigger relation`, `trigger death <bond>`, `trigger oldage`, `condition chain` }.

**Rules** (each one gets an `ExpectInvalid`/`ExpectError` case with a message fragment, plus one valid case):
- **Triggers:**
  - Supported: `draw`, `adoption`, `stage teen|adult`, `sick`, `recovered`, `visit`, bare `death`, and `break 1-3`.
  - `death <bond>`, `relation` and `old_age` stay « not supported yet ».
- **`other:`:**
  - A bond list that mixes hostile bonds (rivals, enemies) with others is refused.
  - `other_if:` needs `other:`.
  - `other_if:` accepts only `relation`, `affinity` and `can_couple`.
  - `affinity` and `can_couple` anywhere else are refused (« only in other_if: »).
  - `affinity n` takes −100 to 100.
- **`was:`:**
  - The key has no dot and passes `IsKey`.
  - It is unique across the whole catalog. The message names both scenes.
  - It is refused on a retired scene.
- **`handover` scene:**
  - It is not the arc's first scene.
  - It has options (it is not a narration).
  - Every outcome of every live option is `[end]`.
  - No `[needs flag …]` and no `[if flag …]` in it.
  - No `[flag]` or `[unflag]` in it. *(Builder's call, wider than the spec: a reply's flags would belong to a run that never continues.)*
  - It is written `pace: now`. *(Builder's call: the reply is queued at once, and the file should say so.)*
- **Where the new words may appear:**
  - `[accept]`, `[refuse]`, `[couple]` and `[heartbreak]` only in handover scenes.
  - `[coping …]` only in an arc whose trigger is `break`, and its trait must be `TraitKind.Coping`.
  - `[bond …]` and `[roll <stat> vs]` need `other:`.
  - A vs roll skips the 1–20 difficulty check.
- **Stress:** `[stress n]` goes down to −150 in a break arc. Everywhere else the floor stays −100.

**Walker** (`StoryWalker.Walk`):
- A step whose scene is `Handover` is not counted as a decision.
- A short event allows 1–3 decisions. Change the message to « a short event has 1 to 3 », and change the `decisions 3` case at `StoryChecks.cs:138` to a 4-decision case.

- [ ] **Step 1:** Write the fixtures and the failing cases.
  - `test_handover.story` is short. It has:
    - `family: love`, `other: known` and `other_if: [can_couple] [affinity >= 0]`;
    - an asking option `[owner-only]` → `[-> answer]`;
    - a free option with `[roll courage vs]` and `[bond friends]`;
    - a handover scene `answer` with `pace: now`, one `[accept]` option ending `[couple] [end]` and one `[refuse]` option ending `[heartbreak] [end]`;
    - a second scene `more` with `pace: 24h-72h`.
  - `test_break.story` has `trigger: break 2`, `[coping]`, `[coping journaller]` and `[stress -120]`.
- [ ] **Step 2:** Run the checks. They should fail on the new cases only.
- [ ] **Step 3:** Implement the rules and the walker change.
- [ ] **Step 4:** Run the build and the checks.
  - Expected: they pass.
  - Both fixtures validate and walk clean.
  - `-- story test_handover` writes a report without crashing.
  - If `StoryReport` needs a word for a new effect kind, use one plain French word per kind. Wording is the builder's call.
- [ ] **Step 5:** Don't commit. Report `git status`.

### Task 4: Compile the new words

**Files:**
- Modify: `ProjectSYNCS/Helpers/Plynling/Stories/StoryCompiler.cs` and `StoryConditions.cs`.
- Test: `tests/ProjectSYNCS.Checks/StoryEngineChecks.cs`. Add a `Compile()` section that compiles `test_handover` and `test_break` from the fixture load (the way `Fixtures()` does).

**Interfaces:**
- Consumes: the Task 2 model.
- Produces:
  - `StoryConditions.OnTarget(IReadOnlyList<StoryCondition> conditions, TargetInfo t) : bool`. It handles Relation (`t.Bond`), Affinity (`t.Affinity`, with the op) and CanCouple (`t.CanCouple`), honouring `Negated`.
  - `StoryCompiler.Aliases(load)` now also maps every live scene's `was:` key → `arc.scene`.

**Compile rules:**
- **Target** (openers and follow-ups; the handover def below is the exception):
  - `Bonds` → `Hostile` when every bond is hostile, else `Known`.
  - `Known`, `Hostile` and `Anyone` → that `TargetKind`.
- **TargetCondition:**
  - For `Bonds`: the bond is in the list AND `OnTarget(OtherIf)`.
  - For the other kinds: `OnTarget(OtherIf)`, or null when `OtherIf` is empty.
- **Opening** (`EventType.Triggered` unless noted):
  - `sick` → `OnAction.FellSick`;
  - `recovered` → `OnAction.Recovered`;
  - `visit` → `OnAction.AfterVisit`;
  - `death` → `OnAction.Bereaved`;
  - `break n` → no `OnAction`, with `BreakLevel: n` on the opener.
- **A handover scene's def** is `EventType.Response`, with `Target: None`, `TargetCondition: null`, `Condition: null`, `Trigger: null` and `BreakLevel 0`. This is what today's replies carry, and Task 1's fingerprint checks it.
- **`Effects()`:**
  - An outcome whose `Next` resolves to a handover scene adds `new AskTarget(Key(arc, scene))` instead of a `FollowUp`.
  - A `Range` pace becomes `FollowUp(Hours, MaxHours)`.
- **`Effect()`:**
  - `Coping` → `GainCoping(key == "" ? null : key)`;
  - `Couple` → `new Couple()`;
  - `Heartbreak` → `new Heartbreak()`;
  - `Bond`: `peace` → `SetAffinityAtLeast(0)`, `friends` → `SetAffinityAtLeast(PlynlingBonds.FriendsFrom + PlynlingBonds.BondMargin)`, `bestfriends` → `SetAffinityAtLeast(PlynlingBonds.BestFriendsFrom + PlynlingBonds.BondMargin)`.
- **`Options()`:**
  - The challenge is `new EventChallenge(r.Stat, r.Vs ? 0 : r.Difficulty, VsTarget: r.Vs)`.
  - The option passes `Stance: o.Stance`.

- [ ] **Step 1:** Write failing checks on the compiled fixtures:
  - The handover def is a `Response` with target `None`.
  - The asking option's `OnSuccess` is exactly `[AskTarget("test_handover.answer")]`, and `OwnerOnly` is true.
  - The answer options carry `Stance.Accept`/`Couple` and `Stance.Refuse`/`Heartbreak`.
  - The vs roll compiles to `EventChallenge(Courage, 0, true)`.
  - `[bond friends]` compiles to `SetAffinityAtLeast(30)`.
  - `pace: 24h-72h` compiles to `FollowUp(…, 24, 72)`.
  - The opener's `TargetCondition` is false for an acquaintance with `CanCouple` false, and true with `CanCouple` true and affinity 0.
  - `test_break` compiles to `Triggered`, `BreakLevel 2`, `Trigger null`, with `GainCoping(null)` and `GainCoping("journaller")`.
  - Inline arcs for `sick`, `recovered`, `visit` and `death` compile to their `OnAction`.
  - `Aliases` maps `test_everything_old` to its scene.
  - **(Review Focus 5)**, using `PlynlingEventStory.OutcomeProse(def, option, true, ctx)`:
    - The asking option's prose equals that option's own prose, and contains no line of the handover scene.
    - The handover def's `SceneText` is the handover scene's prose alone.
- [ ] **Step 2:** Run the checks. They should fail on these cases.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Run the build and the checks. Expected: they pass, and Task 1's blocks still match.
- [ ] **Step 5:** Don't commit. Report `git status`.

### Task 5: Move Asks and Moments to story files

**Files:**
- Create, under `ProjectSYNCS/Stories/`. Each arc lists its scenes as `scene` (old key, delay); scenes marked *handover* are the answers.

  | Folder | Arc | Scenes |
  |---|---|---|
  | `Friendship/` | `campfire` | `fire` (grown_campfire); `answer` *handover* (reply_campfire) |
  | `Friendship/` | `secret` | `secret` (teen_secret); `answer` *handover* (reply_secret) |
  | `Friendship/` | `befriend` | `plan` (teen_befriend); `rain` (teen_befriend_rain, `24h-48h`); `ask` (teen_befriend_ask, `24h-48h`); `answer` *handover* (reply_befriend) |
  | `Friendship/` | `pact` | `pact` (social_pact); `answer` *handover* (reply_pact) |
  | `Love/` | `declare` | `heart` (social_declare); `answer` *handover* (reply_declare) |
  | `Love/` | `letters` | `letter` (grown_letters); `more` (grown_letters_more, `24h-72h`); `bridge` (grown_letters_bridge, `24h-48h`); `answer` *handover*, a copy of the declaration reply with **no** `was:` |
  | `Rivalry/` | `rival` | `boast` (social_rival); `answer` *handover* (reply_rival) |
  | `Rivalry/` | `mend` | `market` (social_mend); `answer` *handover* (reply_mend) |
  | `Milestone/` | `teen` | `grown` (on_teen) |
  | `Milestone/` | `adult` | `grown` (on_adult) |
  | `Personal/` | `forgot` | `found` (on_visit_forgot); `shelf` (follow_forgot, `24h-72h`) |
  | `Personal/` | `sick` | `sniff` (on_sick) |
  | `Personal/` | `recovered` | `up` (on_recovered) |
  | `Hardship/` | `bereaved` | `empty` (on_bereaved) |

- Delete: `ProjectSYNCS/Helpers/Plynling/PlynlingEvents/PlynlingEvents.Asks.cs` and `PlynlingEvents.Moments.cs`.
- Modify: `PlynlingEvents.cs`. `All` becomes `{ Breaks(), GrowingUp(), StoryCatalog.Compiled }`.
- Modify: `tests/ProjectSYNCS.Checks/StoryEngineChecks.cs`:
  - `Reachability`: the asking-pulse count now counts every pulse in `All` with an `AskTarget` and stays ≥ 6.
  - `Stakes`: the `on_teen` « a C# def keeps its text » checks move to a hand-built `EventDef`.

**Headers:**

| Arc | Family | Stage | Trigger | Other | Other if |
|---|---|---|---|---|---|
| campfire | friendship | adult, elder | draw | anyone | `[relation acquaintances] [affinity >= 0]` |
| secret | friendship | teen | draw | friends, bestfriends | |
| befriend | friendship | teen | draw | anyone | `[relation acquaintances] [affinity >= 0]` |
| pact | friendship | adult, elder | draw | friends | |
| declare | love | adult, elder | draw | known | `[can_couple]` |
| letters | love | adult, elder | draw | known | `[can_couple]` |
| rival | rivalry | adult, elder | draw | hostile | |
| mend | rivalry | adult, elder | draw | hostile | |
| teen | milestone | teen | stage teen | | |
| adult | milestone | adult, elder | stage adult | | |
| forgot | personal | baby, teen, adult, elder | visit | anyone | |
| sick | personal | baby, teen, adult, elder | sick | | |
| recovered | personal | baby, teen, adult, elder | recovered | | |
| bereaved | hardship | baby, teen, adult, elder | death | anyone | |

- Every arc is `size: short`.
- Openers and handover scenes write `pace: now`.
- Stages are written in the enum's order.

**How each C# piece is written:**

| C# | In the file |
|---|---|
| `Ai((Boldness, 2))` | `[lean boldness +2]` |
| `GrowStat(s)` | `[grow s]` |
| `AffinityShift(n)` | `[affinity ±n]` |
| `StressChange(n)` | `[stress n]` |
| `ApplyModifier(k)` | `[modifier k]` |
| `GiveItem(k)` | `[item k]` |
| `LiftNeed(n, x)` | `[lift n x]` |
| `Try(…, new EventChallenge(s, d))` | `[roll s d]` with `? success` / `? failure` |
| `VsTarget` | `[roll s vs]` |
| `Ask(…)`, owner-only by default | `[owner-only]` + `[-> answer]`. Without `[owner-only]` for `tell`, `pact` and `mend`. |
| `Answer(…, Stance.X, …)` | `[accept]` / `[refuse]` |

- A plain outcome ends `[end]`.
- An outcome that brought a `FollowUp` ends `[-> <scene>]`, with its effects first, in C#'s order.

One complete file, as the shape to follow (`Love/declare.story`):

```
# arc: declare
title: Le cœur qui bat
family: love
stage: adult, elder
trigger: draw
size: short
other: known
other_if: [can_couple]

## scene: heart
title: Le cœur qui bat
was: social_declare
pace: now

Depuis quelque temps, {A} rougit chaque fois que {B} passe. Même de loin. Même de dos.

* declare | Tout avouer à {B}  [owner-only]
  [lean boldness +2] [lean sociability +1]
  {A} prend son courage à deux mains et va tout dire à {B}.
  [-> answer]

* wait | Garder ça pour soi encore un peu
  [lean boldness -1]
  {A} garde le secret, bien au chaud. Certaines choses mûrissent mieux à l'ombre.
  [grow learning] [end]

## scene: answer
title: Une déclaration
was: reply_declare
pace: now
handover

{B} est venu{b:|e} tout avouer à {A}, les joues rouges et la voix qui tremble.

* yes | Dire oui  [accept]
  [lean sociability +1] [lean compassion +1]
  {A} dit oui. {B} en oublie de respirer, puis rit, puis respire. Tout le hameau est au courant avant le soir : la Courge y a veillé.
  [couple] [end]

* no | Dire non, doucement  [refuse]
  [lean honor +1]
  {A} dit non, avec beaucoup de douceur. {B} hoche la tête et rentre par le chemin le plus long.
  [heartbreak] [end]
```

Each file starts with `//` lines naming the old keys it replaces. Write no other notes.

- [ ] **Step 1:** Write the 14 files from today's C#. Copy each text from the C# string literal: same characters, same `{b:|e}` tokens.
- [ ] **Step 2:** Delete the two C# files and update `All`.
- [ ] **Step 3:** Run the build and the checks.
  - Expected: they pass.
  - All 50 blocks match. The 26 moved keys resolve through `was:`, and `grown_letters_bridge`'s `ask { … }` matches its copied reply.
  - On a mismatch, fix the file, never `OldEvents.txt`.
- [ ] **Step 4:** Don't commit. Report `git status`.

### Task 6: Move GrowingUp and Breaks, and delete the C# catalog

**Files:**
- Create, under `ProjectSYNCS/Stories/`. Each arc lists its scenes as `scene` (old key, delay):

  | Folder | Arc | Scenes |
  |---|---|---|
  | `Personal/` | `dream` (baby) | `dream` (baby_grow_up); `explore`, `cafe`, `owl` and `chief` (grown_dream_*, `14d-16d`) |
  | `Personal/` | `apprentice` (teen) | `choice` (teen_apprentice); `owl` (teen_apprentice_owl, `2d-4d`); `station` and `market` (teen_apprentice_station / _market, `2d-4d`); `master_owl`, `master_station` and `master_market` (grown_master_*, `7d-10d`) |
  | `Hardship/` | `break_<name>`, one file per break (12) | one scene `break`, carrying `was: break_<name>` |

- Delete: `PlynlingEvents.Breaks.cs` and `PlynlingEvents.GrowingUp.cs`. In `PlynlingEvents.cs`, delete `Baby`, `Teen`, `Grown`, `AnyStage`, `Nothing`, `E`, `Stress`, `Ai`, `NoStress`, `Plain`, `Try`, `Ask` and `Answer`.
  - `All` becomes `StoryCatalog.Compiled`.
  - Rewrite the catalog comment to match: there are no hand-written events, and `Removed` keeps the deleted titles.
- Modify: `tests/ProjectSYNCS.Checks/StoryEngineChecks.cs` `Reachability`:
  - Drop the old-catalog walk; the story walker covers reachability now.
  - Add `Expect(PlynlingEvents.All.All(d => d.Story is not null))`.
  - Keep the removed-key, `TitleOf`, asking-pulse (≥ 6) and per-stage variety checks.

**Headers:**
- The dream is `family: personal`, `stage: baby`, `trigger: draw`.
- The apprenticeship is `family: personal`, `stage: teen`, `trigger: draw`.
- Each break is `family: hardship`, `stage: baby, teen, adult, elder`, `trigger: break <its level>`.
- All of them are `size: short`.

**Pieces with a new form:**
- `TraitGate("bossy")` is written `[needs trait bossy] [secret]`, with no lock text.
- `Stress(("honest", 10))` is written `[stress honest 10]`.
- `GainCoping()` is written `[coping]`, and `GainCoping("x")` is written `[coping x]`.

- [ ] **Step 1:** Write the 14 files.
- [ ] **Step 2:** Delete the C# files and the helpers, and set `All`. `PlynlingEvents.Removed.cs` stays as it is.
- [ ] **Step 3:** Run the build and the checks.
  - Expected: they pass, and all 50 blocks match.
  - The `ado baseline` lines show the shift. Accept it, and add no scene-level `stage:`; that is the builder's call the spec allows. Copy the printed before → now lines into the task report.
- [ ] **Step 4:** Don't commit. Report `git status`.

### Task 7: Old keys wherever stored keys are compared

**Files:**
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingEvents/PlynlingEvents.cs`, `ProjectSYNCS/Helpers/Plynling/Stories/StoryRuns.cs` and `ProjectSYNCS/Services/Plynling/PlynlingService.Events.cs`:
  - line 137: `PulseKeys`;
  - lines 184–189: `recent`, `waiting`;
  - line 240: `BreakKeys`;
  - line 618: the on-action dedupe.
- Modify: `ProjectSYNCS/Services/Plynling/PlynlingService.Stories.cs` (lines 20–21), `ProjectSYNCS/Helpers/Plynling/PlynlingReport.cs` (line 69) and `ProjectSYNCS/Commands/Admin/DebugModule/DebugModule.Tools.cs` (lines 94, 127).
- Test: `tests/ProjectSYNCS.Checks/StoryEngineChecks.cs` (`Lookups`, `Runs`).

**Interfaces** (all produced here):
- `PlynlingEvents.StoredKeys(Func<EventDef, bool> which) : string[]` returns every key `ByKey` resolves to a matching def: live keys, `was:` keys and retired-scene keys.
- `PlynlingEvents.Current(string storedKey) : string` returns `ByKey(storedKey)?.Key ?? storedKey`.
- `StoryRuns.Openers(IEnumerable<EventDef> defs, IReadOnlyDictionary<string, EventDef> aliases)` is the existing map plus each alias whose def is its arc's first scene. Remove the one-argument overload, or keep it delegating with an empty alias map.
- `PlynlingEvents.Aliases : IReadOnlyDictionary<string, EventDef>` (the `StoryCatalog.Aliases` map, exposed for the line above).

**Service changes** (the same shape at each site):
- `PulseKeys = PlynlingEvents.StoredKeys(d => d.Type == EventType.Pulse && d.BreakLevel == 0)`.
- `BreakKeys = PlynlingEvents.StoredKeys(d => d.BreakLevel > 0)`.
- `recent` and `waiting` are mapped through `PlynlingEvents.Current` after `ToListAsync`.
- The dedupe becomes `ReferenceEquals(PlynlingEvents.ByKey(i.EventKey), def)`.
- `StoryOpeners = StoryRuns.Openers(PlynlingEvents.All, PlynlingEvents.Aliases)`.
- The owner report groups by `PlynlingEvents.Current(e.EventKey)`.
- `/debug event`: drop « événements classiques » from the key-list heading, since every key is a story's first scene now.

- [ ] **Step 1:** Write failing checks (each is one `Expect`):
  - **(Review Focus 3):**
    - `StoredKeys(d => d.BreakLevel > 0)` contains `break_cloud` and `break_cloud.break`.
    - `StoredKeys(pulse)` contains `grown_campfire` and `campfire.fire`, and contains no response key (`reply_declare`).
  - **(Review Focus 2):** `Current("grown_campfire") == "campfire.fire"`, and `Current("nope") == "nope"`.
  - **(Review Focus 1):**
    - `ByKey("grown_letters_more")` has options `meet` and `riddle`, and each `FollowUp` in them resolves to a def whose `Story.Scene.Was == "grown_letters_bridge"`.
    - `ByKey("reply_declare")` is a `Response` with options `yes` and `no`.
  - **(Review Focus 4):**
    - `AdoHistory([(ByKey("teen_apprentice")!.Options.Single(o => o.Key == "magpie"), true)]).Count == 1`.
    - `TitleOf("on_teen") == "Plus tout à fait bébé"`.
    - `StoryRuns.Openers(All, Aliases)["on_teen"].Key == "teen"`.
    - `StoryRuns.Starts([("on_teen", t0)], openers).LastStarted["teen"] == t0`.
- [ ] **Step 2:** Run the checks. They should fail on these cases.
- [ ] **Step 3:** Implement. Then grep `ProjectSYNCS/` for `EventKey ==`, `.Contains(i.EventKey)` and `EventKey)` to confirm no other site compares raw stored keys with computed def keys. Name any site you find in the report, and fix it the same way.
- [ ] **Step 4:** Run the build and the checks. Expected: they pass.
- [ ] **Step 5:** Don't commit. Report `git status`.

### Task 8: Docs and release

**Files:**
- Modify: `ProjectSYNCS/config.yaml` (`version: "7.74.3"`) and `ProjectSYNCS/CHANGELOG.md`. The new entry is `## 7.74.3 — 2026-11-19`, with the bullet « The last hand-written Plynling events (asks, moments, growing up, mental breaks) now live in story files; nothing changes in play. »
- Modify: `docs/agents/plynling-storylines.md`:
  - the new words, with one line each and its reason;
  - the handover rules, including that effects land on the decider and that the run ends at the answer;
  - `was:` and the stored-key rule (« compare through `ByKey`/`StoredKeys`/`Current`, never raw keys »);
  - short events allow 1–3 decisions.

  Keep the file under about 40 KB. Split it into a folder index if it grows past that.
- Modify: `docs/agents/plynling-events.md`. There is no C# catalog any more; `Removed` holds the titles of deleted events.
- Modify: `docs/superpowers/specs/2026-10-29-plynling-story-file-format-design.md`. Add a one-line pointer to the 2026-11-19 spec at its handover and triggers sections.
- Modify: `tests/ProjectSYNCS.Checks/OldEventChecks.cs`. Delete the `-- old-events write` mode: there is no C# left to snapshot, and the frozen file stays.

- [ ] **Step 1:** Make the edits.
- [ ] **Step 2:** Run the build and the checks. Expected: they pass.
- [ ] **Step 3:** Write the dev-guild checklist in the final report. The owner plays it, since it needs two owners:
  - an ask answered by a second owner;
  - a break, forced with `/debug event break_cloud.break`;
  - a stage moment;
  - an instance queued with `/debug event grown_letters_more` before updating, then resolved after.
- [ ] **Step 4:** Don't commit. Report `git status`, and name the version, 7.74.3.
