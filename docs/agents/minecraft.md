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
`BotResponses.Minecraft*.cs`, through `ResponsePicker`, so `docs/syncs-voice.md` and the checks
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
- **The conversation streak** is Discord's (`ConversationStreak`, its own instance, per player): only
  what wears her patience counts (`McTalk.Wears`: an insult, a low-effort word, nothing understood). The
  third becomes `StreakNudgeLines`, the fifth ends it (`StreakEndLines`) and she says nothing to them
  for 10 minutes. Never Papa.

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

**In `minecraft/remarks.md`**: how often she speaks in game (the two gates, `RemarkBudget`, rolled remarks, order acks, bursts), the small pools, being hit and fighting back, and the kaomoji Minecraft's font can draw. Read it before adding or changing anything she says in game.

## Deployment

**In `minecraft/deployment.md`**: the body's Docker stage and how the service finds it, the server's version and ViaBackwards, and every Mineflayer protocol patch (`tick_end`, the wider box, `player_input`, `entity_action`, the 26.1 packets, teleports, search timeouts, stdout). Read it before touching the Dockerfile's node stage, `pickVersion`, a patch in `lib/`, or after a server update breaks her.
