# Plynling personality and events — design

Crusader Kings III's character layer, adapted to the Plynlings: **traits**, five **stats**, a
**stress** gauge with mental breaks, timed **modifiers**, and **events** — a short scene with
choices, some gated by traits or stats, some a skill challenge with a visible chance, some costing
stress, some chaining into follow-ups or asking another Plynling's owner to answer.

The stats are also the groundwork for a later adventure/RPG layer (Force, Agilité, Endurance), which
is **not** part of this design beyond leaving room for it.

## Goals and the one hard rule

- **A daily reason to come back** — about one small decision a day, played privately.
- **Stories other people see** — every outcome is told publicly in the game channel, as a paged
  story.
- **Groundwork for the RPG** — stats and challenges that will matter later.
- **Hard rule: someone who never plays events loses nothing on the tamagotchi side.** Everything
  below that could hurt hunger, happiness, hygiene or death is reachable *only* through choices the
  owner made. An ignored event is decided by the Plynling itself, in character, and that path can
  never cost stress or apply a negative modifier.

## Decisions (as agreed)

| Topic | Decision |
|---|---|
| Delivery | **Pull.** A pending event shows a button on the card; the owner plays it in an ephemeral card whenever they like. After 24 h the Plynling decides on its own. The outcome is always posted publicly, mentions off. |
| Stats | Five: **Diplomatie, Intendance, Sagesse, Ruse, Courage.** Body stats come with the RPG. |
| Traits | 1 childhood trait at bébé, 2 personality traits at ado, a 4th at adulte drawn **weighted** by the ado years. Full CK3 list, adapted. |
| Stress | Comes only from owner choices; **does** touch the tamagotchi (happiness drain). |
| Modifiers | Touch stats, stress, event odds **and** the tamagotchi; never the economy. |
| Social | One-sided by default (the other Plynling reacts in character); **response events** for four big moments. |
| Pacing | A daily pulse plus triggered events (on-actions), max 3 pulse events pending. |
| Choices | CK3-faithful: trait icons on gated options, stress cost shown, challenge chance shown as a percentage. |
| Mental breaks | End in a coping trait, a modifier, or just a bad event. The coping trait is drawn **uniformly**, not from current traits. |

## Delivery in five phases

Each phase ships on its own, gets its own implementation plan, and is tested against the dev guild
before the next one starts. Version bump in `config.yaml` per phase.

1. **Foundation** — traits, stats, personality title, new life stages, card line and « Personnalité ».
2. **Event engine** — catalog, pulse, pending queue, choice card, challenges, deciding alone, public
   story, first personal events.
3. **Stress and modifiers** — the gauge, its tamagotchi effects, modifiers, the `Settle` timeline,
   mental breaks, coping traits.
4. **Social and chains** — follow-ups, response events, targets, on-actions.
5. **Content waves** — ongoing.

---

# Phase 1 — Foundation

## Life stages change

`PlynlingLife.StageStart`: **Teen 7 days** (was 2), Adult 14 days (unchanged), Elder 180 days
(unchanged). Stages stay cosmetic for needs and death; they now also gate trait draws and events.

Consequence, accepted: a Plynling aged 2–7 days today goes back to « bébé » on its card until it
turns 7 days; its « est devenu ado » journal moment already exists and is not rewritten or
duplicated.

## Traits

### Catalog — `Helpers/PlynlingTraits`

Each trait: a **stable key** (string, **append-only** — a rename orphans every stored copy), a kind
(`Childhood`, `Personality`, `Coping`), M/F display names, an emoji, a one-line description (M/F),
stat modifiers, AI-axis values, an exclusion group (personality only), and optional stress-gain /
stress-loss multipliers (used from phase 3).

**Stat abbreviations:** Dip = Diplomatie, Int = Intendance, Sag = Sagesse, Rus = Ruse,
Cou = Courage. CK3's six skills map onto five: Diplomacy → Dip, Stewardship → Int, Learning → Sag,
Intrigue → Rus, and **Martial and Prowess merge into Cou, taking the larger of the two** (Brave:
Martial +2, Prowess +3 → Cou +3).

**AI axes** (CK3's AI personality): Boldness (Bol), Compassion (Com), Greed (Gre), Energy (Ene),
Honor (Hon), Rationality (Rat), Sociability (Soc), Vengefulness (Ven), Zeal (Zea). Values are CK3's,
from the wiki's Traits page (fetched 2026-10-06).

### Childhood traits (5) — one at bébé, kept for life

| Key | M / F | CK3 | Stats | Axes |
|---|---|---|---|---|
| `bossy` | Autoritaire | Bossy | Int +1, Cou +1 | +25 Bol, +25 Gre, +15 Hon, +15 Rat, +15 Ven |
| `charming` | Adorable | Charming | Dip +1, Rus +1 | +25 Gre, +25 Soc, +15 Com, +15 Rat, +15 Ven, −15 Hon |
| `curious` | Curieux / Curieuse | Curious | Dip +1, Sag +1 | +25 Bol, +25 Com, +15 Ene, +15 Hon, +15 Soc, −15 Ven |
| `pensive` | Rêveur / Rêveuse | Pensive | Int +1, Sag +1 | +25 Rat, +15 Ene, +15 Hon, −15 Gre, −15 Bol, −25 Soc |
| `rowdy` | Turbulent / Turbulente | Rowdy | Rus +1, Cou +1 | +25 Bol¹, +25 Ene, +15 Soc, +15 Ven, −15 Com, −15 Hon, −15 Rat |

¹ The wiki's cell lost its axis name; Boldness is assumed.

### Personality traits (36) — two at ado, the 4th at adulte

A Plynling never holds two traits of the same **group**. Groups are the 16 pairs, the trio
`compassion`, and `eccentric` alone.

| Group | Key | M / F | CK3 | Stats | Axes |
|---|---|---|---|---|---|
| bravery | `brave` | Courageux / Courageuse | Brave | Cou +3 | +200 Bol, +20 Ene, +20 Soc, −20 Rat |
| bravery | `craven` | Peureux / Peureuse | Craven | Rus +2, Cou −3 | +10 Rat, −20 Ene, −20 Soc, −200 Bol |
| temper | `calm` | Calme | Calm | Dip +1, Rus +1 | +75 Rat, −10 Ene, −10 Ven, −20 Bol |
| temper | `wrathful` | Colérique | Wrathful | Dip −1, Rus −1, Cou +3 | +35 Bol, +20 Ven, +10 Ene, −20 Com, −35 Rat |
| romance | `chaste` | Pudique | Chaste | Sag +2 | +20 Hon, +10 Ene, +10 Zea, −20 Gre, −20 Soc |
| romance | `lustful` | Fleur bleue | Lustful | Dip +2² | +35 Soc, +20 Gre, +10 Ene, −10 Hon, −10 Zea |
| ambition | `content` | Content / Contente | Content | Sag +2, Rus −1 | +10 Hon, −10 Soc, −10 Ven, −10 Zea, −35 Bol, −35 Ene, −50 Gre |
| ambition | `ambitious` | Ambitieux / Ambitieuse | Ambitious | +1 all five | +75 Ene, +75 Gre, +50 Bol, +20 Soc, +10 Zea, −20 Hon |
| work | `diligent` | Travailleur / Travailleuse | Diligent | Dip +2, Int +3, Sag +3 | +75 Ene, +35 Bol, +20 Rat, +10 Ven |
| work | `lazy` | Paresseux / Paresseuse | Lazy | −1 all five | +10 Gre, −10 Com, −10 Soc, −10 Ven, −20 Bol, −50 Ene |
| constancy | `stubborn` | Têtu / Têtue | Stubborn | Int +3 | +35 Hon, +35 Ven, −10 Rat |
| constancy | `fickle` | Lunatique | Fickle | Dip +2, Int −2, Rus +1 | +20 Bol, −20 Hon, −20 Rat, −20 Ven |
| grudge | `forgiving` | Indulgent / Indulgente | Forgiving | Dip +2, Sag +1, Rus −2 | +35 Com, +20 Hon, +10 Rat, −10 Ene, −200 Ven |
| grudge | `vengeful` | Rancunier / Rancunière | Vengeful | Dip −2, Rus +2, Cou +2 | +200 Ven, +10 Ene, −10 Hon, −10 Rat, −20 Com |
| greed | `generous` | Généreux / Généreuse | Generous | Dip +3 | +35 Com, +20 Hon, +10 Soc, −200 Gre |
| greed | `greedy` | Radin / Radine | Greedy | Dip −2 | +200 Gre, −10 Hon, −20 Com |
| sociability | `gregarious` | Sociable | Gregarious | Dip +2 | +200 Soc, +35 Com, +20 Bol |
| sociability | `shy` | Timide | Shy | Dip −2, Sag +1 | −10 Ven, −10 Zea, −20 Bol, −200 Soc |
| honesty | `honest` | Franc / Franche | Honest | Dip +2, Rus −4 | +50 Hon, +20 Soc, +10 Bol, +10 Com |
| honesty | `deceitful` | Menteur / Menteuse | Deceitful | Dip −2, Rus +4 | +10 Rat, −10 Bol, −10 Com, −50 Hon |
| pride | `humble` | Modeste | Humble | Dip +1, Rus −1³ | +20 Com, +20 Hon, −10 Ene, −50 Gre |
| pride | `arrogant` | Vaniteux / Vaniteuse | Arrogant | Cou +1, Dip −1³ | +35 Bol, +20 Gre, +20 Soc, +10 Ene, −20 Com, −20 Hon, −20 Rat |
| justice | `just` | Juste | Just | Int +2, Sag +1, Rus −3 | +200 Hon, +20 Rat, +10 Ven, +10 Zea |
| justice | `arbitrary` | Capricieux / Capricieuse | Arbitrary | Int −2, Sag −1, Rus +3 | +10 Bol, −10 Com, −10 Zea, −20 Rat, −200 Hon |
| patience | `patient` | Patient / Patiente | Patient | Sag +2 | +35 Rat, +10 Ven, −10 Ene, −20 Bol |
| patience | `impatient` | Impatient / Impatiente | Impatient | Sag −2 | +20 Bol, +10 Ene, −10 Ven, −35 Rat |
| appetite | `temperate` | Frugal / Frugale | Temperate | Int +2 | +10 Ene, −10 Ven, −35 Gre |
| appetite | `gluttonous` | Gourmand / Gourmande | Gluttonous | Int −2 | +35 Gre, −10 Ene |
| trust | `trusting` | Confiant / Confiante | Trusting | Dip +2, Rus −2 | +35 Hon, +35 Soc, +20 Com, −20 Rat, −20 Ven |
| trust | `paranoid` | Méfiant / Méfiante | Paranoid | Dip −1, Rus +3 | +20 Ven, −10 Com, −20 Hon, −20 Rat, −35 Soc |
| belief | `zealous` | Superstitieux / Superstitieuse⁴ | Zealous | Cou +2 | +200 Zea, +20 Ene, −20 Rat |
| belief | `cynical` | Sceptique | Cynical | Sag +2, Rus +2 | +35 Rat, −10 Com, −20 Ene, −200 Zea |
| compassion | `compassionate` | Bienveillant / Bienveillante | Compassionate | Dip +2, Rus −2 | +200 Com, +35 Hon, +35 Soc, −20 Gre |
| compassion | `callous` | Froid / Froide | Callous | Dip −2, Rus +2 | +10 Rat, −10 Soc, −35 Hon, −200 Com |
| compassion | `sadistic` | Moqueur / Moqueuse⁵ | Sadistic | Rus +2, Cou +2 | +20 Bol, +20 Soc, −100 Com |
| eccentric | `eccentric` | Excentrique | Eccentric | Dip −2, Sag +2 | +75 Bol, −20 Hon, −20 Soc, −200 Rat |

² CK3 gives Lustful Intrigue +2 (seduction is a scheme); a romantic *fleur bleue* reads as
Diplomatie. In visits, Fleur bleue may raise the confession chance — the boy-and-girl rule is
untouched.
³ CK3 gives Humble and Arrogant no skill; these are the owner's additions.
⁴ No religion: belief in forest spirits and lucky charms.
⁵ Softened from Sadistic: it teases, nothing cruel. Prowess +4 lowered to Cou +2; axes softened
from −75 Hon / −200 Com.

Keys stay CK3's English names (code is English); only display names are French.

### Coping traits (10) — from mental breaks only (phase 3)

On top of the four, **at most two**, permanent. They touch stats and stress relief only — never
meals or any need (« Mange ses émotions » and « Sans appétit » do not change feeding).

| Key | M / F | CK3 | Stats | Axes | Stress relief (phase 3) |
|---|---|---|---|---|---|
| `comfort_eater` | Mange ses émotions | Comfort Eater | Int −1 | +5 Gre, −5 Ene | each meal −10 |
| `inappetetic` | Sans appétit | Inappetetic | Dip −1, Cou −3 | −5 Gre, −10 Ene | daily decay ×1.25 |
| `contrite` | Repentant / Repentante | Contrite | Rus −2 | +10 Com, +10 Hon, +10 Zea, −10 Ven | each pet −10 instead of −5 |
| `improvident` | Imprévoyant / Imprévoyante | Improvident | Dip +1 | +10 Zea, +10 Com, −10 Gre | each visit −30 instead of −20 |
| `reclusive` | Reclus / Recluse | Reclusive | Dip −2, Int −1 | −10 Bol, −10 Ene, −35 Soc | visits ×0.5, daily decay ×1.5 |
| `irritable` | Irritable | Irritable | Dip −2, Cou +2 | +10 Bol, +10 Ene, +10 Ven, −10 Com, −20 Rat | pets ×0.5, games ×1.5 |
| `profligate` | Dépensier / Dépensière | Profligate | — | +10 Gre, −10 Com | each bath −10 instead of −3 |
| `confider` | Confident / Confidente | Confider | Dip +1 | +20 Soc, +10 Com | visits with a friend, best friend or partner ×2 |
| `journaller` | Écrit son journal | Journaller | Sag +1 | +10 Rat | daily decay ×1.5 |
| `athletic` | Sportif / Sportive | Athletic | Cou +1 | +25 Ene, +5 Bol | games ×2 |

Dropped from CK3: Drunkard, Flagellant, Rakish, Hashishiyah, Burdened.

### Stress multipliers carried from CK3 (phase 3)

Paranoid (Méfiant) stress gain ×2; Eccentric stress gain ×1.5 and loss ×1.5; Arbitrary
(Capricieux) gain ×0.5; Diligent (Travailleur) loss ×0.5; Calm, Content, Gluttonous loss ×1.1.

### Storage — `PlynlingTrait`

`Id`, `PlynlingId` (cascade, like the journal), `Key`, `Kind` (int, append-only), `AcquiredAt`.
Unique index on (`PlynlingId`, `Key`). Schema-only migration.

### Draws

- **When:** where the sweep already writes the « est devenu… » moments (`PlynlingSweepService`,
  inside the per-item `try`), plus at adoption for the childhood trait. The rule is **"draw whatever
  its current stage is owed and it lacks"** — bébé owes 1 childhood, ado owes 2 personality, adulte
  and ancien owe the 4th. That same rule backfills every existing living Plynling on the first sweep
  after release, with no data migration. Dead rows are skipped; a resurrected one catches up.
- **How:** hashed like `PlynlingSickness.Roll` (id + purpose), never a `Random`, then **stored** — a
  trait appended to the catalog must not change anyone's existing traits. Personality draws exclude
  every group already held.
- **The 4th trait** is weighted by the ado years (from phase 2; uniform until then, and for anyone
  whose ado years predate events): each event resolved during ado adds the chosen option's axis
  weights to an *ado leaning* vector; a candidate's weight is
  `max(0.25, 1 + leaning · traitAxes / 400)` plus `0.5` per growth point earned during ado in a stat
  the trait raises. Constants are tuned by simulation in the harness.
- Each draw writes a journal moment (`JournalKind.TraitGained`, appended; detail = trait key, worded
  at display so it follows gender).

## Stats

`PlynlingStat` enum, stored nowhere as values but **append-only** (the RPG appends Force, Agilité,
Endurance at the end): `Diplomacy, Stewardship, Learning, Intrigue, Courage`.

A stat is **computed, never stored**:

```
base (1d6, hashed from id + stat — nothing stored)
+ passion bonus (+2 on one stat, table below)
+ trait modifiers (all traits, coping included)
+ growth (stored, earned through events — phase 2)
+ modifier deltas (phase 3)
+ stress-level penalty (phase 3)
```

Floor 0, **no upper limit**. `Helpers/PlynlingStats.Compute(plynling, traits, modifiers, now)`
returns the five values plus a breakdown for the tooltip.

**Growth:** five int columns on `Plynling` (`GrowthDiplomacy`, `GrowthStewardship`,
`GrowthLearning`, `GrowthIntrigue`, `GrowthCourage`), 0 in phase 1.

### Passion → stat

| Stat | Passions |
|---|---|
| Diplomatie | Music, Dance, Painting |
| Intendance | Cooking, Gardening, Rocks |
| Sagesse | Astronomy, Stories |
| Ruse | Gaming, Naps |
| Courage | Sport, Insects |

The bonus follows the innate `Passion`, except when the taught passion resolves to a catalog one
(`PlynlingPassions.Taught(p).Catalog`): then that one's stat gets it instead. A taught passion that
stays free text changes nothing — the innate one keeps the bonus.

## Personality title

The nine axes are summed over all its traits; the two largest by absolute value, each with its
sign, pick two adjectives from an 18-entry table (9 axes × 2 directions, M/F), shown as
« *{Adj1} et {adj2}* ». The 18 adjective pairs are written in the phase 1 writing pass.

## The mascot

Ping-Qilin's traits and stats are **fixed**, not drawn (`PlynlingMascot`), so she is the same
character on dev and production:

- Traits: **Adorable** (childhood) · **Vaniteuse** · **Méfiante** · **Moqueuse**.
- Base stats: **4** in all five (no 1d6).
- With Naps (+2 Rus): **Dip 3 · Int 4 · Sag 4 · Rus 12 · Cou 7.**

The same "ensure traits" path that draws everyone else's gives her rows these four instead (at
creation, then on every sweep if one is missing). She decides every event on
her own and never has stress from choices, so Méfiante's stress multiplier never matters.

## Display

- **Card:** one new `TextDisplay` under the status: the title plus trait emojis (and, from phase 3,
  modifier icons and the stress level).
- **A second button row**, the card's last, shown while alive (frozen included):
  « 📜 Personnalité » (`plyn:traits:{id}`, anyone may press) and, from phase 2,
  « ✨ Événement (n) » (`plyn:events:{id}`, only while something is pending, refused in the handler
  for anyone but the owner, like « Laver »). It cannot share the care row: that row is hidden at
  night and holds three buttons when sick.
- **« Personnalité »** answers **ephemeral**: each trait with its description and stat effects, the
  five stats with their breakdown (base, passion, traits, growth, modifiers, stress), the title.
- No new slash command: `/plynling` stays at 15.

---

# Phase 2 — Event engine

## Catalog — `Helpers/PlynlingEvents`

C# data, like `PlynlingScripts`, so the compiler and the harness can check it. Generated from
scratch writing sheets, readable by hand.

**Event:** stable `Key` (append-only); `Type` (`Pulse`, `Triggered`, `FollowUp`, `Response`);
eligible stages; a condition (`Func<EventContext, bool>` — traits, stats, sick or not, bonds…); a
base weight and weight modifiers (by trait, stat, modifier); a target filter for social events;
gendered text templates for the scene; and 2–4 options.

**Option:** stable `Key` (unique within its event, append-only); a button label (≤ 80 chars); an
optional **gate** (a trait, or a stat threshold) — a gated option is only shown to a Plynling that
meets it, with the trait's emoji; **stress costs** by trait (`traitKey → amount`, applied from
phase 3 and displayed from phase 3, *stored from phase 2* — where it already keeps an option out of
deciding alone); an optional **challenge** (stat and difficulty, or
stat against the target's stat); effects on success and on failure; **AI weights** by axis.

**Templates:** `{A}` / `{B}` names, `{a:m|f}` / `{b:m|f}` agreements, `{ils}` — the same mechanism
as visit scripts.

## Storage — `PlynlingEventInstance`

| Column | |
|---|---|
| `Id` | |
| `PlynlingId` | cascade |
| `EventKey` | |
| `TargetPlynlingId` | nullable, set null if the target is abandoned |
| `ParentInstanceId` | nullable (phase 4) |
| `CreatedAt`, `AvailableAt`, `ExpiresAt` | `AvailableAt` = `CreatedAt` except for follow-ups |
| `ResolvedAt`, `OptionKey` | null while pending |
| `DecidedAlone` | bool |
| `ChallengeSucceeded` | nullable bool |
| `CancelledAt` | nullable — death before resolution |

**Pending** = `ResolvedAt` and `CancelledAt` null and `AvailableAt` reached. Date filtering in memory
(SQLite rule). The history also feeds « not seen recently » and the ado leaning.

New column on `Plynling`: `LastPulseDay` (int, `AppTime.DayKey`), like `LastGiftDay`.

## The pulse

In `PlynlingSweepService`, inside the per-item `try`:

- One pulse per Paris day, at an instant hashed between **08:00 and 20:00** Paris.
- If **3 pulse events are already pending, the pulse is skipped** (the day is still marked). Triggered
  events, follow-ups and responses **ignore the cap** — they are rare, and dropping one would break a
  story.
- The event is a weighted draw among eligible `Pulse` events, hashed (id + day + "pulse"), excluding
  events seen in the last 14 resolutions.
- No pulse while frozen or dead. Sick is allowed; conditions may exclude events.
- `ExpiresAt` = creation + 24 h.

## Choosing — the event card

- « ✨ Événement (n) » opens the **oldest** pending event as an **ephemeral** Components V2 card:
  the Plynling's sprite, the scene, then one line per available option — gate emoji, stress cost
  (« 😣 +30 stress (Peureux) »), challenge chance (« 🎲 Courage : 65 % ») — and one button per option,
  custom-id `plev:pick:{instanceId}:{optionKey}`.
- The pick **defers first**, then in **one save**: the hashed challenge roll, the effects, the journal
  moment, `ResolvedAt` / `OptionKey`. A pick on an already-resolved instance is refused politely.
- After the save: the ephemeral card shows a short result and « Raconté dans #canal »; the **public
  story** is posted in `PlynlingAnnouncer.GameChannelId` (guild owning that channel only, like
  deaths), `AllowedMentions.None`, names through `PlynlingCardUi.SafeName`.

## Challenges

`chance = clamp(50 + 5 × (stat − difficulty), 5, 95)` — difficulty typically 4 (easy), 7 (normal),
10 (hard). Against another Plynling: `clamp(50 + 5 × (statA − statB), 5, 95)`. The shown percentage
is the one rolled; the roll is hashed from the instance id.

## Deciding alone

- At expiry the sweep resolves it. While frozen nothing is decided; it waits for the thaw.
- **Eligible options:** no gate it fails **and no stress cost for any of its traits**. Every event
  must have at least one option that is ungated with no stress cost at all (harness invariant), so
  the set is never empty.
- Weight per option: `max(1, 100 + Σ_axis optionAi[axis] × plynlingAxis[axis] / 10)`, hashed draw.
- The story says it decided on its own (« … a décidé tout·e seul·e », gendered).
- **The mascot** decides immediately at creation; she never has anything pending.

## Effects in phase 2

`GrowStat(stat, +1)`; `Affinity(target, delta)` through `PlynlingBonds` (band changes announced as
after a visit); a journal moment (`JournalKind.EventStory`, appended; detail = instance id). **No
cailloux, no items** (no farming). Phase 3 adds stress and modifiers, phase 4 follow-ups and
responses.

## The public story

A paged Components V2 card, like visit stories: picture(s), text, « 2/4 », and ◀ ▶. Pages: the
scene; the choice (or « a décidé tout·e seul·e »); the challenge if any; the outcome. Every button
its own verb: `evs:prev:{instance}:{page}`, `evs:next:…`, `evs:first:…`, `evs:last:…` (Fin on every
page but the last; ▶ becomes « ↺ Début » on the last). **Rebuilt from the stored instance** on every
click — line picks are hashed from the instance id — so a restart never breaks the arrows.

## First content

Phase 2 ships **six starter events** covering every mechanic (2 bébé, 2 ado, 2 adulte/ancien, one of
them social). The first content wave (phase 5) brings the catalog to 15–20 — at least 5 bébé, 5 ado,
10 adulte/ancien — through reviewed writing sheets.

---

# Phase 3 — Stress and modifiers

## Stress

- `Plynling.Stress` (int, 0–400). **Levels at 100, 200, 300.**
- **Gain** — only from owner choices: the option's stress cost for each trait it opposes (typically
  +20 to +40), times the trait multipliers above; some failed challenges add stress (owner choices
  only). Deciding alone never adds stress.
- **Decay** — once a day at the **05:00 morning** that `Settle` already plays: −15 × loss
  multipliers (traits, coping, modifiers).
- **Relief** — care lowers it: pet −5, game −15, visit −20, bath −3, adjusted by coping traits.
- **Tamagotchi effects** (never hunger, never death):

| Level | Happiness drains | Stats |
|---|---|---|
| 0 (< 100) | ×1 | — |
| 1 | ×1.15 | — |
| 2 | ×1.35 | −1 all |
| 3 | ×1.6 | −2 all |

Every stress change is a transition and **rebases** the needs, so the drain rate is constant between
two transitions.

## Mental breaks

- Crossing a level **upward** queues a `Triggered` mental-break event (ignores the cap), drawn among
  break events for that level.
- Outcomes, by option and roll: a **coping trait** (drawn uniformly among those it lacks; none
  offered once it has two), a **modifier**, or simply a **bad event**.
- **Every outcome lowers stress by 60–100**, so a break resolves the level it was triggered by.
- **The one exception to "deciding alone is harmless":** a break left to expire is decided alone and
  may still end in a coping trait, a negative modifier or a bad event. This keeps the hard rule,
  because stress — and therefore every break — only ever comes from choices the owner made.

## Modifiers

- **Storage — a column, `Plynling.Modifiers`** (`key:unixSeconds;…`), not a table: `PlynlingLife` is
  pure and reads only the row, and `Settle` must see every modifier's end to play it in time order.
  The journal keeps the history; the instance that applied one can be found from the event history.
- **Catalog — `Helpers/PlynlingModifiers`:** M/F name, icon, description, duration, and effects: stat
  deltas; multipliers on happiness, hygiene and hunger drain; meal factor; happy-gift chance; stress
  decay; event weights. (A pet-cooldown effect was dropped: the cooldown is an in-memory gate per
  petter on a shared card.)
- The **same key re-applied refreshes** its end; different keys combine, each combined multiplier
  clamped to 0.5–2.
- **A negative modifier never touches hunger** — the death clock and the warning DM can only become
  more lenient. Negative tamagotchi modifiers come only from owner choices and mental breaks;
  deciding alone can only apply positive or neutral ones (mental breaks excepted, see above).
- Durations run on the wall clock, freeze included.
- Display: icons on the card's personality line, details in « Personnalité ».

## The `Settle` timeline

`Settle` becomes a small timeline of **due moments, played in time order**, rebasing at each:
self-freeze thaw, starvation, the 05:00 morning (sickness rolls and stress decay), and **modifier
expiries**. Every drain helper (`HungerAt`, `HappinessAt`, `HygieneAt`, `EffectiveDeathAt`, `WarnAt`)
reads the effective rates (stress level × modifiers) of the current segment; the hygiene crossing
under `DirtyBelow` stays an exact piece inside a segment. This is the main technical change of the
phase.

---

# Phase 4 — Social and chains

## Follow-ups

An option effect `FollowUp(eventKey, delayMin, delayMax)` creates an instance with a hashed
`AvailableAt` in the future, the same target, and `ParentInstanceId`. Ignores the cap. The story
shows « Suite de… ». Cancelled (`CancelledAt`) if the Plynling dies first; removed by cascade on
abandonment.

## Response events — the four big moments

**Déclaration, défi de rivalité, pacte de meilleurs amis, réconciliation.**

- The initiator's option creates a `Response` instance on the **target's** Plynling (✨ on the
  target's card, ignores the cap, 24 h). The initiator's story ends on « … attend la réponse de
  {B} »; the response publishes its own story with both.
- The target's owner chooses; otherwise it is decided alone through **CK3-style acceptance**:
  affinity, hidden compatibility, its traits and axes.
- **Existing rules apply:** a declaration follows the visit confession rules (a boy and a girl, one
  living partner, `InCoupleAsync`); bonds and their announcements go through `PlynlingBonds`. A pact
  lifts affinity into the best-friend band; a reconciliation lifts enemies into the acquaintance
  band; a rivalry challenge is stat against stat.

## Targets

Any living, non-frozen Plynling **of the same guild** matching the event's target filter (friend,
rival, stranger…), weighted by relation. The mascot can be a target and answers immediately, in
character. **At most one social event per pair of Plynlings per 24 h**, checked against the stored
instances (survives a restart).

## Anti-griefing

An event may change **another owner's Plynling only through affinity and the bond** — never its
stress, stats or modifiers. Enforced by the effect types: target-side effects are a separate, closed
set.

## On-actions — `Helpers/PlynlingOnActions`

`Fire(kind, plynling, target?)`, called **after** the owning service's save, in its own save,
swallow-and-log: a failed event creation never breaks a visit, a death or an adoption.

| On-action | Event |
|---|---|
| Adoption | welcome event revealing the childhood trait |
| Turned ado / adulte | the trait draw told as an event — the trait stays random, options are reactions (flavour, a small growth) |
| After a visit | hashed chance of a follow-up (« {B} a oublié quelque chose… ») |
| Death of a friend, best friend or partner | a grief event for the survivors — positive or neutral options only |
| Fell sick / recovered | a sickness event / a recovery event |

From this phase the silent phase-1 draws at stage changes are replaced by these events (the trait
is still drawn and stored the same way; the event tells it).

## Content

About 10 social events (the four big moments included) and 3–4 chains.

---

# Phase 5 — Content waves

- Every event, trait, modifier, title adjective and story template follows
  `docs/plynling-writing-style.md` (re-read before each wave), in both genders.
- **CK3 as a source of situations and mechanics only.** The game files (`game/events/`,
  `game/localization/french/`, `game/common/on_action/`) are read from a folder **outside the repo**
  — the remote is public and those files are Paradox's. Nothing is translated or copied; every line
  is rewritten in Plynling style.
- Workflow, as for `PlynlingScripts`: writing sheets in the scratch folder → generated C# → owner
  review.
- Waves of 10–15 events, keeping stage coverage balanced.

---

# Cross-cutting

## Harness checks (the owner's scratch checks)

- Unique keys: events, options within an event, traits, modifiers.
- **Every event has at least one ungated option with no stress cost** (the deciding-alone invariant
  behind the hard rule).
- Target-side effects are only affinity / bond.
- Discord caps: custom ids ≤ 100 chars, button labels ≤ 80, ≤ 5 buttons per row, ≤ 40 components on
  the event card and the story card, measured on the largest event.
- Every template and story expanded for the four gender pairs; the gender word bans of
  `plynlingui` apply.
- The 4th-trait weighting and the AI choice weights tuned by simulation, never by feel.

## Project rules applied

- **Append-only:** trait, event, option and modifier keys; `PlynlingStat`; new `JournalKind`
  values (`TraitGained`, `EventStory`, `MentalBreak`) appended at the end; `PlynlingTrait.Kind`.
- **Migrations are schema-only:** tables `PlynlingTrait`, `PlynlingEventInstance`; columns `Stress`,
  `Modifiers`, `StressLossBonusPercent`, `LastPulseDay`, the five growth columns.
- **Custom-ids:** `plyn:traits:`, `plyn:events:`, `plev:pick:`, `evs:prev|next|first|last:` — grep
  the project for collisions before adding.
- Handlers live in `Interactions/Components/` with `ignoreGroupNames: true`, **never on
  `PlynlingModule`** (the `/pl` rule).
- Every handler that touches the database **defers first**.
- Everything added to the sweep goes inside its **per-item `try`**.
- Rolls are hashed (`PlynlingSickness.Roll` style), never `Random`; text lines picked through
  `ResponsePicker` where a pool repeats.
- Names and typed passions are hostile input: `SafeName`, mentions off on every public story.

## Docs

- New `docs/agents/plynling-events.md`, plus a row in the CLAUDE.md table; `plynling.md` points to it
  and records the new stage thresholds.
- README: a section. `/help`: one line.
- **`/plynling help` is 106 characters short of the 6000 cap**: split it into **two pages** with a
  button (care / personality and events), each measured.

## Owner tools

`/debug plynling` gains event actions (force an event, set stress, add or remove a modifier) — owner
only, absent from `/help`. Without it, breaks and chains would take days to test on the dev guild.
No new `/admin` action for now.

## Risk to watch

With N active players the game channel gets about N stories a day. If it gets noisy, a later option
is to post only notable outcomes (bond changes, mental breaks, chains) and keep the rest in the
journal.
