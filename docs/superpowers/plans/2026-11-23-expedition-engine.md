# Expedition engine and La Combe aux Mousses — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A Plynling goes on an expedition into a place written as a `.place` file, played on the spot in one public
Components V2 message, and comes back with notebook steps, at most one mushroom, cailloux and XP. The first place is the
Combe aux Mousses.

**Architecture:** A place file is read with the story format's reader and tags by its own `PlaceParser` /
`PlaceValidator` / `PlaceLibrary` / `PlaceCatalog` (the `Visits/` pattern). The rules of a trip (draw, odds, roll,
outcome, find, rewards) are pure functions in `Helpers/Plynling/Expeditions/`. One `ExpeditionRun` row per trip holds
its progress; `PlynlingService.Expeditions.cs` loads, applies and saves through `UnitAsync`. A card builder, a component
handler, the `/plynling expedition` command, a one-minute sweep and `/inventory notebook` sit on top.

**Tech Stack:** .NET 10, Discord.Net (Components V2), EF Core + SQLite, Python 3 + Pillow (the art export), the checks
console (`tests/ProjectSYNCS.Checks`).

**Specs:** `docs/superpowers/specs/2026-11-23-expedition-engine-design.md` (the engine; binding),
`docs/superpowers/specs/2026-11-23-combe-aux-mousses-design.md` (the place), `docs/plynling-expedition-combe-sheet.md`
(the approved French text, copied word for word), `docs/superpowers/specs/2026-11-23-plynling-rpg-design.md` (the RPG).
Art: `tools/creature-art/creatures.py`, approved 2026-11-23.

## Global Constraints

- **The owner commits by hand.** No task commits; each ends with the checks green and the change left in the tree.
- **Every task's verification is** `dotnet build ProjectSYNCS -warnaserror` (0 warnings) and
  `dotnet run --project tests/ProjectSYNCS.Checks` printing `ALL CHECKS PASSED`. New checks go in
  `tests/ProjectSYNCS.Checks/ExpeditionChecks.cs`, under **the next free section number** in `Program.cs` (66 after
  the merge of 2026-11-23: upstream's 60–64, then the RPG foundation's 65), called after `RpgChecks`
  (`await ExpeditionChecks.Run(Fail);`), in the `Expect(bool, string)` style of `RpgChecks.cs`.
- **French for every user-facing string; English for code, comments, logs, parser and validator errors.**
- **Append-only:** `NotebookStep`, `ExpeditionStatus`, `ExpeditionPhase`, `JournalKind`, modifier keys, metric keys
  (`earn.expedition`, `act.expedition`), place / creature / scene / option / thread / epithet keys.
- **Stored text is copied word for word** from the approved sheet (prose, titles, labels). Tags are syntax: the place
  format reads them as the sheet writes them (French stat names, `? win` / `? lose` / `? done`, `[if no step …]`).
- **Plynling lines** are `GenderedLines` or `{a:m|f}` templates, written per `docs/plynling-writing-style.md`.
- **Components V2:** ≤ 40 components across the tree, ≤ 4000 characters of text, button labels ≤ 80, one custom-id
  verb per row, `MessageFlags.ComponentsV2` re-asserted on every update, `AllowedMentions.None`, builders static and
  measured in the checks with 32-character names. Option buttons blurple; « Rentrer » green, « Continuer » red.
- **Every handler defers first.** Every service entry point goes through `UnitAsync`.
- **SQLite:** no `DateTimeOffset` comparison in SQL (filter by status or id, compare dates in memory); snowflakes
  `.HasConversion<long>()`.
- **Rolls** use `StableRoll`, never `Random`, except where an existing API takes a `Random` (none here).
- **Version:** a minor (new subcommands and a migration): **the next minor above what `config.yaml` shows** when the
  work starts (after the merge of 2026-11-23 the RPG foundation, still uncommitted, is 8.7.0 on top of origin's 8.6.0). If `git diff ProjectSYNCS/config.yaml` already shows a bump in the same batch, extend its
  CHANGELOG entry instead.
- **Numbers pinned by the spec:** Combe Énergie 30, duration 1 h, 3 scenes, wounds out of 2 (the 2nd brings the
  rescue), abandon after **15 minutes** without a click, XP **30 base, up to 40**, no activity above **1.33 XP per
  Énergie**, cailloux **5–15** a trip, find rarity **25 / 40 / 28 / 7**, money **under three average `/work` shifts a
  day** (`3 × (PebbleEconomy.WorkMin + WorkMax) / 2` = 97.5).

**Before starting:** this plan was written against a27bfa5 plus the uncommitted RPG foundation. On 2026-11-23 origin's
cosmetics, crafting and forage places (8.3.0–8.6.0) were merged under the foundation (now 8.7.0); re-check every file
and line this plan names against the tree, especially `/plynling forage` and `PlynlingService.Life.cs`, which forage
places changed.

## Review Focus

1. **A click on a message that already moved on** (a double click, two tabs, the sweep closing it meanwhile) answers
   « Déjà joué. » and applies nothing twice — pinned in Task 7 (`stale_click_applies_nothing`).
2. **The Plynling is frozen or dies between two clicks**: a freeze closes the run as abandoned with what it found; a
   death closes it `Void` with nothing given — pinned in Task 7 (`freeze_mid_run_heads_home`, `death_mid_run_voids`).
3. **A trip that runs into the night** (started at 00:50, clicked at 01:20): starting is refused asleep, but a run
   already open keeps playing — pinned in Task 7 (`open_run_plays_through_the_night`).
4. **A find drawn in a season where its rarity has no findable mushroom** falls back to a common one, never throws —
   pinned in Task 5 (`find_falls_back_in_every_season`).
5. **A notebook step earned by a Plynling that later died** still shows that Plynling's name, and a second Plynling of
   the same person never re-earns it — pinned in Task 7 (`notebook_is_the_persons`).

---

## File structure

| File | Responsibility |
|---|---|
| `Helpers/Plynling/Expeditions/PlaceModel.cs` | Records of a parsed place; `NotebookStep` |
| `Helpers/Plynling/Expeditions/PlaceParser.cs` | Text → `Place`, errors with file:line |
| `Helpers/Plynling/Expeditions/PlaceValidator.cs` | Every rule of the spec's validator |
| `Helpers/Plynling/Expeditions/PlaceLibrary.cs`, `PlaceCatalog.cs` | Load a folder; the lazy process-wide catalog |
| `Helpers/Plynling/Expeditions/ExpeditionRules.cs` | Pure trip rules: draw, visibility, odds, roll, find, rewards |
| `Helpers/Plynling/Expeditions/ExpeditionStakes.cs` | The stakes line under each option |
| `Helpers/Plynling/Expeditions/ExpeditionArt.cs` | Creature picture URLs |
| `Helpers/Plynling/PlynlingEpithets.cs` | One lookup for story and place epithets |
| `Models/Plynling/ExpeditionRun.cs`, `Models/Plynling/NotebookEntry.cs` | The two tables |
| `Services/Plynling/PlynlingService.Expeditions.cs` | Start, pick, home/continue, close, away, notebook reads |
| `Services/Plynling/ExpeditionSweepService.cs` | The 1-minute loop closing runs left alone |
| `Commands/Plynling/ExpeditionCards.cs`, `Commands/Economy/NotebookCards.cs` | Static, measured cards |
| `Interactions/Components/ExpeditionHandler.cs`, `Interactions/Components/NotebookHandler.cs` | Buttons and select |
| `ProjectSYNCS/Stories/Expeditions/combe.place` | The Combe, from the sheet |
| `tests/ProjectSYNCS.Checks/Expeditions/test_place.place` | Fixture holding every word of the format |
| `tests/ProjectSYNCS.Checks/ExpeditionChecks.cs`, `ExpeditionSimulation.cs` | Section 61 and the simulation |
| `tools/creature-art/export.py` | Writes `assets/creatures/*.png` |
| `docs/plynling-expedition-combe-notebook-sheet.md` | The new French text, for the owner's review |
| `docs/agents/plynling-expeditions.md` | The subsystem note |

---

### Task 1: The notebook sheet (owner review gate)

The engine design adds French text the approved writing sheet doesn't hold. It goes to the owner first; Task 10 waits
for the approval, the other tasks don't.

**Files:**
- Create: `docs/plynling-expedition-combe-notebook-sheet.md`

- [ ] **Step 1: Write the sheet**, in French, in the writing sheet's layout (a header saying it awaits review, then
  blocks copied later into `combe.place` and `PlynlingModifiers.cs`):
  - **The place's line** for its notebook page: one sentence.
  - **Eight creature blurbs** (`## creature: <key>` + `name:` + 2–3 sentences, what a curious villager would say, no
    magic, nothing linking the Phalènes to Asha or the Trame), keys `toad`, `moths`, `whistler`, `hedgehog`, `beetle`,
    `ferret`, `snail`, `badger`, names as in the Combe design (`Crapaud-Mousse`, `Phalènes-Lanternes`, `Siffleur`,
    `Hérisson-Châtaigne`, `Lucane-Écorce`, `Furet des Fougères`, `Escargot-Cloche`, `Vieux Blaireau gris`).
  - **The silhouette's line** under « ❔ ??? » (one short sentence).
  - **Two modifiers**, each with masculine and feminine names, an emoji and a one-line description in the
    `PlynlingModifiers` voice: `scratched` « Égratigné / Égratignée » and `battered` « Tout cabossé / Toute cabossée ».
  - **Review questions** at the end, defaults stated, as in the writing sheet.
- [ ] **Step 2: Hand it to the owner** and record the answer at the top of the file before Task 10 copies it.

---

### Task 2: Énergie spend, XP grant and one odds function

**Files:**
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingRpg.cs`, `ProjectSYNCS/Helpers/Plynling/PlynlingEventEngine.cs`
- Test: `tests/ProjectSYNCS.Checks/ExpeditionChecks.cs` (create, with `Run` and section 61 in `Program.cs`)

**Interfaces:**
- Produces: `PlynlingRpg.SpendEnergy(Plynling p, int max, double amount, DateTimeOffset now) : void` (rebases:
  `Energy = max(0, EnergyAt − amount)`, `EnergyAsOf = now`); `PlynlingRpg.AddXp(Plynling p, int amount) : (int Gained,
  int LevelBefore, int LevelAfter)` (stops at `XpForLevel(MaxLevel)`); `PlynlingEventEngine.StatChance(PlynlingStat
  stat, int value, int against) : int` (body → `PlynlingRpg.BodyChance`, mind → `Math.Clamp(50 + 5 × (value −
  against), 5, 95)`). `Drill` and `Chance(EventChallenge, ctx)` call them; their behaviour doesn't change.

- [ ] **Step 1: Write the failing checks**

```csharp
// spend_rebases: full 120 at t, spend 30 → EnergyAt(t) == 90 and EnergyAsOf == t
// spend_floors_at_zero: spend 500 → Energy == 0
// add_xp_stops_at_cap: RpgXp = XpForLevel(100) - 5; AddXp(40) → Gained 5, LevelAfter 100
// add_xp_levels: RpgXp = XpForLevel(2) - 1; AddXp(1) → (1, 1, 2)
// stat_chance_matches_events: StatChance(Courage, 6, 4) == 60; StatChance(Force, 5, 5) == BodyChance(5, 5)
```

- [ ] **Step 2: Run the checks.** Expected: build error (the methods don't exist).
- [ ] **Step 3: Implement the three methods**; `Drill` uses `SpendEnergy` and `AddXp`, `Chance(EventChallenge, ctx)`
  uses `StatChance`.
- [ ] **Step 4: Run the checks.** Expected: `ALL CHECKS PASSED` (RpgChecks' drill and pace checks unchanged).

---

### Task 3: The place format — model and parser

**Files:**
- Create: `ProjectSYNCS/Helpers/Plynling/Expeditions/PlaceModel.cs`, `PlaceParser.cs`
- Create: `tests/ProjectSYNCS.Checks/Expeditions/test_place.place`
- Modify: `ProjectSYNCS/Helpers/Plynling/Stories/StoryLibrary.cs` (skip `.place` like `.visit`)
- Test: `ExpeditionChecks.cs`

**The grammar** *(the builder's call, pinned here)*. Lines are read with `StoryParser.Read` (BOM, CRLF, `//` notes),
tags with `StoryTags.Split` / `StoryTags.Words`.

```
# place: combe
name: La Combe aux Mousses
emoji: 🌿
energy: 30
duration: 1h
scenes: 3
level: 1
encounters: 2            (most encounter scenes a trip)
threads: 1               (most thread scenes a trip)
finds: 1                 (most scenes able to give a find, a trip)
rarity: 25 40 28 7       (common uncommon rare legendary, for [find])
rarity_rare: 0 30 50 20  ([find rare])
xp: 30
xp_win: 2                (per roll won)
xp_max: 40
cailloux: 5-10           (the trip's base, hashed; [cailloux n] adds)

<the place's line: one paragraph>

## creature: toad
name: Crapaud-Mousse
<blurb paragraphs>

## epithet: phalenes
{a:celui|celle} qui a vu le bassin des Phalènes

## arrival: arrive_fog        ## return: return_1        ## rescue: rescue      (prose only)

## scene: toad
kind: encounter toad   |  sequel hedgehog  |  find  |  obstacle  |  thread source 1
title: La pierre qui respire
needs: step whistler seen | found | unlock source_1 | met 5     (optional, one line, one condition)
weight: 20                                                        (optional, default 100)
moment: A trouvé le bassin des Phalènes, au fond de la Combe aux Mousses.   (optional)

<prose>

* look | Regarder la pierre de plus près  [roll Intelligence 4] [if no step toad seen]
  ? win  [step toad seen]
  <outcome prose>
  ? lose  [dirt]
  <outcome prose>
```

- **Option tags:** `[roll <stat> <n>]` with the stat's French name (`PlynlingStats.Name`: Force, Agilité, Endurance,
  Intelligence, Vitesse, Courage…, n 1–15); `[needs step <c> seen|close|tamed]`; `[if no step <c> <s>]`;
  `[needs unlock <k>]`; `[needs trait <k>]`; `[if found]`; `[if not found]`. All are visibility conditions: an option
  whose conditions fail is not shown.
- **Outcome markers:** `? win` and `? lose` (a rolled option has both, nothing else), `? done` (an unrolled one has only
  it).
- **Outcome effects:** `[wound]`, `[step <c> <s>]`, `[find]`, `[find rare]`, `[lose find]`, `[dirt]`, `[moment]`,
  `[-> <scene>]`, and through `StoryVocabulary.Effect`: `[cailloux n]`, `[stress n]`, `[unlock k]`, `[epithet k]`,
  `[modifier k]`. Any other story effect is an error « not in a place file ».
- **`needs: met n`**: the person holds at least one step with n other creatures of the place.

**Interfaces:**
- Produces (in `PlaceModel.cs`, namespace `ProjectSYNCS.Helpers`):

```csharp
public enum NotebookStep { Seen, Close, Tamed }                      // stored: append-only
public enum PlaceSceneKind { Encounter, Sequel, Find, Obstacle, Thread }
public enum PlaceConditionKind { Step, NoStep, Unlock, Trait, Found, NotFound, Met }
public sealed record PlaceCondition(PlaceConditionKind Kind, string Key, NotebookStep Step, int Number, StoryPos Pos);
public enum PlaceEffectKind { Wound, Step, Find, FindRare, LoseFind, Dirt, Moment, Story }
public sealed record PlaceEffect(PlaceEffectKind Kind, string Key, NotebookStep Step, StoryEffect? Story, StoryPos Pos);
public sealed record PlaceOutcome(string Text, IReadOnlyList<PlaceEffect> Effects, string? Next, StoryPos Pos);
public sealed record PlaceRoll(PlynlingStat Stat, int Difficulty);
public sealed record PlaceOption(string Key, string Label, PlaceRoll? Roll, IReadOnlyList<PlaceCondition> When,
    PlaceOutcome? Done, PlaceOutcome? Win, PlaceOutcome? Lose, StoryPos Pos);
public sealed record PlaceScene(string Key, PlaceSceneKind Kind, string? Creature, string? Thread, int ThreadStep,
    string Title, PlaceCondition? Needs, int Weight, string? Moment, string Prose, IReadOnlyList<PlaceOption> Options, StoryPos Pos);
public sealed record PlaceCreature(string Key, string Name, string Blurb, StoryPos Pos);
public sealed record PlaceText(string Key, string Prose, StoryPos Pos);
public sealed record Place(string Key, string Name, string Emoji, string Line, int Energy, TimeSpan Duration,
    int SceneCount, int MinLevel, int MaxEncounters, int MaxThreads, int MaxFinds,
    IReadOnlyList<int> Rarity, IReadOnlyList<int> RareRarity, int Xp, int XpWin, int XpMax, int CaillouxMin, int CaillouxMax,
    IReadOnlyList<PlaceCreature> Creatures, IReadOnlyList<EpithetDecl> Epithets, IReadOnlyList<PlaceText> Arrivals,
    IReadOnlyList<PlaceScene> Scenes, IReadOnlyList<PlaceText> Returns, IReadOnlyList<PlaceText> Rescues, StoryPos Pos)
{
    public PlaceScene? Scene(string key);          // by key, or null
    public PlaceCreature? Creature(string key);
}
```

  `PlaceParser.Parse(string file, string text, List<StoryError> errors) : Place?`. Prose keeps its paragraphs joined by
  `"\n\n"`; an outcome is one paragraph.

- [ ] **Step 1: Write the fixture** `test_place.place`: a place `test` with 3 creatures, 2 threads steps, every option
  tag, every outcome marker, every effect, `needs:` of each kind, `weight:`, `moment:`, 2 arrivals, 2 returns, 1 rescue,
  one sequel reached by `[-> …]`. It must also pass Task 4's validator.
- [ ] **Step 2: Write the failing checks**

```csharp
// parse_fixture: errors empty; place.Key == "test"; Scenes.Count, Creatures.Count as written
// parse_roll_french_stat: "[roll Agilité 4]" → Roll == (Agility, 4); "[roll Agility 4]" → an error naming the French names
// parse_markers: a rolled option with "? done" → error; an unrolled option with "? win" → error
// parse_if_no_step: When[0] == (NoStep, "toad", Seen)
// parse_story_effects: "[cailloux 5]" → Story effect Cailloux 5; "[remember x]" → error "not in a place file"
// parse_every_word: every PlaceEffectKind and PlaceConditionKind appears somewhere in the fixture
// story_library_skips_place: StoryLibrary.Load on a folder holding only a .place file reports no "not a .story" error
```

- [ ] **Step 3: Run.** Expected: build error.
- [ ] **Step 4: Implement** `PlaceModel.cs`, `PlaceParser.cs`, and the `.place` skip in `StoryLibrary`
  (`PlaceLibrary.Extension = ".place"`).
- [ ] **Step 5: Run.** Expected: `ALL CHECKS PASSED`.

---

### Task 4: Validator, library, catalog and the shared epithet lookup

**Files:**
- Create: `Helpers/Plynling/Expeditions/PlaceValidator.cs`, `PlaceLibrary.cs`, `PlaceCatalog.cs`,
  `Helpers/Plynling/PlynlingEpithets.cs`
- Modify: `ProjectSYNCS/Program.cs` (touch `PlaceCatalog.Places` in the `try` beside `VisitCatalog`),
  `Helpers/Plynling/PlynlingPersonality.cs`, `Helpers/Plynling/PlynlingEventStory.cs` (epithet reads → `PlynlingEpithets`),
  `tests/ProjectSYNCS.Checks/Program.cs` (`PlaceCatalog.Folder` = the repo's `ProjectSYNCS/Stories/Expeditions`)

**Interfaces:**
- Produces: `PlaceValidator.Validate(IReadOnlyList<Place> places, IReadOnlySet<string> storyEpithets) :
  IEnumerable<StoryError>`; `PlaceLibrary.Load(string folder, IReadOnlySet<string> storyEpithets) : PlaceLoad(Places,
  Errors)`; `PlaceCatalog.Folder { get; set; }`, `PlaceCatalog.Places : IReadOnlyList<Place>`,
  `PlaceCatalog.ByKey(string) : Place?` (lazy, throws `StoryLoadException` and keeps throwing, as `VisitCatalog`);
  `PlynlingEpithets.ByKey(string key) : EpithetDecl?` (stories first, then places).
- **Load order:** `PlaceCatalog` reads `StoryCatalog.Epithets.Keys` when it loads, so `Program.cs` touches it after
  `StoryCatalog.Compiled`.

**The rules** (each an error with file:line):
- header fields present and in range (energy 1–100, duration 15 min–6 h, scenes 1–6, level 1–100, rarity four
  non-negative ints summing to 100, xp ≤ xp_max, xp_max ≤ round(1.33 × energy), cailloux min ≤ max);
- keys: place, creature, scene, option, arrival/return/rescue, epithet unique in their scope; a place epithet key never
  equals a story epithet key; scene keys ≤ 20 and option keys ≤ 12 characters (custom-ids);
- every scene has 2–4 options and **at least one free option** (no roll, no `[needs trait]`, no `[wound]`, no
  `[stress n>0]`); a sequel is never drawn, is reached by some `[-> …]`, and `[->]` only names a sequel of the same
  creature; an outcome never has both `[wound]` and `[->]`; at most one `[step]` per outcome;
- **every creature can earn all three steps** somewhere; `[step c s]` and `[needs step c s]` name declared creatures;
  every `[needs step c s]` is reachable (some `[step c s]` exists); `[unlock k]` names `<thread>_<n>` of a declared
  thread scene, and every thread step n has a scene giving `<thread>_<n>`; `[epithet k]` names a place epithet; `[moment]`
  only in a scene with `moment:`; `[needs trait k]` names a trait;
- **the draw limits can always fill a trip:** with every encounter and thread excluded and the last trip's scenes
  excluded, the obstacles alone still number ≥ `scenes`;
- at least one arrival, one return, one rescue;
- **Discord's caps**: each label ≤ 80, each expanded text measured with a 32-character name and each `{a:…|…}` at its
  longer form; no `{…}` token but `{A}` and `{a:m|f}` left after expansion in both genders.

- [ ] **Step 1: Write the failing checks**

```csharp
// fixture_validates: PlaceLibrary.Load(fixtureFolder, StoryCatalog.Epithets.Keys) has no errors
// each rule, by a one-line mutation of the fixture text parsed in memory, gives exactly its error:
//   no_free_option, step_unreachable ([needs step x close] with no [step x close]), creature_missing_tamed,
//   unlock_unknown_thread, sequel_never_reached, wound_and_next, label_over_80, epithet_collides_with_story,
//   too_few_obstacles (scenes: 5 with 4 obstacles), xp_over_ceiling (xp_max: 41 for energy 30)
// epithet_lookup: PlynlingEpithets.ByKey(<a story epithet>) and ByKey("<fixture epithet>") both resolve
// catalog_throws_and_keeps_throwing: Folder = a temp folder with a broken file → StoryLoadException twice
```

- [ ] **Step 2: Run.** Expected: build error.
- [ ] **Step 3: Implement**; the `Program.cs` touch logs and rethrows like the story load. Until Task 10 there is no
  `Stories/Expeditions/` folder: **a missing folder loads no place and no error** (unlike `VisitLibrary`), so the bot
  runs between Task 4 and Task 10, and `/plynling expedition` answers `UnknownPlace`. Task 10 adds `combe_loads`, which
  makes the Combe required.
- [ ] **Step 4: Run.** Expected: `ALL CHECKS PASSED`.

---

### Task 5: The trip's rules (pure)

**Files:**
- Create: `Helpers/Plynling/Expeditions/ExpeditionRules.cs`
- Test: `ExpeditionChecks.cs`

**Interfaces:**
- Produces:

```csharp
public sealed record ExpeditionKnowledge(
    IReadOnlySet<(string Creature, NotebookStep Step)> Notebook,   // the person's, for this place
    IReadOnlySet<string> Unlocks, IReadOnlySet<string> Traits, bool Found,
    IReadOnlyDictionary<PlynlingStat, int> Stats);                 // totals from PlynlingStats.Compute
public sealed record TripSoFar(IReadOnlyList<string> Drawn, IReadOnlySet<string> LastTrip, bool Found);

public static class ExpeditionRules
{
    public static readonly TimeSpan IdleLimit = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan HungerMargin = TimeSpan.FromHours(6);
    public const int MaxWounds = 2;
    public const double DirtAmount = 0.2;
    public const double DoneCreatureFactor = 0.25;                // weight of a creature whose 3 steps are held

    public static PlaceScene Draw(Place place, int runId, int step, TripSoFar trip, ExpeditionKnowledge atStart);
    public static bool Holds(PlaceCondition c, ExpeditionKnowledge k, Place place);
    public static IReadOnlyList<PlaceOption> Visible(PlaceScene s, ExpeditionKnowledge k, Place place);
    public static int Chance(PlaceRoll roll, ExpeditionKnowledge k);     // PlynlingEventEngine.StatChance
    public static bool Succeeds(int runId, int screen, int chance);      // StableRoll.Unit(runId, RollSalt, screen) * 100 < chance
    public static ItemInfo DrawFind(Place place, bool rare, int runId, int screen, DateTimeOffset now);
    public static int BaseCailloux(Place place, int runId);
    public static int Xp(Place place, int wins);                        // min(XpMax, Xp + XpWin × wins)
    public static bool CanGiveFind(PlaceScene s);                       // any outcome with Find or FindRare

    // The trip's counters after one outcome, and where the trip goes next. Pure: the service applies the
    // Plynling's side (dirt, stress, marks, modifiers, moment, notebook rows) itself; the simulation replays
    // trips with this alone. findKey is the item DrawFind gave for this screen when the outcome finds.
    public static (TripTally Tally, NextMove Move) Apply(Place place, PlaceOutcome outcome, bool won, TripTally tally,
        int drawnCount, string? findKey);
}

public sealed record TripTally(int Wounds, int Wins, int Cailloux, string? Find, bool LostFind,
    IReadOnlyList<(string Creature, NotebookStep Step)> Steps);
public enum NextMove { Sequel, Rescue, Finish, AfterWound, NextScene }
```

`Apply`: `Find`/`FindRare` keep the first find of the trip; `LoseFind` clears it and sets `LostFind`; `Step` appends
when not already in `Steps`; story `Cailloux` adds; `Wound` counts; a won roll counts. Then `Next` → `Sequel`; else
`Wounds >= MaxWounds` → `Rescue`; else `drawnCount == place.SceneCount` → `Finish`; else a wound this outcome →
`AfterWound`; else `NextScene`.

**The draw** *(my call, within the spec)*: each scene is drawn when its turn comes and stored at once (the spec's
"stored on the run"; drawing late also covers the Furet). Candidates: scenes of kind encounter, find, obstacle or
thread, not drawn yet this trip; minus encounters once `MaxEncounters` were drawn, threads once `MaxThreads` were,
scenes that `CanGiveFind` once `MaxFinds` were; minus a thread scene unless the Plynling holds `<thread>_<n−1>` (n > 1)
and not `<thread>_<n>`; minus scenes whose `needs:` fails (notebook as at the trip's start, `found` as now); minus the
last trip's scenes **unless** that leaves no candidate. Weight: the scene's `Weight`, × `DoneCreatureFactor` for an
encounter whose creature's three steps are all held. `StableRoll.Weighted(pool, runId, DrawSalt, step)`.
Salts: `DrawSalt = 610`, `RollSalt = 611`, `FindSalt = 612`, `CaillouxSalt = 613` (unused by any other caller: grep).

**The find:** a rarity from `Rarity` (or `RareRarity`), then a Champignons item (`ItemCatalog.MushroomSet`) of that
rarity among `ItemCatalog.Findable(item, now)`; none findable of that rarity → common. Add
`ItemCatalog.DrawMushroom(ItemRarity rarity, double unit, DateTimeOffset now) : ItemInfo` beside `DrawFrom` and use it.

- [ ] **Step 1: Write the failing checks** (fixture place)

```csharp
// draw_is_stable: Draw(p, 7, 0, …) twice → same scene
// draw_respects_limits: over runIds 1..2000, no trip of 3 has more than MaxEncounters encounters, MaxThreads threads,
//   MaxFinds find-giving scenes, nor a sequel, nor a scene twice
// draw_avoids_last_trip: with LastTrip = 3 obstacles and enough others, none of them is drawn in 2000 runs
// draw_needs_found: the fixture's "needs: found" scene never comes when trip.Found is false, comes sometimes when true
// thread_in_order: thread step 2 never drawn without "<t>_1"; never step 1 once "<t>_1" is held
// done_creature_rarer: with all 3 steps of creature X held, X's encounter share drops to about a quarter (±30 %)
// visible_conditions: [if no step x seen] hidden once (x, Seen) is held; [needs trait patient] hidden without it
// chance_body_and_mind: Chance((Force,5), stats Force 5) == BodyChance(5,5); Chance((Courage,4), Courage 6) == 60
// find_falls_back_in_every_season: for each month and each rarity unit in {0.01, 0.5, 0.9, 0.99}, DrawFind returns a
//   Champignons item findable that month
// xp_capped: Xp(combe-like 30/2/40, 3) == 36; Xp(…, 9) == 40
// cailloux_in_range: BaseCailloux over 1000 runs stays within [CaillouxMin, CaillouxMax]
// apply_moves: a [-> x] outcome → Sequel; 2nd [wound] → Rescue even on the last scene; last scene unwounded → Finish;
//   1st [wound] mid-trip → AfterWound; [find] twice keeps the first; [lose find] → Find null, LostFind true;
//   the same [step] twice is listed once
```

- [ ] **Step 2: Run.** Expected: build error.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run.** Expected: `ALL CHECKS PASSED`.

---

### Task 6: Storage, keys and the new catalog entries

**Files:**
- Create: `Models/Plynling/ExpeditionRun.cs`, `Models/Plynling/NotebookEntry.cs`, a migration
- Modify: `Data/AppDbContext.cs`, `Helpers/Plynling/PlynlingJournalUi.cs` (append `ExpeditionMoment`),
  `Helpers/Plynling/PlynlingModifiers.cs` (append `scratched`, `battered`), `Helpers/Economy/EconomyLog.cs`
  (`EarnExpedition = "earn.expedition"`, `ActExpedition = "act.expedition"`, added to `Activities`),
  `Commands/Admin/AdminCards.cs` (label « Expéditions »)

**Interfaces:**
- Produces:

```csharp
public enum ExpeditionStatus { Open, Finished, HomeEarly, Rescued, Abandoned, Void }   // stored
public enum ExpeditionPhase { Scene, AfterWound }                                      // stored
public class ExpeditionRun
{
    public int Id; public ulong GuildId; public ulong OwnerId; public int PlynlingId; public string PlaceKey = "";
    public ulong ChannelId; public ulong? MessageId;
    public DateTimeOffset StartedAt, ReturnsAt, LastActionAt; public DateTimeOffset? EndedAt;
    public ExpeditionStatus Status; public ExpeditionPhase Phase;
    public int Screen;                 // clicks resolved; the stale-click guard
    public string Drawn = "";          // "toad;find_ring" — drawn scene keys, in order
    public string Current = "";        // the scene on screen (a sequel included); "" in AfterWound
    public string Arrival = "";        // the arrival text key
    public string? LastOutcome;        // "<scene>.<option>.<win|lose|done>"
    public int Wounds, Wins, Cailloux; public string? FoundItemKey; public bool LostFind;
    public string Steps = "";          // "toad.Seen;snail.Close" earned this trip
    public int XpGained, LevelBefore, LevelAfter, CaillouxPaid;
    public int Version;                // concurrency token
}
public class NotebookEntry
{
    public int Id; public ulong GuildId; public ulong UserId; public string CreatureKey = "";   // "combe.toad"
    public NotebookStep Step; public int PlynlingId; public string PlynlingName = ""; public DateTimeOffset EarnedAt;
}
```

  Indexes: `ExpeditionRun` `(PlynlingId)` **unique filtered** `"Status" = 0` (one open trip per Plynling, as the
  living-owner index); `(Status)`; `(PlynlingId, Id)`. `NotebookEntry` unique `(GuildId, UserId, CreatureKey, Step)`.
  Snowflakes `.HasConversion<long>()`; `Version` `.IsConcurrencyToken()` (bumped on every save, like `Plynling.Version`).
- Modifiers: `scratched` 6 h, negative, −1 to each body stat; `battered` 24 h, negative, −1 to each body stat,
  `Happiness: 1.3`. Names and descriptions from Task 1's sheet (placeholders « Égratigné » / « Tout cabossé » until the
  approval, replaced in Task 10).
- `JournalKind.ExpeditionMoment`: detail `"<place>.<scene>"`; its line is `🌿 ` + the scene's `moment:` expanded with
  the Plynling's gender, or « 🌿 Une expédition. » when the place or scene is gone.

- [ ] **Step 1: Write the failing checks**

```csharp
// schema_has_tables: an in-memory SQLite EnsureCreated has ExpeditionRuns and NotebookEntries
// one_open_run: two Open runs for one Plynling → DbUpdateException; an Open and a Finished → fine
// notebook_unique: the same (guild, user, creature, step) twice → DbUpdateException
// modifiers_negative: scratched and battered are Negative, lower every body stat by 1, appended after practice_speed
// journal_moment_line: Line(ExpeditionMoment, "test.<scene>", …) starts with "🌿 "
// metric_keys: EconomyLog.Activities contains act.expedition; AdminCards labels it
```

- [ ] **Step 2: Run.** Expected: build error.
- [ ] **Step 3: Implement**, then `dotnet ef migrations add Expeditions` in `ProjectSYNCS/`. The migration is
  schema-only; check it holds both tables and the filtered index, and apply it to a scratch copy of a database.
- [ ] **Step 4: Run.** Expected: `ALL CHECKS PASSED`.

---

### Task 7: The service — start, play, come home, close

**Files:**
- Create: `Services/Plynling/PlynlingService.Expeditions.cs`
- Modify: `Helpers/Plynling/PlynlingText.cs` (the refusals and short lines)
- Test: `ExpeditionChecks.cs` (in-memory SQLite, the `RpgChecks.ServiceAsync` setup; the fixture place via
  `PlaceCatalog.Folder`)

**Interfaces:**
- Consumes: Tasks 2–6.
- Produces:

```csharp
public enum StartOutcome { Done, NoPlynling, Dead, Frozen, Mascot, TooYoung, Asleep, Sick, Away, NoEnergy, TooHungry,
    LevelTooLow, UnknownPlace }
public sealed record StartResult(StartOutcome Outcome, Plynling? Plynling, ExpeditionRun? Run = null,
    DateTimeOffset? Until = null);                      // Away → its return; NoEnergy → EnergyReadyAt
public enum PlayOutcome { Done, NotOwner, Stale, Gone }
public sealed record ExpeditionView(ExpeditionRun Run, Place Place, Plynling Plynling, ExpeditionKnowledge Knowledge,
    bool Ended);                                        // what the card needs, after the save

Task<StartResult> StartExpeditionAsync(ulong guildId, ulong ownerId, string placeKey, ulong channelId, DateTimeOffset now);
Task AttachMessageAsync(int runId, ulong messageId);
Task<(PlayOutcome Outcome, ExpeditionView? View)> PickAsync(int runId, ulong userId, int screen, string optionKey, DateTimeOffset now);
Task<(PlayOutcome Outcome, ExpeditionView? View)> ComeHomeAsync(int runId, ulong userId, int screen, DateTimeOffset now);
Task<(PlayOutcome Outcome, ExpeditionView? View)> ContinueAsync(int runId, ulong userId, int screen, DateTimeOffset now);
Task<ExpeditionView?> CloseIfIdleAsync(int runId, DateTimeOffset now);     // the sweep's unit
Task<IReadOnlyList<int>> OpenRunIdsAsync();                                 // by Status, no date in SQL
Task<DateTimeOffset?> AwayUntilAsync(int plynlingId, DateTimeOffset now);   // the latest run's ReturnsAt, if later than now
Task<ExpeditionView?> GetViewAsync(int runId, DateTimeOffset now);
```

**Behaviour** (spec, *Who can go*, *The trip's shape*, *Away*, *Edge cases*):
- **Start** refuses, in this order and before spending anything: no Plynling, dead, frozen, mascot, too young
  (`PlynlingRpg.CanTrain`), asleep, sick, away (`AwayUntilAsync`, which also covers an open run), level below the
  place's, unknown place, not enough Énergie, `PlynlingLife.HungerAt(p, now + Duration + HungerMargin) <= 0`. Then it
  spends the Énergie (`SpendEnergy`), adds the run (`ReturnsAt = now + Duration`, `LastActionAt = now`), picks the
  arrival (`StableRoll.Pick`), draws scene 0 (knowledge at the start; `LastTrip` = the person's previous run of this
  place's `Drawn`), logs `act.expedition`, saves once. The run's id exists only after the first save: save the row,
  then draw with its id and save again, in the same unit.
- **Pick:** the run must be Open, the clicker its owner (`NotOwner`), `screen == Run.Screen` and `Phase == Scene`, the
  option visible now (`Stale` otherwise). Rolls with `Succeeds(run.Id, run.Screen, Chance)`. The run's counters come
  from `ExpeditionRules.Apply` (the tally is read from and written back to the row); the Plynling's side is applied in
  order: `Step` → a `NotebookEntry` if the person lacks it (read through `Local`, idempotent) and `Steps` +=;
  `Find`/`FindRare` → `FoundItemKey` if none yet; `LoseFind` → `FoundItemKey = null`, `LostFind = true`; `Dirt` →
  `PlynlingLife.Dirty(p, now, DirtAmount)`; `Moment` → `AddMomentAsync(p, ExpeditionMoment, "<place>.<scene>")`;
  story `Cailloux` → `run.Cailloux +=`; `Stress` → `PlynlingLife.AddStress` (scaled by traits) or `Relieve`;
  `Unlock`/`Epithet` → `GiveMark` with no instance (`InstanceId = null`; add an overload of `GiveMarkAsync` without the
  instance); `Modifier` → `AddModifier`; `Wound`, wins, finds, cailloux and steps are the tally's. `Screen++`, `LastOutcome` set,
  `LastActionAt = now`. Then by `NextMove`: `Sequel` → `Current = Next`; `Rescue` → **rescue**; `Finish` → **finish**;
  `AfterWound` → `Phase = AfterWound`, `Current = ""`; `NextScene` → draw the next scene.
- **Come home** (AfterWound only): ends `HomeEarly`. **Continue** (AfterWound only): `Phase = Scene`, draw the next.
- **Ending** (`Finished`, `HomeEarly`, `Rescued`, `Abandoned`): XP `Xp(place, Wins)` through `AddXp` (the mascot never
  reaches here); cailloux `BaseCailloux + Cailloux` to the wallet with `earn.expedition`; the find granted through
  `InventoryService.GrantAsync` unless `Rescued` or lost; modifier `battered` + stress 15 when rescued, `scratched`
  when it ends with exactly one wound; `EndedAt`, `Status`, `XpGained`, `LevelBefore/After`, `CaillouxPaid`. One save.
  The `Rescued` summary shows the rescue text (`StableRoll.Pick` of `Rescues`).
- **Close if idle:** an Open run whose Plynling died → `Void` (nothing given, its message says so); frozen → ends
  `Abandoned`; `now − LastActionAt >= IdleLimit` → `Abandoned`; otherwise null. A click on a run whose Plynling is
  frozen closes it the same way and answers `Gone`.
- **Concurrency:** the run's `Version` makes the slower of a click and the sweep throw; `UnitAsync` reruns it, and the
  rerun finds `Screen` moved on (`Stale`) or the run closed.

- [ ] **Step 1: Write the failing checks** (fixture place; a test Plynling at level 1, full needs, as in RpgChecks)

```csharp
// start_refusals_spend_nothing: bébé, asleep (02:00 Paris), sick, mascot, no energy, hungry → each refusal,
//   Energy and RpgXp unchanged, no run row
// start_spends_and_draws: Done; EnergyAt(now) == max − place.Energy; Drawn has 1 key; ReturnsAt == now + Duration
// start_twice_is_away: a second start → Away with Until == ReturnsAt
// pick_plays_to_the_end: picking each scene's first visible option until Ended → Status Finished; XpGained in
//   [place.Xp, place.XpMax]; wallet up by CaillouxPaid; earn.expedition logged
// stale_click_applies_nothing: PickAsync with an old screen → Stale; Screen, Wins, wallet unchanged
// not_owner: another user → NotOwner, nothing applied
// wound_then_home: an option forced to [wound] (fixture scene with a free wounding option) → Phase AfterWound;
//   ComeHomeAsync → HomeEarly, scratched applied
// second_wound_rescues: two wounds → Rescued; FoundItemKey not granted; battered applied; XP still given
// freeze_mid_run_heads_home: freeze the Plynling, CloseIfIdleAsync → Abandoned, the find granted
// death_mid_run_voids: DiedAt set → Void, no XP, no cailloux, no item
// idle_closes_after_15_min: CloseIfIdleAsync at LastActionAt + 14 min → null; + 15 min → Abandoned
// open_run_plays_through_the_night: start at 00:50 Paris, PickAsync at 01:20 → Done
// notebook_is_the_persons: a step earned by Plynling A (then A dies, a new Plynling B is adopted) still lists A's name;
//   B earning the same step adds no row; another guild's notebook is separate
// away_until: AwayUntilAsync during the trip == ReturnsAt, after ReturnsAt == null, also after an early end
```

- [ ] **Step 2: Run.** Expected: build error.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run.** Expected: `ALL CHECKS PASSED`.

---

### Task 8: Away from home, the card, the command and the run's message

**Files:**
- Create: `Commands/Plynling/ExpeditionCards.cs`, `Helpers/Plynling/Expeditions/ExpeditionStakes.cs`,
  `Interactions/Components/ExpeditionHandler.cs`
- Modify: `Commands/Plynling/PlynlingModule/PlynlingModule.cs` (the `expedition` subcommand; both `/plynling` and `/pl`
  get it through the base class), `PlynlingModule.Rendering.cs` (`BuildCardAsync` loads `AwayUntilAsync`; `BuildCard`
  takes `DateTimeOffset? awayUntil = null`: the status gets « 🌿 En expédition · retour <t:…:R> » and the care buttons
  are left out), the away refusals: `Services/Plynling/PlynlingService.cs` (feed, pet, bath, medicine: append
  `CareOutcome.Away`), `PlynlingService.Life.cs` (forage), `PlynlingService.Rpg.cs` (`TrainOutcome.Away`), play
  (`PlynlingModule.cs` near line 153), visits both ways (`PlynlingModule.cs` near line 211,
  `PlynlingComponentHandler.cs` near line 316), self-freeze (append `FreezeOutcome.Away`); `PlynlingCareService.Refusal`
  and `PlynlingText` for the away line, which names the return time

**Interfaces:**
- Produces:

```csharp
public static class ExpeditionStakes { public static string Line(PlaceOption o, ExpeditionKnowledge k, Place place, PlynlingGender g); }
public static class ExpeditionCards
{
    public static MessageComponent Run(ExpeditionView v, DateTimeOffset now);        // a scene, or Rentrer/Continuer
    public static MessageComponent Summary(ExpeditionView v, DateTimeOffset now);    // the trip's end
    public static string Refusal(StartResult r);                                     // private
}
```

  Custom-ids: `exp:opt:{runId}:{screen}:{optionKey}` (the option row), `exp:home:{runId}:{screen}` and
  `exp:on:{runId}:{screen}` (the Rentrer/Continuer row). The handler defers (`DeferAsync`), checks the clicker against
  the run's owner (« Ce n'est pas ton expédition. », private, nothing applied), calls the service, then
  `ModifyOriginalResponseAsync` with `Run` or `Summary`; `Stale` answers « Déjà joué. » privately.

**The stakes line** (`ExpeditionStakes.Line`, parts joined by « · »): a trait option opens with the trait's emoji and
name; a roll reads `🎲 <Stat> <n> % : <gains> · sinon <losses>`; gains and losses in written order: `📔 une étape du
carnet`, `🍄 une trouvaille` (find or find rare), `🪙 n`, `😌 −n stress`, `🏷️ un titre`, `🧭 le chemin continue`
(unlock), `⚔️ la suite` (`[->]`), `🩹 une blessure`, `🍂 la trouvaille perdue`, `💧 de la boue`, `😣 +n stress`. An
unrolled option shows its gains alone (empty when nothing).

**The run card** (the spec's sketch): heading `🌿 <place> — <name> · scène i/N`, the Plynling's sprite as the
section's thumbnail, the counts line `🩹 Blessures : w/2 · 🍄 0|1 · 🪙 n`, the last outcome (quoted, expanded), a
separator, then on screen step 0 the arrival above the scene's prose, the scene's title in bold, its prose, each
visible option as « **Label** » + its stakes line, then one blurple button per option. In `AfterWound`: « Rentrer »
(green) and « Continuer » (red) instead. **The summary**: the return text (or the rescue text), then « 🎒 Ce qu'il en
rapporte » with the find (`ItemCatalog.ClearName`) or « rien cette fois », `+XP` and the level (on a level-up, `PlynlingTrainCards.LevelUpLine`: the message is public, so it is the
level-up's public line the spec asks for, with no second post), `🪙 n`, each step `📔 <Créature> : Aperçu|Approché|Apprivoisé !`, the thread line when an unlock was given,
the modifier when one applied, and « de retour <t:ReturnsAt:R> »; `Void` reads « L'expédition s'arrête là. ».

**The command:** `[SlashCommand("expedition", "Envoyer ton Plynling en expédition : quelques scènes, des créatures, peut-être une trouvaille")]`
with `[Choice("La Combe aux Mousses", "combe")] string place`. It defers **publicly**, starts, and either refuses
(`RefuseAsync`: the private refusal) or answers the run card, then `AttachMessageAsync(run.Id,
(await GetOriginalResponseAsync()).Id)`. When `PlaceCatalog.ByKey(place)` is null it answers `UnknownPlace`. Count the
subcommands before adding: `/plynling` must stay ≤ 25.

- [ ] **Step 1: Write the failing checks**

```csharp
// card_caps: for every scene of the fixture and of every shipped place, with a 32-character name, both genders, all
//   options visible: Run(...) builds (ComponentBuilderV2 throws past 40), text ≤ 4000, labels ≤ 80, ids unique, ≤ 100
// summary_caps: a rescued run with 3 steps, a find, a level-up and an unlock: builds, text ≤ 4000
// stakes_line: a rolled option giving [step] and losing on [wound] reads "🎲 Intelligence 55 % : 📔 une étape du carnet · sinon 🩹 une blessure"
//   (with a Plynling whose chance is 55)
// buttons_colours: option buttons Primary; Rentrer Success; Continuer Danger
// away_card: BuildCard(..., awayUntil: t) has the « En expédition » status and no plyn:pet / plyn:feed / plyn:bath ids
// away_refusals: feed, pet, bath, forage, train, play, visit and self-freeze each answer the away line during a trip
```

- [ ] **Step 2: Run.** Expected: build error.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run.** Expected: `ALL CHECKS PASSED`.

---

### Task 9: The sweep

**Files:**
- Create: `Services/Plynling/ExpeditionSweepService.cs`
- Modify: `Program.cs` (`services.AddSingleton<ExpeditionSweepService>()` and
  `AddHostedService(sp => sp.GetRequiredService<ExpeditionSweepService>())`)

**Interfaces:**
- Produces: `ExpeditionSweepService : BackgroundService` with `CheckInterval = 1 min`, `FirstDelay = 1 min`, and
  `public Task SweepOnceAsync(DateTimeOffset now)`: per open run id, its own scope and `try`, `CloseIfIdleAsync`, then
  rewrite the run's message (`ChannelId`, `MessageId`) with `ExpeditionCards.Summary` and
  `MessageFlags.ComponentsV2`; a missing channel or message is logged and skipped (Discord side effects never break the
  flow). It does not check `SilenceService` (closing a run answers a command). Singleton: takes `IServiceProvider` and
  opens `CreateAsyncScope()` per run.

- [ ] **Step 1: Write the failing check** — `sweep_closes_only_idle_runs`: two open runs (one idle 16 min, one 2 min),
  `SweepOnceAsync` with a provider over the in-memory database and no Discord client (the rewrite is skipped and
  logged) → the first `Abandoned`, the second still `Open`; a third run whose `PlynlingId` no longer exists doesn't stop
  the others.
- [ ] **Step 2: Run.** Expected: build error.
- [ ] **Step 3: Implement** (the client is optional in the constructor for the check, as `BadgeAwarder` is built there).
- [ ] **Step 4: Run.** Expected: `ALL CHECKS PASSED`.

---

### Task 10: The Combe — its file, its pictures, its numbers

Needs Task 1's approval.

**Files:**
- Create: `ProjectSYNCS/Stories/Expeditions/combe.place`, `tools/creature-art/export.py`,
  `assets/creatures/combe_<key>_v1.png` (8 + `combe_unknown_v1.png`), `Helpers/Plynling/Expeditions/ExpeditionArt.cs`,
  `tests/ProjectSYNCS.Checks/ExpeditionSimulation.cs`
- Modify: `PlynlingModifiers.cs` (the approved names and descriptions), `tools/creature-art/README.md`

**Interfaces:**
- Produces: `ExpeditionArt.Version = 1`, `ExpeditionArt.Creature(string place, string key) : string` and
  `ExpeditionArt.Unknown(string place) : string`, URLs under `PlynlingArt.BaseUrl`'s repository at
  `assets/creatures/<place>_<key>_v<Version>.png`. `export.py` writes each `creatures.CREATURES` picture at ×8
  (256×256, nearest-neighbour, PNG), the silhouette as `<place>_unknown`.

- [ ] **Step 1: Write `combe.place`**: the header (Energy 30, duration 1h, scenes 3, level 1, encounters 2, threads 1,
  finds 1, rarity 25 40 28 7, rarity_rare 0 30 50 20, xp 30, xp_win 2, xp_max 40, cailloux 5-10), the place line and
  the eight creatures from Task 1's approved sheet, the epithet `phalenes` « {a:celui|celle} qui a vu le bassin des
  Phalènes », and every block of `docs/plynling-expedition-combe-sheet.md` **word for word** (the 3 arrivals, the 21
  scenes with their `kind:`, `title:`, `needs:` lines, the 3 returns, the rescue). Mechanics the sheet leaves out:
  `moment:` on `source_3` (the sheet's journal line), `weight: 20` and `needs: met 5` on `badger` (the Combe design:
  rare, offered once the person has a step with five other creatures).
- [ ] **Step 2: Write the failing checks**

```csharp
// combe_loads: PlaceCatalog.ByKey("combe") has 8 creatures, 21 scenes, 3 arrivals, 3 returns, 1 rescue
// combe_word_for_word: every scene's prose, option labels and outcome texts equal the sheet's fenced blocks
//   (parse the sheet's ```…``` blocks with PlaceParser after the same header, compare field by field)
// combe_art_exists: for each creature and the silhouette, assets/creatures/combe_<key>_v1.png exists and is 256×256
// combe_cards: the card_caps check of Task 8 now covers the Combe (it loops over shipped places)
```

- [ ] **Step 3: Run.** Expected: FAIL (no file, no art).
- [ ] **Step 4: Export the art** (`python tools/creature-art/export.py`) and write `combe.place`.
- [ ] **Step 5: Write the simulation** (`ExpeditionSimulation.Run(fail, args)`, the section after
  `ExpeditionChecks`; `-- expedition-sim`
  prints its table): 2000 Plynlings × 30 days, a level-1 Plynling with innate Endurance 4 (max Énergie 120), every
  Énergie point spent on Combe trips as soon as it refills (4 trips a day), two policies — random visible option, and
  best (the highest-chance rolled option, else the free one) — over the real rules (`ExpeditionRules`, the run logic
  replayed purely: `Draw`, `Visible`, `Chance`, `Succeeds`, `DrawFind`, `Apply`, `Xp`, `BaseCailloux`, the notebook
  knowledge growing from each trip's steps and the thread's unlocks carried over). It asserts:
  - **XP per Énergie** for each policy in **[0.67, 1.33]**;
  - **the RPG pace re-pinned:** best play, every day's Énergie on trips, reaches level 100 **no earlier than day 360**
    (`PlynlingRpg.Level` over 365 days of `trips/day × mean XP`);
  - **cailloux a day** (base + options + each find valued `SellPrice(rarity) + SetReward / SetSize` of Champignons) **<
    97.5** for best play;
  - and prints: mean XP and cailloux a trip, the find rate and rarity split, wounds a trip, **how often Asha comes**
    (rescues per 100 trips), steps per trip, and trips to a full notebook.
- [ ] **Step 6: Run.** Expected: `ALL CHECKS PASSED`. If cailloux fail, **lower the find rate, never the rarity**
  (owner): drop `finds:` scenes' weight in `combe.place`, never touch `rarity:`. If XP fails, lower `xp_win` then `xp`.
  Record the final numbers in `docs/agents/plynling-expeditions.md` (Task 11).

---

### Task 11: The notebook

**Files:**
- Create: `Commands/Economy/NotebookCards.cs`, `Interactions/Components/NotebookHandler.cs`
- Modify: `Commands/Economy/InventoryModule.cs` (`notebook`), `Services/Plynling/PlynlingService.Expeditions.cs`
  (`GetNotebookAsync`)

**Interfaces:**
- Produces: `Task<IReadOnlyList<NotebookEntry>> GetNotebookAsync(ulong guildId, ulong userId)`;
  `NotebookCards.Place(Place place, IReadOnlyList<NotebookEntry> entries, string ownerName, ulong userId) :
  MessageComponent` (each creature one line: « 👁️ 🤝 💚 » lit or « ▫️ », « ❔ ??? » before its Aperçu; a select
  `nb:open:{userId}:{place}` listing the creatures, unseen ones as « ❔ ??? »); `NotebookCards.Creature(Place place,
  PlaceCreature c, IReadOnlyList<NotebookEntry> entries, ulong userId) : MessageComponent` (its picture
  `ExpeditionArt.Creature`, or the silhouette and « ❔ ??? » with no name and no blurb before its Aperçu; each step held
  with the Plynling's name and `<t:…:d>`; a back button `nb:back:{userId}:{place}`).
- `[SlashCommand("notebook", "Le carnet des créatures : ce que tes Plynlings ont croisé en expédition (le tien par défaut)")]`
  with an optional `IUser user`, open to anyone like `collection`. One page per place; with one place, no pager.
  `/inventory` stays ≤ 25 subcommands.

- [ ] **Step 1: Write the failing checks**

```csharp
// notebook_hides_unseen: no entries → every creature reads "❔ ???", no creature name in the text
// notebook_shows_steps: (toad, Seen) and (toad, Close) → the toad's line shows 👁️ and 🤝 lit, 💚 not
// creature_page_names_the_plynling: the page lists "Brindille" and the date for each held step
// notebook_caps: a full notebook with 32-character Plynling names builds, ≤ 40 components, ≤ 4000 characters
```

- [ ] **Step 2: Run.** Expected: build error.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run.** Expected: `ALL CHECKS PASSED`.

---

### Task 12: Docs, help and the release

**Files:**
- Create: `docs/agents/plynling-expeditions.md`
- Modify: `CLAUDE.md` (a table row: « Plynling expeditions: places, `.place` files, runs, the notebook, the sweep »),
  `docs/agents/plynling-rpg.md` (expeditions as an XP source, the re-pinned pace), `docs/agents/economy.md`
  (`earn.expedition`, `act.expedition`), `docs/agents/plynling-storylines.md` (a « Place files » section pointing to
  the new note, like « Visit files »), `PlynlingModule.Help.cs` (an « Expéditions » field; `/plynling help` stays
  under 6000), `HelpModule` (`/help`), `README.md`, `ProjectSYNCS/CHANGELOG.md`, `ProjectSYNCS/config.yaml` (see
  Global Constraints for the version), the engine design (« Implemented in … »)

- [ ] **Step 1:** Write `plynling-expeditions.md`: the format (Task 3's grammar), the validator's rules, the draw, the
  run's phases and statuses, the stale guard, away, the sweep, the notebook, the art and its version, the simulation's
  numbers, the keys that are forever.
- [ ] **Step 2:** The help field, `/help`, README, CHANGELOG bullets (one per user-visible change: `/plynling
  expedition`, `/inventory notebook`, away while on a trip).
- [ ] **Step 3: Run** the help-size check and everything. Expected: `ALL CHECKS PASSED`; `/plynling help` < 6000.
- [ ] **Step 4: Dev guild** (the owner, or me if asked): start a Combe trip with an ado, play to the end, wound twice
  once (Asha), leave one alone 15 minutes, check the card while away, `/inventory notebook`, `/pl expedition`.
