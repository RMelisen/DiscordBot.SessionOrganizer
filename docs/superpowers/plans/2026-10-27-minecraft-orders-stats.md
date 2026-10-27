# Minecraft orders and stats — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** « tu fais quoi ? » in game, a free-time order, fetch and craft orders, Minecraft stats by
month in `/report minecraft` and Papa's digest, and `minecraft.md` split in three.

**Architecture:** C# reads and decides (`McCommand`, `MinecraftBodyService`, pools in
`BotResponses.Minecraft.cs`); the Node body acts and reports JSON events. Stats get a daily-bucket
table beside `MinecraftStat`, read by a pure `MinecraftReport` and rendered by `ReportCards`.

**Tech Stack:** .NET 10, EF Core SQLite, Discord.Net 3.20; Node 22 ES modules, mineflayer 4.39.

**Spec:** `docs/superpowers/specs/2026-10-27-minecraft-orders-stats-design.md`

## Global Constraints

- Branch `minecraft-orders-stats` (worktree `.claude/worktrees/minecraft-orders-stats`), base `f8c4e7f`.
- **No commits**: the owner commits by hand. Every "commit" point below is "leave it in the tree".
- Version `7.44.0` → `7.45.0` in `ProjectSYNCS/config.yaml`, one `## 7.45.0 — 2026-10-27` CHANGELOG entry, English, no member names.
- Command/option names English; every user-facing string French; code, comments, logs English.
- No French in the body. Every line is a `Mc*` pool, picked through `ResponsePicker.Pick(pool, LineChoices)` via `Reply`/`Ack`.
- In-game lines: plain text ≤ 256 chars, drawable by Minecraft's font, kaomoji ≤ 1 line in 4, none in pools of 2–3 lines (`docs/syncs-voice.md`).
- Every new pool goes in the checks' in-game plain-text list; every person-aimed pool with twins goes in `McFamilyPools`.
- Stored keys append-only: `ReportKind.Minecraft` appended last; `McStats` keys unchanged. `McOrder` is not stored: new members appended last anyway.
- SQLite: filter `Day` (int) in SQL, never `DateTimeOffset`.
- Check before `stay()`: a body op that can fail at once must fail before dropping the current task.
- Verify with `dotnet build` and `dotnet run --project tests/ProjectSYNCS.Checks` (exit 0). Body: `node --check` on every touched file, and `npm ci` in `ProjectSYNCS/MinecraftBody` then `node tools/mc-pathtest/fight-check.js` and `node tools/mc-pathtest/column-check.js`. The local Paper server's folder is unknown: server-side scenarios are written but only run if the owner points at it.

## Review Focus

1. « t'es libre ce soir ? » / « t'es libre demain ? » to a friend must stay talk, not free her. → Task 2 cases.
2. « je vais chercher du pain dans le coffre » (talk about oneself) must not send her to a chest; « range le pain dans le coffre » must stay Store. → Task 4 cases.
3. « fais-moi peur », « fais-moi rire », « fais-moi confiance » must not be craft; « fabrique-moi une pioche en fer » must not be Collect iron. → Task 5 cases.
4. A month she didn't play must not put an empty Minecraft card in Papa's digest, and his brief must still end with « À regarder ». → Task 3 checks.
5. A whispered order's place must not leak when another player asks « tu fais quoi ? ». → Task 1 check on `FirstPerson` with `place: null`.

---

### Task 1: « tu fais quoi ? » and the `doing` report

**Files:**
- Create: `ProjectSYNCS/MinecraftBody/lib/doing.js`
- Modify: `ProjectSYNCS/MinecraftBody/lib/wiring.js` (register the tick), `ProjectSYNCS/MinecraftBody/index.js` (protocol header: `doing {kind,player,what}`)
- Modify: `ProjectSYNCS/Helpers/Minecraft/McDoing.cs`, `ProjectSYNCS/Services/Minecraft/MinecraftBodyService.cs` (field), `.Events.cs` (case), `.Talk.cs` (branch), `.Discord.cs` (`PublicStatusText`)
- Modify: `ProjectSYNCS/Services/Personality/BotResponses/BotResponses.Minecraft.cs`
- Test: `tests/ProjectSYNCS.Checks/Program.cs`

**Interfaces:**
- Produces (body): event `doing {kind, player, what}`, emitted when the triple changes, checked once a second. `kind` ∈ the task's kind for an order (`come`, `follow`, `fish`…), else `routine` (`what` = farm|fish|ranch|home|trip), `tagAlong` (`player` = who), `forage`, `retrieve`, `danger` (fight, hazard, dodge, defend, flee), `sleep` (`bot.isSleeping`), `held` (`sitting` or `heldUntil > now`), `idle`.
- Produces (C#): `public sealed record McBodyDoing(string Kind, string Player, string What);` in `McDoing.cs`; field `private McBodyDoing? _doing;` (reset to null where `_inventory` is); `public static string FirstPerson(McOrder order, string? forWho, string? place)`; `public static string? OwnLife(McBodyDoing? doing, string? who, bool firstPerson)` — null for `idle`, unknown kinds and order kinds; `who` is `Who(doing.Player)` for tagAlong.
- Produces (pools): `McDoingLines` ({1} the clause, starting lower-case, e.g. « {1}. Et toi ? »), `McDoingNothingLines`.

- [ ] **Step 1: Write the failing checks**
  - `foreach (McOrder o in Enum.GetValues<McOrder>()) if (string.IsNullOrWhiteSpace(McDoing.FirstPerson(o, "Bob", "« mine »"))) Fail(...)`.
  - `McDoing.FirstPerson(McOrder.Follow, "Papa", null) == "je suis Papa"`; `FirstPerson(McOrder.Fish, "Bob", null) == "je pêche pour Bob"`; `FirstPerson(McOrder.GoToPlace, "Bob", null)` contains no « « ».
  - `McDoing.OwnLife(new("routine","","fish"), null, true) == "je pêche un peu"`; `OwnLife(new("tagAlong","papa",""), "Papa", true) == "je traîne avec Papa"`; `OwnLife(new("idle","",""), null, true) is null`; `OwnLife(new("routine","","farm"), null, false) == "elle s'occupe de la ferme (sa routine)"`.
  - Add `McDoingLines`, `McDoingNothingLines` to the in-game list; `McDoingLines` formats with a 60-char {1} (add to `McWideArgs`).
- [ ] **Step 2: Run checks** — `dotnet run --project tests/ProjectSYNCS.Checks`; expect build errors (missing members).
- [ ] **Step 3: Implement `McDoing.FirstPerson` / `OwnLife` and the pools.** First person mirrors `Describe` arm for arm (« je rejoins », « je suis », « je protège », « j'attends ici », « je vais en … », « je ramasse des ressources », « je moissonne », « je m'occupe de la ferme », « je m'occupe des animaux », « je pêche », « je fais cuire quelque chose », « je dors », « j'apporte quelque chose à … », « je range mes affaires », « je monte le camp », « je démonte le camp », plus the Task 2/4/5 orders), default « je m'occupe d'une demande ». OwnLife's third person ends with « (sa routine) » for routine kinds only.
- [ ] **Step 4: Body `doing.js`** — `export function doingTick()` (throttled to 1 s by its own timestamp, like `autonomyTick`), computing the triple from `task`, `bot.isSleeping`, `sitting`, `heldUntil` and emitting on change; `export function forgetDoing()` resets the last value (called on spawn so the first report always goes). Register `doingTick` on `physicsTick` in `wiring.js` after `autonomyTick` (order matters only for being last).
- [ ] **Step 5: Service** — `case "doing"` stores `_doing` under `_gate`. In `Talk()`, right after `if (read is not { } talk) return false;`: when `talk.Kind == nameof(ChatIntent.WhatAreYouDoing)`, build the clause — `_order ?? _pending` first (`FirstPerson(job.Order, Who(job.Player), job.Private && !SamePlayer(job.Player, player) ? null : job.Detail)`), else `OwnLife(_doing, …, true)`, else nothing — and `Reply(player, whispered, clause is null ? McDoingNothingLines : McDoingLines, clause)` through `McChat.Game`, then return true. `PublicStatusText`: with no job, use `OwnLife(_doing, …, false)` when non-null.
- [ ] **Step 6: Run checks and build** — exit 0; `node --check` on `doing.js`, `wiring.js`.

### Task 2: Free-time order

**Files:**
- Modify: `ProjectSYNCS/Helpers/Minecraft/McCommand.cs` (enum, reading), `McCommand.Words.cs` (`FreePhrases`, `FreeLaterWords`), `McDoing.cs`, `McHelp.cs`
- Modify: `MinecraftBodyService.Orders.cs`, `BotResponses.Minecraft.cs`
- Modify: `ProjectSYNCS/MinecraftBody/lib/commands.js`, `lib/orders.js`, `lib/combat.js` (`holdForFight`), `index.js` header
- Test: `tests/ProjectSYNCS.Checks/Program.cs`

**Interfaces:**
- Produces: `McOrder.Free` (appended); body op `{"op":"free","player":"…"}`; `orders.js` `export function skipGrace()` (sets `wasOrder = false; freeSince = 0`).
- Produces (pools): `McFreeLines` + `McFreeLinesOwner` + `McFreeLinesTata` (twins, no {0} in twins; add `"McFreeLines"` to `McFamilyPools`), `McFreeAlreadyLines`.

- [ ] **Step 1: Write the failing cases**
  `Mc("syncs t'es libre", McOrder.Free)`, `Mc("syncs quartier libre", McOrder.Free)`, `Mc("syncs fais ta vie", McOrder.Free)`, `Mc("c'est bon syncs tu peux y aller", McOrder.Free)`, `Mc("syncs repos", McOrder.Free)`;
  `Order("syncs t'es libre ce soir ?", McOrder.None)` (or Unknown — assert `!= McOrder.Free`), same for « t'es libre demain ? », « t'es libre samedi ? »; `Order("syncs vas-y", …) != Free`; `Order("syncs t'es pas libre", McOrder.None)`; `Mc("syncs bouge pas", McOrder.Stay)` still.
  Help: `new("syncs quartier libre", McOrder.Free)` parses.
- [ ] **Step 2: Run checks** — fail.
- [ ] **Step 3: Parser** — `FreePhrases` (spec list: « t es libre », « tu es libre », « quartier libre », « fais ta vie », « vis ta vie », « c est bon tu peux y aller », « tu peux y aller », « tu peux disposer », « repos », « rompez », « dismissed », « go play », « tu peux arreter de me suivre »), written in `Clean`'s normalised form like the other lists. Read just before `SitPhrases` (McCommand.cs ~323) with `When`, skipped when any `FreeLaterWords` word follows the hit (« ce », « demain », « soir », « aprem », « samedi », « dimanche », « weekend », « week », « lundi »…« vendredi », « quand », « bientot »). `McDoing.Describe`: `McOrder.Free => "elle reprend sa vie"`; `FirstPerson`: « je reprends ma vie ».
- [ ] **Step 4: Service** — in `OnChat`, treat Free as a stop for the double-send gate (`stops` includes it). After the claim check: if `_order is null && _pending is null && _doing?.Kind is not "held"`, `Reply(McFreeAlreadyLines)` and return; otherwise `body.Send(new { op = "free", player })`, clear `_order` and `_pending`, and `Ack(player, whispered, "free", Kin(player, McFreeLines, McFreeLinesOwner, McFreeLinesTata))` at once (as Stay is acknowledged).
- [ ] **Step 5: Body** — `commands.js` `case 'free': stay(); letGo(); skipGrace(); break;` (not in `OrderOps`). `holdForFight`: hold `free` like `stay`/`sit`.
- [ ] **Step 6: Run checks, build, `node --check`** — exit 0.

### Task 3: Stats by day, `/report minecraft`, Papa's digest

**Files:**
- Create: `ProjectSYNCS/Models/Minecraft/MinecraftDailyStat.cs`, `ProjectSYNCS/Helpers/Minecraft/MinecraftReport.cs`, migration `AddMinecraftDailyStats`
- Modify: `Data/AppDbContext.cs`, `Models/Minecraft/MinecraftStat.cs` (comment), `Services/Minecraft/MinecraftDataService.cs`, `MinecraftBodyService.Discord.cs` (`FlushStatsAsync`), `Services/Admin/ReportService.cs`, `Commands/Admin/ReportCards.cs`, `Commands/Admin/ReportModule.cs`, `Services/Admin/MonthlyDigestService.cs`, `Commands/Core/HelpModule.cs:130`, `README.md` (l.50, ~645, digest), `docs/agents/admin.md`
- Test: `tests/ProjectSYNCS.Checks/Program.cs`

**Interfaces:**
- Produces: `public class MinecraftDailyStat { int Id; int Day; string Key; long Count; }`, unique index `(Day, Key)`, `DbSet<MinecraftDailyStat> MinecraftDailyStats`.
- Produces: `Task AddStatsAsync(IReadOnlyDictionary<string, long> counts, int day)` — totals and `day`'s bucket in one save; caller passes `AppTime.DayKey(DateTimeOffset.UtcNow)`.
- Produces: `Task<Compared<MinecraftReportData>> ReportAsync(ReportWindow window)` on `MinecraftDataService` (Day filtered in SQL, computed for `window` and `window.Previous`).
- Produces: `public sealed record McCountLine(string Name, long Count);` and `public sealed record MinecraftReportData(long Minutes, int DaysPlayed, long Orders, IReadOnlyList<McCountLine> TopPlayers, long Deaths, long Kills, long Fish, long Treasure, long Harvested, IReadOnlyList<McCountLine> Gathered) { public bool Played => Minutes > 0 || Orders > 0; }`; `public static class MinecraftReport { public const int ListedPlayers = 5; public static MinecraftReportData Compute(IReadOnlyList<MinecraftDailyStat> rows); }` — `DaysPlayed` = distinct days with `minutes` > 0; `TopPlayers` from `orders:` keys, largest first, at most 5; `Gathered` from `gathered:` keys, largest first (all of them; the card fits them).
- Produces: `ReportKind.Minecraft` (last), `ReportCards.BuildMinecraft(Compared<MinecraftReportData> c, ReportWindow w, bool names)`, `ReportService.IsEmptyAsync(ReportKind kind, ReportWindow window)` (true only for Minecraft with `!Played`), `BuildOwnerBrief(…, Compared<MinecraftReportData> minecraft, IReadOnlyList<DigestAlert> alerts, …)`.

- [ ] **Step 1: Failing checks**
  - `MinecraftReport.Compute` on rows `{20261003 minutes 90}`, `{20261004 minutes 30}`, `{20261004 orders 7}`, `{20261004 orders:bob 5}`, `{20261004 orders:papa 2}`, `{20261004 gathered:wood 40}`, `{20261005 deaths 1}` → `Minutes 120`, `DaysPlayed 2`, `Orders 7`, `TopPlayers[0] == ("bob",5)`, `Gathered[0] == ("wood",40)`, `Deaths 1`, `Played`; empty rows → `!Played`.
  - Card: worst case (5 players of 32 chars, 9 resources, all counts 9-digit) through `T6Fits("minecraft", BuildMinecraft(new(w, w), t6Month, names), names)` for names true/false; `names: false` has no player names (assert the 32-char name is absent).
  - Digest list check becomes `{ Syncs, Commands, Quiz, Minecraft }`.
  - Both `BuildOwnerBrief` calls pass a `Compared<MinecraftReportData>`; the last field is still « À regarder »; a « Minecraft » field exists and reads « Pas joué ce mois-ci. » when `!Played`.
- [ ] **Step 2: Run checks** — fail.
- [ ] **Step 3: Model, DbSet, `OnModelCreating`** (`e.HasIndex(x => new { x.Day, x.Key }).IsUnique();`), `MinecraftStat` comment updated; `dotnet ef migrations add AddMinecraftDailyStats` from `ProjectSYNCS/`; inspect that the migration only creates the table and index.
- [ ] **Step 4: `AddStatsAsync(counts, day)`** and its caller; `MinecraftReport.Compute`; `ReportAsync`.
- [ ] **Step 5: Report wiring** — `ReportService` takes `MinecraftDataService`; `BuildAsync` arm `ReportKind.Minecraft => ReportCards.BuildMinecraft(await _minecraft.ReportAsync(window), window, names)`. Card `⛏️ Minecraft`: fields « Temps de jeu » (`Duration(minutes)` + trend, « sur N jours »), « Ordres » (total + trend; top players `Format.Sanitize`d, only when `names`), « Aventures » (deaths, monsters killed, fish, treasures), « Récoltes » (harvested + gathered resources through `Fit`, named via `BotResponses.McResourceNames` or the raw key); empty window → description « Elle n'a pas joué sur cette période. ».
- [ ] **Step 6: `/report minecraft`** — `[SlashCommand("minecraft", "Minecraft : son temps de jeu, ses ordres, ses aventures")] public Task MinecraftAsync() => ShowAsync(ReportKind.Minecraft);`
- [ ] **Step 7: Digest** — `OwnerDigest` appends `ReportKind.Minecraft`; the loop does `if (await reports.IsEmptyAsync(kind, window)) continue;` before building; `BuildBriefAsync` (Owner) fetches `_minecraft.ReportAsync(window)` and passes it; `BuildOwnerBrief` adds a « Minecraft » field before `AddAlerts` (« **12 h 30** en jeu, N jours » + trend, or « Pas joué ce mois-ci. »).
- [ ] **Step 8: Docs** — HelpModule `/report` line gains « Minecraft »; README row and sub-bullet; admin.md kinds list, helpers list, Papa's figures, Papa's digest kinds (« `minecraft`, left out for a month she didn't play »).
- [ ] **Step 9: Run checks and build** — exit 0; note the `/help` page measure still passes.

### Task 4: Fetch from a chest

**Files:**
- Create: `ProjectSYNCS/MinecraftBody/lib/handover.js`, `ProjectSYNCS/MinecraftBody/lib/fetch.js`
- Modify: `lib/commands.js`, `lib/orders.js` (`OrderOps` += `fetch`), `index.js` header
- Modify: `McCommand.cs` (enum + reading), `McCommand.Words.cs` (`FetchVerbs`), `McDoing.cs`, `McHelp.cs`, `MinecraftBodyService.Orders.cs`, `.Events.cs`, `.Outcomes.cs`, `.AtOnce.cs` (unknown goods), `BotResponses.Minecraft.cs`
- Test: `tests/ProjectSYNCS.Checks/Program.cs`

**Interfaces:**
- Produces (body): `handover.js` `export async function handOver(player, gained)` — `gained` a `Map<itemType, n>`; walks within 2 of the player, looks at them, and per type tosses whole stacks smallest first while `left > 0`, **including a last stack bigger than `left`** (the merged one); returns `delivered` (bool). Task 5 uses it.
- Produces (body): `fetch.js` `export async function fetchFromChests(player, key, count)`; op `{"op":"fetch","player","goods","count"}` (`count` 0 = up to 64); events `fetching {player,goods}`, `fetched {player,goods,count,asked,delivered}`, `cantFetch {player,goods,why: none|noChest}`.
- Produces (C#): `McOrder.Fetch` (appended), using `McRequest.Goods` and `Count` (0 when no number given).
- Produces (pools): `McFetchAckLines`, `McFetchDoneLines` + twins (`McFamilyPools`), `McFetchShortLines` ({1} count, {2} goods), `McFetchNoneLines` ({1} goods), `McFetchNoChestLines`, `McFetchUnknownLines`.

- [ ] **Step 1: Failing cases**
  `Mc("syncs ramène-moi du fer du coffre", McOrder.Fetch, r => r.Goods == McGoods.Iron && r.Count == 0)`; `Mc("syncs va chercher 20 pains dans le coffre", McOrder.Fetch, r => r.Goods == McGoods.Food && r.Count == 20)`; `Mc("syncs sors-moi les patates du tonneau", McOrder.Fetch, r => r.Goods == McGoods.Food)` (check the goods word table first; use a word it has); `Mc("syncs apporte-moi du bois du coffre", McOrder.Fetch, r => r.Goods == McGoods.Wood)`;
  `Mc("syncs range le pain dans le coffre", McOrder.Store)`; `Mc("syncs mets tout dans le coffre", McOrder.Store)`; `Mc("syncs va chercher du fer", McOrder.Collect)`; `Order("je vais chercher du pain dans le coffre syncs", …) != Fetch`; `Order("syncs ramène pas le fer du coffre", McOrder.None)`.
  Help: `new("syncs ramène-moi du pain du coffre", McOrder.Fetch)`.
- [ ] **Step 2: Run checks** — fail.
- [ ] **Step 3: Parser** — `FetchVerbs` (« ramene », « ramene moi », « va chercher », « vas chercher », « sors moi », « apporte », « apporte moi », « amene moi », « recupere », « bring », « fetch »; « file moi » only if not already a give verb). Read at McCommand.cs just before `StorePhrases` (l.354): `chest && Find(wordsOnly, FetchVerbs) is { } f` → `Unless(f, wordsOnly, new(McOrder.Fetch, Goods: GoodsAfter(wordsOnly, f.Start + f.Length)?.Goods, Count: counts.FirstOrDefault(n => n > 0)))`. Goods null → answered at once with `McFetchUnknownLines` in `AnswerAtOnce`.
- [ ] **Step 4: Service** — command `McOrder.Fetch => new { op = "fetch", player, goods = …ToLowerInvariant(), count = request.Count }`; detail = `McGoodsNames[goods]`; errand resume list becomes `Give or Store or Fetch or Craft`. Events: `fetching` → `Ack(player, Confirm(player).Whispered, "fetch", McFetchAckLines)`; `fetched` → `OnOutcome` with Done (twins via `Kin`) or Short when `asked > 0 && count < asked`; `cantFetch` → `OnOutcome` with None / NoChest. `McDoing` arms: « elle va chercher quelque chose dans un coffre{who} » / « je vais chercher un truc dans le coffre pour … ». Count `McStats.Orders` through `Confirm` as usual.
- [ ] **Step 5: Body** — `fetchFromChests`: `Goods[key]` or return; target visible else `cantReach notVisible`; containers (`chest`, `barrel`, `trapped_chest`) by `bot.findBlocks` within 16 of her **and** with `point: target.position` within 16 of the asker, `count: 8` each, deduplicated, sorted by distance to her; none → `cantFetch noChest` (before `stay()`). Then `back = pausedActivity(task)`, `stay()`, `mine = { kind: 'fetch', player }`, `emit('fetching')`; per chest: `goto GoalNear 2`, `openContainer`, `withdraw(type, null, take)` per matching type until `want` (count or 64) is met, catch = pockets full → stop; `close` in `finally`; `task !== mine` → return. Taken nothing → `cantFetch none`; else `handOver(player, taken)`, `emit('fetched', { player, goods, count: takenTotal, asked: count, delivered })`. `setHandedAt(Date.now())`, `setTask(null)`, `resumeActivity(back)` as `give` does.
- [ ] **Step 6: Server scenario** (`tools/mc-pathtest/orders.js`, entries `fetch`, `fetchNone`): chest at the yard's `at(0,0,10)` filled with 20 bread via `data merge`; `body.send({op:'fetch', player:'Papa', goods:'food', count:0})`; pass when Papa carries or has lying near him 20 bread; `fetchNone` with an empty chest expects `cantFetch` `why: 'none'`.
- [ ] **Step 7: Run checks, build, `node --check`** — exit 0.

### Task 5: Craft on demand

**Files:**
- Create: `ProjectSYNCS/Helpers/Minecraft/McCraft.cs`, `ProjectSYNCS/MinecraftBody/lib/craftorder.js`
- Modify: `lib/commands.js` (case `craft`, resume argument), `lib/orders.js` (`OrderOps` += `craft`), `lib/resume.js` (`Restartable` += `craft`; `resumeFor` carries `table`), `index.js` header
- Modify: `McCommand.cs` (enum, `McRequest.Item`, reading), `McCommand.Words.cs` (`CraftVerbs`, `CraftMakeVerbs`), `McDoing.cs`, `McHelp.cs`, `McItems.cs` (missing names: hoes, shears, ladder, torch, furnace, crafting table, chest, planks, sticks, bow, fishing rod — whatever `McItems.Name` lacks), `MinecraftBodyService.Orders.cs`, `.Events.cs`, `.Outcomes.cs`, `.AtOnce.cs`, `BotResponses.Minecraft.cs`
- Test: `tests/ProjectSYNCS.Checks/Program.cs`

**Interfaces:**
- Produces (C#): `McRequest` gains `string? Item = null` (appended parameter). `McCraft`: `public sealed record McCraftable(string Key, string[] Words, int Yield, bool Tool, int Max);` `public static readonly McCraftable[] All;` `public static (string Item, int Count)? Read(string[] words, int from, int[] counts)` — `Item` is a Minecraft id (`iron_pickaxe`, `torch`) or a bare tool key (`pickaxe`, `axe`, `shovel`, `hoe`, `sword`) when no material is named; materials « bois »→`wooden`, « pierre »→`stone`, « fer »→`iron`, « or »→`golden`, « diamant »→`diamond`. Count: tools default 1, max 4; others default `Yield` (torch 4, planks 4, stick 4, ladder 3, else 1), max 64.
- Produces (body): op `{"op":"craft","player","item","count"}` (+ `table` on a restart); events `crafting {player,item,count}`, `crafted {player,item,count,delivered}`, `cantCraft {player,item,why: missing|noWood|noStone|unknown|failed, missing?}` (`missing` ∈ iron|gold|diamond|coal|string).
- Produces (pools): `McCraftAckLines` ({1} item), `McCraftDoneLines` + twins ({1} item; twins don't use {0}), `McCraftMissingLines` ({1} material), `McCraftNoWoodLines`, `McCraftNoStoneLines`, `McCraftUnknownLines`, `McCraftFailedLines`; `BotResponses.McCraftMaterialNames` (`iron` → « du fer », `gold` → « de l'or », `diamond` → « des diamants », `coal` → « du charbon », `string` → « de la ficelle »), covered by a check over the body's `missing` values.

- [ ] **Step 1: Failing cases**
  `Mc("syncs fabrique-moi une pioche en fer", McOrder.Craft, r => r.Item == "iron_pickaxe" && r.Count == 1)`; `Mc("syncs fais-moi 16 torches", McOrder.Craft, r => r.Item == "torch" && r.Count == 16)`; `Mc("syncs crafte une table", McOrder.Craft, r => r.Item == "crafting_table")`; `Mc("syncs fais-moi une épée", McOrder.Craft, r => r.Item == "sword")`; `Mc("syncs craft un coffre", McOrder.Craft, r => r.Item == "chest")` (not Store);
  `Order("syncs fais-moi peur", …) != Craft`, « fais-moi rire », « fais-moi confiance »; `Mc("syncs va chercher du fer", McOrder.Collect)` unchanged; `Mc("syncs fabrique-moi un truc", McOrder.Craft, r => r.Item is null)` (answered at once); `Order("je fabrique une pioche syncs", …) != Craft`.
  Help: `new("syncs fabrique-moi une pioche en pierre", McOrder.Craft)`.
- [ ] **Step 2: Run checks** — fail.
- [ ] **Step 3: Parser** — read right after the Sleep/Guard loop (McCommand.cs ~331), before the `chest` flag: `CraftVerbs` (« fabrique », « fabrique moi », « crafte », « crafte moi », « craft », « crafter », « tu peux me fabriquer ») take any item or none (Item null); `CraftMakeVerbs` (« fais moi », « fait moi », « tu peux me faire ») only when `McCraft.Read` finds an item within the next 3 words (an article or a number may sit between). `Unless` applies (refusal, talk about oneself).
- [ ] **Step 4: Service** — command `McOrder.Craft => new { op = "craft", player, item = request.Item, count = request.Count }`; `Item is null` → `McCraftUnknownLines` in `AnswerAtOnce`. Detail = the item's `McItems` name (bare tool → its word). Events: `crafting` → `Ack(… "craft", McCraftAckLines, itemName)`; `crafted` → `OnOutcome` Done (twins); `cantCraft` → `OnOutcome` with the matching pool. `McDoing` arms: « elle fabrique quelque chose{who} » / « je fabrique un truc pour … ».
- [ ] **Step 5: Body `craftorder.js`** — `export async function craftFor(player, item, count, resume = null)`:
  1. Resolve a bare tool to the best material she can afford, `diamond > iron > golden > stone > wooden`, counting what she carries; stone and wood count as affordable when `gatherCandidates('stone').open` / `treesNear()` have some.
  2. A recipe table in the module (`Recipes`: per item, units of material + sticks + extras, and `yield`): pickaxe 3+2, axe 3+2, shovel 1+2, hoe 2+2, sword 2+1, bow 3 sticks + 3 string, fishing_rod 3 sticks + 2 string, shears 2 iron, planks ← 1 log (×4), stick ← 2 planks (×4), crafting_table 4 planks, chest 8 planks, furnace 8 cobblestone, torch 1 coal-or-charcoal + 1 stick (×4), ladder 7 sticks (×3). Crafts = `ceil(count / yield)`.
  3. Before `stay()`: player visible; anything short other than logs/planks/sticks/cobblestone → `cantCraft missing` (first missing material); unknown item → `cantCraft unknown`.
  4. `back = pausedActivity(task)`, `stay()`, `mine = { kind: 'craft', player, before: resume?.before || itemCounts(), table: resume?.table || null }`, `emit('crafting')` unless resuming.
  5. Short of logs: `fellTrees(treesNear(), logsShort, mine)` (none → `cantCraft noWood`). Short of cobblestone: `craftOne('wooden_pickaxe', table)` if she has no pickaxe, then `gatherRounds('stone', short, mine)` (none → `cantCraft noStone`). Then `craftPlanks`, sticks with `craftOne('stick')`.
  6. Table: a `crafting_table` block within 16 (`findBlocks`); else `craftOne('crafting_table')` and place it on firm ground beside her (an adjacent air cell over a solid top, `equip` + `placeBlock`, nobody standing there), remembering `mine.table`. Walk within 3 of it, `craftOne(item, tableBlock)` × crafts.
  7. Take her own table back (`bot.dig` + `pickUp` of `crafting_table`, 8 s), then `handOver(player, Map{itemType → count crafted})`. The wooden pickaxe and anything else made along the way stay hers.
  8. `emit('crafted', { player, item, count, delivered })`, `setTask(null)`, `resumeActivity(back)`. A craft that fails 3 times → `cantCraft failed`.
  Restartable: `resumeFor` adds `table: previous.table` and `before` for `craft` (like camp's extra fields); `commands.js` passes `{ table, before }` as `resume`. Interrupted by another order, her table stays where it is with a log line (the tree pillar's rule; see Task 6 doc note).
- [ ] **Step 6: Server scenarios** (`orders.js`: `craftTable` with a crafting table set at `at(3,0,3)`, give her 3 iron + 2 sticks, order `iron_pickaxe` → Papa gets one, no block left placed; `craftOwnTable`, no table near, give her 4 oak logs, order `stick` count 8 → Papa gets ≥ 8 sticks and no crafting table stands in the yard; `craftMissing` order `iron_pickaxe` with no iron → `cantCraft missing` `iron`).
- [ ] **Step 7: Run checks, build, `node --check`** — exit 0.

### Task 6: Split `minecraft.md`, document, release

**Files:**
- Create: `docs/agents/minecraft-orders.md`, `docs/agents/minecraft-body.md`
- Modify: `docs/agents/minecraft.md`, `CLAUDE.md` (table), `ProjectSYNCS/config.yaml`, `ProjectSYNCS/CHANGELOG.md`, `README.md` (Minecraft orders), `docs/superpowers/specs/2026-10-27-minecraft-orders-stats-design.md` (the table-left-standing note)

- [ ] **Step 1: Split** per the spec's §6, content moved verbatim:
  - `minecraft.md`: *The body's code*, *Two processes, one voice*, *When she joins and leaves*, *Talk*, *`/minecraft`*, *Failures*, *Her remarks* (with *Family in game*), *Deployment* (version workarounds included) minus its *Testing movement locally* paragraph.
  - `minecraft-orders.md`: *Orders*, *Free time*.
  - `minecraft-body.md`: *Water*, *Fights*, *Body language*, *Arriving*, *Testing movement locally*.
  - Each file opens with one line naming the other two; `minecraft.md` keeps a pointer where *Testing movement locally* was.
- [ ] **Step 2: New text** — in `minecraft-orders.md`: « tu fais quoi ? » (the `doing` event, precedence order → own life → nothing, whispered places), *Free time* order, *Fetch*, *Craft* (incl. the table exception, and that an order taking her away mid-craft leaves her table standing with a log line), the `/minecraft stats` note in `minecraft.md` that daily buckets now exist for `/report minecraft`; « She never digs and never places a block » list gains the craft table.
- [ ] **Step 3: CLAUDE.md** — the Minecraft row becomes three rows (her body/service/`/minecraft` → `minecraft.md`; orders → `minecraft-orders.md`; movement, water, fights → `minecraft-body.md`); the `/minecraft` subcommand count unchanged (7).
- [ ] **Step 4: Release** — `version: "7.45.0"`; CHANGELOG entry (« tu fais quoi ? » in game; free-time order; fetch from a chest; craft on demand; `/report minecraft` and Minecraft in the owner's monthly digest).
- [ ] **Step 5: Verify** — `dotnet build` (no warnings as errors), checks exit 0, `dotnet run --project tests/ProjectSYNCS.Checks -- --mc-review` shows the new pools as game lines; `npm ci` in `ProjectSYNCS/MinecraftBody`, `node --check` on every body file, `node tools/mc-pathtest/fight-check.js` and `column-check.js` pass. Server scenarios: run only if the owner gives the local server's folder; otherwise report them as not run.
