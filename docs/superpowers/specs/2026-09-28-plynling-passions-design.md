# Plynling passions and five-beat visit stories — design

Date: 2026-09-28. Status: approved in conversation, awaiting spec review.

## Goal

Make a visit read like a little Tomodachi Life scene: the two Plynlings talk about something
one of them loves, the other reacts according to what they are to each other, and what they do
together follows from it. Passions are the new ingredient. Separately, an accepted visit's story
is posted as a new message at the bottom of the channel instead of replacing the knock.

This is the first writing pass. The pools are expected to grow over several later rounds, so the
structure must make adding lines, passions and combos a data-only edit.

## 1. Data and catalog

### Two kinds of passion

- **Innate** — one of a fixed catalog of 12, rolled at adoption, never changes. Gets rich,
  hand-written content.
- **Taught** — optional free text typed by the owner (`/plynling passion`), like a Tomodachi
  Life word. Used through generic templates that accept any noun phrase. If the text matches a
  catalog passion, it is *upgraded* to that passion and gets the rich content.

### Storage (one migration)

On `Plynling`:

| Column | Type | Meaning |
|---|---|---|
| `Passion` | `PlynlingPassion` (int) | the innate passion |
| `TaughtPassion` | `string?` | the typed text, cleaned, 2–40 characters |
| `TaughtPassionAt` | `DateTimeOffset?` | when it was last set — the 24 h change cooldown |

`PlynlingPassion` is stored as an int, so it is **append-only**, like `PlynlingSpecies`.

Existing rows need an innate passion. The migration backfills them once with a formula on the
id (`(Id * 5 + 1) % 12`), so the result is stable and needs no runtime roll. This makes it the
third migration that carries data; `CLAUDE.md`'s note on data migrations is updated to say so
and why (a new required trait for rows that predate it — nothing is wiped). New Plynlings roll it
at adoption through the same `Random` as gender.

Deriving it from the id at read time instead (the way compatibility is derived) was rejected:
adding a 13th passion later would silently reassign every existing Plynling's passion.

### Catalog — `Helpers/PlynlingPassions.cs` (pure)

```text
enum PlynlingPassion { Cooking, Music, Gaming, Astronomy, Gardening, Rocks,
                       Stories, Dance, Painting, Sport, Insects, Naps }
```

| Emoji | Key | Label (used in `{P}`) |
|---|---|---|
| 🍳 | Cooking | la cuisine |
| 🎵 | Music | la musique |
| 🎮 | Gaming | les jeux vidéo |
| 🔭 | Astronomy | l'astronomie |
| 🌱 | Gardening | le jardinage |
| 🪨 | Rocks | les cailloux |
| 📚 | Stories | les histoires |
| 💃 | Dance | la danse |
| 🎨 | Painting | la peinture |
| 🏃 | Sport | le sport |
| 🐞 | Insects | les insectes |
| 😴 | Naps | les siestes |

Each entry (`PassionInfo`) carries: `Emoji`, `Label`, `Keywords` (normalised, for matching
typed text) and three pools — `Openers` (beat 2), `SharedLines` (added to beat 3 when both
share it) and `Activities` (beat 4). A separate `Combos` table maps an unordered pair of
passions to activity lines; lookup works in either order.

### Matching typed text — `PlynlingPassions.Resolve(string)`

Normalise: lowercase, strip accents, strip a leading article (`le`, `la`, `les`, `l'`, `un`,
`une`, `des`, `du`, `de la`, `de l'`), trim punctuation and spaces. A catalog passion matches
when one of its keywords appears as a whole word in the normalised text. Result: the catalog
passion, or none (custom).

### What a Plynling "has" — `PassionSet`

A small value computed from a Plynling: the innate passion plus the taught one, each either a
catalog passion or a custom text (with its normalised form). Two passions are **the same**
when both are the same catalog passion, or both are custom with equal normalised text.

### Where passions show

- **Plynling card:** one line in the existing text, no extra component —
  `💭 Passions : 🍳 la cuisine · « les trains à vapeur »` (a resolved taught passion shows its
  catalog emoji and label; a custom one shows the text in « guillemets »).
- **Journal:** a new `JournalKind.LearnedPassion` (appended at the end — the enum is stored),
  detail = the text: « S'est pris(e) de passion pour « … ». »

## 2. Commands

### `/plynling passion`

The 15th `/plynling` subcommand (cap: 25).

- Opens a modal, « Quelle passion veux-tu lui apprendre ? », pre-filled with the current taught
  passion. Text input `maxLength: InputCaps.Passion` (40, new constant), not required, with a
  placeholder: « avec son article : la pêche, les trains… ».
- Leaving it empty removes the taught passion. The innate one never changes.
- Refusals (ephemeral): no living Plynling; cooldown not over (« tu pourras lui en apprendre une
  autre <t:…:R> »); text containing a link (`http`, `www.`, `discord.gg`); fewer than 2
  characters after cleaning.
- Cleaning before save: line breaks become spaces, runs of spaces collapse, trimmed.
- On success: `TaughtPassion`, `TaughtPassionAt` and a `LearnedPassion` journal moment in one
  save (the moment only when setting, not when clearing). The card is posted publicly with a line
  in SYNCS's voice from a new `BotResponses.PlynlingPassionTaughtLines` (`GenderedLines`,
  `{0}` = name, `{1}` = rendered passion). `AllowedMentions.None`.
- Frozen Plynlings may be taught (it is not a need, nothing ticks).

### `/admin plynling passion-reset user:`

Third command in that subgroup, after `rename` and `resurrect`. `IsStaff` only, ephemeral.
Clears `TaughtPassion` and `TaughtPassionAt` and DMs the owner from a new
`BotResponses.PlynlingStaffPassionResetDms` (`GenderedLines`, `{0}` = name), the same shape as
the rename DM.

### Safety

The taught text is hostile input, exactly like a Plynling name: rendered through
`PlynlingCardUi.SafeName`'s sanitising everywhere it appears, and every message carrying it is
sent with `AllowedMentions.None`.

### Docs

`/plynling help` (`BuildEmbed`, re-measure the caps), `README.md` and `CLAUDE.md`. The main
`/help` already points to `/plynling help` in one line and does not change.

## 3. Story engine — five beats

`PlynlingVisitStory.Build` stays pure: every draw comes from the `Random` and the `pick`
function it is given. `VisitCast` gains the Plynling's `PassionSet`, captured at visit time like
the sprite.

### Choosing the subject

1. Speaker: visitor or host, 50/50. The other is the listener.
2. If the two share a passion, it is the subject 60 % of the time.
3. Otherwise the subject is one of the speaker's passions: innate or taught, 50/50 when both
   exist.

### The beats

1. **Arrival** — unchanged: `*{place scene}*` then a line from `Arrivals[mood]`.
2. **The subject** — the subject passion's `Openers` if it is a catalog passion, else a line
   from the generic `CustomOpeners` pool. An opener is a line of narration followed by the
   speaker's quoted line.
3. **The reaction** — a two-line exchange (listener, then speaker) from
   `Reactions[(mood, shared)]`: 6 moods × shared/not = 12 generic pools. When the subject is
   shared *and* a catalog passion, one of its `SharedLines` is appended.
4. **The activity** — the first that applies:
   1. a `Combos` line for the subject paired with one of the listener's catalog passions;
   2. the subject's own `Activities` (catalog);
   3. a line from `CustomActivities[mood]` (custom subject);
   4. today's `Activities[mood]`.

   In `Conflict` the activity is always from `Activities[Conflict]`. A line from today's
   `Exchanges[mood]` follows, so the pools already written stay in use.
5. **Departure** — unchanged: `Departures[mood]` then the outcome lines.

### Templates

Existing: `{A}` `{B}` (visitor, host, bold), `{ils}` `{Ils}`, `{a:m|f}` `{b:m|f}` `{p:m|f}`.
New:

- `{S}` `{L}` — speaker and listener (bold), with `{s:m|f}` `{l:m|f}`.
- `{P}` — the subject passion: the catalog label (« la cuisine ») or the typed text in
  « guillemets ».

**Every generic pool places `{P}` so that any noun phrase reads correctly**: after a colon, as a
quoted aside, or as a sentence subject — never after `de` or `à` (« parler de les trains »).
Catalog pools are written for their passion and need not use `{P}` at all.

### First writing pass (volumes)

Per catalog passion: ~6 `Openers`, ~3 `SharedLines`, ~5 `Activities`. ~15 `Combos`.
`Reactions`: ~5 per pool (60). `CustomOpeners`: ~8. `CustomActivities`: ~4 per mood.
Later rounds add lines only.

## 4. Playback and the new card

In `PlynlingComponentHandler`, accepting a visit keeps every existing check, then:

1. `UpdateAsync` on the knock: `BuildKnockClosed` with « ✅ **B** a accueilli **A** —
   l'histoire est juste en dessous ↓ » — buttons gone, `AllowedMentions.None`.
2. `FollowupAsync` with beat 1 of the story (Components V2 flag, `AllowedMentions.None`) — a new
   message at the bottom of the channel. A follow-up needs no channel permission and stays
   editable through the interaction token for 15 minutes.
3. The background task edits **the follow-up** (not the original response) for beats 2–5,
   `BeatPause` (7 s) apart — about 28 s in all. The last beat adds ◀ ▶ and « 5/5 ».

`PlayStoryAsync` therefore takes the follow-up message rather than the component. Paging
(`vis:prev` / `vis:next`) is unchanged: it updates whichever message carries the buttons.

Failures: the visit is saved before any of this, as today. A follow-up that cannot be sent is
logged and nothing else breaks (the knock then says "below" about nothing). A failed beat edit is
retried once then logged, as today. The story card's layout and component count do not change.

## Verification

No test project. Three checks:

1. `dotnet build` clean.
2. A throwaway check in the session scratchpad (not committed) that builds stories across all
   moods, all four gender pairs, catalog/custom/shared/unshared passions and every pool entry, and
   flags leftover `{…}` placeholders, a `{P}` directly after `de`/`à`, exchanges without exactly
   one `\n`, and `Resolve` misses on each passion's own label.
3. A visit in the dev guild, watched end to end, if the owner wants it.

## Out of scope

- Passions affecting anything but visit stories (petting, play, feeding lines).
- More than one taught passion.
- Passions influencing compatibility or bond odds.
