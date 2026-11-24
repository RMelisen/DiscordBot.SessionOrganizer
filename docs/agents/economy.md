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
instead of an error. It also retries a unique-index collision (two first spends of the day each
inserting today's `MemberDailyStat` or metric row): the re-run finds the row the other save committed.
Its work may run more than once: database work only, all reads inside.
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

**Only the jar counts (`Helpers/Economy/Patronage`) for the « Mécène » badge and `/leaderboard`'s
« Mécènes » view** (see `xp.md`; owner's call, 7.73.0: a patron gives to everyone, not to
themselves). `JarService` is `Patronage.AddAsync`'s only caller, beside its `spend.jar` line; like
`EconomyLog` it never saves and looks in `Local` first, and writes `CaillouxSpent` on `MemberXp` and
today's `MemberDailyStat` (the totals + buckets pair behind `/leaderboard`). **A new sink gets no
`Patronage` call** unless the owner says so. Before 7.73.0 every sink that leaves the economy
counted (shop, packs, meals, medicine, cosmetics, crafting, the `/pay` tax, bribes, dedications);
what they added stays in the totals, by the owner's choice. A jar gift creates a `MemberXp` row for
someone with no XP: `XpService.GetRankAsync` calls 0 XP unranked, and the activity readers (`/report`, « ça faisait
longtemps ») filter on XP, reactions and voice, never on `CaillouxSpent`.

**`/pay member amount`** (`PebbleService.PayAsync`, top-level in `EconomyModule`): 10 to 2 000; a 10 %
tax rounded up (`PebbleEconomy.PayTax`) is added on top, paid by the sender and burned, so the
recipient gets exactly what was typed. **Cap: 2 000 sent per sender per Paris day**, on the wallet
(`PayDay` + `PaidToday`, like the passive cap, `PebbleEconomy.PayRoom`) so the check and the transfer
save together under `Balance`'s token; it blunts funnelling from alt accounts. Checks run TooSmall →
OverCap → TooPoor. Self and bots are refused before the service runs. Logged as `spend.tax`; the
amount moved is not an economy flow and logs nothing.

**`/bribe favourite` and `/bribe shame category`** (`BribeService`, `BribeModule`): one price for both,
`Bribes.Price`: 100, doubled by every bribe the same person paid this Paris ISO week
(`CosmeticCatalog.WeekKey`; `BribeWeek` + `BribesThisWeek` on the wallet, like `PayDay`). Logged
`spend.bribe` (not « Mécène »). The chouchou lasts 24 h (`PebbleWallet.ChouchouUntil`,
extended from its end when bought again); Papa and Tata are refused before any price. **`ChouchouBook`**
(singleton) caches who is chouchou for `ChatterService` and `ReactionService`, which must never read
the database per message: it loads every `ChouchouUntil` once, lazily, and `BribeService` calls `Set`
after each purchase; a failed load answers « no » and retries on the next message. Erasing a shame hit
is `ShameService.ForgiveOneAsync` (see `shame.md`), run with `ExecuteUpdate` inside the bribe's
transaction, so each bribe is `BeginTransaction` … `SaveChanges` … `Commit` inside `EconomySave`.

**`/shoutout bid amount` and `/shoutout status`** (`ShoutoutService`, `ShoutoutModule`, home guild only:
the hello is only said there): an auction for a dedication in the morning hello. `Shoutouts.OpenDay`:
before 20:00 Paris a bid is for tomorrow, from 20:00 for the day after. Opening 50, each raise +10,
so no ties. One `ShoutoutBid` row per (guild, day), overwritten by each new leader; `Amount` is a
concurrency token, so two racing bids re-check. **The bid is held, not spent**: debited at once (the
difference when raising your own lead), the outbid leader refunded in the same save, nothing logged.
It is logged `spend.shoutout` only when the dedication is posted
(`MarkShownAsync`, called by `MorningGreetingService`; see `personality/ambient.md`). A day's
winner is posted on that day; a dedication carried over a missed hello waits for a morning nobody
won (`NextDueAsync`). `BidResult.Cost` is what the bid takes, so a refusal names the difference when
raising your own lead.

**`/jar give amount` and `/jar status`** (`JarService`, `JarModule`, rules in `Helpers/Economy/Jar`):
one `CaillouxJar` row per guild. Goals rise, `Jar.GoalFor(level)` = 1 500 + 500 per goal reached,
capped at 5 000; the overflow carries over, so one gift can reach several. A gift is one
`EconomySave` unit (wallet, jar, the giver's all-time `JarGift` total, `spend.jar`, `Patronage`);
`Filled` is a concurrency token, so racing gifts land one on top of the other. **A gift only fills
the jar; the rewards are carried out after it**: a reached goal leaves `Level > RewardsDone`, and
`JarModule` calls `ResolveNextRewardAsync` in a loop, one reward per save with `RewardsDone++`
(also a token, so two resolvers can't run one reward twice). A failure leaves the rest pending for
the next gift, never lost. The rotation (`Jar.RewardFor`, by goal index, modulo the enum's size):
double `/daily`, feast, giveaway. `JarReward` is never stored, so the rotation can change; the quiz
day it once held was dropped (owner's call, 7.73.0). Double `/daily` applies **from tomorrow**
(today's `/daily` may be claimed) as a day range on the jar (`DoubleDaily*`); a second unlock while
one is pending adds the day after (`Jar.Extend`). `PebbleService.ClaimDailyAsync` doubles the
rolled amount (`DailyResult.Doubled`). The feast feeds every living, unfrozen Plynling a truffle,
free and unlogged. The giveaway is a `Giveaway` row organised by the bot, with `PebblePrize` 150 for
3 winners, paid in the draw's save (`earn.giveaway`, see `scheduling.md`). Each unlock is a line in
the bots channel (`BotsChannel.ResolveAsync`, else where `/jar give` was used), followed there by the
giveaway's card; mentions off, and it ignores silence mode: a gift set it off.

## The `/work` grace

A `/work` (or `;work`) asked for within `PebbleEconomy.WorkGrace` (5 minutes) of the cooldown's end
is paid anyway, once per Paris day per person, opening on a `WorkGraceLines` pout above the usual
shift line. **`PebbleEconomy.WorkGraceApplies` is the one rule.** The day is stored on the wallet
(`WorkGraceDay`, Paris `yyyymmdd`) and stamped in the same save as the pay, under `Balance`'s
concurrency token, so two early `/work` at once grace once and a restart hands out no second one.
`LastWorkAt` becomes the graced time: the next cooldown counts from there, so the grace pays no
extra shift, it only moves the clock a few minutes earlier. Outside the window, or with today's grace
spent, the refusal is the plain one. The checks pin the rule (section 40).

## The `/work` DM reminder

Switched on and off with `/balance`'s button (`bal:remind:on` / `bal:remind:off`,
`EconomyComponentHandler`; the card is ephemeral, so the clicker is its owner). Stored on the wallet:
`WorkReminder` and `WorkRemindedAt`. **`PebbleEconomy.WorkReminderDue` is the one rule**: the
cooldown has ended and no DM went out since `LastWorkAt`, so each shift gets one DM, from `/work` or
`;work` alike. **Switching on stamps `WorkRemindedAt` only when `/work` is already available**
(`ReminderStampOnEnable`): a cooldown that ended before opting in never DMs, while switching on
mid-shift still gets this shift's. `WorkReminderService` (own 2-minute loop) filters the flag in SQL
and the dates in memory, then claims each wallet (`ClaimWorkReminderAsync`, re-checks and stamps,
through `EconomySave` since `Balance` is a token) before the DM, so a crash costs the DM, never a
second one. **Both stamps read the clock at the save, never earlier**: a `/work` running meanwhile
records the time it *started*, and an older stamp landing after it would hide that shift's DM. A closed DM is dropped, with no channel fallback; silence mode doesn't mute it (the
member asked). The paid `/work` footer hints at `/balance` while the reminder is off. The checks pin
the rule (section 40).

## `/daily` — the streak

Once per **Paris calendar day**, a claim pays cailloux and sometimes an item, both growing with the
streak of consecutive days; a missed day sends it back to 1. The numbers are all in
`Helpers/Economy/DailyStreak` (pure, pinned by the checks, section 44): base `10 + 5 per day`, a
spread of ±5 / ±10 / ±15 as the streak grows, clamped to 5–100; an item chance of 10 % rising by 2 %
a day to 60 %. **Only the chance grows, never the rarity**: the find is `ItemCatalog.DrawForage`
unchanged.

- **Days are compared as dates** (`AppTime.FromDayKey`), never as « 24 h ago »: on the 25-hour day
  23:30 minus 24 h is still the same day, and a claim then would break the streak.
- **A day she was offline is excused** (`DailyStreak.ExcusedDays`): 3 h or more of downtime that
  Paris day, overlapping records merged so time counts once. A gap made only of excused days
  continues the streak (+1); one unexcused day still resets it. Downtime is every `UptimeEvent`
  plus every `CleanStop` (updates, the add-on stopped — recorded by `PiHealthService` on the next
  start, in their own table so `/health` and the report don't count them as incidents). Read only
  when there is a gap, dates compared in memory.
- **The state is on the wallet** (`DailyDay`, `DailyStreak`, `DailyBest`) and the claim runs through
  `EconomySave` (`PebbleService.ClaimDailyAsync`): `Balance`'s token makes a double claim lose the
  race and be refused on retry, and the cailloux, the streak and the find land in one save
  (`InventoryService.GrantAsync` logs `item.found` and pays a completed set into the same wallet).
  The roll happens inside the work, so a retry rolls again.
- **`DailyBest` is the badge's counter** (« Assiduité », see `xp.md`): the best streak, never the
  current one, so a reset takes no tier away.
- Cailloux log under `earn.daily` (« /daily »).
- The reply is built once, `EconomyModule.DailyReply`, and shared with `;daily`: public, one of her
  lines on top (`DailyLines`: said on any day, so none assumes a streak or a reset; Papa, Tata and a
  chouchou get their twins through `BotResponses.DailyPool`), then two lines,
  « trouvé : » before the item because items carry no grammatical gender. A refusal is private.
  It's a command answer, so silence mode doesn't apply. No reminder, on purpose.

## `/admin dashboard` — `EconomyDailyStat`

One row per (guild, Paris day, metric), `Day` an int `yyyymmdd` so windows filter in SQL.
**`Helpers/Economy/EconomyLog.AddAsync` finds or creates the row (`Local` first) and never saves** — the
action's own `SaveChanges` carries it, so a refused action records nothing and a recorded one can't
be lost.

- **A new source or sink of cailloux, or a new Plynling action, needs its own `EconomyLog` call and
  metric key. Metric keys are stored — append-only.**
- A pop quiz win pays under `earn.quiz` (« quiz »), inside `QuizService.RecordWinAsync`'s
  transaction (see `quiz.md`), and so does the weekly crown's purse (`QuizService.PayCrownAsync`):
  same source, no new metric key. Quiz wins grant no XP.
- Plynling events pay through `GiveCailloux` under `earn.event` (« événements » on the dashboard), and
  their `GiveItem` finds count as `item.found` like every other find.
- Admin adjustments record what actually moved (after the clamp), not what was asked.
- `/arcade` games log net flows under `earn.arcade` / `spend.arcade` (« arcade »), through
  `ArcadeService.SettleAsync` only — see `arcade.md`.
- The `/pay` tax logs under `spend.tax` (« taxe »), collection packs under `spend.pack` (« sachets »), bribes under `spend.bribe`
  (« pots-de-vin »), posted dedications under `spend.shoutout` (« dédicaces »);
  a pack's finds count as `item.found`, its completed sets as `item.set` + `earn.collection`.
- The jar logs under `spend.jar` (« bocal »), lottery tickets under `spend.lottery` and the
  lottery's payout under `earn.lottery` (both « loterie »), a giveaway's cailloux under
  `earn.giveaway` (« giveaways »).
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
redraws it with ×1/×5/×10 buttons `shop:buy:{key}:{qty}`, disabled when unaffordable. A purchase
that goes through redraws the card clean and confirms in its own ephemeral follow-up
(`ShopCards.Receipt`, owner's call: the redrawn card alone went unnoticed, and the next click erased
it); a refusal (too poor, unknown) is a notice on the card. Not a confirmation *before* buying. `InventoryService.BuyAsync(key, qty)` refuses any key not in
`Shop`, so a forged custom-id can't buy a collectible. A new buyable item is a line in `Shop`, not
a new subcommand.

**Collection packs** are the shop card's other line: `ItemCatalog.PackKey` is the select's value
and **never an item** (nothing called a pack is ever held). Picking it shows ×1/×5 buttons
`shop:pack:{qty}` (their own verb); `InventoryService.OpenPackAsync` takes `PackCost(qty)` (the
shop's `BulkPrice`, so ×5 is −10 %) and grants `PackSize` (3) `DrawCollectible` finds each through `GrantAsync`, in one
save, so a pack that completes a set pays it at once. Rolled inside the work, so a retry rolls
again. **`PackPrice` (150) is set by the checks' simulation, not by feel**: a find sells for ~4.5,
but set rewards (100 to 1 000, paid again on every turn-in) make it worth more; section 52 plays
5 000 packs with the best possible player and fails if a pack returns more than 70 % of its
discounted price by five (135). Re-run it after changing a set's reward, the rarity split or the catalog.

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

## Forage places

`/plynling forage place:` and `;forage <place>` steer a forage (`Helpers/Economy/ForagePlaces`, checks
section 60). Five places of `docs/village-lore.md` § 5: la Sommeille, la Lisière, les Mouilles, le
Communal, la place du Puits. Keys are code-only, **never stored** (no migration); the typed aliases are a
contract with players — add, don't rename, and no alias may reach two places.

- **A place belongs to the item.** A set has default places (`SetPlaces`), an item may override them
  (`ItemPlaces`; Saisons has no default, each item says where it grows). **A new collection only lists
  its places**; the checks fail on a collectible with none, and on a place with no all-year commun in
  some season (an aimed forage must always be able to find something).
- **No place → exactly today's draw.** `ForagePlaces.Draw(rng, now, null)` calls `DrawForage` with the
  same random numbers (checked seed by seed), then names one of the item's places for the story (any
  place for food). So the 60 % mushroom share of a placeless forage, and the Champignons economy, stand.
- **A place → a tilt.** Same food chance; then `Tilt` (¾) of the collectibles come from the place's
  findable items, through the shared rarity roll `ItemCatalog.DrawAmong` (fallback to communs), the
  rest from `DrawCollectible`. The odds live in `DrawAmong` only.
- The reply is one pool per place (`BotResponses.PlynlingForaged`, `{0}` name, `{1}` the find after a
  colon: it must read for food too). The gift, games, visits and packs stay unaimed.
- Where things are found shows on each book page (one line under the progress bar) and on every
  recipe ingredient (`ForagePlaces.Whereabouts`: rarity, place, season).

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
- Every other collectible, the six sets and every cosmetic have icons drawn in the same style:
  16×16 text grids in `tools/item-art/icons.py`, in the pack's palette (Pear36 + eight pack
  colours), exported ×8 to `ProjectSYNCS/Assets/Icons/<key>.png` (`<key>` = item key or
  `set.<set key>`).
- **Plynling traits and badges are the exception: 22×22, exported ×4 (88 px)** — traits as CK3
  tiles (`trait.<trait key>`), badges as a tier shield around a subject (`shield()`; a set's badge
  is its set icon on the shield). 22 is Discord's inline emoji size, so they show one art pixel per
  screen pixel. Keep items at 16: the mushroom pack is fixed at that size.
- **`ApplicationEmojiService`**, on the first Ready, lists the application's emojis, uploads any
  missing sprite (from both folders via `ItemEmojis.Sources`), and records the markup in the static
  `ItemEmojis` map. Names: mushrooms `shroom_<slug>`, the rest `ItemEmojis.EmojiName`'s short
  prefixes (`c_`, `s_`, `th_`, `ti_`, `ac_`, `gr_`, `au_`, `tr2_`, `b_`) — Discord caps names at 32 chars. Each
  application (dev, prod) gets its own copy with no manual step.
- **An existing emoji is reused by name, never replaced** — to change a picture, delete the emoji in
  the developer portal and restart. To redraw a few icons, add their keys to `ItemEmojis.Revisions`
  (name + `_v2`, bumped on each redraw); to redraw a whole family, change its prefix instead (traits:
  `tr_` → `tr2_`) **and add the old one to `ItemEmojis.RetiredPrefixes`**. Either way the new names
  upload on their own. The checks verify every revised key is a shipped sprite with a valid name.
- **Stale emojis are deleted on start**, right after the sync: `ItemEmojis.StaleNames` is every
  application emoji named with one of the bot's prefixes (today's, `shroom_`, or retired) that no
  shipped sprite claims — earlier revisions, old family prefixes, retired items. A name outside
  those prefixes is never touched, so **don't upload a hand-made application emoji whose name
  starts with one** (`c_`, `s_`, `th_`, `ti_`, `ac_`, `gr_`, `au_`, `tr_`, `tr2_`, `b_`, `shroom_`).
  Nothing is deleted when a sprite folder is missing or yields no sprite, so a broken publish can't
  wipe the list. Messages already posted with a deleted emoji show its `:name:` instead.
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
means *discovered*: an item traded away shows « ×0 » and still counts. « Déposer » shows only when a
turn-in is possible. A completed set's bar counts what is in hand toward the next turn-in, Diamant
included (a turn-in still pays). A rarity-split page
leaves the rarity out of each line so twelve lines fit in 1024.

## Cosmetics

**Cosmetics are items; what a Plynling wears is five keys on it.** `CosmeticCatalog` holds the 97
(`cos.<slot>.<name>`, stored, **append-only** — three retired, see below) with slot data (a thème's accent and banner, a titre's
two genders, an accessoire's article, a cadre's two sides). Each is also an `ItemInfo` of
`ItemKind.Cosmetic`, so give, trade, autocomplete and inventory handle them unchanged; they belong to
no set. `Plynling.ThemeKey`, `TitleKey`, `AccessoryKey`, `GraveKey`, `AuraKey` are read and written only through
`CosmeticSlots`. **`CosmeticSlot` is append-only**: the shop seeds each slot's draw on its number. The
order players see is `CosmeticCatalog.SlotOrder` (Thème, Titre, Accessoire, Aura, Cadre); the Garde-robe's
slot row is then 5 buttons, a row's maximum — a sixth slot needs a select there. Wearing uses nothing up; a dead row keeps its keys (its grave shows its cadre).

- **Losing the last one takes it off**: `InventoryService.TakeAsync` clears the key from the owner's
  *living* Plynling in the same unit of work.
- **Cosmetics cannot be sold** (`GiveOutcome.NotSellable`) — a buy-back price invites loops.
- On the card a thème costs one component (its banner); titre and accessoire ride in the heading
  text — and the accessoire is also **drawn on the Plynling's picture** (8.5.0, `PlynlingArt.SpriteOf`,
  see `plynling.md` *Art*).
- **Accessory styles** (`AccessoryStyles`): some accessories have one or two choices — a colour (nœud,
  fleur, écharpe, casquette, ballon, moufles), a place at the neck (écharpe, collier, médaillon) — whose
  first option is the default. The picked options are one string on the Plynling, `AccessoryStyle`
  (« bleue_haut », null = default), written only by the Garde-robe's style selects
  (`CosmeticService.StyleAsync`, free) and **cleared whenever the accessory changes**
  (`CosmeticSlots.Set`, so a take-off by `InventoryService.TakeAsync` clears it too). `Outfit` carries
  it, and a try-on of another accessory shows that one's default. Option keys are stored:
  append-only, never renamed, never containing « _ »; a new option needs its drawing first (checks
  section 62 compares the two).
- **Auras** (8.6.0, `cos.aura.*`): an animated effect drawn around the Plynling, worn **with** the
  accessory — `Outfit.With`/`Without` on one never touch the other. Three, fixed by design: Scintillements
  (the look the rarer species lost in 8.5.1, now for anyone) and Bulles in the weekly rotation — with two
  rotating auras and `RotatingPerSlot` 2, both are on sale every week until more exist — and Pétales, a
  *Chef-d'œuvre* from primevères and cerises, **spring-only** (a recipe mixing seasons can't be measured by
  section 61, which simulates one season). Aura has no *Ouvrage*; section 61 exempts the slot. `Label` is
  « Aura X » like « Thème X »; the card says « 🫧 Entouré de bulles » under the accessory line. Not drawn on
  the sick and frozen faces: the try-on then says « Son aura ne se voit pas tant qu'il est malade ou gelé. »
  Art and file cost: `plynling.md` *Art*.
- **Retired cosmetics**: the sac à dos and the casque audio left the catalogue in 8.5.0; the data
  migration `RetireBackpackAndHeadphones` refunded 1 200 cailloux a copy and took them off. The
  Citrouilles cadre (`cos.grave.citrouilles`, autumn) left in 8.8.0, the owner's call;
  `RetirePumpkinGraveFrame` refunded 2 400 (the seasonal price) the same way and cleared `GraveKey`
  on any Plynling, dead or alive. To retire another, copy one of those migrations. Their keys stay
  retired — never reuse them.

**The shop is a function of the week, not state.** `CosmeticCatalog.Shop(now)` = the basics, then
`RotatingPerSlot` (2) per slot drawn with `new Random(week * 10 + slot)` (seeded: same for everyone,
across restarts), then the season's items. The week is the Paris ISO week (`WeekKey`, `yyyyww`),
turning Monday 00:00 Paris. The shop select and the buy buttons carry their week and `BuyAsync`
refuses any other week, so a shop left open over the weekend can't buy what rotated out.

**One screen, three tabs** (`CosmeticCards`, static and measured by the checks, section 59): Garde-robe,
Boutique, Fabrication, in one ephemeral Components V2 message. `/inventory cosmetics` opens it on the
shop, `/plynling wardrobe` on the wardrobe (`/inventory craft` was folded into it in 8.5.0). Every
click defers, then `CosmeticComponentHandler` re-reads everything (`CosmeticCards.LoadAsync`) and
redraws in place.

- **The try-on is the real card head.** `PlynlingModule.CardHead` (colour, banner, heading, sprite)
  is shared by the card and the screen; the screen passes a copy of the `Outfit` with the picked item
  put on (`Outfit.With`), so a preview never writes to the tracked Plynling. Anything that changes
  how a worn cosmetic shows goes through `Outfit`, so the card and the try-on stay identical. A cadre
  previews as the grave's first line (`PlynlingCardUi.GraveFirstLine`), since a living Plynling has
  no death date for `GraveLine`.
- **A wardrobe pick is worn at once** (wearing is free); a shop or recipe pick is only tried on, and
  opens the item view: « Acheter » / « Acheter et porter », « Fabriquer » / « Fabriquer et porter »
  (disabled until affordable or ready, a recipe naming what it lacks: `CosmeticCards.Missing`), or
  « Porter » for one already owned. **« …et porter » wears it in the purchase's own `EconomySave`
  unit** (`CosmeticResult.Worn`); a Plynling that died meanwhile leaves the purchase standing, worn
  by nobody, and the notice says so.
- A purchase that goes through redraws clean and gets the same ephemeral receipt as
  `/inventory shop` (`ShopCards.Receipt` around `CosmeticCards.Bought`). A craft keeps its notice on
  the card: it is not a purchase.
- **Custom-ids use the `cosm:` prefix**, one verb per control: `cosm:tab:{tab}`, `cosm:slot:{slot}`,
  `cosm:pick:{tab}:{week}` (the shop's select; the wardrobe's « Aucun » is `none:{slot}`). **A select
  holds 25 options**: the Fabrication tab has one select per group (`:0` Ouvrages, `:1` Chefs-d'œuvre)
  and a wardrobe slot splits « Aucun » + what is owned into selects of 25 (`:0`, `:1`, … — titles and
  cadres pass 25); the handler reads that segment only on the shop. Checks section 59 measures it with everything owned.
  `cosm:buy`/`cosm:buyw:{week}:{key}`, `cosm:make`/`cosm:makew:{key}`, `cosm:wear:{tab}:{key}`,
  `cosm:back:{tab}`, `cosm:style:{choice}:{key}` (the worn accessory's colour or place, on the Accessoire
  slot; refused if that accessory is no longer worn). The view (tab, slot, picked key) lives only in the
  ids, so a restart loses nothing.
  The old screens' `cos:` ids answer nothing, on purpose: a new prefix keeps every wildcard clear of them.
- Without a living Plynling the screen still opens: shop and recipes work, the head is a plain title,
  « …et porter » and « Porter » are hidden, and the wardrobe lists what is owned.

### Crafting — Ouvrages and Chefs-d'œuvre

27 recipes, each made **once per person** (`AlreadyOwned`). `CosmeticInfo.Trophy` splits them, and the
Fabrication tab lists them in that order:

- **Ouvrages** (14: Thème 2, Titre 5, Accessoire 3, Cadre 4, ~100 cailloux; the per-slot counts are pinned
  in `CraftingChecks.OuvragesPerSlot`): commons (at most one peu commun) found at **one place**, every
  place served, for the duplicates. Three titles are named after their place (Sommeillant, Mouillé,
  Puisatier). **Band: 10–25 aimed forages.**
- **Chefs-d'œuvre** (13, 200–300 cailloux): aimed hunts. **Band: 60–150 aimed forages.**
- **The bands are enforced** (checks section 61, `CraftingChecks.ExpectedForages`: 300 seeded runs from
  nothing, aiming each time at the place covering the most missing units, in the ingredients' season or
  mid-April). A recipe outside its band fails the checks: retune it, never widen the band to fit.
- **« Any commons of a set »** is an ingredient (`Recipes.AnyCommon(set)`, key `any:<set>`, never stored):
  any of the set's communs, counted together. La Lisière's Ouvrage must use it (12 common mushrooms
  dilute any named one); le Communal's two do too. **A craft takes from the largest stack, one unit at a
  time** (ties: catalog order), so the single copies a turn-in needs go last. All ingredient logic lives
  in `Recipes` (`Have`, `Take`, `Lacking`, `ItemsReady`, `Where`): the craft, the tab and the checks
  share it.
- **« Il a tout pour fabriquer… »** — a forage whose find completes a recipe's items (not owned, not
  ready before) adds a line to the reply (`Recipes.NowReady`, `PlynlingText.NowReady`). Forage only;
  cailloux never announce one.
