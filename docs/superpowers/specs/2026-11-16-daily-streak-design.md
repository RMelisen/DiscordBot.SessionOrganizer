# `/daily` — a daily claim with a streak

## Intent

Give members a reason to come back **every day**, not one more big payout: `/work` stays the main
income. A claim once per Paris day pays cailloux that grow with the streak of consecutive days, plus
a chance of an item that also grows. A long streak shows on the profile as a tiered badge.

## Behaviour (decided)

- **One claim per Paris calendar day** (`AppTime.DayKey`), not 24 h since the last one.
  - last claim = today → refused (ephemeral, « déjà réclamé », next claim `<t:…:R>` at Paris midnight);
  - last claim = yesterday → streak + 1;
  - anything else → streak = 1 (**a missed day resets to zero**, no protection, no grace).
- **Cailloux**: base `10 + 5 × (streak − 1)`, plus a random spread that widens with the streak,
  **total clamped to [5, 100]**.
  - Spread (my call): ±5 for streak 1–5, ±10 for 6–14, ±15 from 15. The base reaches 100 on day 19;
    from there the roll lands in 85–100.
- **Item**: chance `min(60 %, 10 % + 2 % × (streak − 1))` — 10 % on day 1, ceiling 60 % from day 26.
  Only the **chance** grows, never the rarity: the find is `ItemCatalog.DrawForage` unchanged
  (1 in 5 a pantry food, otherwise a collectible at the usual 60/28/10/2 odds, seasonal rules apply).
- **Public reply**, exactly two lines (no « demain » line):
  ```
  Série : 12 jours
  +62 cailloux  ·  🍄 Morille trouvée !
  ```
  Without a find, the second line is just the cailloux. Item names go through the usual
  `ItemInfo.Emoji` / `ClearName`.
- **`;daily`** typed shortcut alongside `;work` / `;forage` (my call, proposed and accepted).
- **No DM reminder.**
- **Badge**: a new palier in « Succès » over the **best streak ever** (so a reset never takes a tier
  away), unit `Days`, tiers **7 / 30 / 100 / 200 / 365**.

### Addendum: days she was offline don't count against a streak (decided after review)

- A missed Paris day is **excused** when she was unreachable for **3 h or more** of it, summed over
  merged stretches (overlapping records count once). If every day between the last claim and today
  is excused, the streak carries on (+1); any unexcused day still resets it.
- Downtime = every `UptimeEvent` (power cut, crash, gateway outage ≥ 10 min) **plus clean stops**
  (updates, the add-on stopped), recorded in a new `CleanStop` table from the last heartbeat to the
  next start. Clean stops stay out of `/health` and the monthly report: they serve streaks only.

## Approaches considered

1. **Three columns on `PebbleWallet`** — `DailyDay` (int `yyyymmdd`), `DailyStreak`, `DailyBest`.
   **Chosen.** The wallet already carries the `/work` state (`LastWorkAt`, `PassiveDay`); its
   `Balance` concurrency token makes a double `/daily` lose the race, retry through
   `EconomySave.RetryAsync` and be refused as already claimed; streak and payout land in one save.
2. A separate `DailyStreak` table — cleaner separation, but the claim touches two tables and the new
   one needs its own concurrency guard for the same protection. More code, no visible difference.
3. Best streak stored in `MemberBadge.Count` (like `arcade` / `nanny`) — rejected: splits the state,
   and paliers are meant to be derived from existing data.

## Constraints the builder must respect

- Claim runs inside `EconomySave.RetryAsync`, all reads inside the work (it may run twice).
- Defer before the DB work (3 s interaction rule).
- New dashboard metric **`earn.daily`** via `EconomyLog.AddAsync` (stored key, append-only); the item
  counts as `item.found` like every other find. Item added through `InventoryService.AddAsync` in the
  same unit of work (so a completed set pays in the same save).
- Badge: new key `daily` in `MemberBadges.Keys` (append-only), derived from `DailyBest` added to
  `MemberBadgeFacts`; no write through `BadgeAwarder` needed since nothing is stored. Icons
  `badge.daily_<tier>` are optional (medal fallback).
- The cailloux/chance formulas are pure (`Helpers/Economy/`), so the checks project can pin them.
- `/daily` is a top-level command: `[CommandContextType(InteractionContextType.Guild)]`.
- Migration adds the three columns (default 0). **Minor bump** 7.58.3 → **7.59.0**, CHANGELOG entry,
  `/help` and `README.md` updated, line in `docs/agents/economy.md`.

## Non-goals

- No streak leaderboard, no streak protection item, no reminder, no milestone item rewards.
- No change to `/work`, forage odds or rarity weights.

## Left to the builder

- Exact French wording of the refusal and where `;daily` hooks into `PlynlingShortcutService`.
- Where the badge sits in the Paliers tab order.
