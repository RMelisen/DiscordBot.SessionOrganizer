# Visit conversations: eight steps, faces, side-by-side sprites — design

Date: 2026-09-28. Status: approved in conversation, awaiting spec review.
Builds on `2026-09-28-plynling-passions-design.md` (passions and the five-beat story).

## Goal

Make a visit read as a real conversation: five talking stages on one subject instead of two,
each Plynling's face following the conversation (happy, content, sad, and a new angry face),
the two Plynlings shown side by side without names, and the reader paging with ◀ ▶ instead of
an automatic timer.

## 1. Story structure — eight steps

| # | Step | Speaker |
|---|---|---|
| 1 | Arrival: place scene + `Arrivals[mood]` | narration |
| 2 | Opener | A — the one whose passion is the subject |
| 3 | Reaction | B (always) |
| 4 | Talk | see below |
| 5 | Talk | see below |
| 6 | Closer: the talk winds down and leads into the activity | A or B (50/50) |
| 7 | Activity + small-talk exchange (unchanged) | narration + both |
| 8 | Departure + outcome lines (unchanged) | narration |

A and B are chosen as today: speaker visitor or host 50/50; the subject is a shared passion
60 % of the time when there is one, else one of A's.

**Talk steps (4, 5).** Each step's speaker is the other one than the previous step's speaker
70 % of the time, the same one 30 % of the time (so someone may talk twice in a row). What they
say depends on who they are:

- **A says a Detail** — an anecdote or a fact about the subject: `PassionInfo.Details` for a
  catalog passion, `CustomDetails` (with `{P}`) for a typed one.
- **B says a Follow-up** — a question or a comment: `Followups[(mood, shared)]`.

Both pools are written so that two lines in a row from the same speaker read naturally.

**Opener (2).** Unchanged: narration + A's line, one `\n`.
**Reaction (3).** One line from B: `Reactions[(mood, shared)]`, now **single lines**. When the
subject is shared and a catalog passion, a `SharedLines` narration line follows, as today.
**Closer (6).** One line from `Closers[mood]` (new).

**A talking step shows one speech bubble**: `💬 **Name** : line` (plus the opener's narration on
step 2 and the shared line on step 3).

### Pools

| Pool | Change | Keyed by | Speaker |
|---|---|---|---|
| `Reactions` | the first line of every current two-line entry | (mood, shared) | B |
| `AnswerFollowups` | **the second lines** of the current entries, rewritten where they only made sense after their first line | (mood, shared) | A |
| `Followups` | new, 5 per key | (mood, shared) | B |
| `PassionInfo.Details` | new, 10 per passion | passion | A |
| `CustomDetails` | new, 10, `{P}` standalone | — | A |
| `Closers` | new, 6 per mood; `{S}`/`{L}` = whoever speaks / the other | mood | either |

A's talk line is drawn from `Details` or `AnswerFollowups` (50/50 — a fact, or a reply that
keeps the exchange going); B's from `Followups`. Unchanged: Arrivals, Openers, SharedLines,
Activities, Combos, CustomOpeners, CustomActivities, Exchanges, Departures, Places.

## 2. Faces

Visit faces are four: **happy, content, sad, angry**. `PlynlingMood` gains `Angry` (appended;
never produced by `PlynlingLife.Mood`, so an everyday card never shows it).

**Defaults** by mood, for each Plynling in each step:

| Mood | Narration steps (1, 7, 8) | Speaker | Listener |
|---|---|---|---|
| Acquaintances | content | happy | content |
| Friends | happy | happy | happy |
| BestFriends | happy | happy | happy |
| Lovers | happy | happy | happy |
| Rivals | content | happy | angry |
| Conflict | angry | angry | angry |

Step 8 follows the outcome first: a refused confession or a break-up → both sad; a new couple →
both happy; newly enemies → both angry; otherwise the table.

**Tags override the defaults.** A line may start with a tag, removed before display:

- `[sad]` on a **spoken** line → the speaker's face.
- `[sad]` on a **narration** line (arrival, opener narration, shared line, activity, departure)
  → both faces.
- `[A:sad B:happy]` (or either half) on a narration line → each face separately. `A`/`B` mean
  the visitor and the host.

Allowed faces in tags: `happy`, `content`, `sad`, `angry`. When a step carries two lines (e.g.
the opener's narration and its spoken line), the spoken line's tag wins for the speaker.

**A tag pass** over every existing pool adds tags where a line's emotion clashes with the
default (a sad line among friends, an angry line between acquaintances…).

**Storage.** `VisitCast` keeps species and life stage instead of one sprite URL; `VisitStory`
keeps, per step, the pair of faces. The URL is built at render time from species, stage and
face, so ◀ ▶ still needs no database. A baby keeps its baby art in every face.

## 3. Card and navigation

```
## 🥖 À la boulangerie
[ sprite A ] [ sprite B ]      MediaGallery of two, each in this step's face (animated WebP)
─────────────
step text
-# 3/8
[ ◀ ] [ ▶ ]
```

- No names or roles under the sprites; each gallery item's description (alt text) is the
  Plynling's name. Accent colour: the host's, as today.
- Known trade-off: on a phone a two-item gallery spans the full width, so each sprite is about
  half the screen. Accepted; judged on the first live run.
- **Arrows only.** « Accueillir » closes the knock (« ✅ … l'histoire est juste en dessous ↓ »)
  and posts the story as a follow-up at step 1 **with ◀ ▶ already there**. ◀ is disabled on step
  1, ▶ on step 8. `PlayStoryAsync` and `BeatPause` are removed. Paging (`vis:prev` / `vis:next`)
  is unchanged; after a restart a click is told privately that the story is gone.
- The outcome lines stay on step 8.

## 4. The angry face (art)

In `tools/plynling-art`:

- `sprites.face`: an `angry` state — brows slanting down to the middle (a V), narrowed eyes flat
  on top, a small downturned mouth, flushed cheeks.
- `sprites.extras`: a small 💢-style anger mark near the top of the Plynling.
- `motion.pose`: the mark pulses in place, a short vertical stomp, a small steam puff rising
  straight up. **Nothing moves side to side.**
- Every species' drawing must accept the state (checked by exporting all of them).
- `export.STATES` gains `angry`: 14 new files (7 adults, 7 babies), e.g.
  `plynling_cepe_angry_v4.webp`. New files, no `ART_VERSION` bump.
- A preview image of the 14 faces is sent to the owner for approval **before** the bot uses them.

## Verification

1. `dotnet build` — 0 warnings, 0 errors.
2. The scratch harness, extended: every template expands for all four gender pairs with no
   leftover `{…}`; every tag parses and names an allowed face; no displayed line still starts
   with `[`; a full story built from test Plynlings has 8 steps and a face pair per step; the
   talk steps' speakers follow the rules (step 3 is B).
3. The rendered angry faces, approved by the owner.
4. A visit in the dev guild (owner).

## Docs

`CLAUDE.md` (visit story paragraph: eight steps, arrows only, gallery, faces and tags),
`README.md` (visits), `tools/plynling-art/README.md` (the angry state, 14 new files).

## Out of scope

- Faces other than the four listed (no hungry or sleepy faces in visits).
- Tags that name a Plynling other than the visitor and the host.
- Changing the outcome rules or the bond odds.
