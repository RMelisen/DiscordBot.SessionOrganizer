# Visits — `Helpers/Plynling/Visits`, `PlynlingVisitCards`

Part of `docs/agents/plynling.md`: read that first. The visit file format is in `plynling-storylines.md`.

**Played, not told** (since 8.2.0): each owner makes one choice, and the whole visit lives in **one
message**, edited from the first pick to the ending. No pager.

1. **The visitor starts it** — `/plynling visit` or `;pl visit @x`. `OpenVisitAsync` runs every check,
   then `PlynlingVisitRunner.DrawScene` draws a scene for the bond **before** the visit, the Paris hour
   and both stages (`VisitCatalog.Eligible`), through `ResponsePicker` over `visit-scene:<key>` so the
   same scene doesn't come twice running. `BuildChoice` posts the place, the visitor, the intro and its
   2–4 options for the visitor's owner, plus « 💌 Lui avouer ses sentiments » when
   `VisitTermsAsync` says so (one read of the relation gives « met today », the bond and the offer: the
   start answers without a defer). **This first send is the one that pings**, the invited owner only
   (`AllowedMentions` with that id); not from `;pl visit` (the person's own message already did), not
   at her home. Every edit after it sends `AllowedMentions.None`.
2. **`pvc:`, the visitor's option** → the message becomes **the knock** (`BuildKnock`): what the visitor
   chose, and the host's 2–4 reactions for the invited owner. It defers and reads both Plynlings (the
   knock names the host, and the visitor's owner is not in the id).
3. **`pvx:`, « 💌 »** → the same message becomes the confession scene's choice: its options are ways of
   confessing, with no « 💌 ». It asks `MayConfessNowAsync` first (offer day aside): a pair that stopped
   qualifying since step 1 is told privately (`ConfessionGone`) and the message goes back to the scene's
   own options, rather than finding out after the host's reaction.
4. **`pvr:`, the host's reaction — that is accepting the visit.** Within two hours (`VisitInviteLife`,
   counted from step 1; the expiry rides in every id). It defers, runs the refusal chain (gone, frozen,
   asleep, sick: ephemeral follow-ups, message unchanged), then `PlynlingVisitRunner.RunAsync`, and
   `BuildEnding` replaces the message. The visit is saved by then, so `ShowEndingAsync` never lets a
   refused edit swallow it: it falls back to a closed card with the outcome lines, and logs.

- **Ids**: `pvc:{visitor}:{hostOwner}:{expiry}:{scene}:{option}`, `pvx:{visitor}:{hostOwner}:{expiry}:{scene}`,
  `pvr:…:{scene}:{option}:{reaction}` — everything the handler needs, nothing secret, each step its own
  verb. The validator caps scene keys at 20 and choice keys at 12 so `pvr:` stays under Discord's 100. A
  scene or choice key no longer in `VisitCatalog`, or a past expiry, closes the message as expired.
- **The wrong person** gets a private refusal and the message stays as it was: `NotYourChoice` on the
  visitor's buttons, `NotYourInvite` on the host's. The host's check comes **before** the expiry's, so a
  stranger never closes someone's knock. `RunAsync` refuses two Plynlings of one owner (`VisitSelf`)
  whatever the id says.
- **Old messages**: a knock from before 8.2.0 (`plyn:visit:*:*:*`, « Accueillir ») closes as expired, no
  visit. Told stories keep paging (`vis:*`, `VisitStories`, `VisitStoryRecord`, the `VisitStory` /
  `VisitCast` / `VisitBeat` records) until `KeepDays` (30) after 8.2.0; **then remove all of it in a later
  version**, the table with a migration.

**Once a day per pair, either direction**, in two halves: **stored** as `PlynlingRelation.LastVisitDay`
(set in the visit's own save, re-checked there as the backstop when two visits race), because a visit
finds items and moves the bond and a restart must not reopen the day; and **in memory** in
`PlynlingCooldowns`, which turns a double click away before the database is asked — claimed by
`RunAsync`, released if the visit throws, returns nothing or comes back « trop tard » (its save is its
last step, so a throw means nothing happened — a held claim would answer « déjà vus » for a visit that
never took place). **Every click after the first defers before any database work**: on the Pi the reads
and the save outran Discord's 3 s (« Accueillir » once saved a visit whose knock never closed), so
refusals are ephemeral follow-ups and the message moves through `ModifyOriginalResponseAsync`. **A visit
pays no cailloux** (two accounts could farm it). `Plays`, `PlaysWon` and `Visits` are recorded for badges.

**The odds are hidden** (`VisitOdds`). The base is `PlynlingBonds.GoodSceneChance` (compatibility, the
pull, the lovers' bonus, the enemies' penalty). **The visitor's option is judged by the host's
personality, the host's reaction by the visitor's**: each choice carries 1–2 `[lean <axis> ±n]`, matched
against the judge's axes (`PlynlingPersonality.Axes` of its traits, through `PlynlingService.AxesAsync`).
Each choice shifts the chance by `MaxShiftPerChoice` (0.075) × tanh(match / `LeanSoftness` (1000)), so
both together move it 15 points at most and luck always stays; `GoodChance` and `AcceptChance` clamp
to 5–95 %. Buttons say nothing of it. The leans sit in a public repo — the owner accepts that.

**Confessions happen only by choice** — no roll after a good scene. « 💌 » is offered when the pair
`MayConfess`: it `CanConfess` (below) and no refusal between them in the last `RefusalCooldown` (3 days,
either direction, a refused visit or a refused village declaration alike — that event goes by the same
rule — stored as `PlynlingRelation.LastRefusalAt` so a restart does not lift it), **and** today is one of the
pair's offer days: `PlynlingBonds.ConfessionOffered` hashes the pair and the Paris day, true on
`ConfessionOfferChance` (25 %) of them, so restarting the visit cannot reroll it. **The 25 % is the
owner's choice and departs from the design**, which offered it on every eligible visit: an owner
confessing at the first chance then made couples twice as fast (see the simulation below). The save
re-checks eligibility and the cooldown, not the offer day (a knock answered past midnight keeps its
confession); if the pair may no longer confess, the visit **does not happen**: nothing saved, the claim
released, the message closed with `VisitTooLate`, and the pair may still meet that day. A confession
counts as a good scene (+12…17, finds, closeness), then the answer rolls on `AcceptChance` (the old
acceptance chance shifted like the scene); a refusal costs `HeartbreakLoss` and sets `LastRefusalAt`.

**The ending** is two halves and the outcome: the visitor's option's half, the host's reaction's half —
`good` / `bad`, or `yes` / `no` in the confession — then `PlynlingPlayCards.VisitOutcomeLines`. A host's
half must read right after any of the visitor's options. Faces: good → `Faces(MoodFor(true, After))`'s
narration face; bad → sad between friends or closer, angry otherwise; `OutcomeFace` overrides when the
visit broke something (refusal, break-up: sad; new couple: happy; new enemies: angry). A face tag at the
start of a half (`[sad]` for that half's Plynling, `[A:… B:…]` for each) wins. `PlynlingMood.Angry` is for
visits only (`PlynlingLife.Mood` never returns it). Both sprites go through `PlynlingArt.VisitSprite`
(`_visit` files, a larger transparent canvas — `VISIT_CANVAS` in `tools/plynling-art/export.py`, 80 % —
because a gallery of two spans the width), and stay clean.

**Scenes are files**: `ProjectSYNCS/Stories/Visits/<key>.visit`, 36 scenes plus `confess` (20 at launch,
8 for lovers and enemies in 8.2.8, 8 for the weak spots and everyday visits in 8.2.9), the format and the load rules in `plynling-storylines.md` (*Visit
files*). Each scene names its bonds (a first meeting is acquaintances), its hours (Paris; `20-1` runs across
midnight) and its stages. **A scene shared by several bonds is written neutral** (no fondness or grudge
assumed; between enemies a good ending is a moment's truce); **a scene for one bond may lean into it** — the
second wave's lovers-only scenes are openly tender, its enemies-only ones openly squabbling. Lovers exist at
every stage (`CanConfess` only needs best friends), so an all-stage lovers scene stays innocent.
**Every bond × stage pair needs a scene at every hour** (`VisitValidator.Coverage`, at startup and in the
checks), or a visit could find nothing to draw. Templates: `{A}` is always the visitor, `{B}` the host,
`{ils}` / `{Ils}` the pair (« elles » only for two girls), `{a:m|f}` / `{b:m|f}` / `{p:m|f}` agreements —
`PlynlingVisitStory.Expand`. Button labels carry the names raw (a button renders no markdown); text
displays carry `SafeName`. The writing follows `docs/plynling-writing-style.md` and `docs/village-lore.md`;
Claude writes a sheet in French, the owner reviews it, and the files are copied from the approved sheet.
