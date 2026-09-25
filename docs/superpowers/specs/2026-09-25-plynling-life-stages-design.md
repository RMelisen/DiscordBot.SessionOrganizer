# Plynling life stages — design

**Date:** 2026-09-25 · **Status:** approved, art pending review

## Goal

A Plynling grows up: **bébé → ado → adulte → ancien**. The stage is cosmetic — a label on the
card and, for species that have the art, a different picture. The Cèpe gets its stage art
first; the other five species follow later, one at a time.

## Decisions

| Question | Decision |
|---|---|
| What a stage changes | **Looks only.** Hunger, happiness, food, freezing and death are untouched. |
| Thresholds (time actually lived) | bébé **< 1 day**, ado **< 7 days**, adulte **< 90 days**, ancien **≥ 90 days**. Aligned with the memorial tiers (7 d → petite stèle, 90 d → urne). |
| Art direction | **Body shape only** — proportions, posture, colour. No props. |
| Species without stage art | **Labelled anyway.** Every Plynling shows its stage from day one; a species with no stage art shows its adult picture at every stage until its art lands. |
| Announcing a new stage | **None.** The card simply shows it the next time someone looks. |

## 1. The rule — `Helpers/PlynlingLife`

```csharp
public enum PlynlingStage { Baby, Teen, Adult, Elder }   // in PlynlingCatalog.cs, beside PlynlingMood

public static PlynlingStage Stage(Plynling p, DateTimeOffset now) => Age(p, now).TotalDays switch
{
    < 1 => PlynlingStage.Baby,
    < 7 => PlynlingStage.Teen,
    < 90 => PlynlingStage.Adult,
    _ => PlynlingStage.Elder,
};
```

Derived from `Age`, so it inherits every rule `Age` already has: frozen and dead time are never
counted (a frozen Plynling does not grow up), a resurrected one resumes at the stage its lived
age gives it, and **nothing is stored** — no column, no migration. `PlynlingStage` is never
persisted, so unlike `PlynlingSpecies` it is not append-only.

## 2. The card — `Helpers/PlynlingCardUi`

The heading's third line names the stage between the owner and the age:

```
## Pouf
♀ Cèpe · *commun*
à @owner · bébé · âgée de 5 h
```

`StageLabel(PlynlingStage, PlynlingGender)` beside `MoodLabel`: **bébé**, **ado**, **adulte**,
**ancien / ancienne** (the only one that agrees, through `PlynlingGrammar.Agree`). Every species
shows it. `/graveyard` rows and the death announcement are unchanged — a grave is measured by
its memorial, not a stage.

## 3. The art — `tools/plynling-art`

`cepe(state, frame, shadow, stage="adult")`. Only the body changes; every stage wears the shared
face, the six moods and the idle loop from `motion.py`, under the same rules (nothing moves side
to side; frame 0 is the still sprite).

- **Bébé** — a short, round body under an oversized, rounder cap, the face set low: a button
  mushroom.
- **Ado** — taller and slimmer than the adult, a smaller cap tipped slightly: lanky.
- **Adulte** — today's Cèpe, **byte-identical** to the v3 files.
- **Ancien** — the cap drooping and flatter, the tones slightly faded, the gills hanging below
  the rim like a white moustache.

`export.py` writes the three new stages for every species listed in a `STAGED` set (only
`"cepe"` for now), 6 moods each: **18 new animated WebPs**. Filenames insert the stage for
non-adult stages only:

```
plynling_cepe_happy_v3.webp          # adult — unchanged filename, unchanged file
plynling_cepe_baby_happy_v3.webp
plynling_cepe_teen_happy_v3.webp
plynling_cepe_elder_happy_v3.webp
```

**No art version bump.** No existing file changes, and new files are new URLs, so nothing Discord
has cached is invalidated. `ART_VERSION` and `PlynlingArt.Version` stay at 3.

## 4. The bot — `Helpers/PlynlingArt`

```csharp
// Species whose life stages have their own art. Must match STAGED in export.py (artcheck).
public static readonly IReadOnlySet<PlynlingSpecies> StagedSpecies = new HashSet<PlynlingSpecies> { PlynlingSpecies.Cepe };

public static string Sprite(PlynlingSpecies species, PlynlingStage stage, PlynlingMood mood)
```

An adult, or any stage of a species not in `StagedSpecies`, resolves to today's adult filename.
Both callers that show a living Plynling pass the stage alongside the mood:
`PlynlingModule` (the card thumbnail) and `PlynlingAnnouncer.AnnounceResurrectionAsync`. The
death announcement shows a memorial and is untouched.

## 5. Checks (scratch harnesses, as before)

- **artcheck** — for staged species, all 4 stages × 6 moods resolve to existing animated WebPs;
  unstaged species resolve every stage to the adult file; no orphan files; `StagedSpecies`
  matches `STAGED` in `export.py`. Mutation-tested.
- **animcheck** — the 18 new loops run 16 slots of 125 ms, each slot exactly what the code draws;
  all 54 pre-existing files stay byte-identical to v3.
- **stage check (C#)** — the boundaries (just under / at 1, 7, 90 days), that frozen time does not
  advance the stage, and that `StageLabel` agrees in gender.
- Build with `-warnaserror`.

## 6. Review flow

The three Cèpe stages are prototyped in all six moods and sent as previews before anything is
exported, then iterated on until approved — the same loop as the idle animation.

## Docs

`README.md` (Plynlings section: the four stages and their ages), `/plynling help` (one line,
re-checked against the embed caps), `tools/plynling-art/README.md` (stages, `STAGED`, filename
scheme) and a `CLAUDE.md` note: stages are derived from `Age` and never stored, `StagedSpecies`
must match `STAGED`, and the adult filename deliberately carries no stage segment.
