# `/plynling stats` — design

2026-11-26. Approved approach: **C** (tabs + duel view), **counter table**, **every counter listed below**.

## Intent

A complete stats sheet for one Plynling, for **showing off its life and comparing** with other people's.
Today the card shows needs and mood, the journal shows moments; nothing adds up a life in numbers
(« 412 repas, 58 % de victoires à pierre-papier-ciseaux, 3e du serveur »).

## Goals

- `/plynling stats` (and `/pl stats`): one public card, five tabs, every stat the database can give.
- **Comparing**, in two ways: a server rank on each tab's headline stats (« 🥇 », « 3e / 14 »), and a
  duel view (`vs:`) showing two Plynlings side by side, stat by stat.
- **Start counting what nobody counts per Plynling today** (baths, foods eaten, games by type…), in a new
  key/value table that can take more counters later without a migration.

## Non-goals

- No `;pl stats` typed shortcut (can be added later; the slash command is enough to ship).
- No history graphs, no per-day buckets for the new counters (all-time totals only).
- No new rankings on `/leaderboard`: the ranks live on this card only.
- No backfill of the new counters from `EconomyDailyStat` (guild-level, can't be split per Plynling).

## Anti-goals

- **A number that lies.** A new counter starts at zero on release day: an older Plynling must not read as
  if it never bathed. Those stats carry « depuis le 26/11 » (see *Counting since*).
- A card that throws at send time (4 000-char text, 40 components): measured in the checks.
- Counting that breaks or slows an action: counters ride the action's own save, never a save of their own.

## Constraints (from CLAUDE.md and `plynling.md`)

- A command added to `PlynlingModule` lands in `/plynling` and `/pl`: 16 → 17 subcommands each (cap 25).
- Components V2: no content, flag re-asserted on every update, `AllowedMentions.None`, names through
  `PlynlingCardUi.SafeName`. Under 40 components counting the whole tree.
- Every button row its own custom-id verb; tabs: selected blurple, others grey.
- Defer before touching the database (stats read a lot; the Pi is slow).
- `[CommandContextType(Guild)]` is already on the module.
- Counter writes **never save**; they ride the action's `SaveChanges`, like `PlynlingScore.AddAsync` and
  `EconomyLog.AddAsync`, and sit beside those calls.
- Keys are stored strings: **append-only, never renamed** (an economy-metric-like contract).
- `/plynling help` is at 5 985 / 6 000: the one line it gains must be paid for by shortening another.
- Minor bump (new command + migration): 8.24.1 → **8.25.0**, CHANGELOG entry.
- `/help`, `README.md` and `guide/` updated (checks section 69 fails if `/plynling stats` appears in no
  guide page).

## Approaches considered

**Storage**
1. Columns on `Plynling` — simplest, `Version`-guarded, but a migration per future counter. Rejected:
   the owner wants the list to grow.
2. **Key/value table `PlynlingCounter` — chosen.** New counters cost a constant, not a migration; ranking a
   key is one query.

**Card**
- A. One long card — overflows 4 000 chars on a busy Plynling. Rejected.
- B. Tabs with ranks — compares through ranks only.
- **C. B plus a duel view — chosen** by the owner.

## Data

### `PlynlingCounter` (new table)

`Id`, `PlynlingId` (FK, cascade like the journal: gone with an abandoned Plynling, kept with a dead one),
`Key` (string), `Value` (long). Unique index on (`PlynlingId`, `Key`).

**Concurrency:** `PlynlingCounter` and `PlynlingCarer` carry their **own `Version` token**, bumped by
`AppDbContext.BumpVersions` like `Plynling` and `PlynlingRelation` *(builder-check call: an event decided
alone can pay `earn.event` without touching the Plynling row, so leaning on the row's token would lose
increments)*. Every counter is still written inside a `PlynlingService` unit, which retries a conflict from
scratch. The first insert of a key can race on the unique index: `UnitAsync` already retries
`EconomySave.IsRace`. `TryGiftAsync` is not retried: it catches that race too and leaves the draw unspent,
as it does for a conflict today.

### `PlynlingCarer` (new table)

Who cared for it: `Id`, `PlynlingId` (FK, cascade), `UserId` (ulong, `.HasConversion<long>()`), `Meals`,
`Pets`. Unique on (`PlynlingId`, `UserId`). Gives « 9 personnes l'ont nourri ou caressé » and « Son plus
grand fan : @x (42 repas) ». Written in feed and pet only (both modify the Plynling row: same rule).
Counts the owner too (their row is « toi »), so the fan is the top **non-owner**.

### The writer — `Helpers/Plynling/PlynlingCounters`

`PlynlingCounters.AddAsync(db, Plynling p, string key, long amount)` — looks in `db.PlynlingCounters.Local`
first, then the database, adds a row if missing, never saves (the `EconomyLog.AddAsync` shape).
`PlynlingCounters.AddCarerAsync(db, p, userId, meals, pets)` the same for carers.
Keys are `const string`s in this file, with `Since` (see below) and the per-food / per-game key builders.

**Pure paths** (`PlynlingLife.Settle`): a settled sick morning and an auto-thaw can't write. They go on
`Plynling.PendingCounters` (`[NotMapped]` list of (key, amount), beside `PendingScore`) and
`FlushMomentsAsync` writes them, like good mornings. A list's in-memory settle saves nothing; the real row
replays the same morning once.

### The counters (keys, append-only)

| Key | Counted where | Amount |
|---|---|---|
| `meal.food.{food}` (one per `PlynlingFood`, lower-case enum name) | `FeedOnceAsync` | 1 |
| `meal.spent.owner` / `meal.spent.others` | `FeedOnceAsync`, paid from a wallet | cailloux |
| `meal.pantry` | `FeedOnceAsync`, served from the pantry | 1 |
| `pet.owner` / `pet.others` | `PetOnceAsync` (needs the petter's id: add `actorId` to `PlynlingService.PetAsync`; `PlynlingCareService.PetAsync` already has it) | 1 |
| `bath.free` / `bath.soap` | `BathOnceAsync` | 1 |
| `medicine` | `MedicateOnceAsync` | 1 |
| `sick.days` | a morning played while sick (`Settle` → pending) | 1 |
| `warning` | the sweep's warning DM (`Units.cs`, where `WarningSent = true`) | 1 |
| `frozen.seconds` | every thaw: self, staff, and the auto-thaw in `Settle` (→ pending) | seconds frozen |
| `play.{game}` / `play.{game}.won` (one per `PlynlingGame`) | `FinishPlayOnceAsync` (pass the game: add a `PlynlingGame` parameter; `PlynlingComponentHandler` has the session) | 1 |
| `gift.cailloux` / `gift.items` / `gift.cailloux.total` | `DrawGiftAsync` (a draw that found nothing counts nothing) | 1 / 1 / cailloux |
| `forage` | `ForageOnceAsync` | 1 |
| `find.forage` / `find.play` / `find.visit` / `find.gift` / `find.event` | where each grants an item (`find.event`: `GiveItem` in `ApplyEventAsync`) | 1 |
| `visit.made` / `visit.hosted` / `visit.good` | `VisitOnceAsync` (good: both sides) | 1 |
| `confess.made` / `confess.accepted` / `confess.refused` / `confess.received` | `VisitOnceAsync` (made/accepted/refused on the visitor, received on the host) | 1 |
| `earn.game` / `earn.badge` / `earn.event` | where those cailloux are paid to the owner (`FinishPlayOnceAsync`, the badge payout in `Life.cs`, `ApplyEventAsync`'s `GiveCailloux`) | cailloux |

The mascot counts like anyone (her badges pay nothing, so her `earn.*` stay 0); she is never ranked.

### Counting since

`PlynlingCounters.Since` = 2026-11-26 (release day, a constant; if the deploy slips, move the constant to the
deploy day — a Plynling adopted in the gap would otherwise miss its marker). On a Plynling adopted before it, every
stat read from the new tables shows a discreet « depuis le 26/11 » marker (once per tab, in the tab's
footer: « * compté depuis le 26/11 »), and those lines carry the `*`. Adopted after: no marker.

### What is derived, not counted

From existing data, exact or near-exact, no counter needed:
- **Existing columns:** `Meals`, `Pets`, `FedByOthers`, `Plays`, `PlaysWon`, `Visits`, `Drills`, `RpgXp` →
  level, `Score`, `Stress`, age, stage, `AdoptedAt`, the death date and cause.
- **Journal:** times sick (`FellSick`), times frozen (`Frozen`), recoveries, traits gained, stories lived
  (`EventStory`). The journal is capped at 500 entries; a Plynling past that may undercount — accepted.
- **Relations:** current bond counts (friends, best friends, couple, rivals, enemies), total meetings, the
  Plynling it met most (« Meilleur copain de visite : X, 23 rencontres »).
- **Events (`PlynlingEventInstance`):** events lived (resolved), challenges tried / succeeded (success %),
  long stories finished (an `EndingKey`).
- **Badges** (n / 19), **traits**, **memories**, **marks** (by kind).

## The card — `Commands/Plynling/PlynlingStatsCards.cs` (static, Context-free)

Components V2, public. Header: the Plynling's dressed picture (`SpriteOf`, sent through `CardSend`), its
name, species, stage, owner. Then the tab's lines as `TextDisplay`s, then one row of five tab buttons.

**Tabs** (`StatsTab` enum: `Life, Care, Play, Social, Collection`; labels « Vie », « Soins », « Jeux »,
« Social », « Collection »):

- **Vie** — age and stage, adopted on, (died on / cause), score ★rank, RPG level ★rank, stress, times sick,
  days sick*, close calls*, times frozen, days frozen*.
- **Soins** — meals ★rank, favourite food (most eaten*, « 37 Morilles »), meals by food*, from the pantry*,
  cailloux spent on its meals (by its owner* / by others*), fed by others, pets ★rank (owner* / others*),
  carers count*, biggest fan*, baths* (free / soap), medicine*.
- **Jeux** — games played ★rank, won, win %, each game played / won / %*, drills ★rank, forages* and items
  found* (by source), gifts received* (cailloux / items, total cailloux), cailloux earned for its owner*.
- **Social** — visits ★rank (made* / received*), good visits*, meetings, bonds now (by kind), most met
  Plynling, confessions* (made / accepted / refused / received), events lived ★rank, challenges (success %),
  stories finished.
- **Collection** — badges n/19 ★rank, traits (by kind), memories, marks (epithets, keepsakes, unlocks).

★rank = a server rank shown after the number: « 🥇 », « 🥈 », « 🥉 », else « 4e / 14 ». **Population:**
every Plynling of the guild, living and dead, never the mascot — the same population as `/leaderboard`'s
Plynlings view. The denominator is that whole population; **a 0 shows no rank** (nothing to brag about),
so a new Plynling's line just has no ★. Ties share the better rank. Ranks are computed for the ★ stats only (about a dozen
queries' worth; group/count in SQL where the column is plain, counters by key).

The selected tab is blurple, the others grey. Custom-ids: `plst:tab:{plynlingId}:{tab}` (one row, one
verb). **Anyone may switch tabs** (the card is public, like the graveyard's pager); a switch defers, reads,
and redraws through `ModifyOriginalResponseAsync`.

### Duel view (`vs:`)

`/plynling stats user:@a vs:@b` — `user` defaults to the caller; `vs` names the other. Same five tabs; each
stat on one line, the leader in bold: « Repas — **412** · 389 ». Header: both names and both pictures (two
sections with thumbnails). Footer: « Pwet mène 7 à 4 sur cet onglet ». No ranks in the duel. Custom-id
`plst:duel:{aId}:{bId}:{tab}` (its own verb). A stat that is « depuis le 26/11 » for either side keeps its
`*`.

**Who leads a line** — each line is a `DuelLine` with a `Better` of `Higher`, `Lower` or `None`:
- `Higher`: every count and total (meals, games, wins, win %, visits, badges, score, level, age…).
- `Lower`: stress, times sick, days sick, close calls — the healthier life wins.
- `None` (never bold, never counted): dates, cause of death, favourite food, biggest fan, most-met
  Plynling, days frozen, times frozen, bonds by kind (an enemy is not « better »).
- **A `*` line is `None` when only one side was adopted before `Since`**: one partial count against one
  complete count is not a contest. When both were (or neither), it competes normally.
- Lists (meals by food, each game) are **one duel line per item**; an item at 0 on both sides is left out.
- Equal values: neither bold, neither scores. The footer counts lines won on the tab shown: « Pwet mène
  7 à 4 », « Égalité 5 à 5 », or nothing when no line competes.

**Which Plynling a mention means:** the living one, else the latest grave (the `/plynling view user:` rule).
Refusals (private): nobody has one, `vs` is the same Plynling.

### Text

Card wording in French, generic where it isn't about one Plynling, gendered (`GenderedLines` or
`PlynlingGrammar.Agree`) where it is (« nourri / nourrie »). No SYNCS voice lines needed: it is a sheet.
Numbers formatted with a French thin space (`12 345`), percentages without decimals.

## Code layout

- `Models/Plynling/PlynlingCounter.cs`, `PlynlingCarer.cs`; `AppDbContext` sets; migration `AddPlynlingCounters`.
- `Helpers/Plynling/PlynlingCounters.cs` — keys, `Since`, writers.
- `Helpers/Plynling/PlynlingStatsUi.cs` — pure: a `PlynlingStatSheet` record → each tab's lines, the duel
  lines and the leader count, rank formatting.
- `Services/Plynling/PlynlingService.Stats.cs` — `GetStatSheetAsync(plynling)` (read-only, `AsNoTracking`,
  settled in memory like the lists) and the ranks.
- `Commands/Plynling/PlynlingStatsCards.cs` — `Build(sheet, tab)`, `BuildDuel(a, b, tab)`.
- `PlynlingModule`: the `stats` command. Tab and duel handlers in `Interactions/Components/` (a new
  `PlynlingStatsHandler`, never on `PlynlingModule`).

## Checks (`tests/ProjectSYNCS.Checks`)

A new section: a sheet with every value at its maximum plausible size (long names, 7-digit numbers, every
food and game) built into both cards for every tab, asserting < 40 components and < 4 000 text characters,
and the leader count on a hand-made duel. Gender halves of any gendered line.

## Docs

`plynling.md` gets a *Stats* section (the counter rule, the pending path, append-only keys, the
population); `/plynling help` gains one line (shorten another to stay under 6 000); `/help` unchanged (it
points at `/plynling help`); `README.md` and the guide's Plynling page gain the command; CHANGELOG 8.25.0.

## Left to the builder

Exact emoji per line, line order inside a tab, the wording of labels, the thin-space formatter's home, how
the five rank queries are batched.
