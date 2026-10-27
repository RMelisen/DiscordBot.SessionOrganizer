# Personality subsystem

Separate from scheduling. `ChatterService` decides *how* to react to a message, `BotResponses`
holds the canned lines (one partial file per category in `Services/Personality/BotResponses/`,
indexed at the top of `BotResponses.cs`), `ResponsePicker` chooses one, `MessageCues` (split by theme in `MessageCues/`) detects intent (mood,
greeting, verdicts, threats, what a message aimed at her is doing), `HerMessages` remembers what her
own recent posts were, `BreakdownService` plays the easter egg, `ReactionService` answers
with an emote instead of words, `RivalryService` handles other bots. Verdicts ("good bot") are
their own subsystem — see `bot-feedback.md`.

This file holds what applies to the whole subsystem. The rest is split by topic under
`docs/agents/personality/`: read this file, then **only the topic file you are changing** (and
whatever it points to). A reference elsewhere to « personality, *Some section* » means the section
of that name in the file below.

| Topic | Sections | File |
|---|---|---|
| How chat is read, and in what order | *`MessageCues`*, *Vocabulary rules*, *`ChatterService.HandleMessageAsync` — branch order* | `personality/cues.md` |
| What a message aimed at her is asking | *Listening*, *Intents — `MessageCues.ReadIntent`* | `personality/intents.md` |
| The rest of listening, and its stats | *Two-step messages*, *Reply context*, *Under her cards*, *Conversation streaks*, *Her records, quoted*, *Answer stats — `/debug answers`* | `personality/listening.md` |
| Emote reactions and pictures | *Reactions and emotes*, *Sticker follow-ups* | `personality/reactions.md` |
| Who she likes, fears and names | *Favouritism*, *Shutdown threats*, *Talked about*, *`RivalryService`* | `personality/people.md` |
| Easter eggs (keep out of `/help`, README, changelog) | *Quoicoubeh*, *Tron / Trouille*, *Glitch* | `personality/easter-eggs.md` |
| What she does unprompted | *Morning hello*, *Ambient life*, *Her seat in voice*, *Welcome*, *Her body* | `personality/ambient.md` |
| Her fun commands | *`/yesno`*, *`/rate`*, *`/ship`*, *`/blame`*, *`/wouldyourather`* | `personality/fun-commands.md` |

A rule you add goes in the topic file it belongs to; only what binds every topic goes here.

## Who handles what

**`ChatterService` and `ReactionService` split the room.** Anything aimed at her — an @mention or
a reply to one of her messages — belongs to `ChatterService`; `ReactionService` skips those so she
never both roasts and decorates the same message. Reactions are for conversations *nobody*
addressed to her: a message qualifies on a `MessageCues` hit, or on being the owner's (anything he
writes qualifies — that's the favouritism), then passes a probability roll and a per-channel
cooldown.

**Her name counts as addressing her.** A message saying « syncs » or « sync » (`MessageCues.NamesHer`) is
`ChatterService`'s too, mention or not: `ReactionService` and the early morning hello skip it, as
they skip a mention, so she never both answers and decorates it. So does one of Papa's pet names,
from him only: every such check goes through `MessageCues.NamesHerFrom(content, fromOwner)`. See
*Talked about* (`personality/people.md`).

**One exception: the energy drink can.** A message mentioning Monster or an energy drink
(`MessageCues.MentionsEnergyDrink`) gets an `EnergyDrinkReactions` can **every time**: no roll, no
cooldown, even on a message aimed at her (only bots, DMs and a running breakdown are skipped). It
names a drink rather than reading the message, so it is not a decoration on a comeback, and it never
takes the mood reaction's turn. "Monster Hunter" doesn't count; posting the `:monster:` emote does.

**`ReactionService`'s two paths are gated differently, on purpose — don't unify them.** Reacting to
a *message* is rationed by `Cooldown` (she volunteers an opinion). Copying someone else's
*reaction* (`HandleReactionAddedAsync`) is odds-only, no cooldown (piling on should feel reflexive).

**The pile-on path skips bots twice.** A reaction *added by* a bot is ignored (bookkeeping marks),
and a reaction *on* a bot's message is ignored (she'd be applauding a rival while
`RivalryService` sulks at it). The second check needs the author, so it costs a fetch and lives
inside the `try` with the owner/self checks.

## Sending

Her chatter goes through `BotChat` (`ReplyWithTypingAsync` / `PostWithTypingAsync` /
`PostEmbedWithTypingAsync`), which pauses behind the typing indicator, capped at 2 s to stay inside
the 3 s `HandlerTimeout`. The embed variant still takes text, to size the pause. `BreakdownService`
keeps its own much slower pacing, knowingly exceeding the timeout for ~a minute once a month
(`BreakdownService.Cooldown`, 30 days).

