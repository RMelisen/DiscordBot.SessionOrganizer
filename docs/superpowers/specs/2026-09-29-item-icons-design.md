# Item icons — design

Every collectible, collection set and cosmetic gets a small pixel-art icon in the style of the
Champignons, shown through the bot's own application emojis exactly as the mushrooms are today.
**105 icons:** the 40 collectibles of the five other sets, the 6 sets, and the 59 cosmetics.

## Decisions

- **Scope:** Cailloux, Nature, Trésors, Saisons, Insectes (8 each), one icon per set (including
  Champignons), and every cosmetic — 17 thèmes, 13 titres, 16 accessoires, 13 cadres.
- **Every titre gets its own object** (Gourmet a spoon, Rêveur a cloud, Dormeur a nightcap, Sage an
  owl, Botaniste a sprout…), replacing the shared 🏷️.
- **The card's banner and the grave's frame use the icons too**, so what is bought is what is shown.
- **Hand-drawn, original art** (no third-party pack): 16×16 text grids in Python, like the Plynling
  sprites. A single icon may be replaced by a hand-edited PNG as an escape hatch.
- **Cailloux first, as a pilot**, judged on its sheet before anything else is drawn or any bot code
  changes.
- **Out of scope:** the Plynling sprites and their hats (their own plan,
  `2026-09-28-plynling-hats-design.md`), the four foods (they already share their twin's mushroom
  picture), badges, and the 🍄 in the « Nourrir… » placeholder.

## The art (`tools/item-art/`)

Beside `tools/plynling-art/`, same conventions (Python 3 + Pillow).

- **`palette.py`** — **Pear36**, the palette the mushroom pack is drawn in, plus the pack's eight
  extra colours (`#4b3837 #7d5d5c #a27c6e #cfa385 #193a66 #316196 #79a5d3 #732c47`). One character
  per colour. The pack itself uses 23 of these and no green or purple; Pear36 supplies both.
- **`icons.py`** — every icon as 16 strings of 16 characters, keyed by its icon key (below). `.` is
  transparent.
- **`export.py`** — writes `ProjectSYNCS/Assets/Icons/<key>.png` at 128×128 (×8, nearest
  neighbour), and a sheet per group. It refuses a grid that is not 16×16 or a character not in the
  palette. It does not know the bot's catalog: that every file names a real key is the C# check's
  job (below), so the key list lives in one place. A key with a hand-edited
  `tools/item-art/overrides/<key>.png` (16×16) exports that file instead of its grid, through the
  same checks.
- **The sheet** shows each icon at 128 px, and at **22 px** — the size Discord draws any inline
  emoji, custom or Unicode — beside the Unicode emoji it replaces and beside a mushroom, on Discord's
  dark background (`#313338`). Icons are judged at the size they are seen.

**Style, taken from the mushrooms:**

- A 1-pixel `#272736` outline all round; nothing touches the frame's edge.
- Light from the top left: one highlight, one or two shadow tones, usually 4–7 colours.
- The object fills the frame the way a mushroom does (about 14×14 inside the 16×16) — no scenery, no
  background. If the pilot reads heavier than the Unicode beside it at 22 px, icons get a 1–2 px
  transparent margin instead; the mushrooms stay the reference so both families match.
- Only fully opaque or fully transparent pixels.
- **Rarity shows in the drawing**, not in a badge: plain materials for common items, the palette's
  glints (`#ffe478`, `#ffffeb`) for legendary ones, as on the Oronge. No borders.
- Titres are pictured by an object; thèmes by the motif their banner repeats (the thème's accent
  colour leads its icon); cadres by what surrounds the grave.

## Files and names

- The mushrooms stay in `Assets/Mushrooms/` with their `CREDITS.txt`, untouched, and keep their
  `shroom_<slug>` emojis — nothing already uploaded is renamed or uploaded twice.
- The new icons live in **`ProjectSYNCS/Assets/Icons/<key>.png`**. The csproj already copies
  `Assets/**/*.png` to the output and publish folders.
- **Icon keys** are item keys for items (`col.galet`, `cos.accessory.noeud`) and
  **`set.<set key>`** for sets (`set.cailloux` … `set.champignons`), sets not being items.
- **Emoji names** come from one function, `ItemEmojis.EmojiName(key)`, with a short prefix per kind
  (Discord allows `[A-Za-z0-9_]`, 2–32 characters):

  | Key prefix | Emoji prefix | Example |
  |---|---|---|
  | `col.` | `c_` | `c_galet` |
  | `set.` | `s_` | `s_cailloux` |
  | `cos.theme.` | `th_` | `th_prairie` |
  | `cos.title.` | `ti_` | `ti_gourmet` |
  | `cos.accessory.` | `ac_` | `ac_collier_coquillages` (22, the longest) |
  | `cos.grave.` | `gr_` | `gr_bougies` |

  Mushroom sprites keep `ItemEmojis.MushroomPrefix` (`shroom_`).

## Upload (`ApplicationEmojiService`)

Unchanged rules: once on the first Ready, in the background; lists the application's emojis;
uploads only what is missing; never replaces one; a failed list retries on the next Ready; a failed
upload is logged and that key keeps its fallback. What changes:

- It scans both folders: `Mushrooms/` as today, and `Icons/`, where the key is the file name and
  must be a known item key or `set.<known set>` — otherwise a warning, and the file is skipped.
- Each found or uploaded emoji is recorded with `ItemEmojis.Set(key, markup)`.
- At most 105 first-time uploads; Discord.Net waits out rate limits itself. 30 + 105 is far under
  the 2 000 application emojis allowed.

**An emoji is never replaced**, so a bot keeps the first version of an icon it uploaded. Changing a
committed icon means deleting its emoji in the developer portal and restarting — the same rule as the
mushrooms. That is why each batch is approved on its sheet before it is committed.

## The bot

**One rule: the uploaded icon, else today's Unicode.**

- **Collectibles** need nothing: `ItemInfo.Emoji` is already `ItemEmojis.For(PictureKey(Key)) ??
  DefaultEmoji`, and every screen printing an item follows.
- **Sets:** `CollectionSet`'s stored emoji becomes `DefaultEmoji`, and `Emoji` a computed property,
  `ItemEmojis.For("set." + Key) ?? DefaultEmoji`.
- **Cosmetics:** the same on `CosmeticInfo` — stored `DefaultEmoji`, computed `Emoji =
  ItemEmojis.For(Key) ?? DefaultEmoji`. Its `ItemInfo` twin is built with the same `DefaultEmoji`,
  so both read the same icon. Titres keep 🏷️ as their fallback.
- **Banner and grave sides become computed:** `Banner` is five copies of the thème's `Emoji` joined
  by « · », `GraveLeft`/`GraveRight` two copies of the cadre's, separated by a space — the strings
  they are built as today, now read at display time. They leave the positional record; their two
  call sites (`PlynlingModule.BuildCard`, `PlynlingCardUi` for the grave) are unchanged.
- **Select menus** stop building `new Emoji(c.Emoji)`: `CosmeticCards` (shop, crafting, the four
  wardrobe menus) and `InventoryModule`'s set menu go through `EmoteMarkup.Parse`, which returns an
  `Emote` for custom markup and an `Emoji` for Unicode. `new Emoji("<:…>")` would otherwise be
  rejected at send time.
- **Autocomplete** stays text-only through `ItemCatalog.TextEmoji`, which already drops custom
  markup; cosmetics go through the same path.

**Discord's caps.** A custom emoji costs ~45 characters (`<:ac_collier_coquillages:<19-digit id>>`)
where Unicode costs 1–2, so screens listing many can pass a field's 1024 or an embed's 6000 — a
send-time throw with nothing in the logs. The check fills `ItemEmojis` with a worst-case marker for
**every** key and builds, for someone owning everything: the book (overview, every set page, each
filter), the inventory, the shop, crafting, the wardrobe, the card with a thème and an accessory,
and the graveyard with cadres — then measures each against Discord's caps. A screen that overflows
is split, the way the book already splits a set by rarity.

**`/inventory view` becomes two pages.** Measured before any change, someone holding everything
would reach ≈ 7 100 characters once every icon is uploaded (the cap on a message's embeds is 6 000);
even the wardrobe as icons only came to ≈ 6 280. So the embed splits into « 🎒 Objets » (pantry,
collectibles, the collection line) and « 👗 Garde-robe » (the cosmetics by slot, with names), behind
two buttons with one custom id per page, `inv:page:{Items|Wardrobe}`, the page on screen disabled.
The message is ephemeral, so whoever clicks is its owner and the id needs no user. `/help`'s line
for `/inventory view` mentions the garde-robe.

## Checks (scratch harness)

- Every PNG in `Assets/Icons/` maps to a known key; 128×128; an exact ×8 upscale of a 16×16; alpha
  only 0 or 255; colours only from the palette.
- `EmojiName` is unique over all keys, valid, ≤ 32 characters, and never collides with a `shroom_`
  name.
- Computed emojis: fallback before `ItemEmojis.Set`, the icon after, for a set, a cosmetic and its
  `ItemInfo` twin; the banner and grave sides follow.
- `EmoteMarkup.Parse` gives an `Emote` for markup and an `Emoji` for Unicode in every menu.
- The worst-case caps above.
- Once the last batch is in: every collectible, set and cosmetic has its icon. Until then a missing
  icon only means its Unicode.

## Rollout

1. **Tool and pilot:** `tools/item-art/` and the 8 Cailloux icons, with their sheet. Nothing is
   committed until the owner approves the sheet.
2. **Bot code, once:** folder scan and emoji names, computed emojis on sets and cosmetics, computed
   banner and grave sides, `EmoteMarkup.Parse` in the menus, the caps checks. From here a batch is
   drawings only.
3. **Batches, one sheet each:** Nature, Trésors, Saisons, Insectes; the 6 sets; thèmes, titres,
   accessoires, cadres.
4. **Docs:** CLAUDE.md (beside the Champignons emoji note), `tools/item-art/README.md`, the README's
   collections line if it mentions emojis, and a version bump. The owner commits and pushes.
