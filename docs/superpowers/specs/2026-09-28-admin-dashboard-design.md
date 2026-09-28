# Admin dashboard — design

`/admin stats` is a snapshot of what is stored. The dashboard is the view **over time**: what the
economy earned and spent, what people did, and what they found, day by day. Nothing in the past
can be recovered, so every series starts on the day this ships.

## Recording

- **One table, `EconomyDailyStat`:** (guild, day, metric) → count, unique on the three.
  `Day` is the Paris `yyyymmdd` int from `AppTime.DayKey`, like the other daily buckets, so date
  windows filter in SQL. `Metric` is a stable string key — stored, so append-only like every key.
- **One helper, `EconomyLog.Add(db, guildId, metric, amount, now)`:** static, context-taking,
  Local-first find-or-create, **never saves** — the count rides the action's own `SaveChanges`,
  so a failed action records nothing and every count matches something that happened.
- **Cost:** one small row per metric per day (~30 metrics → ~11 000 rows a year, well under 1 MB);
  no extra network, no extra round trip.

### Metrics

| Key | Counted where | Amount |
|---|---|---|
| `earn.work` | `PebbleService.WorkAsync` (paid) | cailloux |
| `earn.passive` | `PebbleService.AddPassiveAsync` | cailloux granted |
| `earn.game` | `PlynlingService.FinishPlayAsync` (win) | cailloux |
| `earn.gift` | `PlynlingService.TryGiftAsync` (cailloux gift) | cailloux |
| `earn.badge` | `PlynlingService.AwardAsync` | badge rewards |
| `earn.collection` | `InventoryService.AddAsync` (set completed) | set reward |
| `earn.sale` | `InventoryService.SellAsync` | cailloux |
| `earn.admin` / `spend.admin` | `PebbleService.AdjustAsync` | amount actually moved |
| `spend.shop` | `InventoryService.BuyAsync` | price |
| `spend.meal` / `spend.meal_other` | `PlynlingService.FeedAsync` (paid in cailloux; own / someone else's) | price |
| `spend.cosmetic` | `CosmeticService.BuyAsync` | price |
| `spend.craft` | `CosmeticService.CraftAsync` | cailloux part |
| `act.meal` | `FeedAsync` (done, pantry or paid) | 1 |
| `act.pet` | `PetAsync` (done) | 1 |
| `act.game` | `FinishPlayAsync` | 1 |
| `act.visit` | `VisitAsync` | 1 |
| `act.forage` | `ForageAsync` (found) | 1 |
| `act.trade` | `InventoryService.TradeAsync` (done) | 1 |
| `act.give` | `InventoryService.GiveAsync` (given) | 1 |
| `item.found` | `InventoryService.GrantAsync` (forage, gift, game, visit finds) | 1 |
| `item.set` | `InventoryService.AddAsync` (set completed) | 1 |
| `cos.bought` | `CosmeticService.BuyAsync` | 1 |
| `cos.crafted` | `CosmeticService.CraftAsync` | 1 |

Callers without an instant (`AddPassiveAsync`, `SellAsync`, `AdjustAsync`) use `DateTimeOffset.UtcNow`.

## `/admin dashboard`

- Staff only (`SessionPermissions.IsStaff`), ephemeral, `AllowedMentions.None`.
- **Periods:** buttons **7 jours / 30 jours / Tout** (`dash:win:{period}`), each total compared with
  the previous period of the same length (« +12 % »; « nouveau » when the previous was 0; no
  comparison for « Tout »). Opens on 7 jours.
- **Charts:** a Unicode sparkline (`▁▂▃▄▅▆▇█`, one character per day, scaled to the series' max)
  for each headline series; « Tout » compresses to at most 30 characters by summing days.
- **Sections (embed fields):**
  - **Économie** — earned, spent, net (with sparklines), then the breakdown by source and by sink
    as percentages.
  - **Activité** — meals, pets, games, visits, forages, trades, gifts: total and sparkline each.
  - **Objets & cosmétiques** — items found, sets completed, cosmetics bought and crafted.
- A footer says since when data exists (the first recorded day).
- `EconomyDashboardService` (transient) loads the rows for the window and the previous window in
  one query; `AdminCards.BuildDashboard` renders it (static, measurable).

## Checks

- Every recording point counts once, in the action's save, and nothing on a refused action.
- Two metrics in one save share nothing; the same metric twice in one save lands in one row.
- Windows: the 7-day window covers today and the six days before (Paris), the previous window the
  seven before that; comparisons and « nouveau » right.
- Sparklines: length, scaling, empty series, « Tout » compression.
- The embed fits Discord's caps with every metric non-zero.
