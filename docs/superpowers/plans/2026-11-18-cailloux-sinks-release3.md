# Cailloux sinks, release 3 — the jar and the weekly lottery — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Two last sinks for everyone: `/jar`, a shared jar whose goals unlock a reward for the whole
server, and `/arcade lottery`, a weekly draw where the house burns a quarter of the pot.

**Architecture:** Pure rules in `Helpers/Economy/Jar.cs` and `Helpers/Arcade/Lottery.cs`; transient
`JarService` / `LotteryService`, each unit of work through `EconomySave.RetryAsync`. A gift only fills
the jar. Each goal it reaches leaves a reward pending (`Level > RewardsDone`), and
`JarService.ResolveNextRewardAsync` carries them out one at a time, each in its own save. A failure
leaves the reward pending for the next gift, never lost. The lottery draws from a new
`LotteryDrawService` loop modelled on `GiveawayDrawService`; a unique index on the draw row makes a
draw happen once.

**Tech Stack:** C# .NET 10, Discord.Net 3.20, EF Core + SQLite.

**Spec:** `docs/superpowers/specs/2026-11-18-cailloux-sinks-design.md`, section « Release 3 ».

## Global Constraints

- Work in a git worktree on a branch `cailloux-release3` cut from `main` (`630c08c` or later).
  Never in the main checkout. The worktree compiles the checks: `dotnet run --project tests/ProjectSYNCS.Checks`.
- No `git commit` until the owner asks (memory rule).
- Command and option names English; every user-facing string French; code, comments, logs English.
- Files are CRLF: edit with Edit or `perl` using `\r\n`, never `sed -i`.
- Stored keys are append-only. These new metric keys go at the end of `EconomyLog`'s lists:
  - `spend.jar`, `spend.lottery`;
  - `earn.lottery`, `earn.giveaway`.

  Their dashboard labels in `AdminCards`: « bocal », « loterie » (spend), « loterie », « giveaways » (earn).
- Any economy write goes through `EconomySave.RetryAsync`; helpers that take a context never save.
- Mécène (`Patronage.AddAsync`) counts the jar, **not** the lottery (spec: it would reward gambling).
- Mentions:
  - Replies naming people send with `AllowedMentions.None`.
  - The lottery winner's announcement pings that user only (`new AllowedMentions { UserIds = { winner } }`).
  - Never roles or everyone.
- Never `DateTime.Now`; Paris time through `AppTime`; DateTimeOffset comparisons in memory, not SQL.
- Her lines come from pools only. Read `docs/syncs-voice.md` before writing any line. Voice rules:
  - Never gender the person addressed.
  - The sparkle is `{Emotes.Sparkle}`, never ✨.
  - Never (｡•́︿•̀｡).
  - Pick a weekly pool through `DailyRotation`, a rare one through `ResponsePicker`.
- Where the draw is announced: the bots channel, through `BotsChannel.ResolveAsync`. It ignores
  silence mode, like giveaway draws (a notice a command set up). The jar's unlocks go to the main
  channel and ignore silence too: a gift set them off.
- `/arcade` goes from 10 to 11 subcommands. Update the count in `CLAUDE.md`. `/jar` is a new top-level group.
- Minor bump: `ProjectSYNCS/config.yaml` 7.70.0 → **7.71.0** (one migration), with a
  `## 7.71.0 — <date>` entry in `ProjectSYNCS/CHANGELOG.md`, English, no member names.
- `/help` stays under 6000 characters per page: measure after editing.

**Calls made in this plan (the spec left them open):**
- **Jar goals:** `GoalFor(level) = min(1500 + 500 × level, 5000)`, where `level` is the number of
  goals reached so far (1500, 2000, … then 5000 for good).
  - Overflow carries into the next goal, so one gift can reach several.
  - A gift is 10 to 100 000.
- **The reward order** follows the spec, `RewardFor(level) = level % 4`: double `/daily`, quiz day,
  feast, giveaway.
- **Timed rewards (double `/daily`, quiz day) apply from tomorrow, Paris time.**
  - Today's quiz slots are already drawn, and a `/daily` already claimed today couldn't be doubled.
  - Each kind keeps a range of days (`From`, `Until`). A second unlock while one is pending adds the
    day after `Until`, so none is lost.
  - Double `/daily`: the rolled amount ×2 (the item chance is unchanged).
  - Quiz day: every window keeps its slot (3 questions, each still waiting for someone in the channel).
- **Feast:** at once. Every living, unfrozen Plynling of the guild eats a truffle
  (`PlynlingLife.Feed` with `PlynlingFood.Truffle`), free. No log, no journal moment.
- **Giveaway:**
  - At once: a `Giveaway` row with `OrganizerId` = the bot, in the announcement channel.
  - 3 winners × 150 cailloux, ends after 24 h.
  - Prize text « 150 cailloux chacun ».
  - The cailloux are paid at the draw, in the draw's save, through a new `Giveaway.PebblePrize`
    column (0 for members' giveaways, which change nothing).
- **Where an unlock is announced:** the guild's `/config main-channel` if set. Otherwise
  `MorningGreetingService.DefaultChannelId` when it belongs to the guild. Otherwise the channel
  `/jar give` was used in.
- **Jar commands:** `/jar give amount` and `/jar status`. Discord can't have a group that is also a
  command, so the spec's bare `/jar` becomes `/jar status`, as `/shoutout status` did.
- **Top contributors:** all-time per guild (`JarGift` totals), top 5, plus your own total.
- **Lottery tickets:**
  - 20 cailloux each, at most 500 per purchase, unlimited in total.
  - **Debited at purchase**, logged `spend.lottery`; the winner's 75 % is logged `earn.lottery` at
    the draw.
  - It does **not** settle through `ArcadeService.SettleAsync`, despite the spec: that path writes
    `earn.arcade` / `spend.arcade` and holds nothing in escrow, and a ticket must stay bought until
    the draw.
- **The weekly draw:**
  - Sunday 21:00 Paris. A ticket bought on Sunday after 21:00 is for the next Sunday.
  - Rows are keyed by the draw's day key (`DrawDay`, yyyymmdd), not by ISO week.
  - Her ticket is the last of `total + 1`. If it wins, nothing is paid and the whole pot is burned.
  - A week with no ticket bought draws nothing and says nothing.
- `/arcade lottery [tickets]`:
  - Without `tickets`: the status (pot, your tickets, your odds, the draw's time), ephemeral.
  - With `tickets`: buys them, public line, `AllowedMentions.None`.

## Review Focus

1. **Two gifts racing past the same goal:** the goal is counted once, the reward resolved once, and
   the sum of the wallets plus the jar's `Given` never changes.
2. **One gift reaching two goals:** both rewards resolve in order, and two timed rewards of the same
   kind make two days.
3. **A ticket bought at Sunday 20:59 vs 21:00 Paris, DST weekends included:** it goes to this draw
   vs the next.
4. **The loop running the same draw twice (crash, two passes):** paid once, announced at most once.
5. **A giveaway created by the jar, drawn while the winners' wallets are being written** (a `/work`
   at the same moment): each winner paid exactly once.

---

### Task 1: Schema, the jar's rules, gifts, the double `/daily` and the quiz day

**Files:**
- Create:
  - `ProjectSYNCS/Helpers/Economy/Jar.cs`
  - `ProjectSYNCS/Models/Economy/CaillouxJar.cs` (also holds `JarGift`)
  - `ProjectSYNCS/Models/Arcade/Lottery.cs` (`LotteryEntry`, `LotteryDraw`)
  - `ProjectSYNCS/Services/Economy/JarService.cs`
  - migration `AddJarAndLottery`
- Modify:
  - `ProjectSYNCS/Models/Scheduling/Giveaway.cs` (`long PebblePrize`)
  - `ProjectSYNCS/Data/AppDbContext.cs`
  - `ProjectSYNCS/Helpers/Economy/EconomyLog.cs`
  - `ProjectSYNCS/Commands/Admin/AdminCards.cs`
  - `ProjectSYNCS/Services/Economy/PebbleService.cs` (`ClaimDailyOnceAsync`, `DailyResult`)
  - `ProjectSYNCS/Commands/Economy/EconomyModule.cs` (`DailyReply`)
  - `ProjectSYNCS/Helpers/Quiz/QuizSchedule.cs`
  - `ProjectSYNCS/Services/Quiz/QuizMasterService.cs` (`PostIfDueAsync`, `PlanFor`)
  - `ProjectSYNCS/Program.cs` (DI)
- Test: `tests/ProjectSYNCS.Checks/JarLotteryChecks.cs` (new, section 54), `tests/ProjectSYNCS.Checks/Program.cs`

**Interfaces:**
- Produces:
  - `CaillouxJar { int Id; ulong GuildId; int Level; long Filled; long Given; int RewardsDone; int DoubleDailyFrom; int DoubleDailyUntil; int QuizDayFrom; int QuizDayUntil }`
    - Unique index on `GuildId`.
    - `Filled` and `RewardsDone` are concurrency tokens.
    - Day fields are day keys, 0 = none.
  - `JarGift { int Id; ulong GuildId; ulong UserId; long Total }`, unique (GuildId, UserId).
  - `LotteryEntry { int Id; ulong GuildId; int DrawDay; ulong UserId; int Tickets }`, unique (GuildId, DrawDay, UserId).
  - `LotteryDraw { int Id; ulong GuildId; int DrawDay; ulong WinnerId; long Pot; long Paid; int Tickets; DateTimeOffset DrawnAt }`
    - Unique (GuildId, DrawDay).
    - `WinnerId` 0 = her ticket won.
  - `Giveaway.PebblePrize` (default 0).
  - All snowflakes `.HasConversion<long>()`. DbSets: `CaillouxJars`, `JarGifts`, `LotteryEntries`, `LotteryDraws`.
  - The release's one migration holds all four tables and the column.
  - `EconomyLog.SpendJar = "spend.jar"`, `SpendLottery = "spend.lottery"`,
    `EarnLottery = "earn.lottery"`, `EarnGiveaway = "earn.giveaway"`.
  - `enum JarReward { DoubleDaily, QuizDay, Feast, Giveaway }` (not stored). `Jar`:
    - Constants: `MinGive = 10`, `MaxGive = 100_000`, `GiveawayWinners = 3`, `GiveawayPrize = 150`,
      `GiveawayFor = 24 h`, `FeastFood = PlynlingFood.Truffle`.
    - `static long GoalFor(int level)`, `static JarReward RewardFor(int level)`.
    - `static (int Level, long Filled) Fill(int level, long filled, long amount)`.
    - `static int NextDay(int dayKey)`.
    - `static (int From, int Until) Extend(int from, int until, int tomorrow)`: when
      `until < tomorrow`, gives `(tomorrow, tomorrow)`; otherwise `(from, NextDay(until))`.
    - `static bool Covers(int from, int until, int day)`.
  - `enum JarGiveOutcome { Given, TooPoor }`;
    `record JarGiveResult(JarGiveOutcome Outcome, long Amount, long Balance, int Reached, JarStatus Jar)`.
  - `record JarStatus(int Level, long Filled, long Goal, JarReward Next, IReadOnlyList<(ulong UserId, long Total)> Top, long Mine, int DoubleDailyFrom, int DoubleDailyUntil, int QuizDayFrom, int QuizDayUntil)`.
  - `JarService(AppDbContext db)`:
    - `Task<JarGiveResult> GiveAsync(ulong guildId, ulong userId, long amount, DateTimeOffset now)`:
      one `RetryAsync` unit, one save. It:
      - debits the wallet;
      - finds or creates the jar (`Local` first), applies `Fill` and adds to `Given`;
      - upserts the `JarGift`;
      - logs `spend.jar` and calls `Patronage.AddAsync`.

      `Reached` is the new level minus the old one.
    - `Task<JarStatus> StatusAsync(ulong guildId, ulong userId, DateTimeOffset now)`.
    - `static Task<bool> IsDoubleDailyAsync(AppDbContext db, ulong guildId, int day)` and
      `Task<bool> IsQuizDayAsync(ulong guildId, int day)`, no tracking.
  - `DailyResult` gains `bool Doubled`. `ClaimDailyOnceAsync` doubles the rolled amount when
    `JarService.IsDoubleDailyAsync(db, guildId, today)`. `DailyReply` adds the line
    « 🫙 Journée double, grâce au bocal ! » when doubled.
  - `QuizSchedule.DrawSlots(DateTimeOffset now, Random random, bool everyWindow = false)`: with
    `everyWindow`, each window keeps a slot regardless of `SlotChance`.
  - `QuizMasterService.PostIfDueAsync`:
    1. Before taking `_gate`, when `_plans` holds no plan for today, read `IsQuizDayAsync` in its own
       scope. A failure is logged and means false.
    2. Pass the flag to `PlanFor(guildId, now, quizDay)`, which hands it to `DrawSlots`.

- [ ] **Step 1: Write the failing checks in `JarLotteryChecks.cs`.**
  - Setup: the same `Db` helper shape as `BribeChecks`. Call `await JarLotteryChecks.Run(Fail);`
    from `Program.cs` after section 53.
  - `GoalFor`: `GoalFor(0) == 1500`, `GoalFor(1) == 2000`, `GoalFor(7) == 5000`, `GoalFor(50) == 5000`.
  - `RewardFor`: `RewardFor(0) == DoubleDaily`, `RewardFor(3) == Giveaway`, `RewardFor(4) == DoubleDaily`.
  - `Fill`:
    - `Fill(0, 1400, 100) == (1, 0)`;
    - `Fill(0, 0, 3600) == (2, 100)` (1500 + 2000 reached, 100 carried);
    - `Fill(0, 0, 10) == (0, 10)`.
  - `Extend`:
    - `Extend(0, 0, 20261119) == (20261119, 20261119)`;
    - `Extend(20261119, 20261119, 20261119) == (20261119, 20261120)`;
    - `Extend(20261110, 20261112, 20261119) == (20261119, 20261119)`.
  - `NextDay(20261130) == 20261201`, `NextDay(20261231) == 20270101`.
  - Gifts, wallets A 2000 and B 2000:
    - A gives 1000 → `Given`, `Reached` 0, A 1000.
    - B gives 600 → `Reached` 1, jar `Level` 1, `Filled` 100.
    - A gives 5000 → `TooPoor`, nothing moved.
    - Then: `spend.jar` today 1600; A's `CaillouxSpent` 1000; `JarGift` totals A 1000, B 600.
    - `StatusAsync` for A: `Top` = [(A, 1000), (B, 600)], `Mine` 1000, `Goal` 2000, `Next == QuizDay`.
    - Invariant after each step: A + B + `Given` == 4000.
  - Race: two contexts on the same connection.
    1. Context `c` loads the jar first (a stale tracked copy).
    2. A gives 1400 through another context.
    3. B gives 200 through `c`: B's save hits the `Filled` token, retries, and lands on top.
    4. Result: `Level` 1, `Filled` 100.
  - Double `/daily`: set the jar's `DoubleDailyFrom = DoubleDailyUntil` = today's day key.
    - Two `ClaimDailyAsync` calls for fresh users with `new Random(7)`: on the doubled guild the
      amount is twice the amount on a guild without a jar, and `Doubled`.
    - The next day (no range) → not doubled.
  - Quiz day: `DrawSlots(08:00 Paris, new Random(1), everyWindow: true)` holds 3 slots, one per window.
  - Without the flag, a `Random` subclass whose `NextDouble` always returns 0.5 (≥ `SlotChance`)
    gives 0 slots; with the flag, the same `Random` gives 3.
- [ ] **Step 2: Run, expect compile errors.** `dotnet run --project tests/ProjectSYNCS.Checks`:
  `Jar`, `JarService`, `CaillouxJar` missing.
- [ ] **Step 3: Implement the Interfaces above.**
  1. Register `JarService` as transient.
  2. Run `dotnet ef migrations add AddJarAndLottery` from `ProjectSYNCS/`.
  3. Check that its `Up` holds exactly the four tables with their unique indexes and the
     `Giveaways.PebblePrize` column (default 0).
- [ ] **Step 4: Run** `dotnet run --project tests/ProjectSYNCS.Checks` → `ALL CHECKS PASSED`, and
  `dotnet ef migrations has-pending-model-changes` → no changes.

### Task 2: Resolving the jar's rewards, and giveaways that pay

**Files:**
- Modify:
  - `ProjectSYNCS/Services/Economy/JarService.cs`
  - `ProjectSYNCS/Services/Scheduling/GiveawayService.cs` (`CreateAsync`, `TryDrawAsync`)
- Test: `tests/ProjectSYNCS.Checks/JarLotteryChecks.cs`

**Interfaces:**
- Consumes: Task 1's `CaillouxJar`, `Jar`, `Giveaway.PebblePrize`, `EconomyLog.EarnGiveaway`.
- Produces:
  - `record JarUnlock(JarReward Reward, int Level, int Day, int Fed, Giveaway? Giveaway)`. `Day` is
    the bonus day for a timed reward, 0 otherwise. `Fed` is how many Plynlings ate.
  - `Task<JarUnlock?> JarService.ResolveNextRewardAsync(ulong guildId, ulong botId, ulong channelId, DateTimeOffset now)`:
    one `RetryAsync` unit, one save. Steps:
    1. Re-read the jar. If `RewardsDone >= Level`, return null.
    2. Carry out `RewardFor(RewardsDone)` and increment `RewardsDone`:
       - Double `/daily` or quiz day: `Extend` its range with tomorrow's day key.
       - Feast: load the guild's Plynlings with `DiedAt == null && FrozenAt == null`, then
         `PlynlingLife.Feed(p, PlynlingCatalog.Info(Jar.FeastFood), now)` on each.
       - Giveaway: add a `Giveaway` row in the same save, with `OrganizerId = botId`,
         `ChannelId = channelId`, `Prize = "150 cailloux chacun"`,
         `WinnerCount = Jar.GiveawayWinners`, `PebblePrize = Jar.GiveawayPrize`, `EndsAt = now + 24 h`.

    A concurrent resolver hits the `RewardsDone` token, retries, re-reads, and gets the next pending
    reward or null.
  - `GiveawayService.TryDrawAsync` runs through `EconomySave.RetryAsync`. When `PebblePrize > 0`, it
    credits each winner's wallet (`PebbleService.GetOrCreateWalletAsync`) and logs `earn.giveaway`
    (prize × winners) in the draw's save. A retry redraws; only the saved run counts.

- [ ] **Step 1: Write the failing checks:**
  - Setup: a jar at `Level` 4, `RewardsDone` 0. Call `ResolveNextRewardAsync` five times at
    2026-11-18 12:00 UTC.
  - Rewards: DoubleDaily (`Day` 20261119), QuizDay (20261119), Feast, Giveaway, then null.
  - Jar afterwards: `DoubleDailyFrom == DoubleDailyUntil == 20261119`, the same for the quiz day,
    `RewardsDone` 4.
  - Two timed rewards of one kind: jar `Level` 5 from `RewardsDone` 4 with `DoubleDailyUntil` already
    20261119 → the unlock's `Day` is 20261120, and the range is 20261119–20261120.
  - Feast, with three Plynlings (one living at hunger 10, one dead, one frozen):
    - `Fed == 1`;
    - the living one's hunger rose;
    - the dead and frozen ones are untouched.
  - Giveaway: the row exists with `PebblePrize` 150, `OrganizerId` the bot id passed, `EndsAt` now + 24 h.
  - Race: two contexts both resolve with the jar at `Level` 1, `RewardsDone` 0 → exactly one gets a
    `JarUnlock`, the other null; `RewardsDone` 1.
  - Paid draw:
    - Setup: a giveaway with `PebblePrize` 150, 2 winners, 3 entrants with wallets at 0.
    - After `TryDrawAsync`: the two winners at 150, the loser at 0, `earn.giveaway` 300.
    - A second `TryDrawAsync` returns null and pays nothing.
    - A giveaway with `PebblePrize` 0 moves no wallet.
- [ ] **Step 2: Run, expect compile errors.**
- [ ] **Step 3: Implement** the Interfaces above.
- [ ] **Step 4: Run** the checks → `ALL CHECKS PASSED`.

### Task 3: `/jar`, its card and the unlock announcements

**Files:**
- Create:
  - `ProjectSYNCS/Commands/Economy/JarModule.cs`
  - `ProjectSYNCS/Services/Personality/BotResponses/BotResponses.Jar.cs`
- Modify: `ProjectSYNCS/Services/Personality/BotResponses/BotResponses.cs` (the pool index comment)
- Test: `tests/ProjectSYNCS.Checks/JarLotteryChecks.cs`

**Interfaces:**
- Consumes:
  - Task 1's `JarService.GiveAsync` / `StatusAsync`, `JarStatus`, `JarGiveResult`.
  - Task 2's `ResolveNextRewardAsync`, `JarUnlock`.
  - `GiveawayModule.BuildEmbed` / `BuildComponents`, `GiveawayService.SetMessageLocationAsync`.
- Produces:
  - The group: `[Group("jar", "Le bocal à cailloux du serveur")]`, `[CommandContextType(InteractionContextType.Guild)]`.
  - `/jar status` (« Voir le bocal, son objectif et ses plus grands donateurs »): ephemeral, the card.
  - `/jar give amount` (« Verser des cailloux dans le bocal »), `[MinValue(Jar.MinGive)]`, `[MaxValue(Jar.MaxGive)]`:
    1. Defer, then `GiveAsync`.
    2. `TooPoor` → delete the deferred reply and follow up ephemerally (as `/pay` does), naming the balance.
    3. `Given` → a public reply (`AllowedMentions.None`) with the amount and the card.
    4. Then loop `ResolveNextRewardAsync` until null, each unlock in its own scope. A throw is logged
       and stops the loop: the rest stays pending for the next gift.
  - Each unlock posts one line in the announcement channel (resolved as in *Calls made*), through
    `BotChat.PostWithTypingAsync` with `AllowedMentions.None`. A giveaway also posts its card there
    and calls `SetMessageLocationAsync`. Discord failures are swallowed and logged.
  - `static Embed JarModule.BuildCard(JarStatus s, DateTimeOffset now)`:
    - Title « 🫙 Le bocal ».
    - A 10-segment progress bar with `Filled / Goal`.
    - « Prochaine récompense : » plus `RewardTitle(s.Next)`.
    - Any pending or current bonus day (`Covers`, or `From` > today), as a `<t:…:D>` date.
    - The top 5 as mention pills, and « Ton total : N ».
  - `static string JarModule.RewardTitle(JarReward r)`: « Double `/daily` pour une journée »,
    « Journée quiz », « Festin pour tous les Plynlings », « Un giveaway de cailloux ».
  - `static string JarModule.UnlockLine(JarUnlock u, ulong giverId, ResponsePicker picker)`.
  - Pools in `BotResponses.Jar.cs`:
    - `JarDoubleDailyLines`, `JarQuizDayLines`: `{0}` the giver's mention, `{1}` the date as `<t:…:D>`.
    - `JarFeastLines`: `{0}` the giver, `{1}` how many ate.
    - `JarGiveawayLines`: `{0}` the giver.
    - At least 6 lines each. Her voice: she runs the jar and is a little proud of what it buys.

- [ ] **Step 1: Write the failing checks:**
  - `BuildCard`:
    - For `Filled` 500 / `Goal` 2000 with two donors: the description holds « 500 » and « 2000 »,
      both pills, and `RewardTitle(QuizDay)`.
    - Total length under 6000; no field over 1024.
  - `UnlockLine` for each reward contains the giver's `<@id>`. The feast line holds the count; the
    timed lines hold `<t:`.
  - Pools:
    - Each pool has at least 6 lines and no duplicate.
    - Every line formats with its arguments without throwing.
    - No ✨, no (｡•́︿•̀｡).
    - No gendered address: reuse `BribeChecks`' gendered regex, made `internal` if needed.
- [ ] **Step 2: Run, expect compile errors / FAILs.**
- [ ] **Step 3: Implement** the Interfaces above, the lines after reading `docs/syncs-voice.md`.
- [ ] **Step 4: Run** the checks → `ALL CHECKS PASSED`; `dotnet build ProjectSYNCS -warnaserror` clean.

### Task 4: The lottery's rules and service

**Files:**
- Create: `ProjectSYNCS/Helpers/Arcade/Lottery.cs`, `ProjectSYNCS/Services/Arcade/LotteryService.cs`
- Modify: `ProjectSYNCS/Program.cs` (DI)
- Test: `tests/ProjectSYNCS.Checks/JarLotteryChecks.cs`

**Interfaces:**
- Consumes: Task 1's `LotteryEntry`, `LotteryDraw`, `EconomyLog.SpendLottery` / `EarnLottery`.
- Produces:
  - `Lottery` constants: `TicketPrice = 20`, `MaxPerBuy = 500`, `DrawTime = 21:00`.
  - `static long Lottery.Payout(long pot) => pot * 3 / 4`.
  - `static int Lottery.DrawDay(DateTimeOffset now)`: the day key of the Sunday whose 21:00 Paris
    is the next draw strictly after `now`.
  - `static DateTimeOffset Lottery.DrawAt(int drawDay)`.
  - `static int Lottery.Pick(IReadOnlyList<int> tickets, int roll)`:
    - `roll` is in `[0, sum + 1)`;
    - returns the index whose range holds `roll`;
    - returns −1 when `roll == sum` (her ticket).
  - `enum LotteryBuyOutcome { Bought, TooPoor }`.
  - `record LotteryStatus(int DrawDay, DateTimeOffset DrawAt, long Pot, int Tickets, int Mine, int Players)`.
  - `record LotteryBuy(LotteryBuyOutcome Outcome, int Tickets, long Cost, long Balance, LotteryStatus Status)`.
  - `record LotteryResult(int DrawDay, ulong? Winner, long Pot, long Paid, int Tickets)`.
  - `LotteryService(AppDbContext db)`:
    - `Task<LotteryBuy> BuyAsync(ulong guildId, ulong userId, int tickets, DateTimeOffset now)`: one
      `RetryAsync` unit. Debit `tickets × 20`, upsert the entry for `DrawDay(now)`, log
      `spend.lottery`, one save. No Patronage.
    - `Task<LotteryStatus> StatusAsync(ulong guildId, ulong userId, DateTimeOffset now)`.
    - `Task<List<(ulong GuildId, int DrawDay)>> DueAsync(DateTimeOffset now)`: the distinct
      (guild, draw day) pairs among the entries, with no `LotteryDraw` row, whose `DrawAt <= now`
      (filtered in memory).
    - `Task<LotteryResult?> DrawAsync(ulong guildId, int drawDay, Random rng, DateTimeOffset now)`:
      one `RetryAsync` unit. Steps:
      1. Return null when the draw row exists.
      2. Load the entries ordered by `UserId`, then roll `rng.Next(sum + 1)` and `Pick`.
      3. If a player won: credit `Payout(pot)` and log `earn.lottery`.
      4. Insert the `LotteryDraw` row and save.

      A concurrent draw hits the unique index, retries, finds the row, and returns null.

- [ ] **Step 1: Write the failing checks:**
  - `DrawDay` (2026-11-22 is a Sunday):
    - `DrawDay(2026-11-22 19:59 UTC)` (20:59 Paris) == `20261122`;
    - `DrawDay(2026-11-22 20:00 UTC)` == `20261129`;
    - `DrawDay(2026-11-18 12:00 UTC)` == `20261122`.
    - Summer: `DrawDay(2026-07-05 18:59 UTC)` == `20260705`, `19:00 UTC` == `20260712`.
    - The DST Sunday: `DrawDay(2026-10-25 19:59 UTC)` == `20261025`.
  - `DrawAt(20261122) == 2026-11-22 20:00 UTC`.
  - `Pick([3, 2], 0) == 0`, `Pick([3, 2], 2) == 0`, `Pick([3, 2], 3) == 1`, `Pick([3, 2], 5) == -1`.
  - `Payout(100) == 75`, `Payout(20) == 15`.
  - Buy, wallets A 1000 and B 100:
    - A buys 10 → A 800, `spend.lottery` 200.
    - A buys 5 more → one entry with 15 tickets.
    - B buys 6 → `TooPoor`, nothing moved.
    - `StatusAsync` for A: `Pot` 300, `Mine` 15, `Players` 1.
    - A's `CaillouxSpent` is unchanged (no Patronage).
  - Draw:
    - Setup: entries A 15 and B 5 for `20261122`, plus one for `20261129`.
    - `DueAsync(2026-11-22 20:01 UTC)` lists only (guild, 20261122).
    - A seeded `Random` whose first `Next(21)` lands under 15 → A wins, A +300, `earn.lottery` 300
      (the pot is 400).
    - A second `DrawAsync` → null, no second payment.
    - `DueAsync` no longer lists it.
  - Her ticket: a `Random` subclass returning 20 for `Next(21)` → `Winner` null, `Paid` 0, no
    wallet moved, no `earn.lottery`.
- [ ] **Step 2: Run, expect compile errors.**
- [ ] **Step 3: Implement** the Interfaces above; register `LotteryService` as transient.
- [ ] **Step 4: Run** the checks → `ALL CHECKS PASSED`.

### Task 5: `/arcade lottery` and the weekly draw

**Files:**
- Create:
  - `ProjectSYNCS/Services/Arcade/LotteryDrawService.cs`
  - `ProjectSYNCS/Services/Personality/BotResponses/BotResponses.Lottery.cs`
- Modify:
  - `ProjectSYNCS/Commands/Arcade/ArcadeModule.cs` (the subcommand, next to `duel`/`heist`)
  - `ProjectSYNCS/Program.cs` (hosted service)
  - `ProjectSYNCS/Services/Personality/BotResponses/BotResponses.cs` (the pool index comment)
- Test: `tests/ProjectSYNCS.Checks/JarLotteryChecks.cs`

**Interfaces:**
- Consumes: Task 4's `LotteryService`, `Lottery`, `LotteryStatus`, `LotteryBuy`, `LotteryResult`.
- Produces:
  - `/arcade lottery [tickets]` (« La loterie de la semaine : 20 cailloux le ticket, tirage dimanche à 21 h »):
    - `tickets` is optional, `[MinValue(1)]`, `[MaxValue(Lottery.MaxPerBuy)]`.
    - Without it: the status, ephemeral.
    - With it: `BuyAsync`. `TooPoor` is ephemeral, naming the cost and the balance. Bought is public:
      « 🎟️ <@u> prend N ticket(s) · cagnotte : **P** · tirage <t:…:R> », `AllowedMentions.None`.
  - `static string ArcadeModule.LotteryStatusText(LotteryStatus s)`:
    - The pot, your tickets and odds (`Mine / (Tickets + 1)` as a percentage, 1 decimal, `fr-FR`), and
      the draw's time as `<t:…:F>`.
    - With no tickets: « Personne n'a encore joué cette semaine ».
  - `static string ArcadeModule.LotteryBuyReply(LotteryBuy b, ulong userId)`.
  - `LotteryDrawService : BackgroundService`:
    - Its own `CheckInterval = 1 min`; skips while disconnected. Built like `GiveawayDrawService`:
      a scope per pass, a per-item `try`.
    - For each `DueAsync` pair: `DrawAsync(…, Random.Shared, now)`. Null → nothing to say.
    - Otherwise post in `BotsChannel.ResolveAsync(client, config, announcer, guildId)`:
      - A player won: `string.Format(DailyRotation.Pick(LotteryWinLines, drawDay, WinSalt), "<@id>", payout)`,
        with mentions narrowed to the winner.
      - Her ticket won: `LotteryGloatLines` formatted with the pot, `AllowedMentions.None`.
    - Through `BotChat.PostWithTypingAsync`; it does **not** check `SilenceService`.
  - Salts `WinSalt = 0x5359_4E43_534C_5457` ("SYNCSLTW"), `GloatSalt = 0x5359_4E43_534C_5447` ("SYNCSLTG").
  - Pools in `BotResponses.Lottery.cs`:
    - `LotteryWinLines`: `{0}` the winner's mention, `{1}` the payout.
    - `LotteryGloatLines`: `{0}` the burned pot. She won her own lottery and burns it, smug.
    - At least 8 lines each.

- [ ] **Step 1: Write the failing checks:**
  - `LotteryStatusText`:
    - Pot 300, 15 of 15 tickets → contains « 93,8 % ».
    - No tickets → « Personne n'a encore joué ».
  - `LotteryBuyReply` for `Bought` holds the pot and `<t:`; for `TooPoor` the cost and the balance.
  - Pools: at least 8 lines each, no duplicate, each formats with its arguments, and the
    `{Emotes.Sparkle}` / kaomoji / gendered rules as in Task 3.
  - `/arcade` count: reflect over `ArcadeModule` (both partial files) and count `[SlashCommand]` on
    it → 11, under 25.
- [ ] **Step 2: Run, expect compile errors / FAILs.**
- [ ] **Step 3: Implement** the Interfaces above. Register `LotteryDrawService` with `AddHostedService`.
- [ ] **Step 4: Run** the checks → `ALL CHECKS PASSED`; `dotnet build ProjectSYNCS -warnaserror` clean.

### Task 6: Docs, help, version

**Files:**
- Modify:
  - `ProjectSYNCS/Commands/Core/HelpModule.cs`, `README.md`, `CLAUDE.md` (`/arcade` 10 → 11)
  - `docs/agents/economy.md`, `docs/agents/arcade.md`, `docs/agents/architecture.md`
  - `docs/agents/quiz.md`, `docs/agents/scheduling.md`
  - `ProjectSYNCS/config.yaml`, `ProjectSYNCS/CHANGELOG.md`

- [ ] **Step 1:** Add `/jar` and `/arcade lottery` lines to the `/help` economy and arcade pages,
  and README rows and bullets for both.
- [ ] **Step 2:** Update the notes:
  - economy.md: the jar's goals, the order of rewards, pending rewards resolved after each gift,
    the four metric keys, the jar counted toward Mécène and the lottery not.
  - arcade.md: the lottery (tickets debited at purchase, Sunday 21:00, her ticket, 75 / 25, the
    bots channel, why not `SettleAsync`).
  - architecture.md: `LotteryDrawService (1 min)` in the loop list; seventeen loops.
  - quiz.md: the quiz day keeps every window.
  - scheduling.md: `Giveaway.PebblePrize`, paid in the draw's save.
- [ ] **Step 3:** Bump `config.yaml` 7.70.0 → 7.71.0 (unless it already differs from `HEAD`) and add
  the CHANGELOG entry: `/jar`, `/arcade lottery`, giveaways that can pay cailloux.
- [ ] **Step 4: Verify:**
  - `dotnet build ProjectSYNCS -warnaserror` clean.
  - Checks `ALL CHECKS PASSED`.
  - Each `/help` page measured ≤ 6000.

## Verification (dev guild, after Task 6)

- **Jar:**
  - `/jar give` from two accounts past the first goal: the public reply, then the unlock line in the
    main channel naming the giver.
  - `/jar status` shows the bonus day.
  - The next day `/daily` pays double with the jar line.
  - Lower a goal by hand in the database to reach the feast and the giveaway quickly: the
    Plynlings' hunger rises; the giveaway card appears; once drawn (set `EndsAt` close), each winner
    is paid 150.
- **Lottery:**
  - `/arcade lottery` alone (status), then with tickets from two accounts.
  - Set a `DrawDay` in the past by hand: within a minute the bots channel announces the winner,
    pinged alone, and the wallet grows by 75 % of the pot.
- `/admin dashboard` shows « bocal », « loterie » and « giveaways ».
