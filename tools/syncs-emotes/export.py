"""Turns Gemini's mood pictures of SYNCS (flat cyan background, docs/syncs-appearance.md, "Emote
pack") into transparent 128 px emotes: docs/assets/emotes/syncs_<mood>.png, ready to upload to the
server by hand (the ids then go into Helpers/Emotes). Also writes out/sheet.html, every emote on
Discord's dark and light backgrounds at 128, 48 and 22 px.

    python -I export.py [--head] <picture>=<mood> [<picture>=<mood> …]
    python -I export.py smug.jpg=smug sulk.png=sulk

--head writes a head-only version to docs/assets/emotes/head/ instead (sheet: out/sheet_head.html).
--sticker writes a 320 px server sticker (Discord's size, 512 KB max) to docs/assets/stickers/
instead (sheet: out/sheet_sticker.html); the pixel art is scaled up whole pixels first, so it stays
crisp rather than blurred. It also writes the sticker's strip (below).
--strip [<mood> …], alone, rebuilds the strips from the stickers already in docs/assets/stickers/
(every one, or the moods named): ProjectSYNCS/Assets/Stickers/syncs_<mood>.png, the sticker at the
left of a 1000x320 transparent strip, which the bot posts as an image. The phone app stretches an
image to the screen's width, so a square one fills the screen; at this ratio the sticker comes out
sticker-sized on the phone and on PC alike.
Nothing is cut along a line: the body is taken off piece by piece so the head keeps its own
silhouette — see head_only.

Steps, per picture:
1. Snap to the pixel grid when there is one (Gemini draws on an exact grid, 10 px for the
   avatars): each art pixel becomes the median of its cell's inside, which drops the JPEG noise
   and the blended cell borders that would otherwise leave a cyan fringe.
2. Key out the cyan: everything close to #00FFFF goes fully transparent — reached from the edge
   (the background) or not (holes between hair strands). Her palette has no cyan, by design.
3. Crop to what is left, centre it on a square with a small margin, scale down to 128 px with
   alpha-correct resampling (at emote size a hard nearest-neighbour cut drops whole rows).
"""
import base64
import html
import os
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
MOOD_DIR = os.path.normpath(os.path.join(HERE, "..", "..", "docs", "assets", "emotes"))
STICKER_DIR = os.path.normpath(os.path.join(HERE, "..", "..", "docs", "assets", "stickers"))
STRIP_DIR = os.path.normpath(os.path.join(HERE, "..", "..", "ProjectSYNCS", "Assets", "Stickers"))
OUT = os.path.join(HERE, "out")

# The emote pack, docs/syncs-appearance.md. The emoji name is syncs_<mood>.
MOODS = ["smug", "flustered", "sulk", "jenote", "outraged", "hurt", "mama", "glitch", "devoted",
         "terror", "happy", "laugh", "bored", "shocked", "sleepy",
         "bargain", "overheat", "menace", "reboot", "disgust", "love", "full_love",
         "tipsy_love"]

CYAN = (0, 255, 255)
KEY_DISTANCE = 110      # RGB distance to cyan under which a pixel is background
FRINGE_DISTANCE = 190   # …and under which a pixel touching the background is a leftover fringe
SIZE = 128
STICKER_SIZE = 320
STRIP_WIDTH = 1000      # 1000x320, tested on PC and phone; 800 came out too big on PC
# Head only. The chin sits at 2/3 of the height in every v1 picture (Gemini kept the reference
# framing), so the body is searched for below these fractions only.
COLLAR_SEED = 0.62      # turtleneck pixels from here down seed the flood that removes it
DARK_BELOW = 0.645      # from here down, navy joined to the body goes too (outline is redrawn)
NECK_BELOW = 0.665      # from here down, neck-coloured skin joined to the body goes
OUTLINE = (20, 22, 60)  # her navy outline, drawn where the body was taken off
MARGIN = 4              # transparent pixels kept around her at 128 px


def distance(c, ref=CYAN):
    return sum((a - b) ** 2 for a, b in zip(c, ref)) ** 0.5


def grid_period(im):
    """The pixel-art grid in px, or None when there isn't a clear one."""
    lum = im.convert("L")
    w, h = lum.size
    px = lum.load()
    dx = [0] * (w - 1)
    for y in range(0, h, 4):
        for x in range(w - 1):
            dx[x] += abs(px[x + 1, y] - px[x, y])
    mean = sum(dx) / len(dx) or 1
    scores = {}
    for p in range(4, 41):
        acc = [0] * p
        for x, v in enumerate(dx):
            acc[(x + 1) % p] += v
        scores[p] = max(acc) / (len(dx) / p) / mean
    # The avatars score above 4; a picture with no grid stays near 1. Every multiple of the real
    # period lands on the same borders and can score as high, so take the smallest period that
    # comes close to the best: snapping to a multiple would merge whole blocks of art pixels.
    best = max(scores.values())
    if best < 2.5:
        return None
    return min(p for p, s in scores.items() if s >= 0.85 * best)


def snap(im, p):
    lum = im.convert("L").load()
    w, h = im.size

    def phase(horizontal):
        acc = [0] * p
        for a in range((w if horizontal else h) - 1):
            s = 0
            for b in range(0, h if horizontal else w, 4):
                s += abs(lum[a + 1, b] - lum[a, b]) if horizontal else abs(lum[b, a + 1] - lum[b, a])
            acc[(a + 1) % p] += s
        return max(range(p), key=lambda k: acc[k])

    ox, oy = phase(True), phase(False)
    cols, rows = (w - ox) // p, (h - oy) // p
    src = im.load()
    out = Image.new("RGB", (cols, rows))
    dst = out.load()
    inset = max(1, p // 5)
    for j in range(rows):
        for i in range(cols):
            x0, y0 = ox + i * p, oy + j * p
            chans = ([], [], [])
            for y in range(y0 + inset, y0 + p - inset):
                for x in range(x0 + inset, x0 + p - inset):
                    for c, v in zip(chans, src[x, y]):
                        c.append(v)
            dst[i, j] = tuple(sorted(c)[len(c) // 2] for c in chans)
    return out


def key_out(im):
    w, h = im.size
    src = im.load()
    out = Image.new("RGBA", (w, h))
    dst = out.load()
    for y in range(h):
        for x in range(w):
            c = src[x, y]
            dst[x, y] = (0, 0, 0, 0) if distance(c) < KEY_DISTANCE else (*c, 255)
    # One pass of fringe: a cyan-tinted pixel touching the background is a blended border.
    fringe = []
    for y in range(h):
        for x in range(w):
            if dst[x, y][3] and distance(src[x, y]) < FRINGE_DISTANCE and any(
                    0 <= x + dx < w and 0 <= y + dy < h and dst[x + dx, y + dy][3] == 0
                    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                fringe.append((x, y))
    for x, y in fringe:
        dst[x, y] = (0, 0, 0, 0)
    return out


SIDES = ((1, 0), (-1, 0), (0, 1), (0, -1))
AROUND = SIDES + ((1, 1), (1, -1), (-1, 1), (-1, -1))


def is_collar(c):
    """The turtleneck's navy body, (56,48,80) and its shade (40,32,64), or the glitch's darker
    (28,32,72) under its scanlines; not the outline, (20,22,60)."""
    r, g, b, a = c
    return a and (30 <= r <= 75 and 25 <= g <= 70 and r + 15 <= b <= 110
                  or 22 <= r < 30 and 28 <= g <= 45 and 66 <= b <= 95)


def is_outline(c):
    r, g, b, a = c
    return a and r < 30 and g < 35 and b < 80


def is_neck(c):
    """Neck skin: the dark mauve shadow under her jaw, (172,100,124), and the pink of its lit part,
    (240,166,173), with the glitch's more saturated (118,59,103) and (228,140,161) — never the
    face's (254,208,195), so the chin keeps its own curve."""
    r, g, b, a = c
    shadow = 110 <= r <= 215 and 50 <= g <= 145 and 90 <= b <= 165 and r - g >= 50
    lit = 220 <= r <= 250 and 135 <= g <= 182 and 150 <= b <= 188 and r - g >= 60
    return a and (shadow or lit)


def components(px, w, h):
    """8-connected opaque regions: a dict pixel → region number."""
    label, n = {}, 0
    for y in range(h):
        for x in range(w):
            if px[x, y][3] and (x, y) not in label:
                n += 1
                stack = [(x, y)]
                while stack:
                    q = stack.pop()
                    if q in label:
                        continue
                    label[q] = n
                    qx, qy = q
                    stack.extend((qx + dx, qy + dy) for dx, dy in AROUND
                                 if 0 <= qx + dx < w and 0 <= qy + dy < h and px[qx + dx, qy + dy][3])
    return label


def head_only(im):
    """Takes the body off, piece by piece, and leaves the head with its own outline.

    1. The turtleneck: its body colour flooded up from the bottom, so the bits of collar showing
       beside the chin go too, while navy elsewhere (outlines, the glitch's bands) stays.
    2. Below the chin, whatever joins the removed collar and is navy (the glitch's turtleneck is
       outline-dark under its scanlines; outlines are redrawn in 5) or neck skin — its shadow
       and lit pink, never the face's colour, so the chin keeps the curve it was drawn with.
    3. The collar's own outline, left floating: outline pixels beside what was removed that touch
       nothing but outline or air. Only beside it — a pupil is solid outline colour too.
    4. Whatever only held on through the body (fists, the notebook, crossed arms): every region
       that was part of her before and is no longer joined to her cap. Extras that were never
       joined (the moon, sparkles, sweat drops, steam) stay, unless they sat against the body
       below the chin (the plug's shake marks).
    5. Her outline along every edge the body left, so the chin closes like the rest of her.
    """
    w, h = im.size
    px = im.load()
    clear = (0, 0, 0, 0)
    before = components(px, w, h)
    sizes = {}
    for region in before.values():
        sizes[region] = sizes.get(region, 0) + 1
    her = max(sizes, key=sizes.get)   # the moon or the steam can sit higher than her cap
    removed = set()

    stack = [(x, y) for y in range(int(h * COLLAR_SEED), h) for x in range(w) if is_collar(px[x, y])]
    while stack:
        x, y = stack.pop()
        if (x, y) in removed or not (0 <= x < w and 0 <= y < h) or not is_collar(px[x, y]):
            continue
        removed.add((x, y))
        stack.extend((x + dx, y + dy) for dx, dy in SIDES)

    def goes(x, y):
        c = px[x, y]
        return (y >= h * DARK_BELOW and (is_collar(c) or is_outline(c))
                or y >= h * NECK_BELOW and is_neck(c))

    stack = [(x + dx, y + dy) for x, y in removed for dx, dy in SIDES]
    while stack:
        x, y = stack.pop()
        if (x, y) in removed or not (0 <= x < w and 0 <= y < h) or not goes(x, y):
            continue
        removed.add((x, y))
        stack.extend((x + dx, y + dy) for dx, dy in SIDES)
    for q in removed:
        px[q] = clear

    for _ in range(2):
        loose = [(x, y) for y in range(h) for x in range(w) if is_outline(px[x, y])
                 and any((x + dx, y + dy) in removed for dx, dy in SIDES)
                 and not any(0 <= x + dx < w and 0 <= y + dy < h and px[x + dx, y + dy][3]
                             and not is_outline(px[x + dx, y + dy]) for dx, dy in SIDES)]
        for q in loose:
            px[q] = clear
            removed.add(q)

    after = components(px, w, h)
    kept = {after[q] for q in after if before.get(q) == her and q[1] < h // 2}
    near = {(x + dx, y + dy) for x, y in removed for dx in range(-4, 5) for dy in range(-4, 5)}
    body_extras = {after[q] for q in after if before.get(q) != her and q in near}
    body_extras -= {after[q] for q in after if q[1] < h * NECK_BELOW}
    for q, region in after.items():
        if before.get(q) == her and region not in kept or region in body_extras:
            px[q] = clear

    edge = [(x, y) for x, y in removed if not px[x, y][3] and any(
        0 <= x + dx < w and 0 <= y + dy < h and px[x + dx, y + dy][3] and not is_outline(px[x + dx, y + dy])
        for dx, dy in SIDES)]
    for q in edge:
        px[q] = (*OUTLINE, 255)
    return im


def frame(im, size=SIZE):
    box = im.getbbox()
    if box is None:
        raise SystemExit("nothing left after keying out the cyan")
    im = im.crop(box)
    side = max(im.size)
    margin = MARGIN * size // SIZE
    inner = size - 2 * margin
    square = Image.new("RGBA", (side, side))
    square.paste(im, ((side - im.width) // 2, (side - im.height) // 2))
    if side < inner:
        # Scaling up: whole pixels first, so the resample below only softens their edges.
        k = 4 * inner // side + 1
        square = square.resize((side * k, side * k), Image.NEAREST)
    # Premultiplied, so the transparent pixels' colour never bleeds into the edges.
    small = square.convert("RGBa").resize((inner, inner), Image.LANCZOS, reducing_gap=3.0).convert("RGBA")
    out = Image.new("RGBA", (size, size))
    out.paste(small, (margin, margin))
    return out


def export(path, mood, dest_dir, head, size=SIZE):
    im = Image.open(path).convert("RGB")
    p = grid_period(im)
    if p:
        im = snap(im, p)
    elif max(im.size) > 600:
        im = im.resize((600, 600 * im.height // im.width), Image.LANCZOS)
    keyed = key_out(im)
    emote = frame(head_only(keyed) if head else keyed, size)
    dest = os.path.join(dest_dir, f"syncs_{mood}.png")
    emote.save(dest, optimize=True)
    print(f"{os.path.basename(path)} -> syncs_{mood}.png (grid {p or 'none'}, {os.path.getsize(dest) // 1024} KB)")


def strip(mood):
    sticker = Image.open(os.path.join(STICKER_DIR, f"syncs_{mood}.png")).convert("RGBA")
    if sticker.size != (STICKER_SIZE, STICKER_SIZE):
        raise SystemExit(f"syncs_{mood}.png is {sticker.size}, expected a {STICKER_SIZE} px sticker")
    out = Image.new("RGBA", (STRIP_WIDTH, STICKER_SIZE))
    out.paste(sticker, (0, 0))
    os.makedirs(STRIP_DIR, exist_ok=True)
    dest = os.path.join(STRIP_DIR, f"syncs_{mood}.png")
    out.save(dest, optimize=True)
    print(f"syncs_{mood}.png -> strip ({os.path.getsize(dest) // 1024} KB)")


def sheet(dest_dir, name, sizes=(128, 48, 22)):
    files = sorted(f for f in os.listdir(dest_dir) if f.endswith(".png"))
    rows = []
    for f in files:
        with open(os.path.join(dest_dir, f), "rb") as fh:
            uri = "data:image/png;base64," + base64.b64encode(fh.read()).decode()
        cells = "".join(
            f'<td style="background:{bg}">' + "".join(
                f'<img src="{uri}" width="{s}" height="{s}">' for s in sizes) + "</td>"
            for bg in ("#313338", "#ffffff"))
        rows.append(f"<tr><th>{html.escape(f[:-4])}</th>{cells}</tr>")
    os.makedirs(OUT, exist_ok=True)
    with open(os.path.join(OUT, name), "w", encoding="utf-8") as fh:
        fh.write("<!doctype html><meta charset=utf-8><title>SYNCS emotes</title>"
                 "<style>body{font:14px sans-serif}td{padding:8px}img{vertical-align:middle;margin:0 8px}"
                 "th{text-align:left;padding-right:12px}</style><table>" + "".join(rows) + "</table>")


def main(args):
    if args and args[0] == "--strip":
        moods = args[1:] or sorted(f[len("syncs_"):-4] for f in os.listdir(STICKER_DIR)
                                   if f.startswith("syncs_") and f.endswith(".png"))
        for mood in moods:
            if mood not in MOODS:
                raise SystemExit(f"{mood}: mood one of {', '.join(MOODS)}")
            strip(mood)
        return
    head = "--head" in args
    sticker = "--sticker" in args
    args = [a for a in args if a not in ("--head", "--sticker")]
    if not args:
        raise SystemExit(__doc__)
    if head and sticker:
        raise SystemExit("--head and --sticker don't combine: a sticker keeps her body")
    dest_dir = STICKER_DIR if sticker else os.path.join(MOOD_DIR, "head") if head else MOOD_DIR
    size = STICKER_SIZE if sticker else SIZE
    os.makedirs(dest_dir, exist_ok=True)
    for arg in args:
        path, sep, mood = arg.rpartition("=")
        if not sep or mood not in MOODS:
            raise SystemExit(f"{arg}: expected <picture>=<mood>, mood one of {', '.join(MOODS)}")
        export(path, mood, dest_dir, head, size)
        if sticker:
            strip(mood)
    if sticker:
        sheet(dest_dir, "sheet_sticker.html", (320, 160))
    else:
        sheet(dest_dir, "sheet_head.html" if head else "sheet.html")


if __name__ == "__main__":
    main(sys.argv[1:])
