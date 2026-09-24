# Plynling art

Placeholder pixel art for the Plynlings, drawn by code so a tweak is an edit, not a redraw.

- `common.py` — palettes, the six species, the pixel grid, the shared outline pass.
- `sprites.py` — the face and mood extras every Plynling shares, and `build()`, which hands each
  species to its own drawing.
- `species.py` — one silhouette per species, each in six moods (happy, content, sad, hungry,
  starving, frozen): the Cèpe as it always was, a fly-agaric Amanite with its skirt, a button-capped
  Rosé, a flat-capped Russule, a Mycena bell on a spindle stem, a chanterelle funnel. The face is
  clipped to each body; sweat drops and frost are measured from the model, not from fixed spots.
  `stem_tint` in `common.SPECIES` colours a stem (the Mycena's lavender, the chanterelle's gold).
- `memorials.py` — the five memorial tiers (cairn → statue), each carrying the species' accent colour.
- `source/` — the four food sprites (16×16, hand-drawn), exported as-is.
- `export.py` — renders all 70 files at 256×256 into `assets/plynlings/`.

Requires Python 3 and Pillow. Run from this folder: `python export.py`.

**Changing the art:** bump `ART_VERSION` in `export.py` **and** `PlynlingArt.Version` in the
bot, re-export, commit, push. The bot links to GitHub raw URLs on `main`, and Discord caches
images by URL — a new version must be a new filename, never an overwrite. The art is at **v2**
(the per-species shapes); the Cèpe, the memorials and the foods came out byte-identical to v1.
