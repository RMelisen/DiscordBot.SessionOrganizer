# Cailloux sinks for everyone — roadmap + release 1 plan

## Context

Members who don't own a Plynling pile up cailloux with nothing to spend them on: an active member
earns ~130–220/day (`/work`, passive, `/daily`, quiz), and every sink except `/arcade` (which mostly
moves cailloux between players) is Plynling-only (food, care, cosmetics). Goal: give everyone
repeatable sinks, some with no ceiling, that remove cailloux from the economy — and make spending a
status, not a loss.

Decided with the owner (2026-11-18):
- **Keep**: Mécène badge (4), Mécènes leaderboard tab with 7/30-day windows (5), bribes —
  « chouchou du jour » + erasing a shame hit (7, 20), morning-hello dedication sold by auction (8+10),
  a jar with automatic rewards (13), a weekly lottery (15), `/pay` with a tax (17), collection
  booster packs (18).
- **Dropped**: weekly auction of a unique cosmetic (14), bounties (16).
- Dedication: **her lines, a big-up to whoever paid**; on a dedicated day the dedication replaces
  the classic hello line; the classic hello only when nobody bought it.
- Lottery: unlimited tickets, odds ∝ tickets, house keeps ~25 %, **SYNCS gets one ticket each week**.
- **Shipped in three releases**, each planned in detail before it is built.

**Mécène rule (my call)**: only cailloux that *leave the economy* count — shop, meals (own and
others'), medicine, cosmetics, crafting, packs, the `/pay` tax, bribes, the dedication, the jar.
Not arcade, not lottery (would reward gambling), not admin debits.

---

## Release 1 — Mécène + `/pay` + booster packs (minor bump, one migration)

### 1a. Spent counter (totals + daily buckets)
- Add `long CaillouxSpent` to `Models/Xp/MemberXp.cs` and `Models/Xp/MemberDailyStat.cs` — the
  existing totals/buckets pair behind `/leaderboard`, so windows and tallies come for free. One
  migration (`AddCaillouxSpent`), schema only.
- New `Helpers/Economy/Patronage.cs`: `AddAsync(db, guildId, userId, amount, now)` — finds or
  creates both rows (`Local` first, like `EconomyLog.AddAsync`), adds, **never saves**; the action's
  own save carries it.
- Call it next to each counted `EconomyLog` spend: `InventoryService.cs:176` (shop),
  `CosmeticService.cs:39` / `:63`, `PlynlingService.cs:192` (meals) / `:305` (medicine), plus the
  new pack and `/pay` tax below. Not `ArcadeService.cs:96`, not `PebbleService.cs:181`.
  The economy paths run inside `EconomySave.RetryAsync` (work may re-run): fine, it's DB work only.

### 1b. Mécène badge
- `Helpers/Xp/MemberBadges.cs`: new key `mecene` (append-only), `Palier`, unit Count, read from
  `MemberXp.CaillouxSpent` in `ValueOf` (facts via `MemberBadgeService.GetFactsAsync`) — derived,
  not a `MemberBadge.Count` row. Thresholds (my call): 500 / 2 500 / 10 000 / 25 000 / 60 000.
- Icons optional (`Assets/Icons/badge.mecene_<tier>.png`); medal fallback works without them.

### 1c. Mécènes leaderboard tab
- `LeaderboardView.Patrons` in `Commands/Xp/LevelModule.cs` + `XpService` tally/`ValueFor`,
  `LevelCardUi` row/title/label/empty line/footer cases. Tab row becomes 5/5 buttons; card goes
  32 → 33 of 40 components (re-count in the checks).

### 1d. `/pay member amount`
- Top-level in `EconomyModule` (with `/work`, `/balance`, `/daily`), `[CommandContextType(Guild)]`.
- 10 % tax rounded up, burned; min 10; refuse self, bots, insufficient balance; daily cap per
  sender (my call: 2 000/day) to blunt alt funnelling.
- `PebbleService.PayAsync` through `EconomySave.RetryAsync`, both wallets in one save
  (`Balance` tokens guard it). Log tax as new metric `spend.tax` (« taxe » on the dashboard);
  Patronage counts the tax only. Public reply, recipient as a pill, `AllowedMentions.None`.

### 1e. Booster packs
- A « Sachet » line on the `/inventory shop` card (`ShopCards`) opening at once into 3
  `ItemCatalog.DrawCollectible` draws (no food) via `InventoryService.OpenPackAsync`; finds go
  through `InventoryService.AddAsync` (logs `item.found`, pays completed sets in the same save).
  New metric `spend.pack` (« sachets »). Not stored as an item; its button gets its own verb
  (`shop:pack:{qty}`).
- **Price set by simulation, not guessed**: one draw sells for ~4.5, but set rewards (100–1 000,
  paid again on every turn-in) make the true value higher. Add a checks section that simulates
  many openings (sell value + discovery and turn-in rewards) and asserts the expected return stays
  well under the price. Starting point ~60 for 3.

### Docs & version
- `docs/agents/economy.md` (Patronage rule, `/pay`, packs, new metric keys), `docs/agents/xp.md`
  (badge, tab), `/help` (`HelpModule`, measure the 6000 cap) and `README.md`.
- Minor bump in `ProjectSYNCS/config.yaml` + `CHANGELOG.md` entry — unless `config.yaml` already
  differs from `HEAD` when this lands (the story-events batch in the working tree may bump first).

### Verification
- `dotnet build` (with `-warnaserror` as CI does); `dotnet run --project tests/ProjectSYNCS.Checks`
  incl. the new pack simulation and leaderboard component count.
- Dev guild: buy food/cosmetic/pack → `/level` badge progress and Mécènes tab (Semaine/Mois/Tout)
  move; `/pay` with tax, self/bot/over-cap refusals, two parallel `/pay` racing on one wallet;
  `/admin dashboard` shows « taxe » and « sachets ».

---

## Release 2 — bribes + dedication auction (design level; detailed plan before building)

- **`/bribe`** (English name, French everything else); price doubles per bribe in the same Paris
  week per person (e.g. 100 → 200 → 400). Counts toward Mécène.
  - **Chouchou du jour**: 24 h stored on the wallet (`ChouchouUntil`, survives restarts — it was
    paid for). Effects: warm pools instead of roasts on mention/reply (new pools; openly
    transactional, never « family »; read `docs/syncs-voice.md` + `personality/people.md`), some
    reactions via `ReactionService`. Papa/Tata tiers and `/rate`'s Papa-first rule unchanged.
  - **She looks away**: remove one `/shame` hit from a category the payer picks (mean, perfidy,
    shout, abandon; **ban votes excluded**, my call) — decrement `ShameRecord` and today's
    `ShameDailyStat`, clamped at 0. No migration.
- **Dedication auction** (`/shoutout bid amount`): bids until 20:00 Paris for tomorrow's hello;
  charged at bid, outbid refunded at once; stored table (restart-safe), tie → earliest. Next
  morning `MorningGreetingService` posts a dedication line (her pool, pings the winner only) in
  place of the hello line, fun fact kept. Must teach `IsMorningHello`/`AlreadyGreetedTodayAsync` to
  recognise dedication lines; on her birthday or a silenced day the dedication carries to the next.

## Release 3 — jar + lottery (design level)

- **Jar** (`/jar`, `/jar give amount`): one standing jar per guild; goals rise; each goal reached
  unlocks the next automatic reward in rotation — double `/daily` day, extra quiz slots for a day,
  a feast for every living Plynling, a giveaway run by SYNCS — then refills. Top contributors on
  the card; counts toward Mécène; unlock announced in the main channel.
- **Lottery** (`/arcade lottery` — `/arcade` is at 10/25): 20 per ticket, unlimited, draw weekly at
  a fixed Paris slot by a new stored-table loop modelled on `GiveawayDrawService` (own interval,
  line in `architecture.md`). Winner gets 75 %, 25 % burned. SYNCS holds one free ticket (adds
  nothing to the pot); if she wins, the whole pot is burned and she gloats. Metrics
  `spend.lottery` / `earn.lottery`; settles through `ArcadeService.SettleAsync`.
