# Her body on the Minecraft server

SYNCS joins the group's Minecraft server (Java Edition, offline mode) as a real player.
`MinecraftBodyService` (C#) decides and speaks. The body (`ProjectSYNCS/MinecraftBody/index.js`,
Node + Mineflayer) moves and reports.

## Two processes, one voice

The body is a Node child process the service starts and stops. They talk in JSON lines: commands
go in on stdin, events come out on stdout, and logs go to stderr, which the service forwards to
`ILogger`. The protocol is listed at the top of `index.js`.

**The body holds no words and no policy.** It never decides to speak, to join or to leave, and it
has no French in it. Every line she says in game comes from the `Mc*` pools in
`BotResponses.Minecraft.cs`, through `ResponsePicker`, so `docs/syncs-voice.md` and the checks
cover them like any other pool. A new reaction is a new event in the body plus a pool and a branch
in the service, never a string in JS.

**The child starts with an empty environment.** It gets only `PATH`, `HOME`, the temp variables,
`SYSTEMROOT` (which Node needs on Windows) and its own `MC_*` variables. The bot's environment holds
the Discord token (`run.sh` exports it), and a third-party npm tree must never see it. Don't
"simplify" this by inheriting the environment.

**Death and advancement messages are read by translate key** (`death.attack.creeper`,
`chat.type.advancement.task`), with the victim as the first argument and the killer as the second.
The keys are the same in every language. Mineflayer renders the text from its own English data, so
`McDeath` never parses text, only keys and entity keys (`entity.minecraft.creeper`). A death whose
victim isn't on the server, such as a named pet's (sent to its owner only), is ignored.

## When she joins and leaves

**Nothing happens until the owner allows it.** `/debug minecraft` → « L'autoriser à jouer » /
« Lui interdire de jouer » flips a switch kept in `minecraft-state.json` next to the database. It
survives a restart, because « off until I say so » must not turn itself back on after a crash. (The
file also keeps where she last saw Papa and Tata, see *Arriving*, and the places she was asked to
remember, see *Orders*; everything else is in memory.)
It starts off (no file = forbidden). While forbidden she still
pings the server, so `/debug` shows it, but never joins, and a forced join is refused. Forbidding
takes her out at once.

`McPresence.Decide` is pure, and the checks walk it:

- **Join** when at least 4 players are on, or the owner is (`Minecraft:Owner`, compared without
  case).
- **Leave** once 2 or fewer others have been on for more than 3 minutes, or when the server stops
  answering its status ping. While the owner is on, the "≤2" rule is suspended, or she would join
  for the owner, leave 3 minutes later and come straight back.
- **Three players is a dead band**: she stays if she's already in, but doesn't join for three.

Before she's in, the count and names come from the **status ping** (`ServerListPing`, which needs
nothing on the server). Once she's in, they come from her own player list. Two limits of the ping:
it lists at most 12 names, and a player who turned off « Allow Server Listings » shows as
« Anonymous Player ». So the owner can be missed before she joins: the count still applies, and so
does `/debug minecraft join`.

**An address with no port** is how most hosts hand out a server: the real host and port are in a
DNS SRV record, `_minecraft._tcp.<name>`. `McAddress` does that lookup for the ping (DnsClient)
under the same conditions as the client library: the default port, and a name that isn't an IP.
The body gets the address as configured and does its own lookup, so its handshake carries the same
host a player's client would. Leave `minecraft_port` at 25565 for such an address.

The service polls on **its own 1-minute interval**, like every loop. The server is only up during
sessions and G-Portal has no start/stop API, so she **detects** the server and never starts it.

## Orders

A player gives her an order in chat (`McCommand`, pure, with cases in the checks). It needs her
name in the message (`McCommand.HerNames` plus her in-game name: « sync » counts), because the
chat is shared. Stay is read before refusals, because « bouge pas » means stay; « reste avec moi »
is read before stay, because it means follow.

**Refusals are read next to the verb only**: « ne » up to two words before, « pas » just before or
up to two words after, « jamais » after, « plus » after unless it means more (« plus de bois »).
A « pas » further away belongs to another sentence (« c'est pas grave, syncs viens »).

**The phrase lists are written the way people type in game chat**: familiar French (« ramène ta
fraise », « fais du bois », « il me faut »), shortcuts (« vien », « vi1 », « diams »), a little
English, and the infinitives of questions (« tu peux venir ? »). Every phrase is whole words,
longest first. A word that is common outside orders stays out (« assez », « help » alone, « à
moi »): a missed order costs a retype, a false one makes her walk off. Add the misses found in the
wild, with a case in the checks.

Before matching, `Clean` squeezes letters held down for emphasis (three or more become one:
« viiiiens »), maps game-chat spellings (`Slang`: « mwa », « twa », « chui » → « je suis », so
« chui là » is never « suis » = follow) and drops politeness. Words of six letters or more match
with one slip (`Close`: a letter added, dropped, changed or two swapped), and so does her name;
shorter words must be exact, because at four or five letters one slip makes another word
(« viens » / « liens »).

**The slip cuts both ways at six letters too**: « ranger » / « manger », « ranges » / « manges »,
« préviens » / « reviens », « pêches » / « poches ». Such a word stays out of a list alone and only
comes in a longer phrase (« ranger tes affaires », « préviens-moi »). Check any new six-letter
word against the common words one slip away.

**Talk about oneself is not an order.** A phrase right after « je » doesn't count (« je range ma
chambre », « ça fait 5 min que j'attends »), unless it starts with it (« j'ai faim »); nor does one
right after « ai » or « as », the past tense (« je t'ai donné ma pioche », « j'ai vidé mes
poches »). A few one-word verbs are nouns or participles after certain words (`NounAfter`: « la
pêche », « j'ai pêché », « c'est cuit », « au fond », « la base est protégée ? »), looked up by the
word as typed, since the slip lets « pêché » match « pêcher ».

**Counts** come from a number anywhere (« 20 », « x20 », « 20x »), else from a quantity in words
right before the resource (« dix bûches », « une stack de pierre », « une dizaine de bûches »),
else 10. Only right before: « du bois pour une maison » isn't one log. **Coordinates** accept
F3's decimals, floored to the block (-300.7 is block -301), and labels or brackets around them.

**An order can come in two steps.** A message that only names her (« syncs », « hé syncs ? ») is a
call, but only when the name opens or closes it; elsewhere it's talk about her. She answers with a
short « oui ? » (`McCalledLines`), and that player's **next message within 30 seconds** is read as
if it carried her name (`McCommand.Parse(…, called: true)`): « syncs » … « suis moi ». One message
only, whatever it turns out to be, so a call followed by chat doesn't keep her listening; a
follow-up she doesn't understand gets silence, not another « oui ? ». Other players still need her
name in that window. The window lives in the service, per player, in memory. There is no help
list: the owner removed it.

- **Come** walks to where the player stood (`GoalNear`, 2 blocks). **Follow** keeps a moving goal
  on the player (`GoalFollow`). **Stay** clears the goal and cancels a gathering run.
- **Go to** takes 2 numbers (x z, `GoalNearXZ`) or 3 (x y z), read from the raw text so a minus
  sign survives normalisation. Over 1500 blocks she refuses (`tooFar`): the pathfinder can walk
  that far by planning partial paths as chunks load, but she'd be gone for the evening.
- **Guard** follows the player and, every physics tick, picks the closest monster within 10 blocks
  of them (or 6 of her), chases it, and hits it on a 650 ms cooldown, with the best sword or axe she
  carries. Our own loop, not `mineflayer-pvp`, which pulls a whole Mineflayer 2.x through
  `mineflayer-utils`. Endermen and piglins are left alone: hitting them starts the fight she's
  supposed to prevent. Kills are a rationed remark.
- **Collect** (`mineflayer-collectblock`) finds the requested blocks within 48 blocks, checks she
  can harvest them (with what she carries, or by hand; ores and stone need a pickaxe, `noTool`),
  breaks them, then walks back and tosses what she gained (an inventory diff) at the player's
  feet. collectblock asks the pathfinder's movements before breaking anything, so it gets
  `gatheringMovements`: `safeToBreak` is true **only for the target blocks**, so nothing else on
  the way is ever broken. Wood is a non-stripped log with *natural* leaves within reach
  (`persistent: false`): a log in a wall has none, and placed leaves are persistent.
- **Sleep**: the night only passes when every player sleeps (by default), and that includes her.
  When a player goes to bed at night (`entitySleep`) she looks for a bed within 48 blocks, once per
  night, and says so when there's none (`McNoBedLines`), since she is then why the night won't pass.
  « syncs dodo » does the same on demand.
- **Harvest** (« syncs récolte le blé », McOrder.Harvest with a `McCrop`, `Any` for « syncs
  récolte ») breaks the *ripe* crops within 24 blocks (wheat, carrots, potatoes at age 7, beetroot
  at 3), puts a seed back on each patch of farmland left bare, and tosses the haul at the player's
  feet like a gathering run. Melons and pumpkins count only next to an *attached* stem, so a pumpkin
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
  or barrel within 10 blocks, keeping 16 of each seed back to replant; a full chest leaves the rest
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
- **Food and armour** need no order: she eats when hunger drops below 14 (never rotten flesh and
  the like), and wears the best piece she carries for each slot on spawn and after each pickup.
  Both are our own few lines; `mineflayer-auto-eat` is ESM-only now and the armour plugin is
  unmaintained.
- **With nothing to eat, she asks.** Below 14 food with no edible item, the body reports `hungry`
  each time the bar drops a notch; the service decides: `McHungryLines` every 10 minutes at most,
  `McStarvingLines` every 5 once she's at 6 or less (Minecraft stops her sprinting there, so she
  can't run away). Each ask has its own gate, given back when the remark gates swallow the line.
  The next food she picks up after asking is someone feeding her: the closest player within 8
  blocks gets `McFedLines` (Papa and Tata their twins), said at once, and she eats.
- **Guard and patrol never hit a creeper.** Its blast would land on the player she guards. With a
  bow and arrows she shoots it from 6 blocks or more (our own few lines, aimed a little high for the
  drop, not `mineflayer-hawkeye`); without, she keeps out of its reach and warns once per creeper
  (`McCreeperLines`). Under 3 hearts she stops fighting and backs off, eating to heal, until she's
  at 7 hearts or 20 s have passed (`McRetreatLines`). The tick changes her goal only when what she's
  doing changes (`setMode`): a goal set on every tick is never walked.
- **Patrol** (« syncs surveille la base ») is guard around a spot: where she stands, or a remembered
  place named with it. Read before stay and guard: a care verb with a place word within four words
  after (`BaseWords`, « ici » included), never when the verb is followed by « moi » (« protège-moi à
  la maison » is guard). Like a farm, it lasts while its keeper is on the server.
- **Errands and activities.** Farming, fishing and patrolling are activities: they last until
  someone says otherwise. Handing something over (Give) and putting her things away (Store) are
  errands: they pause an activity, the body goes back to it after (a fresh copy of the task, so the
  old loop sees it's no longer hers and stops), and the service gives the claim back to the
  activity (`McJob.Resume`), without telling its keeper she left. A night's sleep pauses and resumes
  every activity the same way (`resumeAfterSleep`).
- **Give** (« vide tes poches », « donne-moi ton poisson ») tosses what she carries at the player's
  feet; armour includes what she wears. A gathering word only becomes Give with a possessive
  (« donne-moi ton fer »): « donne-moi du fer » stays the gathering order. Goods she can't fetch
  (`CarriedOnly`: food, fish, tools…) take any article. Give is read before Store, whose « ranger »
  is one slip from « manger ». **Store** (« range tes affaires », or anything put « dans le coffre »)
  fills the closest chest or barrel within 16 blocks and keeps her kit: the best sword, pickaxe, axe
  and shovel, a rod, her bow and arrows.
- **Fishing** walks to open water within 16 blocks and casts until told otherwise. No bite in 45 s
  and she reels in and casts again; only then does she click to reel in, since after any other end
  a click would cast. The catch stays on her; a remark now and then (a treasure always).
- **Smelt** uses only a furnace someone else isn't using (nothing in its input or output, no other
  fuel), smokers for food and blast furnaces for ore too, with her own fuel. She waits by it, takes
  the result as it comes, and brings it back. Whether a furnace is free is only known by opening it,
  so « all busy » is the one refusal that comes after she has dropped her task.
- **Remembered places** (`minecraft-state.json`, up to 20, by `McCommand.PlaceKey`) are where the
  player stood when they said « retiens ici : nom »; the body reports it (`mark`). Her lines show the
  name as typed (`PlaceName`) in guillemets. Anyone may save one; only its saver or family may move or
  forget it. A known place is read anywhere after a going verb, before gathering (« va à la mine de
  fer » has « mine » and « fer »); an unknown name only after a preposition (« va à la X »), and the
  service says it doesn't know it. « rentre » is home: « maison », else the other home words.
  **Guide** walks ahead and waits when the player falls 12 blocks behind; 30 s out of sight and she
  gives up. A place in another dimension is refused (`otherDimension`).
- **Answered at once, taking her from nobody**: where she is (the body reports her position), where
  a place is, the list, forgetting, saving, hopping, waving (any greeting, read last so « salut
  syncs viens » is come) and **reminders** (« préviens-moi dans 5 min », « rappelle-moi dans 10 min
  de sortir le fer »). A timer is read first, as its note can hold any word; the length comes from
  the raw text (`Duration`), the note is what follows the first « de », « pour » or « que » once the
  length is out. Three per player, two hours at most, in memory, gone with the body. Sitting is
  stay, crouched.
- **She never digs and never places a block.** `canDig = false`, no scaffolding blocks, no
  pillaring: a path through someone's wall or a dirt tower in their build is not help. Where
  walking can't get there she says so (`McNoPathLines`) instead. **The one exception is a seed
  on bare farmland where she just harvested** (`harvestBlocks`): nothing else, nowhere else. Keep
  it that narrow.
- **She can only reach a player the server shows her** (render distance). Beyond that the body
  answers `cantReach notVisible` at once (`McTooFarLines`). A followed player who goes out of
  sight ends the follow (`lost`).
- **An order that fails at once changes nothing.** The body checks an order can be done (in sight,
  not too far, something to gather, a bed) *before* `stay()` drops the current one, so
  « syncs va chercher des diamants » with none around never pulls her off someone's follow. Keep
  that order in any new op.
- **Orders are acknowledged when the body reports them under way** (`moving`, `collecting`,
  `sleeping`), not when they're read. « J'arrive » followed by « je te vois pas » reads as a broken
  promise. Stay is acknowledged straight away.
- **Whispered orders get whispered answers**, outcome included. The service remembers who gave the
  current order and how. In every line Papa and Tata are « Papa » and « Tata » (`Who`), never their
  in-game names.
- **She works for one player at a time** (`McClaim`, pure, walked by the checks). Whoever gave the
  order under way holds her: only they can stop her or send her elsewhere, and anyone else gets
  `McBusyLines` (once every 10 seconds per player). Family outranks: Papa > Tata > everyone, so
  family can always take her and nobody takes her from them; whoever she leaves mid-order hears why
  (`McTakenLines`). A claim runs out **10 minutes** after its order got under way, Papa's included,
  so a forgotten « suis-moi » never locks her for the evening; family can still take her back.
  The service keeps the order under way (`_order`) apart from the one just sent (`_pending`): the
  pending one only becomes hers when the body confirms it, so a failed order leaves the claim where
  it was. Going to bed (asked, or because a player did) ends any claim.
- **Orders aren't rationed like remarks.** One gate per player ignores a second order within the
  same second (a double send), never a stop, so « viens » then « stop » always works. Another gate
  gives each player her « oui ? » once every 10 seconds at most (the window still opens).

## Arriving

Two seconds after her hello she walks up to **Papa, or else Tata**, when either is on (`seek`), and
she's not already busy with an order. In sight, she goes straight to them. Out of sight, the server
gives her no way to know where they are (positions only reach a client within render distance; no op,
no map plugin), so she goes to **where she last saw them**: the body reports `seen` each time a
player leaves her sight, including logging out next to her, and when she quits. Players log back in
where they logged out, so that spot is a good guess. The service keeps only the family's spots, in
`minecraft-state.json`, with the dimension: a spot in another dimension, or over 1500 blocks, is not
tried. If they show up on her way she heads for them. Found: `McSeekMetLines…`; not found, or no
spot known: `McSeekMissLines…` (« t'es où ? »). Any order replaces the walk; it is never a claim.

Player chat reaches the body as Mineflayer's `chat` and `whisper` events. A chat plugin that
reformats messages can hide them from Mineflayer's parser; if orders go unanswered on a server
with one, that's the first thing to check.

## Failures

An unplanned end (crash, kick, refused login, unsupported version) backs off 2, then 5, then
15 minutes. Spawning resets the count. The last kick reason shows in `/debug minecraft`. The tick
catches everything; nothing escapes the loop.

`/debug minecraft leave` keeps her out for 30 minutes, or the next tick would send her back.
`join` clears the backoff and the hold. Once she's in, the rule applies as usual.

## Her remarks

- One per kind every 30 seconds, and never two within 8 seconds. Two separate `CooldownGate`s,
  owned by the service.
- Night falls (50 %) and advancements (60 %) are rolled on top, because both are frequent.
- A player hitting her is a guess: Mineflayer doesn't say who hit, so the body names the closest
  player within reach. With nobody in reach it's a mob, a fall or fire, and she says nothing.
- **Repeated hits escalate** (`McHitStreaks`, pure, walked by the checks). The same player hitting
  her three times with no more than a minute between two hits is someone trying to kill her: that
  third hit gets `McHitAgainLines` (angry and scared at once) **at once, past the remark gates**
  (`Remark(…, force: true)`, which still starts both cooldowns). Further hits in the streak keep that
  pool, rationed like any remark. Streaks are per player and start over after a minute's pause, or
  when she leaves. Papa's twin is scared and hurt, never angry; Tata's is panicked bargaining.
- **And she runs** (`flee`): from the escalating hit on, the service sends `flee` with the attacker,
  **never for Papa**, the owner's call: she stays and takes it, scared. The body sprints away
  (`GoalInvert(GoalFollow(attacker, 20))`) until she's 20 blocks off, out of sight, or 15 seconds
  have passed (a new hit while running extends it), then reports `safe` (`McFleeSafeLines`, a
  rationed remark). Still no digging or building: with no way out she's `cornered`
  (`McCorneredLines`, said at once). An activity (`resumeActivity`) or a follow or guard is picked
  up again after; a one-off order is dropped and its player told why (`dropped` →
  `McFleeDroppedLines`), unless they're the attacker. She sprints only while her food bar is above
  6, so she runs better when she has something to eat.
- In-game lines are **plain text under 256 characters**: no custom emotes, no sticker tags (the
  checks enforce it).
- **Kaomoji in game must be drawable by Minecraft's font**, which covers the Basic Multilingual
  Plane through Unifont, one code point at a time. So: nothing past U+FFFF (most emoji, `👁`), no
  combining marks (`(•̀ᴗ•́)`, `( ͡° ͜ʖ ͡°)`: the accents land beside the face), no format characters,
  no `§` (colour codes), no `<` (the checks' markup rule), and no script that needs shaping
  (Malayalam's `ദ്ദി`). The checks reject the first four. Katakana, box drawing, `ω`, `ಠ`, `✧`,
  `✿` all draw fine. The in-game set (`syncs-voice.md`) is wider than Discord's, but used
  sparingly, the owner's call: **about one line in four** at most, none in most pools of two or
  three lines; dry lines and Papa hurting her stay bare.
- Her join and leave posts go to the bots channel (`BotsChannel`): low stakes, her own initiative.

**Family in game.** `McFamily.Of` matches a player against `Minecraft:Owner` (Papa) and
`Minecraft:Tata` without case, and the service picks a twin pool (`…Owner`, `…Tata`) for the
lines aimed at one person: the « oui ? », their death, them hitting her, their advancement, and the
acks for come, follow, guard and a delivery. Twins call them « Papa » and « Tata », never `{0}`
(the checks enforce it). Same tiers as on Discord (`personality.md`, *Favouritism*): Papa is never
roasted and, when he hits her, she's hurt and forgives; Tata gets cheeky warmth and the mute
rights. Every other pool is shared, because it's about the game rather than the person. A new
person-aimed pool gets its twins too, and its name goes in the checks' `McFamilyPools`. Offline
mode lets anyone take those names, which is fine: names only pick lines here.

## Deployment

The Dockerfile has a `node:22-bookworm-slim` stage that runs `npm ci` from the lockfile. The final
image gets the `node` binary and `/app/body`. The service looks for the body at
`<app>/body/index.js`, then at `MinecraftBody/index.js` under the working directory (a dev
`dotnet run`), or at `Minecraft:BodyScript`.

**The server's version.** Mineflayer lags each Minecraft release by weeks. 26.3 came out while
4.39.0 stopped at 26.1, with PrismarineJS's 26.3 work still in open pull requests. So the body pings
first (`pickVersion`). It speaks the server's version when `minecraft-protocol` supports it, and
otherwise the newest version it does support, reporting a `version` event with `fallback: true`
that `/debug minecraft` shows. A newer server accepts that older client only with
**ViaBackwards** (with ViaVersion) installed, a Paper/Spigot plugin or a Fabric mod. Without it,
the server kicks her for an outdated client and she backs off. When Mineflayer catches up, bump
`mineflayer`, `minecraft-protocol` and `minecraft-data` together in `package.json` (exact pins) and
the fallback goes quiet by itself. Mods that add blocks or entities can break the body; plugins that
only add commands can't.

**`tick_end` after every move (PrismarineJS/mineflayer#4189).** Since 26.3 the server accepts one
position packet per client tick and resets that count only on the client's `tick_end`, which real
clients have sent after every tick since 1.21.2. Mineflayer never sends it, so a 26.3 server
(ViaBackwards or not) kicks a stock bot for `invalid_player_movement` on its second move, about
1.5 s in. `sendTickEnds` wraps the client's `write` and follows every movement packet with a
`tick_end`, on versions whose protocol has one. Remove it once Mineflayer sends the packet itself,
and check the issue before bumping Mineflayer.

**stdout carries events only.** The protocol libraries print their errors through `console.*`, so
the body sends every console method to stderr before loading them, and the service logs any stdout
line that isn't JSON instead of parsing it.

Options (`config.yaml` → `run.sh` → `Minecraft__*`): `minecraft_host` (empty: off),
`minecraft_port`, `minecraft_username`, `minecraft_owner`, `minecraft_tata` (Tata's in-game name,
for her lines only). If the server has a whitelist, her name
must be on it. Offline mode means anyone can log in under any name, so she trusts names only to
choose who to talk to, never for anything that matters.
