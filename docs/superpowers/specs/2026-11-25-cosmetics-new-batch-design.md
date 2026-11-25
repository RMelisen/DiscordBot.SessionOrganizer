# Cosmetics: new batch, second basics, Gentleman collection — design

Approved by the owner on 2026-11-25. Builds on the uncommitted 8.19.0 work (six tiers,
collections, `docs/cosmetics-balance.md`).

## Intent

The catalogue has gaps in its tiers. Épique and légendaire have 4 items each. The Aura slot has no
commun, épique or légendaire. Each slot has only one basic, so a new player has almost nothing to
choose from. The owner wants:

- new cosmetics at every tier;
- **two basics in every slot except Aura**;
- **the baguette magique removed**;
- **Grand Illusionniste turned into a gentleman collection**;
- a **tasse de café** and **lunettes** among the new items.

## Goals

- 18 new cosmetics (table below), including 4 new basics.
- The baguette retired and refunded.
- The « Grand Illusionniste » collection becomes « Gentleman », with the new pieces that fit it.

## Non-goals

- No change to tier prices, the size of the weekly rotation or the crafting bands.
- No aura basic.
- No « Hall des héros » collection (Le Champion, Le Sage, L'Immortel) in this batch.
- The other open points of `docs/cosmetics-balance.md` §6 (spring's thin seasonal shop, slow title
  rotation, basics read « commun ») are not addressed here.

## Anti-goals

- A retired key reused, or a cosmetic key renamed (keys are append-only).
- A refund that pays less than what people actually paid for the baguette.
- A shop or wardrobe screen that breaks Discord's caps (40 components per V2 message, 25 select
  options, 6 000 characters across embeds) once 4 more basics and 19 more items exist.

## Approaches considered

1. **Second basics are new items** (chosen). Four cheap, neutral additions; the rotation pool
   stays as it is.
2. *Promote an existing commun rotating item to basic* (for example Aurore, Le Câlin, Fleur,
   Feuilles). This needs no art, but it shrinks the rotation and makes something people paid 600
   for now cost 300 or 500. Rejected.
3. *Leave the baguette in the catalogue and stop selling it* (a new « not sold » state). This adds
   a state the catalogue doesn't have. The established route is a retire migration. Rejected.

## The new cosmetics

Keys follow `cos.<slot>.<name>`. Accessory categories follow `AccessoryFit.Category`.

| Tier | Slot | Key | Name (F for titles) | Emoji | Category / notes | Collection |
|---|---|---|---|---|---|---|
| Basique (500) | Thème | `cos.theme.ciel` | Ciel | 🌤️ | light sky blue | — |
| Basique (300) | Titre | `cos.title.gentil` | Le Gentil / La Gentille | 🏷️ | | — |
| Basique (300) | Accessoire | `cos.accessory.cafe` | une tasse de café | ☕ | Held (sided) | — |
| Basique (500) | Cadre | `cos.grave.paquerettes` | Pâquerettes | 🌼 | | — |
| Commun | Accessoire | `cos.accessory.lunettes_rondes` | des lunettes rondes | 👓 | Eyes (clashes with the sunglasses) | — |
| Commun | Titre | `cos.title.studieux` | Le Studieux / La Studieuse | 🏷️ | | — |
| Commun | Aura | `cos.aura.mouches` | Mouches | 🪰 | a small cloud of flies, for Le Grognon | — |
| Peu commun | Accessoire | `cos.accessory.noeud_papillon` | un nœud papillon | 🎀 | Neck | Gentleman |
| Peu commun | Titre | `cos.title.distingue` | Le Distingué / La Distinguée | 🏷️ | | Gentleman |
| Peu commun | Cadre | `cos.grave.roses` | Roses rouges | 🌹 | | Gentleman |
| Peu commun | Aura | `cos.aura.confettis` | Confettis | 🎊 | | Jour de fête |
| Rare | Accessoire | `cos.accessory.monocle` | un monocle | 🧐 | Eyes | Gentleman |
| Rare | Thème | `cos.theme.velours` | Velours | 🍷 | deep bordeaux | Gentleman |
| Épique | Accessoire | `cos.accessory.canne` | une canne à pommeau | 🦯 | Held (sided) | Gentleman |
| Épique | Aura | `cos.aura.eclairs` | Éclairs | ⚡ | | — |
| Légendaire | Titre | `cos.title.magnifique` | Le Magnifique / La Magnifique | 🏷️ | | Gentleman |
| Légendaire | Aura | `cos.aura.etoiles_filantes` | Étoiles filantes | 🌠 | | Ciel de nuit |
| Mythique | Accessoire | `cos.accessory.montre_gousset` | une montre à gousset | 🕰️ | Held (sided), Chef-d'œuvre, Puits recipe; held rather than neck so it can be worn with the nœud papillon | Gentleman |

The emojis are placeholders until the 16×16 icons are drawn; each is the item's `DefaultEmoji`.

**Ciel de nuit and Étoiles filantes.** Ciel de nuit has Scintillements as its aura, and a
collection can hold several auras, so adding Étoiles filantes is fine.

**Elision on the card.** `PlynlingCardUi` prints « Entouré de {name} » with no elision, so two of
the new auras would read « de éclairs » and « de étoiles filantes ». Make it use « d' » before a
vowel.

## The baguette magique

- Remove the `Accessory("baguette", …)` line, its `AccessoryFit.Category` and `AccessoryStyles`
  entries, and its place in the collection. As with the sac à dos, take it out of the accessory
  list in `accessories.py` and delete its layer files and icon PNG. `export.py layers` regenerates
  every listed accessory, and checks section 62 (`StylesMatchTheArt`) fails if any baguette files
  are left on disk. The drawing code can stay.
- `AccessoryFitChecks` and `PaintChecks` use the baguette as their sample held item; switch them
  to another held item.
- **Data migration `RetireMagicWand`**, copied from `RetirePumpkinGraveFrame`. It refunds
  **2 400 cailloux per copy**. It has been rare (2 400) since it was added. 8.19.0 keeps it rare
  (the owner's call, 2026-11-25), so nobody pays any other price, whatever order the two batches
  ship in. It deletes the inventory rows and clears it from **all three** accessory slots
  with their styles on every Plynling, dead or alive, and closes the gap so no slot sits empty in front of a filled one. A remembered style in `SavedAccessoryStyles` is left in place: it is never read for an item nobody can wear (builder's call). Its key
  `cos.accessory.baguette` is retired for good; add it to the retired list in `economy.md`.

## The Gentleman collection

`CosmeticCollections`: « Grand Illusionniste » becomes **« Gentleman »**, in the same place in the
list.

- Kept: Le Mystérieux, Le Fripon, Haut-de-forme.
- Removed: Baguette magique (retired) and Colombes (a magician piece; it joins no collection).
- Added: nœud papillon, Le Distingué, Roses rouges, monocle, Velours, canne à pommeau,
  Le Magnifique, montre à gousset.

That makes 11 pieces across Thème, Titre, Accessoire and Cadre: U ×4, R ×4, É ×1, L ×1, M ×1.

## Constraints from the project

- **Accessories and auras need art.** Each new accessory is drawn in
  `tools/plynling-art/accessories.py` (`python export.py layers`). Each new aura is drawn in
  `auras.py` and added to `AURAS` (about 126 layer files each), with its `Entry` in checks
  section 64. Every non-title cosmetic needs a 16×16 icon in `tools/item-art/icons.py`.
- **Every new accessory needs a category** in `AccessoryFit.Category`, matching its family in
  `accessories.py` (checks section 71 compares the two). Held items need side styles
  (`held_sides`, as the other held items have).
- **The Chef-d'œuvre recipe must land in the 60–150 forage band** (checks section 61). Its cailloux
  follow the effort rule: 200 under 90 forages, 300 for 90–115, 400 above 115.
- **Basics price by slot as they do now**: 300 for titres and accessories, 500 for thèmes and
  cadres (`PriceOf`).
- **The shop grows by 4 basics.** Re-measure the Boutique tab (checks section 59) and the
  wardrobe page (section 23) with everything owned.
- **Pinned counts in the checks change**: `Rarities`, `Bands`, `Collections` in `CraftingChecks`,
  the aura `Entry` list, `Crafted.Count() == 27` (becomes 28). They are updated to match, not
  loosened. The « seven data migrations » in `CLAUDE.md` and `architecture.md` become eight.
- **Version.** One minor bump: a migration and new user-visible items. If 8.19.0 is still
  uncommitted, this extends the same batch and its changelog entry. If 8.19.0 was committed first,
  this is 8.20.0 with its own entry. Check `git diff` for `config.yaml` at build time.
- **Docs.** Update `docs/agents/economy.md` (counts, retired key), regenerate
  `docs/cosmetics-balance.md`, and update the guide page on cosmetics (`guide/cailloux.md`) if it
  names counts or items.

## Left to the builder

- The exact recipe for the montre à gousset. It should use Puits treasures, with a rare or
  légendaire anchor (for example Pièce ancienne + Clé rouillée + Boussole), tuned to the band.
- Thème accent colours (Ciel light blue around `0x8EC5E8`, Velours bordeaux around `0x7A1F3D`).
- Accessory styles beyond held sides (a colour for the nœud papillon or the lunettes rondes is
  optional).
- How the four auras move and look, within the existing aura conventions.
- The order of the new lines inside `Build()`. Adding rotating items reshuffles that slot's weekly draw from the deploy on, whatever the order; a buy from a shop left open is still refused if the item is no longer on sale (`CosmeticService` checks `Shop(now)`).
