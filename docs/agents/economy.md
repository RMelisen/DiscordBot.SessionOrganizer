# Economy — cailloux, inventory, items, cosmetics, dashboard

`/work`, `/balance` (`EconomyModule`), `/inventory` (`InventoryModule`, its own group — belongings
outlive the creature). `/plynling forage` stays under `/plynling` (it's something the Plynling
*does*); only its find lands in the inventory.

## Cailloux (pebbles)

**`PebbleService.GetOrCreateWalletAsync(context, …)` never saves and looks in `Local` first.** A
wallet created earlier in the same unit of work isn't in the database yet; creating a second breaks
the unique index at save time (a first-time feeder whose meal also earned a badge hit this).
`PebbleService.AdjustAsync` (admin) clamps at 0.

**Passive cailloux are granted from `XpTracker.GrantAsync`, never detected separately.** Every XP
grant pays `PebbleEconomy.PassivePerGrant`, capped per day at 30% of three average `/work` shifts —
so the cap inherits every XP defence (cooldowns, excluded channels, voice eligibility) and can never
pay for something XP refused. Own `try`, so it can't swallow the level-up card. **Plynling actions
grant no XP** — the link runs one way, or money and levels would feed each other.

**The passive cap lives on the wallet row** (`PassiveDay` + `PassiveToday`), not in a daily-bucket
table: nothing ranks cailloux by date, so a buckets table would have no reader.

## `/admin dashboard` — `EconomyDailyStat`

One row per (guild, Paris day, metric), `Day` an int `yyyymmdd` so windows filter in SQL.
**`Helpers/EconomyLog.AddAsync` finds or creates the row (`Local` first) and never saves** — the
action's own `SaveChanges` carries it, so a refused action records nothing and a recorded one can't
be lost.

- **A new source or sink of cailloux, or a new Plynling action, needs its own `EconomyLog` call and
  metric key. Metric keys are stored — append-only.**
- Admin adjustments record what actually moved (after the clamp), not what was asked.
- No totals row: the stored balances already are the totals (`/admin stats` reads those). Nothing
  was backfilled.
- Window buttons `dash:win:{Week|Month|All}` in `AdminComponentHandler`, which re-checks `IsStaff`.
  « Tout » has no trend; its sparkline sums days into at most 30 buckets to stay on one line.

## Inventory

**It belongs to the person, not the Plynling.** `InventoryItem` rows are keyed on (guild, user,
`ItemCatalog` key) and survive every death and abandonment. **Item keys are stored — append-only.**
A row at quantity 0 is **kept**: it is what makes an item *discovered* for good, which is why a set
completes on discovery (not on holding all at once) and why trading or selling never undoes a set.
`CollectionCompletion` (unique index) is the once-only guard on a set's reward.

**`InventoryService`'s static `AddAsync` / `TakeAsync` / `GrantAsync` take a context and never
save**: every source (pantry feeding, gift, game, visit, forage) moves items in its **own** unit of
work, so the action and its items land in one `SaveChanges`. `AddAsync` pays a completed set into the
wallet in that same save. They check `Local` first, so two adds in one save don't create two rows.

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

## Item icons — application emojis

**The pictures are application emojis** (owned by the bot's application, so unlike reaction emotes
they work in any server), and **the bot uploads them itself**.

- Mushroom sprites ship in `ProjectSYNCS/Assets/Mushrooms/<slug>.png` (128 px, ×8 from the pack's
  16 px without smoothing; copied to output and publish by the csproj). File names are the item keys
  (`col.<file name>`) — the harness checks both ways. `CREDITS.txt` is the pack's list with Latin
  names.
- Every other collectible, the six sets and every cosmetic have icons drawn in the same style: 16×16
  text grids in `tools/item-art/icons.py`, in the pack's palette (Pear36 + eight pack colours),
  exported ×8 to `ProjectSYNCS/Assets/Icons/<key>.png` (`<key>` = item key, or `set.<set key>`).
- **`ApplicationEmojiService`**, on the first Ready, lists the application's emojis, uploads any
  missing sprite (from both folders via `ItemEmojis.Sources`), and records the markup in the static
  `ItemEmojis` map. Names: mushrooms `shroom_<slug>`, the rest `ItemEmojis.EmojiName`'s short
  prefixes (`c_`, `s_`, `th_`, `ti_`, `ac_`, `gr_`) — Discord caps names at 32 chars. Each
  application (dev, prod) gets its own copy with no manual step.
- **An existing emoji is reused by name, never replaced** — to change a picture, delete the emoji in
  the developer portal and restart.
- **Emoji properties are computed**: `ItemInfo.Emoji`, `CollectionSet.Emoji`, `CosmeticInfo.Emoji`,
  a thème's `Banner` and a cadre's `GraveLeft`/`GraveRight` use the uploaded icon, else the stored
  `DefaultEmoji` — so nothing breaks before or without the upload.
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
a row holds five buttons), with a Tout / Trouvés / Manquants filter row on set pages. State is in the
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
