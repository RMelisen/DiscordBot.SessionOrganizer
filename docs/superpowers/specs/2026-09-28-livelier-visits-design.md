# Livelier visits — design

A `/plynling visit` used to end on one line. Inspired by Tomodachi Life, where you look in on two
Miis hanging out somewhere and watch a little scene, a visit now plays out as a short story in a
place, shaped by the two Plynlings' relationship — without changing any rule.

## Unchanged

`PlynlingService.VisitAsync` still decides everything in one save: the scene (good or bad), the
affinity and bond, a confession, happiness, finds, badges, journal moments. The story is only how
that outcome is shown, so a restart or a failed edit mid-story can never change what happened.

## The card — small pictures, fit for a phone

The two-picture `MediaGallery` (stretched to full width on phones) is replaced by one `Section` per
Plynling, each with its sprite as a small thumbnail beside its line — the Plynling card's layout.
Above them, the place as a heading (« ## 🌳 Au parc »); below, the current beat.

## A story in 3 beats

1. **Arrival** — « 🚪 Pouf arrive au café ; Mimi l'attend déjà. »
2. **The activity, with a little exchange in speech bubbles** —
   « 🍰 Ils partagent une part de tarte. » then
   « 💬 **Pouf** : Tu crois que les nuages ont un goût ? » / « 💬 **Mimi** : Sûrement fraise. »
3. **Parting, with the outcome** — a departure line (happy or not, per the scene), then everything
   the old single card said: confession, bond change, finds, badges, and the happiness line.

The host's « Accueillir » answers with beat 1 at once (inside Discord's 3 s); beats 2 and 3 follow
by editing the same message about every 2.5 s from a background task that swallows and logs its
errors, like every other Discord side effect.

### Rereading: ◀ ▶

Once beat 3 is shown, the card gets two buttons, ◀ and ▶, and a « 1/3 » marker: anyone can page
back through the story. Two verbs, `vis:prev:{story}:{beat}` and `vis:next:{story}:{beat}`, so the
two ids never collide; the button at an end is disabled. Stories live in memory in a singleton
(`VisitStories`), keyed by a short id, the most recent 300 kept — like games and trade offers. After
a restart, an arrow on an old visit answers privately « Cette histoire n'est plus disponible » and
the card stays on its last beat.

## Places

Eight places, one drawn per visit; some only at certain hours (Paris time):

| Place | When |
|---|---|
| 🌳 Au parc, 🍂 Sous le grand chêne, 🏡 Chez l'hôte, ☕ Au café du coin, 🪵 Au bord de l'étang | always |
| 🌼 Dans la prairie | 6 h – 20 h |
| ☀️ Au soleil, sur les rochers | 10 h – 19 h |
| 🌙 Sur le toit, sous les étoiles | 20 h – 6 h |

## Activities and exchanges by relationship

Drawn from the bond **after** the visit, or from a conflict pool when the scene was bad:

| Bond | Activities (examples) | Exchange tone |
|---|---|---|
| Connaissances | parler de la pluie, comparer leurs chapeaux | polite small talk |
| Amis | course jusqu'au chêne, cache-cache, bataille de feuilles | playful |
| Meilleurs amis | construire une cabane, se raconter des secrets | complicit |
| En couple | goûter en tête-à-tête, regarder les étoiles | tender |
| Rivaux | concours de grimaces, qui saute le plus haut | teasing |
| Ennemis / scène ratée | dispute pour le dernier gland, bouder dos à dos | grumpy |

About 60 lines in all (arrivals, activities, exchanges, departures). Lines name both Plynlings and
agree with the pair: « ils », or « elles » when both are girls; adjectives on one of them agree with
that one (« ravi / ravie »). Every line goes through `ResponsePicker` so a pool does not repeat.

## Code

- `Helpers/PlynlingVisitStory` — pure: `Build(outcome, visitorOwner, hostOwner, now, Random)` →
  a `VisitStory` (place, three beats as text). All the pools and the gender agreement live here, so
  every line is checkable without a gateway.
- `VisitStories` — the in-memory store (singleton).
- `PlynlingPlayCards.BuildVisitStory(story, beat, now, withArrows)` — the card (Components V2, small
  thumbnails; the arrows only on the finished story).
- `PlynlingComponentHandler` — « Accueillir » now plays the story; `vis:prev` / `vis:next` page it.

## Checks

- Every place is reachable at its hours and never outside them; the rooftop only at night.
- Each bond draws from its own pool; a bad scene always draws a conflict activity.
- Agreement: « ils » / « elles », and single-Plynling adjectives, for every pair of genders; no
  placeholder left in any rendered line.
- Beat 3 carries every outcome line the old card had (confession, bond change, finds, badges,
  happiness).
- The card stays well under 40 components; the arrows have distinct ids, disabled at the ends.
- The store keeps the last 300 and answers « plus disponible » for an unknown id.
