# Plynling art

Placeholder pixel art for the Plynlings, drawn by code so a tweak is an edit, not a redraw.

- `common.py` — palettes, the six species, the pixel grid, the shared outline pass.
- `sprites.py` — the face and mood extras every Plynling shares, and `build()`, which hands each
  species to its own drawing.
- `species.py` — one silhouette per species, each in six moods (happy, content, sad, hungry,
  starving, frozen): the Cèpe as it always was, a fly-agaric Amanite with its skirt, a button-capped
  Rosé, a flat-capped Russule, a Mycena bell on a spindle stem, a chanterelle funnel, and a Coprin
  (shaggy ink cap): a shaded, flaring bell whose rim melts into ink through pink and grey — the
  hungrier it is, the more (`COPRIN_INK`) — with a falling ink drop as its idle touch. The face is
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
  adult). Only species in `STAGED` (`export.py`) draw a baby — today all seven mushrooms, each
  through its own hand-drawn `<species>_baby` in `species.py`: a smaller cap of its own shape
  over the shared baby frame (body rows 20–28 with no feet, cap ending at row 18, face 3 rows
  low). Shrinking the adult drawing instead was tried and read too little like a baby. A new
  species needs its own baby function before it joins `STAGED`, which must match
  `PlynlingArt.StagedSpecies`. The adult file
  keeps its stage-less name (`plynling_cepe_happy_v3.webp`); the baby inserts the stage
  (`plynling_cepe_baby_happy_v3.webp`). Adding a species' baby adds files and changes none, so
  it needs no `ART_VERSION` bump.
- **The angry face** (`angry`) is a visit face only — the bot never shows it on a Plynling's own
  card. Brows in a V, narrowed eyes, a tight frown, flushed cheeks, and a red 💢 that pulses in
  place, with two short vertical stomps per loop. The 💢 is placed from the drawing itself
  (`sprites.cap_corner`: the solid pixel furthest up and to the right), so it sits on the cap's
  top-right shoulder for every species and every stage, and follows the head on every frame; a
  narrow round cap puts that corner near the top, so the Mycena and the Coprin nudge it with
  `anger_nudge` in `common.SPECIES`. Its 14 files (7 adults, 7 babies) were added without a
  version bump — they are new filenames, and every existing file re-exports byte-identical.
- **The sick face** (`sick`) is a card face only: heavy lids with bags, a queasy wavy mouth, a
  greenish flush instead of the pink, a green tint over the whole Plynling, a sliding fever sweat
  drop and a fever shiver (the starving shudder: in place, never sideways). 14 files, added without
  a version bump.
- **The dirt** (`dirt.py`, `build(..., dirty=True)`, `plynling_<species>[_baby]_<face>_dirty_v5.webp`,
  98 files) marks a « sale » Plynling on its card, for the living faces in `DIRTY_STATES` — not
  frozen (the ice covers it), not angry and not the visit pictures. A brownish grime tint, mud on the
  cap, a streak and splashes on the body — painted on the model *before* the face, so the face is
  always on top — three stink wisps rising beside the left flank, and a fly. The fly's spot is
  searched for once per animation on its rest pose, with room for the breath, the hop and the
  Russule's tipping cap, and outside the starving cold sweat; it is drawn before the wisps so they go
  round it. Everything is placed from the drawing, so it fits every species and stage. Added
  without a version bump.
- **Visit pictures** (`plynling_<species>[_baby]_<face>_visit_v5.webp`, 56 files: happy, content, sad
  and angry, adults and babies) are the same animation on a larger transparent canvas
  (`VISIT_CANVAS = 40` cells against the sprite's 32, so the sprite fills 80 % of its tile; still
  whole-pixel scaling, so the pixel art stays sharp). A visit shows two Plynlings side by side in a
  gallery and Discord stretches each to half the message width, which can't be shrunk from the
  bot — so the sprite is shrunk inside its picture. Change `VISIT_CANVAS` and re-export to make
  them smaller or larger. Added without a version bump, like the angry faces.
- `accessories.py` — the 32 cosmetic accessories, drawn on the Plynling's own picture. Each has a
  family that says where it sits — a hat on the cap (couronne, haut-de-forme, casquette, chapeau de paille, bonnet, couronne de fleurs, chapeau chinois, and the sugegasa, drawn by code and sized from each cap), on the cap
  (nœud, fleur, kanzashi), on the eyes, at the neck (écharpe, collier, médaillon), on the hands, or held in a
  hand, the left by default (the heart, the sweat and the « z » are on the right) — and is placed from the drawing
  itself on every frame (the cap's real top, the first stem row under it, the arms), so one drawing
  fits every species and stage and follows the breath and the hop. Some come in **styles** the player
  picks: colours (nœud, fleur, écharpe, casquette, ballon, moufles, bonnet) and a place at the neck (haut,
  bas); the first of each is the default, and the names must match `AccessoryStyles` in the bot. **Sides:** nœud, fleur and every held item also carry a side as the last style part (`rose_gauche`, `blanche_droite`, `rouge_gauche` for the ballon, a bare `gauche`/`droite` for the other held items); the default side comes first (nœud and held items gauche, fleur droite) and draws exactly as before. The other side mirrors the drawing (`on_cap` flips `at` and the centre; `held` stamps mirrored with its grip just outside `hand_r`). `file_style` names a style without its default side (`noeud rose`), only the other side shows (`noeud rose_droite`). `python accessories.py <outdir>` also writes `accessories_sides.png` (every switched side on all 14 shapes, content and happy). An
  adult's cap reaches the top of its picture, so an adult in a hat is drawn on a 40 px canvas
  (`HAT_CANVAS`, the Plynling at 80 %); `HAT_SETTLE` sinks the hat a row into some caps. The accessory
  stays clean on a muddy Plynling and takes the ice tint on a frozen one. Every placement and colour
  was approved by the owner species by species — redraw through the preview sheets
  (`python accessories.py <outdir>`) and show them before exporting.
- `auras.py` — the 11 cosmetic auras, animated around the Plynling over the 16-frame loop (the
  later five: Mouches, Confettis, Éclairs, Étoiles filantes, and Carpes koï — three small side-view
  koï drifting round flat ovals beside the stem, a faint ripple trailing; the first six:
  Scintillements (twinkling stars, the look the rarer species lost in v5), Bulles (bubbles rising and
  popping, two of three on the left, away from the mood marks), Pétales (petals falling along the
  sides), and the seasonal three: Lucioles (fireflies wandering in little figures of eight, glowing
  and going out), Feuilles mortes (orange, red and gold leaves swinging wide as they fall, flipping
  edge-on) and Flocons (snowflakes falling at the sides, the big ones turning)). They exist only as
  layers. Drawn last, over the plain or dressed frame, **only on empty pixels** (or the faint ground
  shadow), so a particle never covers the body, the face or an accessory; placed from the body's box at
  rest, so it fits every shape and doesn't breathe with it; around the body on the hat canvas too. Only on
  `AURA_STATES` (happy, content, sad, hungry, starving, sleeping) — must match `AuraMoods` in the bot.
  Approved by the owner on the preview sheets (`python auras.py <outdir>`: faces, accessories, and an
  animated WebP per aura) — redraw through them.
- `memorials.py` — the five memorial tiers (cairn → statue), each carrying the species' accent colour.
- `source/` — the four food sprites (16×16, hand-drawn), exported as-is.
- `export.py` — renders all 319 files into `assets/plynlings/` (256×256, and 320×320 for the 56 `_visit` ones): the 224 living sprites as
  looping, lossless animated WebP (so the soft shadow and the Mycena's halo keep their partial
  transparency), the memorials and foods as PNG. Pillow merges identical consecutive frames into
  one longer frame, so a file holds fewer than 16 frames while still lasting 2 s. Dressed pictures
  are not exported: the bot draws them from the layers (below). The pre-rendered
  `_with_`/`_aura_` files were retired on 2026-11-25.

Requires Python 3 and Pillow. Run from this folder: `python export.py` (everything, after deleting the
old files) or `python export.py layers` (the layers only).

**Changing the art:** bump `ART_VERSION` in `export.py` **and** `PlynlingArt.Version` in the
bot, re-export, commit, push. The bot links to GitHub raw URLs on `main`, and Discord caches
images by URL — a new version must be a new filename, never an overwrite. The art is at **v5**
(v3 the idle animation; v5 dropped the sparkles the Mycena, the Doré (Girolle) and the Coprin wore from the
retired rarity tiers — no species is drawn as rarer than another). Frame 0 of every loop is the
still, so a client that cannot animate WebP shows a picture that makes sense. A new species'
drawing function must take `frame` and route through `finish` with its pose, or it will not move.

**Redrawing one species** without renaming everything: add it to `REDRAWN` in `export.py` **and**
`PlynlingArt.Redrawn` in the bot with the next version (the Coprin is at 6: its idle drop turned black),
then `python export.py redraw <species>` and `python export.py layers`. Its bare and visit pictures get
new names (the bot links those); its old bare files stay for posted messages. A full `python export.py` deletes the superseded bare files.

## Layers for runtime drawing

`python export.py layers` writes `ProjectSYNCS/Assets/Plynlings/` (deleted first; 10 752 files, about 9.5 MB), the pictures the bot lays over each other instead of reading a pre-dressed file: `base/<shape>_<face>[_dirty]`, `acc/<shape>_<face>_<acc>[_<full style>]` (clean faces only: dirty shares the clean layer), `aura/<shape>_<face>_<aura>[_tall]` (raw, unmasked particles; `_tall` is the 320 px hat canvas, adults only) and `marks/<shape>_<face>` (heart, sweat, « z », frost; no file for content and sad). Every file is an 8x lossless loop on the shared 125 ms timing, its layers sitting at the foot of the largest canvas in play. `sprites.marks()` draws the mood extras alone.
