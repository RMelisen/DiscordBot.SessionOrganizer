# Minecraft orders — her free time

Part of her Minecraft orders: read `docs/agents/minecraft-orders.md` (the index, and the rules
that bind every order) first.

## Free time

Between orders she has her own life. **Own tasks** (`own: true`, no player) are body-only, like
eating: they hold no claim, report no order events, and any order ends one through `stay()`. She
counts as free during one (`isFree`), so she can still defend family, forage and go to bed. Running
away or defending someone never goes back to an own task: autonomy picks again by itself.

`autonomyTick` (once a second) starts nothing while she's asleep, eating, sitting, busy with an
order, **held** or in the **grace**:

- « reste » and « assis » hold her for **10 minutes**: no routine, tagging along or retrieval (body
  language still runs). Any other order that gets under way lets her go.
- Once an order is over she waits **45 s** before starting anything, so « viens » doesn't end with
  her walking off at once.
- **« quartier libre »** (« t'es libre », « fais ta vie », « tu peux y aller », « repos »,
  `McOrder.Free`, `FreePhrases`) gives her back her life at once: the order or activity ends, a
  « reste » hold is lifted (`letGo`) and the grace skipped (`skipGrace`). A stop like stay, so the
  double-send gate never holds it back. Not when a time word follows (`FreeLaterWords`: « t'es
  libre ce soir ? », « demain », « samedi »…): that's asking a friend, not freeing her. With no order,
  no pending one and no hold, she says she already is (`McFreeAlreadyLines`). « arrête de me suivre »
  stays stay; « tu peux arrêter de me suivre » frees her.

Then, in this order: getting her things back, the routine, tagging along, nothing. **What she
starts on her own gets a rare rolled remark** (the owner's call): the routine 25 %, tagging along
20 %. Getting her things back always gets one.

- **Routine (named places).** `McRoutine.Classify` (pure, in the checks) finds among the remembered
  places (Papa's first, then Tata's, then anyone's, kind by kind) a pen (an animal-pen word anywhere in the name: « ferme aux animaux »), a farm or a fishing
  spot (a farm or water word first: « champ de blé », « étang »), and home (`McRoutine.HomeKeys`,
  shared with « rentre »). The service sends them (`routine`) on spawn and after every save, move or
  forget. By day (time 0–11499) she works in turns at the farm and the fishing spot, **9 minutes in the fields, 6
  fishing** (`WorkBlock`), and **visits the pen twice a day** (the owner's call): in the morning (a visit may start
  from 0 to 2999) and before dusk (from 8000), **2 minutes each, counted from her arrival** (`PenSlots`). Twice a day
  breeds twice (an adult breeds again after 5 minutes, and yesterday's babies are grown) and shears twice. A visit's
  hour preempts a turn at the fields; a visit under way runs to its end whatever the clock says, and counts only once
  it has (`penDone`, by `bot.time.day`), so one an order cut short is made again while its hours last. With only a pen,
  she's free between the visits. With the clock stopped (`advance_time` off), the pen gets the visit of the hour it
  stopped at, once. Each turn starts with the order's own checks (crops and a chest, water and a rod, animals and a
  chest); her fishing is quiet (no catch remarks). At dusk (11500–12541) she goes home and stays there till
  morning; the night is `autoSleepTick`'s. A spot in another dimension or over **256 blocks** away is
  left out (her own trips are far shorter than an order's 1500); one that fails (no path, not reached
  in 3 minutes, nothing to do there) is left out for 10 minutes, silently, with a log line. A
  player's order always replaces her own activity, and then holds her under `McClaim` as usual. Her
  activities pause for errands and sleep like a player's.
- **Tagging along.** With nothing in the routine, she keeps near Papa, else Tata, when one is in
  sight: over 8 blocks away she walks up to 4, **without sprinting** unless they're over 16 off (her
  own `Movements`; `stay()` puts the usual one back), and every 15–40 s she takes a few slow steps
  around them. They go out of sight, and it ends silently. Family only: the twins are the only pools.
- **Getting her things back.** On death the body notes where, in which dimension, and whether there
  was lava; 3 s after respawning she goes back (`retrieve`, ahead of the routine) unless they're in
  another dimension, over 500 blocks off, in the void or in lava (`retrieveLost`). At the spot she
  sweeps the items within 8 blocks for 20 s (`RetrieveRadius`: a death scatters them up to 6 off,
  and they roll); her armour goes back on by itself. An order comes first;
  the walk waits until **5 minutes after the death**, when the items despawn. Still carrying things
  after respawning means the server keeps inventories: she doesn't go. Lines: `McRetrieveLines`
  (said at once), `McRetrieveEmptyLines` when nothing was left (always), `McRetrievedLines` and
  `McRetrieveLostLines` half the time.
- **Loot.** Where something she was fighting or culling dies is a loot spot for 30 s. Guarding a
  player or a place, between fights, she walks onto the items within 3 blocks of a loot spot, within
  6 of her and inside what she guards; one she can't reach is left. The ranch picks up its own drops.
  **She never picks up anything else**: a player's dropped items stay where they are.
- **Creeper warnings.** A creeper within 4 blocks of a player (within 32 of her) and behind them
  (over 100° off where they look) gets a warning, once per creeper; the player she guards is
  `guardTick`'s. One per player every 15 s (`_creeperGate`), said at once (`force`): a warning can't
  wait. `McCreeperBehindLines` has family twins.
- **Needs.** Every 10 s the body checks her kit (best sword, pickaxe, axe and shovel, rod, shears,
  bow): a tool at **10 %** durability or less is reported once (`wornOut`), again only for a new one;
  one reported worn that disappears without being handed over, stored or lost in a death `broke`.
  She also reports what she lacks (`lacking`): a rod for her own fishing, shears for sheep, food for
  the animals, arrows when guarding with a bow, room in her pockets. Wearing no armour is not a lack
  (the owner's call): she never asks for any. The service says each once every 10 minutes at most (`_needGate`, given back when the
  remark gates swallow the line). They're about the game, so the pools are shared.
