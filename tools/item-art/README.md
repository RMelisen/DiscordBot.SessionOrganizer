# Item icons

The 16×16 icons of the collectibles, sets and cosmetics, and the 22×22 traits and badges, drawn as
text grids so a tweak is an edit.
They are exported to `ProjectSYNCS/Assets/Icons/<key>.png` and uploaded by the bot as its own
application emojis at startup (`ApplicationEmojiService`). The Champignons are a separate,
third-party pack in `Assets/Mushrooms/` — these icons are drawn in its palette (Pear36 plus eight of
the pack's colours) to match it.

- `palette.py` — the 44 colours, one character each (`.` transparent, `#` the outline).
- `icons.py` — every icon, by key, in `GROUPS` (the batches).
  Traits and badges are **22×22**, the exception to 16×16: at Discord's 22 px inline size that is
  one art pixel per screen pixel, where a 16×16 icon gets stretched unevenly. Items stay 16×16, the
  mushroom pack's size. `outlined()` wraps a fill in the outline; `+` in a fill is a thin line (a
  handle, a leg, a cord) drawn in the outline colour without an outline of its own.
  Plynling traits (`trait.<key>`) are CK3-style tiles: `tile(kind, …)` draws the frame, its rim and
  background giving the kind (enfance gold on wine, personnalité bronze on teal, coping silver on
  slate), and only the symbol is drawn — up to 16×16, in `TRAIT_SYMBOLS`; `TRAITS` lists them by
  batch. Badges (`badge.<key>`): `shield(tier, …)` draws a heater shield whose rim and field give the tier (bronze to diamond,
  plus moment and secret), and only the subject is drawn — up to 16×16, so a set's own icon fits
  as is. A member badge's subject is in `MEMBER_BADGE_SUBJECTS` under its `MemberBadges` key;
  paliers get all five tiers, moments and secrets one shield each.
- `export.py [group…]` — writes the PNGs and `out/sheet_<group>.html`, which shows each icon at
  8 px per art pixel, at 22 px (Discord's inline emoji size) beside the Unicode it replaces and a
  mushroom. 16×16 icons are exported ×8 (128 px), traits and badges ×4 (88 px, an exact multiple of
  the 44 px the CDN serves inline emojis at).
- `check.py` — grid shape, palette, a 1-pixel `#272736` outline wherever the drawing meets air,
  nothing on the frame's edge, and an exact ×8 (or ×4) export.
- `overrides/<key>.png` — a hand-edited grid at the key's size that replaces its grid (same checks).

Style: light from the top left, 4–7 colours, the object filling about 14×14, rarity in glints
(`y`, `w`) rather than borders.

- **The outline stays one dark colour.** The mushroom pack outlines in `#` alone (all but one of
  its edge pixels), so coloured outlines would set these icons apart from it.
- **The silhouette must say what it is at 22 px**, before any shading: judge the 22 px column of
  the sheet first. A round blob with dots on it reads as nothing.
- **Separate the parts with inner `#` lines or gaps** — petals, a cap from its body, a thumb from
  the hand. Gaps in a fill become outline, which is what makes five petals read as five.
- **Shade along a hue ramp, not one colour darker**: yellow → orange → red-brown (`y a o R`),
  red → crimson → plum (`E c r z`), green → teal (`l L t T`), blue → violet (`B b P`).

**An uploaded emoji is never replaced**: to change a committed icon, give it a revision in
`ItemEmojis.Revisions` (its emoji name gains `_v2`, so the bot uploads it on its own), or for a
whole family change its emoji prefix there (the traits went `tr_` → `tr2_` when redrawn at
22×22). The old emojis are left unused, to delete in the developer portal.
