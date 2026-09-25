# Plynlings: relationships — design

**Date:** 2026-09-26 · **Status:** approved

Tomodachi-like relationships that grow out of visits.

## 1. Affinity and compatibility

One `PlynlingRelation` row per pair that has met (lower id first): **affinity** −100…+100
(starts at 0), the current **bond**, how many times they met, since when. Each pair also has a
hidden **compatibility** −15…+15, derived from the two ids (nothing stored).

Every **accepted visit** plays a scene: **good** (+10…+15 affinity) or a **squabble**
(−12…−18). Good odds: 75 % + compatibility (±15 points), −5 points between rivals, −15 between
enemies, kept within 10–95 %. The scene is a line on the visit card.

## 2. Bonds

| Affinity | Bond |
|---|---|
| ≤ −60 | 😠 Ennemis |
| −59 … −20 | ⚡ Rivaux |
| −19 … +19 | 🙂 Connaissances |
| +20 … +59 | 🤝 Amis |
| ≥ +60 | 💛 Meilleurs amis |
| by confession | 💞 Amoureux |

A change of bond is announced on the visit card. `PlynlingBond` is stored as an int:
append-only.

## 3. Romance — a boy and a girl only

After a **good** scene between **best friends**, a **boy and a girl**, affinity **≥ 80**,
**neither in a couple**: 25 % chance of a confession. **Yes** (50 % + compatibility) → 💞
amoureux. **No** → heartbreak: affinity −40, both lose 20 % happiness. A couple whose affinity
falls **below +40** breaks up (its bond becomes whatever the affinity says). One partner at a time.

## 4. Effects

- **Visit happiness by bond** (was a flat +20 %): acquaintances +20 %, friends +25 %, best
  friends +30 %, lovers +40 %, rivals +10 %, enemies **−10 %** for both.
- **Grief:** when a Plynling dies or is abandoned, its best friends and partner fall to **20 %**
  happiness at most, with a journal moment.
- **Badges** (19 in all): 🤝 Premier ami (10), 💛 Meilleur ami (20), 💞 En couple (30).
- **Moments:** new friend, best friend, in love, heartbreak, break-up, rivals, enemies, grieving.

## 5. Where it shows

The visit card (scene, changes, confession); the journal (a « Relations » line: partner, best
friends); **`/plynling relations [user]`** — every Plynling it knows, closest first (an embed).

## Checks

Scene odds and compatibility, every threshold and change, the confession rules (boy and girl,
one partner, heartbreak, break-up), grief, visit happiness by bond, the badges, lines in both
genders. Build `-warnaserror`; docs; version.
