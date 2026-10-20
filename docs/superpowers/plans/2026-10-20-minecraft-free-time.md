# Plan: SYNCS's free time in Minecraft (7.18.0)

## Context

In Minecraft she does a lot when someone gives her an order, but once she's free she just stands there. This batch
gives her a life between orders:

- body language
- tagging along with family
- a daily routine built on named places
- going back for her stuff after dying
- warning players about creepers
- pointing out worn tools and what she lacks
- looking after animals (`ranch`)
- picking up loot from her kills

Owner decisions:
- **The routine comes from named places.** A farm word, a water word, an animal-pen word or a home word, saved with
  « retiens ici ».
- **The routine comes first.** She tags along with Papa or Tata only when the routine has nothing for her to do.
- **Animals:** she breeds and shears, and kills surplus adults over **8 per species**. She never kills a named animal
  or a baby.
- **Things she starts on her own get a rare rolled remark.** Retrieving her stuff always gets a line.

Every existing rule stays: she never digs or places blocks (except seeds), the body holds no words and no policy
beyond self-care, every line comes from `BotResponses.Minecraft.cs` through `ResponsePicker`, and Papa and Tata get
twin pools.

## Core concept: own tasks (body)

`ProjectSYNCS/MinecraftBody/index.js` gets **own tasks**: tasks with `own: true` and `player: ''`. They are body-only,
hold no claim, and produce no order events.

- `isFree(t) = !t || t.own`. Use it everywhere that now means "nobody needs her":
  - `canDefend`
  - `autoSleepTick`
  - `startForage`
  - `flee`: an own task gives no resume and emits no `dropped`
  - `defend`: an own task gives no resume, and autonomy restarts by itself
- Any order still goes through `stay()`, which ends an own task.
- **`held` state.** `stay` and `sit` set `heldUntil = now + 10 min`, and any other order clears it. While held, no
  routine, tag-along or retrieval runs; body language still does.
- **Grace after an order.** When a non-own task ends (task goes from non-own to null), `freeSince = now`. Autonomy
  waits `OrderGrace = 45 s` before starting anything, so « viens » doesn't end with her walking off at once.
- **`autonomyTick`** runs every second (throttled from `physicsTick`). It returns early when she is sleeping, eating,
  sitting, held, in grace, or has a non-own task. Otherwise it picks, in priority order:
  1. a pending retrieval (#4)
  2. the routine (#3)
  3. tag-along (#2)
  4. nothing (idle body language only)

## 1. Body language (body only, no C#)

`idleTick` runs on `physicsTick`, throttled to 250 ms.

**`lookFree()`**: true when she is not sleeping, not eating, not drawing her bow, the pathfinder isn't moving, and
the task is none, own-idle or tagAlong. Fishing, fights and furnaces keep control of her gaze.

- **Head tracking.** Who she looks at, in order:
  - a player who chatted in the last 8 s within 12 blocks (`lastChat[name]`, set in the `chat` and `whisper`
    handlers)
  - otherwise the nearest player within 6 blocks
  - otherwise a random glance every 5–10 s

  She turns with `bot.lookAt(pos)` (no force, so it isn't a snap).
- **Turning away.** On `entityHurt` with an exact player source, she records `hitBy = { name, until: now + 10 s }`.
  For any player, Papa included (she's hurt). While it lasts and `lookFree()`, she looks at the opposite yaw of that
  player instead.
- **Crouching for family.** For each family member, track whether they were away (not in sight, or more than 10
  blocks off). When one comes within 5 blocks, she looks at them and crouches twice (`sneak` on and off, 250 ms).
  Once per 2 min per member. Not while fighting, fleeing, defending, sleeping or eating; if she's `sitting`, she
  stands up and back down.
- **Slow steps around family** are part of tag-along (#2).

## 2. Tag-along

A `tagAlong` own task: Papa first, else Tata, if in sight and nothing in the routine is due.

- **Distance.** Over 8 blocks away, she follows to 4 with `GoalFollow`. The sprint is off for this walk, with its
  own `Movements` (`allowSprinting = false`), and on again when they're more than 16 blocks off. `stay()` restores
  `gentleMovements()`.
- **Slow steps.** Every 15–40 s she walks to a random point 2–4 blocks around them.
- **End.** It ends silently when they go out of sight.
- **Event.** `tagAlong {player}` on start. C# rolls 0.2 and says `McTagAlongLinesOwner` / `McTagAlongLinesTata`
  through `Remark("tagAlong", …)`.

## 3. Routine (named places)

**C#.** A new pure helper, `Helpers/Minecraft/McRoutine.cs`. `McRoutine.Classify(placeKeys)` returns
`(farm, fish, ranch, home)` keys:

| Kind | Rule | Words |
|---|---|---|
| ranch | any word of the key is an animal-pen word | enclos, paturage, bergerie, etable, poulailler, animaux, vaches, moutons, cochons, poules, elevage |
| farm | first word | ferme, champ(s), potager, plantation(s), culture(s) |
| fish | first word | peche, etang, lac, ponton, riviere, mare |
| home | existing `HomeKeys` | |

- Keys are already normalised (`McCommand.PlaceKey`), so the words are without accents.
- So « ferme aux animaux » is a ranch and « champ de blé » is a farm.

The service sends `{"op":"routine","farm":spot|null,"fish":spot|null,"ranch":spot|null,"home":spot|null}`:
- on spawn, after `family`
- after every place save, move or forget (`OnMarked`, `ForgetPlace`)

**Body.** It stores those spots. **Dimension:** a spot in another dimension is skipped. **Distance:** autonomous
trips are capped at `OwnTripMax = 256` blocks, much less than `MaxTrip`.

The day phase comes from `bot.time.timeOfDay`:

**Day (0–11499).** She works in `WorkBlock = 6 min` turns, taking the available activities in turn. Each starts
with a `trip` own task to the spot. On `goal_reached` it starts the activity with the same checks as the order
version:

- **Farm:** crops within 16 blocks and a chest within 10. It reuses `farmRound` / `storeProduce` with an own `farm`
  task.
- **Fish:** open water near the spot and a rod. With no rod she reports `lacking rod` (#8). A `quiet` flag makes
  `fishLoop` emit no `caught`, and when the rod breaks the activity just ends. The loop's stop condition generalises
  to `until`.
- **Ranch:** see #12.

A spot that fails (no path, nothing there) is marked failed for 10 min and skipped, silently, with a log line. An
own activity that runs past its `until` is ended by `stay()` and the next one is picked.

**Dusk (11500–12541).** If a home is known and she's more than 6 blocks from it, a `trip` home. Then she stays
there (`atHome`) until morning.

**Night.** Nothing new: `autoSleepTick` already finds a bed within 48 blocks, and at home one is likely.

**Event.** `routine {what: farm|fish|ranch|home}` when she sets off. C# rolls 0.25 and says it through
`Remark("routine", …)` with `McRoutineFarmLines` / `McRoutineFishLines` / `McRoutineRanchLines` /
`McRoutineHomeLines`.

**How it fits with existing activities.** Own activities count for `isActivity`, so errands, sleep and flight pause
and resume them like the ordered ones. A player's farm, fish, ranch or patrol order replaces the own one through
`stay()`, and then holds her under `McClaim` as today.

## 4. Getting her stuff back

- **On `death`.** She records `deathAt = {pos, dimension, at}`.
- **Respawn.** A new `bot.on('spawn')` listener (the existing one is `once`) arms `pendingRetrieve` 3 s after the
  respawn.
- **Skipped** (emit `retrieveLost {why}`) when:
  - it's another dimension (`otherDimension`)
  - it's more than 500 blocks off (`tooFar`)
  - she died in the void, `y < minY + 1` (`void`)
  - there's lava within 2 blocks of the spot (`lava`)
- **The walk.** A `retrieve` own task, which outranks the routine: `GoalNear(pos, 1)`, then she sweeps the item
  entities within 6 blocks for up to 20 s, walking onto each. A diff of `itemCounts` gives what she got back.
  - The armour goes back on through the existing `playerCollect` handler.
  - She emits `retrieved {count}`.
- **Interrupted by an order:** `pendingRetrieve` stays until 5 min after the death (when the items despawn), and
  autonomy picks it up first.
- **C# lines:**
  - `retrieving`: `McRetrieveLines`, forced
  - `retrieved` with a count of 0: `McRetrieveEmptyLines`, always
  - `retrieved` with a count over 0: `McRetrievedLines` with `{1}` = count, rolled 0.5
  - `retrieveLost`: `McRetrieveLostLines`, rolled 0.5

## 5. Creeper warnings for anyone

**Body.** `creeperWatchTick` runs every 250 ms.

- **What triggers it.** For each player in sight within 32 blocks (not her): a creeper within 4 blocks of them, and
  **behind** them. Behind means the angle between their yaw direction and the vector to the creeper is over 100°.
- **Once each.** Each creeper is warned about once (`warnedCreepers`, a set of ids pruned on `entityGone`).
- **Not the player she guards:** `guardTick` already handles that one.
- **Event:** `creeperBehind {player}`.

**C#.**
- **Gate:** one per player, `_creeperGate` (`CooldownGate<string>`, 15 s).
- **Remark:** `Remark("creeperBehind", Kin(...), player, force: true)`. A warning can't wait.
- **Pools:** `McCreeperBehindLines`, plus its `Owner` / `Tata` twins (added to `McFamilyPools` in the checks).

## 8. Durability and what she lacks

**Body.** `needsTick` runs every 10 s.

- **Worn tool.** Her kit tools are the best pickaxe, axe, sword and shovel (`kitSlots` logic), plus a rod, shears and
  bow. When one is at 10 % durability or less (`item.durabilityUsed` against
  `registry.items[type].maxDurability`), she emits `wornOut {tool}`. Once per item: it's re-armed when that tool's
  remaining durability goes back up, which means a new tool.
- **Broken tool.** A tool already reported worn disappears while she isn't giving or storing: `broke {tool}`.
- **Lacking.** `lacking {what}`, where `what` is:

  | `what` | When |
  |---|---|
  | `rod` | an own fishing turn, no rod |
  | `shears` | sheep to shear, no shears |
  | `breedFood` | the ranch has nothing to feed |
  | `arrows` | guarding with a bow and no arrows |
  | `armour` | nothing worn, 2 min after spawn |
  | `space` | `bot.inventory.emptySlotCount() === 0` |

**C#.**
- **Gate:** `_needGate` (`CooldownGate<string>`, 10 min per what or tool), released when `Remark` swallows the line,
  like `OnHungry`.
- **Pools:** `McToolWornLines` and `McToolBrokeLines` (`{1}` = tool name from a new `McToolNames`),
  `McNeedRodLines`, `McNeedShearsLines`, `McNeedBreedFoodLines`, `McNeedArrowsLines`, `McNeedArmourLines`,
  `McPocketsFullLines`. They're about the game, not a person, so they're shared pools.

## 12. Animals: the `ranch` order and activity

**Parser** (`McCommand.cs`). Append `McOrder.Ranch` at the end of the enum.

- **What matches.** A ranch-care verb with an animal word within four words after it:
  - verbs: « occupe-toi », « prends soin », « entretiens », « nourris », « élève », « gère »
  - animal words: animaux, bêtes, bétail, troupeau, enclos, élevage, vaches, moutons, cochons, poules, poulets,
    lapins, bergerie, étable, poulailler
  - or a phrase that says it alone: « fais de l'élevage », « mode éleveuse »
- **Order.** It's read **before** the farm cue, so « occupe-toi des animaux de la ferme » is a ranch.
- **Not ranch.** « garde » and « surveille » stay patrol or guard: protecting animals must never start a slaughter.
- **Checks.** Six-letter words go through the slip check: « élevage », « moutons », « cochons », « poulets »,
  « lapins ».
- **Refusals** like the farm's.

**Body** (`ranch(player)` and `ranchRound`, every 15 s like the farm). It's anchored where she stands and appended
to `Activities`.

**Refusals.**
- `cantRanch none`: no farm animals within 16 blocks
- `cantRanch noChest`: no chest or barrel within 10

**Species and their food.**

| Species | Food |
|---|---|
| cow, sheep | wheat |
| pig | carrot, potato, beetroot |
| chicken | any seeds |
| rabbit | carrot, dandelion |

Never named animals (`customName`), never babies, never any species outside this list.

**Each round.**
1. **Breed.** Only if a species has fewer than 8 animals (adults and babies) around the anchor. She picks 2 adults
   not fed in the last 5 min (`fedAt[id]`), equips the food, and calls `activateEntity` on each. With no food she
   reports `lacking breedFood`.
2. **Shear.** She uses shears on adult sheep not tried in the last 5 min (`shornAt[id]`).
3. **Slaughter.** If a species has more than 8 adults, she kills at most 2 surplus adults per round with her
   weapon, through the same hit loop as defend.
4. **Collect and store.** She picks up the drops (#13), then stores meat, leather, wool and feathers in the chest.
   She keeps 16 of each breeding food, and up to 8 cooked or raw meat for herself.

**C#.**
- `IsActivity` and `LastingLocked` include `Ranch`.
- The command map gets `McOrder.Ranch => new { op = "ranch", player }`.
- Events and pools:

  | Event | Pool |
  |---|---|
  | `ranching` | `McRanchAckLines` |
  | `cantRanch none` | `McRanchNoneLines` |
  | `cantRanch noChest` | `McRanchNoChestLines` |

## 13. Picking up her own loot

- **Loot spots.** On `entityDead` of an entity she was fighting or slaughtering (guard, patrol, defend, ranch), she
  records `lootSpots.push({pos, at})`. They expire after 30 s.
- **`pickUpDrops(spot, radius)`** walks onto the item entities (`entity.name === 'item'`) within 3 blocks of a loot
  spot. It's shared with the retrieval sweep.
- **Guard and patrol.** In `goHome`, before returning to the player or the spot: if a loot spot has items within 6
  blocks of her and inside the guard radius, she goes to them (mode `loot`). Collecting XP orbs comes from walking
  near.
- **No other pickup.** Only her own kill spots, so she never takes items a player dropped.

## C# service changes (summary)

`ProjectSYNCS/Services/Minecraft/MinecraftBodyService.cs`:
- **Op sent:** `routine` (on spawn, and on place changes).
- **New `OnEvent` cases:**
  - `routine`, `tagAlong`
  - `retrieving`, `retrieved`, `retrieveLost`
  - `creeperBehind`
  - `wornOut`, `broke`, `lacking`
  - `ranching`, `cantRanch`
- **New gates:** `_creeperGate` and `_needGate`.
- **New chance constants:** `RoutineChance` 0.25, `TagAlongChance` 0.2, `RetrievedChance` 0.5, `RetrieveLostChance`
  0.5.
- **The `Ranch` order** wired into the command map, `IsActivity` and `LastingLocked`.

Reuse what's already there: `Remark`, `Kin`, `Who`, `Confirm` / `OnOutcome`, `FindPlace`, `HomeKeys`,
`DimensionOf`, and `CooldownGate.Release` as in `OnHungry`.

## Lines (`Services/Personality/BotResponses/BotResponses.Minecraft.cs`)

New pools are written to `docs/syncs-voice.md` and the in-game rules:
- plain text under 256
- kaomoji drawable by Minecraft's font, about 1 line in 4 at most, none in pools of 2–3
- Papa and Tata by title in the twins

The new pools:
- `McTagAlongLinesOwner`, `McTagAlongLinesTata`
- `McRoutineFarmLines`, `McRoutineFishLines`, `McRoutineRanchLines`, `McRoutineHomeLines`
- `McRetrieveLines`, `McRetrievedLines`, `McRetrieveEmptyLines`, `McRetrieveLostLines`
- `McCreeperBehindLines` (+ `Owner`, `Tata`)
- `McToolWornLines`, `McToolBrokeLines`, `McToolNames`
- `McNeedRodLines`, `McNeedShearsLines`, `McNeedBreedFoodLines`, `McNeedArrowsLines`, `McNeedArmourLines`,
  `McPocketsFullLines`
- `McRanchAckLines`, `McRanchNoneLines`, `McRanchNoChestLines`

## Checks (`tests/ProjectSYNCS.Checks/Program.cs`)

- **Ranch parser cases.**
  - Positive: « syncs occupe-toi des animaux », « syncs nourris les vaches », « syncs occupe-toi des animaux de la
    ferme ».
  - Negative: « syncs garde les moutons » ≠ ranch, « je nourris mes vaches » (talk about oneself), « syncs occupe-toi
    pas des animaux » (refused).
  - Unchanged: farm phrases still farm.
- **`McRoutine.Classify` cases.**
  - ferme → farm, « champ de blé » → farm, « ferme aux animaux » → ranch, étang → fish, maison → home.
  - An unrelated name → nothing.
- **Pools.** Every new pool goes in the in-game markup list. `McCreeperBehindLines` goes in `McFamilyPools`.
  `McTagAlongLinesOwner` / `Tata` go in the explicit list, like `McSeekMetLinesOwner`.

## Docs and release

- **`docs/agents/minecraft.md`.** New sections:
  - *Free time* (own tasks, held, grace, priority)
  - *Body language*
  - *Routine*
  - *Getting her things back*
  - *Creeper warnings*
  - *Needs*
  - the ranch bullet under *Orders*
  - loot pickup

  Write the rules and their reasons, not the story. Also update the protocol header at the top of `index.js`.
- **`README.md`.** Extend the Minecraft section (around l.138–203) with the new behaviours and the ranch order.
- **Version.** `ProjectSYNCS/config.yaml` 7.17.0 → **7.18.0** (minor: new order and behaviour). Add an entry to
  `ProjectSYNCS/CHANGELOG.md` in English, with no member names.
- **No commit.** The owner commits by hand.

## Implementation order

Build or syntax-check after each step.

1. Own-task core (`isFree`, held, grace, `autonomyTick` skeleton) and body language: `node --check`.
2. Tag-along, loot pickup and creeper warnings: body, then C# events and pools.
3. Routine: `McRoutine` and its checks, the `routine` op, the body scheduler, the own farm and fish.
4. Death retrieval.
5. Ranch: parser and checks, body, C#.
6. Needs and durability.
7. Docs, README, changelog, version bump.

## Verification

- **Static checks.**
  - `node --check ProjectSYNCS/MinecraftBody/index.js`
  - `cd ProjectSYNCS && dotnet build`
  - `dotnet run --project tests/ProjectSYNCS.Checks`, which must exit 0
- **Live test on the dev server**, with `/debug minecraft join`:
  - **Body language.** She turns to whoever talks. She turns away after a hit. She crouches when Papa walks up from
    afar.
  - **Tag-along.** With no routine places, she trails Papa, slowly, with short steps around him.
  - **Routine.**
    - Save « ferme » (crops plus a chest) and « étang »: by day she alternates between them, and goes to « maison »
      at dusk.
    - « syncs viens » interrupts her, and she resumes about 45 s after arriving.
    - « syncs reste » holds her.
  - **Death retrieval.** Die near the base (`/kill`): she walks back, picks up her stuff, and says so.
  - **Creepers.** A creeper behind another player gets the warning, once.
  - **Needs.** A nearly broken pickaxe gets the worn-tool line. Full pockets get the pockets-full line.
  - **Ranch.** « syncs occupe-toi des animaux » in a pen with a chest: she breeds up to 8 and culls above 8. A named
    cow survives.
  - **Loot.** After guarding through a night, she picks up the drops near her kills.
