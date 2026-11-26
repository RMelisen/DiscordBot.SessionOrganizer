# `/plynling stats` Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A public `/plynling stats` (and `/pl stats`) card with five tabs and server ranks, a duel view (`vs:`), and new per-Plynling counters stored in a key/value table.

**Architecture:** Two new tables (`PlynlingCounter`, `PlynlingCarer`) written by a never-saving helper beside each action's `EconomyLog`/`PlynlingScore` call; pure `Settle` paths queue counters on `Plynling.PendingCounters` for `FlushMomentsAsync`. A read-only `PlynlingService.GetStatSheetAsync` gathers one `PlynlingStatSheet`; the pure `PlynlingStatsUi` turns sheets into lines (single and duel); the static `PlynlingStatsCards` draws Components V2.

**Tech Stack:** .NET 10, Discord.Net (Components V2), EF Core + SQLite, `tests/ProjectSYNCS.Checks` console.

**Spec:** `docs/superpowers/specs/2026-11-26-plynling-stats-design.md` — read it first; this plan does not repeat its tables.

## Global Constraints

- Read `CLAUDE.md`, `docs/agents/plynling.md` before coding; their rules bind.
- Command and option names English (`stats`, `user`, `vs`); every user-facing string French; code/comments/logs English.
- Counter keys are stored strings: **append-only, never renamed**. Exact keys: the spec's counter table, plus `find.event`.
- `PlynlingCounters.Since` = `2026-11-26` (Paris day).
- Counter/carer writers **never save**; they ride the action's own `SaveChanges`.
- `PlynlingCounter` and `PlynlingCarer` carry a `Version` concurrency token bumped in `AppDbContext.BumpVersions`.
- Snowflakes `.HasConversion<long>()`. No `HasMaxLength`. Never `DateTime.Now`.
- Components V2: no content/embeds, `AllowedMentions.None`, < 40 components, < 4 000 text chars, names via `PlynlingCardUi.SafeName`. Send through `CardSend` (`RespondCardAsync` / `ModifyCardAsync`).
- Tabs: selected `ButtonStyle.Primary`, others `Secondary`. Custom-ids `plst:tab:{plynlingId}:{tab}` and `plst:duel:{aId}:{bId}:{tab}`; handlers with `ignoreGroupNames: true`, **never on `PlynlingModule`**.
- Ranks: every Plynling of the guild, living and dead, never the mascot; denominator = that population; value 0 → no rank; ties share the better rank (rank = 1 + count strictly greater). Medals 🥇🥈🥉, else « Ne / M » (« 4e / 14 »).
- Version 8.24.1 → **8.25.0**; `ProjectSYNCS/CHANGELOG.md` entry `## 8.25.0 — 2026-11-26`.
- **No commit steps**: the owner commits by hand.
- Build: `cd ProjectSYNCS && dotnet build -warnaserror` (dotnet output is French). Checks: `dotnet run --project tests/ProjectSYNCS.Checks` → `ALL CHECKS PASSED`.

## Review Focus

1. **A dead Plynling's sheet** (`/pl stats user:@x` where x's latest is a grave): shows the memorial picture, death date/cause, keeps all counts, and still ranks (the dead are in the population). → Task 5 test, Task 7 card test.
2. **The mascot**: her sheet renders and she can be dueled, but she never appears in any rank, not even as a denominator. → Task 5 test.
3. **A Plynling adopted after `Since` dueling one adopted before**: `*` lines are not contests (no bold, not in the footer count). → Task 6 test.
4. **Oversized content**: 32-char names fully escaped, 7-digit values, every food and game non-zero, both tabs and duel — fits 40 components / 4 000 chars. → Task 7 test.
5. **An abandoned Plynling** (row deleted): its counters and carers go with it (cascade), and nothing else breaks. → Task 1 test. (Retried units are already safe: `PlynlingService.ResetTracked` reverts every tracked entity, counters included, before a retry.)

---

### Task 1: Counter storage and writer

**Files:**
- Create: `ProjectSYNCS/Models/Plynling/PlynlingCounter.cs`, `ProjectSYNCS/Models/Plynling/PlynlingCarer.cs`, `ProjectSYNCS/Helpers/Plynling/PlynlingCounters.cs`
- Modify: `ProjectSYNCS/Data/AppDbContext.cs` (DbSets, `BumpVersions`, entity config beside `PlynlingScoreDay`), `ProjectSYNCS/Models/Plynling/Plynling.cs` (`PendingCounters`), `ProjectSYNCS/Services/Plynling/PlynlingService.Life.cs:437` (`FlushMomentsAsync`)
- Migration: `cd ProjectSYNCS && dotnet ef migrations add AddPlynlingCounters`
- Test: create `tests/ProjectSYNCS.Checks/StatsChecks.cs` (section 74), register in `Program.cs` after section 73.

**Interfaces — Produces:**
- `class PlynlingCounter { int Id; int PlynlingId; string Key; long Value; int Version; }` — FK cascade to `Plynling`, unique (`PlynlingId`, `Key`), `Version` concurrency token.
- `class PlynlingCarer { int Id; int PlynlingId; ulong UserId; long Meals; long Pets; int Version; }` — FK cascade, `UserId` → long, unique (`PlynlingId`, `UserId`), token.
- `AppDbContext.PlynlingCounters`, `AppDbContext.PlynlingCarers`.
- `Plynling.PendingCounters` : `[NotMapped] List<(string Key, long Amount)>`.
- `static class PlynlingCounters`:
  - `static readonly DateTimeOffset Since` (2026-11-26 00:00 Paris, via `AppTime`); `static bool IsPartial(Plynling p) => p.AdoptedAt < Since`.
  - `const string` for every key in the spec table (`MealPantry = "meal.pantry"`, `PetOwner = "pet.owner"`, … `FindEvent = "find.event"`).
  - `static string Food(PlynlingFood f)` → `"meal.food." + f.ToString().ToLowerInvariant()`; `static string Game(PlynlingGame g)` → `"play." + lower`; `static string GameWon(PlynlingGame g)` → `"play." + lower + ".won"`.
  - `static IReadOnlyList<string> All` — every key, builders expanded over every enum value.
  - `static Task AddAsync(AppDbContext db, Plynling p, string key, long amount)` — `amount <= 0` returns; `Local` then DB lookup; adds row if missing; never saves.
  - `static Task AddCarerAsync(AppDbContext db, Plynling p, ulong userId, long meals, long pets)` — same shape.
- `FlushMomentsAsync` writes then clears `p.PendingCounters` (after the score loop).

- [ ] **Step 1: Write the failing checks** in `StatsChecks.Run(Action<string> fail)` → `Keys()` and `async WriterAsync()` (in-memory SQLite harness copied from `ScoreChecks.ServiceAsync`):
  - `PlynlingCounters.All` distinct; contains `Food(f)` for every `PlynlingFood` and `Game(g)`/`GameWon(g)` for every `PlynlingGame`; contains `"find.event"`.
  - Two `AddAsync(db, p, "bath.free", 1)` then one `SaveChanges` → exactly one row, `Value == 2`.
  - `AddAsync(..., 0)` adds no row.
  - `AddCarerAsync(db, p, 5, 1, 0)` twice + save → one row, `Meals == 2`.
  - `IsPartial` true for a Plynling adopted 2026-11-01, false for 2026-11-27.
  - Deleting a Plynling that has a counter and a carer row → both tables empty for its id.
- [ ] **Step 2:** `dotnet run --project tests/ProjectSYNCS.Checks` → build fails (types missing).
- [ ] **Step 3:** Implement the models, DbContext config, `BumpVersions` (two more loops), `PlynlingCounters`, `PendingCounters`, the flush; add the migration and read its `Up` — only the two tables and indexes.
- [ ] **Step 4:** Checks → section 74 passes, `ALL CHECKS PASSED`.

### Task 2: Care hooks (feed, pet, bath, medicine)

**Files:** Modify `ProjectSYNCS/Services/Plynling/PlynlingService.cs:171-326`, `ProjectSYNCS/Services/Plynling/PlynlingCareService.cs:33`; test in `StatsChecks.cs`.

**Interfaces:**
- Consumes: Task 1 writers and keys.
- Produces: `PlynlingService.PetAsync(int plynlingId, ulong actorId, DateTimeOffset now)` (new `actorId`; update every caller — grep `\.PetAsync(`).

Counting, all inside the existing unit, beside the `EconomyLog` lines: feed → `Food(food)` 1; `meal.pantry` 1 when `fromPantry`, else `meal.spent.owner`/`meal.spent.others` by `isOwner` with `price`; carer (`actorId`, meals 1). Pet → `pet.owner`/`pet.others`; carer (pets 1). Bath → `bath.soap` when `useSoap` else `bath.free`. Medicine → `medicine`.

- [ ] **Step 1: Failing checks** `CareAsync()`: owner feeds a Truffle from wallet → `meal.food.truffle == 1`, `meal.spent.owner == price`; other person feeds from pantry (grant them 2 of the food first) → `meal.pantry == 1`, no `meal.spent.others`; carers: owner row Meals 1, other row Meals 1; pet by other → `pet.others == 1`, carer Pets 1; a refused feed (TooPoor) changes no counter; free bath → `bath.free == 1`.
- [ ] **Step 2:** Checks fail (counters 0 / `PetAsync` arity).
- [ ] **Step 3:** Implement the hooks and the `actorId` parameter.
- [ ] **Step 4:** Checks pass; `dotnet build -warnaserror` clean.

### Task 3: Play, gift, forage, visit, finds and earnings hooks

**Files:** Modify `PlynlingService.Relations.cs` (`FinishPlayAsync`, `VisitOnceAsync`), `PlynlingService.Life.cs` (`DrawGiftAsync`, `TryGiftAsync`, `ForageOnceAsync`, badge payout ~line 393), `PlynlingService.Events.cs:360-366`, `Interactions/Components/PlynlingComponentHandler.cs:512`; test in `StatsChecks.cs`.

**Interfaces — Produces:** `FinishPlayAsync(int plynlingId, ulong ownerId, PlynlingGame game, bool won, long pebbles, DateTimeOffset now, Random? rng = null)` (the handler passes the session's game).

Counting: play → `Game(game)`, `GameWon(game)` on a win, `earn.game` pebbles, `find.play` on a find. Gift → cailloux: `gift.cailloux` 1 + `gift.cailloux.total` found; item: `gift.items` 1 + `find.gift` 1; nothing found: nothing. `TryGiftAsync` also catches `DbUpdateException` when `EconomySave.IsRace(ex)` → `DiscardChanges()`, `GiftResult.None`. Forage → `forage` 1, `find.forage` 1. Visit → `visit.made` (visitor), `visit.hosted` (host), `visit.good` both when good; `confess.made` (visitor) + `confess.received` (host) when `play.Confess`, then `confess.accepted`/`confess.refused` on the visitor; `find.visit` per side's find. Badge payout → `earn.badge` reward (not the mascot, already gated). Events → `earn.event` amount in the `GiveCailloux` case, `find.event` 1 in the `GiveItem` case.

- [ ] **Step 1: Failing checks** `PlayVisitAsync()`: `FinishPlayAsync(a, Owner, PlynlingGame.Memory, won: true, pebbles: 5, Now, new Random(1))` → `play.memory == 1`, `play.memory.won == 1`, `earn.game == 5`; a lost RPS → `play.rockpaperscissors == 1`, no `.won`; a visit between two healthy Plynlings with `VisitPlay` non-confess (see `VisitChecks` for how one is built) → visitor `visit.made == 1`, host `visit.hosted == 1`, both `visit.good` equal to whether `outcome.Good`; `ForageAsync` → `forage == 1`, `find.forage == 1`.
- [ ] **Step 2:** Checks fail.
- [ ] **Step 3:** Implement hooks and the `game` parameter.
- [ ] **Step 4:** Checks pass; build clean.

### Task 4: Pure paths — sick days, frozen time, warnings

**Files:** Modify `Helpers/Plynling/PlynlingLife.cs` (`PlayMorning` ~388, `EndFreeze` ~655), `Services/Plynling/PlynlingService.Units.cs:126-129`; verify every thaw save path (self/staff thaw in `PlynlingService.cs` ~396, the sweep's auto-thaw) calls `FlushMomentsAsync` before its save — add the call where missing. Test in `StatsChecks.cs`.

Counting: `PlayMorning`, in the branch reached when the Plynling is **already sick at the start of the morning** → `p.PendingCounters.Add(("sick.days", 1))`. `EndFreeze(p, at)` → `p.PendingCounters.Add(("frozen.seconds", (long)(at - p.FrozenAt!.Value).TotalSeconds))`, read before `FrozenAt` is cleared. Sweep warning → `PlynlingCounters.AddAsync(db, p, "warning", 1)` where `WarningSent = true`.

- [ ] **Step 1: Failing checks** `PureAsync()` (pure, no DB): a Plynling sick since yesterday's morning (well fed, so it can't die), `Settle` to just after the next 05:00 → exactly one `("sick.days", 1)` in `PendingCounters` (sick at the start of that morning, whatever the morning then rolls); a healthy one settled across a morning → no `sick.days`; a self-freeze at T, `PlynlingLife.Thaw(p, T + 2 days)` → `frozen.seconds == 172800`. DB: freeze then `ThawAsync` through the service → stored `frozen.seconds == 172800`.
- [ ] **Step 2:** Checks fail.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Checks pass, including every existing section (Settle is shared; sections on sickness and score must stay green).

### Task 5: The stat sheet and the ranks

**Files:** Create `Services/Plynling/PlynlingService.Stats.cs`, `Helpers/Plynling/PlynlingStatSheet.cs`; test in `StatsChecks.cs`.

**Interfaces — Produces** (in `Helpers/Plynling/PlynlingStatSheet.cs`, namespace `ProjectSYNCS.Helpers`):
```csharp
public enum RankedStat { Score, Level, Meals, Pets, Plays, Drills, Visits, EventsLived, Badges }
public sealed record StatRank(int Rank, int Of);
public sealed record StatFacts(
    int TimesSick, int TimesFrozen, int Badges, int Memories,
    IReadOnlyDictionary<TraitKind, int> Traits, IReadOnlyDictionary<MarkKind, int> Marks,
    IReadOnlyDictionary<PlynlingBond, int> Bonds, int Meetings, string? MostMetName, int MostMetMeetings,
    int EventsLived, int ChallengesTried, int ChallengesWon, int StoriesFinished);
public sealed record PlynlingStatSheet(
    Plynling Plynling, DateTimeOffset Now, bool Partial,
    IReadOnlyDictionary<string, long> Counters,
    IReadOnlyList<PlynlingCarer> Carers,
    StatFacts Facts,
    IReadOnlyDictionary<RankedStat, StatRank> Ranks)
{ public long Count(string key) => Counters.TryGetValue(key, out var v) ? v : 0; }
```
- `PlynlingService.GetStatSheetAsync(Plynling p, DateTimeOffset now) : Task<PlynlingStatSheet>` — everything `AsNoTracking`, writes nothing. `Partial = PlynlingCounters.IsPartial(p)`. Facts per the spec's *derived* list (journal `FellSick`/`Frozen`; resolved `PlynlingEventInstance` = `ResolvedAt != null`; challenges = `ChallengeSucceeded != null`; stories finished = `EndingKey != null`; most met = relation with the most `Meetings`, ties on lower other id). `Ranks` contains only stats whose value > 0 and only when `p` is not the mascot.
- `PlynlingService.RankAsync(ulong guildId, RankedStat stat, int plynlingId) : Task<StatRank?>` (private is fine) — population: `Plynlings` in guild with `OwnerId != PlynlingMascot.OwnerId`; values: `Score`, `RpgXp` (Level), `Meals`, `Pets`, `Plays`, `Drills`, `Visits` from columns; `EventsLived` and `Badges` by `GroupBy(PlynlingId)` counts. Rank = 1 + count of strictly greater; `Of` = population size.

- [ ] **Step 1: Failing checks** `SheetAsync()`: guild with A (Meals 10), B (Meals 10, dead), C (Meals 3), mascot (Meals 99): A → `Ranks[Meals] == (1, 3)`; B → `(1, 3)` (dead ranked, tie shares); C → `(3, 3)`; a Plynling with Drills 0 has no `Drills` rank; the mascot's sheet has no ranks; journal with two `FellSick` → `TimesSick == 2`; counters from Tasks 2–3 appear in `Counters`.
- [ ] **Step 2:** Checks fail.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Checks pass.

### Task 6: Lines — single and duel (pure)

**Files:** Create `Helpers/Plynling/PlynlingStatsUi.cs`; test in `StatsChecks.cs`.

**Interfaces — Produces:**
```csharp
public enum StatsTab { Life, Care, Play, Social, Collection }
public enum Better { Higher, Lower, None }
public sealed record StatLine(string Id, string Label, string Text, double? Value, Better Better, bool SinceRelease, StatRank? Rank);
public sealed record DuelLine(string Label, string A, string B, int Winner); // -1 A, 1 B, 0 none
public static class PlynlingStatsUi
{
    public static string TabLabel(StatsTab t);                                  // « Vie », « Soins », « Jeux », « Social », « Collection »
    public static IReadOnlyList<StatLine> Lines(PlynlingStatSheet s, StatsTab t); // spec's tab contents, in order
    public static string Render(StatLine l, bool partial);                       // « 🍽️ Repas : 412 · 🥇 », « * » when SinceRelease && partial
    public static string Rank(StatRank r);                                        // 🥇/🥈/🥉 or « 4e / 14 »
    public static IReadOnlyList<DuelLine> Duel(PlynlingStatSheet a, PlynlingStatSheet b, StatsTab t);
    public static string? DuelFooter(IReadOnlyList<DuelLine> lines, string aName, string bName); // « X mène 7 à 4 », « Égalité 5 à 5 », null
    public static string? SinceFooter(PlynlingStatSheet s, StatsTab t);         // « -# * compté depuis le 26/11 » when partial and the tab has a SinceRelease line
}
```
Rules (spec, *Who leads a line*): `Better` per line as listed; list items (each food, each game) are separate lines with stable `Id`s (`food.morel`, `game.memory`); single view skips list items at 0; `Duel` zips by `Id`, drops list items 0 on both sides, sets `Winner = 0` when `Better == None`, values equal, or `SinceRelease && a.Partial != b.Partial`; bold the winner's text (`**412**`). Gendered words through `PlynlingGrammar.Agree`. Numbers with the French group separator (`ToString("N0", fr-FR)`), percentages integer.

- [ ] **Step 1: Failing checks** `LinesChecks()` on hand-built sheets (no DB; construct `Plynling` with `PlynlingLife.Create` and dictionaries):
  - `Rank(new(1, 9)) == "🥇"`, `Rank(new(4, 14)) == "4e / 14"`.
  - Duel on Care: A meals 412 vs B 389 → that line `Winner == -1`, `A == "**412**"`.
  - Duel on Life: A times sick 2 vs B 0 → `Winner == 1`.
  - A adopted 2026-11-01 (partial), B adopted 2026-11-27: `bath.free` 5 vs 9 → `Winner == 0`; both partial → `Winner == 1`.
  - Food Morel 0/0 line absent from duel; Morel 3/0 present.
  - `DuelFooter` with wins 7/4 → contains « mène 7 à 4 »; 0 competing lines → null.
  - `SinceFooter` null for a non-partial sheet.
- [ ] **Step 2:** Checks fail.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Checks pass.

### Task 7: Cards, command, handlers

**Files:** Create `Commands/Plynling/PlynlingStatsCards.cs`, `Interactions/Components/PlynlingStatsHandler.cs`; modify `Commands/Plynling/PlynlingModule/PlynlingModule.cs` (the `stats` command); test in `StatsChecks.cs`.

**Interfaces — Produces:**
- `PlynlingStatsCards.Build(PlynlingStatSheet s, StatsTab tab) : MessageComponent` — container (accent colour of your choice), header section with thumbnail (`PlynlingArt.SpriteOf(p, now)` alive, `PlynlingArt.Memorial(species, MemorialTier(p))` dead, like `PlynlingJournalCards`), title « 📊 Statistiques de **{SafeName}** » + species · stage · `<@owner>`, the lines as one or two `TextDisplay`s, `SinceFooter`, one row of five tab buttons `plst:tab:{id}:{(int)tab}`.
- `PlynlingStatsCards.BuildDuel(PlynlingStatSheet a, PlynlingStatSheet b, StatsTab tab) : MessageComponent` — two header sections (one per Plynling), duel lines « Label — A · B », footer, tab row `plst:duel:{a.Id}:{b.Id}:{(int)tab}`.
- Command in `PlynlingModule`:
  `[SlashCommand("stats", "Les statistiques d'un Plynling, ou un duel entre deux (le tien par défaut)")] StatsAsync([Summary("user", "À qui est le Plynling (par défaut : toi)")] IUser? user = null, [Summary("vs", "Comparer avec le Plynling de quelqu'un")] IUser? vs = null)`
  — `GetShownAsync` for each; refusals ephemeral before defer (`PlynlingText.NoPlynling` / `NoneFor`, and « C'est le même Plynling ! » when both resolve to one id); `DeferAsync()` public, build sheets, `ModifyCardAsync`.
- `PlynlingStatsHandler : InteractionModuleBase<SocketInteractionContext<SocketMessageComponent>>` (copy the shape of an existing component handler): `[ComponentInteraction("plst:tab:*:*", ignoreGroupNames: true)]` and `[ComponentInteraction("plst:duel:*:*:*", ignoreGroupNames: true)]` — `DeferAsync()`, load by id (`GetByIdAsync` read path that settles without saving or the shown-read used by graveyard), a Plynling gone (abandoned) → ephemeral follow-up « Ce Plynling n'existe plus. », else `ModifyOriginalResponseAsync` via `CardSend.ModifyCardAsync`.

- [ ] **Step 1: Failing checks** `CardBudget()`: a sheet with a 32-char name of `*`, every counter key at 9 999 999, every fact at 9 999 999, every rank set, partial; for each `StatsTab`: `Build` and `BuildDuel(sheet, sheet2)` → components ≤ 40 and text ≤ 4 000 (reuse `MultiServerChecks`' `Count`/`TextLength` — move them to `Check.cs` as public helpers); no custom-id appears twice in one card; a dead sheet's header thumbnail is the memorial URL.
- [ ] **Step 2:** Checks fail.
- [ ] **Step 3:** Implement; if a tab overflows, split its text over two `TextDisplay`s or trim labels — never drop stats.
- [ ] **Step 4:** Checks pass; `dotnet build -warnaserror` clean.
- [ ] **Step 5:** Count subcommands: `/plynling` and `/pl` each 17 ≤ 25.

### Task 8: Help, docs, guide, version

**Files:** `Commands/Plynling/PlynlingModule/PlynlingModule.Help.cs`, `README.md`, the guide page that lists `/plynling journal` (grep `guide/` for it), `docs/agents/plynling.md`, `ProjectSYNCS/config.yaml`, `ProjectSYNCS/CHANGELOG.md`.

- [ ] **Step 1:** `/plynling help`: add one line for `stats` (and `vs:`); shorten another line so the embed stays ≤ 6 000 (it is at 5 985 — measure).
- [ ] **Step 2:** README and the guide page: the command, the tabs, the duel, the ranks, « depuis le 26/11 » — plain, members' view, her voice only in asides (`docs/syncs-voice.md`).
- [ ] **Step 3:** `plynling.md`: a `## Stats` section — the counter table and keys (append-only), the never-save writer and `PendingCounters`, the `Version` tokens, the rank population, the duel's leader rule, the card budget check (section 74).
- [ ] **Step 4:** `config.yaml` → `8.25.0`; CHANGELOG `## 8.25.0 — 2026-11-26` with bullets: new `/plynling stats` with tabs and server ranks; duel view with `vs:`; new per-Plynling counters from today.
- [ ] **Step 5:** `dotnet build -warnaserror` and the full checks → `ALL CHECKS PASSED` (section 69 sees `/plynling stats` in the guide).
