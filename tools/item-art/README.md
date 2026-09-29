# Item icons

The 16×16 icons of the collectibles, sets and cosmetics, drawn as text grids so a tweak is an edit.
They are exported to `ProjectSYNCS/Assets/Icons/<key>.png` (×8) and uploaded by the bot as its own
application emojis at startup (`ApplicationEmojiService`). The Champignons are a separate,
third-party pack in `Assets/Mushrooms/` — these icons are drawn in its palette (Pear36 plus eight of
the pack's colours) to match it.

- `palette.py` — the 44 colours, one character each (`.` transparent, `#` the outline).
- `icons.py` — every icon, by key, in `GROUPS` (the batches).
- `export.py [group…]` — writes the PNGs and `out/sheet_<group>.html`, which shows each icon at
  128 px, at 22 px (Discord's inline emoji size) beside the Unicode it replaces and a mushroom.
- `check.py` — grid shape, palette, a 1-pixel `#272736` outline wherever the drawing meets air,
  nothing on the frame's edge, and an exact ×8 export.
- `overrides/<key>.png` — a hand-edited 16×16 that replaces that key's grid (same checks).

Style: light from the top left, 4–7 colours, the object filling about 14×14, rarity in glints
(`y`, `w`) rather than borders. **An uploaded emoji is never replaced**: to change a committed icon,
delete its emoji in the developer portal and restart the bot.
