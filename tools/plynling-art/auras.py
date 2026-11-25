"""The auras a Plynling wears: an animated effect around it (cosmetics, docs/agents/economy.md).

Ten effects (one per season among them: Pétales, Lucioles, Feuilles mortes, Flocons), each a
function of the frame over the shared 16-frame idle loop (motion.py), so frame 15 flows back into
frame 0. They are drawn last — over the plain or dressed frame — and only
on empty pixels (or the faint ground shadow), so a particle never covers the body, the face or an
accessory: one passing behind the Plynling simply disappears. Where they sit is read from the
body's own regions at rest, so one drawing fits the 7 species and their 7 babies and doesn't
follow the breath. They are not dirtied with the body, and never drawn on the frozen, sick and
angry faces (AURA_STATES).

On an adult in a hat the frame is the taller hat canvas (accessories.HAT_CANVAS), the sprite at
HAT_AT: positions are in the sprite's coordinates, offset by HAT_AT, so the aura surrounds the body.

    python tools/plynling-art/auras.py <outdir> [aura…]   # the preview sheets and animations (every aura, or these)
"""

import math
import os
import sys

from PIL import Image, ImageDraw

from accessories import HAT_AT, loop, tall, wear
from common import LABEL, N, SPECIES
from motion import FRAMES, FRAME_MS

# The card faces an aura is drawn on. Must match AuraMoods in PlynlingArt.cs (checks section 64).
AURA_STATES = ["happy", "content", "sad", "hungry", "starving", "sleeping"]

SHADOW_ALPHA = 90          # under this a pixel is empty or the ground shadow: a particle may cover it


def _bounds(body):
    """The body's box at rest (its regions, plus the one-pixel outline)."""
    r = body.rest.r
    xs = [x for y in range(N) for x in range(N) if r[y][x]]
    ys = [y for y in range(N) for x in range(N) if r[y][x]]
    return min(xs) - 1, min(ys) - 1, max(xs) + 1, max(ys) + 1


class _Spray:
    """Puts particle pixels onto `out` in the sprite's coordinates, only where `under` (the frame
    before the aura) is empty."""

    def __init__(self, out, under, ox, oy):
        self.out, self.under, self.ox, self.oy = out, under, ox, oy
        # the whole canvas, in sprite coordinates
        self.x_min, self.y_min = -ox, -oy
        self.x_max, self.y_max = out.width - 1 - ox, out.height - 1 - oy

    def clamp(self, x, y, margin=1):
        return (min(max(x, self.x_min + margin), self.x_max - margin),
                min(max(y, self.y_min + margin), self.y_max - margin))

    def put(self, x, y, c, a=255):
        X, Y = x + self.ox, y + self.oy
        if 0 <= X < self.out.width and 0 <= Y < self.out.height and self.under.getpixel((X, Y))[3] < SHADOW_ALPHA:
            self.out.putpixel((X, Y), c + (a,))


# ---- Scintillements: twinkling stars, the look the rare species wore until 8.5.1 ------------

STAR_CORE, STAR, STAR_DIM = (255, 255, 240), (255, 236, 150), (255, 250, 205)


def scintillements(spray, box, f):
    x0, y0, x1, y1 = box
    h = y1 - y0
    # (site, phase): six places around the body, two or three lit on any frame, frame 0 included
    sites = [((x0 - 2, y0 + 2), 2), ((x1 + 2, y0 + h // 4), 7), ((x1 - 1, y0 - 2), 12),
             ((x0 - 1, y0 + h // 2), 10), ((x1 + 1, y0 + h // 2 + 3), 1), ((x0 + 1, y0 - 2), 5)]
    for (sx, sy), phase in sites:
        x, y = spray.clamp(sx, sy, margin=2)
        t = (f + phase) % FRAMES
        if t in (0, 5):                   # rising and falling: one dim dot
            spray.put(x, y, STAR_DIM, 170)
        elif t in (1, 4):                 # a small plus
            spray.put(x, y, STAR_CORE)
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                spray.put(x + dx, y + dy, STAR, 200)
        elif t in (2, 3):                 # full twinkle: a longer cross
            spray.put(x, y, STAR_CORE)
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                spray.put(x + dx, y + dy, STAR, 230)
            for dx, dy in ((2, 0), (-2, 0), (0, 2), (0, -2)):
                spray.put(x + dx, y + dy, STAR_DIM, 130)


# ---- Bulles: soap bubbles rising straight up, then popping -----------------------------------

BUBBLE_RIM, BUBBLE_SHINE, BUBBLE_IN = (176, 222, 255), (255, 255, 255), (214, 240, 255)
BIG_BUBBLE = [".oo.", "os.o", "o..o", ".oo."]
SMALL_BUBBLE = [".o.", "o.o", ".o."]


def _bubble(spray, x, y, shape):
    for j, row in enumerate(shape):
        for i, ch in enumerate(row):
            if ch == "o":
                spray.put(x + i, y + j, BUBBLE_RIM, 210)
            elif ch == "s":
                spray.put(x + i, y + j, BUBBLE_SHINE, 240)
            elif ch == "." and 0 < i < len(row) - 1 and 0 < j < len(shape) - 1:
                spray.put(x + i, y + j, BUBBLE_IN, 70)


def bulles(spray, box, f):
    x0, y0, x1, y1 = box
    # (start column, phase, shape): each bubble lives one loop — 12 frames rising, a 2-frame pop.
    # Two on the left: the mood marks (heart, Zzz, sweat) live on the right.
    for sx, phase, shape in ((x0 - 4, 0, BIG_BUBBLE), (x0 - 2, 8, SMALL_BUBBLE), (x1 + 2, 4, BIG_BUBBLE)):
        t = (f + phase) % FRAMES
        x, start = spray.clamp(sx, y1 - 4, margin=0)
        x = min(max(x, spray.x_min), spray.x_max - len(shape[0]) + 1)
        if t < 12:
            _bubble(spray, x, start - (t * 3) // 2, shape)
        elif t < 14:                       # the pop: four droplets flying apart
            cx, cy, d = x + len(shape[0]) // 2, start - 18 + 1, t - 11
            for dx, dy in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
                spray.put(cx + dx * d, cy + dy * d, BUBBLE_RIM, 200 - 60 * (d - 1))


# ---- Pétales: petals drifting down, swaying a little -----------------------------------------

PETAL_L, PETAL, PETAL_D = (255, 214, 228), (250, 160, 192), (214, 110, 150)
SWAY = [0, 0, 1, 1, 1, 1, 0, 0, 0, 0, -1, -1, -1, -1, 0, 0]
# a petal leaning right and its mirror, as it rocks (l light, p pink, d its dark tip)
PETAL_RIGHT, PETAL_LEFT = ["lp.", ".pd"], [".pl", "dp."]
PETAL_FLAT = ["lpd"]


def petales(spray, box, f):
    x0, y0, x1, y1 = box
    top, fall = y0 - 2, (y1 - y0) + 2
    # four petals, mostly at the sides (the adults' caps fill the top), a quarter-loop apart
    for sx, phase in ((x0 - 3, 0), (x1 + 1, 4), (x0 - 1, 8), (x1 + 3, 12)):
        t = (f + phase) % FRAMES
        x, _ = spray.clamp(sx, top, margin=2)
        y = top + (t * fall) // FRAMES
        s = SWAY[t]
        shape = PETAL_RIGHT if s > 0 else PETAL_LEFT if s < 0 else PETAL_FLAT
        for j, row in enumerate(shape):
            for i, ch in enumerate(row):
                if ch != ".":
                    spray.put(x + s + i - 1, y + j, {"l": PETAL_L, "p": PETAL, "d": PETAL_D}[ch], 235)


# ---- Flocons: snowflakes falling straight down, turning as they go (winter) ------------------

SNOW, SNOW_ARM = (255, 255, 255), (196, 222, 255)
DRIFT = [0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0, -1, -1, -1, -1, 0]


def flocons(spray, box, f):
    x0, y0, x1, y1 = box
    top, fall = y0 - 3, (y1 - y0) + 4
    # (column, phase, big): two big flakes and three small ones, at the sides like the petals
    for sx, phase, big in ((x0 - 3, 0, True), (x1 + 2, 6, True), (x0 - 1, 10, False), (x1 + 4, 3, False), (x0 - 5, 13, False)):
        t = (f + phase) % FRAMES
        x, _ = spray.clamp(sx, top, margin=2)
        x += DRIFT[(t + phase) % FRAMES]
        y = top + (t * fall) // FRAMES
        a = 150 if t in (0, FRAMES - 1) else 255          # faint as it appears and as it melts
        if not big:
            spray.put(x, y, SNOW, (a * 9) // 10)
            continue
        spray.put(x, y, SNOW, a)
        # a plus, then a cross: the flake turns a quarter every two frames
        arms = ((1, 0), (-1, 0), (0, 1), (0, -1)) if (t // 2) % 2 == 0 else ((1, 1), (-1, -1), (1, -1), (-1, 1))
        for dx, dy in arms:
            spray.put(x + dx, y + dy, SNOW_ARM, (a * 4) // 5)


# ---- Feuilles mortes: autumn leaves tumbling down, swinging wide (autumn) ---------------------

# (o leaf, d its dark edge, l a light vein) per leaf: orange, red, gold; the stem is brown
LEAF_INKS = [{"o": (226, 124, 44), "d": (164, 78, 28), "l": (246, 170, 84)},
             {"o": (196, 58, 42), "d": (134, 34, 30), "l": (232, 104, 74)},
             {"o": (232, 182, 52), "d": (176, 122, 32), "l": (250, 216, 112)}]
STEM = (112, 72, 40)
SWING = [0, 1, 1, 2, 2, 2, 1, 0, 0, -1, -1, -2, -2, -2, -1, 0]
# a leaf leaning one way, its mirror, and edge-on as it flips between them (s its stem)
LEAF_RIGHT, LEAF_LEFT, LEAF_EDGE = [".ol", "odo", "s.."], ["lo.", "odo", "..s"], ["sodo"]


def feuilles(spray, box, f):
    x0, y0, x1, y1 = box
    top, fall = y0 - 3, (y1 - y0) + 3
    # three leaves, one of each colour, never a third of a loop apart so two don't flip together
    for (sx, phase), ink in zip(((x0 - 3, 0), (x1 + 2, 5), (x0 - 1, 11)), LEAF_INKS):
        t = (f + phase) % FRAMES
        x, _ = spray.clamp(sx, top, margin=3)
        y = top + (t * fall) // FRAMES
        s, step = SWING[t], SWING[(t + 1) % FRAMES] - SWING[t - 1]
        shape = LEAF_RIGHT if step > 0 else LEAF_LEFT if step < 0 else LEAF_EDGE
        for j, row in enumerate(shape):
            for i, ch in enumerate(row):
                if ch != ".":
                    spray.put(x + s + i - 1, y + j, STEM if ch == "s" else ink[ch], 240)


# ---- Lucioles: fireflies wandering in small loops, glowing and going out (summer) -------------

GLOW_CORE, GLOW, GLOW_HALO, EMBER = (255, 255, 196), (222, 246, 104), (190, 236, 80), (122, 140, 52)
# how bright a firefly is over its loop: out, warming, lit, fading
BLINK = [0, 0, 0, 1, 2, 3, 3, 3, 3, 2, 1, 0, 0, 0, 0, 0]
# a little figure-of-eight, closed over the loop
WANDER = [(round(2 * math.cos(2 * math.pi * t / FRAMES)), round(1.4 * math.sin(4 * math.pi * t / FRAMES))) for t in range(FRAMES)]


def lucioles(spray, box, f):
    x0, y0, x1, y1 = box
    h = y1 - y0
    # four, two on the left: the mood marks (heart, Zzz, sweat) live on the right
    for (sx, sy), phase in (((x0 - 4, y0 + h // 3), 0), ((x0 - 2, y1 - 3), 9), ((x1 + 1, y0 - 1), 5), ((x1 + 3, y1 - 5), 13)):
        t = (f + phase) % FRAMES
        bx, by = spray.clamp(sx, sy, margin=3)
        dx, dy = WANDER[(t + phase) % FRAMES]
        x, y = bx + dx, by + dy
        b = BLINK[t]
        if b == 0:                         # out: a faint body, still there in the dark
            spray.put(x, y, EMBER, 120)
        elif b == 1:
            spray.put(x, y, GLOW, 200)
        else:
            spray.put(x, y, GLOW_CORE)
            for ddx, ddy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                spray.put(x + ddx, y + ddy, GLOW_HALO, 150 if b == 3 else 90)


# ---- Mouches: a few flies buzzing round in tight loops, wings beating (for Le Grognon) ---------

# a khaki body (a black one vanishes on Discord's dark theme), a darker head, pale wings
FLY, FLY_HEAD, FLY_WING = (132, 118, 98), (78, 68, 60), (214, 228, 244)


def mouches(spray, box, f):
    x0, y0, x1, y1 = box
    h = y1 - y0
    # three flies, two on the left (the mood marks live on the right), each on its own closed loop
    for (sx, sy), phase, rx in (((x0 - 3, y0 + h // 3), 0, 2), ((x0 - 2, y0 + 2 * h // 3), 5, 1), ((x1 + 2, y0 + 1), 11, 2)):
        t = (f + phase) % FRAMES
        bx, by = spray.clamp(sx, sy, margin=3)
        a = 2 * math.pi * t / FRAMES
        x, y = bx + round(rx * math.cos(2 * a)), by + round(1.2 * math.sin(3 * a))
        spray.put(x, y, FLY)
        spray.put(x + (1 if t < FRAMES // 2 else -1), y, FLY_HEAD)       # facing where it goes
        if t % 2 == 0:                     # wings up
            spray.put(x - 1, y - 1, FLY_WING, 200)
            spray.put(x + 1, y - 1, FLY_WING, 200)
        else:                              # wings level, a blur
            spray.put(x - 1, y, FLY_WING, 150)
            spray.put(x + 1, y, FLY_WING, 150)


# ---- Confettis: bits of coloured paper fluttering down, flipping as they fall ------------------

CONFETTI = [(240, 84, 100), (84, 170, 240), (250, 210, 72), (122, 210, 110), (200, 124, 232)]


def confettis(spray, box, f):
    x0, y0, x1, y1 = box
    top, fall = y0 - 3, (y1 - y0) + 3
    # five pieces, one of each colour, at the sides like the petals, spread over the loop
    for (sx, phase), c in zip(((x0 - 3, 0), (x1 + 2, 3), (x0 - 1, 7), (x1 + 4, 10), (x0 - 5, 13)), CONFETTI):
        t = (f + phase) % FRAMES
        x, _ = spray.clamp(sx, top, margin=2)
        x += DRIFT[t]
        y = top + (t * fall) // FRAMES
        shade = tuple(v * 3 // 4 for v in c)
        if (t // 2) % 2 == 0:              # flat: a little strip
            spray.put(x, y, c, 240)
            spray.put(x + 1, y, shade, 240)
        else:                              # edge-on
            spray.put(x, y, c, 240)
            spray.put(x, y + 1, shade, 200)


# ---- Éclairs: small lightning bolts flashing at the sides, then sparks ---------------------------

BOLT_CORE, BOLT, BOLT_AFTER, SPARK = (255, 252, 214), (255, 222, 84), (150, 170, 255), (255, 240, 150)
BOLT_SHAPE = ["..x", ".x.", "xxx", ".x.", "x.."]


def eclairs(spray, box, f):
    x0, y0, x1, y1 = box
    h = y1 - y0
    # three sites, each striking once a loop: two frames lit, an afterglow, two sparks, then dark
    for (sx, sy), phase in (((x0 - 4, y0 + 1), 0), ((x1 + 2, y0 + h // 2), 6), ((x0 - 3, y0 + h // 2 + 2), 11)):
        t = (f + phase) % FRAMES
        x, y = spray.clamp(sx, sy, margin=3)
        if t <= 2:
            for j, row in enumerate(BOLT_SHAPE):
                for i, ch in enumerate(row):
                    if ch == "x":
                        if t == 2:
                            spray.put(x + i - 1, y + j - 2, BOLT_AFTER, 120)
                        else:
                            spray.put(x + i - 1, y + j - 2, BOLT_CORE if j == 2 else BOLT, 255 if t == 0 else 220)
        elif t in (3, 4):
            d = t - 2
            spray.put(x - d, y + 2 + d, SPARK, 200 - 60 * (d - 1))
            spray.put(x + d, y + 2 + d, SPARK, 200 - 60 * (d - 1))


# ---- Étoiles filantes: shooting stars streaking down past the cap, a trail behind -------------

SHOOT_CORE, SHOOT, SHOOT_TRAIL = (255, 255, 240), (255, 232, 140), (180, 200, 255)


def etoiles_filantes(spray, box, f):
    x0, y0, x1, y1 = box
    # two stars half a loop apart, one down each side where the body leaves room (a star crossing
    # the cap would vanish behind it): 7 frames streaking steeply down and outwards, then a twinkle
    for sx, phase, dx in ((x1 + 2, 0, 1), (x0 - 2, 8, -1)):
        t = (f + phase) % FRAMES
        x, y = spray.clamp(sx, y0 + 1, margin=3)
        if t < 7:
            hx, hy = x + dx * (t // 3), y + 2 * t
            for k, a in ((3, 70), (2, 120), (1, 180)):            # the trail, faintest last
                spray.put(hx - dx * (k // 3), hy - 2 * k, SHOOT_TRAIL, a)
                spray.put(hx - dx * (k // 3), hy - 2 * k + 1, SHOOT_TRAIL, a // 2)
            spray.put(hx, hy, SHOOT_CORE)
            for ddx, ddy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                spray.put(hx + ddx, hy + ddy, SHOOT, 170)
        elif t == 7:                                              # it burns out in a twinkle
            hx, hy = x + dx * 2, y + 14
            for ddx, ddy in ((1, 1), (-1, -1), (1, -1), (-1, 1)):
                spray.put(hx + ddx, hy + ddy, SHOOT, 150)


# key (the cosmetic key without « cos.aura. ») -> drawing, in catalogue order
AURAS = {"scintillements": scintillements, "bulles": bulles, "petales": petales,
         "flocons": flocons, "feuilles": feuilles, "lucioles": lucioles,
         "mouches": mouches, "confettis": confettis, "eclairs": eclairs, "etoiles_filantes": etoiles_filantes}

def surround(im, body, state, aura, frame, tall=False):
    """A copy of the built (and maybe dressed) frame `im` with aura `aura` drawn for `frame`; `tall`
    when `im` is the hat canvas, the sprite at HAT_AT. Faces outside AURA_STATES come back unchanged."""
    out = im.copy()
    if state not in AURA_STATES:
        return out
    ox, oy = HAT_AT if tall else (0, 0)
    AURAS[aura](_Spray(out, im, ox, oy), _bounds(body), frame)
    return out


def aura_loop(state, sp, aura, stage="adult", dirty=False, acc=None, style=None):
    """Every frame of the idle loop, dressed in `acc` (or not) and surrounded by `aura`."""
    frames = []
    for f, (im, body) in enumerate(loop(state, sp, FRAMES, stage, dirty)):
        if acc:
            im = wear(im, acc, body, state, style)
        frames.append(surround(im, body, state, aura, f, tall=bool(acc) and tall(acc, stage)))
    return frames


# ---- previews -----------------------------------------------------------------------------

BG = (47, 49, 54, 255)


def _grid(cells, row_labels, col_labels, path, k=5, cell=40):
    """A sheet of pictures, each bottom-centred in a `cell`-square box (so a 32 sprite and a 40 hat
    canvas share the scale), scaled by k without smoothing."""
    gap, top, left = 10, 26, 130
    T = cell * k
    out = Image.new("RGBA", (left + len(col_labels) * (T + gap), top + len(row_labels) * (T + gap)), BG)
    d = ImageDraw.Draw(out)
    for j, lab in enumerate(col_labels):
        d.text((left + j * (T + gap) + T // 2, top // 2), lab, fill=(220, 221, 222), anchor="mm", font=LABEL)
    for i, lab in enumerate(row_labels):
        d.text((left // 2, top + i * (T + gap) + T // 2), lab, fill=(220, 221, 222), anchor="mm", font=LABEL)
        for j in range(len(col_labels)):
            im = cells[i][j]
            big = im.resize((im.width * k, im.height * k), Image.NEAREST)
            out.alpha_composite(big, (left + j * (T + gap) + (T - big.width) // 2, top + i * (T + gap) + T - big.height))
    out.save(path)
    return path


def _animated(frame_grids, path, k=6, cell=40):
    """An animated WebP of a grid of loops (rows of [frames]), for seeing the motion."""
    rows, cols = len(frame_grids), len(frame_grids[0])
    T = cell * k
    frames = []
    for f in range(FRAMES):
        out = Image.new("RGBA", (cols * T, rows * T), BG)
        for i in range(rows):
            for j in range(cols):
                im = frame_grids[i][j][f]
                big = im.resize((im.width * k, im.height * k), Image.NEAREST)
                out.alpha_composite(big, (j * T + (T - big.width) // 2, i * T + T - big.height))
        frames.append(out)
    frames[0].save(path, save_all=True, append_images=frames[1:], duration=FRAME_MS, loop=0, lossless=True)
    return path


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "."
    os.makedirs(out, exist_ok=True)
    shapes = [(sp, st) for sp in SPECIES for st in ("adult", "baby")]
    names = [f"{sp}{' bébé' if st == 'baby' else ''}" for sp, st in shapes]
    worn = [None, "couronne", "haut_de_forme", "echarpe", "ballon", "lunettes"]
    for aura in sys.argv[2:] or AURAS:
        # every shape in every aura face, frame 0, plain
        cells = [[aura_loop(state, sp, aura, st)[0] for state in AURA_STATES] for sp, st in shapes]
        print(_grid(cells, names, AURA_STATES, os.path.join(out, f"aura_{aura}_faces.png")))
        # the content Amanite and Cèpe adults with accessories (the hats on the taller canvas)
        cells = [[aura_loop("content", sp, aura, acc=acc)[0] for acc in worn] for sp in ("amanite", "cepe", "coprin")]
        print(_grid(cells, ["amanite", "cepe", "coprin"], [a or "rien" for a in worn], os.path.join(out, f"aura_{aura}_accessories.png")))
        # the motion: every shape, content, and three faces of the Amanite with a hat
        loops = [[aura_loop("content", sp, aura, st) for sp, st in shapes[r::2]] for r in (0, 1)]
        loops.append([aura_loop(s, "amanite", aura, acc="couronne") for s in ("happy", "sad", "sleeping")]
                     + [aura_loop("content", "amanite", aura, dirty=True), aura_loop("hungry", "rose", aura, acc="echarpe"),
                        aura_loop("starving", "dore", aura), aura_loop("happy", "russule", aura, "baby")])
        print(_animated(loops, os.path.join(out, f"aura_{aura}_motion.webp")))
