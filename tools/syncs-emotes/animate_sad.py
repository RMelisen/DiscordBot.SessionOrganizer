"""Experiment, not used by the bot yet: animates the sad sticker (docs/assets/stickers/syncs_sad.png)
into a looping GIF (40 frames, 65 ms, 2.6 s). Nothing is redrawn: every frame moves the sticker's
own pixels, so it stays on model.

Writes docs/assets/animated/syncs_sad.gif (320 px, transparent) and syncs_sad_128.gif (the emote
size, kept under Discord's 256 KB emote cap), plus out/syncs_sad_dark.gif (on Discord's dark grey)
and out/sad_sheet.png (a few frames zoomed) to look at.

    python -I animate_sad.py

Beats, all periodic in the loop:
- a constant small shiver (the whole figure jitters 1 px), paused for a sniffle hitch once per loop;
- the eyes brim: the highlights wobble as one block in a tiny circle inside each iris, while the
  eye's outline stays still (a per-row wave shredded the round highlights). Never a tear: she
  can't cry, docs/syncs-appearance.md;
- the ﹏ mouth quivers: a wave runs along it;
- the tremble marks beside her head flick in and out, pushed wide on the sniffle;
- the LED breathes dim green, in four steps whose colours are pinned in the palette: median cut
  alone drops the LED's handful of pixels and it turns grey.

The coordinates (eyes, irises, mouth, LED, marks) are this sticker's; another mood needs its own.
"""
import math
import os

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(HERE, "..", ".."))
SRC = os.path.join(REPO, "docs", "assets", "stickers", "syncs_sad.png")
DEST = os.path.join(REPO, "docs", "assets", "animated")
OUT = os.path.join(HERE, "out")

F = 40          # frames
DELAY = 65      # ms per frame -> 2.6 s loop

base = Image.open(SRC).convert("RGBA")
W, H = base.size
bpx = base.load()
for y in range(H):
    for x in range(W):
        r, g, b, a = bpx[x, y]
        bpx[x, y] = (r, g, b, 255) if a >= 128 else (0, 0, 0, 0)


def lum(c):
    return 0.299 * c[0] + 0.587 * c[1] + 0.114 * c[2]


# --- tremble marks: cut out of the figure, redrawn per frame ---------------------------------
MARK_BOXES = {"left": (48, 200, 64, 227), "right": (270, 183, 291, 213)}
marks = {}
for side, (x0, y0, x1, y1) in MARK_BOXES.items():
    pts = []
    for y in range(y0, y1):
        for x in range(x0, x1):
            if bpx[x, y][3]:
                pts.append((x, y, bpx[x, y]))
                bpx[x, y] = (0, 0, 0, 0)
    marks[side] = pts


# --- eye masks: the eye's own pixels (navy, purple, whites), flood-filled from its centre ------
def is_eye(c):
    r, g, b, a = c
    if not a:
        return False
    if lum(c) < 140:
        return True
    if min(r, g, b) > 215 and max(r, g, b) - min(r, g, b) < 30:
        return True
    return b > r and lum(c) < 185


def eye_mask(seed, box):
    x0, y0, x1, y1 = box
    m, st = {seed}, [seed]
    while st:
        x, y = st.pop()
        for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if x0 <= n[0] < x1 and y0 <= n[1] < y1 and n not in m and is_eye(bpx[n]):
                m.add(n)
                st.append(n)
    return m


EYES = [eye_mask((140, 165), (116, 140, 168, 188)), eye_mask((215, 165), (194, 140, 240, 188))]

MOUTH = (155, 193, 210, 218)
IRISES = [(142, 163, 15, 14), (215, 161, 13, 13)]
WOBBLE = [(0, 0), (1, 0), (1, 1), (0, 1)]

# LED pixels: green-dominant, opaque
LED = [(x, y) for y in range(75, 95) for x in range(245, 262)
       if bpx[x, y][3] and bpx[x, y][1] > bpx[x, y][0] + 25 and bpx[x, y][1] > bpx[x, y][2] + 25]

LEVELS = [0.5, 0.65, 0.8, 0.95]
_tones = {0: [], 1: [], 2: []}
LED_TONE = {}
for p in LED:
    c = bpx[p]
    tone = 0 if lum(c) < 150 else 1 if lum(c) < 195 else 2
    LED_TONE[p] = tone
    _tones[tone].append(c)
TONE_RGB = {k: tuple(sum(c[i] for c in v) // len(v) for i in range(3)) for k, v in _tones.items() if v}


def led_colour(tone, k):
    r, g, b = TONE_RGB[tone]
    return (int(r * k + 38 * (1 - k)), int(g * k + 45 * (1 - k)), int(b * k + 50 * (1 - k)), 255)


LED_COLOURS = sorted({led_colour(t, k)[:3] for t in TONE_RGB for k in LEVELS})

SHIVER = [0, 1, 1, 0, -1, -1, 0, 1, 0, -1]
SNIFF = {30: -1, 31: -2, 32: -2, 33: -1}   # frame -> vertical hitch


def frame(t):
    im = base.copy()
    src = base.load()
    px = im.load()

    # eyes: the iris content (the highlights) wobbles as one block in a tiny circle, inside an
    # ellipse fit to each iris, so the eye's silhouette and the highlights' shapes stay intact
    ex, ey = WOBBLE[(t // 2) % len(WOBBLE)]
    for (cx, cy, rx, ry), m in zip(IRISES, EYES):
        for (x, y) in m:
            if ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 > 1:
                continue
            s = (x - ex, y - ey)
            if ((s[0] - cx) / rx) ** 2 + ((s[1] - cy) / ry) ** 2 <= 1 and s in m:
                px[x, y] = src[s]

    # mouth: vertical wave running along it
    ph = 2 * math.pi * t / 8
    x0, y0, x1, y1 = MOUTH
    for x in range(x0, x1):
        dy = round(1.0 * math.sin(2 * math.pi * x / 14 - ph))
        if not dy:
            continue
        col = [src[x, y] for y in range(y0, y1)]
        for y in range(y0 + 1, y1 - 1):
            sy = min(max(y + dy, y0), y1 - 1)
            px[x, y] = col[sy - y0]

    # LED: dim, slow breath, in LEVELS steps so its colours can be pinned in the palette
    k = LEVELS[round((0.5 + 0.5 * math.cos(2 * math.pi * t / F)) * (len(LEVELS) - 1))]
    for (x, y) in LED:
        px[x, y] = led_colour(LED_TONE[(x, y)], k)

    # whole-figure shiver / sniffle hitch
    if t in SNIFF:
        dx, dy = 0, SNIFF[t]
    else:
        dx, dy = SHIVER[t % len(SHIVER)], 0
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    out.paste(im, (dx, dy))
    opx = out.load()

    # tremble marks: flick between two spots, pushed wide on the sniffle
    spread = 3 if t in SNIFF else (1 if (t // 2) % 2 else 0)
    hop = -1 if t in SNIFF else (-1 if (t // 3) % 2 else 0)
    for side, sign in (("left", -1), ("right", 1)):
        for (x, y, c) in marks[side]:
            nx, ny = x + sign * spread, y + hop
            if 0 <= nx < W and 0 <= ny < H and not opx[nx, ny][3]:
                opx[nx, ny] = c
    return out


frames = [frame(t) for t in range(F)]


def save_gif(frs, path, bg=None, colors=255):
    """One shared palette; index 255 transparent unless composited on bg."""
    if bg:
        rgb = []
        for f in frs:
            b = Image.new("RGBA", f.size, bg + (255,))
            b.alpha_composite(f)
            rgb.append(b.convert("RGB"))
    else:
        rgb = []
        for f in frs:
            b = Image.new("RGB", f.size, (255, 0, 255))
            b.paste(f.convert("RGB"), mask=f.split()[3].point(lambda a: 255 if a >= 128 else 0))
            rgb.append(b)
    mosaic = Image.new("RGB", (frs[0].width, frs[0].height * len(frs)))
    for i, f in enumerate(rgb):
        mosaic.paste(f, (0, i * f.height))
    # Median cut alone drops the LED (a handful of pixels): quantize the rest to fewer colours and
    # pin the LED's colours in the palette.
    pinned = LED_COLOURS
    base_pal = mosaic.quantize(colors=colors - len(pinned), method=Image.Quantize.MEDIANCUT,
                               dither=Image.Dither.NONE).getpalette()[: 3 * (colors - len(pinned))]
    flat = base_pal + [v for c in pinned for v in c]
    flat += [0] * (768 - len(flat))
    pal = Image.new("P", (1, 1))
    pal.putpalette(flat)
    out = []
    for f, src in zip(rgb, frs):
        q = f.quantize(palette=pal, dither=Image.Dither.NONE)
        if not bg:
            qp, ap = q.load(), src.load()
            for y in range(q.height):
                for x in range(q.width):
                    if ap[x, y][3] < 128:
                        qp[x, y] = 255
        out.append(q)
    kw = dict(save_all=True, append_images=out[1:], duration=DELAY, loop=0, disposal=2, optimize=False)
    if not bg:
        kw["transparency"] = 255
    out[0].save(path, **kw)
    print(path, os.path.getsize(path) // 1024, "KB")


os.makedirs(DEST, exist_ok=True)
os.makedirs(OUT, exist_ok=True)
save_gif(frames, os.path.join(DEST, "syncs_sad.gif"))
save_gif(frames, os.path.join(OUT, "syncs_sad_dark.gif"), bg=(49, 51, 56))
small = [f.resize((128, 128), Image.LANCZOS) for f in frames]
save_gif(small, os.path.join(DEST, "syncs_sad_128.gif"), colors=80)

# contact sheet of the face, zoomed, for checking frames by eye
sheet = Image.new("RGBA", (6 * 300, 3 * 210), (49, 51, 56, 255))
for i, t in enumerate([0, 1, 2, 3, 4, 5, 6, 7, 8, 25, 26, 27, 28, 29, 30, 12, 18, 24]):
    c = frames[t].crop((40, 130, 300, 235)).resize((300, 121), Image.NEAREST)
    bgc = Image.new("RGBA", c.size, (49, 51, 56, 255))
    bgc.alpha_composite(c)
    sheet.paste(bgc, ((i % 6) * 300, (i // 6) * 210))
sheet.save(os.path.join(OUT, "sad_sheet.png"))
print("eyes", [len(m) for m in EYES], "led", len(LED), "marks", {k: len(v) for k, v in marks.items()})
