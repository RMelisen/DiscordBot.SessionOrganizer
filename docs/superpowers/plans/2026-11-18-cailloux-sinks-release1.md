# Cailloux sinks — Release 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give members without a Plynling places to spend cailloux, and make spending a status: a per-member « cailloux dépensés » counter, the « Mécène » badge, a « Mécènes » leaderboard tab, `/pay` with a burned tax, and collection booster packs in `/inventory shop`.

**Architecture:** The spent counter rides the existing totals + buckets pair (`MemberXp` / `MemberDailyStat`), written by a never-saving helper beside each counted `EconomyLog` spend, so badge, tab and windows read what is already there. `/pay` and packs are new economy actions following the `PebbleService` / `InventoryService` pattern (`EconomySave.RetryAsync`, one save, `EconomyLog` metric).

**Tech Stack:** C# / .NET 10, Discord.Net 3.20, EF Core + SQLite.

**Spec:** `docs/superpowers/specs/2026-11-18-cailloux-sinks-design.md` (Release 1 section).

## Global Constraints

- Command and option names English; every user-facing string French; code, comments, logs English.
- Metric keys, badge keys and enum-by-int values are stored: **append-only**.
- Never `DateTime.Now`; days via `AppTime.DayKey(now)`; SQLite can't compare `DateTimeOffset` in SQL.
- Every wallet/item write goes through `EconomySave.RetryAsync` (work may re-run: DB work only, all reads inside); helpers called inside take the context and **never save**.
- Counted toward « dépensés »: shop, meals (own and others'), medicine, cosmetics, crafting, packs, the `/pay` tax. **Not**: arcade, admin debits, the `/pay` amount itself.
- Mécène thresholds: 500 / 2 500 / 10 000 / 25 000 / 60 000.
- `/pay`: tax 10 % rounded up, paid by the sender on top of the amount and burned; minimum 10; cap 2 000 sent per Paris day per sender; refuse self and bots.
- Pack: 3 collectibles from `ItemCatalog.DrawCollectible` (no food); starting price 60, final price whatever the simulation in Task 5 allows.
- The owner commits by hand: **no `git commit` steps** — each task ends with build + checks green.
- Version: `ProjectSYNCS/config.yaml` is at `7.67.0` and unchanged from `HEAD` → minor bump to **7.68.0** once (Task 6), with its `CHANGELOG.md` entry. Re-check `git diff HEAD -- ProjectSYNCS/config.yaml` first; if it already differs, extend that entry instead.

## Review Focus

1. **A member whose only `MemberXp` row comes from spending** (no XP yet): `/level` must say « non classé », not a last-place rank — pinned in Task 1 (`GetRankAsync` returns null at `TotalXp == 0`).
2. **Spent-only daily buckets must not count as activity**: `/report`, « ça faisait longtemps » and the sleeping list filter on XP/reactions/voice and must stay that way — pinned in Task 1 (`LastActiveDayBeforeAsync` ignores a spent-only bucket).
3. **`/pay` when amount + tax exceeds the balance but the amount alone doesn't** → refused, nothing moves — pinned in Task 4.
4. **The `/pay` cap across midnight Paris**: yesterday's sends don't count today — pinned in Task 4.
5. **A pack opening that completes a set** pays the set reward in the same save, and the notice says so — pinned in Task 5.

---

### Task 1: The spent counter

**Files:**
- Modify: `ProjectSYNCS/Models/Xp/MemberXp.cs`, `ProjectSYNCS/Models/Xp/MemberDailyStat.cs` (add `public long CaillouxSpent { get; set; }`)
- Create: `ProjectSYNCS/Helpers/Economy/Patronage.cs`
- Create: migration `AddCaillouxSpent` (also carries Task 4's two wallet columns — do Task 4 Step 1's model change before generating)
- Modify: `Services/Economy/InventoryService.cs:176`, `Services/Economy/CosmeticService.cs:39,63`, `Services/Plynling/PlynlingService.cs:192,305` (call beside the spend log; payer is `userId` / `actorId`)
- Modify: `Services/Xp/XpService.cs` `GetRankAsync` (null when `row.TotalXp == 0`)
- Test: `tests/ProjectSYNCS.Checks/Program.cs`, new section `// 52. Cailloux spent (Patronage), /pay, packs` before the story sections

**Interfaces:**
- Produces: `public static Task Patronage.AddAsync(AppDbContext db, ulong guildId, ulong userId, long amount, DateTimeOffset now)` — finds or creates the `MemberXp` row and today's `MemberDailyStat` (`Local` first, like `EconomyLog.AddAsync`), adds `amount` to both `CaillouxSpent`; no-op when `amount <= 0`; never saves.

- [ ] **Step 1: Add both model properties and `Patronage.cs`; add Task 4's wallet columns (`PayDay`, `PaidToday`, see Task 4) to `PebbleWallet`.**
- [ ] **Step 2: Generate the migration** — `cd ProjectSYNCS && dotnet ef migrations add AddCaillouxSpent`. Open it: it must contain only the four new columns (`MemberXps.CaillouxSpent`, `MemberDailyStats.CaillouxSpent`, `PebbleWallets.PayDay`, `PebbleWallets.PaidToday`), all defaulting to 0. Anything else means the in-progress story-events model has an ungenerated change: stop and tell the owner.
- [ ] **Step 3: Write the failing checks** (in-memory SQLite like section « GetFactsAsync against a real SQLite », `EnsureCreated`, `new InventoryService(db, null!)` is acceptable only if the tested path never touches `_badges` — `BuyAsync` doesn't):
  - wallet 1 000, `BuyAsync(g, me, ItemCatalog.SoapKey, 5, now)` → `MemberXps.CaillouxSpent == 90` and today's `MemberDailyStats.CaillouxSpent == 90`;
  - a second buy the same day adds to the **same** rows (one `MemberXp`, one bucket);
  - `PebbleService.AdjustAsync(g, me, -100)` leaves `CaillouxSpent` unchanged;
  - `GetRankAsync` for a member whose row has `TotalXp == 0` and `CaillouxSpent > 0` → `null`;
  - `LastActiveDayBeforeAsync` with only a spent-only bucket → `null`.
- [ ] **Step 4: Run** `dotnet run --project tests/ProjectSYNCS.Checks` → the new lines FAIL (or don't compile).
- [ ] **Step 5: Wire the five call sites and the `GetRankAsync` change.**
- [ ] **Step 6: Run** `dotnet build ProjectSYNCS` then the checks → `ALL CHECKS PASSED`.

### Task 2: Mécène badge

**Files:**
- Modify: `ProjectSYNCS/Helpers/Xp/MemberBadges.cs` (key, `Palier`, `ValueOf`, facts record)
- Modify: `ProjectSYNCS/Services/Xp/MemberBadgeService.cs` (select `CaillouxSpent` with `VoiceMinutes, ReactionsUsed`)
- Test: section 52

**Interfaces:**
- Consumes: `MemberXp.CaillouxSpent` (Task 1).
- Produces: `MemberBadges.Keys.Patron = "mecene"`; `MemberBadgeFacts` gains trailing `long CaillouxSpent = 0`.

- [ ] **Step 1: Failing check:** `MemberBadges.Evaluate` with facts `CaillouxSpent: 2_500` gives the `mecene` standing `Tier == CollectionTier.Silver`, `Value == 2500`; with `499` → `CollectionTier.None`; `MemberBadges.IsAwardable(g, "mecene") == false` (derived, never stored).
- [ ] **Step 2: Run checks** → FAIL.
- [ ] **Step 3: Implement.** `Palier(Keys.Patron, "🪙", "Mécène", "Ce qu'on dépense ici ne revient pas. On s'en souvient quand même.", new long[] { 500, 2500, 10000, 25000, 60000 })` placed after `Keys.Daily`; `ValueOf`: `Keys.Patron => f.CaillouxSpent`. No icon files needed (medal fallback).
- [ ] **Step 4: Run build + checks** → PASS.

### Task 3: « Mécènes » leaderboard tab

**Files:**
- Modify: `ProjectSYNCS/Services/Xp/XpService.cs` (enum value, `MemberTally`, both tally builders)
- Modify: `ProjectSYNCS/Helpers/Xp/LevelCardUi.cs` (`RowValue`, `Title`, `ViewLabel`, `EmptyLine`, new `Spent(long)`)
- Modify: `ProjectSYNCS/Commands/Xp/LevelModule.cs` (`Footer` switch)
- Test: section 52

**Interfaces:**
- Produces: `LeaderboardView.Patrons` (appended last; the tab row iterates `Enum.GetValues`, so the button appears by itself — 5/5 buttons, card 33/40 components); `MemberTally(..., long Completions = 0, long CaillouxSpent = 0)`; `LevelCardUi.Spent(long n)` → `"{PebbleEconomy.Cailloux(n)} dépensés"`.

- [ ] **Step 1: Failing check** (in-memory SQLite): seed `MemberXp` A=900/B=300 spent, buckets today A=100, B=300, and A=800 forty days ago → `GetRankingAsync(g, Patrons, AllTime)` order A, B; `Week` order B, A; a member with 0 spent is absent. `LevelCardUi.ViewLabel(Patrons) == "Mécènes"`; `Title == "## Classement des mécènes"`.
- [ ] **Step 2: Run checks** → FAIL.
- [ ] **Step 3: Implement.** Empty all-time line: « Personne n'a encore rien dépensé. Les cailloux, ça ne se garde pas sous le matelas (ᵕ • ᴗ •) ». Footer standing: `LevelCardUi.Spent(mine.CaillouxSpent)`.
- [ ] **Step 4: Run build + checks** → PASS. Update the component sum in `docs/agents/xp.md` (33 of 40).

### Task 4: `/pay`

**Files:**
- Modify: `ProjectSYNCS/Models/Economy/PebbleWallet.cs` (`int PayDay`, `long PaidToday` — added in Task 1 Step 1)
- Modify: `ProjectSYNCS/Helpers/Economy/PebbleEconomy.cs`, `Helpers/Economy/EconomyLog.cs`, `Commands/Admin/AdminCards.cs:52`
- Modify: `ProjectSYNCS/Services/Economy/PebbleService.cs`, `ProjectSYNCS/Commands/Economy/EconomyModule.cs`
- Test: section 52

**Interfaces:**
- Produces: `PebbleEconomy.PayMin = 10`, `PayDailyCap = 2000`, `static long PayTax(long amount)` (ceil 10 %), `static long PayRoom(int payDay, long paidToday, int today)`; `EconomyLog.SpendTax = "spend.tax"` (appended to `Spendings`; dashboard label « taxe »); `enum PayOutcome { Paid, TooSmall, TooPoor, OverCap }`; `record PayResult(PayOutcome Outcome, long Amount, long Tax, long Balance, long RoomLeft)`; `Task<PayResult> PebbleService.PayAsync(ulong guildId, ulong fromId, ulong toId, long amount, DateTimeOffset now)`.

- [ ] **Step 1: Failing checks:** `PayTax(10) == 1`, `PayTax(15) == 2`, `PayTax(100) == 10`; `PayRoom(yesterday, 2000, today) == 2000`, `PayRoom(today, 1500, today) == 500`. In-memory SQLite, sender 1 000:
  - `PayAsync(…, 500)` → `Paid`, sender 450, recipient +500, `spend.tax` today 50, sender `CaillouxSpent` 50, recipient's `CaillouxSpent` 0;
  - sender 105, `PayAsync(…, 100)` → `TooPoor` (110 needed), both balances unchanged;
  - after 1 900 sent today, `PayAsync(…, 200)` → `OverCap`, `RoomLeft == 100`; the same call stamped the next Paris day → `Paid`;
  - `PayAsync(…, 9)` → `TooSmall`.
- [ ] **Step 2: Run checks** → FAIL.
- [ ] **Step 3: Implement `PayAsync`** through `EconomySave.RetryAsync`: both wallets via `GetOrCreateWalletAsync`, check order TooSmall → OverCap → TooPoor, move, stamp `PayDay`/`PaidToday`, `EconomyLog.AddAsync(SpendTax, tax)`, `Patronage.AddAsync(fromId, tax)`, one save.
- [ ] **Step 4: Implement the command** `[SlashCommand("pay", "Donner des cailloux à quelqu'un (taxe de 10 %)")] PayAsync(IUser member, [MinValue(10)][MaxValue(2000)] long amount)` in `EconomyModule`: refuse self / bots ephemerally before calling the service; defer (the save can outrun 3 s); success is public, both people as pills, `AllowedMentions.None` — « 💸 <@from> a donné **{Cailloux(amount)}** à <@to> · taxe : {Cailloux(tax)} »; refusals ephemeral (TooPoor names the total needed, OverCap names `RoomLeft`).
- [ ] **Step 5: Run build + checks** → PASS.

### Task 5: Booster packs

**Files:**
- Modify: `ProjectSYNCS/Helpers/Economy/ItemCatalog.cs` (constants), `Helpers/Economy/EconomyLog.cs`, `Commands/Admin/AdminCards.cs:52`
- Modify: `ProjectSYNCS/Services/Economy/InventoryService.cs`
- Modify: `ProjectSYNCS/Commands/Economy/ShopCards.cs`, `ProjectSYNCS/Interactions/Components/ShopComponentHandler.cs`
- Test: section 52 + the existing shop-card check near `Program.cs:1660`

**Interfaces:**
- Produces: `ItemCatalog.PackKey = "pack.collection"` (a select value, never stored as an item), `PackSize = 3`, `PackPrice` (60 until Step 1 says otherwise); `ShopCards.PackQuantities = { 1, 5 }`; `EconomyLog.SpendPack = "spend.pack"` (appended; label « sachets »); `enum PackOutcome { Opened, TooPoor, BadQuantity }`; `record PackResult(PackOutcome Outcome, long Price, long Balance, IReadOnlyList<ItemFind> Finds)`; `Task<PackResult> InventoryService.OpenPackAsync(ulong guildId, ulong userId, int quantity, DateTimeOffset now, Random rng)`; `static string ShopCards.PackNotice(PackResult result)`.

- [ ] **Step 1: Write the price simulation check:** seeded `Random(52)`, 5 000 packs spread evenly over one year of dates (so every season's items appear), one simulated player: add each draw; whenever every item of a set is held ≥ 1, take one of each and add `set.Reward` (discovery and turn-ins alike); at the end sell what's left at `ItemCatalog.SellPrice`. Assert `returned / packs <= 0.7 * PackPrice`; print the ratio. Run it; if it fails, raise `PackPrice` (round to 5) until it passes with the ratio printed, and note the final price.
- [ ] **Step 2: Failing checks** (in-memory SQLite, `Random(7)`): wallet 200, `OpenPackAsync(…, 1, …)` → `Opened`, 3 finds, all `ItemKind.Collectible`, balance `200 - PackPrice`, `spend.pack` today == `PackPrice`, `item.found` today == 3, `CaillouxSpent == PackPrice`; wallet below `PackPrice` → `TooPoor`, nothing granted; quantity 2 → `BadQuantity`. A seeded run where the player already holds every item but one of a set and the pack supplies it → the find's `Completed` holds the set and the wallet received `set.Reward`. Shop card with `picked: ItemCatalog.PackKey` → select + 2 buttons (`shop:pack:1`, `shop:pack:5`), embed ≤ 6 000; the existing soap case still shows 4 components.
- [ ] **Step 3: Run checks** → FAIL.
- [ ] **Step 4: Implement `OpenPackAsync`** through `EconomySave.RetryAsync`, rolling inside the work: price `PackPrice * quantity` (no bulk discount), `quantity * PackSize` × `InventoryService.GrantAsync(db, …, ItemCatalog.DrawCollectible(rng, now), now)`, `EconomyLog.AddAsync(SpendPack)`, `Patronage.AddAsync`, one save.
- [ ] **Step 5: Implement the card and handler.** Embed gains a « Sachets » field: « 🎁 **Sachet de collection** · {price} · 3 objets de collection au hasard, ouverts tout de suite ». Select gains the pack option (value `PackKey`). When `picked == PackKey`, row 1 shows the `PackQuantities` buttons `shop:pack:{qty}` (own verb, disabled when unaffordable). Footer: « La nourriture et les soins vont dans ton garde-manger ; les sachets s'ouvrent tout de suite. » Handler `[ComponentInteraction("shop:pack:*", ignoreGroupNames: true)]`, defers, validates against `PackQuantities`, redraws with `PackNotice` (finds listed with their emoji, then each completed set with its reward; TooPoor names price and balance).
- [ ] **Step 6: Run build + checks** → PASS.

### Task 6: Docs, help, version

**Files:**
- Modify: `docs/agents/economy.md` (Patronage rule and list of what counts; `/pay`; packs; new metric keys `spend.tax`, `spend.pack`), `docs/agents/xp.md` (Mécène badge, Mécènes tab, 33/40)
- Modify: `ProjectSYNCS/Commands/Core/HelpModule.cs` (economy page: `/pay`, packs in the shop line), `README.md` (Economy: `/pay`, packs; Levels: Mécène badge, Mécènes tab)
- Modify: `ProjectSYNCS/config.yaml` → `7.68.0`; `ProjectSYNCS/CHANGELOG.md` new `## 7.68.0 — 2026-11-18` (English bullets: `/pay`, booster packs, Mécène badge, Mécènes leaderboard tab)

- [ ] **Step 1: Write the docs and help text.**
- [ ] **Step 2: Run checks** → the `/help` size checks still pass (6 000 cap).
- [ ] **Step 3: Bump and changelog** (after re-checking the diff, see Global Constraints).
- [ ] **Step 4: Final** `dotnet build ProjectSYNCS -warnaserror` and `dotnet run --project tests/ProjectSYNCS.Checks` → `ALL CHECKS PASSED`.
- [ ] **Step 5: Hand the dev-guild checklist to the owner:** buy soap / a pack / a cosmetic → `/level` Succès shows Mécène progress, `/leaderboard` Mécènes tab moves in Semaine/Mois/Tout; `/pay` success, self, bot, too poor, over cap; `/admin dashboard` shows « taxe » and « sachets ».
