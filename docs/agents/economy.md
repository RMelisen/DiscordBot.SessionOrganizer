# Economy — cailloux, inventory, items, cosmetics, dashboard

`/work`, `/balance` (`EconomyModule`), `/inventory` (`InventoryModule`, its own group — belongings
outlive the creature). `/plynling forage` stays under `/plynling` (it's something the Plynling
*does*); only its find lands in the inventory. `;work` and `;forage` are typed shortcuts for the two
(see `plynling.md`, *Typed shortcuts*).

## Cailloux (pebbles)

**`PebbleService.GetOrCreateWalletAsync(context, …)` never saves and looks in `Local` first.** A
wallet created earlier in the same unit of work isn't in the database yet; creating a second breaks
the unique index at save time (a first-time feeder whose meal also earned a badge hit this).
`PebbleService.AdjustAsync` (admin) clamps at 0.

**`PebbleWallet.Balance` and `InventoryItem.Quantity` are concurrency tokens.** Every write reads
the value, checks it, then saves a new absolute number, and commands run in parallel: two
`/inventory give` of the same 5 items to two people both read 5 and both landed, duplicating them.
Now the slower save throws `DbUpdateConcurrencyException` and its whole unit of work rolls back.
The player-facing entry points (`PebbleService`, `InventoryService`, `CosmeticService`,
`ArcadeService.SettleAsync`, `QuizService.RecordWinAsync`) go through `EconomySave.RetryAsync`,
which clears the context and runs the work again, so the loser re-checks and gets « pas assez »
instead of an error. Its work may run more than once: database work only, all reads inside.
`PlynlingService`'s feeding, care, play and gift paths are not wrapped: there a conflict fails that
one action with nothing saved. Never write a wallet or an item count any other way (no
`ExecuteUpdate` on them, no attaching a detached row), or the token stops guarding it.

**Passive cailloux are granted from `XpTracker.GrantAsync`, never detected separately.** Every XP
grant pays `PebbleEconomy.PassivePerGrant`, capped per day at 30% of three average `/work` shifts —
so the cap inherits every XP defence (cooldowns, excluded channels, voice eligibility) and can never
pay for something XP refused. Own `try`, so it can't swallow the level-up card. **Plynling actions
grant no XP** — the link runs one way, or money and levels would feed each other.

**The passive cap lives on the wallet row** (`PassiveDay` + `PassiveToday`), not in a daily-bucket
table: nothing ranks cailloux by date, so a buckets table would have no reader.

## `/admin dashboard` — `EconomyDailyStat`

One row per (guild, Paris day, metric), `Day` an int `yyyymmdd` so windows filter in SQL.
**`Helpers/Economy/EconomyLog.AddAsync` finds or creates the row (`Local` first) and never saves** — the
action's own `SaveChanges` carries it, so a refused action records nothing and a recorded one can't
be lost.

- **A new source or sink of cailloux, or a new Plynling action, needs its own `EconomyLog` call and
  metric key. Metric keys are stored — append-only.**
- A pop quiz win pays under `earn.quiz` (« quiz »), inside `QuizService.RecordWinAsync`'s
  transaction (see `quiz.md`). Quiz wins grant no XP.
- Plynling events pay through `GiveCailloux` under `earn.event` (« événements » on the dashboard), and
  their `GiveItem` finds count as `item.found` like every other find.
- Admin adjustments record what actually moved (after the clamp), not what was asked.
- `/arcade` games log net flows under `earn.arcade` / `spend.arcade` (« arcade »), through
  `ArcadeService.SettleAsync` only — see `arcade.md`.
- No totals row: the stored balances already are the totals (`/admin stats` reads those). Nothing
  was backfilled.
- Window buttons `dash:win:{Week|Month|All}` in `AdminComponentHandler`, which re-checks `IsStaff`.
  « Tout » has no trend; its sparkline sums days into at most 30 buckets to stay on one line.

## Inventory

**It belongs to the person, not the Plynling.** `InventoryItem` rows are keyed on (guild, user,
`ItemCatalog` key) and survive every death and abandonment. **Item keys are stored — append-only.**
A row at quantity 0 is **kept**: it is what makes an item *discovered* for good, which is why a set
completes on discovery (not on holding all at once) and why trading or selling never undoes a set.
`CollectionCompletion` (unique index) is the once-only guard on the *discovery* reward; its `Count`
then counts every completion (see *Tiers and turn-ins*).

**`InventoryService`'s static `AddAsync` / `TakeAsync` / `GrantAsync` take a context and never
save**: every source (pantry feeding, gift, game, visit, forage) moves items in its **own** unit of
work, so the action and its items land in one `SaveChanges`. `AddAsync` pays a completed set into the
wallet in that same save. They check `Local` first, so two adds in one save don't create two rows.

**`/inventory shop` sells `ItemCatalog.Shop` and nothing else**: the four foods at their menu
price, then the care items (Savon 20, Médicament 30), all at `BulkPrice` (−10 % from 5). It is one
ephemeral card (`ShopCards`, static and measured by the checks project): `shop:pick` (the select)
redraws it with ×1/×5/×10 buttons `shop:buy:{key}:{qty}`, disabled when unaffordable, and every
purchase redraws it with a notice. `InventoryService.BuyAsync(key, qty)` refuses any key not in
`Shop`, so a forged custom-id can't buy a collectible. A new buyable item is a line in `Shop`, not
a new subcommand.

**Feeding serves from the pantry first** — one of that food for your own Plynling, two for someone
else's (the pantry's version of the double price) — and charges cailloux only when there isn't
enough. So `TooPoor` is only reachable with an empty pantry.

**Random streams are kept apart for testability.** A visit's item finds use their own `Random`
(`findRng`), separate from the scene's scripted draws. The gift's item share uses the *upper* half of
its roll (`>= GiftCaillouxShare`) because the existing cailloux checks roll low.

**Trade offers are in memory** (`TradeOffers` singleton): a restart drops them and their buttons say
so. One open offer per proposer (a new one replaces the old). `Take` removes atomically, so two
clicks on « Accepter » can't both swap; the swap re-checks both sides inside one save. If the
*recipient* lacks the items the offer is restored (they may still get them); if the *proposer* does,
it's withdrawn.

## Sets and the Champignons set

Six sets: five of 8 items (Insectes the latest) and **Champignons, 30 items**, found mostly by
foraging: `DrawForage` picks from it 60% of the time, every other source 10%
(`ForageMushroomShare` / `MushroomShareElsewhere`), or its 30 would crowd out the rest. A set over 10
items is laid out one embed field per rarity (`ItemCatalog.Sections`) — 30 lines of emoji markup
overflow a 1024 field.

## Tiers and turn-ins

**A set's first completion is its discovery; every later one is a turn-in.** « Déposer » on a
completed set's book page (`col:dep:{user}:{set}:{filter}`) opens an ephemeral confirmation naming
the last copies it would spend; « Confirmer » (`col:depok:{set}`, ephemeral, so the clicker is the
owner) runs `InventoryService.TurnInAsync`: one of each item taken, `Count + 1`, the set's reward
paid again. Every completion — discovery or turn-in — goes through `PayCompletionAsync`: wallet,
`item.set` + `earn.collection` (same source, so no new metric key) and today's bucket.

- **Tiers are derived from `Count`, never stored** (`Helpers/Economy/CollectionTiers`): Bronze 1,
  Argent 2, Or 3, Platine 5, Diamant 10. Moving a threshold needs no migration.
- **A tier-up is announced in the channel** (`PlynlingText.SetTierUp`, mentions off); a plain
  turn-in only answers the ephemeral.
- **`Count` is a concurrency token**, on top of the items' `Quantity`: a double-click loses the race,
  retries, and finds the items missing.
- **Totals + buckets**: `CollectionCompletion.Count` is the totals (the migration started existing
  rows at 1), `CollectionDailyStat` the per-day buckets behind `/leaderboard`'s Collections windows
  (see `xp.md`). The buckets started empty with 7.9.0.
- **Badges**: `CollectionTiers.Badge` shows the icon `badge.<set>_<tier>` once uploaded (emoji
  `b_<set>_<tier>`; `_` not `.`, since the rest of the key becomes the emoji name), else the tier's
  medal (🥉🥈🥇🏅💠) beside the set's icon. Dropping the 30 PNGs in `Assets/Icons/` is all the art
  needs — `ItemEmojis.IsIconKey` already accepts them.

## Item icons — application emojis

**The pictures are application emojis** (owned by the bot's application, so unlike reaction emotes
they work in any server), and **the bot uploads them itself**.

- Mushroom sprites ship in `ProjectSYNCS/Assets/Mushrooms/<slug>.png` (128 px, ×8 from the pack's
  16 px without smoothing; copied to output and publish by the csproj). File names are the item keys
  (`col.<file name>`) — the harness checks both ways. `CREDITS.txt` is the pack's list with Latin
  names.
- Every other collectible, the six sets, every cosmetic and every Plynling trait have icons drawn in
  the same style: 16×16 text grids in `tools/item-art/icons.py`, in the pack's palette (Pear36 +
  eight pack colours), exported ×8 to `ProjectSYNCS/Assets/Icons/<key>.png` (`<key>` = item key,
  `set.<set key>` or `trait.<trait key>`).
- **`ApplicationEmojiService`**, on the first Ready, lists the application's emojis, uploads any
  missing sprite (from both folders via `ItemEmojis.Sources`), and records the markup in the static
  `ItemEmojis` map. Names: mushrooms `shroom_<slug>`, the rest `ItemEmojis.EmojiName`'s short
  prefixes (`c_`, `s_`, `th_`, `ti_`, `ac_`, `gr_`, `tr_`) — Discord caps names at 32 chars. Each
  application (dev, prod) gets its own copy with no manual step.
- **An existing emoji is reused by name, never replaced** — to change a picture, delete the emoji in
  the developer portal and restart.
- **Emoji properties are computed**: `ItemInfo.Emoji`, `CollectionSet.Emoji`, `CosmeticInfo.Emoji`,
  `TraitInfo.Emoji`, a thème's `Banner` and a cadre's `GraveLeft`/`GraveRight` use the uploaded
  icon, else the stored `DefaultEmoji` — so nothing breaks before or without the upload.
- **Autocomplete is plain text**: go through `ItemCatalog.TextEmoji`, which drops custom markup.
- **A select option must get `EmoteMarkup.Parse(emoji)`, never `new Emoji(emoji)`** (Discord
  rejects custom markup there).
- **Foods share their collectible twin's picture** (`ItemCatalog.SharedPictures`: Champignon →
  Champignon de Paris, Shiitake → Shiitake, Morille → Morille conique, Truffe → Truffe noire), so
  `ItemInfo.Emoji` looks up `PictureKey(Key)`. The Nourrir menu shows them only once uploaded (no
  icon rather than 🍄 four times).
- **Shiitake is the one name a food and a collectible share** (collectible key still
  `col.lentin_chene`). Wherever they could meet — autocomplete, gifts, trades, sales — print
  `ItemCatalog.ClearName`, which suffixes « (nourriture) » / « (collection) » to shared names only.
- **A custom emoji costs ~35 characters where Unicode costs 2.** That's why `/inventory view` is two
  pages (`InventoryPage`, `inv:page:{page}`) — on one page, a full inventory reached ≈ 7 100 of the
  6 000 cap. **Measure anything new that lists many items with every icon uploaded.**

## `/inventory collection` — a book

An overview page, then one page per set picked from a **select menu** (overview + six sets is seven,
a row holds five buttons), with a Tout / Trouvés / Manquants filter row on set pages, and on a
completed set's page a row-2 « Déposer » button (see *Tiers and turn-ins*). State is in the
custom-ids under two verbs — `col:set:{user}:{filter}` (menu), `col:fil:{user}:{set}:{filter}`
(buttons) — handled by `InventoryComponentHandler`; every click re-reads the inventory. « Trouvés »
means *discovered*: an item traded away shows « plus en stock » and still counts. A rarity-split page
leaves the rarity out of each line so twelve lines fit in 1024.

## Cosmetics

**Cosmetics are items; what a Plynling wears is four keys on it.** `CosmeticCatalog` holds the 60
(`cos.<slot>.<name>`, stored, **append-only**) with slot data (a thème's accent and banner, a titre's
two genders, an accessoire's article, a cadre's two sides). Each is also an `ItemInfo` of
`ItemKind.Cosmetic`, so give, trade, autocomplete and inventory handle them unchanged; they belong to
no set. `Plynling.ThemeKey`, `TitleKey`, `AccessoryKey`, `GraveKey` are read and written only through
`CosmeticSlots`. Wearing uses nothing up; a dead row keeps its keys (its grave shows its cadre).

- **Losing the last one takes it off**: `InventoryService.TakeAsync` clears the key from the owner's
  *living* Plynling in the same unit of work.
- **Cosmetics cannot be sold** (`GiveOutcome.NotSellable`) — a buy-back price invites loops.
- On the card a thème costs one component (its banner); titre and accessoire ride in the heading
  text.

**The shop is a function of the week, not state.** `CosmeticCatalog.Shop(now)` = the basics, then
`RotatingPerSlot` (2) per slot drawn with `new Random(week * 10 + slot)` (seeded: same for everyone,
across restarts), then the season's items. The week is the Paris ISO week (`WeekKey`, `yyyyww`),
turning Monday 00:00 Paris. The buy select carries its week (`cos:buy:{week}`) and `BuyAsync` refuses
any other week, so a shop left open over the weekend can't buy what rotated out. The three screens are
`CosmeticCards` (static, measurable), ephemeral, redrawn in place by `CosmeticComponentHandler` with a
one-line notice. The wardrobe has one select per slot, `cos:wear:{slot}`, each led by « Aucun ».
