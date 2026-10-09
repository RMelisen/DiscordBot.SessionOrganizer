# Plynlings

The virtual pet. `/plynling` (`PlynlingModule`), card buttons in `PlynlingComponentHandler`, state
rules in `Helpers/PlynlingLife`. Money, items and cosmetics are in `economy.md`. **Before writing any
Plynling line, read `docs/plynling-writing-style.md`.**

Traits, stats, the personality title, events, stress and modifiers are in
`plynling-events.md`.

**`/pl` is a shortcut for `/plynling`**, asked for by people on phones ("/pl v" finds "pl view",
never "plynling view"). Discord has no aliases, so `PlynlingModule` is **abstract and group-less**
and `Commands/PlynlingGroups.cs` registers it twice: `PlynlingLongModule` (`/plynling`) and
`PlynlingShortModule` (`/pl`). Discord.Net picks up inherited `[SlashCommand]`s and
`[CommandContextType]`, and skips abstract modules, so **a command added to `PlynlingModule` lands in
both** — and counts toward each one's 25-subcommand cap. **Never put a `[ModalInteraction]` or
`[ComponentInteraction]` on `PlynlingModule`**: it would register once per group, two owners for one
custom-id. Keep the handler `protected` there and bind it in `PlynlingLongModule` only
(`ignoreGroupNames` lets it answer a modal opened from either group). Both subclasses repeat the
base constructor, so a new dependency goes in three places. Texts keep saying `/plynling`; the
shortcut is mentioned once in each help.

## State is computed; nothing ticks

`Plynling` stores hunger, happiness and hygiene **as they were at `NeedsAsOf`**; `PlynlingLife`
derives current values from elapsed time. Every transition **rebases** (compute, store, restart the
clock). `Rebase` must compute **every** value before storing any — `HappinessAt` reads the stored
hygiene.

**`PlynlingLife.Settle` brings a row up to now, and every read in `PlynlingService` goes through
it**: an expired self-freeze thaws at the moment it was due, then a starved one dies at the moment it
starved, so a death found by a command and one found by the sweep are identical (same instant, age,
memorial). Age is `AgeBankedSeconds` + the live stretch since `LiveSince`; frozen and dead time is
never banked.

**One living Plynling per person is enforced by a partial unique index**
(`HasFilter("\"DiedAt\" IS NULL")`), so racing adoptions or resurrections can't both land. Services
still check first; the index makes the check safe.

**`PlynlingSweepService` (hourly; first pass 2 min after start, so an update takes effect at once;
`/debug sweep` runs one on demand, never alongside a running one) is a safety net** for what nobody
else triggers: announcing deaths
(`DeathAnnounced`), the single warning DM (`WarningSent`, re-armed by feeding), thawing expired
self-freezes, time-earned badges and moments. It saves the flag **before** the side effect, so a
failed announcement is logged once rather than retried hourly. Deaths and resurrections are
announced in the guild's game channel (`PlynlingAnnouncer.ResolveGameChannelAsync`: the one set with
`/config game-channel`, else `DefaultGameChannelId` when it belongs to that guild). A guild with
neither is logged and skipped, never cross-posted.

### Sleep (01:00–05:00 Paris)

Time of day, not a need: `PlynlingLife.IsAsleep` reads the Paris wall clock; `Mood` returns
`Sleeping` (frozen still wins); nothing is stored. Hunger keeps dropping, but **death moves**:
`EffectiveDeathAt` puts a death due in the window at 05:00, and `Settle`, `ShouldWarn` and the feed
re-arm all read the effective instant, never raw `DeathAt`. The warning (`WarnAt`) is 3 h before
effective death, pulled back to 23:00 when it would land between 23:00 and 05:00. `WakeAfter` and
`WarnAt` build instants from the Paris wall clock because both DST changes fall in the window (the
harness pins 2026-03-29 and 2026-10-25).

### Happiness, gift and sulking

`PlynlingLife.MealFactor` scales a meal's **hunger** only: ×1.15 above 80% happiness, ×0.75 below
30%, measured before the meal. `IsSulking` (happiness < 0.5%, shown as 0%) refuses every meal free of
charge — **except while starving**: the sulk must never be what kills it (and at night petting is
refused, so nothing could end it). **The happy gift** is one draw per Paris day (`LastGiftDay`,
stored so a restart can't grant a second), made on the owner's first look while happy (`/plynling
view`, or a pet or meal from the card); a look while not happy leaves the draw unspent. Stress levels
and modifiers change drain rates, the meal factor and the gift chance — see `plynling-events.md`.

### Hygiene

A third need; dirt is a knock-on, never a killer. Below `DirtyBelow` (33%), happiness drains
`DirtyHappinessFactor` faster **from the exact instant** hygiene crosses it — `HappinessAt` is two
exact straight pieces. « Laver » is owner-only, free, rationed in memory like petting.

### Sickness — one 05:00 morning at a time, every roll hashed

`LastMorningDay` is the last morning played; `Settle` plays each later morning in time order (a
starvation due first wins and stops the loop; frozen mornings are skipped but recorded). **Rolls come
from `PlynlingSickness.Roll(id, day, purpose)`, never a `Random`** — the same morning must decide the
same way whoever settles it. `Settle` stays pure, so its journal moments ride on
`Plynling.PendingMoments` (not mapped) until `FlushMomentsAsync` — **every save path after a settle
must call it**. `DeathCause` and the `FellSick` / `Recovered` journal kinds are ints
(**append-only**). Sick means: half meals, no games, no visits either way, no self-freeze (freezing
pauses the illness and would dodge the death rolls). `/admin plynling cure` sends no DM on purpose;
`/debug plynling` exists because sickness is rare by design.

### Life stages

`PlynlingLife.Stage` derives them from `Age`, never stored: bébé (< 7 d), ado (< 14 d), adulte (< 180 d), ancien. A frozen
Plynling doesn't grow up; a resurrected one resumes. Nothing about needs or death reads them; they
gate trait draws (`plynling-events.md`). Every species shows the label; only a **bébé** of a species in
`PlynlingArt.StagedSpecies` gets its own picture (ado/ancien art was dropped by the owner).

## The card and care

**Components V2** (root rules apply). Two verbs, `plyn:pet:{id}` and `plyn:feed:{id}`, offered only
while alive and not frozen. A press rewrites the card in place with her line. Feeding and petting
happen **only on the card** (the commands were removed); someone else's is reached with `/plynling
view user:`. `PlynlingCareService` is what the buttons call.

- **« Nourrir » is a select on the card** (one fewer round trip). Anyone may feed anyone's; a
  non-owner pays double (`PlynlingLife.FeedPrice`) from their own wallet in the same save. Each
  option's label shows the **owner's** stock of that food instead of the price when they have any
  (`PlynlingService.GetPantryAsync`, passed to `BuildCard` by every call site — the card is one
  message everyone sees). The pantry is served first (`economy.md`).
- **Feeding pays and feeds in one save**: `FeedAsync` loads the wallet through
  `PebbleService.GetOrCreateWalletAsync(its own context, …)`, not through a second context.
- **« Caresser »** is hidden while asleep (and the pet refused). It is **never greyed out for a
  cooldown** — the cooldown is per petter on a shared card. The cooldown is an in-memory
  `CooldownGate` keyed on (petter, Plynling), released when the pet is refused; the refusal shows
  the ready time via `CooldownGate.TryClaim`'s overload as `<t:…:R>`.
- **« 💞 En couple avec … »** comes from `PlynlingService.GetPartnerAsync` (living partner only,
  none on a dead card), loaded with the rest by `PlynlingModule.BuildCardAsync`, which every call site uses.
- **Everything that shows the card defers first** (the care buttons; `adopt`, `view`, `freeze`,
  `thaw` and the passion modal): each reads and saves before it knows what to show, and the card
  reads more, which on the Pi can outrun Discord's 3 s. A care button's refusal is an ephemeral
  follow-up and the card is redrawn through `ModifyOriginalResponseAsync`. A command defers
  publicly (the card is public), so its refusal deletes the deferred reply and follows up privately
  (`RefuseAsync`). Checks that need no database (an empty name, a cooldown in memory, staff rights)
  still refuse before deferring.

**Freezing has two owners.** A self-freeze (`FrozenByStaff = false`) follows rules that exist only
to stop people escaping death: hunger ≥ 50%, 14 days max, thawable early, 7-day cooldown after it
ends. A staff freeze has none of them and is lifted by staff only; the owner is DM'd whenever staff
freeze, thaw or rename theirs. `LastSelfThawAt` is written only when a *self*-freeze ends.

**`/plynling abandon` deletes the row** — never graveyard, never resurrectable. So it's a slash
command confirmed by typing the name (`PlynlingCardUi.NamesMatch`: ignores case, outer spaces and
runs of spaces, but not a missing space), never a button. The cost is shame: a public
`PlynlingAbandonLines` announcement, `AbandonHits` on `/shame` (*L'Indigne*), and a 30-minute
in-memory adoption cooldown (`PlynlingCooldowns`). Grief runs **before** the deletion (which takes
the relations with it); the shame point is recorded in its own `try` **after** — a failed write must
not undo the abandonment.

**Names are hostile input**: `PlynlingCardUi.SafeName` (`Format.Sanitize`) *and*
`AllowedMentions.None` on every message carrying one — the death announcement is public.

## `/plynling play`

**Secrets never leave the process.** The hidden rock, the throw and the number live in
`PlynlingPlayService` (singleton, sessions by short id, 10-minute expiry), never in a custom-id
(readable from the client). Rules are pure in `Helpers/PlynlingGames` with the `Random` passed in;
text in `Helpers/PlynlingGameUi`; card `PlynlingPlayCards.BuildGame`. Each move runs under the
session's `Gate`, so exactly one move finishes the game, ends the session and pays through
`PlynlingService.FinishPlayAsync` (happiness, counts, cailloux in one save). The hourly limit is
claimed at **start**, so abandoning never rerolls. A restart ends games in progress.

## Visits

**An invitation, with a knock meant to ping** — `AllowedMentions` with the invited owner's id only.
« Accueillir » carries the visitor's Plynling, the host and the expiry in its custom-id (nothing
secret); only the host may press it, within the hour. **Once a day per pair of owners, either
direction**, in memory in `PlynlingCooldowns`, released if the visit fails **or `VisitAsync` throws**
(its save is its last step, so a throw means nothing happened — a held claim would answer « déjà
vus » for a visit that never took place). **« Accueillir » defers before any database work**: on the
Pi the reads and the save outran Discord's 3 s, leaving the visit claimed with the knock never
closed. Its refusals are therefore ephemeral follow-ups, and the knock closes through
`ModifyOriginalResponseAsync`. **A visit pays no
cailloux** (two accounts could farm it). `Plays`, `PlaysWon` and `Visits` are recorded for badges.

**Decided before it starts, told as a story.** `VisitAsync` saves everything first; then
`Helpers/PlynlingVisitStory.Build` (pure) picks a place open at that Paris hour and tells it in
**five to eight steps** (four plus half the script's lines): arrival; the script's opener; the rest
of the script two lines to a step, the last pair finished by a closer that turns talk into doing;
the activity with a two-line exchange; parting plus `PlynlingPlayCards.VisitOutcomeLines`. Pools are
keyed by `VisitMood`: the bond **after** the visit, or `Conflict` for a bad scene or enemies. A
restart mid-story can't change what happened. **Exception: the arrival uses the bond *before***
(`MoodFor(true, Before)`), so Conflict arrivals are for enemies only.

**The parting follows the outcome when the visit broke something**: `DeparturePool` gives a refused
confession `RefusedDepartures` (the visitor always confesses, the host said no) and a break-up
`BreakUpDepartures` — exactly the outcomes `OutcomeFace` turns sad. Other partings stay mood-keyed,
so they must read right just before any outcome line.

### Conversations — `Helpers/PlynlingScripts`

**One script, never lines drawn separately** (separately drawn lines never answered each other).
Whole conversations of **2, 4, 6 or 8** `ConvoLine`s: A's opener (narration `\n` words), then
spoken lines each answering the last, from whichever Plynling the script says (some talk twice in a
row — two in a row share one bubble, use it on purpose). Keyed by passion, `ConvoFlavor` (rivals and
conflicts `Tense`, otherwise `Friendly`) and whether B shares the passion; a typed passion only gets
the generic `ForCustom` scripts, which name it as `{P}` only.

- Scripts are drawn by opener (for the picker's no-repeat history) — **openers must be unique within
  a key** (harness checks).
- **Add whole scripts, never loose lines.** Lengthen by inserting in the middle, never appending —
  the last line leads into the activity.
- **Two lines to a step**, and the closer goes to whoever did **not** say the last line, so the
  final pair is always an exchange. Hence the **even** count (harness checks). A two-line script is
  opener + one answer that lands alone.
- Keep each key's length mix: about a third at 2, half at 4, the rest 6 or 8.
- `PlynlingScripts.cs` is generated from the scratch writing sheets but reads fine by hand.
- Templates: `{A}`/`{B}` names, `{ils}`/`{Ils}` (« elles » only for two girls),
  `{a:m|f}`/`{b:m|f}`/`{p:m|f}` agreements. The harness expands every line for all four gender
  pairs and builds full stories.

**Faces.** `VisitBeat` holds the text and a face per Plynling: happy, content, sad, or
`PlynlingMood.Angry` (visits only — `PlynlingLife.Mood` never returns it). Defaults come from
`Faces(mood)` (speaker, listener, narration); a parting follows the outcome (refused/break-up sad, new
couple happy, new enemies angry). A line may start with a tag, stripped before display: `[sad]` on a
spoken line is the speaker's face, on narration both; `[A:sad B:happy]` sets each. Only the four faces
parse — the harness fails on any line still starting with `[`.

**The story card never moves on its own.** Each step: a `MediaGallery` of both sprites (that step's
face, alt text = name), the text, « 3/7 », and ◀ ▶ (`vis:prev:{story}:{beat}` / `vis:next:…`,
disabled at the ends). « Accueillir » closes the knock in place and posts step 1 as a **follow-up**
(a new message at the bottom). No timer, no background edit. Every step before the last also has
« ⏭ Fin » (`vis:last:…`); on the last, ▶ becomes « ↺ Début » (`vis:first:…`) and « Fin » goes —
every button its own verb. A gallery of two spans the width, so the sprite is shrunk inside its
picture: `PlynlingArt.VisitSprite` links the `_visit` files (larger transparent canvas,
`VISIT_CANVAS` in `tools/plynling-art/export.py`, 80%). Stories live in the `VisitStories` singleton
(last 300) with a snapshot of both Plynlings (name, species, stage, passions), so paging needs no
database; the picture URL is rebuilt per step.

### Passions

`Plynling.Passion` is one of 12 in `Helpers/PlynlingPassions`, stored as an int (**append-only**),
rolled at adoption. **`TaughtPassion` is hostile free text** from `/plynling passion` (2–40 chars, no
links, 24 h cooldown; staff clear it with `/admin plynling passion-reset`), sanitised and sent with
pings off. `PlynlingPassions.Resolve` upgrades a typed text naming a catalog passion.

- Catalog passions have hand-written pools (openers, shared lines, activities, pair combos); a
  custom one appears only through generic templates as `{P}` — **never after « de » or « à »**
  (« parler de les trains »). `Expand` inserts `{P}` **last**, so typed text is never read as a
  template. `{S}`/`{L}` are speaker/listener, with `{s:m|f}`/`{l:m|f}`.
- Beat 2's speaker is visitor or host at random; the subject is a shared passion 60% of the time
  when there is one.
- The activity is the squabble in a conflict; otherwise `MoodActivityShare` (30%) from
  `Activities[mood]`, the rest from the subject: a catalog passion's activities **plus** any combo
  with the listener's passions (combos join the pool, never replace it — each of the 25 pairs has
  three), or the custom ones.

**A typed passion is quoted back, Tomodachi-style** — one that stayed free text
(`PlynlingPassions.Typed`, not recognised by the catalog) is the owner's own words. In a visit, when either has one, 45%
(`TypedMomentChance`) of the time it comes up once outside the conversation — arrival, exchange or
parting, from `TypedArrivals` / `TypedExchangesOwnerFirst|Second` / `TypedDepartures` (`{S}` its
owner, `{P}` the text; the exchange pool is chosen by who owns it, the visitor speaking first; a
typed parting never replaces a refusal's or break-up's). On the card, `PlynlingPassions.PickLines`
swaps in `PlynlingPetTypedLines` / `PlynlingFeedTypedLines` / `PlynlingVisitKnockTypedLines` 15%
(`TypedLineChance`) of the time, with the passion as the **last** format argument (`{1}` for pet,
`{2}` for feed and knock) — callers always pass it. The text never opens a sentence (lowercase).
`/plynling view` adds a thought bubble: `PlynlingPassions.Thought` returns `PlynlingThoughtTypedLines`
(or `PlynlingDreamTypedLines` asleep) 25% (`ThoughtChance`) of the time, never dead or frozen, and
only when no gift line fills the card.

## Relationships — `Helpers/PlynlingBonds`

One `PlynlingRelation` per pair, **lower id first** (unique index); cascades with either Plynling,
survives a death. Hidden compatibility is **derived from the two ids**, never stored. `PlynlingBond`
is an int, **append-only**.

**Where a pair ends up is decided by compatibility, with wide scatter — not by how often it visits.**
`BaseSceneChance` (55%) sits near break-even; `AffinityPull` draws affinity toward about +2 + 4 ×
compatibility (compatibility −20…+20, so −78…+82). Over 100 visits bonds spread roughly 18% enemies /
20% rivals / 22% acquaintances / 19% friends / 10% best friends / 11% couples. Changing the range
reshuffles every existing pair. A couple feels no pull and gets `LoversBonus` instead. `BondMargin`
(10) stops a pair on a band edge flipping (and being announced) every visit. **Retune by re-running
the simulation, never by feel.**

The bond follows affinity (`BondFor`), except a couple: only a confession makes one, only a slide
below +40 undoes it. **Confessions are a boy and a girl only — the owner's explicit choice, keep
it** — and one living partner at a time (`InCoupleAsync` counts living partners only, or a widow
could never love again). `VisitAsync` takes a `Random` so scenes are checkable. **Grief** (best
friends and partner fall to 20%) runs on death, from the sweep, and on abandonment before the row
is deleted.

## Badges and journal

`Helpers/PlynlingBadges` is the catalog (16, each with a **stable key** — a rename orphans every
earned copy). `PlynlingBadge` rows have a unique index on (Plynling, key), which is what really makes
each reward paid once. **`AwardAsync` and `AddMomentAsync` never save**: an action, its moments, its
badges and their cailloux land in the caller's one `SaveChanges`. Time-earned badges, the « est
devenu… » moments (dated by `PlynlingLife.StageStart`) and the death moment are written by the sweep.
A stage moment is written only within 2 days of the stage starting (`PlynlingTraits.JustGained`, the
window trait moments use): past that, a missing one was trimmed, and re-adding it as the oldest
entry would evict the next-oldest — another stage moment the next sweep re-adds, every hour.
`JournalKind` is an int (**append-only**); moments store kind + detail and are worded at display
(`PlynlingJournalUi`) so they follow gender. At most `JournalCap` (500), oldest dropped — counting
moments added earlier in the same save, and the new moment itself: a backdated one older than the
whole journal is dropped, not a newer memory. Both tables cascade: an abandoned Plynling takes its
journal with it; a dead one keeps it.

## Gender

**Every Plynling line exists in both genders, enforced by the type.** Each pool in `BotResponses` is
a `GenderedLines(M, F)`; a call site must use `.For(p.Gender)`. A girl is *une Plynling*. Text not
about one specific Plynling (`/plynling help`, `/help`, README, command descriptions, person-level
refusals like `NoPlynling`) stays generic masculine. Short words go through `PlynlingGrammar.Agree`.
The `plynlingui` harness bans `il`, `-le`, `mort` in `F` and `elle`, `-la`, `morte` in `M` (whole
words, allow-list for *la mort*). `PetCooldown` has no gender (refused before the Plynling loads).

## The mascot — Ping-Qilin

A girl Amanite whose passion is naps, **one per guild**, an ordinary row owned by the bot's user id —
so view, pet, feed, list and journal need no special case. `Helpers/PlynlingMascot` holds her data
and the owner id, **bound on Ready by `PlynlingMascotService`, never hardcoded** (dev and prod are
different applications). It creates her row in any guild lacking one (any bot-owned row counts) on
every Ready and on `JoinedGuild`.

- **She cannot die**: `Settle` hands her row to `Tend`, so it holds on every read. `Tend` tops a need
  up to 90% (happiness 80%, hygiene 100%) when it falls under 50% — not to full, so others' meals
  still matter — cures illness silently, and marks mornings played without rolling. A staff freeze is
  left alone. Grief or a refused confession fades on her next read.
- **Her badges pay nothing** (a reward would open a wallet for the bot and put cailloux from nowhere
  into `/admin dashboard`); her visits find no items, for the same reason.
- **She speaks for her own Plynling**: `MascotPetLines` / `MascotFeedLines` replace the usual pools
  (no typed variant), `/plynling view` opens with `MascotViewLines`, « Laver » answers with
  `PlynlingText.MascotBath`. These are plain `string[]`, **not** `GenderedLines` (an M half would be
  dead text).
- **Visiting her skips the knock**: `/plynling visit user:@SYNCS` runs at once, posts a
  `MascotWelcomeLines` line instead of the knock (no ping), then the story. Both entry points go
  through `PlynlingVisitRunner` (claim, scene, release on failure, story) so they can't drift; each
  keeps its own pre-checks. Otherwise a bot host is refused.

## Art

- **Living = animated WebP; memorials and foods = PNG.** `PlynlingArt.Sprite` ends in `.webp`, the
  others `.png`; they share `Version` (4) but not the extension — don't tidy them into one. A client
  that can't animate shows frame 0, kept pixel-identical to the v2 still. Idle motion lives in
  `tools/plynling-art/motion.py`, and **nothing moves side to side** (the owner rejected every
  left-right motion) — keep motion vertical or in place.
- **`PlynlingArt.SpriteOf` is the one way to picture a living Plynling** (card, play card, journal,
  knock, announcements); it adds `_dirty` when hygiene < 33% and not frozen. `DirtyMoods` must match
  `DIRTY_STATES` in `export.py`. Visit stories stay clean (`VisitSprite` never takes it) — a
  decision. See `tools/plynling-art/README.md` for the mud and the fly.
- `StagedSpecies` must match `STAGED` in `export.py` (artcheck compares them). The adult filename has
  no stage segment. Every mushroom baby is hand-drawn (`<species>_baby` in `species.py`) — a derived
  shrink kept the adult's full-size cap.
- `PlynlingArt.Key` is exhaustive and **throws** on an unkeyed species (never default to one).

## Families — dormant

`PlynlingFamily`, six sunflower species and their catalog rows exist, and `PlynlingCatalog` rolls
within a family, but `/plynling adopt` always rolls a mushroom and no sunflower art exists. The rest
is Tasks 3–5 of `docs/superpowers/plans/2026-09-24-plynling-gender-and-sunflowers.md`, deferred on
purpose. `PlynlingSpecies` is **append-only**; the family is `SpeciesInfo.Family`, not a column.
**Every line is family-neutral** — never name a cap, petals or spores.

## Graveyard

`/plynling graveyard` is Components V2 with two button rows and two verbs (`grave:sort:` newest /
longest life, `grave:page:`). Budget 24 of 40 (container, heading, five picture rows × 3, footer, two
rows of two). Changing the sort resets to page 0. It settles every living Plynling in the guild
before listing. Ties break on id.

## `/plynling help`

Within ~100 characters of the 6000 embed cap (5 894). Measure before adding; shorten or split rather than
let it throw. The main `/help` points to it in one line only.

## Data migrations and the production launch

- `AddPlynlingPassions` backfilled an innate passion from the id (`(Id * 5 + 1) % 12`). Deriving it
  at read time was rejected — a 13th passion would have changed every existing one.
- `PrepareProdLaunch` (scoped to the production guild) zeroed wallets, deleted its
  `EconomyDailyStats` (the beta's money) and copied Pwet from a test server as a newborn. Its
  timestamps are computed in C# inside `Up` — never generate a SQL script from it.
- `Helpers/PlynlingLaunch` once chose two people's *first* Plynling on production (a Coprin and a
  Girolle) and gave the second an id whose compatibility hashed to the maximum (+20). It was removed
  once both existed; the two rows are now ordinary ones.
- `SwapLaunchPairGenders` flips those two living Plynlings' genders (Coprin male → female, Girolle
  female → male), matching on current species and gender so it is a no-op on any row that is no
  longer the launch one. Nothing else stores a gender — compatibility comes from the ids, the art is
  not gendered, journal moments are worded at display.
