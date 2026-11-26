# Plynling badge art — design

## Intent

Plynling badges have no pictures yet. A palier tier shows as a medal before a Unicode emoji (🥉🎲) and a
moment as its bare emoji (`BadgeInfo.Icon`). Give all of them pixel-art icons, the way the member badges and
collection sets have them, so a Plynling's journal reads at a glance and looks like part of the same game.

## Goals, non-goals, anti-goals

- **Goal**: 62 icons (11 paliers × 5 tiers + 7 moments), uploaded as application emojis, shown wherever a
  Plynling badge's icon prints today: the journal's badge list and moment lines, and the « Nouveau badge »
  notice.
- **Non-goals**: no change to the badges themselves (keys, thresholds, rewards, names), and no change to the
  member badges' art.
- **Anti-goal**: the Plynling badges must not look interchangeable with the member « Succès » shields. They
  belong to the creature, not to the person.

## Approaches

- **A. The member heater shield.** No new frame, one badge language everywhere. Rejected: a Plynling's
  journal would look like a member's « Succès ».
- **B. A rosette (chosen by the owner).** A round prize rosette with two ribbon tails, like a village fair
  ribbon. It keeps the shield's tier colours and glints, so Bronze reads as Bronze everywhere, and it reads
  as "the Plynling's".
- **C. A leaf or paw-print frame.** Rejected: the irregular outline cramps the subject and blurs the tier at
  22 px.

## The frame (decided)

The owner picked the **first draft** of the rosette over a reworked second one:

- **Petals**: 12 shallow scalloped lobes around the medallion. The pleats alternate between the lit rim
  and the inner line on the top-left half, and between the inner line and the shaded rim on the
  bottom-right half.
- **Medallion**: a two-tone field (split on the diagonal, as on the shield) inside an inner-line ring.
- **Tails**: two near-vertical ribbon tails under the disc, with notched ends.
- **Size and placement**: 22×22. The disc is centred at (11, 10), the tails come down to row 19, and row 21
  is left empty.
- **Colours**: the existing `SHIELD_TIERS` entries for bronze → diamond, plus `moment`. The glints sit on
  the top-left pleats, one more per tier, as on the shield.

The geometry comes from the spike (scratchpad `rosette/spike.py`, v1 parameters):

| Parameter | Value |
|---|---|
| Lobes | 12 |
| `r_out` | `7.75 + 0.55·cos(12θ)` |
| Field radius | 5.3 |
| Inner-line radius | 6.3 |
| Glints | (7, 3), (4, 6), (11, 2), (3, 10) |
| Left tail spans (row: columns, mirrored for the right tail) | 14: 6–9, 15: 6–9, 16: 5–8, 17: 5–8, 18: 4–7, 19: points at 4 and 7 |

The builder ports it as `rosette(tier, *subject)` beside `shield()` in `tools/item-art/icons.py`, and should
favour a hand-tuned template grid over the formula wherever a pixel looks off.

## Subjects (decided)

Each subject is a fill of **at most 8×8** (10×10 once outlined), so a ring of the medallion stays visible
around it. The owner judged the die at that size; at 9×9 it covered the medallion.

| Badge | Key | Subject |
|---|---|---|
| Longévité | `longevity` | a small tree |
| Joueur·se | `player` | a die showing five (the spike's 8×8 grid) |
| Champion·ne | `champion` | a trophy cup |
| Voyageur·se | `traveller` | a signpost with two arrows |
| Hôte·sse | `host` | a cottage door with a heart on it |
| Gourmand·e | `gourmet` | a small mushroom cap |
| Pot de colle | `cuddly` | a glue pot with its brush |
| Chouchou·te du village | `beloved` | a gift box |
| Entraînement | `training` | a dumbbell |
| Niveau | `level` | a star |
| Athlète | `athlete` | a flexed arm |
| Sauvé·e de justesse | `saved` | a life ring |
| Revenu·e d'entre les morts | `resurrected` | a halo with a sparkle |
| Premier·ère ami·e | `first_friend` | a handshake (fallback: two linked rings, if it doesn't read) |
| Meilleur·e ami·e | `best_friend` | a yellow heart |
| En couple | `couple` | two overlapping hearts, pink and red |
| Guéri·e | `recovered` | a two-tone capsule |
| Coquet·te | `dapper` | a top hat |

Moments use the `moment` colours.

## Wiring (builder's calls, flagged for review)

- **Icon keys**:
  - `plbadge.<palier>_<tier>` for a tier, with the tier as `bronze` … `diamond`, the way `CollectionTiers.Badge` names them.
  - `plbadge.<moment key>` for a moment.
  - This is by palier and tier, not by the legacy stored keys (`week`, `games_100`…), so the files line up
    with the catalog.
- **Emoji prefix**: `pb_` in `ItemEmojis.Prefixes`. It is a new family, so nothing is retired.
  - It must not collide with the prefixes the stale-emoji sweep owns.
  - The longest name, `pb_beloved_platinum`, stays under Discord's 32 characters.
- **`ItemEmojis.IsIconKey`** accepts the new keys from the `PlynlingBadges` catalog.
- **`BadgeInfo.Icon`** shows the uploaded emoji once it exists, and falls back to today's medal + emoji, the
  pattern `CollectionTiers.Badge` uses.
- **Art tool**:
  - Add `plbadge.` to `export.py`'s 22×22 prefixes and to `check.py`'s known kinds.
  - Add two groups, `plynling_badges_paliers` and `plynling_badges_moments`, so they get their own sheets.
- **Length budget**: a custom emoji costs about 35 characters against 2–4 for today's Unicode. Measure the
  journal card with every icon uploaded against the 6000-character and component limits before shipping.

## Version and docs

- **Version**: art is a patch, so 8.26.0 → **8.26.1**, with a `CHANGELOG.md` entry.
- **Docs**:
  - `docs/agents/plynling/badges.md`: replace « Paliers have no art yet ».
  - `docs/agents/economy.md`: the icons section, covering the rosette and the `pb_` prefix.
  - `tools/item-art/README.md`: the rosette.
- **Guide**: no change, since the guide doesn't show the icons.

## Left to the builder

- The exact pixels of each subject. Judge them on the 22 px column of the sheet first, and follow the
  README's style rules: one outline colour, separations with inner `#`, shading along a hue ramp.
- Whether the handshake reads, or falls back to the linked rings.
