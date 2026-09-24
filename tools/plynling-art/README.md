# Plynling art

Placeholder pixel art for the Plynlings, drawn by code so a tweak is an edit, not a redraw.

- `common.py` — palettes, the six species, the pixel grid, the shared outline pass.
- `sprites.py` — the living Plynling in six moods (happy, content, sad, hungry, starving, frozen).
- `memorials.py` — the five memorial tiers (cairn → statue), each carrying the species' accent colour.
- `source/` — the four food sprites (16×16, hand-drawn), exported as-is.
- `export.py` — renders all 70 files at 256×256 into `assets/plynlings/`.

Requires Python 3 and Pillow. Run from this folder: `python export.py`.

**Changing the art:** bump `ART_VERSION` in `export.py` **and** `PlynlingArt.Version` in the
bot, re-export, commit, push. The bot links to GitHub raw URLs on `main`, and Discord caches
images by URL — a new version must be a new filename, never an overwrite.
