# The Coprin — a second rare Plynling — design

**Date:** 2026-09-25 · **Status:** approved, art pending review

## Goal

A new rare mushroom Plynling, the **Coprin** — the shaggy ink cap (*Coprinus comatus*), the tall
white mushroom that melts into black ink. Like every species it is data plus art: no gameplay
differs. Its signature is the ink, and the ink tells its state: the hungrier it is, the more it
melts.

## Decisions

| Question | Decision |
|---|---|
| Rarity | **Rare, sharing the rare slot** with the Mystique: Mystique **27 → 14**, Coprin **13** (out of 300). The family ladder stays 70 % commun / 18 % peu commun / 9 % rare / 3 % légendaire; each rare is ~4.5 %. |
| Name on the card | **« Coprin »** — short, like « Mystique » and « Doré ». |
| The ink | **An inky rim and an idle drip, growing with hunger.** |

## 1. Data

- `PlynlingSpecies`: **`Coprin` appended after `Solaire`** (value 12). The enum is stored as an int
  and append-only; appending changes no existing row, so there is no migration.
- `PlynlingCatalog.Species`: `new SpeciesInfo(PlynlingSpecies.Coprin, PlynlingFamily.Mushroom,
  "Coprin", PlynlingRarity.Rare, 13, 0x5A5A70)`, and the Mystique's weight **27 → 14**. The
  accent is an ink slate: a near-black strip would vanish on Discord's dark theme. The ladder
  comment is updated to say the rare slot can be shared. Only future adoptions see the new odds.

## 2. The adult — `coprin()` in `species.py`

A tall, shaggy, white cap — **taller than wide**, a closed bell/cylinder — with a beige-brown
skullcap at its apex and small upturned scales for the shag, over a slim white stem wearing the
shared face. Like the other rare species it carries **sparkles** (silver-white).

**The ink, by mood** (the cap's lower edge):

| Mood | Ink |
|---|---|
| happy, content, sad | a thin black rim |
| hungry | a thicker rim |
| starving | the lower cap visibly dissolving into ragged drips |
| frozen | the drips frozen in place, like black icicles |

**Idle touch** (`motion.py`): a black drop forms at the rim and slides **straight down** to the
ground, then the loop restarts. It plays wherever species touches play (not starving, not
frozen). Nothing moves side to side; frame 0 is the still.

## 3. The baby — `coprin_baby()`

A small closed white "egg" with a brown tip, on the shared baby frame (body rows 20–28, no feet,
cap ending at row 18, face 3 rows low). **No ink at rest** — too young to melt — and **no idle
drip**; the ink appears with hunger only (a rim when hungry, melting when starving, frozen drips
when frozen).

## 4. Wiring

- `common.SPECIES["coprin"]`: a white cap palette, a beige-brown `spot` (the skullcap and scales),
  an ink-dark `gill`, a silver-white `sparkle`, tier `"rare"`.
- `memorials.accent`: the Coprin's memorials take its **ink** (`gill`) as accent.
- `PlynlingArt.Key(Coprin) => "coprin"`; `Coprin` joins `PlynlingArt.StagedSpecies` and `STAGED`
  in `export.py`.
- **17 new files**: 6 adult sprites, 6 baby sprites, 5 memorials. No existing file changes, so no
  art version bump.

## 5. Checks (scratch harnesses)

- **artcheck**: 84 sprite URLs (72 + 12), every one existing and animated; memorials and key parity
  with `common.py` for the Coprin; `STAGED` ↔ `StagedSpecies`.
- **odds**: the mushroom family's weights still total 300; Mystique 14, Coprin 13; `PickSpecies`
  reaches the Coprin.
- **animcheck**: the 12 new loops slot-exact; the 106 existing files byte-identical.
- Build with `-warnaserror`.

## 6. Review flow

The adult and the baby are prototyped in all six moods and sent as previews; nothing is exported
until the owner approves.

## Docs

`README.md` (« three common, one uncommon, two rares — the Mystique and the Coprin — and a
legendary Doré »), `tools/plynling-art/README.md` (the Coprin and its ink rule), and `CLAUDE.md`
only if a new trap emerges. `/plynling help` names tiers only and is unchanged.
