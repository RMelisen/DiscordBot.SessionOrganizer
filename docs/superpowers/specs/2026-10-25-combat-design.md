# Combat — design

## Why

She dies to monsters while she's busy: building a camp, following Papa through a cave. The cause is
in the body: `entityHurt` on **her** only reports who hit her (`hurt`) and does nothing else. She
fights only when guarding (`guardTick`), when a monster hurts a *player* near her (`defend`), and
dodges or shoots creepers (`creeperTick`). A zombie hitting her while she places a camp block gets
nothing: she keeps building until she dies. Her only retreat at low health is inside guard and
defend.

Papa wants **a full combat overhaul so she stays alive in most dangerous situations**, without
anyone babysitting her.

## Goals

1. **Anything that attacks her gets dealt with, whatever she's doing.** She stops, deals with it,
   and goes back to what she was doing, the way she already does after defending someone.
2. **She sizes the fight up and picks one of three answers** (Papa's OK, my design):
   - **Fight**: one or two monsters, decent health. She fights it out.
   - **Fall back**: outnumbered, badly hurt, or no weapon. She backs off **towards the player she's
     with**, hitting whatever follows her into reach, and makes her stand beside them. Never away
     into the dark alone.
   - **Get out**: something she can't handle (a warden, a creaking, a creeper with no bow). She
     leaves, as she does today from creepers.
3. **She fights better**: her shield up against arrows and blasts, hits timed to her weapon's real
   speed, a golden apple or a healing potion she carries when it gets bad, and a totem in her
   off-hand when she's close to dying.
4. **Fire and lava**: on fire, she runs to water nearby. In lava, she gets out to the closest solid
   ground.
5. **No fighting on edges**: no chasing along a cliff or a ravine, and never with a drop or lava
   behind her, where one knockback kills her.
6. **Ranged foes** (skeletons, strays, bogged, pillagers, blazes, ghasts, phantoms, witches, a
   drowned with a trident): shot back with her bow when she has one, otherwise closed in on behind
   her shield. A ghast's fireball gets hit back.
7. **Not surrounded**: when several come at once, she moves so they're all on one side of her,
   ideally against a wall or into a corridor, so they come one or two at a time.

## Non-goals (Papa's call)

- **Poison and wither**: no milk, no special answer to effects.
- **Arming herself**: she doesn't craft a sword or a shield, and doesn't ask for one.
- No change to how she treats players: running from a player who hits her (`flee`), defending
  family against players (`familyHurt`, `DefendStage`), never hitting family, all stay as they are.

## Anti-goals (a failure even if it works)

- **Placing or breaking a block to survive**: no torches, no pillar, no blocking a hole, no
  water-bucket landing, no digging an escape. « She never digs and never places a block » stands,
  with its existing exceptions only.
- **Leaving the player she's with** alone in a cave because she ran the other way.
- **Dying by chasing**: off an edge, into lava, deep into a cave after a monster that ran.
- **Hitting a player**: a swing, a sweep or an arrow that lands on a player near the monster.
- **Twitching**: dropping her camp or her field every few seconds for a monster that's no real
  threat, or flipping between fight and fall back on every tick.
- **Starting fights she used to avoid**: endermen and piglins stay left alone until they hurt her.
- **Words in the body**: the body still holds no French and decides nothing about speaking.

## What exists and stays

Read `docs/agents/minecraft.md` first; this builds on it.

- **Guard and patrol** (`guardTick`), **defending a player from a monster** (`defend`,
  `defendTick`), **creepers** (`creeperTick`, `dodgeTick`, `shoot`), **retreat at 3 hearts**
  (`RetreatHealth`), **eating to heal** (`healTick`, `eatIfHungry`), **armour and shield worn**
  (`wearBestArmor`). Their behaviour stays; they move onto the shared fight core below.
- **What a fight interrupts and goes back to**: `canDefend`, `Restartable`, `resumeFor`,
  `resumeAfterBreak`, `backTick`. The camp resumes at the same site (`resumeFor` carries `site`,
  `placed`, `floor`, `heights`). Self-defence uses the same machinery.
- **In sight only** (`inSight`), **never under water** (`headUnder`), **never through a player**
  (`playerInLine`), **never a creeper by hand**.
- Every per-tick piece runs in the physics tick and is wrapped in `safely`.

## How it works

### One fight core

Today guard, patrol and defend each carry their own copy of « chase, hit on a 650 ms cooldown ».
They and the new self-defence share **one fight core**: target choice, positioning (edges, not
surrounded), the weapon's cooldown, the shield, ranged foes, and the emergency measures. Guard keeps
its own target choice (around the player or the spot) and hands the core the foe; self-defence
hands it whatever is on her. How the core is cut into functions is the builder's.

### When she fights for herself

- **Hit by a monster** (`entityHurt` on her, the source an `isFoe` monster or one of the neutrals
  below): self-defence starts.
- **What she never hits** (my call): a tamed animal (a player's wolf, Papa's included: it attacks
  her when its owner hits her), an iron golem (a fight she loses), and the other animals that only
  hit back (bees, goats, llamas, polar bears, a wild wolf). Hurt by one, she gets away from it
  (*Getting out*), never fights it.
- **Before the first hit** (my call): a monster in sight, within 4 blocks of her and coming at her.
  A zombie walking up to her at her camp is a fight anyway; waiting for its first hit only costs
  hearts. A monster further off that isn't coming is left alone, so she doesn't drop her work for
  every skeleton on a hill (*Twitching*).
- **Peaceful ones that hurt her** (an enderman, a piglin, a zombified piglin) become foes for that
  fight only. The warden and the creaking are never fought, only got away from.
- **What it interrupts**: everything `canDefend` allows today, and also what isn't restartable
  (waiting at a furnace, handing over, storing): her life comes before an errand (my call). A
  restartable order, an activity, a follow or guard comes back after (`resumeFor`). A one-off order
  that can't restart is dropped and its player told (the body reports `dropped` with no attacker; a
  new C# pool for « a monster made me drop it », shared, about the game). Never while she sleeps,
  rides a boat (her physics is off) or crosses a portal.
- **Over** when nothing hostile is on her or near her (in sight, within about 8) for a couple of
  seconds, then back to what she was doing. Picking up the loot of her kills follows today's loot
  rule.

### Orders during a fight (Papa's call)

- **« viens » and « suis-moi »** (come, follow) from a player she can reach: she falls back towards
  them at once, fighting as she goes (*Falling back*), and the order is hers once the fight's over,
  acknowledged then like any order getting under way.
- **Any other order waits** until the fight is over, then starts. It's kept as pending (`_pending`
  in C#, the claim unchanged until the body reports it under way), and a newer order from the same
  player replaces it. « stop » / « reste » mid-fight wait too: she can't stand still under attack.
- **Getting out is never delayed** by an order: a warden comes first.

### Sizing the fight up

A pure function (no `bot`), so it can be read and tuned in one place: it takes the foes in sight
near her and her own state, and answers **fight**, **fall back** or **get out**.

- **The foes**: each kind weighs (a zombie or skeleton about 1, a spider or cave spider a little
  more, a witch, vindicator or blaze more, a ravager a lot, a warden or creaking « get out »). A foe
  further off weighs less.
- **Her**: hearts, armour points of what she wears, her weapon's tier (a bare hand counts very low),
  whether she has a bow and arrows for the ranged ones.
- **Hysteresis**: she switches from fight to fall back at a lower bar than she switches back, and
  holds a decision a couple of seconds at least, so it doesn't flip on every tick (*Twitching*).
- The weights and bars are the builder's to set and tune on the local server; the guide is « what
  a careful player with her kit would do ».
- At 3 hearts (`RetreatHealth`) it's always fall back, as today.
- **No weapon is not fall back by itself** (my call): a bare hand or the camp's wooden axe still
  fights one or two weak monsters (a zombie, a skeleton up close, a spider). Having no weapon only
  lowers her side of the scale. She doesn't craft or ask for one (Non-goals), so this is common.

### Falling back

- **Towards whom**: the player of the order or activity she's on (follow, guard, the asker), else
  Papa, else Tata, else the closest player, within about 32 blocks and in sight or reachable. She
  heads for them and stops beside them (2–3 blocks), then fights there with them. With nobody, she
  backs away from the foes to a point off their side, like `dodgeTick`'s 8-block hop, again when
  she gets there.
- **Hitting as she goes**: a foe in reach and her weapon ready, she turns and swings, then keeps
  going.
- **Healing**: eating (`eatIfHungry(20)`) when nothing is in reach; a golden apple or a healing
  potion first when she's low (below).
- **Back to fighting** once the sizing says fight again (healed, or beside her player, which counts
  as help).
- **A time limit** (my call): falling back alone lasts about 20 s (like `RetreatFor`). If what
  chases her is still on her then, she turns and fights it as a last resort, unless it's a « get
  out » kind. Running for ever from one zombie never finishes the camp.
- **Words**: `McRetreatLines` (already a rationed remark, already about backing off hurt), when she
  starts falling back. Nothing else is said in a fight (my call, matching defend today).

### Getting out

- From a warden, a creaking, an animal she never hits, or a sizing that says « get out »: away from
  it, out of its sight or well out of its reach, then back to what she was doing.
- **Never away from her player** (my call): following or guarding someone, or with a player near
  her, she gets clear of it **on their far side from it**, staying within reach of them, and the
  follow or guard is never dropped for it. Alone, she goes away from it. An order that can't wait
  for it to leave is dropped like a fight's (above). The warden is blind and finds by sound and smell; she just walks away, no
  sprinting needed close to it (the builder may find otherwise on the local server).
- She keeps the « no way out » answer of `flee`: no digging, no building.

### Fighting better

- **The weapon's cooldown**: one function gives the cooldown of what she holds (sword 625 ms,
  trident ~900, axe 1000–1250 by tier, mace ~1700, a bare hand 250), used everywhere the 650 is
  today (guard, defend, the ranch's cull, `shoot`'s pause). Weapon choice stays `Weapons`' order.
- **The shield** (her off-hand, `wearBestArmor` already wears one): raised when a ranged foe in
  sight is facing her and she isn't about to hit, while she closes in on a skeleton, and when a
  creeper is about to blow within reach and she can't get clear. Lowered to strike, raised between
  strikes when a foe is in reach. An axe hit disables it for 5 s (the game's rule): she then fights
  without. How to read « about to shoot » is the builder's (entity metadata for a drawn bow, or
  simply « in sight and in range »), checked on the local server.
- **Emergency** (my calls):
  - **Golden apple** (enchanted first) at 4 hearts or less, if she carries one, even mid-fight;
    eaten while falling back, never with a foe in reach and her shield down.
  - **A healing potion** she carries (instant health, regeneration), same moment, before the apple.
    Only drinking, no splash, no brewing.
  - **A totem**: at 4 hearts or less, a totem she carries goes in her off-hand in place of the
    shield; back to the shield once she's over 7 hearts. A totem popping is a forced remark from a
    new small pool (« j'ai failli y passer »): shared, about the game.
  - **Kept**: `Store` keeps golden apples, healing potions and totems with her kit; « vide tes
    poches » still hands them over (it's a player's request).
- **Sweep and arrows near players**: a swing or a shot with a player within about a block of the
  target waits a moment or steps aside, as `playerInLine` does for arrows today. The builder checks
  whether a sword's sweep reaches players on a PvP server and widens the rule if it does.

### Edges

- **Fight movements**: while fighting she moves with her own `Movements` (like `walkPace` does):
  no parkour, no drop deeper than 2, and lava or a drop of more than 3 within a block of a step costs
  a lot, so she never chases along a ravine's lip. The tick changes goals only on a mode change
  (`setMode`), as today.
- **Nothing behind her**: before a swing, if the block behind her (away from the foe) is a drop of
  more than 3, lava, or the void, she steps sideways or forwards first, so a knockback can't push
  her off.
- **No chasing over a gap**: a foe across a drop or on lower ground she can't walk to is left (a
  ranged foe gets the bow, or she gets out of its sight).
- A foe that runs further than about 12 blocks from where the fight started is let go, so she isn't
  drawn deep into a cave.

### Fire and lava

- **On fire** (the entity's « on fire » flag) and not in water: she goes for the closest water within
  about 8 blocks (a water source or flowing water she can stand in), steering directly if the
  pathfinder won't plan into it, and stays until the flag clears. No water near: she keeps away from
  fire and lava, and the fight goes on.
- **In lava**: straight towards the closest standable solid ground within a few blocks, jumping, by
  her own controls (the pathfinder avoids lava and may not plan from inside it), then for water if
  she's still burning.
- A **fire resistance** potion she carries is drunk at once in lava or on fire (my call; drinking
  only, like the healing potions).
- These run whatever she's doing and pause a fight; they come before everything except being in
  bed. **They interrupt like a fight** (my call): getting out of lava only takes her controls, like
  `waterTick`; the walk to water goes through `resumeFor` and back, and an errand that can't restart
  is dropped. The `dropped` line for it must not blame a monster: the new pool is about danger in
  general (« un danger », not « un monstre »), and serves fights too.

### Ranged foes

- **With a bow and arrows**: ranged foes (the list in Goals 6) are shot from where she stands when
  they're over about 6 blocks off, with `shoot`'s rules (aimed high for the drop, never through a
  player, not into a wall). Ghasts and blazes are always shot when she can; phantoms when they're
  circling, hit by hand when they swoop in.
- **Without**: she closes in on a skeleton, stray, bogged or pillager behind her shield, using the
  fight movements. A ghast or a blaze she can't reach: she gets out of its sight. A witch: closed in
  on, like a skeleton.
- **A ghast's fireball** (the `fireball` entity) within about 4 blocks and heading at her: she hits
  it, which sends it back. Only in the Nether or wherever a ghast is.
- Arrows already count as a need when guarding with a bow (`lacking`); no new ask.

### Not surrounded

- When three or more melee foes in sight are within about 8 blocks and spread round her (more than
  a quarter circle apart, as seen from her), she moves so they're all on one side: towards the side
  with none, a few blocks, or to a spot within about 6 blocks with solid blocks on two sides (a wall
  at her back, a corridor), if one is plainly there. Then she fights the closest.
- If the sizing says fall back, falling back takes over: walking towards her player usually puts
  them all behind her anyway.

## In C#

Small, because the body decides fights by itself:

- `McRetreatLines` on falling back: the body's existing `retreat` event, now also sent outside guard.
- `dropped` with no attacker: a new shared pool about danger in general (« désolée {0}, j'ai dû
  lâcher ta commande, c'était trop dangereux »), for fights and fire alike, following
  `docs/syncs-voice.md`.
- Orders during a fight: the body holds the waiting order and reports it under way once the fight
  ends; C# keeps it as `_pending` meanwhile, as it does for any order not yet confirmed.
- A totem popping: a new event and a new shared pool, a forced remark.
- `McHelp` and the help pages need nothing: there's no new order.

## Testing

- `tools/mc-pathtest/survival.js` gets a scene per situation, checked on the server's side as
  always: zombies at a camp being built (she fights, then finishes the camp); following Papa in a
  cave with skeletons and zombies (she survives and stays with him); a horde (she falls back to Papa
  and fights beside him); a warden (she gets out); a fight by a ravine and by lava (she never falls
  in); set on fire near water; dropped in lava; a skeleton with and without a bow (the shield); a
  ghast's fireball (in the Nether arena `wild.js` already has, or a built one); a totem with her at
  low health.
- Run `node run.js all`, and `survival` with `MC_CPU_FACTOR=3.5` (the fight core runs every tick).
- The sizing function is pure: the builder may add a small node check for it beside the test tool.

## Shipping

- Minor bump (new behaviour): `7.38.0` → `7.39.0`, a `CHANGELOG.md` entry.
- `docs/agents/minecraft.md` updated: the fight core, self-defence, the sizing, hazards, ranged
  foes, the emergency items.

## Left to the builder

- How the fight core is cut, and its tick order against `waterTick`, `creeperTick`, `dodgeTick`,
  `guardTick` and `defendTick`.
- The foe weights, the bars and their hysteresis, the distances marked « about ».
- How to read a drawn bow, the on-fire flag and the shield cooldown on 26.1 through ViaBackwards.
- Whether `creeperTick`/`dodgeTick` fold into the core or stay beside it.
