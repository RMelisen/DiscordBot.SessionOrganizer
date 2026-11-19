# Plynling visits with two choices — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the told visit story with one short scene in which the visitor's owner picks an option, the host's owner picks a reaction, and both choices — judged by the other Plynling's personality — sway a hidden roll; confessions become the visitor's choice.

**Architecture:** Visit scenes are a new file kind, `ProjectSYNCS/Stories/Visits/*.visit`, read by a small `VisitParser` that reuses the story format's line reader, tag layer, vocabulary, token rules and worst-case measure, checked by `VisitValidator`, served by a lazy `VisitCatalog` that stops the bot on any error (like `StoryCatalog`). Pure odds live in `VisitOdds`; `PlynlingService.VisitAsync` takes the players' play (shift, confess) instead of rolling everything blind; one public message carries the visit from the visitor's options to the knock to the ending.

**Tech Stack:** C# / .NET 10, Discord.Net 3.20 (Components V2), EF Core + SQLite, the plain-console checks project `tests/ProjectSYNCS.Checks`.

**Spec:** `docs/superpowers/specs/2026-11-19-plynling-visit-choices-design.md`

## Global Constraints

- **Never run `git commit`.** The owner commits by hand; each task ends with a hand-off (what changed, the checks output), not a commit.
- Read `CLAUDE.md`, `docs/agents/plynling.md` (*Visits*, *Relationships*) and `docs/agents/plynling-storylines.md` before touching code; their rules bind.
- Command and option names English; every user-facing string French; code, comments, logs English.
- Odds are never shown: no 🎲, no percentage, no stakes line on any visit button.
- Shift cap: at most **±0.075 per choice**, **±0.15 together**; final scale set by the simulation (Task 5).
- Confession acceptance base: **0.5 + 0.35 × compatibility / 20** (50 % average, 85 % / 15 % at the extremes), then the shift, clamped 0.05–0.95.
- After a refused confession, **no confession for that pair, either direction, for 3 days** (`RefusalCooldown`).
- A confession visit counts as a **good scene** (+12…17 affinity, finds, closeness relief); refused costs `HeartbreakLoss` (25), sadness, `Heartbroken` moments.
- Confessions: a boy and a girl only, one living partner at a time (`PlynlingBonds.CanConfess`) — unchanged.
- Once a day per pair (stored `LastVisitDay` + `PlynlingCooldowns` claim, released on failure or throw) — unchanged. The host's click **defers before any database work**.
- The message that shows the visitor's options pings the invited owner only (`AllowedMentions` with that id) — except from `;pl visit`, which never pings. Every later state is an edit (`UpdateAsync` / `ModifyOriginalResponseAsync`) with `MessageFlags.ComponentsV2` re-asserted and `AllowedMentions.None`.
- Every button its own custom-id; custom-ids ≤ 100 chars. Button labels ≤ 80 chars at worst case (32-char names). ≤ 40 components per V2 message.
- `{A}` is always the visitor, `{B}` always the host; tokens `{A}` `{B}` `{a:m|f}` `{b:m|f}` `{p:m|f}` — rendered with `PlynlingVisitStory.Expand`.
- Keys (scene, option, reaction) are forever: never renamed or reused.
- Prose follows `docs/plynling-writing-style.md` and `docs/village-lore.md`.
- Version: **minor** bump to `8.2.0` in `ProjectSYNCS/config.yaml` with a `CHANGELOG.md` entry (adds a migration and new behaviour) — once, in Task 10.

## Review Focus

- **Double click on a host reaction** (or two reactions pressed at once): exactly one visit happens; the second click is told « déjà vus » privately — pinned in Task 8.
- **Someone other than the visitor's / host's owner pressing a button**: private refusal, the message unchanged — pinned in Task 8.
- **A button pressed after expiry, after a restart, or naming a scene/option key no longer in the catalog**: the message closes as expired, nothing saved — pinned in Task 8.
- **Visitor dies, freezes or falls sick between the pick and the host's reaction**: private refusal, claim released, no visit — pinned in Task 8.
- **Confession chosen while eligible, no longer eligible at the host's click** (couple formed elsewhere, refusal cooldown started by a visit the other way): « trop tard », nothing saved, claim released — pinned in Task 4.

---

### Task 1: Shortlist and writing sheet (owner review gate)

**Files:**
- Create: `docs/plynling-visit-scenes-sheet.md`

No code. This runs first so the owner can review while Tasks 2–6 are built; Task 7 waits for the approval.

- [x] **Step 1: Write the shortlist** — 20 scenes + 1 confession: key (≤ 20 chars, `[a-z0-9_]`), title, place (emoji + name, from `docs/village-lore.md`), bonds (`acquaintances friends bestfriends lovers rivals enemies`), hours (omit = all day), stages (omit = all), one-line premise. Spread so every bond has ≥ 4 eligible scenes and **every bond has at least one all-day, all-stage scene** (the coverage rule of Task 3).
- [x] **Step 2: Write each scene in full** in the sheet, in the file shape of Task 2: intro (2–4 sentences), 2–4 options with label, 1–2 leans, good and bad half; 2–4 reactions likewise. Reaction halves must read right after **any** option. The confession scene: options are ways of confessing, halves `? yes` / `? no`; its intro is what the knock shows (it says plainly that `{A}` came to confess).
- [x] **Step 3: Hand to the owner for review.** Do not start Task 7 before the owner approves the sheet.

### Task 2: Visit file model and parser

**Files:**
- Create: `ProjectSYNCS/Helpers/Plynling/Visits/VisitModel.cs`, `ProjectSYNCS/Helpers/Plynling/Visits/VisitParser.cs`
- Modify: `ProjectSYNCS/Helpers/Plynling/Stories/StoryParser.cs` (make `Src` and `Read` `internal`), `ProjectSYNCS/Helpers/Plynling/Stories/StoryVocabulary.cs` (extract lean parsing), `ProjectSYNCS/Helpers/Plynling/Stories/StoryParser.Scenes.cs` (use it)
- Create fixtures: `tests/ProjectSYNCS.Checks/Visits/test_visit.visit`, `tests/ProjectSYNCS.Checks/Visits/test_confession.visit`
- Test: `tests/ProjectSYNCS.Checks/VisitChecks.cs` (new, wired into `Program.cs` next to `StoryChecks.Run`)

**Interfaces:**
- Produces:
  ```csharp
  public sealed record VisitHalves(string Good, string Bad);          // confession: Good = yes, Bad = no
  public sealed record VisitChoice(string Key, string Label, IReadOnlyDictionary<AiAxis, int> Lean, VisitHalves Halves, StoryPos Pos);
  public sealed record VisitScene(string Key, bool Confession, string Title, string Place,
      IReadOnlySet<PlynlingBond> Bonds, int FromHour, int ToHour, IReadOnlySet<PlynlingStage> Stages,
      string Intro, IReadOnlyList<VisitChoice> Options, IReadOnlyList<VisitChoice> Reactions, StoryPos Pos)
  { public bool IsOpenAt(int hour); }     // same rule as VisitPlace.IsOpenAt
  public static class VisitParser { public static VisitScene? Parse(string file, string text, List<StoryError> errors); }
  // StoryVocabulary:
  public static bool TryLean(IReadOnlyList<string> words, out AiAxis axis, out int amount);   // ["lean", axis, ±n]
  ```

The file shape (fixtures hold every word):

```
# visit: kite                       ← or '# confession: confess'
title: Le cerf-volant
place: 🌳 Sous le grand chêne
bonds: acquaintances friends bestfriends    ← not in a confession file
hours: 8-20                          ← optional; FromHour-ToHour, Paris; across midnight allowed (20-1)
stages: teen adult elder             ← optional; default all four

Intro prose, one paragraph or more.

* climb | Grimper le chercher  [lean boldness +2] [lean energy +1]
  ? good                             ← '? yes' / '? no' in a confession file
  {A} grimpe et décroche le cerf-volant du premier coup.
  ? bad
  [sad] {A} reste coincé{a:|e} à mi-hauteur.

> hold | Tenir le tronc  [lean compassion +1]
  ? good
  …
  ? bad
  …
```

Defaults: no `hours` = 0-24; no `stages` = all. Face tags (`[sad]`, `[A:… B:…]`) at the start of a half are kept in the text (rendering untags them). A half's lines join with one space.

- [x] **Step 1: Write the failing checks** in `VisitChecks.Parser()`:
  - `test_visit.visit` parses with no errors: key `kite`-like, `Confession == false`, 2+ options, 2+ reactions, each with both halves, a lean read back exactly (`Lean[AiAxis.Boldness] == 2`), `IsOpenAt` true inside the hours and false outside, an across-midnight fixture scene open at 23 and 0.
  - `test_confession.visit` parses with `Confession == true`, `Bonds` empty, halves from `? yes` / `? no`.
  - Each of these, parsed from an inline string, yields exactly one error naming the right line: unknown header field; `bonds:` in a confession file; `? good` in a confession file; `? yes` in a visit file; option missing `? bad`; `[lean bravery +1]` (unknown axis); `[roll …]` on an option (not supported); a `## section`; a CRLF + BOM file parses like its LF twin.
- [x] **Step 2: Run** `dotnet run --project tests/ProjectSYNCS.Checks` — expected: build fails (types missing).
- [x] **Step 3: Implement** `VisitModel.cs`, `VisitParser.Parse`, `StoryVocabulary.TryLean` (and switch `StoryParser.Scenes.cs`'s `case "lean"` to it, same error text). Errors in English, every problem reported, parsing goes on.
- [x] **Step 4: Run** the checks — expected: no `FAIL visit:` line, and no new `FAIL story:` line.
- [x] **Step 5: Hand-off** (no commit).

### Task 3: Validator, library and catalog

**Files:**
- Create: `ProjectSYNCS/Helpers/Plynling/Visits/VisitValidator.cs`, `VisitLibrary.cs`, `VisitCatalog.cs` (same folder)
- Modify: `ProjectSYNCS/Helpers/Plynling/Stories/StoryValidator.cs` (expose `public static IEnumerable<string> BadTokens(string text)` from the token loop at lines 316–318; the story validator calls it), `ProjectSYNCS/Helpers/Plynling/Stories/StoryLibrary.cs` (skip files ending `.visit` without an error)
- Test: `tests/ProjectSYNCS.Checks/VisitChecks.cs`

**Interfaces:**
- Consumes: Task 2's model and parser.
- Produces:
  ```csharp
  public static class VisitValidator
  {
      public static IReadOnlyList<StoryError> Validate(IReadOnlyList<VisitScene> scenes);   // per-scene and cross-scene rules
      public static IReadOnlyList<StoryError> Coverage(IReadOnlyList<VisitScene> scenes);   // the draw can never come up empty
  }
  public sealed record VisitLoad(IReadOnlyList<VisitScene> Scenes, IReadOnlyList<StoryError> Errors);
  public static class VisitLibrary { public static VisitLoad Load(string folder, bool requireCoverage); }   // *.visit under folder
  public static class VisitCatalog
  {
      public static string Folder { get; set; }                 // default AppContext.BaseDirectory/Stories/Visits
      public static IReadOnlyList<VisitScene> Scenes { get; }   // ordinary scenes; throws StoryLoadException on any error
      public static VisitScene Confession { get; }
      public static VisitScene? ByKey(string key);              // either kind
      public static IReadOnlyList<VisitScene> Eligible(PlynlingBond bond, int hour, PlynlingStage visitor, PlynlingStage host);
  }
  ```

Rules `Validate` enforces (one error each, with file and line):
scene key ≤ 20 chars, option/reaction keys ≤ 12 chars (custom-id budget, see Task 6), keys unique across files and within a scene (options and reactions share one namespace); 2–4 options and 2–4 reactions; every choice has 1–2 leans, each a non-zero −3…+3; labels non-empty and ≤ 80 at `StoryValidator.WorstCase`; title, place, intro and both halves non-empty; no bad tokens (`BadTokens`) and no `{T}`; an ordinary scene has ≥ 1 bond; exactly one confession scene in the set.
`Coverage`: for each of the 6 bonds × 24 hours × 16 (visitor stage, host stage) pairs, `Eligible` is non-empty.
Eligibility: `Bonds` contains the bond, `IsOpenAt(hour)`, both stages in `Stages`.

- [x] **Step 1: Write the failing checks**: the fixtures folder loads with no `Validate` error; one inline case per rule above produces its error; `Coverage` on the fixtures alone reports missing cells, and on a synthetic all-bonds, all-day, all-stages scene reports none; `StoryLibrary.Load` over a folder holding a `.visit` file reports no "not a .story file" error; `VisitCatalog` pointed at a folder with an error throws `StoryLoadException` on `Scenes`, and keeps throwing.
- [x] **Step 2: Run** the checks — expected: build fails.
- [x] **Step 3: Implement** the four pieces; `VisitCatalog` mirrors `StoryCatalog`'s lazy, lock-guarded load. Do **not** touch `Program.cs` yet (no shipped scenes exist until Task 7).
- [x] **Step 4: Run** the checks — expected: no `FAIL visit:`, no new `FAIL story:`.
- [x] **Step 5: Hand-off.**

### Task 4: Odds and the visit's save

**Files:**
- Create: `ProjectSYNCS/Helpers/Plynling/Visits/VisitOdds.cs`
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingBonds.cs` (remove `ConfessionChance` / `RollConfession`; add `RefusalCooldown`, `ConfessionCoolingDown`), `ProjectSYNCS/Models/Plynling/PlynlingRelation.cs` (`DateTimeOffset? LastRefusalAt`), `ProjectSYNCS/Services/Plynling/PlynlingService.cs` (`VisitPlay`, `VisitOutcome.TooLate`), `ProjectSYNCS/Services/Plynling/PlynlingService.Relations.cs` (`VisitAsync`, `ConfessionOpenAsync`)
- Create: migration `AddRelationLastRefusal` (`dotnet ef migrations add AddRelationLastRefusal` from `ProjectSYNCS/`)
- Test: `tests/ProjectSYNCS.Checks/VisitChecks.cs` (pure odds; service against in-memory SQLite, following `BribeChecks`' harness)

**Interfaces:**
- Produces:
  ```csharp
  public static class VisitOdds
  {
      public const double MaxShiftPerChoice = 0.075;
      public const double LeanSoftness = 300;    // placeholder; Task 5 sets the final value
      // Σ lean[a] × judge[a] through MaxShiftPerChoice × tanh(match / LeanSoftness): sign of the match, never past the cap.
      public static double Shift(IReadOnlyDictionary<AiAxis, int> lean, IReadOnlyDictionary<AiAxis, int> judgeAxes);
      public static double GoodChance(int compatibility, PlynlingBond bond, int affinity, double shift);   // clamp(GoodSceneChance + shift, .05, .95)
      public static double AcceptChance(int compatibility, double shift);                                // clamp(.5 + .35·c/20 + shift, .05, .95)
      public static VisitChoice MascotReaction(VisitScene scene, IReadOnlyDictionary<AiAxis, int> herAxes); // best Σ lean×axes, ties: file order
  }
  // PlynlingBonds:
  public static readonly TimeSpan RefusalCooldown = TimeSpan.FromDays(3);
  public static bool ConfessionCoolingDown(DateTimeOffset? lastRefusalAt, DateTimeOffset now);
  // PlynlingService:
  public sealed record VisitPlay(double Shift, bool Confess);
  // VisitOutcome gains: bool TooLate = false   (a confession no longer possible: nothing saved)
  public Task<VisitOutcome?> VisitAsync(int visitorId, int hostId, DateTimeOffset now, VisitPlay play, Random? rng = null, Random? findRng = null);
  public Task<bool> ConfessionOpenAsync(Plynling visitor, Plynling host, DateTimeOffset now);   // CanConfess on the stored relation && !cooling down
  ```
  The judge's axes come from `PlynlingPersonality.Axes(await GetTraitsAsync(p))`.

`VisitOnceAsync` with `play.Confess`: re-check `CanConfess` + cooldown on the stored relation **before** any change (no `LastVisitDay`, no save) → `TooLate = true`. Otherwise: good scene, `+rng.Next(12, 18)`, then `rng.NextDouble() < AcceptChance(compat, play.Shift)`; refused → `−HeartbreakLoss`, `LastRefusalAt = now`, sadness and moments as today. Without `Confess`: `good = rng.NextDouble() < GoodChance(…, play.Shift)`, deltas as `RollScene`; **no automatic confession**. Everything after (bond, happiness, relief, finds, badges, on-actions) unchanged.

- [x] **Step 1: Write the failing checks**:
  - `Shift`: matching lean on a bold judge (`Boldness = 200`) > 0; same lean on a craven judge < 0; empty judge axes → 0; huge match never exceeds 0.075 in size.
  - `GoodChance(0, Acquaintances, 0, 0) == GoodSceneChance(0, Acquaintances, 0)`; `+0.075` raises it by 0.075; clamped at 0.95.
  - `AcceptChance(0, 0) == 0.5`, `(20, 0) == 0.85`, `(−20, 0) == 0.15` (±1e-9).
  - `ConfessionCoolingDown(now − 71h, now)` true, `(now − 73h, now)` false, `(null, now)` false.
  - `MascotReaction` returns the reaction leaning her way; on a tie, the first in file order.
  - Service (seeded `Random`): a confess play on an eligible boy/girl pair with a forced accept → `After == Lovers`, `Confession.Accepted`; forced refusal → `LastRefusalAt == now`, affinity −25 net of the delta; a confess play right after that refusal → `TooLate`, `LastVisitDay` unchanged; an ordinary play never yields a confession, even eligible and lucky.
- [x] **Step 2: Run** — expected: build fails.
- [x] **Step 3: Implement**; generate the migration (schema-only); fix every caller of `VisitAsync` / `RollConfession` the build names (`PlynlingVisitRunner` passes `new VisitPlay(0, false)` until Task 8).
- [x] **Step 4: Run** the checks and `dotnet build ProjectSYNCS` — expected: no `FAIL visit:`, build clean.
- [x] **Step 5: Hand-off.**

### Task 5: The bond simulation

**Files:**
- Create: `tests/ProjectSYNCS.Checks/VisitSimulation.cs`
- Modify: `ProjectSYNCS/Helpers/Plynling/Visits/VisitOdds.cs` (final `LeanSoftness`), `docs/agents/plynling.md` (*Relationships*: the new numbers)

**Interfaces:**
- Consumes: `VisitOdds`, `PlynlingBonds` (`GoodSceneChance`, `BondFor`, `CanConfess` rules, `HeartbreakLoss`, `RefusalCooldown`), `PlynlingTraits.Draw` for trait spreads, the fixture or shipped scenes for leans.
- Produces: `VisitSimulation.Run(Action<string> fail, string[] args)`, wired into `Program.cs`; prints its table only with `-- visit-sim`.

One visit per pair per day. Three players: **baseline** (today's rules, the old `RollConfession` copied into this file as `BaselineConfession`), **random** (uniform option and reaction; confesses whenever offered with probability 1 / (options + 1)), **best** (the choices with the highest shift; confesses whenever offered). Pairs: random ids → compatibility via `PlynlingBonds.Compatibility`, traits via `PlynlingTraits.Draw` at adult.

- [x] **Step 1: Write the checks**: baseline over 100 visits reproduces `plynling.md`'s spread (12/18/21/19/13/16 % ± 3 points each), 30 % ± 4 couples within 60 visits for boy/girl pairs, median couple time at compatibility +10 of 51 ± 6 visits. Random: each band within ± 3 points of baseline, couples within 60 visits within ± 5 points. Best: median couple time ≥ 0.8 × baseline's.
- [x] **Step 2: Run** with the placeholder `LeanSoftness` — expected: baseline passes (if not, the copy of today's rules is wrong — fix it before tuning); record what random/best give.
- [x] **Step 3: Tune** `LeanSoftness` (and only if needed the per-choice cap, never above 0.075) until random and best pass. If no value passes, stop and report to the owner with the table — do not change other constants.
- [x] **Step 4: Run** — expected: no `FAIL visit-sim:`. Write the three tables' headline numbers into `plynling.md` next to the old ones.
- [x] **Step 5: Hand-off** with the table.

### Task 6: The three cards

**Files:**
- Create: `ProjectSYNCS/Commands/Plynling/PlynlingVisitCards.cs`
- Test: `tests/ProjectSYNCS.Checks/VisitChecks.cs`

**Interfaces:**
- Consumes: Task 2 model, `PlynlingVisitStory.Expand` / `Untag`, `PlynlingArt.VisitSprite`, `PlynlingPlayCards.VisitOutcomeLines`, `PlynlingCardUi.SafeName`.
- Produces:
  ```csharp
  public static class PlynlingVisitCards
  {
      // custom-ids — each verb its own; ids parsed back by Task 8's handlers
      public static string ChoiceId(int visitorId, ulong hostOwnerId, long expiresUnix, string scene, string option);           // "pvc:…"
      public static string ConfessId(int visitorId, ulong hostOwnerId, long expiresUnix);                                       // "pvx:…"
      public static string ReactionId(int visitorId, ulong hostOwnerId, long expiresUnix, string scene, string option, string reaction); // "pvr:…"
      public static MessageComponent BuildChoice(VisitScene scene, Plynling visitor, ulong hostOwnerId, DateTimeOffset expires, bool offerConfession, DateTimeOffset now);
      public static MessageComponent BuildKnock(VisitScene scene, VisitChoice option, Plynling visitor, ulong hostOwnerId, DateTimeOffset expires, DateTimeOffset now);
      public static MessageComponent BuildEnding(VisitScene scene, VisitChoice option, VisitChoice reaction, VisitOutcome outcome);
      public static MessageComponent BuildClosed(string text);   // expired, too late, old knock
  }
  ```

Content (French): the choice card — `## {place}`, the visitor's sprite, « **{A}** part rendre visite à <@host> » (the mention is what the first send pings; the caller's `AllowedMentions` lists the host only, so the visitor's own mention below stays silent), the intro, « Que fait **{A}** ? », options as buttons (+ « 💌 Lui avouer ses sentiments » when `offerConfession`), « Réservé à <@visitorOwner> · expire <t:…:R> ». The knock — place, intro, « **{A}** a choisi : {label, first letter lowercased} », « <@host>, que fait **{B}** ? », reactions as buttons, expiry line. The ending — place, the two visit sprites (faces: good → `Faces(mood)` narration face; bad → angry, or sad between friends or closer; a refused confession or break-up → sad, accepted → happy, new enemies → angry — reuse `OutcomeFace`; a face tag at the start of a half wins over the default for that Plynling), the option's half, the reaction's half, then `VisitOutcomeLines`. `PlynlingVisitStory.Faces` and `OutcomeFace` become `internal`.

- [x] **Step 1: Write the failing checks**: for every fixture scene × every option × every reaction × good/bad × the four gender pairs, with 32-char names: the ending card builds, ≤ 40 components, total text ≤ 4000; choice and knock cards build with `offerConfession` both ways; every custom-id ≤ 100 chars with `int.MaxValue`, `ulong.MaxValue`, a 20-char scene key and 12-char choice keys; all ids on one card distinct; no button label > 80; no rendered text still holds `{` or a leading `[`.
- [x] **Step 2: Run** — expected: build fails.
- [x] **Step 3: Implement.**
- [x] **Step 4: Run** — expected: no `FAIL visit:`.
- [x] **Step 5: Hand-off.**

### Task 7: The 20 + 1 scene files

**Files:**
- Create: `ProjectSYNCS/Stories/Visits/<key>.visit` × 21, from the **approved** sheet of Task 1
- Modify: `ProjectSYNCS/Program.cs` (touch `VisitCatalog.Scenes` and `VisitCatalog.Confession` right after `StoryCatalog.Compiled`, inside the same catch that logs each error at `Critical` and rethrows); `tests/ProjectSYNCS.Checks/VisitChecks.cs`

- [x] **Step 1: Add the check**: `VisitLibrary.Load(<repo>/ProjectSYNCS/Stories/Visits, requireCoverage: true)` has no error; the Task 6 card walk also runs over the shipped scenes.
- [x] **Step 2: Run** — expected: fails (folder empty).
- [x] **Step 3: Write the files** exactly as approved (wording changes go back to the owner).
- [x] **Step 4: Run** the checks and `dotnet build ProjectSYNCS` — expected: no `FAIL`; the build output has `Stories/Visits/*.visit` (csproj `Stories\**\*` already copies them — verify in `bin/`).
- [x] **Step 5: Hand-off.**

### Task 8: The flow — visitor's pick, host's reaction, the mascot

**Files:**
- Modify: `ProjectSYNCS/Commands/Plynling/PlynlingModule/PlynlingModule.cs` (`VisitAsync`, `OpenVisitAsync`, `VisitOpening`), `ProjectSYNCS/Services/Plynling/PlynlingShortcutService.cs` (the `;pl visit` path), `ProjectSYNCS/Services/Plynling/PlynlingVisitRunner.cs`, `ProjectSYNCS/Interactions/Components/PlynlingComponentHandler.cs`, `ProjectSYNCS/Helpers/Plynling/PlynlingText*.cs` (new refusals: `VisitTooLate`, choice not yours)
- Test: `tests/ProjectSYNCS.Checks/VisitChecks.cs` (runner against in-memory SQLite)

**Interfaces:**
- Consumes: Tasks 3, 4, 6.
- Produces:
  ```csharp
  // PlynlingVisitRunner (replaces RunAsync's story return)
  public Task<(VisitOutcome? Outcome, string? Refusal, IReadOnlyList<int> Untold)> RunAsync(
      Plynling visitor, Plynling host, VisitScene scene, VisitChoice option, VisitChoice reaction, DateTimeOffset now);
  // computes VisitPlay: Shift(option.Lean, hostAxes) + Shift(reaction.Lean, visitorAxes), Confess = scene.Confession
  // TooLate → releases the claim, Refusal = PlynlingText.VisitTooLate
  public static VisitScene DrawScene(PlynlingBond bond, DateTimeOffset now, PlynlingStage visitor, PlynlingStage host, ResponsePicker picker);
  ```

- `OpenVisitAsync`: same checks; then `DrawScene` (bond before, Paris hour, both stages; picker over scene keys) and `ConfessionOpenAsync`; returns the choice card (mascot host too — she has no knock, but her visitor still picks).
- `pvc:` (visitor's owner only): expired → `BuildClosed(InviteExpired)`; unknown scene/option key → same; mascot host → **defer**, re-check, `MascotReaction`, `RunAsync`, edit to `BuildEnding`, her `MascotWelcomeLines` line above it, then `_announcer.TellAsync` for `Untold`; otherwise `UpdateAsync` to `BuildKnock` (no DB work — no defer needed).
- `pvx:` (visitor's owner only): `UpdateAsync` to `BuildChoice(VisitCatalog.Confession, …, offerConfession: false)`.
- `pvr:` (host's owner only): expired / unknown key → closed; **defer**; the refusal chain of today's `OnVisitAcceptedAsync`; `RunAsync`; refusal → ephemeral follow-up (too late → also edit to `BuildClosed`); success → `ModifyOriginalResponseAsync` to `BuildEnding`, then `TellAsync`.
- `plyn:visit:*:*:*` (old knocks): `UpdateAsync` to `BuildClosed(PlynlingText.InviteExpired)`, nothing else.
- The `vis:*` pager handlers and `VisitStories` stay untouched (old stories page until they expire).

- [x] **Step 1: Write the failing checks** (runner, seeded): a normal run saves one visit and returns an outcome; a second `RunAsync` for the pair the same day → « déjà vus » refusal, nothing saved (Review Focus: double click); visitor frozen between pick and reaction → refusal, `PlynlingCooldowns.VisitedToday` false afterwards; confession `TooLate` → refusal `VisitTooLate`, claim released; `DrawScene` never returns a scene ineligible for the inputs, across all bonds × 24 hours × stage pairs on the shipped catalog.
- [x] **Step 2: Run** — expected: fails.
- [x] **Step 3: Implement** the runner, `DrawScene`, the handlers (`[ComponentInteraction(..., ignoreGroupNames: true)]`), `OpenVisitAsync` and the shortcut path; the visitor-only / host-only checks answer `NotYourInvite` (or a new « Ce choix ne t'appartient pas. ») ephemerally and leave the message unchanged.
- [x] **Step 4: Run** the checks and `dotnet build ProjectSYNCS -warnaserror` — expected: clean.
- [x] **Step 5: Dev-guild play-through** (waived by the owner, 2026-11-19) (`dotnet run` in `ProjectSYNCS/`): `/plynling visit` → pick → second account reacts → ending; `;pl visit` (no ping); a confession on a forced eligible pair (`/debug` tools); a stranger clicking each button; an expired knock (wait or edit expiry); an old `plyn:visit` knock from before the deploy; a visit to her Plynling. Report what was seen.
- [x] **Step 6: Hand-off.**

### Task 9: Remove the told story

**Files:**
- Delete: `ProjectSYNCS/Helpers/Plynling/PlynlingScripts/` (whole folder), `ProjectSYNCS/Helpers/Plynling/PlynlingVisitStory/PlynlingVisitStory.Arrivals.cs`, `.Exchanges.cs`, `.Endings.cs`, `.Typed.cs`
- Modify: `PlynlingVisitStory.cs` (keep `VisitCast`, `VisitBeat`, `VisitStory`, `FaceTag`, `VisitMood`, `MoodFor`, `Faces`, `OutcomeFace`, `Untag`, `Expand`; delete `Places`, `VisitPlace`, `PlaceFor`, `Build`, `ActivityPool`, `PickSubject`, typed-moment code), `BotResponses.Plynlings.Play.cs` / `BotResponses.cs` (remove `PlynlingVisitKnockLines`, `PlynlingVisitKnockTypedLines`), `PlynlingPassions.cs` (drop the knock's typed pick), `PlynlingPlayCards.cs` (remove `VisitId`, old `BuildKnock`; keep `BuildVisitStory` and the pager row for old stories), `tests/ProjectSYNCS.Checks/Program.cs` (drop the sections checking removed pools/scripts — the build names them)

- [x] **Step 1: Delete and trim**, then build: `dotnet build ProjectSYNCS -warnaserror` and `dotnet run --project tests/ProjectSYNCS.Checks` — expected: clean, no `FAIL`. Remove only checks of deleted code; a failing check of kept code is a bug to fix, not to delete.
- [x] **Step 2: Grep** the whole repo for `PlynlingScripts`, `PlaceFor`, `PlynlingVisitKnock`, `plyn:visit` — expected: only the old-knock handler and docs history.
- [x] **Step 3: Hand-off.**

### Task 10: Docs, help, version

**Files:**
- Modify: `docs/agents/plynling.md` (*Visits* rewritten; *Conversations — PlynlingScripts* removed; the passions paragraph about visits trimmed; a line on the 30-day removal of `vis:*` / `VisitStories` / `VisitStoryRecord` as a later cleanup), `docs/agents/plynling-storylines.md` (a *Visit files* section: the `.visit` kind, `VisitCatalog`, its load-stops-the-bot rule, keys forever), `ProjectSYNCS/Commands/Plynling/PlynlingModule/*Help*` (`/plynling help` visit lines — measure both pages against 6000), `HelpModule` (if its visit line changes), `README.md`, `ProjectSYNCS/config.yaml` (`8.1.0` → `8.2.0`), `ProjectSYNCS/CHANGELOG.md` (`## 8.2.0 — <date>`, English, no member names)

- [x] **Step 1: Write the docs**; keep each notes file under ~40 KB.
- [x] **Step 2: Run** the checks (they measure the help pages) and `dotnet build ProjectSYNCS -warnaserror` — expected: clean.
- [x] **Step 3: Hand-off** to the owner: the full diff summary, the simulation table, the dev-guild report, version `8.2.0`.
