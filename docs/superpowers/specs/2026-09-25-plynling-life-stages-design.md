# Plynling life stages — design

**Date:** 2026-09-25 · **Status:** approved; art approved (bébé only — see §3)

## Goal

A Plynling grows up: **bébé → ado → adulte → ancien**. The stage is cosmetic — a label on the
card and, for a **bébé** of a species that has the art, a different picture. Ado and ancien wear
the adult picture. The Cèpe gets its baby art first; the other five species may follow.

## Decisions

| Question | Decision |
|---|---|
| What a stage changes | **Looks only.** Hunger, happiness, food, freezing and death are untouched. |
| Thresholds (time actually lived) | bébé **< 2 days**, ado **< 14 days**, adulte **< 180 days**, ancien **≥ 180 days** (6 months, a month taken as 30 days like the memorials — the same point the memorial becomes a statue). |
| Art direction | **Body shape only** — proportions, posture, colour. No props. **Only the bébé gets its own picture**: ado and ancien were prototyped and dropped for simplicity; they show the adult sprite. |
| Species without stage art | **Labelled anyway.** Every Plynling shows its stage from day one; a species with no stage art shows its adult picture at every stage until its art lands. |
| Announcing a new stage | **None.** The card simply shows it the next time someone looks. |

## 1. The rule — `Helpers/PlynlingLife`

```csharp
public enum PlynlingStage { Baby, Teen, Adult, Elder }   // in PlynlingCatalog.cs, beside PlynlingMood

public static PlynlingStage Stage(Plynling p, DateTimeOffset now) => Age(p, now).TotalDays switch
{
    < 2 => PlynlingStage.Baby,
    < 14 => PlynlingStage.Teen,
    < 180 => PlynlingStage.Adult,
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

`cepe(state, frame, shadow, stage="adult")`; `stage="baby"` draws `cepe_baby`, anything else the
adult. The baby wears the shared face, the six moods and the idle loop from `motion.py`, under
the same rules (nothing moves side to side; frame 0 is the still sprite).

- **Bébé** — a small, squat button: a big round cap sitting low over a stubby body with no feet,
  the face set lower.
- **Ado, adulte, ancien** — today's Cèpe, **byte-identical** to the v3 files.

`export.py` writes the baby for every species listed in a `STAGED` set (only `"cepe"` for now),
6 moods each: **6 new animated WebPs**. The baby's filename inserts the stage; the adult's does
not change:

```
plynling_cepe_happy_v3.webp          # adult (and ado, ancien) — unchanged filename, unchanged file
plynling_cepe_baby_happy_v3.webp
```

**No art version bump.** No existing file changes, and new files are new URLs, so nothing Discord
has cached is invalidated. `ART_VERSION` and `PlynlingArt.Version` stay at 3.

## 4. The bot — `Helpers/PlynlingArt`

```csharp
// Species whose bébé has its own art. Must match STAGED in export.py (artcheck).
public static readonly IReadOnlySet<PlynlingSpecies> StagedSpecies = new HashSet<PlynlingSpecies> { PlynlingSpecies.Cepe };

public static string Sprite(PlynlingSpecies species, PlynlingStage stage, PlynlingMood mood)
```

Only a bébé of a species in `StagedSpecies` gets the `_baby` segment; every other combination
resolves to today's adult filename.
Both callers that show a living Plynling pass the stage alongside the mood:
`PlynlingModule` (the card thumbnail) and `PlynlingAnnouncer.AnnounceResurrectionAsync`. The
death announcement shows a memorial and is untouched.

## 5. Checks (scratch harnesses, as before)

- **artcheck** — every stage × mood of every species resolves to an existing animated WebP; only a
  staged species' bébé differs from its adult file; no orphan files; `StagedSpecies`
  matches `STAGED` in `export.py`. Mutation-tested.
- **animcheck** — the 6 new loops run 16 slots of 125 ms, each slot exactly what the code draws;
  all 70 pre-existing files stay byte-identical to v3.
- **stage check (C#)** — the boundaries (just under / at 2, 14, 180 days), that frozen time does not
  advance the stage, and that `StageLabel` agrees in gender.
- Build with `-warnaserror`.

## 6. Review flow

The Cèpe stages were prototyped in all six moods and reviewed; the owner kept the bébé and
dropped ado and ancien art (both wear the adult).

## Docs

`README.md` (Plynlings section: the four stages and their ages), `/plynling help` (one line,
re-checked against the embed caps), `tools/plynling-art/README.md` (stages, `STAGED`, filename
scheme) and a `CLAUDE.md` note: stages are derived from `Age` and never stored, `StagedSpecies`
must match `STAGED`, and the adult filename deliberately carries no stage segment.
