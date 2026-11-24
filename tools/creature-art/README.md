# Creature art

The expedition creatures' pictures for the notebook, 32×32 in the Plynlings' style, drawn by code so a
tweak is an edit. It reuses `../plynling-art/common.py` (the grid, the tones, the top-left light).

- `creatures.py`: one function per creature, plus `unknown()`, the silhouette a creature shows until its
  Aperçu. `CREATURES` lists them as (key, name, function). Today it holds the Combe aux Mousses' eight.
  The style: forms lit from the top left and dithered between tones, a selective outline in each part's
  own dark tone, the Plynlings' glossy 2×2 eyes, and their soft two-row ground shadow (none for a
  flier). Texture comes from `hashed()`, never a random draw, so an export is the same every time.
- `sheet.py [key…]` writes `out/sheet_combe.png` for review. Each picture is shown at 8 px per art pixel,
  then at 96 px (about a card's thumbnail) on Discord's dark and light backgrounds.

The Combe's sheet was approved by the owner on 2026-11-23. There's no export yet. The asset files, their names and their version come with the expedition engine.

Requires Python 3 and Pillow. Run from this folder: `python sheet.py`.
