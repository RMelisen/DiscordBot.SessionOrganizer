# Plynling events: waves 8 and up (CK3 survey, rewards, long arcs)

**Status (6.0.3):** wave 8 shipped as content only (181 events, recorded in `2026-10-07-plynling-events-waves-2-3.md`). It covers the grand arcs below except the regatta's free bets, and splits endings by roll instead of a story score. The engine items in section 1 (score, earned titles, favours, tiered outcomes, `FellSick` target) are still open.

A selection plan, not a writing sheet. Same pipeline as before (writing sheet, owner review, catalog,
harness, dev-guild read-through, one patch version per wave). CK3 ids are pointers for inspiration
only: nothing is translated or copied, and every line is written fresh in the Plynling voice
(`docs/plynling-writing-style.md`).

**Source:** the real event scripts on the E: install (readable again since 2026-10-08), not only the
localization. They show what each option *does* (`add_gold`, `give_nickname`, `change_variable`, ...),
and that is where the reward ideas below come from.

**Already in the catalog, so avoid:** everything in `2026-10-07-plynling-events-waves-2-3.md`
(waves 2–7), including the pet rock, the mystical animal, the apprentice, the market rivalry, the
befriend and court schemes, the judge, the chronicle, the bridge, the student and the sea.

## 1. Rewards: what CK3 gives and what we have

How often each effect appears across CK3's 578 event files, to show what CK3 leans on:

| CK3 effect (uses) | Plynling equivalent | Status |
|---|---|---|
| `add_opinion` (5736) | `AffinityShift` | ✅ |
| `add_character_modifier` (3392) | `ApplyModifier` | ✅ |
| `add_prestige` / `add_piety` (5100) | — | ❌ no sink, see "rejected" |
| `duel` (2705) | `EventChallenge`, `VsTarget` | ✅ pass/fail only |
| `random_list` (2204) | — | ❌ **gap: tiered outcomes** |
| `add_trait` (2105) | `GainCoping` (breaks only) | partial |
| `add_stress` (1206) | `StressChange` | ✅ |
| `add_trait_xp` (621) | — | ❌ gap: trait tracks (later) |
| `add_hook` (577) | — | ❌ **gap: favours owed** |
| `add_gold` (439) | `GiveCailloux` | ✅ |
| `set_relation_friend` / `_rival` | `SetAffinityAtLeast` / negative shift | ✅ |
| `create_artifact` (266) | `GiveItem` | ✅ catalog items only |
| `set_variable` / `change_variable` (story cycles) | — | ❌ **gap: story score** |
| `give_nickname` | Cosmetic **Title** slot already exists | ❌ **gap: earned titles** |
| `create_character_memory` | Journal moments | ✅ |

### Proposed new effects, by value for cost

1. **Story score (`ChainScore`): the key to long arcs.** CK3's story cycles keep a variable (the
   bell-of-Huesca cycle counts what was done, and the chariot race tracks a wager). Store one `int`
   on the event instance, have `FollowUp` copy it to the next step, let options add to it
   (`AddScore(n)`), and let options or endings gate on it (`ScoreGate(atLeast)`). Today
   "a follow-up cannot know which option led to it" forces one key per branch, so a 6-step arc with
   3 choices per step is unwritable. With a score, the final scene can have three endings that
   remember the whole story. Cost: one column + migration, FollowUp copies it, the story render shows it.
2. **Earned titles (`give_nickname`).** The Title cosmetic slot exists. Add a
   `CosmeticSource.Earned` (never sold, never crafted) and award it via `GiveItem("cos.title.…")`, or a
   dedicated `GiveTitle`. Each long arc's *best* ending grants one: « {A} le Lauréat / la Lauréate »,
   « l'Amiral·e de la mare », « l'Âme de la fête ». This is the reward players show off. Check the
   scratch checker's "never legendary" item rule against cosmetics before reusing `GiveItem`.
3. **Favours owed (`add_hook`).** « {B} doit un service à {A} »: stored per pair, granted by a social
   event, **spent** later through an option with `FavourGate` that always succeeds. Only the
   holder's own option spends it, so another owner's Plynling is never touched (anti-griefing
   holds). This gives social arcs a payoff days later.
4. **Tiered outcomes (`random_list`).** A third result on challenges: an *exploit* when the roll beats
   the difficulty by a margin (`CritOutcome` + `OnCrit`). It adds rare memorable moments with no new
   content shape. Cheap: the roll is already stored on the row.
5. **Echoes: events that remember earlier ones.** CK3's *I Want a Pony!* (court_events.3090 → 3091/3092):
   a child declares a dream, and years later the dream comes back. `Condition` needs to read
   "resolved event X with option Y" from history. Then `baby_grow_up`'s four answers each get an
   adulte-stage echo. These are the longest follow-ups possible: across stages, weeks apart.
6. **Chain badges (accolades).** A `BadgeEvent` per long arc's best ending ("Héros de la crue"). Cheap,
   uses the existing badge system.
7. **Upgrading modifiers.** A content pattern, not engine work (party_baron: *making efforts* turns
   into *beloved*): a later step applies a stronger modifier with the same theme. Needs nothing new.

**Later, bigger:** trait XP tracks (`add_trait_xp`: a « Pêcheur 1→3 » lifestyle trait built up across
many events), and unique keepsakes with their own history text (CK3 artifacts record where they came
from). Both are new storage; do them after the arcs prove the score/title loop.

**Rejected:** prestige/piety as a currency (nothing to spend it on, and a third number on the card);
betting real cailloux (deciding alone can never spend, and an `OwnerOnly` wager hits empty
wallets). Bets stay free (« parier un gland ») and only pay out.

**Payout caps for long arcs** (extend the scratch checker): ≤ 30 cailloux per effect as today, **≤ 60
per whole arc**, at most one item and one title per arc, rewards weighted toward the ending. Deciding
alone always reaches the *middle* ending (never the title, never a malus).

## 2. Timing constraints that shape "long"

Stages: bébé days 0–7, ado 7–14, **adulte 14–180**, ancien 180+. One pulse a day, 24 h to choose.

- **Bébé and ado arcs must fit in under a week.** Keep them to 3 steps, 24–48 h apart.
- **Real long arcs (5–7 steps, 1–2 weeks) belong in adulte and ancien.** Ancien is also thin on
  content, so long elder arcs fix two problems at once.
- Follow-ups are not `Pulse`, so they do not fill the 3-waiting cap. A long arc runs *alongside*
  daily pulses. Keep arc steps 24–72 h apart so two arcs never stack more than one step per day.

## 3. Candidates

### Grand arcs (5–7 steps, story score, title on the best ending)

| Working key | Stage | Story | Steps (each a real choice) | CK3 inspiration | Rewards |
|---|---|---|---|---|---|
| `grown_exam` | Adulte+ | **Le Grand Concours du Hibou**: the owl's once-a-year village exam | notice & subject → the night before (cram / party / help a panicking stranger) → a slip with answers under the desk (honest / deceitful) → the exam (challenge by subject) → *caught?* (only if it cheated: own branch key) → results posted | imperial_examination.1000–1070, 4000–4100, 6000, 7000 | `inspired`, ≤ 20 cailloux, title « Lauréat·e » + badge |
| `grown_regatta` | Adulte+, social Hostile | **La Régate des Feuilles**: leaf boats on the pond, against a rival | build the boat (fast / sturdy / pretty: +score) → the squirrel's betting stall (free bets) → overtaking in the reeds → tangled with {B}'s boat (help or push on) → the underdog snail's late surge → the prize | chariot_race.0100–0130, 3000–3050, 0400, 0600 | ≤ 30 cailloux scaled by score, `bille`, title « Amiral·e de la mare » |
| `grown_darling` | Adulte+ | **La coqueluche**: a party-loving weasel newcomer the whole village adores | first impressions: learn from her / envy her / meet her (three branches, own keys) → 2–3 scenes per branch → ending: best friends, an honest truce, or a comic rivalry for life | story_cycle_party_baron (1001–1003, 2001–2007, 3001–3004, 4002) | upgrading modifiers, new `beloved` (+Diplomatie), title « l'Âme de la fête » |
| `grown_flood` | Adulte+ (social Known in step 3) | **La grande crue**: the river rises overnight | woken by shouting (the snail's house or the jam cellar first) → sheltering the flooded (needs drop, affinity) → {B}'s burrow is under water (`AskTarget`: come and stay?) → mud-digging finds → the rebuilding → the thank-you supper | tgp natural_disaster_flavor .0005 / .0010 / .0015, bp2_yearly.1050 | `clear_conscience`, items found in the mud (`coquillage`, `piece_ancienne`), badge « Héros de la crue » |
| `elder_academy` | **Ancien only** | **Jamais trop tard**: a season at the academy in the next valley | arrival → the master who speaks only in riddles → the rowdy students' night out → a public debate with a pompous rival → the falcon lesson → going home changed | bp2_adult_education.0010, 1000–1090, 0009; debate_event.2050 / 2060 | GrowStat Learning (×2 over the arc), `inspired`, `carte_tresor`, title « Docte » |

### Social arcs (3–4 steps, use favours)

| Working key | Stage | Story | CK3 inspiration | Rewards |
|---|---|---|---|---|
| `grown_secret_found` | Adulte+, Known | {A} stumbles on {B}'s harmless secret (secret dance lessons, afraid of ducks): keep it / tease / help → {B} learns that {A} knows → a favour, repaid later | secrets.2002–2004, 3001 | **favour**, affinity |
| `grown_rivals_storm` | Adulte+, Hostile | Enemies to friends: stuck sharing one shelter in a storm, then the long walk home, then a choice to make it last | party_baron.3003 / 4002 (both endings) | `SetAffinityAtLeast` or a comic rivalry modifier |
| `grown_nurse` | Adulte+, Known | {B} turns up with soup when {A} falls sick (**set aside in waves 2–3**, needs `FellSick` to carry a Known target) | bp1_yearly.9024, pet_animal.0204 | `LiftNeed`, `cherished`, favour |

### Echoes (cross-stage follow-ups, needs effect 5)

| Working key | Echo of | Story | CK3 inspiration |
|---|---|---|---|
| `grown_dream_<option>` ×4 | `baby_grow_up` | Weeks later, the adulte meets the dream it shouted at the heron as a baby: lived, abandoned or laughed at | court_events.3090–3092 |
| `grown_apprentice_echo` | `teen_apprentice` | The old master (owl / station / market) needs a hand, and remembers | — | — |

### Singles and short pairs (festival and court flavour)

| Working key | Stage | Situation | CK3 inspiration |
|---|---|---|---|
| `teen_fox_wedding` | Ado | Sun-shower: a fox wedding procession crosses the path, and nobody may watch | festival.106 |
| `grown_moon_party` | Adulte+, Known | {B}'s moon-gazing party on the hill | festival.100 |
| `teen_tug_of_war` | Ado | Tug-of-war against the next village (from the old shortlist) | festival.022 |
| `grown_spilled` | Adulte+, Known | {B} trips and spills soup down {A}'s best jumper, in public | festival.016 |
| `grown_empty_village` | Adulte+ | Midnight craving, and the whole village is gone… (it's a party: a hook into `grown_darling`) | court_events.3040 |
| `grown_stranded_guest` | Adulte+ | A traveller arrives with nothing but a story | court_events.3070 |
| `baby_lonely_shore` | Bébé | Someone on the bank stares at the water every evening | festival.114 |

## 4. Proposed order

1. **Wave 8, engine:** story score, earned titles, favours, echoes, `FellSick` target; tiered
   outcomes if cheap. Harness: payout caps per arc, "alone reaches the middle ending", every score
   gate reachable.
2. **Wave 9:** `grown_exam`, `grown_regatta`, `grown_darling`, the three flagship arcs (~20 events).
3. **Wave 10:** `grown_flood`, `elder_academy`, `grown_secret_found`, `grown_rivals_storm`,
   `grown_nurse`.
4. **Wave 11:** echoes plus the singles.

## Open decisions for the owner

- The weasel newcomer is a new village NPC: name and gender? (Joins the heron, owl, tortoise,
  hedgehog, bear, snail, badger, squirrel, magpie, stork, frog, toad, otter…)
- Titles: a dedicated `GiveTitle` or `GiveItem` on a cosmetic key?
- Should a title's text say where it came from (shown in « Personnalité »)?
- Is the exam adulte-only, or should ado get a 3-step « brevet » version?
