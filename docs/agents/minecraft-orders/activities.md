# Minecraft orders — activities, care and answers

Part of her Minecraft orders: read `docs/agents/minecraft-orders.md` (the index, and the rules
that bind every order) first.

## Activities, care and answers

- **Harvest** (« syncs récolte le blé », McOrder.Harvest with a `McCrop`, `Any` for « syncs
  récolte ») breaks the *ripe* crops within 24 blocks (wheat, carrots, potatoes at age 7, beetroot
  at 3), puts a seed back on each patch of farmland left bare, and tosses the haul at the player's
  feet like a gathering run. **Number states come as strings on 26.1** (an age of « 7 »), booleans
  as booleans: `isRipe` converts, and while it only took numbers she saw nothing ripe at all.
  Melons and pumpkins count only next to an *attached* stem, so a pumpkin
  placed as decoration is never touched, and need no replanting. In the parser a crop word is read
  before the resources (« pommes de terre » has « terre », dirt); a generic word (« champ ») never
  beats a resource (« du bois dans le champ »), and « récolte » after an article is the noun. A crop
  takes any gathering verb plus a few that mean nothing alone (`CropOnlyVerbs`: « arrache »,
  « farm », « déterre »; « arrache-toi » is not an order), and the verb must be another word than
  the crop (« la récolte est prête » names it, doesn't ask). Crop words take one slip from six
  letters (« citrouile »); further misspellings are listed (« carrote », « betrave »). « courge »
  stays out: one slip from « course ».
- **Farm** (« syncs occupe-toi de la ferme ») anchors her where she stands. Every 15 seconds she
  harvests what ripened within 16 blocks of that spot, replants, and stores the produce in a chest
  or barrel within 10 blocks, keeping 16 of each seed back to replant and 16 wheat for the animals (`FeedKept`;
  the seeds, carrots and potatoes kept already feed chickens, pigs and rabbits); a full chest leaves the rest
  on her. No crops, or **no chest**, and she refuses (`McFarmNoneLines`, `McFarmNoChestLines`)
  rather than fill her pockets all evening. The farm cue is read before stay (« reste à la ferme »)
  and guard (« garde le champ »): a phrase that says it alone (« cultive », « mode fermière »), or
  a **care verb with a farm word within four words after it** (`FarmCare`: « prends soin de mes
  plantations », « surveille le potager »). The verb alone never farms: « garde-moi » is guard,
  « il reste du blé » is stay. Only the unambiguous care verbs (`CropCareVerbs`: « occupe-toi »,
  « entretiens ») take a crop instead of a farm word; « garde des carottes » keeps some.
  « ferme » counts only after an article or possessive, since « et ferme-la » means shut up. Going to bed pauses the farm and waking resumes it
  (`resumeAfterSleep`); any order but an errand ends it. A farm and a guarded place are the claims
  that don't run out while their keeper is on the server (`McClaim.Decide(…, lasting)`); family can
  still take her.
- **Ranch** (« syncs occupe-toi des animaux », « nourris les vaches », « fais de l'élevage ») anchors
  her where she stands, like a farm, and lasts like one. Every 15 seconds, for the cows, sheep, pigs,
  chickens and rabbits within 16 blocks: a species under 8 (adults and babies) gets two adults fed
  its food (not the same animal twice in 5 minutes), sheep are sheared, the adults over 8 are culled
  (2 a round at most: the closest first, 6 s to land a first hit on one before trying the next,
  since in a crowded pen she spent whole rounds on one behind the others), and the drops (picked up
  within 4 blocks of the kill, 20 s) go in the chest within 10 blocks (meat, leather, wool,
  feathers; 8 pieces of meat stay on her to eat, and breeding food is never stored). **Short of a species' food,
  she takes some from the pen's chests** (within 10 blocks, 16 at most, `fetchFeed`): the wheat a farm stored is
  there. Chests that had none aren't opened again for that species for 2 minutes; only then is it `lacking`. **Never a named
  animal, never a baby, never another species**; where the server's version doesn't name the baby
  flag (`metadataKeys`), she culls nothing at all. No animals or no chest, and she refuses
  (`McRanchNoneLines`, `McRanchNoChestLines`). In the parser it's a care verb with an animal word
  within four words after it, read **before the farm** (« occupe-toi des animaux de la ferme »).
  « garde » and « surveille » are not ranch verbs: guarding animals stays a patrol and must never
  start a cull. Animal words match exactly (« lapins » is one slip from « sapins »), and « élever »
  stays out (one slip from « enlever »).
- **Food and armour** need no order: she eats when hunger drops below 14 (never rotten flesh and
  the like), and wears the best piece she carries for each slot on spawn and after each pickup,
  and a shield in an empty off-hand (`wearBestArmor`). Golden apples are never a meal (`Reserved`):
  they're for *Fights*' emergencies. Both are our own few lines;
  `mineflayer-auto-eat` is ESM-only now and the armour plugin is unmaintained. Tiers go by
  `ArmorTiers`; **a tier missing from it still beats nothing** (copper armour, missing, was never put
  on). A piece someone gives her (an armour item picked up) gets the closest player's thanks
  (`armoured`, `McArmouredLines` and twins), but not her own things coming back after a death.
- **Armour on and off** (`Dress`, `Undress`, free orders answered from the body's event):
  « mets ton armure », « équipe-toi » put on the best she carries (any piece named is all of it: « mets
  ton casque », « donne-moi ton bouclier »); « enlève ton armure », « retire
  ton armure » take it off **and keep it** in her pockets (`bare`), and she puts nothing on by
  herself until told to or **given a piece**, which is a gift and goes on (all of it). Each piece
  needs a free slot: with full pockets what's left stays on (`McUndressFullLines`). « donne-moi ton
  armure », « rends-moi ton armure » still hand it over, shield included (`wornGear`); so does
  « enlève ton armure et donne-la moi », which has another give verb. « enlève » and « retire » are
  give verbs too, so armour on or off is read just before handing over, and « mets » and « porte »
  need the armour word (« mets tout dans le coffre », « la porte »).
- **Hurt, she eats to heal** (`healTick`, on every health update and every 5 s): Minecraft heals her
  only while her food bar is at 18 or more, and quickly only with saturation left, so below full
  health she eats when the bar is under 18, or under 20 with no saturation left. Not with a monster
  within 10 blocks (a fight's retreat eats for her, and the food in her hand would be her sword), and
  not while she digs, draws her bow, stacks a block or fishes: the food in her hand would cut it off.
  With nothing to eat, 7 hearts or less and the bar under 18, she goes to find some (`startForage`).
- **With nothing to eat, she feeds herself first** (`startForage`, a body-only task with no player and
  **no words**, the owner's call). Only when she's free or at an activity (paused and gone back to,
  like an errand: `pausedActivity`); never during someone's order, and not while fishing, which feeds
  her anyway. Sometimes she picks the 3 closest ripe carrots, potatoes, beetroots or melons within
  24 blocks (replanted, like any harvest), sometimes she fishes for up to 2 minutes (quietly:
  `fishLoop` with `forage` reports no catch), whichever is around, then the other if the first gave
  nothing. If she's not starving (food above 6) she then cooks one kind of raw food (up to 8) in the
  closest furnace or smoker within 16 blocks that has nothing in it at all, with her own fuel, and
  takes back whatever is left. Until then she doesn't eat the raw catch. She eats the most filling
  thing she carries. Nothing found, and she asks as below, and doesn't look again for 5 minutes.
  A farm keeps 8 beetroots and 8 melon slices back for her as well as the seeds (the carrots and
  potatoes kept to replant are food too).
- **With nothing to eat and no way to find any, she asks.** Below 14 food with no edible item, the body reports `hungry`
  each time the bar drops a notch; the service decides: `McHungryLines` every 10 minutes at most,
  `McStarvingLines` every 5 once she's at 6 or less (Minecraft stops her sprinting there, so she
  can't run away). Each ask has its own gate, given back when the remark gates swallow the line.
  The next food she picks up after asking is someone feeding her: the closest player within 8
  blocks gets `McFedLines` (Papa and Tata their twins), said at once, and she eats.
- **Guard and patrol never hit a creeper.** Its blast would land on the player she guards. With a
  bow and arrows she shoots it from 6 blocks or more (our own few lines, aimed a little high for the
  drop, not `mineflayer-hawkeye`: three arrows for three hits at 8, 12 and 15 blocks on the local
  server). She sees one from 16 blocks (`CreeperSight`): it walks 10 in about 3 s, two shots, and it
  takes three. **Never through a player** (`playerInLine`): behind Papa she put four arrows in his
  back while it walked up to him, so with someone in the line she steps 3 blocks aside first, and
  if someone steps in while she draws, the arrow goes up. Without a bow she keeps out of its reach and warns once per creeper
  (`McCreeperLines`). Under 3 hearts she stops fighting and backs off, eating to heal and hitting what keeps
  up with her, until she's at 7 hearts or 20 s have passed (`McRetreatLines`). The tick changes her goal only when what she's
  doing changes (`setMode`): a goal set on every tick is never walked.
- **Patrol** (« syncs surveille la base ») is guard around a spot: where she stands, or a remembered
  place named with it. Read before stay and guard: a care verb with a place word within four words
  after (`BaseWords`, « ici » included), never when the verb is followed by « moi » (« protège-moi à
  la maison » is guard). Like a farm, it lasts while its keeper is on the server.
- **Errands and activities.** Farming, fishing, keeping animals and patrolling are activities: they last until
  someone says otherwise. Handing something over (Give), putting her things away (Store), fetching
  from a chest (Fetch) and crafting (Craft) are errands: they pause an activity, the body goes back to it after (a fresh copy of the task, so the
  old loop sees it's no longer hers and stops), and the service gives the claim back to the
  activity (`McJob.Resume`), without telling its keeper she left. A night's sleep pauses and resumes
  every activity the same way (`resumeAfterSleep`).
- **Give** (« vide tes poches », « donne-moi ton poisson ») tosses what she carries at the player's
  feet; armour includes what she wears. A gathering word only becomes Give with a possessive
  (« donne-moi ton fer »): « donne-moi du fer » stays the gathering order. Goods she can't fetch
  (`CarriedOnly`: food, fish, tools, torches, wool, string, pearls…) take any article. « poudre »
  is gunpowder unless « redstone » follows (`GoodsAt`); « t'as vu ma pioche ? » asks about something
  else (`NotGoods`). A new kind is a `McGoods`, its words, `McGoodsNames`, and `Goods` in the body,
  whose key is the enum's name in lower case. Give is read before Store, whose « ranger »
  is one slip from « manger ». **Store** (« range tes affaires », or anything put « dans le coffre »)
  fills the closest chest or barrel within 16 blocks and keeps her kit: the best sword, pickaxe, axe
  and shovel, a rod, shears, her bow and arrows, and what keeps her alive (golden apples, a totem, healing and
  fire resistance potions).
- **Fetch** (« ramène-moi du fer du coffre », « va chercher 20 pains dans le coffre », `fetch.js`)
  takes a kind of goods from the chests and barrels within 16 blocks of her or of the asker, the
  closest to her first, and hands it over (`handOver`: whole stacks, smallest first, the last one
  whole even when it's more than asked). A number caps it, else up to a stack. Any chest, no owner
  check (the owner's call); a chest is opened and closed, never broken. A bare carrying verb
  (« ramène », « apporte », « rapporte », « amène ») is a fetch only with « du / des / de … coffre »:
  « ramène le fer au coffre » means put it there. « tout » or goods she doesn't know get
  `McFetchUnknownLines` at once: she won't empty chests for nobody knows what. Read before Store, so
  « dans le coffre » after a fetch verb is where to look. No chest near is refused before she drops
  anything (`noChest`); chests with none of it, after (`none`).
- **Craft** (« fabrique-moi une pioche en fer », « fais-moi 16 torches », `McCraft`, `craftorder.js`):
  tools and weapons in wood, stone, iron, gold and diamond, a bow, a fishing rod, shears, planks,
  sticks, a crafting table, a chest, a furnace, torches and ladders. A count is items, made in whole
  recipes (16 torches: 4 crafts), a tool one by default (4 at most), anything else one recipe's
  worth (64 at most). A bare tool (« une pioche ») is the best she can afford: diamond, iron, stone
  (also when there's open stone to gather), else wood; never gold unasked. « fabrique », « crafte »
  take any item or none (« fabrique-moi un truc » gets `McCraftUnknownLines`); « fais-moi » only with
  an item in the next three words, so « fais-moi peur », « fais-moi rire » stay talk and
  « fais-moi du bois » stays gathering. Read after guard and before the chest flag: « craft un
  coffre » makes one. Wood she cuts (whole trees), stone she gathers from open faces with a wooden
  pickaxe she makes if she has no pickaxe; iron, gold, diamonds, coal and string she must carry
  (`cantCraft missing`, named by `McCraftMaterialNames`), checked before she drops anything, like no
  tree (`noWood`) or no open stone (`noStone`) when she's short. She crafts at a crafting table within
  16 blocks, whoever's it is, else at her own (below), and hands over only what she made for them;
  the planks, sticks and wooden pickaxe made along the way stay hers. Planks are of the wood she has
  most logs of. A fight starts the craft again where it was (`Restartable`, with her table and her
  pockets at the start); some made and then a craft refused, she hands over what she has.
- **Fishing** walks to open water within 16 blocks and casts until told otherwise. No bite in 45 s
  and she reels in and casts again; only then does she click to reel in, since after any other end
  a click would cast. The catch stays on her; a remark now and then (a treasure always).
- **Smelt** uses only a furnace someone else isn't using (nothing in its input or output, no other
  fuel), smokers for food and blast furnaces for ore too, with her own fuel. She waits by it, takes
  the result as it comes, and brings it back. Whether a furnace is free is only known by opening it,
  so « all busy » is the one refusal that comes after she has dropped her task. Clay makes bricks and
  logs make charcoal (`McSmelt.Clay`, `Wood`; « fais du charbon de bois »), in a furnace only; the logs
  going in are never her fuel. « fais du charbon » alone stays a trip for coal, like « fais du bois ».
- **Remembered places** (`minecraft-state.json`, up to 20 per player, by `McCommand.PlaceKey`) are
  where the player stood when they said « retiens ici : nom »; the body reports it (`mark`). **They are
  kept by player**: each has their own « maison ». A place named alone is looked up in the player's own
  map, then Papa's, Tata's and everyone else's by name, so the group's map still works for someone who
  saved nothing. « la maison de X », « d'X », « chez X » (X's home, whatever home word X used) name
  someone else's: X is anyone with a place or on the server, or « Papa » / « Tata ». The parser takes
  « de X » out and reads the rest as usual, keeping that reading only when the plain one is also about
  a place, or nothing (« va chercher du bois chez X » stays wood). Her lines show the name as typed
  (`PlaceName`) in guillemets, with whose it is when it isn't the asker's (« « maison » de Papa »).
  Anyone may add a place to anyone's map; only the player it belongs to, its saver or family may move
  or forget it. A known place is read anywhere after a going verb, before gathering (« va à la mine
  de fer » has « mine » and « fer »); an unknown name only after a preposition (« va à la X »), and
  the service says it doesn't know it. « rentre » is her home: the family's « maison », else the other
  home words. The list (« tes lieux ») is the asker's places, then the others they can reach. A file
  from before places were kept by player (one shared `Places` map) is split by saver on load.
  **Guide** walks ahead and waits when the player falls 12 blocks behind; 30 s out of sight and she
  gives up. A place in another dimension is refused (`otherDimension`).
- **What she carries** (« t'as quoi sur toi ? », « montre ton inventaire », « t'as combien de fer ? »,
  « t'as du bois ? »). The body reports her inventory on spawn and after each change, at most every 2 s
  (`inventory`): raw ids and counts, each stack tagged with the kinds of goods `give()` would hand over
  for it, so « t'as du fer ? » counts exactly what « donne-moi ton fer » would toss. The service keeps
  the last report and answers from it, sending nothing. The words are `McItems`, a hand-written table
  (singular, plural, gender: « une pioche en fer », « 23 pains »), since the game's names have neither;
  an item not in it is « un autre truc » in game and its id on Discord. Add an item when she starts
  handling a new one. `McInventory` builds the line, biggest first, within `ListMax`, the rest
  « et N autres trucs »; what she wears only counts when armour is asked. `/debug minecraft` →
  « Voir son inventaire » shows it all, with full counts.
  In the parser (`CarryQuestion`, read before stay and before handing over): « combien » is always a
  question, so is « t'as quoi » alone or followed by « sur toi », « dans… », « comme… », and an
  inventory word of *hers* (« ton inv », not « mes poches ») with a show verb or alone. A gathering,
  give or store verb anywhere makes it an order instead. « t'as du fer ? » asks, because iron is
  something she could be sent to fetch; « t'as du poisson ? » and « t'as quoi comme épée ? » stay
  requests to hand it over, as they always were, except armour (« t'as quoi comme armure ? » can't
  mean « take it off »).
- **Answered at once, taking her from nobody**: where she is (the body reports her position), what
  she carries, where a place is, the list, forgetting, saving, hopping, waving (any greeting, read last so « salut
  syncs viens » is come), compliments and thanks and **reminders** (« préviens-moi dans 5 min », « rappelle-moi dans 10 min
  de sortir le fer »). A timer is read first, as its note can hold any word; the length comes from
  the raw text (`Duration`), the note is what follows the first « de », « pour » or « que » once the
  length is out. Three per player, two hours at most, in memory, gone with the body. Only a delay:
  « réveille-moi à 20h » is a time of day (`ClockTime`) and sets nothing, where it once set twenty
  hours. A bare number after « timer » or « minuteur » is minutes. Sitting is stay, crouched, and
  standing up (« lève-toi », « debout ») is stay, which lets go of the crouch and gets her out of bed.
- **« tu fais quoi ? »** (`ChatIntent.WhatAreYouDoing`, read in `Talk` before anything else talk
  does) is answered from what she's really doing, in her own words (`McDoing.FirstPerson`,
  `OwnLife`): the order under way or just sent first (« je pêche pour Bob », « je te suis » to its
  own player), else her own life from the body's last `doing` report (the routine and where it goes,
  tagging along, walking up to someone, foraging, getting her things back, a danger, sleep, sitting
  or held), else `McDoingNothingLines`. The body sends `doing {kind,player,what}` when it changes,
  checked once a second (`doing.js`), and again on every spawn. A whispered order's place is never
  said to anyone else. `/minecraft status` uses the same report in the third person when she's on
  nobody's order (« elle pêche (sa routine) »).
