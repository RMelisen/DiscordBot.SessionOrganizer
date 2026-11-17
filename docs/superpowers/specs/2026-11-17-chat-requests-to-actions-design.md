# Chat requests become actions — design

## Intent

Today a request aimed at SYNCS (« syncs, organise un valo samedi 21h ») only gets a line pointing at
the command. The owner wants her to feel smoother and more natural: she should read the details of
the request and answer with something one click away, instead of sending people to type the command
themselves.

## Goals

- She reads what a request says, not just its topic: a title or game, a day and time, vote options,
  what to remind, how many teams.
- She answers with her line, saying what she understood, plus the action.
- Better cues: requests phrased without a topic word (« organise un valo samedi ») or as a proposal
  to her (« on se fait un among us demain 21h ? ») are recognised.

## Non-goals

- Nothing for messages not aimed at her (owner's call: only when spoken to — a mention, a reply to
  her, or her name). No unprompted `/tonight` offer on « qui est chaud ce soir ? » said to the room.
- Giveaway, quiz and moderation requests keep pointing at their command, as today.
- No new command, no migration.

## Anti-goals

- A misread must never put a wrong public card in a channel.
- Nobody but the asker can act on her offer.

## Approaches

- **A — always an asker-only button that opens the existing private flow.** A misread costs nothing;
  everything the flows already do (overlap warning, remembered choices, selects) comes for free. One
  extra click. **Picked**, for sessions, votes and `/tonight`.
- **B — act at once when every detail was read**, with « Annuler ». More magical, but a misread is a
  public card and the session skips category, places and voice channel. **Picked only where nothing
  public happens**: reminders (saved at once, « Annuler » button) and `/teams` (stateless, dealt at once).
- C — a button opening a pre-filled form: you can't see what she understood before clicking, and the
  preview still comes after. Dropped.

## How it works

**Reading** — a pure helper (`Helpers/Personality/ChatRequest`), checkable without a gateway:
`ChatRequest.Read(content, nowUtc)` returns the topic and its details, or null.
- **When**: the longest run of words that `SessionTime.Parse` (sessions, `/tonight`) or
  `ReminderTime.Parse` (reminders) accepts, so the date readers stay the single source of truth
  (`FrenchDate` refuses unknown words, so a window scan finds the date inside a sentence).
- **Title / game / reminder text**: what is left once the request words (« organise », « tu peux »,
  « une session de », « rappelle-moi de »…), her name and the date are removed, with the original
  spelling and case kept. Capped at the matching `InputCaps`.
- **Vote options**: « X, Y ou Z », « X ou Y » after the topic word or a colon (`PollRules` rules: 2–10).
- **Teams**: a count (« 3 équipes », « en trois ») and « équilibré(es) ».

**Cue changes** (`MessageCues`):
- A session imperative (« organise », « planifie », « programme », « prépare », « lance ») with a date
  the session reader accepts is a Session request even without a topic word; « ce soir » stays
  `/tonight`'s.
- A proposal said to her with a date (« on se fait / on fait / on organise / et si on faisait un X
  samedi 21h ») is a Session request. Questions about what is planned stay `NextSession` (read first).
- « rappelle-moi », « préviens-moi », « fais-moi penser à » are Reminder requests on their own.

**Answering** — in `ChatterService`, the same hook as her live answers (`TryAnswerFromRecords`, run off
the gateway handler): a Request with a topic that has an action goes there; anything else keeps its
pointer line.

| Topic | What she does |
|---|---|
| Session | her line with what she read + « Préparer la session » → the private preview (`schedule:rv`), title and time filled in, the person's usual category, places and voice; a missing title or time opens the same modal `/schedule create` would, pre-filled |
| Tonight | her line + « Lancer l'appel » → the `/tonight` card posted in the channel (game, hour; 21:00 by default); an open call already → the link, like the command |
| Vote | her line + « Préparer le vote » → the vote modal, title and options filled in |
| Reminder | saved at once (same caps and limits as `/remindme`), her line with the exact time + « Annuler »; no time read → she asks for one and points at `/remindme` |
| Teams | dealt at once in the asker's voice channel (same card and « Remélanger » as `/teams`); not in voice → she says so |

Offers live in memory (a `DraftBox`, 1 hour), keyed by an offer id carried in the button's custom-id
(`chatreq:session:{id}` …). A click by someone else gets a private « c'est pas ta demande » line; an
expired offer says so and names the command. The used button is disabled after the click. In-memory
by design: a restart forgets open offers.

## Constraints

- A chat message can't get a private reply: her line and the button are public; everything after the
  click is private (preview, modal) or is the same public card the command would post.
- Every click defers before touching the database (3 s rule); her line goes through `BotChat`, with
  mentions off (the title is the person's own words).
- New lines follow `docs/syncs-voice.md`; Papa gets the same actions with his own short pool.
- `/debug answers` counts the actions under their own kinds.
- Checks: cases for the reader (dates inside sentences, titles, options, reminders, counts) and for
  the new cues, including what must stay `NextSession` or not a request at all.

## Left to the builder

Exact pool wording and sizes, the offer id scheme, where each click handler lives (the session one in
`ScheduleModule`, whose drafts are private to it; the rest in `Interactions/Components/`).
