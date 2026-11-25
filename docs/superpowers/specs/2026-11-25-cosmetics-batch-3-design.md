# Cosmetics batch 3: seven accessories, two auras — design

The owner asked for more accessories and one or two auras, left the list to Claude, and trimmed
the proposal on 2026-11-25. Builds on 8.20.0 (Soleil-Levant).

## Intent

More variety in the weekly shop, aimed at slots and collections that had few pieces or none
(Tout doux and Au fil de la Sommeille had no aura; Cœur de la Lisière, Chasse au trésor and Ciel de
nuit no accessory).

## The pieces

All rotating, priced by tier, no event behind them.

| Tier | Slot | Key | Name | Emoji | Category | Collection |
|---|---|---|---|---|---|---|
| Commun | Accessoire | `cos.accessory.bonnet_nuit` | un bonnet de nuit | 🌙 | Hat | Ciel de nuit |
| Commun | Accessoire | `cos.accessory.sucette` | une sucette | 🍭 | Held (sided) | Jour de fête |
| Peu commun | Accessoire | `cos.accessory.chapeau_champignon` | un chapeau-champignon | 🍄 | Hat | Cœur de la Lisière |
| Peu commun | Accessoire | `cos.accessory.doudou` | un petit doudou | 🧸 | Held (sided) | Tout doux |
| Peu commun | Accessoire | `cos.accessory.lunettes_coeur` | des lunettes en cœur | 💕 | Eyes | Tout doux |
| Rare | Accessoire | `cos.accessory.cache_oeil` | un cache-œil de pirate | 🏴‍☠️ | Eyes | Chasse au trésor |
| Légendaire | Accessoire | `cos.accessory.aureole` | une auréole | 😇 | Hat | — |
| Peu commun | Aura | `cos.aura.coeurs` | Petits cœurs | 💕 | — | Tout doux |
| Peu commun | Aura | `cos.aura.averse` | Gouttes de pluie (was « Averse »: « Entouré d'averse » read wrong) | 🌧️ | — | Au fil de la Sommeille |

Emojis are `DefaultEmoji` placeholders; every piece gets a 16×16 icon in `tools/item-art/icons.py`.

## Goals and non-goals

- Goals: the nine pieces above, drawn, priced, in their collections, with icons.
- Non-goals: no new accessory category (no mouth slot), no new slot, no change to prices,
  rotation sizes or crafting, no migration.
- Anti-goals: a hat covering the face; an aura drawing over the body; a stored key reused or renamed.

## Approaches

1. **All rotating, spread over existing collections** (chosen): fills the gaps the owner's trim
   aimed at, costs no migration.
2. *A new themed collection* like Soleil-Levant: the trimmed list has no common theme.
3. *Auréole as its own category* so it stacks with hats: it floats where a hat sits; a new category
   is more code for one piece. Rejected — it is a Hat and clashes with hats.

## Constraints from the project

- Accessories: drawn in `accessories.py`, categories in `AccessoryFit.Category` (section 71), held
  items get `held_sides` and side styles in `AccessoryStyles` (section 62). Preview sheets go to the
  owner species by species before `python export.py layers`.
- Auras: drawn in `auras.py`, key in `AURAS`, only on empty pixels around the rest box; `Entry` in
  section 64; aura counts in the checks go 11 → 13. The card line « Entouré de petits cœurs » /
  « Entouré d'averse » — the builder checks the averse reads well and adjusts the name if not.
- Re-measure the Boutique tab (section 59) and both wardrobe pages (section 23) with everything owned.
- Version: one minor bump, **8.21.0**, with a changelog entry. Docs: `economy.md` counts,
  `tools/plynling-art/README.md` counts, `guide/cailloux.md` and `README.md` if they name counts.

## Left to the builder

- The look of each piece within the families (colours, whether the bonnet de nuit and the doudou
  get colour styles; default is none).
- How the auréole glows (a faint pulse is fine, nothing that hides the cap).
- How the averse moves (a small cloud above the cap with drops falling at the sides, never on the body).
