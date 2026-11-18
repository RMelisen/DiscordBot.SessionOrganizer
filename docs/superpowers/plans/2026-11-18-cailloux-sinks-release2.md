# Cailloux sinks, release 2 — bribes and the dedication auction — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Two more ways to spend cailloux for everyone: `/bribe` (24 h as her « chouchou », or one
`/shame` hit erased) and `/shoutout`, an auction for a dedication in tomorrow's morning hello.

**Architecture:** Pure rules in `Helpers/Economy/Bribes.cs` and `Helpers/Economy/Shoutouts.cs`;
transient `BribeService` / `ShoutoutService` doing one unit of work each through
`EconomySave.RetryAsync`; a singleton `ChouchouBook` caches who is chouchou so the gateway paths
(`ChatterService`, `ReactionService`) never read the database per message. The dedication rides
`MorningGreetingService.PostHelloAsync`, the one path both the slot and the early hello take, and is
settled (logged, counted toward « Mécène ») only once it has been posted.

**Tech Stack:** C# .NET 10, Discord.Net 3.20, EF Core + SQLite.

**Spec:** `docs/superpowers/specs/2026-11-18-cailloux-sinks-design.md`, section « Release 2 ».

## Global Constraints

- Work in a git worktree on a branch `cailloux-release2` cut from `main` (`d87503c` or later), never in
  the main checkout: the main checkout holds the unfinished story-events batch, and a migration
  generated there would embed its schema (release 1 had to be split for exactly this). In the
  worktree the stock checks project compiles: `dotnet run --project tests/ProjectSYNCS.Checks`.
- No `git commit`: the owner decides at the end (memory rule). Leave the work in the worktree.
- Command and option names English; every user-facing string French; code, comments, logs English.
- Files are CRLF: edit with Edit or `perl` using `\r\n`, never `sed -i`.
- Stored keys are append-only: new metric keys `spend.bribe` and `spend.shoutout` go at the end of
  `EconomyLog`'s lists; `AnswerKinds.Chouchou = "chouchou"` is new.
- Any economy write goes through `EconomySave.RetryAsync`; helpers that take a context never save.
- Components V2 / replies naming people send with `AllowedMentions.None`; the dedication line pings
  the winner only (`new AllowedMentions { UserIds = { winner } }`), never roles or everyone.
- Never `DateTime.Now`; Paris time through `AppTime`; DateTimeOffset comparisons in memory, not SQL.
- She speaks lines from pools only, through `DailyRotation` for once-a-day pools; read
  `docs/syncs-voice.md` and `docs/agents/personality/people.md` before writing any line. Chouchou
  lines are openly transactional and **never family** (no papa, tata, famille, ma fille, mon enfant).
- Minor bump: `ProjectSYNCS/config.yaml` 7.68.0 → **7.69.0** (one migration), with a
  `## 7.69.0 — <date>` entry in `ProjectSYNCS/CHANGELOG.md`, English, no member names.
- `/help` stays under 6000 characters per page: measure after editing.

**Calls made in this plan (the spec left them open):**
- Bribe price: 100, doubling with every bribe of either kind in the same Paris ISO week
  (`CosmeticCatalog.WeekKey`): 100 → 200 → 400…; the exponent is capped at 40 so it never overflows.
- Chouchou lasts 24 h from purchase; buying again while active extends from the current end.
  Papa and Tata are refused (« déjà de la famille »): their tiers stay as they are.
- A mean message from a chouchou is still roasted, like Tata's: warm, not a doormat.
- Erasing a hit decrements `ShameRecord` and **the most recent `ShameDailyStat` bucket holding a hit
  in that category** (not today's, as the spec says): today's bucket is usually empty, and the
  default 30-day wall would not move, so the payer would see nothing for their money. Old hits from
  before the buckets existed decrement the record only. Nothing to erase → refused, not charged.
- Auction: opening bid 50, each raise at least +10, bids close 20:00 Paris. Before 20:00 a bid is for
  tomorrow's hello, from 20:00 for the day after. A bid must beat the leader, so ties cannot happen
  (the spec's « tie → earliest » holds by construction). Raising your own lead charges the
  difference. The leader's cailloux are held (debited, not logged); an outbid leader is refunded in
  the same save. The winning amount is logged `spend.shoutout` and counted toward « Mécène » when the
  dedication is posted. Home guild only (`HomeGuild.Id`): the hello is only ever posted there.
- Chouchou reactions: a chouchou's non-mean messages draw `ChouchouReactions` under the usual
  `ReactChance` roll and channel cooldown.

## Review Focus

1. Two bids for the same day landing together: exactly one leads, the other is retried and re-checked
   against the new leader; the sum of every wallet plus the held bid never changes.
2. A bid at 19:59 vs 20:00 Paris, including the DST weekends: the target day flips at 20:00 local.
3. Days without a hello (silence, her birthday, the bot down): every won dedication still posts, one
   per morning, oldest day first; none is lost or posted twice.
4. Erasing a hit when only pre-bucket hits exist (record > 0, no bucket) or none at all (refused,
   not charged).
5. The bribe price across Sunday 23:30 → Monday 00:30 Paris: back to 100.

---

### Task 1: Bribes — schema, price, chouchou, erasing a shame hit (services)

**Files:**
- Create: `ProjectSYNCS/Helpers/Economy/Bribes.cs`, `ProjectSYNCS/Helpers/Shame/ShameCategories.cs`,
  `ProjectSYNCS/Services/Economy/BribeService.cs`, `ProjectSYNCS/Services/Economy/ChouchouBook.cs`,
  `ProjectSYNCS/Models/Economy/ShoutoutBid.cs`, migration `AddBribesAndShoutouts`
- Modify: `ProjectSYNCS/Models/Economy/PebbleWallet.cs`, `ProjectSYNCS/Data/AppDbContext.cs`,
  `ProjectSYNCS/Services/Shame/ShameService.cs`, `ProjectSYNCS/Helpers/Economy/EconomyLog.cs`,
  `ProjectSYNCS/Commands/Admin/AdminCards.cs`, `ProjectSYNCS/Program.cs` (DI)
- Test: `tests/ProjectSYNCS.Checks/BribeChecks.cs` (new, section 53), `tests/ProjectSYNCS.Checks/Program.cs`

**Interfaces:**
- Produces:
  - `PebbleWallet`: `DateTimeOffset? ChouchouUntil`, `int BribeWeek`, `int BribesThisWeek`.
  - `ShoutoutBid { int Id; ulong GuildId; int ForDay; ulong UserId; long Amount; DateTimeOffset PlacedAt; int ShownDay }`
    — snowflakes `.HasConversion<long>()`, unique index (GuildId, ForDay), `Amount` a concurrency
    token, `ShownDay` 0 until posted. `DbSet<ShoutoutBid> ShoutoutBids`. The one migration of the
    release holds the three wallet columns and this table.
  - `Bribes.BasePrice = 100`, `Bribes.FavouriteFor = 24 h`,
    `static long Bribes.Price(int bribeWeek, int bribesThisWeek, int thisWeek)`.
  - `enum ShameCategory { Mean, Perfidy, Shout, Abandon }` (not stored) and
    `static string ShameCategories.Title(ShameCategory)` → « Le Malfaisant », « Le Perfide »,
    « L'Hystérique », « L'Indigne ».
  - `static Task<bool> ShameService.ForgiveOneAsync(AppDbContext db, ulong guildId, ulong userId, ShameCategory category)`:
    false when the record holds 0 in that category; otherwise `ExecuteUpdateAsync` on the record
    (`col = col - 1 WHERE col > 0`) and on the most recent bucket with `col > 0`, if any. It runs SQL
    immediately, so it must be called inside the caller's transaction.
  - `EconomyLog.SpendBribe = "spend.bribe"`, `EconomyLog.SpendShoutout = "spend.shoutout"`, appended to
    `Spendings`; dashboard labels « pots-de-vin », « dédicaces ».
  - `enum BribeOutcome { Done, TooPoor, Family, NothingToForgive }`,
    `record BribeResult(BribeOutcome Outcome, long Price, long Balance, long NextPrice, DateTimeOffset? FavouriteUntil)`.
  - `BribeService(AppDbContext db, ChouchouBook book)`:
    `Task<BribeResult> BuyFavouriteAsync(ulong guildId, ulong userId, DateTimeOffset now)` and
    `Task<BribeResult> ForgiveShameAsync(ulong guildId, ulong userId, ShameCategory category, DateTimeOffset now)`.
    Each: one `RetryAsync` unit inside `BeginTransactionAsync` … `CommitAsync`; debit, stamp
    `BribeWeek`/`BribesThisWeek`, `EconomyLog.AddAsync(SpendBribe)`, `Patronage.AddAsync`, one save.
    `BuyFavouriteAsync` calls `book.Set` after the commit.
  - `ChouchouBook(IServiceProvider services)` (singleton):
    `Task<bool> IsFavouriteAsync(ulong guildId, ulong userId, DateTimeOffset now)` (loads every wallet
    with a `ChouchouUntil` once, lazily, behind a `SemaphoreSlim`, then answers from memory) and
    `void Set(ulong guildId, ulong userId, DateTimeOffset until)`.

- [ ] **Step 1: Write the failing checks in `BribeChecks.cs`** (same `Db` helper shape as
  `EconomySinkChecks`; call `await BribeChecks.Run(Fail);` from `Program.cs` after section 52):
  - `Bribes.Price(w, 0, w) == 100`, `Price(w, 2, w) == 400`, `Price(w - 1, 5, w) == 100`, `Price(w, 99, w) > 0`.
  - Week rollover: two `BuyFavouriteAsync` at 2026-11-22 22:30 UTC (Sunday 23:30 Paris) then
    2026-11-22 23:30 UTC (Monday 00:30 Paris) both cost 100.
  - Same week: 100 then 200 then (a forgive) 400; balance 1000 → 300; `spend.bribe` today = 700;
    `MemberXp.CaillouxSpent == 700`.
  - Chouchou: `FavouriteUntil == now + 24 h`; a second buy 2 h later → `now + 48 h`; Papa
    (`AvailabilityService.OwnerId`) and `BotResponses.TataId` → `Family`, balance unchanged.
  - `ChouchouBook` over a `ServiceCollection` with `AddDbContext` on the same in-memory connection: a
    fresh book answers true for a wallet whose `ChouchouUntil` is in the future, false when past, and
    true right after `Set`.
  - Forgive: record `MeanHits` 3 with buckets 10 days ago (2) and 3 days ago (1) → after one forgive,
    record 2, the 3-days-ago bucket 0, the 10-days-ago bucket 2.
  - Pre-bucket only: record `ShoutHits` 1, no bucket → record 0, charged.
  - Nothing: record `PerfidyHits` 0 → `NothingToForgive`, balance unchanged, no `spend.bribe`.
  - Too poor at 50 → `TooPoor`, nothing moved, the shame record untouched.
- [ ] **Step 2: Run, expect compile errors** — `dotnet run --project tests/ProjectSYNCS.Checks`:
  `Bribes`, `BribeService`, `ShameCategory` missing.
- [ ] **Step 3: Implement the Interfaces above**; register `ChouchouBook` singleton and
  `BribeService` transient in `Program.cs`; then `dotnet ef migrations add AddBribesAndShoutouts`
  from `ProjectSYNCS/` and check its `Up` holds exactly the three `PebbleWallets` columns and the
  `ShoutoutBids` table with its unique index.
- [ ] **Step 4: Run** `dotnet run --project tests/ProjectSYNCS.Checks` → `ALL CHECKS PASSED`, and
  `dotnet ef migrations has-pending-model-changes` → no changes.

### Task 2: `/bribe` and the chouchou's warmth

**Files:**
- Create: `ProjectSYNCS/Commands/Economy/BribeModule.cs`,
  `ProjectSYNCS/Services/Personality/BotResponses/BotResponses.Bribes.cs`
- Modify: `ProjectSYNCS/Services/Personality/ChatterService/ChatterService.cs` (mention branch ~line
  512, reply branch ~line 674, constructor), `ProjectSYNCS/Services/Personality/ReactionService.cs`
  (`ChooseReactionPool`, constructor), `ProjectSYNCS/Helpers/Personality/AnswerKinds.cs`,
  `ProjectSYNCS/Services/Personality/BotResponses/BotResponses.cs` (the pool index comment)
- Test: `tests/ProjectSYNCS.Checks/BribeChecks.cs`, `tests/ProjectSYNCS.Checks/Program.cs` (section 1 list)

**Interfaces:**
- Consumes: Task 1's `BribeService`, `BribeResult`, `ShameCategory`, `ShameCategories.Title`, `ChouchouBook`.
- Produces:
  - `/bribe favourite` (« Devenir son chouchou pendant 24 h ») and `/bribe shame category` (« Effacer
    un de tes points de honte »), `[Group("bribe", …)]`, `[CommandContextType(InteractionContextType.Guild)]`,
    `category` a `ShameCategory` with `[ChoiceDisplay]` from `ShameCategories.Title`. Defers, then
    one reply: success public (`AllowedMentions.None`), refusals ephemeral (delete the deferred reply
    and follow up, as `/pay` does).
  - `static string BribeModule.Reply(BribeResult result, ulong userId, ShameCategory? category)`: success
    names what was bought, the price and the next price this week; `TooPoor` names the price and the
    balance; `Family` and `NothingToForgive` their own lines.
  - Pools: `BotResponses.ChouchouGreetings` (mention: summoned), `BotResponses.ChouchouReplies`
    (reply: talked to), at least 12 lines each, same placeholder rules as `TataGreetings` /
    `TataReplies`; `BotResponses.ChouchouReactions` (unicode emoji, at least 5, e.g. 💸 🤑 🪙).
  - `AnswerKinds.Chouchou = "chouchou"`, added to `IsUnrecognised` beside `Tata` (a stand-in, like hers).
  - `public static string[]? ReactionService.ChooseReactionPool(string content, ulong authorId, bool chouchou)`:
    owner → `OwnerReactions`; mean → `MeanReactions`; chouchou → `ChouchouReactions`; otherwise as today.
  - ChatterService: `var chouchou = !mean && await _chouchous.IsFavouriteAsync(guildId, authorId, now)`
    computed once per message before each chain; a new `else if (chouchou)` right **after** the Tata
    branch on both paths, picking `ChouchouGreetings` (mention) / `ChouchouReplies` (reply) with
    `AnswerKinds.Chouchou`. DMs (no guild) are never chouchou.

- [ ] **Step 1: Write the failing checks:**
  - `ChooseReactionPool("salut", 77, chouchou: true) == ChouchouReactions`;
    `ChooseReactionPool("t'es nul", 77, true) == MeanReactions`;
    `ChooseReactionPool("salut", OwnerId, true) == OwnerReactions`; `("salut", 77, false) == GreetingReactions`.
  - `BribeModule.Reply` for each outcome: success favourite contains the price and « 24 h »; forgive
    contains `ShameCategories.Title(Mean)`; `TooPoor` contains the price and the balance.
  - Pools: add `"ChouchouGreetings", "ChouchouReplies"` to section 1's `taggedPools`; in `BribeChecks`,
    every chouchou line is free of `papa`, `tata`, `famille`, `ma fille`, `mon enfant` (case-insensitive),
    each pool ≥ 12 lines, no duplicate line, `ChouchouReactions` all parse with `EmoteMarkup.Parse`.
- [ ] **Step 2: Run, expect compile errors / FAILs** on the missing members.
- [ ] **Step 3: Implement** the Interfaces above. Write the lines after reading `docs/syncs-voice.md`
  and `personality/people.md`: she is warm because she was paid and says so, smug about it.
- [ ] **Step 4: Run** the checks → `ALL CHECKS PASSED`; `dotnet build ProjectSYNCS -warnaserror` clean.

### Task 3: The dedication auction — `/shoutout`

**Files:**
- Create: `ProjectSYNCS/Helpers/Economy/Shoutouts.cs`, `ProjectSYNCS/Services/Economy/ShoutoutService.cs`,
  `ProjectSYNCS/Commands/Economy/ShoutoutModule.cs`
- Modify: `ProjectSYNCS/Program.cs` (DI)
- Test: `tests/ProjectSYNCS.Checks/BribeChecks.cs`

**Interfaces:**
- Consumes: Task 1's `ShoutoutBid`, `EconomyLog.SpendShoutout`, `Patronage.AddAsync`.
- Produces:
  - `Shoutouts.CloseAt = 20:00`, `MinOpening = 50`, `MinRaise = 10`, `MaxBid = 1_000_000`;
    `static int Shoutouts.OpenDay(DateTimeOffset now)` (Paris time before 20:00 → tomorrow's day key,
    else the day after); `static DateTimeOffset Shoutouts.ClosesAt(int forDay)` (20:00 Paris the day
    before `forDay`); `static long Shoutouts.Minimum(long? leading)`.
  - `enum BidOutcome { Leading, TooLow, TooPoor }`,
    `record BidResult(BidOutcome Outcome, int ForDay, long Amount, long Minimum, long Balance, ulong? Outbid)`,
    `record ShoutoutStatus(int ForDay, DateTimeOffset ClosesAt, ulong? Leader, long Amount, long Minimum, int Queued)`.
  - `ShoutoutService(AppDbContext db)`:
    `Task<BidResult> BidAsync(ulong guildId, ulong userId, long amount, DateTimeOffset now)` (one
    `RetryAsync` unit: debit the bidder `amount` — or `amount − current` when raising their own lead —
    refund an outbid leader, overwrite the row, one save; no log, no Patronage: the bid is held);
    `Task<ShoutoutStatus> StatusAsync(ulong guildId, DateTimeOffset now)` (`Queued` = won, unshown
    bids with `ForDay` ≤ today);
    `Task<ShoutoutBid?> NextDueAsync(ulong guildId, DateTimeOffset now)` (unshown, `ForDay` ≤ today,
    lowest `ForDay`);
    `Task MarkShownAsync(int bidId, DateTimeOffset now)` (sets `ShownDay`, logs `spend.shoutout`,
    `Patronage` for the winner, one save; a no-op on a bid already shown).
  - `/shoutout bid amount` (« Enchérir pour une dédicace dans son bonjour de demain »,
    `[MinValue(MinOpening)]`, `[MaxValue(MaxBid)]`) and `/shoutout status`; refused outside
    `HomeGuild.Id` (ephemeral, before deferring). A lead is public: « 📣 <@u> mène les enchères pour
    la dédicace du {date} : **X** » plus « <@prev> récupère ses N cailloux » when someone was
    outbid, `AllowedMentions.None`; refusals ephemeral and name the minimum or the balance.
    `static string ShoutoutModule.BidReply(BidResult r, ulong userId)` and
    `static string ShoutoutModule.StatusText(ShoutoutStatus s)` carry the wording.

- [ ] **Step 1: Write the failing checks:**
  - `OpenDay(2026-11-18 18:59 UTC)` (19:59 Paris) == `20261119`; `OpenDay(2026-11-18 19:00 UTC)` == `20261120`;
    summer: `OpenDay(2026-07-01 17:59 UTC)` == `20260702`, `18:00 UTC` == `20260703`;
    `ClosesAt(20261119) == 2026-11-18 19:00 UTC`.
  - Bids, wallets A 1000, B 1000: A 49 → `TooLow` (min 50); A 100 → `Leading`, A 900; B 105 →
    `TooLow` (min 110); B 150 → `Leading`, `Outbid == A`, A back to 1000, B 850; B raises to 200 →
    charged 50, B 800; A 5000 → `TooPoor`. Invariant: A + B + leading amount == 2000 after each step.
  - Race: with B leading at 200, load the row into context `c` first (a stale tracked copy), then A
    bids 250 through another context, then C bids 260 through `c` → C's save hits the `Amount`
    token, retries, re-checks against 250 and leads; A refunded; total unchanged.
  - Due order: bids for D-2 (unshown), D-1 (unshown), D (unshown), D+1 → `NextDueAsync` at D returns
    D-2; after `MarkShownAsync`, D-1; `MarkShownAsync` twice logs `spend.shoutout` once; the winner's
    `CaillouxSpent` grows by the bid.
  - `BidReply` names the minimum on `TooLow`, the outbid pill on a lead; `StatusText` with no bid says
    the auction is open from `MinOpening`.
- [ ] **Step 2: Run, expect compile errors.**
- [ ] **Step 3: Implement** the Interfaces above; register `ShoutoutService` transient.
- [ ] **Step 4: Run** the checks → `ALL CHECKS PASSED`.

### Task 4: The dedication in the morning hello

**Files:**
- Modify: `ProjectSYNCS/Helpers/Personality/MorningGreeting.cs`,
  `ProjectSYNCS/Services/Personality/MorningGreetingService.cs`,
  `ProjectSYNCS/Services/Personality/BotResponses/BotResponses.Bribes.cs`
- Test: `tests/ProjectSYNCS.Checks/BribeChecks.cs`, `tests/ProjectSYNCS.Checks/Program.cs` (section 1 list)

**Interfaces:**
- Consumes: Task 3's `ShoutoutService.NextDueAsync` / `MarkShownAsync`.
- Produces:
  - `BotResponses.DedicationLines`: at least 10 morning lines, each with exactly one `{0}` (the
    winner's mention), each a hello in its own right (it replaces the classic one), saying openly that
    it was bought.
  - The salts move from the service to `MorningGreeting` as `public const ulong GreetingSalt`,
    `FunFactSalt` (same values, so today's rotation is unchanged), plus `DedicationSalt`.
  - `static (string Text, AllowedMentions Mentions, bool UsesDedication) MorningGreeting.ComposeHello(DateTimeOffset now, ulong? dedicatedTo)`:
    birthday → the birthday line alone, `AllowedMentions.None`, false (the dedication waits);
    `dedicatedTo` set → `DedicationLines` (rotation by day) formatted with `<@id>`, a newline, the fun
    fact, mentions narrowed to that user, true; otherwise today's classic line and fun fact, `None`, false.
  - `static bool MorningGreeting.IsDedicationLine(string firstLine)`: matches a `DedicationLines`
    template with `{0}` as `<@\d+>`.
  - `MorningGreetingService`: takes `IServiceProvider`; `IsMorningHello` and
    `AlreadyGreetedTodayAsync` also accept a dedication first line; `PostHelloAsync`, after the
    silence and already-greeted checks, reads `NextDueAsync(HomeGuild.Id, now)` in its own scope
    (failure logged → classic hello), composes, posts, and only when the post returned a message
    and `UsesDedication` calls `MarkShownAsync` (failure logged). Silence returns before any of this,
    so a silenced day carries the dedication too.

- [ ] **Step 1: Write the failing checks:**
  - `ComposeHello(2026-11-19 08:00 UTC, 42)`: first line `IsDedicationLine`, contains `<@42>`, second
    line is a `MorningFunFacts` line, `Mentions.UserIds` is exactly `{42}`, `UsesDedication`.
  - `ComposeHello(her birthday at 08:00, 42)` → `UsesDedication == false`, text is the birthday line.
  - `ComposeHello(2026-11-19 08:00 UTC, null)` → the same text the classic rotation gives that day.
  - `IsDedicationLine` is false for every `MorningGreetings` line and true for every formatted
    `DedicationLines` line; no `DedicationLines` template contains `{1}`; add `"DedicationLines"` to
    section 1's pools if its rules fit a `{0}` mention, else check it here.
- [ ] **Step 2: Run, expect compile errors.**
- [ ] **Step 3: Implement** the Interfaces above.
- [ ] **Step 4: Run** the checks → `ALL CHECKS PASSED`; `dotnet build ProjectSYNCS -warnaserror` clean.

### Task 5: Docs, help, version

**Files:**
- Modify: `ProjectSYNCS/Commands/Core/HelpModule.cs`, `README.md`, `docs/agents/economy.md`,
  `docs/agents/shame.md`, `docs/agents/personality/people.md`, `docs/agents/personality/ambient.md`,
  `docs/agents/personality/reactions.md`, `ProjectSYNCS/config.yaml`, `ProjectSYNCS/CHANGELOG.md`

- [ ] **Step 1:** `/help` economy page: `/bribe` and `/shoutout` lines; README rows and bullets for both.
- [ ] **Step 2:** Notes: economy.md (bribe price rule, the held bid and when it is logged, the two
  metric keys, `ChouchouBook`); shame.md (a bribe erases a hit: record + most recent bucket, ban
  votes excluded); people.md (the chouchou tier below Tata, not family, mean still roasted, pools);
  ambient.md (the dedication replaces the hello line, carries over birthday and silence, recognised
  by `IsMorningHello`); reactions.md (`ChouchouReactions`).
- [ ] **Step 3:** `config.yaml` 7.68.0 → 7.69.0 (unless it already differs from `HEAD`) and the
  CHANGELOG entry: `/bribe favourite`, `/bribe shame`, `/shoutout`.
- [ ] **Step 4: Verify:** `dotnet build ProjectSYNCS -warnaserror` clean; checks `ALL CHECKS PASSED`;
  the `/help` page length check in the checks (or a measured print) stays ≤ 6000.

## Verification (dev guild, after Task 5)

- `/bribe favourite` twice (100 then 200), then mention and reply to her: warm lines; a mean reply is
  still roasted; her reactions on your messages come from `ChouchouReactions`.
- `/bribe shame` on a category you have a hit in (the wall moves), and one you have none in (refused).
- `/shoutout bid` from two accounts (outbid refund), `/shoutout status`. The morning post itself only
  happens in the home guild: check it there the next morning, and that a reply to it is answered as a
  reply to the hello.
- `/admin dashboard` shows « pots-de-vin » and « dédicaces ».
