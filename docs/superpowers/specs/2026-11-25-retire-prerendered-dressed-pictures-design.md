# Retire the pre-rendered dressed pictures

## Why

Since 8.9.0 the bot draws every dressed Plynling at runtime from the layers in
`ProjectSYNCS/Assets/Plynlings/`. The ~30 000 pre-rendered `_with_` / `_aura_` files in
`assets/plynlings/` (~208 MB) are no longer linked by the bot, yet every new accessory and every
species redraw still regenerates them. The owner wants the clutter and the upkeep gone.

## Goals

- Delete every `*_with_*` and `*_aura_*` file from `assets/plynlings/` on `main`.
- `tools/plynling-art/export.py` never writes them again.
- Docs, comments and checks stop describing or depending on them.

## Non-goals and anti-goals

- **No history rewrite**: the blobs stay in git history; nothing is force-pushed (owner's call).
- The bare pictures, `_visit` pictures, memorials, foods and the old Coprin v5 files stay untouched:
  the bot links some of them, posted messages link the rest.
- No change to what the bot draws or sends.

## Approaches

1. **Keep a frozen sample** (~200 files, ~2 MB) under `tests/ProjectSYNCS.Checks/paint-refs/`, with an
   `export.py refs` mode redrawing them in place; section 70 keeps a pixel guard on the painter.
2. **Drop the pixel comparison** — **picked by the owner.** The layers were proven 0-difference
   against these files when they shipped; sections 62/64 keep checking every layer exists; the rest of
   section 70 stays. Least code, no upkeep; a future painter change is checked by eye on the sheets.
3. Python-drawn full-outfit references — rejected: a second implementation of the wearing rules.

## The design

- **Files:** `git rm` every `assets/plynlings/*_with_*` and `*_aura_*` (the `_aura_` ones without an
  accessory included). Posted messages from ~23–24 Nov that linked them may lose their image once
  Discord's cache expires — accepted.
- **`export.py`:** remove `export_accessories`, `export_auras`, the `auras` and `accessories <name>`
  modes, the dressed half of `redraw <species>` (and `ONLY_EXISTING` if nothing else needs it); the
  plain run writes bare pictures, memorials and foods only. Module docstring rewritten to match.
  `wear`, `loop`, `looks`, `file_style` stay (layers and preview sheets use them).
- **`auras.py`:** remove `PRERENDERED_AURAS` and its import.
- **Checks section 70 (`PaintChecks`):** remove `SinglesMatchToday`, `PrerenderedAuras` / `Auras()`,
  the `Sample` and `AllShapes` if unused, and the `-- art` sweep; keep `LookNamesRoundTrip`, the marks
  checks, `MissingArtIsNull`, `CacheHits`, `VisitPictures` (compares against the bare `_visit` files,
  which stay) and `FilesForFindsPictures`. Header comment rewritten.
- **Comments in the bot:** `PlynlingArt.cs` (the « old `_with_` files stay » note) and
  `CosmeticCatalog.cs` (`PRERENDERED_AURAS` mention).
- **Docs:** `docs/agents/plynling.md` *Art* (the two historical items on pre-rendered files collapse
  into one line: retired on this date, history only), `docs/agents/economy.md` (new aura / new
  accessory cost: layers only), `tools/plynling-art/README.md`, and `CLAUDE.md` (drop the `-- art`
  mention). Old specs under `docs/superpowers/` stay as written.

## Calls made by the builder (owner may correct)

- **No version bump**: the bot's only edits are two comments; the image and its behaviour don't
  change, so Home Assistant has nothing to offer.
- Nothing is committed: the owner commits by hand.

## Verification

`dotnet build`, `dotnet run --project tests/ProjectSYNCS.Checks` passes, `git grep` finds no
`_with_` / `_aura_` / `PRERENDERED` outside history docs, and `assets/plynlings/` holds only bare,
visit, memorial and food files.
