# Community: `/confess` and free Steam games

Two things she does for the server that belong to no other subsystem. Files live in the
`Community/` subfolders.

## `/confess`

`ConfessModule` posts an anonymous, numbered embed (« Confession #12 », `ConfessModule.BuildCard`)
in the channel set with `/config confess-channel`. **No channel means closed**: there is no default,
like the quiz channel.

- **Anonymity is the feature.** The command defers **ephemerally** before anything else: a public
  response would show « X a utilisé /confess » to the room. The receipt is « Confession envoyée. »,
  with no emoji (the owner's call). She never comments on a confession; her chiming in would make it
  feel less anonymous.
- **Only the owner can see who wrote one**, with `/debug confession numero:` (ephemeral; from a DM it
  reads the home guild). Not staff: that was the owner's choice, so the author stays private to
  everyone who could hold it against them. `AnonymousConfession` stores the author, the number and
  the posted message, **not the text**: the message holds it, and staff delete an abusive one like
  any other message.
- **Numbering**: per guild, from 1, unique index on `(GuildId, Number)`. `ConfessionService`
  (singleton: it holds the lock and the cooldown, and scopes `AppDbContext` per call) reserves the
  number by saving the row **before** posting, under a `SemaphoreSlim`, so two confessions at once
  can't both be #12. A post that fails removes its row and gives the cooldown back; numbers can
  have gaps.
- **Cooldown**: one confession per person every 10 minutes (`CooldownGate`, in memory). Text capped
  at `InputCaps.Confession` (1000) at the option.
- **Mentions never ping**: sent with `AllowedMentions.None`. The text is not sanitised, so people
  can still use formatting.

## Free Steam games

`SteamFreeGamesService` (its own 3-hour loop, first pass 5 minutes after start) posts every Steam
game that turns **free to keep** in each guild's `/config freegames-channel` (no channel, no
announcements): her line (`BotResponses.FreeGameLines`, `{0}` the bold sanitised name, `{1}` the
normal price), a card (`SteamFreeGamesService.BuildCard`) and a « Voir sur Steam » link button.

- **Only games you keep**, the owner's call: no free weekends, no free-to-play, no DLC.
  `Helpers/Community/SteamFreeGames` reads Steam's public store endpoints, no key: the search
  (`maxprice=free&specials=1&category1=998`, i.e. games 100 % off) gives app ids, and the app-details
  call confirms each one is `type: game`, not `is_free`, at `discount_percent: 100`. Free-to-play
  games are listed free, not discounted, so the search never returns them. Pure, so it's checkable
  offline.
- **The end date of the offer is not shown**: Steam's API doesn't carry it, only the store page does.
- **No repeats**: `freegames-state.json` (next to the database) maps each announced app id to when.
  An entry is dropped 30 days after the game stopped being free, so a later giveaway of the same
  game is news again. Ids the details call turned down are remembered in memory only.
- **When nothing is marked**: while she is silent (`/debug silence`), or while no guild has a
  channel. The game then goes out once that changes, if it is still free. Once every channel has had
  its try, the game is marked even if one channel failed, so the others don't get it twice.
- She posts this on her own initiative, so silence mode mutes it; it's a dedicated channel, so it
  doesn't go through `BotsChannel`.
