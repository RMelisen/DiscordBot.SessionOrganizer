# Her body on the Minecraft server

SYNCS joins the group's Minecraft server (Java Edition, offline mode) as a real player.
`MinecraftBodyService` (C#) decides and speaks. The body (`ProjectSYNCS/MinecraftBody`, Node +
Mineflayer) moves and reports.

Orders and free time are in `minecraft-orders.md`; water, fights, body language, arriving and testing
movement locally (`tools/mc-pathtest`) in `minecraft-body.md`.

## The body's code

`index.js` only connects and reads commands; the body is in `lib/`, **one ES module per part of her
life**, named like the sections below (`gather.js`, `trees.js`, `farm.js`, `ranch.js`, `camp.js`,
`water.js`, `columns.js`, `boats.js`, `doors.js`, `combat.js`, `strike.js`…). A new behaviour goes in
the module of the part it belongs to, or a new one.

- **`bot` and `task` live in `state.js`** and every module imports them. An imported binding is
  read-only, and live: importers always read the current value. So a module changes another one's
  variable through a function its owner exports that says what happens (`holdHere()`, `forgetSwim()`,
  `shieldBroken()`, `setFamilyNames()`); a bare setter (`setTask(…)`, never `task = …`) only where
  that is all there is to it.
- **`wiring.js` registers every listener, in one place**, because the physics tick runs its handlers
  in the order they were added: the swimming before the hazards, `inputTick` first of all. Keep the
  order when adding one. **`commands.js`** turns C#'s commands into calls. An order started again
  after a fight goes through `restart.js`'s `redo`, which `index.js` connects to `dispatch`: importing
  `commands.js` from the modules that restart orders would make them depend on everything.
- `family.js` has Papa's and Tata's names (`isFamily`, `familyOrder`), for whoever needs them.
- **`compat.js` holds every workaround for Mineflayer and its libraries lagging behind the
  server's version**, and its header lists the few that live elsewhere because they're part of
  what they fix. **Go through that list before bumping `mineflayer`, `minecraft-protocol`,
  `minecraft-data` or `prismarine-physics`.** A new workaround goes there, or in that list.
- **Importing a module starts nothing**: no timer, no connection, no listener. `index.js` starts
  them, and `tools/mc-pathtest/column-check.js` imports `movements.js` with a stand-in bot: a timer
  started on import would keep the check from ever ending.
- `fight.js` is the pure half of her fights (no `bot`), checked by `fight-check.js`.
- The modules are ES modules (`"type": "module"`): the tools' CommonJS checks load them with
  `require`, which needs Node 22.12 or later. The Dockerfile copies `index.js` and `lib/`.

On the C# side, `MinecraftBodyService` is one partial class over files named for what they hold
(`.Events`, `.Orders`, `.Places`, `.AtOnce`, `.Outcomes`, `.Talk`, `.Discord`); its fields all stay in
`MinecraftBodyService.cs`. `McCommand` is three: the reading (`McCommand.cs`), the phrase lists
(`.Words`) and the helpers that find things in the words (`.Reading`).

## Two processes, one voice

The body is a Node child process the service starts and stops. They talk in JSON lines: commands
go in on stdin, events come out on stdout, and logs go to stderr, which the service forwards to
`ILogger`. The protocol is listed at the top of `index.js`.

**The body holds no words and no policy.** It never decides to speak, to join or to leave, and it
has no French in it. Every line she says in game comes from the `Mc*` pools in
`BotResponses.Minecraft.cs`, through `ResponsePicker`, so `docs/syncs-voice.md` and the checks
cover them like any other pool. A new reaction is a new event in the body plus a pool and a branch
in the service, never a string in JS.

**She types before she speaks**, as on Discord: every line goes through `SayLocked`, which queues
it behind her previous one and waits 0.7–3 s by its length. A new send path goes through it too.
Two lines skip the wait: a forced remark (a creeper behind someone, drowning: it can't wait) and
her goodbye, which the quit right behind it would cut off.

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

## Talk

Chat that isn't an order is read the way Discord reads it (`McTalkReader`, pure, with cases in the
checks; Discord's side is `personality/intents.md`, *Listening*), so someone talking to her gets an answer,
not silence. **An answer to someone is not a remark** (the owner's call): no remark gate, no
budget. Only she, speaking on her own, is rationed.

- **When.** McCommand found no order (`None`, or `Unknown` for a call) and the message names her,
  or it's a whisper (always for her), or it comes **within 30 s of her answering that player**:
  every answer opens the window again, so a conversation goes on without her name. A message she
  lets go (« mdr t'as vu ça », to someone else) doesn't close it (`_answeredAt`); only the order
  reading is one message long (`_calledAt`), so « viens » said to a friend a bit later stays theirs. A refused
  order (« syncs viens pas », `McRequest.Refused`) is never talk. Her in-game name is turned into
  « syncs » before reading (`HerNameAsSyncs`), since the cues know only that one.
- **The ladder**, Discord's, first match wins: « Inabot », a shutdown threat, a mean reading
  (`Comebacks`; Papa `OwnerMeanReplies`), a specific intent with Papa's and Tata's twins
  (`OwnerIntentLines`, `TataIntentLines`), Papa's pet names, a kind word (`NiceReplies`), a greeting,
  news (`Sharing`), a question or a low-effort word, Tata's warmth roll, then a comeback
  (`Comebacks`, Papa `OwnerComebacks`). A bare call, or a call with nothing read, gets « oui ? » as
  before.
- **Without her name, right after she answered**, only what is plainly for her answers: a specific
  intent or Papa's pet name. An insult, a « ok » or a question there may be for someone else, and
  gets silence.
- **Talked about, not to** (« syncs a pris mon fer », « je joue avec syncs »: `AddressesHer` is
  false) is her own initiative: a remark (`talkedAbout`, half the time, then the gates and the
  budget), from `TalkedAboutLines`, `BackbiteRoasts` when it's mean.
- **What only Discord has is left out**: her records (fact lines, « tu m'aimes ? » from feedback,
  the next session, someone's level or cailloux), a ship, a blame and a side taken (Discord people),
  « chut » (the hush is per Discord channel), the help list (none in game: « aide-moi » is a call),
  and the pointers to slash commands. Dice and « note-moi » work in game as on Discord. A favour she has no order for (`Request`) gets `McCantDoLines`, the
  one game-only pool here. An opinion is /rate's verdict, « moi » being the player
  (`McTalkReader.AskerId`: Papa and Tata by their Discord ids, anyone else by a stable hash of the
  name).
- **The conversation streak** is Discord's (`ConversationStreak`, its own instance, per player):
  the third message answered by a comeback becomes `StreakNudgeLines`, the fifth ends it
  (`StreakEndLines`) and she says nothing to them for 10 minutes. Never Papa.

**The pools are Discord's, made fit for the game by `McChat.Game`**, computed once per pool: no
sticker tag, no custom emote, no ✨ or bold, no face the font can't draw at the end (« (>⩊<) »,
« ദ്ദി◝ ⩊ ◜.ᐟ », stripped, the line kept). A line that needs Discord to make sense is left out:
backticks or a slash command, Discord words (`McChat.DiscordWords`: « tag », « salon », « vocal »,
« emote », her Discord XP, cailloux and ranking…; « serveur », « message », « sondage » and « session »
stay), what is only true off the game (`McChat.NotInGamePhrases`: « good bot », and her having no
arms, legs or back, or never moving, which in game is false), anything undrawable, over
256 characters. Faces are thinned to one line in four, the in-game rate. Whispered, only the lines
under 200 characters (the body cuts a whisper there). **A Discord line edited for Discord is edited
for the game too**: run `dotnet run --project tests/ProjectSYNCS.Checks -- --mc-review` to see what
the game leaves out (« - », with the reason) and every line as she says it in game (« + »). The
checks fail when a pool she uses in game ends up empty, or a kept line isn't plain game chat.

## `/minecraft`

Her Minecraft life seen from Discord (`MinecraftModule`, design in
`docs/superpowers/specs/2026-10-21-minecraft-command-design.md`). Seven subcommands, all answers
ephemeral. Orders, chat lines and reminders sent into the game from Discord were considered and
dropped by the owner: the only thing Discord does in game is `invite`.

- **`help`** is `McHelp.Sections`, two pages (`minecraft:help:N`, `MinecraftHelpHandler`). **Every
  example is parsed by the checks** and must read as the order it shows (talk examples, as talk): a
  change to `McCommand` that breaks one breaks the checks, so fix the phrasing or the parser, never
  drop the check. A new order gets an example here.
- **`status`** (`PublicStatusText`) shows nothing of the owner's (address, kick reasons, the allow
  switch: `/debug`), and what she's doing through `McDoing.ForStatus` from the order under way; a
  whispered order keeps its place or coordinates private.
  **`inventory`** is `InventoryText`, the owner's view, which holds nothing private. **`where`** sends
  the body a `where` with the asker `DiscordAsker` (U+0001, which no player name can hold), so the
  `position` answer goes back to Discord, not to a player; one question at a time, shared by
  simultaneous callers, 3 s at most.
- **`link`** (`MinecraftLink`, by member, unique on `PlayerKey`, the name in lower case): the member
  gets a 4-digit code (`McLinkCodes`, in memory, 10 minutes, one live code per member) and whispers
  « lien 1234 » to her from that name. A whispered code is never talk: a wrong one gets silence; a
  right one whose save fails (taken meanwhile, or the database refused) gets `McLinkFailedLines`, since
  the code is spent.
  **Papa's and Tata's names only link to their own Discord ids** (offline mode lets anyone log in
  under any name), and **a name linked to someone else is refused**: the holder frees it with
  `remove`, the owner frees anyone's with `remove` and `player`. A link never makes anyone family:
  `Minecraft:Owner` and `Minecraft:Tata` still decide that in game.
- **`invite`** (linked members only, `McInvite.Decide`): refused when she isn't allowed, during the
  owner's 30-minute leave hold, when she's in, when the server didn't answer its last ping, or while
  another invite runs. Otherwise she joins and
  the presence tick wants her in for `McInvite.Length` (an hour) as long as the server answers;
  forbidding her or a forced leave ends it. Her arrival line is then `McInvitedLines` (who invited
  her, `McInvite.Inviter`: only while the invite runs, so an invite whose body never spawned credits
  nobody later) instead of `McArrivalLines`.
- **`stats`** (`MinecraftStat`, totals by key, `McStats`: **stored keys, append-only**): counted in
  memory (`McTally`) from the body's events (her deaths, fish and treasures, guard kills, gathered
  blocks by resource, harvests: an order's `harvested` and every farm round's `farmed`, ordered or her
  own routine, both as items gained; orders confirmed under way, total and per player) and her minutes in
  game, written every 5 minutes by the loop and on a clean stop; a failed write puts the counts back.
  The command adds what's still in memory, so it's current. Each write also adds to the day's bucket
  (`MinecraftDailyStat`, the totals + buckets pattern, `AddStatsAsync`), for `/report minecraft` and
  the Minecraft part of Papa's monthly digest (see admin); `/minecraft stats` still shows the totals.

## Failures

An unplanned end (crash, kick, refused login, unsupported version) backs off 2, then 5, then
15 minutes. Spawning resets the count. The last kick reason shows in `/debug minecraft`. The tick
catches everything; nothing escapes the loop.

**What `/debug minecraft` shows of the body.** « Voir l'état » adds how the last body ended (planned
or not, with its connection's reason) and the body's last 8 log lines, kept across bodies so the
ones before a crash are still there, newest first, as many as fit. « Voir son corps » asks the
body itself (`{"op":"debug"}`, `lib/debug.js`, 3 s at most, deferred): her task, what it goes back
to, held, sitting, swimming, riding, her path's goal, health, food and air, the process's memory,
how late the event loop has run since the last ask (every tick handler and pathfinder search runs on
it, so a slow Pi shows there first), and `compat.js`'s status: which workarounds the server's
version needs, the server corrections counted while walking, and the busiest second of packets. No
answer means the loop is stuck: the logs in « Voir l'état » are the next place to look. The text is
built by `McBodyReport` (pure, measured by the checks).

`/debug minecraft leave` keeps her out for 30 minutes, or the next tick would send her back.
`join` clears the backoff and the hold. Once she's in, the rule applies as usual.

## Her remarks

- One per kind every 2 minutes, and never two within 20 seconds. Two separate `CooldownGate`s,
  owned by the service.
- **And at most 4 in any 10 minutes** (`RemarkBudget`), the owner's call: she talks less than she
  did. A forced remark is neither held back nor counted. A remark the budget swallows gives both
  gates back, like one the gates swallow, so the callers that release their own gate still do.
- **The commonest remarks are rolled on top**, so the chat stays the players': night falls (25 %),
  advancements (30 %), an ordinary death (30 %; hers, a kill by a player and family's always get a
  word), a first hit from anyone but family (60 %, rare enough to keep its odds; the escalation is
  never rolled), a guard kill (10 %: kills come in waves) and a catch (15 %, junk 25 %, a treasure
  always). So are the answers whose gesture already says it: arriving where she was called (25 %)
  and the hop (50 %). Refusals and failures are never rolled: they're how a player knows she heard.
- **Order acks are said once every 3 minutes for the same order** from the same player (`Ack`,
  keyed by the order plus what makes it another one: the place, the coordinates, the resource, the
  crop). « suis-moi » sent again gets her following, not the same words. Refusals, outcomes, « je
  suis avec X » and « je te vois pas » are never held back.
- **A burst gets words once** (`_burstGate`, per kind, across players, 30 s): a hello, a goodbye, a
  « gg », a « merci » or a bare call from several players at once. The first player gets the line;
  the others only the gesture (the wave, the hop; a wave for a thank-you or a call, whose window
  still opens). **Papa and Tata always get their words** (`FirstOfBurst`, the owner's call), and
  still start the burst, so whoever follows them gets the gesture.
- **Lines come from small pools** (most have 2 to 9), heard by the same few players in one evening,
  so every pick in game is `ResponsePicker.Pick(pool, LineChoices)`: among the 2 lines said least
  recently, not among the half of the pool not said lately. A line comes back only once the pool has
  nearly gone round. A new send path uses it too.
- **Who hit her is known for sure** since 1.19.4: the server's `damage_event` names the cause (the
  shooter, for an arrow), which Mineflayer passes as `entityHurt`'s second argument. A mob, a fall
  or fire is no player, and she says nothing. On older versions there's no source, and the body
  falls back to guessing the closest player within reach (`exactHits`).
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
- **She defends whoever a monster hurts near her, and Papa and Tata against players.** The same
  `damage_event` tells her who hurts whom, as long as they're in her sight. She goes when free, during
  an activity, following or guarding anyone, or busy with an order she can start again
  (`Restartable`: collect, harvest, come, goto, guide), and goes back to it after (`resumeFor`,
  `resumeAfterBreak`, shared with running away). Gathering and harvesting look around where she
  stands, so she first walks back to where she stopped (`back`, 20 s at most), then the same
  command runs again from the pockets as they were (`since`): what she cut before the fight still
  counts and is handed over, with no second acknowledgement. Only what's over in seconds or can't be
  left keeps her (a furnace, handing over, storing, a bed, a portal, running away). She never hits
  family, nor a creeper.
  - **A monster** hitting any player within 16 blocks of her: she fights it, for up to 20 s,
    silently. Body-only, like eating. A monster hitting *her* is *Fights*.
  - **A player** hitting one of them is the owner's three steps, by that player's streak
    (`McHitStreaks.Tally`, same one-minute window, `DefendStage`, walked by the checks): the first
    hit gets a warning (`McFamilyHitLines`, rationed), the second within the window she hits them
    back once (`McFamilySwatLines`, said at once), the third and after she fights them for 15 s
    after each hit (`McFamilyFightLines`, the start said at once). Busy with someone's order, the
    body answers `cantDefend` and it stays words. While she's fighting someone, their hits on her
    don't count as hits: no remark, no running away (`_defending`, 30 s). Below 3 hearts she breaks
    off and eats. PvP must be on for her hits to land.
- In-game lines are **plain text under 256 characters**: no custom emotes, no sticker tags (the
  checks enforce it).
- **Kaomoji in game must be drawable by Minecraft's font**, which covers the Basic Multilingual
  Plane through Unifont, one code point at a time. So: nothing past U+FFFF (most emoji, `👁`), no
  combining marks (`(•̀ᴗ•́)`, `( ͡° ͜ʖ ͡°)`: the accents land beside the face), no format characters,
  no `§` (colour codes), no Discord markup (`<:`, `<a:`, `<@`, `<#`, `<t:`, which would show raw;
  a plain `<` like « <3 » is fine), and no script that needs shaping
  (Malayalam's `ദ്ദി`). The checks reject the first four. Katakana, box drawing, `ω`, `ಠ`, `✧`,
  `✿` all draw fine. The in-game set (`syncs-voice.md`) is wider than Discord's, but used
  sparingly, the owner's call: **about one line in four** at most, none in most pools of two or
  three lines; dry lines and Papa hurting her stay bare.
- She posts nothing on Discord when she joins or leaves the server: it was noise, so keep it that way.

**Family in game.** `McFamily.Of` matches a player against `Minecraft:Owner` (Papa) and
`Minecraft:Tata` without case, and the service picks a twin pool (`…Owner`, `…Tata`) for the
lines aimed at one person: the « oui ? », their death, them hitting her, their advancement, and the
acks for come, follow, guard and a delivery. Twins call them « Papa » and « Tata », never `{0}`
(the checks enforce it). Same tiers as on Discord (`personality/people.md`, *Favouritism*): Papa is never
roasted and, when he hits her, she's hurt and forgives; Tata gets cheeky warmth and the mute
rights. Every other pool is shared, because it's about the game rather than the person. A new
person-aimed pool gets its twins too, and its name goes in the checks' `McFamilyPools`. Offline
mode lets anyone take those names, which is fine: names only pick lines here.

## Deployment

The Dockerfile has a `node:22-bookworm-slim` stage that runs `npm ci` from the lockfile, then copies
`index.js` and `lib/`. The final image gets the `node` binary and `/app/body`. The service looks for the body at
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

The workarounds below are all in `lib/compat.js` (its header is the checklist for a bump, see *The
body's code*), and « Voir son corps » in `/debug minecraft` shows which ones the server's version
needs.

**`tick_end` after every move (PrismarineJS/mineflayer#4189).** Since 26.3 the server accepts one
position packet per client tick and resets that count only on the client's `tick_end`, which real
clients have sent after every tick since 1.21.2. Mineflayer never sends it, so a 26.3 server
(ViaBackwards or not) kicks a stock bot for `invalid_player_movement` on its second move, about
1.5 s in. `sendTickEnds` wraps the client's `write` and follows every movement packet with a
`tick_end`, on versions whose protocol has one. Remove it once Mineflayer sends the packet itself,
and check the issue before bumping Mineflayer.

**A hair wider than a player (`playerHalfWidth = 0.3001`).** Mineflayer's collisions leave her
side exactly on a block's face (the step she's jumping up, a wall she walks along), and a 26.3
server takes that as walking into the block: it puts her back 0.01 short, every tick, with no
warning in its console, and she never climbs a one-block step. 0.0001 off the face is enough and
fits every gap a player fits. Found and checked on a local server (*Testing movement locally*):
without it she's stuck at the first step; with it she climbs, sprinting or not.

**Creepers, whatever she's doing** (`creeperTick`, `dodgeTick`), **in sight only** (hers, or that of the player it's
near: one in a cave under them can't get at anyone). Out of everyone's sight 3 s, the dodge ends; out of hers alone,
she waits for it to show rather than shoot the tree; and an arrow whose target went behind something while she drew
goes up. The same goes for the creeper-behind warning, from the player's eyes. guarding, a creeper was always
`guardTick`'s; busy otherwise (a field, fishing, gathering, following) she paid it no mind and one
blew up beside her. One closer than 10 blocks, or within 10 of a player near her (it blew up by the player at her
camp: at 5, three 1.1 s draws came too late), now takes her off whatever can be left and picked up
again (`canDefend`'s rule, `resumeFor`'s way back): shot from 6 blocks or more with a bow and
arrows, else kept out of its reach, never hit by hand; dead, gone or 16 blocks off, she goes back to
it. What can't be left (a furnace, handing over, a bed, a portal, running away) is over in seconds.

**The wider box has two edges to keep (`clearFaces`, the `AABB` patch).** The server places her
(a correction, a teleport) with its own 0.3, so often flush against a face, and her 0.3001 box
then starts 0.0001 inside that block. Mineflayer's collisions only stop a box that starts outside
one, so she walked straight in, was put back flush, walked in again: 364 times in 20 s against a
dark forest step, never arriving. After each placement she's moved out of any face she overlaps by
under 0.001 (`clearFaces`, on a microtask: Mineflayer's own handler for the packet is registered
after hers). And Mineflayer compares faces exactly, so a box rounded a hair past one
(-127.99999999999999 for -128) lost the block in front of it the same way: its three
`computeOffset` take the game's own 1e-7 tolerance. Each half alone still let corrections through.

**Her keys go to the server (`inputTick`).** Since 1.21.2 clients send `player_input` (forward,
jump, sprint, shift…) whenever their keys change; Mineflayer sends it only for sneaking, with every
other key off. `inputTick` sends her whole control state whenever it changes, as a real client
does; not while riding, where `leaveBoat` holds shift itself. **It runs first in the physics tick**
(`prependListener`): Mineflayer moves her, then runs the tick handlers (which press and release
keys for the next tick), then sends the move, so keys read after the handlers would reach the server
a tick early. The same handler sends the `tick_end` of a tick that sent no move, so the server
counts every tick, as with a real client. Neither was what stopped her at steps (the local server
refuses her with or without them), but both are what a client sends.

**`entity_action` by name (`fixEntityActions`).** 1.21.6 moved sneaking to `player_input` and
renumbered the other actions; Mineflayer 4.39 still sends the old numbers, so on 26.1 « start
sprinting » arrived as « start riding jump » and getting out of bed as « stop sprinting ». The
wrapper sends the old numbers as names on versions with the new order. Check Mineflayer's
`physics.js` and `bed.js` before bumping it, and remove the wrapper once they send names.

**The packets 26.1 changed** (`attack`, now its own packet, `use_entity`, `block_place`,
`use_item`, `arm_animation`, `client_command`; `window_click` in 1.21.5) all encode right from what
Mineflayer 4.39 writes, and every action built on them was checked on the local server, on the
server's side (`node run.js actions`): eating, armour (given, off, on, handed back, full pockets), storing, handing over, smelting, sleeping and
getting up, fishing, a boat. Two loose ends: Mineflayer waits 2.5 s for « done eating », a meal takes
1.6 s, and a slow link eats the rest, so a timeout with the food gone is not a failure; and the
server keeps where a sleeper lies in `sleeping_pos` on 26.x, for anyone reading player data.

**Teleports are answered once** (`calmTeleports`). Lying down in a bed, the server puts her in it,
Mineflayer answers every teleport with a confirm and a move to the same spot, and Paper, which still
has her where she stood that tick, takes the move for a sleeper wandering and puts her back: 230 of
each a second until the tick ended, and a kick for « too many packets » (Paper's packet limiter is on
by default; local server). The confirm always goes; the move to the spot she was just put in goes
once, and again only 250 ms later. **A second over 200 packets is logged** with the busiest ones
(« sending N packets a second », every 30 s at most): a normal second is under 60, so the next
flood is named in the add-on log before Paper's kick.

**A search that runs out of time ends nothing by itself.** Out of time (`thinkTimeout`), the
pathfinder walks her to its best point and never searches again (it only does when a path is
reset): she stood there with her goal set, silent. And a goal out of reach, which a fast machine on a
small world ends with `noPath`, runs out of time instead on the Pi or in a big loaded world, so it
became a walk that never ended. `timeoutTick` searches again once she has walked a timed-out partial
path, if it got her 2 blocks or more; got nowhere, or a timed-out search whose best point is where she
stands (`stalledSearch`), and it's `noWay()`, the same as `noPath`. A 250-block trip on the Pi's
budget re-planned ten times and got there; the unreachable lane says « no way » in 3 to 5 s at any
speed. **The Pi's budget is reproduced with `MC_CPU_FACTOR=3.5`** (the time budgets divided, about
how much slower a Pi 5 runs JavaScript than a desktop): run `all` with it before shipping anything
that makes a search heavier. Step costs read state ids (`getBlockStateId`), never `blockAt`: a whole
block per step slowed the search past its limit once.

**What hurts is avoided.** The pathfinder avoids fire, cobwebs and lava; `gentleMovements` adds sweet
berry bushes, wither roses, soul fire and powder snow, makes a magma or campfire floor cost 40, and a
step beside a berry bush, cactus or wither rose cost 8 (a turn cut close clipped a bush: a heart).

**Server teleports while she walks are logged** (`watchCorrections`, « server teleport while
walking », every 30 s at most): what the server sent, where she was, her keys, her last move sent
and the block ahead, and every block her box touches at her feet as her client knows them. A body
stuck against the terrain looks the same from outside whatever the server objects to; this line
says what it refused.

**Testing movement locally** (`tools/mc-pathtest`) is in `minecraft-body.md`.


**stdout carries events only.** The protocol libraries print their errors through `console.*`, so
the body sends every console method to stderr before loading them, and the service logs any stdout
line that isn't JSON instead of parsing it.

Options (`config.yaml` → `run.sh` → `Minecraft__*`): `minecraft_host` (empty: off),
`minecraft_port`, `minecraft_username`, `minecraft_owner`, `minecraft_tata` (Tata's in-game name,
for her lines only). If the server has a whitelist, her name
must be on it. Offline mode means anyone can log in under any name, so she trusts names only to
choose who to talk to, never for anything that matters.
