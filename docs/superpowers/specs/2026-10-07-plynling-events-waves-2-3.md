# Plynling events — content waves 2 and 3 (selection)

Both waves follow `docs/superpowers/plans/2026-10-06-plynling-content-phase5.md` unchanged from Task 2
on (writing sheet → owner review → catalog → harness → dev-guild read-through, a patch version per wave).
This file records **what was selected and why**; the full shortlist with the rejected candidates lives in
the owner's scratch notes, not here.

**Source:** the owner's CK3 localization (`event_localization\…`, situations only). The CK3 ids below are
pointers for inspiration; nothing is translated or copied — every line is written fresh in the Plynling
voice (`docs/plynling-writing-style.md`).

**Avoided on purpose** (already in the catalog): puddle, snail, fledgling, honey pot, Monsieur Brindille,
forbidden shortcut, pine-cone contest, fireflies, timetables, shell game, mystery parcel, borrowed scarf,
jam critique, snail guest, bramble, plant-sitting, and the four social pairs.

## Wave 2 — 12 events (3 bébé, 3 ado, 6 adulte/ancien; 4 social)

| Working key | Stage | Situation | Mechanics | CK3 inspiration |
|---|---|---|---|---|
| `baby_pebble` | Bébé | {A} adopts a pebble as best friend. It's a caillou, technically money, and {A} refuses to spend it. It later "goes missing" in the tortoise's till. | Mod `light_heart`, follow-up chain, Ch Intrigue | pet_rock.0001 / .0104 / .0105 |
| `baby_night_noise` | Bébé | Something rattles outside the window at night: it's the hedgehog doing his rounds. | Ch Courage, St `craven`, Gate `curious` | tgp_child_personality.0009 |
| `baby_sideline` | Bébé | A ball game on the green; {B} watches alone from the side. **First social event below adulte.** | Social Anyone, AffinityShift, Ch Diplomacy | playdate.5001 / 5004 |
| `teen_ballgame` | Ado | The village's yearly chaotic ball game (dried gourd, the bear's pie cart, the sparrow refereeing). | Follow-up chain (2–3 steps), Ch Courage / Intrigue | yearly.7010–7045 |
| `teen_mudcastle` | Ado | A mud castle on the riverbank, and the river rising. Bribe it with cailloux, reinforce the walls, or build a shrine. | Ch Stewardship, follow-up | bp2_yearly.1050 / 1051 |
| `teen_unlucky` | Ado | {B} is sure {A} brings bad luck since stepping on the heron's shadow. | Social Known, Ch Learning / Intrigue, St `honest` | bp1_yearly.1060 / 1070 |
| `grown_smell` | Adulte+ | The snail, very politely, mentions a smell. A bath, a scent quest, or denial. **Only when hygiene is low.** | Condition on `Hygiene`, follow-up chain, St `arrogant` | bp1_yearly.5800–5805 |
| `grown_matchmaker` | Adulte+ | {A} plays matchmaker between the hedgehog station master and the tortoise. | Follow-up, Ch Diplomacy, Mod | bp1_yearly.5100–5102 |
| `grown_bad_dish` | Adulte+ | {B} cooked something special for {A} all week. It is dreadful. (`grown_jam` from the other side.) | Social Known, St `honest` / `deceitful`, AffinityShift | stress_trait_ongoing.5001 |
| `grown_campfire` | Adulte+ | Toasting honey on sticks next to {B}, nearly a stranger. « Tu veux qu'on soit amis ? » | Social Anyone + response, affinity lift | bp1_yearly.5721 |
| `grown_stork_tales` | Adulte+ | A migrating stork holds the café spellbound with impossible stories. One detail later turns out true. | Ch Learning, follow-up, St `cynical` | fp1_yearly.0031 / 1081, yearly.4061 |
| `elder_lap` | **Ancien only** | The old morning lap around the pond, and {A} has to stop halfway. | Stage Elder, St `stubborn`, Mod `soothed` | elder_events.1600 |

## Wave 3 — 16 events (4 bébé, 4 ado, 6 adulte/ancien + 2 ancien-only; 4 social)

| Working key | Stage | Situation | Mechanics | CK3 inspiration |
|---|---|---|---|---|
| `baby_berry_stall` | Bébé | An unattended strawberry stall with a jar for the cailloux. {A} has none. | St `honest` / `deceitful`, Ch Intrigue | stewardship_wealth.6001 |
| `baby_burp` | Bébé | An enormous burp in the silent library. The owl opens one eye. | Ch Diplomacy, Gate `rowdy`, Mod `light_heart` | court.1030 |
| `baby_lost` | Bébé | Lost while picking berries; an old toad herbalist gives directions, at the price of her whole nettle-soup recipe. | Ch Courage / Learning, follow-up | hunt.1080, playdate.3013, bp1_yearly.9034 / 9035 |
| `baby_grow_up` | Bébé | {A} ambushes the heron with a twig and announces what {a:m|f} will be when grown. **Each option grows a different stat.** | GrowStat per option, AI-led | fp1_yearly.0581 |
| `teen_secret` | Ado | A secret too heavy to carry, told to {B}; {B}'s owner answers. | Social Known + response, affinity lift | friendship.2001–2003 |
| `teen_treasure_map` | Ado | A map falls out of a library book. Under the X: the heron's diary from when he was young. | Follow-up chain, Ch Learning / Courage | tour_grounds_events.3030–3032 |
| `teen_ghost_stories` | Ado | Ghost stories by the fire with {B}, first to flinch loses; or {A} comes back under a sheet. | Social Known, Ch Courage **VsTarget**, Ch Intrigue | tour_grounds_events.1008, host_dinner_events.1004 / 1005 |
| `teen_puppets` | Ado | The fair's puppeteer has a puppet just like {A}, and it's the clumsy one. | Ch Diplomacy, St `arrogant`, Mod | festival.017 |
| `grown_snail_race` | Adulte+ | The yearly snail race, with the village snail entered against all advice. | Ch Intrigue, St `honest`, AI Greed | contest_events.3000–3030 / 4600 |
| `grown_magpie` | Adulte+ | The market magpie snatches something shiny. Later, {A} finds its stash. | Ch Courage / Stewardship, follow-up | travel_events.4006 |
| `grown_jam_cellar` | Adulte+ | A midnight snack at the café; behind the pantry, the tortoise's jam cellar, every jar labelled by year. | Ch Intrigue, St `honest`, follow-up | host_dinner_events.3130–3135 |
| `grown_hobby` | Adulte+ | {B} insists {A} try {b:m|f}'s favourite pastime "just once". | Social Known, challenge, AffinityShift, St `stubborn` | bp1_yearly.1000 |
| `grown_bare_pantry` | Adulte+ | {B}'s pantry is nearly bare and {B} pretends otherwise. | Social Known (friends), AffinityShift, St `honest` | bp1_yearly.3002 |
| `grown_wish_oak` | Adulte+ | A wish hung on the old oak comes half true, very literally. | Follow-up, Mod `clear_conscience` | festival.004 / 009 |
| `elder_story` | **Ancien only** | The village babies sit in a ring and ask {A} for a story. | Stage Elder, Ch Diplomacy, Mod `soothed` | mpo feast 0280 / 0320, childhood.7500 |
| `elder_glasses` | **Ancien only** | {A} can't read the jam labels anymore; the owl offers a reading glass. | Stage Elder, St `arrogant`, GrowStat Learning | learning_scholarship.1401, health.7300 |

## What the plan needs adjusted for these waves

1. **Elder-only events are new.** The phase-5 reachability check simulates Baby, Teen and Adult only, so
   an Elder-only pulse would report zero draws and fail. Add `PlynlingStage.Elder` to the simulated stages.
   Coverage of the Elder stage is thin (3 events after wave 3), so the "last 14 resolved" window must keep
   letting the least recently seen come back, as it already does.
2. **Key prefix `elder_`** for Elder-only events (the plan names only `baby_`, `teen_`, `grown_`). Keys
   above are working names, fixed on the writing sheet and never renamed after shipping.
3. **Social pulses below adulte** (`baby_sideline`, `teen_unlucky`, `teen_secret`, `teen_ghost_stories`):
   the engine already picks targets at any stage; the sheet's girl/girl and boy/girl read-through must
   cover a baby or teen target.
4. **Chains:** wave 2 has three multi-step chains (`baby_pebble`, `teen_ballgame`, `grown_smell`); each
   follow-up is its own `FollowUp` event and counts against the wave's writing load, not its event count.
5. No engine change is needed for either wave. `grown_smell` reads `Plynling.Hygiene` in its `Condition`.

## Set aside for later

- **A friend nurses {A} when sick** (bp1_yearly.9024, pet_animal.0204): needs `OnAction.FellSick` to carry a
  Known target — a wave with engine work.
- Extra mental-break variants (stress_trait_coping_decisions: Flowing Ink, Sweat It Out, Alone).
- 21 remaining candidates in the scratch shortlist (surprise party, riddle night, rival at the fishing spot,
  abandoned tea-house, tug-of-war, poetry duel, empty goûter, anonymous note…).

## Wave 4 — 13 events (3 bébé, 3 ado, 6 adulte/ancien, 1 ancien-only; 5 social), 5.17.1

Picked from the remaining shortlist, written and shipped in one pass (no separate writing sheet; the
almanac page is the review copy).

| Key | Stage | Situation | Mechanics | CK3 inspiration |
|---|---|---|---|---|
| `baby_ice_cream` | Bébé | The bear leaves {A} in charge of the ice-cream stand « five minutes ». | Ch Stewardship, St `rowdy` | child_personality.7030 |
| `baby_why` | Bébé | {A} follows the owl all day asking « pourquoi ? ». | Ch Learning, weight ×2 `curious`, Gate `pensive` | child_personality.3001 |
| `baby_kite` (+ `baby_kite_week`) | Bébé | A red kite for a week of work at the badger's toy shop. | Follow-up (96–120 h), Ch Learning | child_personality.0011 |
| `teen_dare` | Ado | {B} waves from the top of the great oak. | Social Known, Ch Courage, St `craven` | playdate.3007 |
| `teen_poetry` | Ado | A poetry duel at the café, signed up by {B}. | Social Hostile, Ch Learning vs {B}, St `shy` / `compassionate` | contest_events.5000–5030 |
| `teen_tarts` | Ado | The tortoise's twelve tarts vanished; a trail of crumbs. | Ch Learning, St `just` / `honest` | host_dinner_events.3040 / 3041 |
| `grown_teahouse` (+ `grown_teahouse_opening`) | Adulte+ | An abandoned tea-house under the ivy, restored and opened. | Follow-up, Ch Stewardship / Diplomacy, St `lazy` | yearly.0025–0029 |
| `grown_surprise` (+ `grown_surprise_cake`) | Adulte+ | Whispers behind {A}'s own door: a surprise party, then three cakes. | Follow-up, Ch Intrigue / Courage, St `craven` / `temperate` / `gluttonous` | bp1_yearly.3004 / 3005 |
| `grown_riddles` | Adulte+ | The owl's riddle night; the owl has never lost. | Ch Learning / Intrigue, St `shy` | fp1_yearly.0101 |
| `grown_fishing_rival` | Adulte+ | {B}, a rival, casts at exactly the same spot. | Social Hostile, Ch Stewardship vs {B}, St `vengeful` | bp1_yearly.5709 |
| `grown_overheard_gift` | Adulte+ | {A} overhears {B} calling {A}'s knitted jumper a horror. | Social Known, Ch Diplomacy, St `wrathful` / `shy` | mpo feast 0310 |
| `grown_secret_note` (+ `grown_note_reveal`) | Adulte+ | An unsigned note in {A}'s pocket; the author is {B}. | Social Anyone (`Affinity >= 0`), follow-up, Ch Intrigue | feast_default.2002 |
| `elder_old_toy` | **Ancien only** | A wooden spinning top found in the attic. | Stage Elder, Ch Learning | bp2_yearly.8071 |

## Wave 5 — 8 story cycles, 24 events, new reward effects, 5.18.0

Longer stories (scenes of 3–5 sentences) built as CK3 story cycles: three or four steps chained by
follow-ups. New effects: `GiveCailloux` (CK3 `add_gold`), `GiveItem` (artifacts → collectibles), `LiftNeed`.

| Chain | Stage | Story | CK3 inspiration | Rewards |
|---|---|---|---|---|
| `baby_den` → `_storm` → `_party` | Bébé, social Anyone | Building a den with {B}, the storm night, the opening | bp2_yearly.1055, playdate events | affinity, a `bille`, needs |
| `baby_dormouse` → `_gifts` → `_sleep` | Bébé | A dormouse moves into the sock drawer, leaves gifts, then hibernates | pet_animal story cycle | `trefle` / `gland` / `plume`, happiness |
| `teen_apprentice` → `_owl` / `_station` / `_market` | Ado | Choosing a master for the season, then the final test | childhood education events | `cle_rouillee`, `piece_ancienne`, `bague`, `bouton`, modifiers |
| `teen_games` → `_day` | Ado | Sack race, pine-cone throw or face-pulling contest | ep2 tournament contests | up to 20 cailloux |
| `grown_golden_carp` → `_dawn` → `_tale` | Adulte+ | A legendary golden carp: rumour, dawn sighting, the tale | hunt_mystical_animal story cycle | `piece_ancienne`, `well_spoken` |
| `grown_spoons` → `_river` → `_end` | Adulte+ | Spoons vanish; an otter's wind chime under the willows | story_cycle_murders_at_court (harmless) | `caillou_plat`, `clear_conscience` |
| `grown_feast` → `_evening` → `_toast` | Adulte+ | Hosting a banquet, the spilled soup, the toast | feast activity events | 15 cailloux, `plume`, `hearty` |
| `elder_sea` → `_road` → `_shore` | Ancien only | Seeing the sea once: a lost duckling on the road | pilgrimage and travel events | `galet` / `plume`, happiness |

## Wave 6 — 9 mental breaks (3 per stress level), 5.18.1

Rule for every break option: a lot of relief with a malus (negative modifier or named coping trait), or no
malus and 30–50 less relief; never a bonus. `break_toomuch` / `help` fixed to fit (−90 + Apaisé → −65).
New penalties: `on_edge` (−2 Diplomatie), `shaken` (−2 Courage), `tense` (−2 Intendance), `sleepless`
(stress decays 30 % slower). `GainCoping` can now name its trait.

| Key | Level | CK3 inspiration | Malus options | Relief (malus / none) |
|---|---|---|---|---|
| `break_spiral` | 1 | Spinning Thoughts | journaller, athletic | −75, −80 / −45 |
| `break_snap` | 1 | Boiling Anger, Lashing Out | À cran, irritable | −80, −75 / −45 |
| `break_biscuits` | 1 | Eat Up!, Not A Bite To Eat | comfort_eater, inappetetic | −80, −75 / −45 |
| `break_impostor` | 2 | Impostor!, I am Unworthy | contrite, Ébranlé | −90 / −60 |
| `break_list` | 2 | Endless Toil, Too Busy | Tendu, profligate | −90 / −60 |
| `break_nightmares` | 2 | Haunted by the Past, nightmares | Les nuits blanches, confider | −90 / −60 |
| `break_empty` | 3 | Nothing Matters, Emptiness Inside | reclusive, improvident | −110 / −70 |
| `break_storm` | 3 | Resentment, Unrestrained Wrath | À cran, Tendu | −120, −110 / −70 |
| `break_flee` | 3 | Shirked Duty, Overload | Ébranlé, profligate | −120, −110 / −70 |

## Wave 7 — 9 more story cycles, 28 events, 5.18.2

| Chain | Stage | Story | CK3 inspiration | Rewards |
|---|---|---|---|---|
| `baby_frogs` → `_lessons` → `_talk` | Bébé | Learning the frogs' language from an old frog | learn_language scheme | `quartz`, `inspired`, `cherished` |
| `baby_music_box` → `_repair` → `_song` | Bébé | A broken music box from the flea market, the hedgehog's workshop, the tortoise's song | artifact events | `bouton`, `cherished` / `clear_conscience` |
| `teen_stalls` → `_market` → `_end` | Ado | A summer syrup-stall rivalry with the squirrel | story_cycle_tax_rivalry | up to 15 cailloux per step, `trade_sense` |
| `teen_befriend` → `_rain` → `_ask` (+ `reply_befriend`) | Ado, social Anyone (acquaintances) | A three-step plan to make a friend, ending in a real ask | befriend scheme | friendship (affinity lift) |
| `grown_letters` → `_more` → `_bridge` (→ `reply_declare`) | Adulte+, social Known (`CanCouple`) | Anonymous letters under the old bridge, then the declaration | court (romance) scheme | affinity, couple |
| `grown_judge` → `_tree` → `_snail` | Adulte+ | Standing in for the mayor: three village disputes | hold court petitions | up to 15 cailloux, `well_spoken` |
| `grown_chronicle` → `_tarts` → `_reading` | Adulte+ | Writing the village chronicle; what to leave out | commissioned epics, legacies | `carte_tresor`, `clear_conscience` |
| `grown_bridge` → `_box` → `_open` | Adulte+ | Rebuilding the cracked old bridge; a box in the foundations | great projects | `boussole`, up to 20 cailloux |
| `elder_student` → `_grows` → `_leaves` | Ancien only | A village little one becomes {A}'s last student | mentor_student events | `trefle_quatre`, `cherished` |

## Wave 8: 181 events (126 pulses, 48 follow-ups and endings, 7 echoes), 6.0.3

Selected from the CK3 survey in `2026-10-08-plynling-events-waves-8-plus.md`; written and shipped in one
pass, with the almanac page as the review copy. No engine change. New modifiers: `laureate`, `party_soul`,
`flood_hero` (positive) and `guilty` (negative, owner choices only). Existing events gained echo
follow-ups: `baby_grow_up` (all four answers) and the three `teen_apprentice_*` endings.

**Long arcs (4–7 events each)**

| Chain | Stage | Story | CK3 inspiration | Rewards |
|---|---|---|---|---|
| `grown_exam` → `_eve` → `_paper` → `_laureate` / `_honest` / `_hollow` / `_caught` | Adulte+ | The owl's Grand Concours; a slip of answers in the hall; four endings by roll | imperial_examination 1000–7000, 4100, 6000 | `laureate`, 25 cailloux, `guilty` |
| `grown_regatta` → `_start` → `_lead` / `_back` | Adulte+ | Leaf-boat regatta; the old snail's first finish in forty years | chariot_race 0100–0600, 3000–3050 | up to 30 cailloux, `coquille_escargot` |
| `grown_darling` → `_lesson` / `_rival` / `_visit` → `_party` | Adulte+ | A weasel at the mill everyone adores: admire, envy or befriend | story_cycle_party_baron | `party_soul`, `well_spoken`, `sly` |
| `grown_flood` → `_shelter` → `_mud` → `_supper` | Adulte+ | The river overflows; shelter, mud, the thank-you supper | tgp natural_disaster_flavor | `flood_hero`, 20 cailloux, `piece_ancienne` |
| `elder_academy` → `_master` → `_students` → `_debate` → `_home` | Ancien only | A season at the academy: the mole who answers with questions, the jay's debate | bp2_adult_education, debate_event 2050 / 2060 | `carte_tresor`, `inspired` |
| `grown_scarecrow` → `_crows` → `_judging` | Adulte+ | Scarecrow contest judged by the sparrow (a bird, nervous) | contests | 25 cailloux, `well_spoken` |
| `grown_forage` → `_fog` → `_find` | Adulte+ | The badger's secret morel clearing | hunt activity | `food.morel`, `food.truffle` |
| `grown_hiccups` → `_cure` → `_relapse` | Adulte+ | A hiccup epidemic, the cure, then {A} catches it | epidemics (comic) | `light_heart`, `soothed` |
| `grown_pond_war` → `_raid` → `_peace` | Adulte+ | Frogs and ducks fight over the pond | house feuds | `party_soul` |
| `teen_rival` → `_race` → `_end` | Ado | A ferret who always wins; the race; the bridge | education rivals | `fired_up`, `cherished` |
| `teen_camp` → `_night` → `_back` | Ado | Three days on the lake island | tour / travel | `coquille_escargot` |
| `elder_memoirs` → `_visitors` → `_reading` | Ancien only | Writing memoirs; the village wants in | tgp_commission_book | `cherished` |

**Social arcs:** `grown_secret_dance` (Known, 3: {B}'s secret dance lessons, then the ball);
`grown_storm_shelter` (Hostile, 3: a rival in the same hollow oak); `grown_prank` (Hostile, 3: a prank
war, 17–17); `grown_journey` (Known, 3: the road, the inn with one bed, the fair).

**Echoes (adulte):** `grown_dream_explore` / `_cafe` / `_owl` / `_chief` (14–16 days after
`baby_grow_up`, CK3 court_events 3090–3092 *I Want a Pony!*); `grown_master_owl` / `_station` / `_market`
(7–10 days after the apprenticeship ends).

**Pairs and singles:** bébé `baby_swing`, `baby_bath`, `baby_dress_up`, `baby_imaginary` (+ `_bye`),
`baby_otter`, `baby_cant_sleep`, `baby_mud_pie`, `baby_doudou` (+ `_found`); ado `teen_fox_wedding`
(festival.106), `teen_tug_of_war` (festival.022), `teen_runaway`, `teen_crush` (Known),
`teen_bakery` (+ `_week`), `teen_hidden_garden` (+ `_owl`); adulte `grown_moon_party` (Known,
festival.100), `grown_spilled` (Known, festival.016), `grown_empty_village` (court_events.3040),
`grown_hare` (court_events.3070), `grown_clock_feud`, `grown_lost_letter` (Known), `grown_board_game`
(+ `_end`, board_game_events), `grown_rumor`, `grown_ghost` (+ `_owl`), `grown_beetle_duel`,
`grown_portrait`; ancien `elder_rocking_chair`, `elder_old_rival`, `elder_dance`, `elder_acorn`.

**Short events (81 singles, no sequel):** CK3 has hundreds of one-scene yearly events; these are their
Plynling counterpart: one or two sentences of scene, two or three quick choices, small rewards. Inspired
by Comet Sighted!, Peek-a-boo!, A Lonely Doll, Smuggling Sweets, Children Say Such Funny Things, I Need a
Hero, A Little Language, Captured Beast, Gray Days, Pawful of Pooches, The Cat-apult, Experimenting,
The Flower Thief, My Arm Against Yours, A Superstitious Mind, A Wonderful Phrase, A New Fashion at
Court, Petty Vandalism, Sprouting Interest, Tall Tales at the Table, Saffron in Flames, Bullying, The
Apple Falls, An Honest Mistake, Heavy Days, Annoying Company, Generous Praise, Snide Remarks, A Friend
in Need, Misery Loves Company, Out with the Old, Grandiose Decor, Open Schedule, Suspicious Snickering,
Lost and Found, Foreign Merchants, Explorer from the West, Delicious Excess, It Came to Me in a Dream,
Not Forgotten, The Good Witch of the Bog, Life in Color, A Helpful Warning, Remembering Birthdays Past,
A Face From Long Ago, Old Regrets, Evening Reflections. 20 bébé, 20 ado (3 social), 31 adulte+
(4 Known, 2 Hostile), 10 ancien. Keys run from `baby_comet` to `elder_sunrise` in the catalog.
