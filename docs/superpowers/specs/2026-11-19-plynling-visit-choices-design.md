# Plynling visits — two choices instead of a story

Design approved in conversation with the owner on 2026-11-19. Replaces the told visit story
(`PlynlingVisitStory`, the passion conversations of `PlynlingScripts`, the ◀ ▶ pager) with a short scene
in which both owners make a choice.

## Intent and the why

People like visiting each other and watching relationships form, but nobody reads the visit story: they
press « ⏭ Fin » and read the outcome lines. That is rational: everything is rolled before the story starts
(`PlynlingService.VisitAsync`), so the five to eight steps are decoration, and the only news is on the last
page. Most visits end on « +8 % de bonheur » and nothing else, because bonds move slowly by design.

The rework makes a visit **something both owners do together**: the visitor's owner picks what their
Plynling does, the host's owner picks how theirs reacts, and the two choices, matched against the other
Plynling's personality, sway whether it goes well. To play well, each owner has to know the *other* one's
Plynling (its traits, visible through « Personnalité »). That is the social pull the owner is after.

## Goals

- A visit is **one short scene** (a few sentences), **one choice per owner**, and **one ending** built from
  those choices — readable in the time the old « ⏭ Fin » took.
- **Choices matter**: they shift the odds of a good visit, through the other Plynling's personality.
- **Confessions become a choice** of the visitor's owner, and the high point of a visit.
- Scenes are **story files**, written in the village's voice, parsed and validated like the events.
- **20 scenes + 1 confession scene at launch**, more added later in waves.

## Non-goals

- No change to what a visit *does* beyond the scene roll and the confession: once a day per pair, items
  found, happiness, badges, sickness/freeze/sleep refusals, the relation bands, grief, the after-visit
  events (`trigger: visit`, e.g. `forgot.story`) all stay as they are.
- Not played by the event engine: no event instances, no 24 h queue, no deciding alone, no stress.
- No new reward for visiting (a visit still pays no cailloux).

## Anti-goals (a failure even if it works)

- **Numbers on the buttons.** The odds are never shown — no 🎲, no percentage, no stakes line. The owner
  was explicit: some choices add chance, some take it away, and people find out by playing.
- **Choices that can be learned by heart.** If the "right answer" is the same for every host, or the
  edge is large, people farm couples in days. Luck stays, the edge is capped, and the answer depends on
  *who* the host (or visitor) is.
- **Couples arriving much faster than today.** Today a boy and a girl are a couple within 60 visits 30 %
  of the time (median ~51 visits at compatibility +10). Average play must keep that; good play may only
  shave a little off.
- **A visit that stalls** waiting for someone who has left.
- **Bland again**: the same three buttons on every knock. Reactions are written per scene, with varying
  attitudes.

## Constraints

- Everything in `CLAUDE.md` and `docs/agents/plynling.md` (*Visits*) still binds: the once-a-day claim in
  two halves (stored `LastVisitDay` + in-memory `PlynlingCooldowns`, released if the visit fails or throws),
  **defer before any database work** on the host's click, the knock pings the invited owner only (and does
  not ping from `;pl visit`), Components V2 rules, `AllowedMentions.None` on V2 cards, user-chosen names
  through `Format.Sanitize`/`SafeName`.
- **Confessions stay a boy and a girl only, one living partner at a time** (`PlynlingBonds.CanConfess`) —
  the owner's explicit choice.
- **Retune by simulation, never by feel** (`PlynlingBonds`).
- Text follows `docs/plynling-writing-style.md` and `docs/village-lore.md` (the village is Humeval; cosy,
  every scene stands alone).
- Discord caps: 5 buttons per row, 80-char button labels, custom-id 100 chars, every button its own id.
- The story format's rules carry over where they apply: a load error stops the bot, every problem reported
  with file and line, keys are forever.

## Approaches considered

- **A. A new kind of story file, played by the visit code — chosen.** Visit scenes live in
  `ProjectSYNCS/Stories/Visits/`, read by the existing `StoryParser` / `StoryValidator` (extended), and
  played by `PlynlingVisitRunner`. Visits keep their own flow (knock, 2 h, once a day); the writing is
  done and checked like the rest of the village. Cost: a small extension of the format.
- **B. Visits as events.** The visitor's pick an event instance, the host's reaction a handover. Most code
  reuse, but the event rules fight visits: 24 h to decide vs the 2 h knock, the « Événement » queue
  filling up, deciding alone, stress, "events never make a couple except by handover". Rejected.
- **C. Scenes in C#**, like the old passion scripts. Least code, but against the 8.0.0 direction
  (everything is a story file), harder for the owner to review, nothing validated at startup. Rejected.

## How a visit plays (decided)

1. **The visitor starts it** — `/plynling visit user:@x` or `;pl visit @x`. All of `OpenVisitAsync`'s
   checks run first, unchanged. A scene is drawn (see *Drawing a scene*) and posted **publicly**: the
   place, the intro, and the visitor's 2–4 options. Only the visitor's owner may press them. **This
   message is the one that pings** the invited owner (an edit never pings), as the knock does today — and,
   as today, not from `;pl visit`, whose own message already did. *(Claude's call: the host is told a
   visit is on its way a moment before the reactions appear, rather than a second message.)*
2. **The visitor's owner picks.** The same message becomes **the knock**: the scene, what the visitor's
   Plynling chose to do, and the host's 2–4 reactions, written for that scene. Only the host's owner may
   press them, within two hours (`VisitInviteLife`, counted from step 1).
3. **The host's owner picks a reaction — that is accepting the visit.** There is no separate « Accueillir ».
   No answer within two hours: the knock closes as expired, no visit, as today.
4. **The odds** are computed (below), the scene is rolled, `VisitAsync` applies and saves everything.
5. **The ending** replaces the knock in the same message: the two Plynlings (faces from the ending), the
   visitor's half, the host's half, then `PlynlingPlayCards.VisitOutcomeLines` (confession, bond change,
   badges, finds, happiness). One message for the whole visit; no pager.

Sketch of the knock (step 2):

```
🌳 Sous le grand chêne
Le cerf-volant de Mochi est coincé tout en haut du grand chêne.
Pix a choisi : grimper le chercher.
@hôte, que fait Mochi ?
[ Tenir le tronc ]  [ Grimper aussi ]  [ Rire en bas ]
```

Sketch of the ending (step 5):

```
🌳 Sous le grand chêne
[Pix, content]  [Mochi, content]
Pix grimpe et décroche le cerf-volant du premier coup.
Mochi applaudit si fort qu'un écureuil en tombe de sa branche.
💞 Pix et Mochi sont maintenant meilleurs amis
-# +12 % de bonheur pour Pix et Mochi
```

### Who presses what

- **Visitor's options**: the visitor's owner only. **Host's reactions**: the host's owner only. Anyone else
  gets the usual private refusal (`NotYourInvite` or its equivalent).
- **Her own Plynling (the mascot)** as host: no knock, no ping, as today. The visitor picks, then she
  "reacts" on her own: the reaction is chosen from her traits (the reaction whose lean best matches her
  axes, ties by hash), and the visit plays at once. Her `MascotWelcomeLines` line stays.

### The odds (decided: B, hidden)

- **Base chance**: today's `PlynlingBonds.GoodSceneChance(compatibility, bond, affinity)` — compatibility,
  the pull toward the middle, the lovers' bonus and the enemies' penalty all stay.
- **The visitor's option is judged by the host's personality**, and **the host's reaction by the
  visitor's personality.** Each option and reaction carries 1–2 `[lean <axis> ±n]` (the `AiAxis` axes the
  story format already uses). The judging Plynling's axes are the sum of its traits' `Axes` (what
  `PlynlingPersonality` sums). An option leaning the way the judge leans raises the chance; one leaning
  against it lowers it.
- **The shift is capped**, so luck always stays. Starting point for the simulation: at most about ±15
  percentage points from both choices together; the final cap and scale come from the simulation.
- **Nothing is shown.** Buttons are plain labels. The leans sit in the story files of a public repo, so a
  determined reader could work out the best choices; **the owner accepts that** (as for the events).
- **The bond simulation is not in the repo** (it was the owner's scratch harness). Rebuild it in
  `tests/ProjectSYNCS.Checks` with a realistic spread of traits, and first reproduce the baseline figures
  in `plynling.md` (12/18/21/19/13/16 %, 30 % couples within 60 visits, median 51 at +10) before tuning.
- **Simulation targets** (with the new chance): with random choices,
  the bond spread over 100 visits and the couple timings match today's (within noise); with the best
  choice every time, couples come only modestly faster. Document the result in `plynling.md` next to the
  existing numbers.

### Confessions (decided: B)

- **No more automatic confessions.** The `RollConfession` path after a good scene goes; a confession only
  happens because the visitor's owner chose it.
- When the pair qualifies (`CanConfess`, evaluated when the visitor starts), the scene's options gain one
  extra button, « Lui avouer ses sentiments ». Pressing it switches the visit to **the confession scene**
  (its own file), written for that moment: its own intro, and the visitor's options are *ways* of
  confessing.
- The host's owner still picks a reaction (that is still how the visit is accepted). **The confession is
  not hidden** *(Claude's call: step 1 is public and shows the button, so hiding it at the knock would hide
  nothing)*: the knock says plainly that the visitor has come to confess, and the suspense is the answer.
- **The answer is a roll**: today's acceptance chance (50 % for an average pair, 85 % / 15 % at the
  extremes of compatibility) shifted by both choices exactly like the scene odds, same cap.
- **Affinity, as today** *(Claude's call)*: a confession visit counts as a good scene — the usual +12…17,
  item finds and the closeness relief — then the answer. Accepted: a couple. Refused: `HeartbreakLoss`
  (25), sadness and the `Heartbroken` moment. The confession scene's halves are **yes / no** instead of
  good / bad; there is no separate scene roll.
- **After a refusal, no confession for 3 days** (owner's choice): the button is not offered to that pair,
  in either direction, until 3 days after the refused confession. This is the brake that keeps couples
  near today's pace now that an owner can confess on every eligible visit. It must survive a restart
  (stored, or derived from the stored `Heartbroken` moment — builder's call), and the simulation counts it.
- Eligibility is checked again when the host reacts (someone else may have formed a couple meanwhile). If
  it fails, **the visit does not happen** *(Claude's call)*: the knock closes with a « trop tard » line and
  the once-a-day claim is released, so the pair can still meet that day.

### Drawing a scene

- Each scene says **which bonds it is for** (one or several of: acquaintances — which includes a first
  meeting —, friends, best friends, lovers, rivals, enemies), and may limit its **hours** (Paris time, as
  `VisitPlace` does now) and its **stages**.
- The draw uses the bond **before** the visit, the hour and both stages, and goes through `ResponsePicker`
  (or an equivalent no-repeat rule) so a pair does not see the same scene twice in a row.
- **Every bond must have at least one eligible scene at every hour and stage** — checked at startup and in
  the checks project, or a visit could find nothing to draw.
- Launch content: **20 scenes**, spread so each of the six bonds has about 4 eligible scenes (a scene may
  serve several bonds), **plus the confession scene**.

### The ending

- **Built from two halves**, so the writing grows by addition, not multiplication: each visitor option has
  a *good* and a *bad* line; each host reaction has a *good* and a *bad* line. A host's line must read
  right after any of the visitor's options (it is about the host's attitude, not the visitor's exact act).
- Then the outcome lines, unchanged (`VisitOutcomeLines`).
- Faces: from the result (good: happy/content by bond; bad: angry or sad), and a parting that broke
  something (refused confession, break-up, new enemies) keeps today's faces (`OutcomeFace`). The
  `[sad]` / `[A:… B:…]` face tags keep working on any line.
- **Visit sprites stay clean** (`PlynlingArt.VisitSprite`), as today.

### Templates

`{A}` is always the **visitor**, `{B}` always the **host**, with `{a:m|f}`, `{b:m|f}`, `{p:m|f}`,
`{ils}` / `{Ils}` as today (`PlynlingVisitStory.Expand` or the story engine's equivalent — they must
agree).

## What goes away

- `PlynlingVisitStory`'s pools (arrivals, exchanges, closers, activities, departures, typed passion
  moments) and the build of the 5–8 step story; `PlynlingScripts` (the passion conversations) entirely.
  Passions themselves stay (card, thoughts, stats); only their visit use goes.
- « Accueillir » and the knock line pools (`PlynlingVisitKnockLines`, `PlynlingVisitKnockTypedLines`) —
  the scene's intro replaces the knock line.
- The ◀ ▶ pager for new visits. **Old posted stories keep paging**: the `vis:*` handlers and
  `VisitStories` stay until the last stored story has expired (`KeepDays` = 30 after the release), then
  they are removed in a later version along with the `VisitStoryRecord` table.
- **Old knocks** (`plyn:visit:*:*:*`) still open at deploy: answer them privately as expired and close
  them, rather than playing an old-style visit.

## Content workflow (owner's choice)

As for the village events: **Claude writes, the owner reviews.** Shortlist of the 20 + 1 scenes →
writing sheet in French (scene, options, reactions, halves, leans) → owner review → story files →
checks → dev-guild play-through. No story file is written before the sheet is approved. The prose follows
`docs/plynling-writing-style.md` and `docs/village-lore.md` (named villagers from `Stories/village.cast`
are welcome; places from the lore).

## Left to the builder

- **The file syntax** for visit scenes (header fields for bonds/hours/stages/place, how host reactions and
  good/bad halves are marked), as long as it extends the existing parser and validator rather than
  forking them, and every new word appears in a fixture. A first idea, not binding:

  ```
  # visit: kite
  title: Le cerf-volant
  place: 🌳 Sous le grand chêne
  bonds: acquaintances, friends, best_friends
  hours: 8-20

  Le cerf-volant de {B} est coincé tout en haut du grand chêne.

  * climb | Grimper le chercher
    [lean boldness +2] [lean energy +1]
    good: {A} grimpe et décroche le cerf-volant du premier coup.
    bad: {A} reste coincé{a:|e} à mi-hauteur, juste à côté du cerf-volant.

  > hold | Tenir le tronc
    [lean compassion +1]
    good: {B} tient le tronc avec un sérieux absolu, comme si l'arbre allait partir.
    bad: {B} tient le tronc, puis lâche pour se gratter le nez. Pile au mauvais moment.
  ```
- The exact odds formula (normalising the trait axes, which reach ±200 on some traits, against the
  option's lean), within the targets above. A bébé holds only a childhood trait; that is enough axes to
  judge by, but check the simulation covers it.
- Custom-id layout for the visitor's options and the host's reactions (scene key, option key, expiry,
  ids — within 100 chars, each button its own id) and what happens if a scene's key no longer exists when
  a button is pressed (answer as expired).
- Whether the ending's message keeps the knock's message or the knock closes and the ending is posted
  below (default: the same message is edited in place — one message per visit).
- What the validator checks beyond the usual (2–4 options and reactions, both halves present, label
  lengths, each bond covered at every hour and stage, the confession scene present and unique).
- Validation of the checks project: walk every scene for all four gender pairs, every option × reaction ×
  good/bad, measure the knock and ending cards against Discord's caps with 32-character names.
- Docs: rewrite *Visits* in `docs/agents/plynling.md` (and the *Conversations* section, which goes), add
  the visit file kind to `docs/agents/plynling-storylines.md`, update `/plynling help`, `/help` if its visit
  line changes, and `README.md`.
- Version: a **minor** bump (new user-visible behaviour) with its `CHANGELOG.md` entry.
