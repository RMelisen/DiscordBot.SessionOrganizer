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
- `motion.py` — the idle loop every Plynling shares: 16 frames of 125 ms (2 s). `pose(sp, state, f)`
  says where everything sits on frame f: the breath, the blink, each mood's touch (hop, tear,
  sweat, shudder, frost star) and each species' own (the Cèpe's cap widens, the Amanite's skirt
  billows and drapes, the Rosé's cap puffs, the Russule's plate tips, the Mycena's glow pulses,
  the Doré's rim ripples). **Nothing moves side to side** — every motion is up and down, or in
  place; the side-to-side versions were tried and rejected. Frame 0 is always the rest pose.
- **Life stages:** `sprites.build(..., stage=)` takes `"baby"`, `"teen"`, `"adult"` or `"elder"`,
  but only the baby has its own art (ado and ancien were prototyped and dropped: they wear the
  adult). Only species in `STAGED` (`export.py`) draw a baby — today all six mushrooms, each
  through its own hand-drawn `<species>_baby` in `species.py`: a smaller cap of its own shape
  over the shared baby frame (body rows 20–28 with no feet, cap ending at row 18, face 3 rows
  low). Shrinking the adult drawing instead was tried and read too little like a baby. A new
  species needs its own baby function before it joins `STAGED`, which must match
  `PlynlingArt.StagedSpecies`. The adult file
  keeps its stage-less name (`plynling_cepe_happy_v3.webp`); the baby inserts the stage
  (`plynling_cepe_baby_happy_v3.webp`). Adding a species' baby adds files and changes none, so
  it needs no `ART_VERSION` bump.
- `memorials.py` — the five memorial tiers (cairn → statue), each carrying the species' accent colour.
- `source/` — the four food sprites (16×16, hand-drawn), exported as-is.
- `export.py` — renders all 106 files at 256×256 into `assets/plynlings/`: the 72 living sprites as
  looping, lossless animated WebP (so the soft shadow and the Mycena's halo keep their partial
  transparency), the memorials and foods as PNG. Pillow merges identical consecutive frames into
  one longer frame, so a file holds fewer than 16 frames while still lasting 2 s.

Requires Python 3 and Pillow. Run from this folder: `python export.py`.

**Changing the art:** bump `ART_VERSION` in `export.py` **and** `PlynlingArt.Version` in the
bot, re-export, commit, push. The bot links to GitHub raw URLs on `main`, and Discord caches
images by URL — a new version must be a new filename, never an overwrite. The art is at **v3**
(the idle animation). Frame 0 of every loop is pixel-identical to the v2 still, so a client that
cannot animate WebP shows exactly the old picture, and the memorials and foods are byte-identical
to v2. A new species' drawing function must take `frame` and route through `finish` with its
pose, or it will not move.
