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
« Lui interdire de jouer » flips a switch kept in `minecraft-state.json` next to the database: the
one piece of her Minecraft state that survives a restart, because « off until I say so » must not
turn itself back on after a crash. It starts off (no file = forbidden). While forbidden she still
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

**Counts** come from a number anywhere (« 20 », « x20 », « 20x »), else from a quantity in words
right before the resource (« dix bûches », « une stack de pierre », « une dizaine de bûches »),
else 10. Only right before: « du bois pour une maison » isn't one log. **Coordinates** accept
F3's decimals, floored to the block (-300.7 is block -301), and labels or brackets around them. A message that only names her
gets the list of what she can do (`McHelpLines`), but only when the name opens or closes it: a
call, not talk about her.

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
- **Food and armour** need no order: she eats when hunger drops below 14 (never rotten flesh and
  the like), and wears the best piece she carries for each slot on spawn and after each pickup.
  Both are our own few lines; `mineflayer-auto-eat` is ESM-only now and the armour plugin is
  unmaintained.
- **She never digs and never places a block.** `canDig = false`, no scaffolding blocks, no
  pillaring: a path through someone's wall or a dirt tower in their build is not help. Where
  walking can't get there she says so (`McNoPathLines`) instead.
- **She can only reach a player the server shows her** (render distance). Beyond that the body
  answers `cantReach notVisible` at once (`McTooFarLines`). A followed player who goes out of
  sight ends the follow (`lost`).
- **Orders are acknowledged when the body reports them under way** (`moving`, `collecting`,
  `sleeping`), not when they're read. « J'arrive » followed by « je te vois pas » reads as a broken
  promise. Stay is acknowledged straight away.
- **Whispered orders get whispered answers**, outcome included. The service remembers who gave the
  current order and how.
- **Orders aren't rationed like remarks.** One gate per player ignores a second order within the
  same second (a double send), never a stop, so « viens » then « stop » always works. Another gate
  gives each player the help list once a minute at most.

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
- In-game lines are **plain text under 256 characters**: no custom emotes, no sticker tags (the
  checks enforce it).
- Her join and leave posts go to the bots channel (`BotsChannel`): low stakes, her own initiative.

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
`minecraft_port`, `minecraft_username`, `minecraft_owner`. If the server has a whitelist, her name
must be on it. Offline mode means anyone can log in under any name, so she trusts names only to
choose who to talk to, never for anything that matters.
