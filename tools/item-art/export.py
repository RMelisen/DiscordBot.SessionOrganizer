"""Exports the item icons: icons.py → ProjectSYNCS/Assets/Icons/<key>.png (nearest neighbour), and
one HTML sheet per group in out/ to judge them at the size Discord shows them.

Items, sets and cosmetics are 16×16, exported ×8 (128 px). Badges and traits (badge.*, trait.*)
are 22×22 — one art pixel per screen pixel at Discord's 22 px inline size — exported ×4 (88 px),
an exact multiple of the 44 px the CDN serves inline emojis at.

    python export.py              every group
    python export.py cailloux     only these groups (other groups' files are left alone)

A hand-edited overrides/<key>.png (the key's size, palette colours) replaces that key's grid.
"""
import base64
import html
import os
import sys

from PIL import Image

from icons import GROUPS, ICONS
from palette import OUTLINE, PALETTE, TRANSPARENT

HERE = os.path.dirname(os.path.abspath(__file__))
ASSETS = os.path.normpath(os.path.join(HERE, "..", "..", "ProjectSYNCS", "Assets"))
ICON_DIR = os.path.join(ASSETS, "Icons")
OUT = os.path.join(HERE, "out")
OVERRIDES = os.path.join(HERE, "overrides")
REFERENCE = os.path.join(ASSETS, "Mushrooms", "girolle.png")
SCALES = {16: 8, 22: 4}


def size_of(key):
    return 22 if key.startswith(("badge.", "trait.")) else 16


def rgb(hex_colour):
    return tuple(int(hex_colour[i:i + 2], 16) for i in (1, 3, 5))


COLOURS = {rgb(h) for h in PALETTE.values()}
OUTLINE_RGB = rgb(PALETTE[OUTLINE])


def check_image(key, im):
    """The style rules that can be checked: size, alpha, palette, margin, outline."""
    size = size_of(key)
    if im.size != (size, size):
        raise SystemExit(f"{key}: {im.size}, expected {size}×{size}")
    px = im.load()
    for y in range(size):
        for x in range(size):
            r, g, b, a = px[x, y]
            if a not in (0, 255):
                raise SystemExit(f"{key}: half-transparent pixel at ({x},{y})")
            if a == 0:
                continue
            if (r, g, b) not in COLOURS:
                raise SystemExit(f"{key}: #{r:02x}{g:02x}{b:02x} at ({x},{y}) is not in the palette")
            if x in (0, size - 1) or y in (0, size - 1):
                raise SystemExit(f"{key}: pixel on the frame's edge at ({x},{y})")
            touches_air = any(px[x + dx, y + dy][3] == 0 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
            if touches_air and (r, g, b) != OUTLINE_RGB:
                raise SystemExit(f"{key}: ({x},{y}) touches transparency but is not outline")


def render(key):
    override = os.path.join(OVERRIDES, key + ".png")
    if os.path.exists(override):
        im = Image.open(override).convert("RGBA")
    else:
        grid, size = ICONS[key].grid, size_of(key)
        if len(grid) != size or any(len(row) != size for row in grid):
            raise SystemExit(f"{key}: the grid must be {size} rows of {size} characters")
        im = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        for y, row in enumerate(grid):
            for x, ch in enumerate(row):
                if ch == TRANSPARENT:
                    continue
                if ch not in PALETTE:
                    raise SystemExit(f"{key}: '{ch}' at ({x},{y}) is not a palette character")
                im.putpixel((x, y), rgb(PALETTE[ch]) + (255,))
    check_image(key, im)
    return im


def write_png(key, small, folder=ICON_DIR):
    os.makedirs(folder, exist_ok=True)
    path = os.path.join(folder, key + ".png")
    big = small.width * SCALES[small.width]
    small.resize((big, big), Image.NEAREST).save(path, optimize=True)
    return path


def data_uri(path):
    with open(path, "rb") as f:
        return "data:image/png;base64," + base64.b64encode(f.read()).decode()


def write_sheet(group, keys):
    """Each icon at 8 px per art pixel, then at 22 px (Discord's inline emoji size) beside the Unicode it
    replaces and a mushroom, then in a line of text as the inventory prints it."""
    ref = data_uri(REFERENCE)
    rows = []
    for key in keys:
        uri = data_uri(os.path.join(ICON_DIR, key + ".png"))
        ic = ICONS[key]
        rows.append(
            f"<tr><td><img src='{uri}' width={size_of(key) * 8} height={size_of(key) * 8} class=px></td>"
            f"<td><img src='{uri}' width=22 height=22> <span class=u>{ic.unicode}</span> "
            f"<img src='{ref}' width=22 height=22></td>"
            f"<td><img src='{uri}' width=22 height=22> {html.escape(ic.name)} ×3<br>"
            f"<span class=u>{ic.unicode}</span> {html.escape(ic.name)} ×3</td>"
            f"<td class=k>{key}</td></tr>")
    page = ("<!doctype html><meta charset=utf-8><title>" + group + "</title><style>"
            "body{background:#313338;color:#dbdee1;font:16px 'gg sans','Segoe UI',sans-serif;margin:16px}"
            "td{padding:6px 14px;vertical-align:middle}.px{image-rendering:pixelated}"
            ".u{font-size:22px;line-height:22px}.k{color:#949ba4;font-size:13px}img{vertical-align:middle}"
            f"</style><h2>{group}</h2><table>{''.join(rows)}</table>")
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, f"sheet_{group}.html")
    with open(path, "w", encoding="utf-8") as f:
        f.write(page)
    return path


def main(groups):
    unknown = [g for g in groups if g not in GROUPS]
    if unknown:
        raise SystemExit(f"unknown group(s): {unknown}; known: {list(GROUPS)}")
    for group in groups or list(GROUPS):
        for key in GROUPS[group]:
            write_png(key, render(key))
        print(f"{group}: {len(GROUPS[group])} icons, sheet {write_sheet(group, GROUPS[group])}")


if __name__ == "__main__":
    main(sys.argv[1:])
