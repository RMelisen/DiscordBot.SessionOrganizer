# Plynling cosmetics — design

A money sink for cailloux: things a Plynling can wear, bought in a rotating shop or crafted
from collectibles. **Phase 1** (this spec) decorates the Plynling's **card and grave**; no
Plynling picture changes. **Phase 2** (not in this spec) adds a few pixel-art hats drawn on the
sprite, which will live in the Accessoire slot.

## Decisions

- **Rendering:** card decorations only in phase 1. Drawn hats are phase 2.
- **Obtaining:** most are bought with cailloux; 8 special ones are crafted from collectibles.
- **Slots:** Thème, Titre, Accessoire, Cadre de tombe — one item worn per slot.
- **Ownership:** the person owns them (inventory items); wearing does not consume; they stay
  after the Plynling dies or is abandoned, ready for the next one. A grave keeps what its
  Plynling wore. They can be given and traded, **not sold**.
- **Shop:** 4 permanent basics + a weekly rotation, the same for everyone.

## Data

- **Cosmetics are items.** `ItemKind.Cosmetic` joins Food and Collectible in `ItemCatalog`, with
  stable keys `cos.<slot>.<name>` (append-only like every stored key). They are held as ordinary
  `InventoryItem` rows, so give, trade, autocomplete and the inventory view work unchanged.
  They belong to no collection set, so the book and its « 62 objets » count are untouched.
- A cosmetic's catalog entry (`CosmeticInfo`, alongside its `ItemInfo`) carries its slot,
  rarity, availability (Basic / Rotating / Seasonal / Crafted), price or recipe, season, and
  what the slot needs:
  - **Thème:** accent colour (uint) and banner text.
  - **Titre:** masculine and feminine forms.
  - **Accessoire:** emoji and name with its article (« une écharpe »).
  - **Cadre de tombe:** left and right decoration.
- **What a Plynling wears** is four nullable key columns on `Plynling`: `ThemeKey`, `TitleKey`,
  `AccessoryKey`, `GraveKey` (migration `AddPlynlingCosmetics`). A dead Plynling's row keeps
  them, which is how its grave shows its frame.
- **Losing a worn cosmetic takes it off.** When `InventoryService.TakeAsync` brings a cosmetic to
  0 (give or trade), it clears that key from the owner's living Plynling in the same unit of
  work, so a card never shows something its owner no longer has.
- `SellAsync` refuses cosmetics (new outcome, « Les cosmétiques ne se vendent pas »).

## On screen

- **Card** (`PlynlingModule.BuildCard`): the Thème replaces the container's accent colour and
  adds one banner `TextDisplay` at the top (re-check the component budget). The Titre (in the
  Plynling's gender) and the Accessoire (« porte 🎀 un nœud ») go into the existing heading text,
  costing no component.
- **Graveyard** (`PlynlingCardUi.GraveLine`): the Cadre de tombe wraps the grave's line.
- **`/inventory view`:** a « Garde-robe » field listing owned cosmetics.
- Everything a Plynling wears is visible to anyone who opens its card.

## Commands

- **`/inventory cosmetics`** (ephemeral) — the shop: « Toujours » (the 4 basics) and
  « Cette semaine » (2 rotating per slot, plus the season's thème and accessoire when in
  season). An « Acheter… » select buys at once, cailloux and item in one save. Refused when
  already owned, when too poor, or when the select comes from a previous week
  (« Cet article n'est plus en boutique »).
- **`/inventory craft`** (ephemeral) — all 8 recipes, each ingredient shown as « 3/5 Plume ✅/❌ ».
  A « Fabriquer… » select takes the items and cailloux and grants the cosmetic in one save, or
  does nothing if anything is missing. Refused when already owned.
- **`/plynling wardrobe`** (ephemeral) — what your living Plynling wears, and 4 selects, one per
  slot, listing the cosmetics you own for it plus « Aucun ». Picking one wears it at once and
  redraws the message. Your own living Plynling only.

`/inventory` goes to 8 subcommands and `/plynling` to 14, both under Discord's 25.

Custom-ids: `cos:buy:{week}` (the shop select, week = the rotation it was drawn from),
`cos:craft` (the recipe select), `cos:wear:{slot}` (one per slot, so no two collide).

## The weekly rotation

- The week is the Paris ISO week (changes Monday 00:00 Paris), as a key `yyyyww`.
- For each slot, 2 distinct items are drawn from that slot's rotating pool with
  `new Random(seed)` seeded from the week key and the slot — deterministic, identical for
  everyone, nothing stored, unaffected by restarts.
- Seasonal items are in the shop throughout their season (`ItemCatalog.SeasonAt`), on top of
  the rotation.

## Prices

An active player earns about 200 cailloux a day.

| Kind | Price |
|---|---|
| Basic | 300 (titre, accessoire), 500 (thème, cadre) |
| Rotating commun | 600 |
| Rotating peu commun | 1 200 |
| Rotating rare | 2 400 |
| Rotating légendaire | 5 000 |
| Seasonal | 2 400 |
| Crafted | collectibles + 200–300 cailloux |

## Catalog (60)

Rarity: C commun · U peu commun · R rare · L légendaire.

### Thèmes (accent colour, banner)

- **Basic:** Prairie 🌿 (green)
- **Rotating:** Aurore 🌸 (C, pink) · Océan 🌊 (C, blue) · Braises 🔥 (C, orange) · Forêt 🌲 (C,
  dark green) · Lavande 💜 (U, purple) · Désert 🌵 (U, sand) · Arc-en-ciel 🌈 (U) · Nuit étoilée ✨
  (R, midnight blue) · Aurore boréale 🌌 (R, teal) · Royal 👑 (L, gold)
- **Seasonal:** Cerisiers 🍒 (printemps) · Plage 🏖️ (été) · Feuilles mortes 🍂 (automne) ·
  Flocons ❄️ (hiver)
- **Crafted:** Champignonnière 🍄 ← 3 Girolle + 3 Coprin chevelu + 2 Cèpe de Bordeaux + 300 ·
  Caverne aux trésors 💎 ← 3 Quartz + 2 Améthyste + 1 Opale + 300

### Titres (masculine / feminine)

- **Basic:** Le Petit / La Petite
- **Rotating:** Le Gourmet / La Gourmande (C) · Le Rêveur / La Rêveuse (C) · Le Curieux /
  La Curieuse (C) · Le Dormeur / La Dormeuse (C) · Le Mystérieux / La Mystérieuse (U) ·
  Le Farceur / La Farceuse (U) · L'Aventurier / L'Aventurière (U) · Le Champion / La Championne
  (R) · Le Sage / La Sage (R) · Le Légendaire / La Légendaire (L)
- **Crafted:** Le Chercheur de trésors / La Chercheuse de trésors ← 1 Carte au trésor +
  1 Boussole + 3 Pièce ancienne + 200 · Le Botaniste / La Botaniste ← 3 Feuille de chêne +
  3 Trèfle + 1 Trèfle à quatre feuilles + 200

### Accessoires (emoji, name with article)

- **Basic:** 🎀 un nœud
- **Rotating:** 🧣 une écharpe (C) · 🌼 une fleur (C) · 🧢 une casquette (C) · 🎈 un ballon (C) ·
  🕶️ des lunettes de soleil (U) · 🎒 un petit sac à dos (U) · 🎧 un casque audio (U) ·
  🎩 un haut-de-forme (R) · 🪄 une baguette magique (R) · 👑 une petite couronne (L)
- **Seasonal:** 🌷 une tulipe (printemps) · 🍦 une glace (été) · 🎃 une lanterne citrouille
  (automne) · 🧤 des moufles (hiver)
- **Crafted:** 🪶 une plume dorée ← 5 Plume + 1 Plume dorée + 200 · 🐚 un collier de coquillages
  ← 3 Coquillage + 3 Galet + 200

### Cadres de tombe (decoration around the grave's line)

- **Basic:** 🕯️ Bougies
- **Rotating:** 💐 Couronne de fleurs (C) · 🍂 Feuilles (C) · 🪨 Galets (C) · 🌾 Blé (C) ·
  ⭐ Étoiles (U) · 🕊️ Colombes (U) · 🦋 Papillons (U) · 🌙 Clair de lune (R) · 🏮 Lanternes (R) ·
  ✨ Aura dorée (L)
- **Crafted:** 💎 Cristaux ← 2 Cristal de givre + 3 Flocon + 200 · 🍄 Cercle de fées ←
  3 Russule dorée + 2 Trompette de la mort + 2 Lactaire indigo + 300

## Checks

- The catalog: 60 cosmetics, unique keys and names, 10 rotating per slot with 4/3/2/1 rarities,
  every recipe ingredient an existing collectible, every title with both genders.
- The rotation: the same for everyone, 2 distinct items per slot, changes at Monday 00:00 Paris
  (including across a DST change), seasonal items only in their season.
- Buying and crafting are all-or-nothing (money, items, cosmetic in one save); refused when
  owned, too poor, missing ingredients, or from a stale week.
- Wearing needs ownership; giving or trading a worn cosmetic away takes it off; a grave keeps its
  frame; cosmetics cannot be sold.
- The card stays within Discord's 40 components with a theme; the shop, recipes, wardrobe and
  inventory stay within embed and select limits (25 options).
- Titles follow the Plynling's gender on the card.

## Out of scope

- Phase 2: pixel-art hats drawn on the sprite.
- Cosmetics for dead Plynlings (a grave keeps what it wore; nothing can be changed after death).
