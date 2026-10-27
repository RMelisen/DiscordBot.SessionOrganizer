# Minecraft: « tu fais quoi ? », free time, fetching, crafting, stats by month, a split doc

The owner's batch of 2026-10-27, on its own branch. Six pieces, built in this order; horses were
dropped from the batch (the owner's call, « for now »).

Read `docs/agents/minecraft.md` first: every rule there holds unless this document says otherwise.

## Why

- In game, « syncs tu fais quoi ? » falls through to Discord's intent lines (her chores, her
  nights), never to what she is actually doing in front of them.
- Once she follows someone, the only way to send her back to her own life is « reste » (which
  holds her 10 minutes) or waiting out the 10-minute claim. The owner wants to let her go at once.
- She puts things in chests and crafts for her camp, but can't fetch from a chest or craft for a
  player, two things players ask a companion for.
- `/minecraft stats` has all-time totals only, so the owner's monthly digest can't say what
  happened this month in game.
- `minecraft.md` is 1,219 lines, past what one read takes in.

## Goals

1. « tu fais quoi ? » in game is answered from her real state, orders and her own life alike.
2. A **free-time order** lets her go back to her own life at once.
3. A **fetch order** brings a player goods from the chests near her.
4. A **craft order** makes a player an item, tools and weapons included, from what she carries,
   gathering wood and stone first when short.
5. Her Minecraft counts get **daily buckets**, a **`/report minecraft`** card, and that card in
   **Papa's monthly digest**.
6. `minecraft.md` is **split** into files a reader can take in whole.

## Non-goals

- Horses (dropped for now). No mounts of any kind.
- No orders, lines or reminders from Discord into the game (the owner's standing call).
- No cailloux or XP for anything done in game.
- Crafting armour, potions, food or redstone. Tools, weapons and a fixed list of everyday items only.
- Remembering which chest holds what. Each fetch looks.
- Tata's digest gets no Minecraft card (the owner asked for *his* digest).

## Anti-goals (a failure even if it works)

- **Words in the body.** Every new line is a `Mc*` pool in `BotResponses.Minecraft.cs`.
- **A false order.** « t'es libre ce soir ? » to a friend freeing her, « je vais chercher du fer
  dans le coffre » (talk about oneself) sending her to a chest. A missed order costs a retype.
- **Taking her from someone.** Free time, fetch and craft go through `McClaim` like every order.
- **Building.** The one new placed block is the crafting table below; nothing else.
- **Her inventory out of step with the server's** (`toss` with a count, window clicks while moving).

## 1. « tu fais quoi ? »

- **When.** The message is for her by the usual talk rules (her name, a whisper, or within the
  30 s after she answered that player) and reads as Discord's « tu fais quoi » intent (the same
  cues, `MessageCues`, so « tfq », « tu fous quoi » count). It is answered **instead of** the
  intent lines, and before the rest of the ladder. Answered at once, taking her from nobody, like
  « t'es où ? ». Whispered, answered in a whisper.
- **What she says** is her state, in the first person, from one pool per state:
  - an order under way: what and for whom (« je suis Papa », « je pêche pour Bob »), the whose
    rendered by `Who`. **Asked by someone else than the order's player, a whispered order keeps its
    place and coordinates to itself**, as `McDoing.ForStatus` does on Discord;
  - her own life: at the farm, fishing, at the pen, home for the night, tagging along after Papa or
    Tata, feeding herself, getting her things back;
  - fighting, falling back, putting out a fire;
  - in bed; held by « reste » / « assis »;
  - nothing at all.
- **Where the state comes from.** The service knows the orders; only the body knows her own life.
  The body reports **what she's doing** (a short kind, plus the player or spot when there is one)
  each time it changes, an event the service keeps (like `inventory`). No round trip at the moment
  of the question.
- **`/minecraft status` uses the same report**, so it also says « elle pêche (sa routine) »
  instead of « elle ne fait rien de spécial » when she's on her own.
- The pools are shared (about the game), with `{0}` the whose. *(Builder's call: twins only if
  a line ends up addressed to Papa or Tata themselves.)*

## 2. Free time

- **Phrases** (a new `McOrder.Free`): « t'es libre », « tu es libre », « quartier libre »,
  « fais ta vie », « vis ta vie », « c'est bon tu peux y aller », « tu peux disposer », « repos »,
  « rompez », « dismissed », « go play », and the ones found in the wild later. **Not** « vas-y »
  or « libre » alone (too common), and not « t'es libre ? » as a question with a time after it
  (« t'es libre ce soir ? », « t'es libre demain ? »): a question about her evening is talk.
  Read before stay (« tu peux arrêter de me suivre » is the same wish; *builder's call* whether it
  joins the list).
- **Who.** Like any order: whoever holds the claim, or family over them. Anyone else gets
  `McBusyLines`.
- **What it does.** The body ends what she's doing (`stay()`), lifts a « reste » / « assis » hold
  (`letGo`), and **skips the 45 s grace**, so `autonomyTick` picks her own life on its next tick.
  Any order she was on ends, an activity (a farm, a pen, a patrol) too. The service drops the
  claim and `_order`.
- **Lines.** `McFreeLines` with Papa's and Tata's twins (an ack aimed at the person, like follow's).
  Already free (no order, no hold) gets `McFreeAlreadyLines`. Acks follow the usual 3-minute rule.
- **Mid-fight**, it waits like any order other than come/follow (`holdForFight`).

## 3. Fetching from a chest

- **Phrases.** A fetch verb (« ramène-moi », « va chercher », « sors-moi », « apporte-moi »,
  « file-moi ») with goods and a chest word (« du coffre », « dans le coffre », « des
  tonneaux »). A store verb with a chest word stays Store (« range ça dans le coffre »). Without a
  chest word, the existing readings stand (« va chercher du fer » gathers, « donne-moi ton fer »
  gives). Talk about oneself is not an order (« je vais chercher du pain dans le coffre »).
- **Goods** are the kinds `give()` already hands over (`Goods`, the same tags « t'as du fer ? »
  counts by): food, fish, wood, iron, coal… so « donne-moi ton fer » and « ramène-moi du fer du
  coffre » mean the same iron.
- **Count**: a number when given (`McCommand`'s count reading), else everything of that kind she
  finds, **up to 64**.
- **Whose chests** (the owner's call): **any** chest or barrel, with no owner check, within 16
  blocks of her **or** of the player asking (the radius `store` uses). Anyone may give the order.
- **Body.** Those chests, closest to her first; she walks to each, opens it, withdraws what
  matches until the count is met or her pockets are full, then walks back to the player and tosses
  it, **whole stacks only**. *(Our call, from the builder check: what she fetched may merge into a
  stack she already carried; she then hands over the whole stack, her own few included. Splitting a
  stack is what puts her inventory out of step on 26.1. The count in her lines is what she took from
  the chests.)* A chest is never a target to break.
- **Outcomes.** None of it in any chest: `McFetchNoneLines`. No chest within 16:
  `McFetchNoChestLines`. Fewer than asked: delivered with `McFetchShortLines` ({1} how many).
  These are refusals and outcomes: never rolled, never held back.
- **An errand**, like Give: it pauses an activity, which comes back after. A fight drops it and
  tells its player (`dropped`), like the other errands.
- The check that it can be done (a chest in reach) comes **before** `stay()`, so a failed fetch
  doesn't pull her off someone's follow.

## 4. Crafting on demand

- **Phrases.** A craft verb (« fabrique-moi », « craft », « crafte-moi », « fais-moi », « tu peux
  me faire ») and an item from the craft table below, with an optional count (« fais-moi 16
  torches »). « fais-moi » needs the item word right after it (« fais-moi peur » is not an order).
  The item words come from the same hand-written table as `McItems` (singular, plural, gender), so
  her answers name it right.
- **What she can make** (the owner: « tools too »): pickaxe, axe, shovel, hoe and sword in wood,
  stone, iron, gold and diamond; a bow, a fishing rod, shears; planks, sticks, a crafting table, a
  chest, a furnace, torches, a ladder. A bare tool word (« une pioche ») means the best material
  she has enough of; a material named is that one or a refusal. *(Changed while building: never gold
  for a bare tool, which wears out faster than stone and would spend the player's gold unasked.)*
- **Count.** Tools: 1 (at most 4). Everything else: a number of items (« 16 torches »), crafted in
  whole recipes, at most 64; no number means one recipe's worth (4 torches, 4 planks, 3 ladders).
- **Materials.** From her pockets. **Short of wood or cobblestone, she gathers it first**, the
  camp's way (`campNeeds`: wood, then stone with a wooden pickaxe she makes for it), within the
  gathering radius. Anything else short (iron, diamonds, gold, coal, string) is a refusal naming
  what's missing (`McCraftMissingLines`, {1} the material), **before** `stay()`.
- **The table.** A recipe that needs a crafting table uses one within 16 blocks. With none, she
  places **her own** beside her (crafting it first if needed), crafts, and **breaks it and takes
  it back** right after. This is a new exception to « she never places a block », as narrow as the
  bridge's: one block, beside her, gone when the craft ends. A fight that pauses the craft comes
  back to the same table and takes it back at the end. *(Changed while planning: another order
  that takes her away mid-craft leaves the table standing, with a log line, as the tree pillar
  does. Taking it back first would make that order wait.)* One that can't be broken (someone
  stands there) also stays, with a log line.
- **Crafting** goes through `craftOne` (recipe book first, `bot.craft` as fallback): each craft
  counts once the item is in her pockets.
- **An errand** *(our call)*, like Give and fetch: a farm, pen or patrol pauses and comes back
  after (`pausedActivity`, `McJob.Resume`), and the claim goes back to the activity.
- **Delivery.** At the player's feet, whole stacks (a stack merged with what she carried goes whole,
  as for fetch). **She keeps nothing she crafted for someone**,
  a weapon included: the combat non-goal « she doesn't arm herself » stands. The wooden pickaxe she
  made to get stone is hers (it's a tool for her own gathering, as at the camp).
- **Restartable** like the camp: a fight or a creeper pauses it, and it picks up with what she
  already has (`resumeFor`).
- **Lines.** Ack when under way (`McCraftLines`), done (`McCraftDoneLines`), missing (above),
  an item she can't make (`McCraftUnknownLines`, read by the service from the parse, no body
  round trip), no way to gather what's short (`McCraftNoWoodLines`).

## 5. Stats by month, `/report minecraft`, Papa's digest

- **Daily buckets.** A new `MinecraftDailyStat` (`Day` as `AppTime.DayKey`, `Key`, `Count`),
  written by `AddStatsAsync` in the same save as the totals (the project's *totals + daily buckets*
  pattern; the buckets don't sum to the totals and mustn't be made to). The day is the flush's
  day. One migration (minor bump).
- **`ReportKind.Minecraft`**, appended (the names are in custom-ids). `/report minecraft`, with the
  period buttons every report has. A pure `MinecraftReport` in `Helpers/` computes; `ReportCards`
  renders; the checks measure the card at its worst (long player names, many resources).
- **The card**: time in game (hours) and the number of days she played; orders (total and the top
  players, by in-game name, sanitized); deaths; monsters killed; fish and treasures; what she
  gathered (top resources, the rest through `ReportCards.Fit`) and harvested. Trends against
  `ReportWindow.Previous` like the other cards. An empty window says she didn't play.
- **Papa's digest** gets the card after his others (`ReportKinds.OwnerDigest`), **left out for a
  month she didn't play**, and his « Le mois en bref » gains one figure: hours in game. Nothing
  added to « À regarder ».
- `/minecraft stats` is unchanged (all-time totals).

## 6. Splitting `minecraft.md`

Into three files, each readable whole, cross-linked:

- `minecraft.md`: the code's layout, two processes and one voice, joining and leaving, failures,
  her remarks, family, `/minecraft`, deployment and the version workarounds.
- `minecraft-orders.md`: orders (this batch's included) and free time.
- `minecraft-body.md`: water, fights, body language, arriving, movement on the server and testing
  locally (`tools/mc-pathtest`).

The CLAUDE.md table points at all three. Content moves as is; only the new sections are new text.

## Release

- **Base**: branch `minecraft-orders-stats`, in its own worktree, from `main` at `f8c4e7f` (the
  `7.44.0` batch, committed). Numbered **`7.45.0`** (minor: new orders and a migration), one
  CHANGELOG entry, in English, no member names. Its migration stacks after `AddMaybeNudgeSent`.
  *(Changed while building: `main` shipped 7.45.0 to 7.49.0 meanwhile, so this batch is `7.50.0`.)*
- `/minecraft help` gains an example for free time, fetch and craft (the checks parse every
  example); measure its pages. `/help` and `README.md` get `/report minecraft` and the new orders.
- Every new phrase gets cases in `tests/ProjectSYNCS.Checks`, misreadings included (« t'es libre
  ce soir ? », « je vais chercher du pain dans le coffre », « fais-moi peur »).
- The body's new orders are checked on the local server (`tools/mc-pathtest`, an `orders` spot
  each: a fetch, a fetch with nothing there, a craft with a table near, one without, one gathering
  wood first, a free-time order during a follow), when the owner's local server is available;
  otherwise the release notes say they weren't.

## Left to the builder

The exact phrase lists beyond those named, the pool texts (in `docs/syncs-voice.md`'s voice, with
`--mc-review`), the body's event names, where in `McCommand`'s order each reading goes (keeping the
documented precedences), and the report card's layout within Discord's caps.
