# Plynlings: gender and the Sunflower family

## Context

Plynlings v1 (plan: `docs/superpowers/plans/2026-09-24-plynlings-v1.md`) shipped one
family — six mushroom species — and wrote every line about a Plynling in the masculine.
This adds two things:

1. **A gender**, male or female, rolled 50/50 at adoption, with the French agreeing
   everywhere a specific Plynling is talked about.
2. **A second family, Sunflowers**, chosen at adoption, with its own six variants, sprites
   and memorials. More families will follow, so the catalog is shaped around a list of
   families rather than special-casing this one.

Plynlings have only ever existed in the dev guild, so no live data needs migrating.

## Decisions (from the design session)

| # | Question | Decision |
|---|---|---|
| 1 | Does the French agree with a female Plynling? | **Full agreement**, via paired pools |
| 2 | Existing rows | Dev only — the column gets a default, no data step |
| 3 | What is a Sunflower Plynling? | A **second family, chosen at adoption**; species rolled within it |
| 4 | Variants | Six, same ladder as mushrooms (3 common / 1 uncommon / 1 rare / 1 legendary) |
| 5 | Food | **Everyone eats the mushroom foods** — feeding is unchanged |
| 6 | Sunflower anatomy | **Face on the flower's disc**, petals around it, stem body, two leaf arms |
| 7 | Sunflower memorials | **Same five tombs, re-tinted**; tier-5 statue in the sunflower's shape |
| 8 | Is the noun gendered? | **Yes**: *une Plynling*, *ta Plynling* for a female one |
| 9 | Where gender shows | **♂ / ♀ by the name** (card, graveyard) + the adoption line; no art difference |
| 10 | Family-flavoured lines? | **No**: pools split by gender only, family-neutral wording |
| 11 | Is `family:` required? | **Yes**, a choice list |
| — | Representation of gendered text | **`GenderedLines` paired-pool type** (not flat `…F` arrays, not grammar tokens) |

## Data and catalog

**Gender.** `enum PlynlingGender { Male, Female }` in `Models/Plynling.cs`, and a
`Plynling.Gender` column. Migration `AddPlynlingGender` is schema-only: `AddColumn` with
default `0` (Male). `PlynlingService.AdoptAsync` rolls it 50/50 and passes it to
`PlynlingLife.Create`, which takes it as a parameter and stays pure (deterministic in the
harnesses). Resurrection reuses the row, so a Plynling keeps its gender through death.
Nothing edits it afterwards — no staff command.

**Families.** `enum PlynlingFamily { Mushroom, Sunflower }` with
`[ChoiceDisplay("Champignon")]` / `[ChoiceDisplay("Tournesol")]`. The family is **not
stored**: `SpeciesInfo` gains a `Family` field and a Plynling's family is
`PlynlingCatalog.Info(p.Species).Family`.

**Species.** `PlynlingSpecies` is stored as an int, so it is **append-only**: the six new
values go after `Dore` — `Tournesol, Citron, Roux, Ivoire, Nocturne, Solaire`.

| Species | Display name | Rarity | Weight | Look |
|---|---|---|---|---|
| `Tournesol` | Tournesol | commun | 70 | classic yellow petals, brown disc |
| `Citron` | Tournesol citron | commun | 70 | pale lemon petals |
| `Roux` | Tournesol roux | commun | 70 | red-bronze petals |
| `Ivoire` | Tournesol ivoire | peu commun | 54 | cream-white petals, dark disc |
| `Nocturne` | Tournesol nocturne | rare | 27 | deep burgundy, almost black |
| `Solaire` | Tournesol solaire | légendaire | 9 | gold petals, glowing disc |

`RollSpecies(family)` / `PickSpecies(family, roll)` roll only within the family; the total
weight is per family (300 each today). Every family uses the same odds ladder.

**Art keys.** `PlynlingArt.Key` currently ends in `_ => "dore"`, so a species missing from
the switch would silently render as a Doré mushroom. It becomes exhaustive and **throws**
on an unknown species. New keys are family-prefixed — `tournesol`, `tournesol_citron`,
`tournesol_roux`, `tournesol_ivoire`, `tournesol_nocturne`, `tournesol_solaire` — so future
families cannot collide with mushroom names. Mushroom filenames are untouched, so
`PlynlingArt.Version` stays `1`.

## Text and grammar

**`GenderedLines`.** A small `record GenderedLines(string[] M, string[] F)` with
`string[] For(PlynlingGender)`, in `BotResponses`. Every Plynling pool becomes one, so a
call site cannot compile without naming the gender:

`PlynlingAdoptLines`, `PlynlingAdoptRareLines`, `PlynlingFeedLines`, `PlynlingPetLines`,
`PlynlingDeathLines`, `PlynlingResurrectLines`, `PlynlingWarningLines`,
`PlynlingStaffFreezeDms`, `PlynlingStaffThawDms`, `PlynlingStaffRenameDms`.

`ResponsePicker` is unchanged — each half is an ordinary array, the bucket stays the
channel or owner id. Placeholders are identical in both halves.

**The adoption line announces the gender**: every `M` line of the two adopt pools says
*garçon*, every `F` line *fille* ("C'est un garçon !" / "C'est une fille !").

**The feminine noun.** A female Plynling is *une Plynling*, *ta Plynling*, *la Plynling de
…*. Text that is not about one specific Plynling stays in the generic masculine:
`/plynling help`, `/help`, the README, command and option descriptions, and person-level
refusals (`NoPlynling`, `NoneFor`, `AlreadyHasOne`, `NoGrave`, `ResurrectBlocked`).

**Family-neutral wording.** Lines must work for any family. The three that mention a cap
are rewritten: "pointe le bout de son chapeau" → "pointe le bout de son nez", "Le chapeau
de **{0}** frétille de bonheur" → "**{0}** frétille de bonheur", "Un chapeau de moins sur
cette terre" → "Un Plynling de moins sur cette terre" (*Une Plynling* in the `F` half).
`WorkLines` keep their spore and morel jokes: they describe the job, not the Plynling.

**Fixed text becomes gender-aware.** In `PlynlingText`, every line that describes the
Plynling becomes a function of its gender: `NotYours`, `Dead`, `Frozen`, `Wasted`,
`PetCooldown` ("Tu l'as caressé/caressée"), `AlreadyFrozen`, `NotFrozen`,
`TooHungryToFreeze`, `ThawStaffOnly`, `FrozenNotice`, `ThawedNotice`. The paths that return
these already have the Plynling loaded.

**The card and the graveyard.** `PlynlingCardUi.MoodLabel(mood, gender)` (content/contente,
heureux/heureuse, affamé/affamée, gelé/gelée; "triste" and "mourant de faim" are invariant); the
frozen clock reads "Gelé jusqu'au" / "Gelée jusqu'au"; the heading carries ♂ / ♀ beside
the name. `GraveLine` gets the symbol and "mort" / "morte".

## Art

**Generator.** A new `tools/plynling-art/sunflowers.py` beside `sprites.py`, reusing
`common.py` (palette, scaling, export). One targeted refactor: the face routines in
`sprites.py` (eyes, blush, brows, mouths per mood) take an origin, so the sunflower wears
the same face on its disc — including the closed-mouth frozen face chosen for mushrooms.
**All 70 existing PNGs must export byte-identical after the refactor** (hashed before and
after).

**The sunflower Plynling.** Face on the brown disc, a ring of petals, a short green stem
body, two leaf arms. Moods mostly move the petals:

| Mood | Petals |
|---|---|
| content | open, relaxed |
| happy | perked up, a sparkle |
| sad | slightly drooping |
| hungry | wilting, leaves limp |
| starving | heavy droop, browned edges, a fallen petal or two |
| frozen | curled in, frosted |

6 variants × 6 moods = **36 sprites**, 256 px, nearest-neighbour, like the mushrooms.

**Memorials.** Tiers 1–4 are the existing stones tinted with the variant's accent; on
sunflower graves the tier-3/4 flowers become tiny sunflowers. Tier 5 is a statue in the
sunflower Plynling's shape. 6 × 5 = **30 memorials**. `MemorialName` is unchanged.

`assets/plynlings/` goes from 70 to **136** files (roughly +300 KB).

**Review checkpoint.** Before the full set, the classic Tournesol in all six moods and its
tier-5 statue are drawn and sent for review; the other variants and memorials follow only
after approval.

## Commands and docs

- **`/plynling adopt name: family:`** — `family` is a required `PlynlingFamily`, rendered
  as a choice list. English option name, French choice display names.
- Every other command keeps its signature; lines and refusals pick by `p.Gender`.
- `PlynlingAnnouncer` picks death, resurrection and warning lines by gender. The tier-5
  statue follows the family through `PlynlingArt.Memorial(species, tier)` with no change.
- **`/plynling help`**: description "un petit champignon ou un tournesol"; mentions the
  family choice and that each Plynling is a boy or a girl. Re-measured against the embed
  caps (1024 per field, 6000 total).
- **`/help`**'s single Plynlings line: "Adopte un petit champignon ou un tournesol…".
- **README** Plynlings section: families, the sunflower variants, gender. Commands table
  unchanged.
- **CLAUDE.md** notes: `GenderedLines` (every Plynling pool is paired; a call site names
  the gender); the feminine noun and the generic-masculine rule; `PlynlingSpecies` is
  append-only; the family is derived, not stored; `PlynlingArt.Key` is exhaustive; a new
  family is a catalog entry plus art and needs no new text.

## Out of scope

- Family-specific foods (decision 5) and family-flavoured lines (decision 10).
- Any gender marker on the sprites (decision 9).
- Changing a Plynling's gender or family after adoption.
- A third family — the shape supports it, but none is designed here.

## Testing

No test project; scratch harnesses referencing the real project, each mutation-tested.

| Harness | New checks |
|---|---|
| `plynlingcheck` | the roll never leaves its family; identical odds ladder per family; `Create` and `Resurrect` keep the gender |
| `plynlingdb` | migration applies; gender round-trips; many adoptions yield both genders; family roll end to end |
| `plynlingui` | every `GenderedLines` field by reflection — ≥3 lines per half, same placeholders, format-safe, in the table of contents; crude grammar guard on whole words (no `il`, `-le`, `mort` in `F`; no `elle`, `-la`, `morte` in `M`), with an explicit allow-list for legitimate exceptions such as the noun *la mort*; adopt halves say *garçon* / *fille*; card and graveyard text per gender |
| `artcheck` | 136 files; `PlynlingArt.Key` parity with `common.py`; an unknown species throws; the 70 mushroom PNGs hash-identical |
| `helpcheck2`, `modulecheck` | both help embeds within caps; the `family` option passes the English naming check |

Build with `-warnaserror` stays clean. Live checks in the dev guild: adopt one of each
family, read both genders' cards, freeze/thaw a female one, kill one of each family and
read the graveyard.

## Implementation order

Each task stops for review and a manual commit.

1. Gender and families in the model and catalog; migration; exhaustive `Key`.
2. `GenderedLines`; every pool and fixed line written in both genders.
3. `family:` on adopt; gender wired through every call site.
4. Face refactor + sunflower draft (classic Tournesol, six moods, tier-5 statue) —
   **stop for art review**.
5. Full sunflower set and memorials.
6. `/plynling help`, `/help`, README, CLAUDE.md.
