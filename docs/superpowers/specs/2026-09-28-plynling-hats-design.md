# Plynling hats — design

Phase 2 of the cosmetics (`2026-09-27-plynling-cosmetics-design.md`): four accessories are drawn
**on the Plynling's sprite**, not only named on its card. Nothing about owning, buying or
wearing a cosmetic changes — a hat is simply an accessory that also has a picture.

## Decisions

- **Four hats:** 🎀 nœud (`cos.accessory.noeud`), 🎩 haut-de-forme (`haut_de_forme`), 👑 petite
  couronne (`couronne`), 🧢 casquette (`casquette`). The other thirteen accessories stay text-only.
- **Rollout in two steps:** the nœud first, pipeline and all; the other three once it is judged
  on screen. Each later hat is only its drawing plus its key on the cosmetic.
- **Overlay, not headroom:** the hat is drawn over the top rows of the cap. The canvas stays
  32×32 and the Plynling keeps its size; the haut-de-forme is designed short and squat to suit.
- **Shown on the card and in visits** — everywhere a living Plynling is pictured. Not on
  memorials or graves.
- **Every mood wears it**, frozen and sleeping included.
- **Nothing moves side to side** (standing rule): the casquette faces the viewer, brim towards
  us, and every hat is rigid on the cap, following it only up and down.

## The art (`tools/plynling-art`)

- **`hats.py`** holds the four hats as small hand-drawn pixel grids, each in two sizes: one for
  adults, a smaller one for babies (whose caps are smaller and sit lower).
- **The hat is stamped onto the finished frame** after the species has drawn it, with its pose.
- **Placement is measured, not hand-set**, the way `sprites.cap_corner` places the anger mark:
  on every frame, find the top of the head (the highest solid pixels of the body, measured
  before the mood extras — heart, z's, sparkles, frost — and never counting the soft shadow or
  the Mycena's halo) and centre the hat on it. It therefore follows the breath, the hop, the
  Cèpe's widening cap and the Rosé's puff with no per-frame work.
- **Per-species nudges** where the measured spot reads wrong: an optional `hat_nudge` in
  `common.SPECIES`, like `anger_nudge`.
- **`build(..., hat=)`** takes an optional hat key; `None` draws exactly what it draws today.
- **`export.py`** gains `HATS` (the hat keys it exports) and writes, per hat, per species, per
  stage in `STAGED`:
  - the card loop for each of the 8 `STATES`: `plynling_<sp>[_baby]_<state>_<hat>_v4.webp`
    (angry is a visit face only, but it is exported like the bare angry file, to keep one loop)
  - the visit loop for each of the 4 `VISIT_STATES`: `plynling_<sp>[_baby]_<state>_<hat>_visit_v4.webp`

  That is 154 files per hat (98 card + 56 visit). New filenames only, so **no `ART_VERSION`
  bump**, and every existing file re-exports byte-identical.
- **A contact sheet first:** `python hats.py` renders every species, both stages and a few moods
  wearing each hat, to be judged by eye before the full export.

## The bot

- **`CosmeticInfo` gets `string? Hat`** — the art key, set on the four accessories above (the
  same text as the key's suffix: `noeud`, `haut_de_forme`, `couronne`, `casquette`) and `null`
  everywhere else. Stored item keys do not change; no migration.
- **`PlynlingArt`**:
  - `HatKeys` — the hats that have art. Must equal `HATS` in `export.py`.
  - `Sprite(species, stage, mood, hat = null)` and `VisitSprite(species, stage, mood, hat = null)`
    insert `_{hat}` before `_v{Version}` / `_visit_v{Version}` when `hat` is set.
  - **`SpriteOf(Plynling p, DateTimeOffset now, PlynlingMood? mood = null)`** — the one way to
    get a living Plynling's picture: its species, stage, mood (or the one given) and the `Hat` of
    the accessory it wears (`CosmeticSlots.Worn(p, Accessory)?.Hat`). It replaces the six copies
    of `Sprite(p.Species, Stage(p, now), Mood(p, now))`: `PlynlingModule` (the card),
    `PlynlingPlayCards` (twice), `PlynlingJournalCards` and `PlynlingAnnouncer` (resurrection,
    and abandonment with `mood: Sad`).
- **Visits:** `VisitCast` gets `string? Hat`, captured with the rest of the snapshot, and
  `VisitCast.Sprite` passes it on — paging back through an old story shows what it wore then.
- **The card heading keeps « porte 🎀 un nœud »**: it names the item, and covers a client that
  shows no picture.
- **The shop and the wardrobe** add « ✨ se voit sur ton Plynling » to the description of an
  accessory that has a `Hat`, so people know which ones change the picture.

## Checks

**The failure that matters is a broken picture** — a cosmetic with a `Hat` whose files were
never exported or pushed shows an empty thumbnail. The scratch artcheck harness asserts:

- every `CosmeticInfo.Hat` is in `PlynlingArt.HatKeys`;
- `HatKeys` equals `HATS` in `export.py`;
- every filename the bot can build with a hat (species × stage × mood, card and visit) exists in
  `assets/plynlings/`.

**In the art tool**, on every frame of every hatted file:

- the hat lies inside the canvas and is never clipped (same pixel count on every frame);
- it stays above the face — above the brows (row 16) for adults, three rows lower for babies;
- its horizontal position is the same on all 16 frames (nothing side to side).

**No regressions:** every existing file re-exports byte-identical, and the count `export.py`
prints rises by exactly 154 per hat.

## Docs

- `tools/plynling-art/README.md`: a hats paragraph (the files, the measured placement, the nudge,
  `HATS` ↔ `HatKeys`).
- `CLAUDE.md`: a short note — a hat is a property of the cosmetic, not stored per Plynling;
  `HatKeys` must match `HATS`; `PlynlingArt.SpriteOf` is the only way to picture a living
  Plynling.
- The cosmetics spec's phase-2 line points here.

## Out of scope

- Hats on memorials and graves.
- Drawing any of the other thirteen accessories.
- Any side-to-side motion.
