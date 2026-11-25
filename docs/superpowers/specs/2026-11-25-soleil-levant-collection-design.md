# Soleil-Levant: a Japanese / Asian cosmetics collection — design

Approved by the owner on 2026-11-25; the kimono was then set aside for later (see *The kimono*). Builds on the
uncommitted 8.19.0 work (Gentleman batch, six tiers, collections).

## Intent

A themed collection, like Gentleman, around Japan and East Asia: the owner asked for an éventail,
two conical hats, a katana, a kimono if it can be drawn, a kanzashi and an onigiri, the titles
Sensei and Zen, the thèmes Torii and Jardin de bambou, a koï aura and a bamboo cadre. No event
behind it: the pieces join the **weekly rotation**, priced by tier.

## Goals

- 12 new cosmetics (table below), all rotating, one collection « Soleil-Levant ».
- `/inventory view`'s wardrobe page split in two so the catalogue can grow past 129.

## Non-goals

- No seasonal or crafted piece, no Chef-d'œuvre in this batch.
- No change to tier prices, rotation sizes or crafting bands.
- No new accessory slot (still three worn at most).

## Anti-goals

- The kimono covering the face or reaching the cap (owner rejected the first test: it must start
  about one row above the arms).
- Two pieces in one message carrying the same custom-id, or any page past Discord's caps.
- A stored key renamed or reused (keys and style options are append-only).

## Approaches considered

1. **All rotating, priced by tier** (chosen): no event behind the collection, and the rotation is
   where new pieces get seen week after week.
2. *A seasonal set* (e.g. winter, for the Lunar New Year): would sit unsold three seasons out of
   four. Rejected — no event asked for it.
3. *Kimono as a second outfit slot* instead of an accessory category: needs new columns and a sixth
   slot button the Garde-robe has no room for. Rejected: a category inside the accessory slot costs
   no migration.

## The pieces

| Tier | Slot | Key | Name (F for titles) | Emoji | Category / styles |
|---|---|---|---|---|---|
| Commun | Accessoire | `cos.accessory.onigiri` | un onigiri | 🍙 | Held (sided) |
| Commun | Accessoire | `cos.accessory.sugegasa` | un sugegasa | 👒 | Hat — wide, flat straw cone, chin cord |
| Commun | Titre | `cos.title.zen` | Le Zen / La Zen | 🏷️ | |
| Peu commun | Accessoire | `cos.accessory.kanzashi` | une épingle kanzashi | 🌸 | Head (sided, like the fleur) |
| Peu commun | Accessoire | `cos.accessory.eventail` | un éventail | 🪭 | Held (sided); colours rouge / indigo / sakura |
| Peu commun | Thème | `cos.theme.jardin_bambou` | Jardin de bambou | 🎋 | bamboo green |
| Peu commun | Cadre | `cos.grave.bambous` | Bambous | 🎋 | |
| Rare | Accessoire | `cos.accessory.chapeau_chinois` | un chapeau chinois | 🎎 | Hat — taller cone, red lacquer, gold rim |
| Rare | Titre | `cos.title.sensei` | Le Sensei / La Sensei | 🏷️ | |
| Rare | Thème | `cos.theme.torii` | Torii | ⛩️ | vermilion |
| Épique | Accessoire | `cos.accessory.katana` | un katana | 🗡️ | Held (sided) |
| Épique | Aura | `cos.aura.koi` | Carpes koï | 🎏 | small koï circling; card « Entouré de carpes koï » |

Emojis are `DefaultEmoji` placeholders until the 16×16 icons exist (every non-title piece gets
one in `tools/item-art/icons.py`).

## The kimono (deferred)

**Not in this batch** (owner, 2026-11-25: « forget the kimono for now, we will go on later »). What
the test settled, for when it comes back:

- Drawn from the body itself on every frame, like the neck pieces: a robe over the stem from the
  row under the mouth (`eye + 5`) to the last full stem row, a two-row obi (one on short babies),
  lapels with a white collar from **one row above the arms** down to the robe, and wide sleeves
  hanging under both arms. Never on the mouth, never up to the cap. Takes the ice tint when frozen.
- **Category Corps clashes with Mains** (the moufles cover the sleeves): wearing one replaces the
  other, the usual « replaced » notice. Held items stay compatible. Corps is drawn first
  (`DrawRank` below the neck), so a scarf or a bow tie sits on top.
- The spike was a throwaway script outside the repo; the real drawing goes in `accessories.py`.

## The wardrobe page

`/inventory view`'s garde-robe page is at 5 818 of 6 000 with everything owned. It becomes two
pages (builder's call): **« 👗 Thèmes, titres, auras »** and **« 🎀 Accessoires, cadres »** (the
Collections line on the second), each keeping the icons-only fallback. `InventoryPage` gains a
value (code-only, never stored); one custom-id per button as today (`inv:page:{page}`).
Checks section 23 measures both pages with everything owned.

## Constraints from the project

- Accessories: drawn in `accessories.py`, `python export.py layers`; category in
  `AccessoryFit.Category` matching the family (section 71); styles in `AccessoryStyles` matching
  the art (section 62). Preview sheets go to the owner species by species before exporting.
- Aura: drawn in `auras.py`, key in `AURAS`, ~126 layer files, its `Entry` in section 64; only on
  empty pixels, around the body's rest box, like the others.
- Collection « Soleil-Levant » in `CosmeticCollections` with the 12 keys (section 61 checks it).
- Pinned counts in the checks (`Rarities`, collections, aura entries) are updated, not loosened.
- Version: extends the uncommitted 8.19.0 entry (`config.yaml` already bumped); otherwise 8.20.0.
- Docs: `economy.md` (counts, Corps category, wardrobe pages), `tools/plynling-art/README.md`,
  `guide/` where cosmetics are listed.

## Left to the builder

- Exact drawings, colours and grips, within the preview-sheet review.
- Thème accents (Torii around `0xD23C2A`, Jardin de bambou around `0x5E9E4A`).
- How the koï move (within the aura conventions: nothing sideways on the body, empty pixels only).
- Order of the new lines in `Build()`.
