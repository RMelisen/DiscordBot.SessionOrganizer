# La Combe aux Mousses — the first expedition place

Design agreed with the owner on 2026-11-23 (brainstorm). Not implemented yet. It's the content session
that `2026-11-23-plynling-rpg-design.md` (project 2) and the 10-28 expeditions design call for.
Decisions are the owner's unless marked *(my call)*: proposed in the brainstorm, not contested, but never
chosen explicitly.

**What this document is, and isn't.** It fixes the **place**: its creatures, its scene pool, what each
option tests, which choices fill the creatures' notebook, its fights, its rescue, its thread and its rewards.
The **engine** gets its own design session next: the expedition runner, the fight system and moves, the
notebook's storage, the scene draw, the odds numbers. Then comes the **writing sheet**, the full French
text, which the owner reviews before any code. (Order chosen by the owner: place design, then engine,
then writing sheet.)

## Intent

When someone comes back from their Plynling's trip, they should be telling the channel about **the strange
creatures and the rare mushrooms it found** (owner). The place is about **discovery**. Wounds and pushing
your luck are the texture, not the point.

## Goals, non-goals, anti-goals

- **Goal:** creatures worth meeting more than once, and a mushroom worth bragging about.
- **Goal:** the first step into the Bois Sourd, the forest the lore reserves for expeditions
  (`docs/village-lore.md` §5), written in the village's gentle tone.
- **Non-goal (owner, "for now"):** a new collection set. The mushrooms come from the existing Champignons set.
- **Non-goal (owner, "for now"):** exclusive cosmetics.
- **Anti-goal (lore):** no fantasy beasts, no magic used by anyone, nothing that solves a village mystery
  (the Souche, the Trame, the lights, where Asha comes from). Nobody dies, nobody is seriously hurt.
- **Anti-goal (10-28):** a farmable money source. The rewards stay under a `/work` shift.

## Approaches considered

- **Sequencing:** place design → engine design → writing sheet (**owner's pick**), over place + engine in one
  document, or the writing sheet straight away.
- **Creatures:** the wood's own creatures (**owner's pick**), over real animals made wilder, or fantasy beasts.
- **Companion:** alone, with Asha as the rescuer (**owner's pick**), over Asha guiding the first trips, or
  Asha always along.
- **Notebook depth:** three steps per creature (**owner's pick**), over met-or-not, or steps plus keepsakes.

## The place

**La Combe aux Mousses** *(my call: the name)*: the first hollow past the Lisière. Damp half-light,
waist-high ferns, a stream that dives underground, fallen trunks furred with moss, a smell of cellar and
rain. It's still within sight of the Lisière's last trees: the edge of the Bois Sourd, so later places
can go deeper.

- **A short trip** *(my call)*: **30 Énergie**, the arrival, **three scenes**, the return, then **1 h away**
  (the 10-28 reference scale for short trips).
- **Open from ado, level 1** *(my call)*: the starting place. Later places unlock by level (RPG design).
- **Alone** (owner). The Plynling goes in by itself. **Asha** (« l'Étrangère », lore character 11) appears only
  to rescue it. Her voice sheet holds: few words, ceremonious, answers a question with a question, a
  soft green glow she hides under her hood. **Nothing in the Combe says where she comes from** (lore).

## The creatures — the wood's own (owner)

Natural creatures nobody in Humeval has seen, the way nobody had seen Asha's kind of mushroom. They're strange
enough to tell about, and nobody uses magic. A fight is a scuffle or a bluff, and the creature runs off.
*(My call: the roster, names and natures below, open to change in the writing sheet.)*

| # | Creature | What it is | Its way |
|---|---|---|---|
| 1 | **Le Crapaud-Mousse** | A toad so still that moss grows on its back; it looks like a stone that breathes. | Patience and stillness. |
| 2 | **Les Phalènes-Lanternes** | Moths with softly glowing green wings, out at dusk. | Never fought; met through the thread. |
| 3 | **Le Siffleur** | Something in the ferns that answers whistles. Heard long before it's seen: a small, round, brown bird. | Listening, then following. |
| 4 | **Le Hérisson-Châtaigne** | A hedgehog whose spines look like chestnut husks; rolls downhill at intruders. | A bluff fight, then play. |
| 5 | **Le Lucane-Écorce** | A bark-coloured stag beetle the size of a loaf, guarding its log. | A shoving match (Force), then respect. |
| 6 | **Le Furet des Fougères** | A quick thief that grabs what the Plynling found. | A chase (Vitesse), or a gift. |
| 7 | **L'Escargot-Cloche** | A snail whose shell rings when tapped; its silver trails lead to rare mushrooms. | Slowness, for once, pays. |
| 8 | **Le Vieux Blaireau gris** | The Combe's old keeper. Rare. | Never beaten: earned by respect. |

The Phalènes' glow echoes Asha's and the Trame's lights **without ever being linked to them**. Nobody
draws the line, in the text or the notes.

## The creatures' notebook — three steps (owner)

- **One notebook per person** *(my call)*, like the collection book: it survives the Plynling's death.
  Losing it to neglect would be harsh on top of the levels. It records which Plynling earned each step.
- **Each creature has three steps:** **Aperçu** (spotted), **Approché** (got close), **Apprivoisé**
  (tamed). Each is earned by a **different kind of choice**, so meeting a creature again is worth it.
- **In order** *(my call)*: a scene offers the option for the next step the person lacks. The safe option
  always still exists. A step already earned isn't offered again; the scene's other options still play.
- **Naming:** the story engine already uses "marks" (epithets, keepsakes, unlocks). The notebook calls its
  own « étapes », to keep the two apart.
- What a full notebook gives, if anything, is left open (see *Left open*).

**How each step is earned** *(my call; the writing sheet sets the exact options)*:

| Creature | Aperçu | Approché | Apprivoisé |
|---|---|---|---|
| Crapaud-Mousse | notice the « stone » breathing (Intelligence) | sit still beside it until it blinks (Endurance) | lift the fern off its back; it lets you |
| Phalènes-Lanternes | see the glow between trunks (safe) | stand still while they land on you (Endurance) | **only at the thread's end** |
| Siffleur | whistle back (safe), seen only once approached | follow the whistle through the ferns (Agilité) | whistle its own tune back right (Intelligence) |
| Hérisson-Châtaigne | dodge its downhill roll (Agilité) | **win the scuffle** (fight) | roll a chestnut back to it: it plays |
| Lucane-Écorce | see the log move (safe) | **win the shoving match** (fight, Force) | leave it its log, help it set a fallen piece back (Intelligence) |
| Furet des Fougères | notice the ferns rustle behind you (Intelligence) | catch it in the chase (Vitesse) | **give it what it stole**: lose that find, gain its trust |
| Escargot-Cloche | hear the ring (safe) | follow its trail all the way (Endurance) | tap its shell back in rhythm (Intelligence) |
| Vieux Blaireau gris | meet it at its sett | stand your ground without a fight (Courage, a mind stat) | bow the way Asha does |

- **The bow is Asha's hand-on-heart bow, known to the whole village** (lore) *(my call)*. Being rescued
  is never required, so getting hurt is never worth it.
- **The Vieux Blaireau is rare** *(my call)*: it's offered only once the person has at least one step
  with five other creatures. A « fight » option exists and never wins: it sits on you, gently. It's
  never fought for real.
- **Expedition tests may use any stat** *(my call)*: mostly body stats (the soft curve), sometimes a
  mind stat where the act calls for it (Courage before the badger).

## The scenes

**A trip** is the arrival, **three scenes** drawn from the pool, then the return. *(My call, all of the
following; the engine design turns it into rules.)*

| Kind | How many | Examples |
|---|---|---|
| Arrival | 3 variants | through the last trees of the Lisière; in morning fog; after rain |
| Creature encounter | 9 | one per creature, two for the Siffleur (heard on one trip, followed on a later one) |
| Mushroom find | 3 | under a fallen trunk; in a fairy ring; on a root over the stream (the Escargot-Cloche's trail is a fourth way to a find) |
| Obstacle | 4 | the stream to cross; a slope of wet leaves; a bramble tangle; fog |
| Thread step | 3 | *La source qui plonge* |
| Return | 1 | with the trip's summary |
| Rescue | 1 | Asha, on the second wound |

- **Draw rules:** at most two creature encounters and one thread step per trip, and **at most one mushroom
  per trip** (owner): a find scene, or the end of the Escargot's trail, never both. The scenes of the
  person's last trip are avoided. A creature whose three steps are done comes up less often.
- **The Siffleur spans trips** *(my call)*: its « heard » scene gives Aperçu; its « follow » scene comes up
  only once the person has that step.
- **The Furet is one scene, drawn only after a find on the same trip** *(my call)*, so there is always
  something to steal. In it you chase it (Approché), give it the find (Apprivoisé), or let it go (safe). Its
  Aperçu is noticing the ferns rustle as it comes.
- **Some options only certain traits unlock** (10-28: « traits change what is possible »), shown with the
  trait's icon: a few per place, never the only way to a notebook step *(my call: which traits and where is
  the writing sheet's)*.
- **Every scene has a safe option** with no roll (10-28), and 2–4 options in all. A risky option shows its stat
  and odds.
- **Difficulties** fit a level-1 to level-15 Plynling: a body stat starts around 3–5 (die + lean), so
  difficulties of about 3–8 give odds of 35–65 % at level 1 (`PlynlingRpg.BodyChance`). The engine design fixes the numbers.

## Failure, wounds, the rescue

- **A failure is written per option** (10-28): a scrape, a lost find, a fright (stress), wet and muddy
  (hygiene), lost time. It's always a small mishap with a smile, never cruelty. Some failures **wound**.
- **A wound** gives « Rentrer » (go home now, keep what was found) or « Continuer ». **A second wound
  brings the rescue** (owner: on every expedition, not only this one; it was the third in 10-28). The words stay gentle:
  an égratignure, a bump, a twisted ankle. Never « grave ».
- **The rescue, on the second wound:** Asha finds it and carries it home under her hood's light. She
  answers its questions with questions. **The trip's mushroom is left behind** (she makes it leave the
  basket) *(my call)*; the XP, the cailloux and any notebook steps it earned are kept. It comes home **« Tout cabossé »** *(my call: replaces 10-28's « Gravement blessé »; same
  role, a longer malus and some stress, in the lore's gentle register)*. Lighter wounds bring a smaller,
  shorter modifier. **It never dies.**
- **Abandoned** (no click for 15 min): it heads home with what it found, taking no new risk (10-28).

## The thread — *La source qui plonge*

The stream dives underground at the Combe. Across trips, the Plynling follows it *(my call)*:

1. **It vanishes** under a mossy rock. The Plynling finds where it goes on.
2. **It's heard underground** and found again where it springs up in a ravine (Agilité or Intelligence).
   A failure just means trying again on a later trip, with nothing lost.
3. **The pool at dusk**, where the Phalènes-Lanternes gather. It's **a sight, not a secret** (lore: nothing to uncover).
   It gives the Phalènes' **Apprivoisé** step, a **journal moment**, and a **story epithet** for the Plynling
   (such as « qui a vu le bassin des Phalènes »).

- At most one step per trip, so it takes at least three trips. The thread is the Plynling's (story
  flags on the Plynling), while the Phalènes step goes to the person's notebook.

## Rewards

- **XP** for every trip *(my call; the engine design tunes it by simulation)*: about **30**, more for tests
  passed, **up to 40**: at most about 1.33 XP per Énergie, the engine design's ceiling, which keeps the RPG
  design's year-long pace. Per Énergie that's better than a drill (10 XP for 15).
- **A few cailloux:** 5–15 a trip, below a `/work` shift (25–40) (10-28).
- **At most one mushroom a trip** (owner), **from the existing Champignons set** (owner: no new set), **with
  the rare ones much likelier than foraging** *(my call: the numbers)*. A find scene draws Champignons only, rarity about
  **25 % common / 40 % uncommon / 28 % rare / 7 % legendary**, against forage's 60/28/10/2. Seasons hold as
  they do now (Morille conique in spring, Truffe noire in winter). The brag is « une Oronge dans la Combe ».
  The Escargot-Cloche's trail tilts a find further toward rare.
  - Rare mushrooms sell (15 cailloux a rare one), and each completed Champignons set pays again when turned
    in, so the engine design must simulate a day of trips, sales and turn-ins against `/work`. **If trips
    out-earn it, finds become rarer per trip, never less rare** (owner: keep the brag).
- **Notebook steps** and **the thread's epithet**.
- **No cosmetics** for now (owner).

## Constraints

- **Lore** (`docs/village-lore.md`):
  - the gentle tone, animals as animals in the village, one discreet magic;
  - no mystery solved, Asha's origin untold, the village unchanged;
  - nothing in the Combe names the Souche or the Trame.
- **Writing:** `docs/plynling-writing-style.md`. Every line in both genders; Plynlings are mushrooms, written
  family-neutral.
- **Append-only:** creature, step, scene, thread and modifier keys are stored. Name them once, never rename.
- **Art:** each creature needs a picture for its notebook page. The size and style come with the engine design
  (Plynlings are 32×32, items 16×16). That's eight pictures.

## Left open

- **What a full notebook gives** (all 24 steps): perhaps nothing but pride; the owner decides later.
- **Seasonal or night scenes**, beyond the dusk of the thread's end.
- **The creatures' fight moves and numbers:** the engine design.

## Left to the engine design

The runner, the fight system and moves (the Hérisson's and the Lucane's fights), the notebook's storage
(per person, recording the Plynling) and its screen, the scene draw, the difficulties, the XP and the mushroom
draw's exact numbers, the modifiers' values (« Égratigné », « Tout cabossé »), the away time and the card while away.

## Left to the writing sheet

Every scene's French text, its options and labels (≤ 80 characters), each failure's consequence, Asha's
lines, and the arrival and return variants, for the owner's review before any code.
