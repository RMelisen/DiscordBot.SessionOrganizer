# Cosmetics — a fifth slot, « Aura »: animated effects around the Plynling

## Context and why

8.5.1 removed the sparkles that the Mycène, the Girolle and the Coprin still wore from the retired
rarity tiers: no species is drawn as rarer than another. The owner wants that look back as
something **any** player can earn, and more cosmetics in general. This design covers the aura only;
new thèmes, titres, accessoires and cadres are a later, separate batch.

**Built on 8.5.1.** Cosmetics today: four slots (thème, titre, accessoire, cadre), one screen with
three tabs (`CosmeticCards`, `CosmeticComponentHandler`), accessories drawn on the sprite as
pre-rendered files (`PlynlingArt.Sprite`, `tools/plynling-art/accessories.py`, 7 560 files).

## Goals

- A new slot **« Aura »**, worn **together with** any accessory (owner's decision).
- **Three different effects** (owner's decision — not one effect in three colours), animated on the
  existing 2-second idle loop.
- It shows everywhere the accessory already shows: the card, the cosmetics try-on, games, the
  journal, announcements — everything that goes through `PlynlingArt.SpriteOf`.
- Buying, trying on, wearing and crafting work exactly as for the other slots.

## Non-goals

- No aura on **visit pictures** (accessories aren't there either).
- No aura styles (colours/positions): each effect has one look.
- No other new cosmetics in this batch.
- No change to how pictures reach Discord (still GitHub raw URLs, no attachments).

## Anti-goals

- An aura that makes a species look rarer than another — every species gets the same three effects.
- Touching any existing filename: Discord caches by URL, and the accessory files must stay valid.
- A second art version bump for this: the new files are new names, no `ART_VERSION` change.

## Approaches considered

1. **Pre-render every combination (chosen by the owner).** The art tool already draws each loop in
   every accessory look; it also draws each look with each aura. Bot code barely changes, Discord
   caches, the Pi does no image work. Cost: files — ~6 200 per aura, ~18 600 for three (~130 MB at
   ~7 KB each), and every future accessory is multiplied by (auras + 1), every future art bump
   re-exports it all.
2. **The bot composites at send time** (an image library, attachments instead of URLs). One sprite
   layer per aura, no explosion, cheap future slots — but every place showing a Plynling switches from
   a link to an attachment, and the Pi encodes an animated WebP per view (3 s interaction limit,
   needs a cache). Rejected for now; it becomes the better investment if more slots come.
3. **Generate only the combinations in use.** Rejected: the bot can't run the Python art tool, and
   a picture would only exist after someone commits and pushes.

## What the player gets

| Key | Name (label) | Effect | Source | Rarity / price |
|---|---|---|---|---|
| `cos.aura.scintillements` | Scintillements | small twinkling stars around it — the look 8.5.1 removed | weekly rotation | rare, 2 400 |
| `cos.aura.bulles` | Bulles | soap bubbles rising and popping | weekly rotation | uncommon, 1 200 |
| `cos.aura.petales` | Pétales | petals drifting down | crafted, *Chef-d'œuvre* | recipe below |

- **Garde-robe**: a fifth slot button « Aura », same select (« Aucun » + owned auras), worn at once.
  The slot row then holds exactly 5 buttons — Discord's maximum; a sixth slot would need a select.
- **Boutique / Fabrication**: the auras appear like any other cosmetic; picking one tries it on the
  real card before paying; « Acheter et porter » / « Fabriquer et porter » / « Porter » work as usual.
- **Try-on on a sick or frozen Plynling** (owner's decision): the preview stays the real card, with
  a line under it — « -# Son aura ne se voit pas tant qu'il est malade ou gelé. » (gendered like the
  card's other lines). Re-measure the screen's component count (section 59) after adding it.
- **Card text**: the line that says what it wears (`PlynlingCardUi`, « Porte 🎩 un haut-de-forme »)
  also names the aura (wording is the builder's draft, owner reviews).
- **Inventory** (`/inventory` cosmetics field per slot) and **admin stats** (worn count) pick the
  slot up through `Enum.GetValues<CosmeticSlot>()`.

### When the aura is not drawn

Not on the **frozen** face (the ice covers it), the **sick** face (the old sparkles went out when
sick too) and the **angry** face (a visit face, no accessory pictures either). On those faces the
picture is the same as without an aura — the bot simply omits the aura segment, as it does for an
accessory on angry. Everything else — happy, content, sad, hungry, starving, sleeping, clean and
dirty, adult and baby, with or without an accessory and its style — has its aura pictures.

## How it is built (decided parts)

### Art — `tools/plynling-art/`

- A new module (e.g. `auras.py`) with the three effects, each a function of the frame (16 frames over
  2 s, like the idle loop) drawn onto an already built — and possibly already dressed — frame.
- Drawn **after** the accessory, on whatever canvas the frame is on: the 32-cell sprite, or the
  40-cell hat canvas for an adult in a hat (placed relative to the sprite at `HAT_AT`, so the aura sits
  around the body, not around the empty canvas).
- Particles stay in the margin around the body and never cover the face; the aura is not tinted by
  dirt and isn't drawn at all on the faces listed above.
- `export.py` exports, for every shape × aura face × dirt × (no accessory + every accessory look) ×
  aura, `plynling_<species>[_baby]_<face>[_dirty][_with_<accessory>[_<style>]]_aura_<aura>_v5.webp`.
  The aura segment comes **last**, so every existing filename is untouched.
- A preview sheet per effect, species by species, for the owner to review visually before the full
  export (the owner corrects in small steps — one pixel, another colour).
- `tools/plynling-art/README.md` documents the files and their count.

### Bot

- `CosmeticSlot` gets `Aura` **appended** (append-only convention). Display order in the Garde-robe
  is the builder's call (after Accessoire reads best).
- `Plynling.AuraKey` (nullable string) + an EF migration (schema-only) → **minor bump, 8.6.0**.
- `CosmeticSlots.Get/Set`, `Outfit` (field, `In`, `With`, `Without`), `CosmeticCatalog`
  (`SlotLabel` « Aura », `SlotPlural` « auras », `Label`, `ShortName`, an `Aura(...)` builder, the
  three entries), `ItemCatalog` (each cosmetic is also an item), `ItemEmojis` if they get icons.
- `PlynlingArt.Sprite` takes the aura and appends `_aura_<key>` for an aura cosmetic it knows, on a
  face that has aura pictures; an unknown key shows no aura. `SpriteOf` passes the worn (or tried)
  aura.
- Shop: `RotatingPerSlot` is 2, so with two rotating auras both are on sale every week — accepted;
  it becomes a real rotation when more auras exist.
- Crafted *Chef-d'œuvre* « Pétales » (owner's decision: **spring-only**): `col.primevere` +
  the rare `col.cerise`, both spring finds at le Communal — no summer Coquelicot, since the crafting
  check (`CraftingChecks.ExpectedForages`) simulates one season and no recipe mixes seasons. Counts
  measured against the trophy band (section 61, 60–150 forages). **No one can craft it before
  spring 2027.**
- Section 61 also requires two *Ouvrages* per slot (`CraftingChecks.Catalog`): the Aura slot is
  exempted and the recipe totals updated, rather than adding auras (builder's call).

### Checks — `tests/ProjectSYNCS.Checks`

- Extend section 62 (`AccessoryArtChecks.cs`) or add a section: every picture the bot can name for a
  worn aura (with and without accessory and style, every shape and aura face) exists on disk, and
  faces without aura pictures never get the segment.
- Section 61 measures the new recipe.

### Docs and release

- `docs/agents/economy.md` (the slot, the art rule, the file cost per accessory and per aura).
- `README.md` / `/help` only if they list the slots (no new command).
- `config.yaml` → 8.6.0, `CHANGELOG.md` entry.
- Pushing the assets to GitHub `main` is needed before testing on the dev guild — ask the owner first.

## Left to the builder

- Exact pixel design and colours of the three effects (owner reviews the sheets).
- The wording of the aura in the card line and in the changelog.
- The exact Pétales recipe counts, within the trophy band.
- Whether auras get custom icons or keep Unicode emojis (✨ 🫧 🌸).
