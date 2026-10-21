# `/minecraft` — design

## Why

Her Minecraft body is only reachable from inside the game: the orders she understands are written
nowhere a player can read (the in-game help list was removed on purpose), and Discord can't see
what she does there. `/minecraft` puts that on Discord: a manual for the orders, a window on what
she's doing and what she's done, a way to link one's Discord account to one's in-game name, and a
way for a linked player to bring her onto the server.

## Goals

- Players can learn every order she understands, with phrasings that really work, without asking.
- Anyone can see whether she's on the server, what she's doing, where she is, what she carries, and
  her lifetime stats.
- A Discord member can link their account to their in-game name, with proof.
- A linked member can invite her onto the server.

## Non-goals

- No orders, chat lines or reminders sent into the game from Discord (considered, then dropped by the
  owner): the only effect in game is `invite`.
- No list of remembered places on Discord (dropped by the owner).
- No chat bridge, no economy (no cailloux for anything done in game).
- No change to how Papa and Tata are recognised in game (`Minecraft:Owner` / `Minecraft:Tata`). A
  link never makes anyone family.
- No in-game help list: « syncs aide » stays a call (the owner's call, `minecraft.md`).

## Anti-goals (a failure even if it works)

- Someone gaining Papa's or Tata's standing through a link (offline mode lets anyone log in under
  any name).
- Someone taking over another member's link.
- Any of this breaking or slowing down the body's loop, or an interaction timing out on the Pi.

## Constraints (from the repo; binding)

- Command and option names English, everything shown French. Every module reading `Context.Guild`
  carries `[CommandContextType(InteractionContextType.Guild)]`.
- New top-level group `minecraft`: 7 subcommands (`help`, `status`, `inventory`, `where`, `link`,
  `invite`, `stats`), under the 25 cap.
- Handlers touching the database defer first (3 s rule). Snowflakes stored with
  `.HasConversion<long>()`. No `DateTimeOffset` comparisons in SQL.
- `MinecraftBodyService` is a singleton: database work through `IServiceProvider.CreateAsyncScope()`,
  never an injected DB service. The body's loop thread never awaits the database; stat writes are
  batched like `ActivityCounter` / `ActivityFlushService` (never a write per event).
- In-game names and the server's item ids are someone else's text: `Format.Sanitize` or code spans
  in Discord replies, mentions off.
- Every Discord side effect swallowed and logged; the body never stops because Discord failed.
- New user-facing commands go in `/help` and `README.md`. Version: minor bump (new commands and a
  migration), changelog entry. The `minecraft.md` notes get a `/minecraft` section.
- Discord's caps: 25 choices, 6000 per message's embeds, 1024 per field.

## The commands, as decided

### Read-only (anyone, ephemeral replies)

- **`help`**: every order she understands, grouped (moving; gathering, farming and fishing;
  places; items and chests; timers; talking to her), each with two or three phrasings that
  `McCommand.Parse` really reads, paged with buttons like `/help`. The phrasings are checked: the
  checks parse every example shown and fail if one reads as another order (so the manual can't
  drift from the parser).
- **`status`**: server up or down, players online, whether she's in and since when, and what she's
  doing and for whom (« elle pêche pour Bob », « elle garde la ferme », « elle suit Papa »), from
  the service's current order or activity.
- **`inventory`**: what she wears, holds and carries (the owner's `/debug` text, in a public form).
- **`where`**: her coordinates and dimension, when she's in.

### `link`

- `link name:<in-game name>` replies (ephemeral) with a 4-digit code and the line to type in game:
  a whisper to her, « lien 4821 » (`/msg <her name> lien 4821`). She must be on the server; if she
  isn't, the reply says so. The code is valid 10 minutes, one at a time per Discord member.
- When that player whispers her the right code, the link is saved (Discord member ↔ in-game name,
  one in-game name per member and one member per name) and she whispers back a confirmation line.
  A wrong code gets nothing special. A member linking a new name replaces their own old link.
- **A name already linked to another member is refused** (at `link`, before any code), the
  owner's call: in offline mode « the latest proof wins » would let anyone take over someone's
  link. The holder frees it with `link remove`; the owner (`AvailabilityService.OwnerId`) can free
  anyone's with `link remove` and a `player` option.
- **Papa's and Tata's in-game names can only be linked by `AvailabilityService.OwnerId` and
  `BotResponses.TataId`** respectively, whatever the code says.
- `link remove:true` deletes the member's link. `link` with no option shows the member's link.
- Stored in the database (new table, migration).

### `invite` (linked members only; any linked member)

She joins now and stays at least an hour even if the presence rule (4 players, or Papa online)
isn't met, as long as she's allowed to play (`/debug minecraft` switch). After the hour, the usual
rule applies. Refused while the owner's 30-minute « leave » hold runs. One invite at a time; a
second one while it runs answers that she's already there.

### `stats`

All-time totals, kept in the database, shown as one embed: her deaths, fish caught (and treasures),
blocks gathered (by resource family), crops harvested, monsters killed while guarding, places
remembered, time spent in game, and the orders received (in game), total and the top players by
orders given. Counted in memory from the body's events and written in batches; a restart may lose
the last few minutes of counts, by design.

## Calls made by Claude (check these)

- Read-only replies are ephemeral, to keep channels clean.
- Link proof is a code whispered to her in game (not a code she whispers to them): it works whether
  or not she can find the player, and only the person at that keyboard can type it.
- `invite` lasts an hour; no cooldown beyond « one at a time ».
- Stats are her totals plus orders per player, all-time only: no dated rankings, so no daily buckets.
- With orders, `say` and reminders dropped, the link only gates `invite` for now; it stays because
  it's the base for anything later that acts as someone.

## Left to the builder

- The exact embed/component layout of each reply, within the caps.
- The data model of the link and stats tables (names, keys), following the repo's rules.
- Her lines for the new moments (link confirmed, invited in): in `BotResponses.Minecraft.cs`, in her
  voice (`docs/syncs-voice.md`), game-chat rules (plain text, drawable, one face in four at most),
  checked like the other `Mc*` pools.
- Whether `status`'s « what she's doing » needs a small description helper next to `McJob`.
