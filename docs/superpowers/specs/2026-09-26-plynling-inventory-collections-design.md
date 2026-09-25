# Plynlings: the inventory and collections — design

**Date:** 2026-09-26 · **Status:** approved

An inventory per **person** (kept across all their Plynlings), shipped in two phases.

## Foundation

`InventoryItem` rows: (guild, user, item key, quantity). A row that exists with quantity 0 still
means **discovered**. Items are defined in code (`Helpers/ItemCatalog`) with **stable keys**:
foods and collectibles now, cosmetics later as a third kind.

## Phase 1 — the pantry and exchanges

- **`/plynling shop food: quantity:`** (1–20) at the normal price, **10 % off from 5** of the same
  (rounded to the nearest caillou). Money and items in one save.
- **Pantry first:** feeding uses the feeder's own stock when they have enough — **1** item for
  their own Plynling, **2** for someone else's (the « double » rule) — instead of cailloux; the
  card says so. Otherwise it pays as before.
- **`/plynling give user: item: quantity:`** — a one-way present, foods or collectibles; not to
  yourself, not to a bot.
- **`/plynling inventory`** — ephemeral: foods, collection count, cailloux.
- Item options are **autocompleted** from what you own.

## Phase 2 — collections

Four sets of 8, rarities **commun 60 % / peu commun 28 % / rare 10 % / légendaire 2 %**:

- 🪨 **Cailloux** — galet, caillou plat, silex (C) · quartz, agate (PC) · améthyste, opale (R) · météorite (L)
- 🍂 **Nature** — feuille de chêne, gland, plume, trèfle (C) · pomme de pin, coquille d'escargot (PC) · trèfle à quatre feuilles (R) · plume dorée (L)
- 💎 **Trésors** — bouton, bille, clé rouillée (C) · pièce ancienne, bague (PC) · boussole, carte au trésor (R) · couronne (L)
- ❄️ **Saisons** — only in their season (Paris; spring Mar–May, summer Jun–Aug, autumn Sep–Nov,
  winter Dec–Feb), a common and a rare each: primevère / cerise · coquelicot / coquillage ·
  châtaigne / citrouille · flocon / cristal de givre

A draw rolls a rarity, then an item of that rarity among what is findable now (a rarity with
nothing findable falls back to commun).

**Where items come from:**
- **`/plynling forage`** — your Plynling (alive, awake, not frozen) brings one thing back, once
  every **4 h** per person (stored). **1 in 5** forages bring a **food** instead (mushroom 60 %,
  shiitake 25 %, morel 10 %, truffle 5 %) into the pantry.
- **The happy gift:** half its wins bring an item instead of cailloux.
- **A game won:** 20 % chance of an item. **A good visit:** 15 % for each owner.
- **Trading and giving.**

**`/plynling collection [user]`** — the book: one field per set, each item found (with how many are
held) or « ??? », « N/8 ». Discovery is **for good** (a traded or sold item stays discovered).
**Completing a set** pays **100 / 150 / 200 / 300** cailloux (Cailloux / Nature / Trésors / Saisons),
once (`CollectionCompletion` rows).

**`/plynling trade user: give: want:`** — a public offer the other person accepts or declines,
valid **1 h**, held in memory by a short id. On accept, both items swap in **one save**, and only
if both still have them. **`/plynling sell item: quantity:`** — collectibles for **2 / 5 / 15 / 50**
cailloux by rarity.

## Checks

Catalog and keys; the shop and its discount; pantry-first feeding (1 / 2 items); giving; the draw
odds, seasons and forage food; discovery surviving trades and sales; set rewards once; trades
all-or-nothing, refused when an item is gone and after expiry; embed caps. Build `-warnaserror`.
