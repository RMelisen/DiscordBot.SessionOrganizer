# `/daily` Streak Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A `/daily` claim (and `;daily`) once per Paris day, paying cailloux and a chance of an item that both grow with a streak of consecutive days, plus a « Succès » palier over the best streak.

**Architecture:** The rules are one pure static class (`Helpers/Economy/DailyStreak`), pinned by the checks project. The state is three columns on `PebbleWallet`; the claim is one `PebbleService` method run through `EconomySave.RetryAsync`, so the payout, the streak and any item land in one save. `EconomyModule` and `PlynlingShortcutService` share one static reply builder. The badge is a derived palier reading `DailyBest`.

**Tech Stack:** .NET 10, Discord.Net 3.20, EF Core + SQLite, plain-console checks (`tests/ProjectSYNCS.Checks`).

**Spec:** `docs/superpowers/specs/2026-11-16-daily-streak-design.md`

## Global Constraints

- One claim per **Paris calendar day** (`AppTime.DayKey`). Yesterday → streak + 1; anything older → streak = 1; today → refused.
- Cailloux: base `10 + 5 × (streak − 1)`; spread ±5 (streak 1–5), ±10 (6–14), ±15 (15+); total clamped to **[5, 100]**.
- Item chance: `min(0.60, 0.10 + 0.02 × (streak − 1))`; the item is `ItemCatalog.DrawForage(rng, now)` unchanged.
- Public reply, two lines: `Série : N jours` then `+X cailloux` (with `  ·  ` and the item when one is found). No « demain » line. No DM reminder.
- Badge key `daily` (stored-key rules apply: append-only), palier over `DailyBest`, unit `Days`, thresholds **7 / 30 / 100 / 200 / 365**.
- New metric key `earn.daily` (append-only), dashboard label « /daily ».
- Command and option names English, every user-facing string French; code/comments/logs English.
- Never `DateTime.Now`; never a bare `Random` for a *line* (this rolls numbers, so `Random` is fine; pass it in).
- Version: `ProjectSYNCS/config.yaml` 7.58.3 → **7.59.0** (minor: new command + migration), once for the batch, with a `CHANGELOG.md` entry.
- **No commit steps**: the owner commits by hand. Each task ends with the build and the checks passing instead.

## Review Focus

1. **Midnight and DST**: a claim at 23:59 then 00:01 Paris is two days; the 25-hour day (2026-10-25) and the 23-hour day (2026-03-29) never break or double a streak. → Task 1 tests `NextStreak` on day keys around both, and at month/year ends.
2. **A wallet that never claimed** (`DailyDay = 0`, every existing row after the migration) → streak 1, not a refusal or a weird jump. → Task 1 tests `NextStreak(0, 0, today) == 1`.
3. **Two claims at once** (`/daily` and `;daily` in the same second) → exactly one pays. → Task 2 relies on `Balance`'s concurrency token; Task 3 has a manual dev-guild step.
4. **Absurd streaks** (1000+ days) → never above 100 cailloux, never above 60 %. → Task 1 tests streak 10 000.
5. **A find that completes sets** → the set lines follow and the reply stays under 2000 chars. → Task 3 tests `DailyReply` with a find completing every set.

---

### Task 1: The rules — `DailyStreak`

**Files:**
- Create: `ProjectSYNCS/Helpers/Economy/DailyStreak.cs`
- Test: `tests/ProjectSYNCS.Checks/Program.cs` (new section `// 44. Daily streak (DailyStreak): …`, placed after section 40)

**Interfaces:**
- Produces (namespace `ProjectSYNCS.Helpers`, `public static class DailyStreak`):
  - `const long MinCailloux = 5`, `const long MaxCailloux = 100`
  - `int? NextStreak(int lastDay, int streak, int today)`: `null` when `lastDay >= today` (already claimed, or a stamp from the future); `streak + 1` when `lastDay` is the calendar day before `today`; else `1`. Compare dates with `AppTime.FromDayKey(lastDay).AddDays(1) == AppTime.FromDayKey(today)`, never by subtracting 24 h from an instant (wrong on a 25-hour day). `lastDay == 0` means never claimed (don't pass 0 to `FromDayKey`).
  - `long Base(int streak)`: `min(100, 10 + 5 × (streak − 1))`
  - `int Spread(int streak)`: 5 / 10 / 15 as above
  - `long RollCailloux(int streak, Random rng)`: `Base + rng.Next(-Spread, Spread + 1)`, clamped to [5, 100]
  - `double ItemChance(int streak)`
  - `DateTimeOffset NextClaimAt(DateTimeOffset now)`: next Paris midnight, `AppTime.AtWallClock(AppTime.FromDayKey(AppTime.DayKey(now)).AddDays(1))`
  - `string Days(int n)`: `"1 jour"` / `"12 jours"` (0 and 1 singular, like `PebbleEconomy.Cailloux`)

- [ ] **Step 1: Write the failing checks** (section 44)

```csharp
// 44. Daily streak (DailyStreak): one claim per Paris day, a missed day resets, the pay and the
// item chance grow and stay capped.
{
    if (DailyStreak.NextStreak(0, 0, 20261116) != 1) Fail("daily: first claim ever");
    if (DailyStreak.NextStreak(20261116, 4, 20261116) is not null) Fail("daily: twice in a day");
    if (DailyStreak.NextStreak(20261117, 4, 20261116) is not null) Fail("daily: a stamp from the future pays");
    if (DailyStreak.NextStreak(20261115, 4, 20261116) != 5) Fail("daily: yesterday continues");
    if (DailyStreak.NextStreak(20261114, 40, 20261116) != 1) Fail("daily: a missed day resets");
    foreach (var (last, today) in new[] { (20261130, 20261201), (20261231, 20270101), (20260228, 20260301),
                                          (20261025, 20261026), (20261024, 20261025), (20260329, 20260330), (20260328, 20260329) })
        if (DailyStreak.NextStreak(last, 3, today) != 4) Fail($"daily: {last} -> {today} is not consecutive");

    if (DailyStreak.Base(1) != 10 || DailyStreak.Base(2) != 15 || DailyStreak.Base(19) != 100 || DailyStreak.Base(10_000) != 100) Fail("daily: base");
    if (DailyStreak.Spread(5) != 5 || DailyStreak.Spread(6) != 10 || DailyStreak.Spread(14) != 10 || DailyStreak.Spread(15) != 15) Fail("daily: spread");
    var dRng = new Random(44);
    foreach (var (streak, lo, hi) in new[] { (1, 5L, 15L), (6, 25L, 45L), (19, 85L, 100L), (10_000, 85L, 100L) })
    {
        var rolls = Enumerable.Range(0, 3000).Select(_ => DailyStreak.RollCailloux(streak, dRng)).ToList();
        if (rolls.Min() != lo || rolls.Max() != hi) Fail($"daily: streak {streak} rolls {rolls.Min()}..{rolls.Max()}, want {lo}..{hi}");
    }
    if (Math.Abs(DailyStreak.ItemChance(1) - 0.10) > 1e-9 || Math.Abs(DailyStreak.ItemChance(26) - 0.60) > 1e-9
        || Math.Abs(DailyStreak.ItemChance(10_000) - 0.60) > 1e-9) Fail("daily: item chance");

    var dEvening = new DateTimeOffset(2026, 11, 16, 22, 30, 0, TimeSpan.Zero);          // 23:30 Paris (CET)
    if (DailyStreak.NextClaimAt(dEvening) != new DateTimeOffset(2026, 11, 16, 23, 0, 0, TimeSpan.Zero)) Fail("daily: next claim in winter");
    var dSummer = new DateTimeOffset(2026, 3, 29, 12, 0, 0, TimeSpan.Zero);             // first CEST day
    if (DailyStreak.NextClaimAt(dSummer) != new DateTimeOffset(2026, 3, 29, 22, 0, 0, TimeSpan.Zero)) Fail("daily: next claim in summer");
    if (DailyStreak.Days(1) != "1 jour" || DailyStreak.Days(12) != "12 jours") Fail("daily: plural");
}
```

- [ ] **Step 2: Run, expect a build failure** (`DailyStreak` doesn't exist)

Run: `dotnet run --project tests/ProjectSYNCS.Checks`
Expected: compile error `The name 'DailyStreak' does not exist`.

- [ ] **Step 3: Implement `DailyStreak`** with the signatures above. A class comment says why days are compared as dates (the 25-hour day).

- [ ] **Step 4: Run the checks**

Run: `dotnet run --project tests/ProjectSYNCS.Checks`
Expected: no `FAIL daily:` line, `ALL CHECKS PASSED`.

---

### Task 2: Storage and the claim

**Files:**
- Modify: `ProjectSYNCS/Models/Economy/PebbleWallet.cs` (three properties + a comment)
- Create: migration `AddDailyStreak` (`dotnet ef migrations add AddDailyStreak`, from `ProjectSYNCS/`)
- Modify: `ProjectSYNCS/Services/Economy/PebbleService.cs` (record + method)
- Modify: `ProjectSYNCS/Helpers/Economy/EconomyLog.cs` (`EarnDaily = "earn.daily"`, appended to `Earnings`)
- Modify: `ProjectSYNCS/Commands/Admin/AdminCards.cs` (`EarnLabels`: `(EconomyLog.EarnDaily, "/daily")`)

**Interfaces:**
- Consumes: Task 1's `DailyStreak`.
- Produces:
  - `PebbleWallet.DailyDay` (`int`, `yyyymmdd`, 0 = never), `DailyStreak` (`int`), `DailyBest` (`int`)
  - `public sealed record DailyResult(bool Claimed, int Streak, long Amount, ItemFind? Find, DateTimeOffset NextClaimAt);`
  - `public Task<DailyResult> ClaimDailyAsync(ulong guildId, ulong userId, DateTimeOffset now, Random rng)` on `PebbleService`

- [ ] **Step 1: Add the three properties** and a comment (why on the wallet: `Balance`'s token makes a double claim lose the race; `DailyBest` exists so a reset never takes a badge tier away). Plain `int`s: no `OnModelCreating` change needed.

- [ ] **Step 2: Generate the migration**

Run (in `ProjectSYNCS/`): `dotnet ef migrations add AddDailyStreak`
Expected: a migration adding three `INTEGER NOT NULL DEFAULT 0` columns to `PebbleWallets` and nothing else. Anything else in the diff (an unrelated column) means the model drifted: stop and report.

- [ ] **Step 3: Implement `ClaimDailyAsync`**, as `EconomySave.RetryAsync(_db_context, () => ClaimDailyOnceAsync(...))` like `WorkAsync`. All reads inside the work. In order:
  `GetOrCreateWalletAsync` → `NextStreak(wallet.DailyDay, wallet.DailyStreak, AppTime.DayKey(now))`. If null, return `Claimed: false` with the stored streak. Otherwise: `RollCailloux`, add to `Balance`, set `DailyDay`, `DailyStreak` and `DailyBest = max`, then `EconomyLog.AddAsync(…, EarnDaily, amount, now)`. If `rng.NextDouble() < ItemChance(streak)`, call `InventoryService.GrantAsync(_db_context, guildId, userId, ItemCatalog.DrawForage(rng, now), now)` (it logs `item.found` and pays a completed set into the same wallet). Then one `SaveChangesAsync`. `NextClaimAt = DailyStreak.NextClaimAt(now)`.

- [ ] **Step 4: Build and run the checks**

Run: `dotnet build -warnaserror` then `dotnet run --project tests/ProjectSYNCS.Checks`
Expected: build succeeds, `ALL CHECKS PASSED`.

---

### Task 3: `/daily`, `;daily` and the reply

**Files:**
- Modify: `ProjectSYNCS/Commands/Economy/EconomyModule.cs` (command + `DailyReply`)
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingShortcut.cs` (`ShortcutKind.Daily` before `Unknown`, which is not stored; `;daily`, `;pl daily`; `Usage` names `;daily`)
- Modify: `ProjectSYNCS/Services/Plynling/PlynlingShortcutService.cs` (case `Daily`, header comment)
- Modify: `ProjectSYNCS/Helpers/Core/ActivityKeys.cs` (`Shortcut`: `Daily => ";daily"`; `Reached`: `";daily" => "daily"`)
- Test: `tests/ProjectSYNCS.Checks/Program.cs` (extend section 44)

**Interfaces:**
- Consumes: Task 2's `DailyResult`, `ClaimDailyAsync`; `PlynlingText.ItemLabel`, `PlynlingText.FindLines`.
- Produces: `public static (string Text, bool Refused) DailyReply(DailyResult result, ulong userId)` on `EconomyModule`.

Exact copy:
- Claimed, no find: `$"Série : {Days(streak)}\n+{PebbleEconomy.Cailloux(amount)}"`
- Claimed with a find: the second line continues with `$"  ·  trouvé : {PlynlingText.ItemLabel(item)}"`, then `PlynlingText.FindLines(…, find, userId)` appends any completed set. (« trouvé : » rather than « Morille trouvée » because items carry no grammatical gender.)
- Refused: `$"Déjà réclamé aujourd'hui. Prochain `/daily` <t:{unix}:R> · série : {Days(streak)}"`

- [ ] **Step 1: Write the failing checks** (append to section 44)

```csharp
    var dAt = new DateTimeOffset(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);
    var (dText, dRefused) = ProjectSYNCS.Commands.EconomyModule.DailyReply(new DailyResult(true, 12, 62, null, dAt), 1);
    if (dRefused || dText != "Série : 12 jours\n+62 cailloux") Fail($"daily reply: {dText}");
    var dItem = ItemCatalog.All.First(i => i.Kind == ItemKind.Collectible);
    var dAllSets = new ItemFind(dItem, ItemCatalog.Sets.ToList());
    var (dFound, _) = ProjectSYNCS.Commands.EconomyModule.DailyReply(new DailyResult(true, 365, 100, dAllSets, dAt), ulong.MaxValue);
    if (!dFound.StartsWith("Série : 365 jours\n+100 cailloux  ·  trouvé : ") || dFound.Length > 2000) Fail($"daily reply with a find ({dFound.Length})");
    var (dNo, dNoRefused) = ProjectSYNCS.Commands.EconomyModule.DailyReply(new DailyResult(false, 3, 0, null, dAt), 1);
    if (!dNoRefused || !dNo.Contains($"<t:{dAt.ToUnixTimeSeconds()}:R>")) Fail("daily reply: refusal");
    if (PlynlingShortcut.Parse(";daily") != new Shortcut(ShortcutKind.Daily) || PlynlingShortcut.Parse(";pl daily") != new Shortcut(ShortcutKind.Daily)
        || PlynlingShortcut.Parse(";daily x") != new Shortcut(ShortcutKind.Unknown) || PlynlingShortcut.Parse(";dailyx") is not null) Fail("daily shortcut");
    if (CommandPath.Shortcut(ShortcutKind.Daily) != ";daily" || CommandPath.Reached(";daily") != "daily") Fail("daily shortcut key");
```

- [ ] **Step 2: Run, expect a build failure** (`DailyReply`, `ShortcutKind.Daily` missing)

Run: `dotnet run --project tests/ProjectSYNCS.Checks`

- [ ] **Step 3: Implement.**
  - `[SlashCommand("daily", "Ta récompense du jour, qui grandit avec ta série")]`: `DeferAsync()` first (the claim writes before anything shows). Then `ClaimDailyAsync(Context.Guild.Id, Context.User.Id, DateTimeOffset.UtcNow, Random.Shared)`. Success → `ModifyOriginalResponseAsync` with the text and `AllowedMentions.None`. Refusal → `DeleteOriginalResponseAsync()`, then `FollowupAsync(text, ephemeral: true, allowedMentions: AllowedMentions.None)` (the `PlynlingModule.RefuseAsync` pattern).
  - Shortcut case `Daily`: `pebbles.ClaimDailyAsync(…, now, Random.Shared)` → `AnswerAsync(message, EconomyModule.DailyReply(result, authorId))`.

- [ ] **Step 4: Build and run the checks**

Run: `dotnet build -warnaserror` then `dotnet run --project tests/ProjectSYNCS.Checks`
Expected: `ALL CHECKS PASSED`.

- [ ] **Step 5: Dev-guild check** (`dotnet run`): `/daily` posts `Série : 1 jour`. A second `/daily` gets a private refusal. `;daily` gets a refusal that deletes itself. Sending `/daily` and `;daily` back to back on a fresh account pays once. In SQLite, the wallet row shows `DailyDay` = today and `DailyStreak` = 1.

---

### Task 4: The « Assiduité » palier

**Files:**
- Modify: `ProjectSYNCS/Helpers/Xp/MemberBadges.cs`
- Modify: `ProjectSYNCS/Services/Xp/MemberBadgeService.cs` (facts)
- Test: `tests/ProjectSYNCS.Checks/Program.cs` (the member-badge section: `MbFacts`, `mbMax`)

**Interfaces:**
- Consumes: Task 2's `PebbleWallet.DailyBest`.
- Produces: `MemberBadges.Keys.Daily = "daily"`; `MemberBadgeFacts` gains a trailing `long DailyBest = 0` (optional, so existing positional constructions still compile).

- [ ] **Step 1: Write the failing checks.** Give `MbFacts` a `long daily = 0` parameter passed as `DailyBest`. Set `mbMax`'s `DailyBest: 99_999`. Add:

```csharp
    if (MbOf(MbFacts(daily: 6), "daily").Earned || MbOf(MbFacts(daily: 7), "daily").Tier != CollectionTier.Bronze
        || MbOf(MbFacts(daily: 30), "daily").Tier != CollectionTier.Silver || MbOf(MbFacts(daily: 365), "daily").Tier != CollectionTier.Diamond) Fail("daily badge tiers");
```

- [ ] **Step 2: Run, expect failure** (`MbOf(…, "daily")` finds no entry → `Single` throws, or compile error on `daily:`).

- [ ] **Step 3: Implement.** In `All`, after `Nanny` and before `Seniority`: `Palier(Keys.Daily, "🔥", "Assiduité", "Un jour après l'autre, sans en sauter un.", new long[] { 7, 30, 100, 200, 365 }, BadgeUnit.Days)`. `ValueOf`: `Keys.Daily => f.DailyBest`. In `MemberBadgeService`, read `DailyBest` from the member's wallet (`AsNoTracking`, `Select(x => x.DailyBest)`, 0 if none) and pass it.

- [ ] **Step 4: Run the checks**

Run: `dotnet run --project tests/ProjectSYNCS.Checks`
Expected: `ALL CHECKS PASSED`. The worst-case « Succès » view and card line still fit (the existing measurement fails otherwise; if it does, stop and report rather than trimming other badges).

---

### Task 5: Help, docs, version

**Files:**
- Modify: `ProjectSYNCS/Commands/Core/HelpModule.cs` (`BuildEconomy`, « Commandes — Cailloux » field)
- Modify: `README.md` (command table row near `/work · /balance`; the « Cailloux come from » paragraph)
- Modify: `docs/agents/economy.md` (new section *`/daily` — the streak*), `docs/agents/xp.md` (paliers: `daily` reads `PebbleWallet.DailyBest`), `docs/agents/plynling.md` (*Typed shortcuts*: `;daily`)
- Modify: `ProjectSYNCS/config.yaml` (`version: "7.59.0"`), `ProjectSYNCS/CHANGELOG.md`

- [ ] **Step 1: Help line**, after `/work`: `"**`/daily`** — Ta récompense du jour : elle grandit chaque jour d'affilée (jusqu'à 100 cailloux, parfois un objet) et repart de zéro si tu en sautes un. Raccourci : `;daily`.\n"`

- [ ] **Step 2: README and agent notes.** economy.md section in the house style: the rule, why it's on the wallet (concurrency token, one save), days compared as dates, `DailyBest` for the badge, `earn.daily`, the reply built once in `EconomyModule.DailyReply` and shared with the shortcut, and that silence mode doesn't apply (it's a command answer).

- [ ] **Step 3: Version and changelog.** `config.yaml` → `7.59.0`. CHANGELOG, newest first:
```
## 7.59.0 — 2026-11-16

- New `/daily` (and `;daily`): a daily reward that grows with each day in a row, up to 100
  cailloux, with a growing chance of an item; missing a day starts the streak over.
- New « Assiduité » badge for your best streak: 7, 30, 100, 200 and 365 days.
```

- [ ] **Step 4: Final verification**

Run: `dotnet build -warnaserror` then `dotnet run --project tests/ProjectSYNCS.Checks`
Expected: `ALL CHECKS PASSED`. The `/help page 2` line (economy) prints at most 6000 chars, with its longest field at most 1024.
