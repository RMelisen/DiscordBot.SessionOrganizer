"""The accessories a Plynling wears, drawn on its own picture (cosmetics, docs/agents/economy.md).

Each accessory is one small drawing and a family that says where it sits: on the cap, over the
eyes, at the neck, on the hands, or held in the left hand (or the right: the side is a style). Some come in styles the player picks
(a colour, a position); the first style is the default. Where is measured from the drawing itself
(the cap's real top and rim, the first stem row under it, the arms), so one drawing fits the 7
species and their 7 babies, and follows the breath and the hop. Nothing moves sideways. The
accessory stays clean on a muddy Plynling and takes the ice tint on a frozen one.

    python tools/plynling-art/accessories.py <outdir>     # the three preview sheets (all, faces, sides)
"""
import os
import sys

from PIL import Image

import species
import sprites
from common import N, SPECIES, lerp, sheet
from species import ADULT_ARMS, BABY_ARMS, FROST_TINT

FROZEN_SHARE = 0.45                       # the same ice tint as the body
HOP = 2                                   # the happy hop lifts the body this many rows

# ---- palette ----------------------------------------------------------------------------
INK = (44, 28, 40)
WHITE = (255, 255, 255)
GOLD_L, GOLD, GOLD_D, GOLD_DD = (255, 238, 150), (248, 198, 66), (200, 142, 38), (146, 98, 30)
RED_L, RED, RED_D = (255, 128, 128), (220, 60, 72), (150, 34, 52)
GEM_BLUE = (96, 176, 246)
HAT_L, HAT, HAT_D = (96, 88, 112), (58, 52, 70), (34, 30, 42)
BLUE_L, BLUE, BLUE_D, BLUE_DD = (140, 188, 252), (78, 138, 226), (48, 92, 172), (32, 62, 124)
PINK_L, PINK, PINK_D = (255, 196, 214), (250, 128, 168), (196, 76, 118)
YELLOW, ORANGE, PETAL_SHADE, GREY = (255, 206, 60), (240, 132, 40), (222, 224, 236), (164, 166, 186)
LENS, LENS_SHINE, LENS_FRAME = (40, 40, 58), (150, 172, 214), (22, 20, 30)
SCARF, SCARF_D, SCARF_STRIPE = (226, 74, 74), (164, 44, 56), (250, 236, 218)
STRING, SHELL_L, SHELL, SHELL_D = (140, 100, 66), (255, 222, 210), (246, 160, 150), (204, 104, 104)
MITT, MITT_D, CUFF = (224, 80, 86), (158, 46, 60), (250, 244, 236)
BALLOON_L, BALLOON, BALLOON_D, THREAD = (255, 168, 176), (238, 70, 86), (168, 38, 60), (206, 206, 218)
WAND = (196, 170, 236)
ROD, ROD_D, LINE = (172, 120, 70), (120, 80, 48), (226, 228, 240)
GLASS, GLASS_IN, LID = (164, 214, 236), (214, 240, 250), (150, 104, 60)
TULIP_L, TULIP, TULIP_D, LEAF, LEAF_D = (255, 140, 160), (232, 64, 98), (168, 36, 72), (110, 186, 90), (70, 140, 64)
CREAM_L, CREAM, CREAM_D, CONE, CONE_D = (255, 222, 232), (255, 168, 198), (222, 112, 156), (230, 178, 104), (182, 124, 62)
QUILL = (244, 234, 204)
GREEN_L, GREEN_DD = (168, 222, 136), (40, 92, 48)
STRAW_L, STRAW, STRAW_D = (252, 230, 160), (232, 194, 108), (182, 136, 66)
COFFEE = (96, 60, 40)
NORI = (34, 58, 44)

# One character per colour in the drawings below; "." is empty.
INKS = {
    "l": GOLD_L, "g": GOLD, "d": GOLD_D, "D": GOLD_DD, "r": RED, "R": RED_D, "e": RED_L, "b": GEM_BLUE,
    "h": HAT_L, "H": HAT, "k": HAT_D, "w": WHITE, "y": YELLOW, "E": ORANGE, "s": PETAL_SHADE, "G": GREY,
    "B": BLUE, "j": BLUE_L, "J": BLUE_D, "v": BLUE_DD,
    "p": PINK_L, "P": PINK, "q": PINK_D,
    "a": BALLOON_L, "A": BALLOON, "z": BALLOON_D, "t": THREAD, "n": WAND,
    "o": ROD, "O": ROD_D, "i": LINE,
    "c": GLASS, "C": GLASS_IN, "L": LID, "x": INK,
    "u": TULIP_L, "U": TULIP, "V": TULIP_D, "f": LEAF, "F": LEAF_D,
    "m": CREAM_L, "M": CREAM, "N": CREAM_D, "K": CONE, "Q": CONE_D, "Z": QUILL,
    "1": MITT, "2": MITT_D, "3": CUFF, "6": GREEN_L, "7": GREEN_DD,
    "4": STRAW_L, "5": STRAW, "8": STRAW_D, "9": COFFEE, "0": NORI,
}


def art(*rows):
    return [r for r in rows]


def recolour(drawing, swap):
    return ["".join(swap.get(ch, ch) for ch in row) for row in drawing]


# ---- the drawings -----------------------------------------------------------------------
# Hats are drawn upright, bottom row on the brim; the held things have their grip (the pixel just
# outside the left arm) at the drawing's bottom-right unless a grip is given.

COURONNE = art(
    "l..ll..l",
    "lg.gg.gl",
    "gggggggg",
    "grgbbgrg",
    "dddddddd",
)
HAUT_DE_FORME = art(
    ".hhhhhhhh.",
    ".hHHHHHHk.",
    ".hHHHHHHk.",
    ".hHHHHHHk.",
    ".rrrrrrrR.",
    "kHHHHHHHHk",
)
CASQUETTE = art(
    "...jjjj...",
    "..jBBBBJ..",
    ".jBBwwBBJ.",
    ".BBBBBBBJ.",
    "jjjjjjjjjj",
    ".vvvvvvvv.",
)
NOEUD = art(              # drawn pink; the other colours swap its three tones
    "pp...pp",
    "pPPqPPp",
    "PqqqqqP",
    "..q.q..",
)
NOEUD_COLOURS = {
    "rose": {},
    "bleu": {"p": "j", "P": "B", "q": "J"},
    "dore": {"p": "l", "P": "g", "q": "d"},
    "blanc": {"p": "w", "P": "s", "q": "G"},
}
FLEUR = art(              # a daisy, the same on both sides; petals "w", their parting "s"
    "..ww..",
    ".swws.",
    "wwyyww",
    "wwyyww",
    ".swws.",
    "..ww..",
)
FLEUR_COLOURS = {
    "blanche": {},
    "rose": {"w": "P", "s": "p"},
    "bleue": {"w": "B", "s": "j"},
    "jaune": {"w": "y", "s": "l", "y": "E"},
}
BALLON = art(
    ".aAA...",
    "aAAAA..",
    "AAAAz..",
    "AAAAz..",
    ".Azz...",
    "..z....",
    "...t...",
    "....t..",
    ".....t.",
    "......t",
)
BAGUETTE = art(
    "..y.....",
    ".yyy....",
    "yywyy...",
    ".yyy....",
    "..y.n...",
    ".....n..",
    "......n.",
    ".......n",
)
CANNE = art(
    "O.......",
    "iO......",
    "i.o.....",
    "i..o....",
    "i...o...",
    "i....o..",
    "r.....o.",
    "w......o",
)
BOCAL = art(
    ".LL.",
    "cCCc",
    "crxc",
    "cCCc",
    ".cc.",
)
BOCAL_GRIP = (3, 2)
TULIPE = art(
    "u.u",
    "uUU",
    "UUV",
    ".f.",
    "Ff.",
    ".f.",
)
TULIPE_GRIP = (1, 5)
GLACE = art(
    ".mM.",
    "mMMN",
    "MMNN",
    "KKKQ",
    ".KQ.",
    ".KQ.",
    "..Q.",
)
GLACE_GRIP = (2, 5)
PLUME = art(
    "ll....",
    "lgg...",
    ".ggd..",
    "..gdZ.",
    "...dZ.",
    ".....Z",
)
MOUFLE = art(          # the left hand; the cuff column (2) sits on the arm itself
    "11.",
    "113",
    "223",
)
CHAPEAU_PAILLE = art(  # a straw hat with a red ribbon and a wide brim
    "....4455....",
    "...445558...",
    "...rrrrrR...",
    "445555555558",
    ".8888888888.",
)
BONNET = art(          # knitted, the moufles' colours (it swaps the same 1/2), a cream cuff and pompom
    "....33....",
    "...1111...",
    "..111112..",
    ".11111122.",
    "3333333333",
    "3s3s3s3s3s",
)
COURONNE_FLEURS = art(  # three flowers on a ring of leaves: white, pink, yellow
    ".w...P...y.",
    "wyw.PyP.yEy",
    "fwfFfPfFfyf",
    ".FfFfFfFfF.",
)
LANTERNE = art(        # a glowing paper lantern hanging from a little stick
    "..ooooo...",
    "..k....oo.",
    "..k......o",
    ".DDD......",
    "EyllE.....",
    "EyllE.....",
    "EyyyE.....",
    ".DDD......",
    "..k.......",
)
LANTERNE_GRIP = (9, 2)


CAFE = art(            # a cup of coffee, steaming; held by its handle
    "..s.s.",
    ".s.s..",
    "w9999w",
    "wwwwwG",
    "swwwsG",
    ".ssss.",
)
CAFE_GRIP = (5, 4)
CANNE_POMMEAU = art(   # the gentleman's cane: a gold pommel, a crook to the hand, a walnut shaft held clear of the body
    "lg..",
    "gdOO",
    "o...",
    "O...",
    "o...",
    "O...",
    "o...",
    "O...",
    "o...",
    "h...",
)
CANNE_POMMEAU_GRIP = (3, 1)
MONTRE = art(          # a gold pocket watch dangling from its chain, the chain's end in the hand
    "....d",
    "...g.",
    "..d..",
    ".lgd.",
    "lwwwd",
    "gwkwD",
    "dwwwD",
    ".dDD.",
)
MONTRE_GRIP = (4, 0)
NOEUD_PAPILLON = art(  # a navy bow tie, worn under the mouth
    "jJ....Jj",
    "JJJvvJJJ",
    "vJ....Jv",
)

# ---- Soleil-Levant ----
CHAPEAU_CHINOIS = art(  # taller and narrower, red lacquer, a gold knob and a gold rim
    ".....l.....",
    ".....g.....",
    "....erR....",
    "...errrR...",
    "..errrrrR..",
    ".errrrrrrR.",
    "lggggggggdd",
)
KANZASHI = art(        # a sakura on a gold pin, two bead drops hanging under it
    "....d.",
    "..pd..",
    ".pPp..",
    "pPyPp.",
    ".pPp..",
    ".t.t..",
    ".t..t.",
    ".r..j.",
)
KANZASHI_AT = (2, 3)   # the flower's heart
ONIGIRI = art(         # a rice triangle wrapped in a band of nori
    "...w...",
    "..wws..",
    ".wwwws.",
    ".w000s.",
    "ww000ss",
    ".s000s.",
)
ONIGIRI_GRIP = (6, 4)


def _katana():
    """A sheathed katana on the diagonal, two pixels thick, the end cap up-left and the hilt in the
    hand: a gold kojiri, a red-lacquer saya with its sheen, a gold tsuba across the blade, a
    black-and-white wrapped hilt. Red rather than black: black lacquer vanished on Discord's dark grey."""
    n = 10
    rows = [["."] * (n + 1) for _ in range(n + 1)]
    for i in range(n):
        top, under = ("l", "g") if i == 0 else ("r", "R") if i < 6 else \
                     ("d", "D") if i == 6 else ("w", "x") if i % 2 else ("x", "w")
        rows[i][i], rows[i + 1][i] = top, under
    rows[5][7] = rows[8][5] = "g"                 # the tsuba, across the blade at (6, 6..7)
    rows[4][8], rows[9][4] = "l", "d"
    return ["".join(r) for r in rows]


KATANA = _katana()
KATANA_GRIP = (8, 8)


def _eventail():
    """A folding fan half open, its pivot in the hand: it opens outward, from a little below the
    hand to well above it, so it stays clear of the cap and the body. Dark ribs at the pivot, the
    paper on the outer band in folds alternating light and mid, its edge dark. Drawn red; the
    other colours swap its three tones."""
    import math
    R, lo, hi = 6.6, -12, 125                      # radius; the spread, 0 straight out, 90 straight up
    pts = {}
    for dy in range(-8, 9):
        for dx in range(-8, 9):
            r = math.hypot(dx, dy)
            angle = math.degrees(math.atan2(dy, dx))
            if r > R or not lo <= angle <= hi:
                continue
            if r >= 2.6:
                pts[dx, dy] = "R" if r > R - 1 else "e" if int((angle - lo) // 23) % 2 else "r"
            elif r < 1 or int(angle - lo) % 46 < 12:
                pts[dx, dy] = "O"
    # dx grows outward (to the left of the hand), dy upward: flip both into rows
    xmax, ymax = max(dx for dx, _ in pts), max(dy for _, dy in pts)
    xmin, ymin = min(dx for dx, _ in pts), min(dy for _, dy in pts)
    rows = [["."] * (xmax - xmin + 1) for _ in range(ymax - ymin + 1)]
    for (dx, dy), ch in pts.items():
        rows[ymax - dy][xmax - dx] = ch
    return ["".join(r) for r in rows], (xmax, ymax)


EVENTAIL, EVENTAIL_GRIP = _eventail()
EVENTAIL_COLOURS = {"rouge": {}, "indigo": {"e": "j", "r": "B", "R": "v"},
                    "sakura": {"e": "p", "r": "P", "R": "q"}}


def sugegasa(pen, b):
    """A wide, flat straw cone sized from the cap (two pixels past it on each side), sunk over the
    cap's dome so the dome hides under it, with its chin cord down both sides of what shows of the
    cap below the brim. Lit from the left like the caps; a weave line every third pixel."""
    r = b.rest
    left, right = r.cap_span()
    width = min(right - left + 5, 36 if b.stage == "adult" else 30)
    half = width // 2
    h = max(3, (width + 2) // 6)
    top_limit = -HAT_AT[1] if b.stage == "adult" else HOP          # the picture's top row
    brim = max(r.cap_top + h // 2, top_limit + h - 1)
    for j in range(h):                                              # j = 0 the brim's row
        y = brim - j
        reach = half if j == 0 else max(1, round(half * (h - j) / h))
        for x in range(16 - reach, 16 + reach):
            side = (x - 16 + reach) / max(1, 2 * reach - 1)           # 0 left .. 1 right
            c = STRAW_D if j == 0 and side > 0.15 else STRAW_L if side < 0.3 else \
                STRAW_D if side > 0.8 else STRAW
            if j and (x - 16) % 3 == 0 and 0.3 <= side <= 0.8:
                c = STRAW_D
            pen.put(x, y, c)
    pen.put(15, brim - h, STRAW_L)                                  # the cone's tip
    pen.put(16, brim - h, STRAW)
    for pick in (min, max):                                         # the cord
        for y in range(brim + 1, r.cap_bottom + 1):
            xs = [x for x in range(N) if r.r[y][x] == "cap"]
            if xs:
                pen.put(pick(xs), y, ROD_D)


# ---- the body, measured -----------------------------------------------------------------

class Body:
    """Where things are on one frame, read from the drawing's regions before the frame's motion,
    then moved by it (dy: the breath lowers, the hop lifts). `rest` is the same face's frame 0:
    whatever must not slide sideways is placed from it."""

    def __init__(self, regions, face_oy, pose, stage, rest=None, sp=None):
        self.r, self.stage, self.oy, self.sp = regions, stage, face_oy, sp
        self.rest = rest or self
        self.dy = pose.bob - pose.hop
        self.eye = 19 + face_oy                   # the top row of the eyes (sprites.face)
        cols = [y for x in (15, 16) for y in range(N) if regions[y][x] == "cap"]
        self.cap_top, self.cap_bottom = min(cols), max(cols)
        # the neck: the first stem row under the cap's centre (below the Amanite's skirt)
        self.neck = next(y for y in range(self.cap_bottom + 1, N) if regions[y][15] == "stem")
        arm_rows = sorted({y for y in range(N) for x in range(N) if regions[y][x] == "arm"})
        self.arms = arm_rows or list(ADULT_ARMS if stage == "adult" else BABY_ARMS)
        self.hand_l, self.hand_r = self.edges(self.arms[0], ("stem", "arm"))

    def edges(self, y, kinds=("stem", "ring", "arm")):
        xs = [x for x in range(N) if self.r[y][x] in kinds]
        return (min(xs), max(xs)) if xs else (11, 20)

    def cap_top_at(self, x):
        ys = [y for y in range(N) if self.r[y][x] == "cap"]
        return min(ys) if ys else None

    def cap_span(self):
        xs = [x for x in range(N) if any(self.r[y][x] == "cap" for y in range(N))]
        return min(xs), max(xs)

    def on_cap(self, x, y):
        """The cap or its outline: the pixels a hat must sit on, never in."""
        return any(0 <= x + dx < N and 0 <= y + dy < N and self.r[y + dy][x + dx] == "cap"
                   for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)))


class Pen:
    """Draws in the sprite's own coordinates onto a picture that may be the taller hat canvas:
    (ox, oy) is where the sprite sits on it."""

    def __init__(self, im, body, frozen, ox=0, oy=0):
        self.im, self.dy, self.frozen, self.ox, self.oy = im, body.dy, frozen, ox, oy

    def put(self, x, y, c, a=255):
        x, y = x + self.ox, y + self.dy + self.oy
        if not (0 <= x < self.im.width and 0 <= y < self.im.height):
            return
        if self.frozen:
            c = lerp(c, FROST_TINT, FROZEN_SHARE)
        self.im.putpixel((x, y), c + (a,))

    def stamp(self, drawing, x0, y0, mirror=False):
        for j, row in enumerate(drawing):
            for i, ch in enumerate(row[::-1] if mirror else row):
                if ch != ".":
                    self.put(x0 + i, y0 + j, INKS[ch])


# An adult's cap fills the picture nearly to its top, so a hat on it would leave the picture: an
# adult wearing a hat is drawn on a taller canvas, the sprite at its foot. The visit pictures'
# canvas (export.VISIT_CANVAS): the Plynling fills 80 % of it.
HAT_CANVAS = 40
HAT_AT = ((HAT_CANVAS - N) // 2, HAT_CANVAS - N)


# Shapes whose round or pointed cap leaves the hat looking perched too high: it sits this many rows
# lower, into the cap's top (owner's call on the preview).
HAT_SETTLE = {("amanite", "adult"): 1, ("mystique", "adult"): 1, ("dore", "adult"): 1,
              ("coprin", "adult"): 1, ("mystique", "baby"): 2}


def hat(drawing):
    """A hat upright in the middle, resting on the top of the cap: its bottom row on the cap's
    outline (lower for the HAT_SETTLE shapes). A baby's picture has room above, so its hat
    also stays clear of the happy hop."""
    w, h = len(drawing[0]), len(drawing)

    def draw(pen, b):
        r = b.rest
        x0 = 16 - w // 2
        tops = [t for t in (r.cap_top_at(x) for x in range(x0, x0 + w)) if t is not None]
        bottom = min(tops) - 1
        if b.stage != "adult":
            bottom = max(bottom, h - 1 + HOP)
        bottom += HAT_SETTLE.get((b.sp, b.stage), 0)
        pen.stamp(drawing, x0, bottom - h + 1)
    return draw


def on_cap(drawing, at, centre, mirror=False):
    """A small thing tucked on the cap's surface, `at` of the way across it. `mirror`: the other
    side of the cap, the drawing flipped and `at` and its centre flipped with it."""
    if mirror:
        at, centre = 1 - at, (len(drawing[0]) - 1 - centre[0], centre[1])

    def draw(pen, b):
        r = b.rest
        left, right = r.cap_span()
        x = int(round(left + (right - left) * at))
        top = r.cap_top_at(x)
        cx, cy = centre
        y = max((top if top is not None else r.cap_top) + 1, cy + HOP)      # clear of the hop
        pen.stamp(drawing, x - cx, y - cy, mirror)
    return draw


def held(drawing, grip=None, side="gauche"):
    """In the left hand (`side` gauche) or the right: the drawing's grip lands just outside the arm.
    On the right the drawing is stamped mirrored, its grip mirrored with it."""
    gx, gy = grip or (len(drawing[0]) - 1, len(drawing) - 1)
    if side == "droite":
        gx = len(drawing[0]) - 1 - gx

        def draw_right(pen, b):
            pen.stamp(drawing, b.hand_r + 1 - gx, b.arms[0] - gy, mirror=True)
        return draw_right

    def draw(pen, b):
        pen.stamp(drawing, b.hand_l - 1 - gx, b.arms[0] - gy)
    return draw


def lunettes(pen, b):
    e = b.eye
    for x0 in (11, 17):
        for x in range(x0, x0 + 4):
            pen.put(x, e - 1, LENS_FRAME)
            pen.put(x, e, LENS)
            pen.put(x, e + 1, LENS)
        pen.put(x0 + 1, e + 2, LENS)
        pen.put(x0 + 2, e + 2, LENS)
        pen.put(x0 + 1, e, LENS_SHINE)
    for x in (15, 16):
        pen.put(x, e, LENS_FRAME)
    pen.put(10, e, LENS_FRAME)
    pen.put(21, e, LENS_FRAME)


# What goes round the neck has two places: high, right under the cap; or low — a little further
# down for an adult, the first stem row (under the Amanite's skirt) for a baby.
def neck_row(b, place):
    if place == "haut":
        return b.cap_bottom + 1
    if b.stage != "adult":
        return b.neck
    n = b.neck + 2
    return n if n + 1 < b.eye else b.cap_bottom + 1   # a short one (Cèpe): no room above the eyes, so as high


SCARF_COLOURS = {                     # knit, its shade (the stripes stay cream)
    "rouge": (SCARF, SCARF_D),
    "bleue": ((84, 136, 226), (52, 88, 166)),
    "verte": ((96, 176, 96), (58, 120, 64)),
}


def echarpe(pen, b, n, colour="rouge"):
    knit, shade = SCARF_COLOURS[colour]
    left, right = b.edges(n)
    for x in range(left - 1, right + 2):
        stripe = (x - left) % 4 == 2
        pen.put(x, n, SCARF_STRIPE if stripe else knit)
        pen.put(x, n + 1, SCARF_STRIPE if stripe else shade)
    for y, c in ((n + 2, knit), (n + 3, SCARF_STRIPE), (n + 4, knit)):  # the end hangs on the left
        pen.put(left - 2, y, c)
        pen.put(left - 1, y, shade if c == knit else c)
    pen.put(left - 2, n + 5, shade)


def collier_coquillages(pen, b, n):
    left, right = b.edges(n)
    for x in range(left, right + 1):
        y = n if x in (left, right) else n + 1
        pen.put(x, y, SHELL_L if (x - left) % 3 == 1 else STRING)
    for x, c in ((15, SHELL_L), (16, SHELL)):
        pen.put(x, n + 2, c)
    pen.put(15, n + 3, SHELL)
    pen.put(16, n + 3, SHELL_D)


def medaillon(pen, b, n):
    left, right = b.edges(n)
    for x in range(left, right + 1):
        y = n if x in (left, right) else n + 1
        pen.put(x, y, GOLD if x % 2 else GOLD_D)
    for (x, y), c in (((15, 2), GOLD_L), ((16, 2), GOLD), ((14, 3), GOLD), ((15, 3), RED),
                      ((16, 3), RED_D), ((17, 3), GOLD_D), ((15, 4), GOLD_D), ((16, 4), GOLD_DD)):
        pen.put(x, n + y, c)


def lunettes_rondes(pen, b):
    """Thin round tortoise-shell frames around each eye, the eyes showing through."""
    e = b.eye
    for x0 in (11, 17):
        for x in (x0 + 1, x0 + 2):
            pen.put(x, e - 1, ROD_D)
            pen.put(x, e + 3, ROD_D)
        for y in (e, e + 1, e + 2):
            pen.put(x0, y, ROD_D)
            pen.put(x0 + 3, y, ROD_D)
    for x in (15, 16):
        pen.put(x, e, ROD_D)
    pen.put(10, e, ROD_D)
    pen.put(21, e, ROD_D)


def monocle(pen, b):
    """A gold rim round the right eye and its chain falling to the side."""
    e, x0 = b.eye, 17
    for x in (x0 + 1, x0 + 2):
        pen.put(x, e - 1, GOLD_L)
        pen.put(x, e + 3, GOLD_D)
    for y in (e, e + 1, e + 2):
        pen.put(x0, y, GOLD)
        pen.put(x0 + 3, y, GOLD_D)
    for i, (x, y) in enumerate(((21, e + 3), (21, e + 4), (22, e + 5), (22, e + 6), (22, e + 7))):
        pen.put(x, y, GOLD if i % 2 else GOLD_D)


def noeud_papillon(pen, b):
    pen.stamp(NOEUD_PAPILLON, 12, b.eye + 5)     # under the mouth: the collar row sits at eye level on the short ones


def at_neck(fn, place):
    return lambda pen, b: fn(pen, b, neck_row(b, place))


MOUFLE_COLOURS = {"rouges": {}, "bleues": {"1": "B", "2": "J"}, "vertes": {"1": "f", "2": "F"}}


def moufles(pen, b, swap=None):
    y = b.arms[0]
    drawing = recolour(MOUFLE, swap or {})
    pen.stamp(drawing, b.hand_l - 2, y - 1)
    pen.stamp(drawing, b.hand_r, y - 1, mirror=True)


def styles(make, options):
    return {name: make(option) for name, option in options}


NECK_PLACES = [("haut", "haut"), ("bas", "bas")]
CASQUETTE_COLOURS = {"bleue": {}, "rouge": {"j": "e", "B": "r", "J": "R", "v": "R"},
                     "verte": {"j": "6", "B": "f", "J": "F", "v": "7"}}
BALLON_COLOURS = {"rouge": {}, "bleu": {"a": "j", "A": "B", "z": "J"}, "jaune": {"a": "l", "A": "g", "z": "d"}}
BONNET_COLOURS = {"rouge": {}, "bleu": {"1": "B", "2": "J"}, "vert": {"1": "f", "2": "F"}}

# The side a nœud, a fleur or a held thing sits on: the last part of its style name, the default
# first (« rose_gauche », « blanche_droite », « droite »). Bare held items are just the side.
SIDES = ("gauche", "droite")
DEFAULT_SIDE = {"noeud": "gauche", "fleur": "droite", "ballon": "gauche", "kanzashi": "droite", "eventail": "gauche"}


def sided(make, colours, acc):
    """`make(swap, side)` in every colour and both sides, `acc`'s default side first in each colour."""
    default = DEFAULT_SIDE[acc]
    order = (default,) + tuple(s for s in SIDES if s != default)
    return {f"{colour}_{side}": make(swap, side) for colour, swap in colours.items() for side in order}


def held_sides(drawing, grip=None):
    return {side: held(drawing, grip, side) for side in SIDES}


def file_style(acc, style):
    """The style as it has always been named in exported files: the default side has no part in it
    (« rose_gauche » -> « rose », a bare « gauche » -> nothing), the other side keeps its own."""
    if not style:
        return style
    default = DEFAULT_SIDE.get(acc, "gauche" if ACCESSORIES[acc]["family"] == "held" else None)
    if default is None:
        return style
    if style == default:
        return ""
    return style[:-len(default) - 1] if style.endswith("_" + default) else style

# key (the cosmetic key without « cos.accessory. ») -> family and styles; the first style is the
# default, the one shown until the player picks another.
ACCESSORIES = {
    "noeud": dict(family="cap", styles=sided(
        lambda swap, side: on_cap(recolour(NOEUD, swap), 0.3, (3, 1), side != "gauche"), NOEUD_COLOURS, "noeud")),
    "echarpe": dict(family="neck", styles={               # colour, then place: « bleue_haut »
        f"{colour}_{place}": (lambda c, pl: lambda pen, b: echarpe(pen, b, neck_row(b, pl), c))(colour, place)
        for colour in SCARF_COLOURS for place, _ in NECK_PLACES}),
    "fleur": dict(family="cap", styles=sided(
        lambda swap, side: on_cap(recolour(FLEUR, swap), 0.74, (2, 3), side != "droite"), FLEUR_COLOURS, "fleur")),
    "casquette": dict(family="hat", styles=styles(lambda swap: hat(recolour(CASQUETTE, swap)), CASQUETTE_COLOURS.items())),
    "ballon": dict(family="held", styles=sided(lambda swap, side: held(recolour(BALLON, swap), None, side), BALLON_COLOURS, "ballon")),
    "lunettes": dict(family="eyes", styles={"": lunettes}),
    "haut_de_forme": dict(family="hat", styles={"": hat(HAUT_DE_FORME)}),
    "couronne": dict(family="hat", styles={"": hat(COURONNE)}),
    "tulipe": dict(family="held", styles=held_sides(TULIPE, TULIPE_GRIP)),
    "glace": dict(family="held", styles=held_sides(GLACE, GLACE_GRIP)),
    "moufles": dict(family="hands", styles=styles(lambda swap: lambda pen, b: moufles(pen, b, swap), MOUFLE_COLOURS.items())),
    "plume_doree": dict(family="held", styles=held_sides(PLUME)),
    "collier_coquillages": dict(family="neck", styles=styles(lambda p: at_neck(collier_coquillages, p), NECK_PLACES)),
    "medaillon": dict(family="neck", styles=styles(lambda p: at_neck(medaillon, p), NECK_PLACES)),
    "bocal": dict(family="held", styles=held_sides(BOCAL, BOCAL_GRIP)),
    "canne_peche": dict(family="held", styles=held_sides(CANNE)),
    "chapeau_paille": dict(family="hat", styles={"": hat(CHAPEAU_PAILLE)}),
    "bonnet": dict(family="hat", styles=styles(lambda swap: hat(recolour(BONNET, swap)), BONNET_COLOURS.items())),
    "lanterne": dict(family="held", styles=held_sides(LANTERNE, LANTERNE_GRIP)),
    "couronne_fleurs": dict(family="hat", styles={"": hat(COURONNE_FLEURS)}),
    "cafe": dict(family="held", styles=held_sides(CAFE, CAFE_GRIP)),
    "lunettes_rondes": dict(family="eyes", styles={"": lunettes_rondes}),
    "noeud_papillon": dict(family="neck", styles={"": noeud_papillon}),
    "monocle": dict(family="eyes", styles={"": monocle}),
    "canne": dict(family="held", styles=held_sides(CANNE_POMMEAU, CANNE_POMMEAU_GRIP)),
    "montre_gousset": dict(family="held", styles=held_sides(MONTRE, MONTRE_GRIP)),
    "onigiri": dict(family="held", styles=held_sides(ONIGIRI, ONIGIRI_GRIP)),
    "sugegasa": dict(family="hat", styles={"": sugegasa}),
    "chapeau_chinois": dict(family="hat", styles={"": hat(CHAPEAU_CHINOIS)}),
    "kanzashi": dict(family="cap", styles={          # at the fleur's spot, the right by default
        side: on_cap(KANZASHI, 0.74, KANZASHI_AT, side != "droite") for side in ("droite", "gauche")}),
    "eventail": dict(family="held", styles=sided(
        lambda swap, side: held(recolour(EVENTAIL, swap), EVENTAIL_GRIP, side), EVENTAIL_COLOURS, "eventail")),
    "katana": dict(family="held", styles=held_sides(KATANA, KATANA_GRIP)),
}


def looks():
    """Every (accessory, style) pair, in catalogue order."""
    return [(acc, st) for acc, a in ACCESSORIES.items() for st in a["styles"]]


# ---- building a dressed frame -----------------------------------------------------------

def _measure(state, sp, f, stage, dirty):
    seen = {}
    finish = species.finish

    def watch(g, p, st, S, face_oy=0, face_ox=0, shadow=True, pose=None, original=False):
        seen.update(regions=[row[:] for row in g.r], oy=face_oy, pose=pose)
        return finish(g, p, st, S, face_oy=face_oy, face_ox=face_ox, shadow=shadow, pose=pose, original=original)

    species.finish = watch
    try:
        im = sprites.build(state, sp, f, stage=stage, dirty=dirty)
    finally:
        species.finish = finish
    return im, seen


def measured(state, sp, f=0, stage="adult", dirty=False):
    """The built frame and its Body: species.finish is watched for the one call that draws the
    frame, to read its regions, face offset and pose."""
    im, seen = _measure(state, sp, f, stage, dirty)
    rest = None
    if f:
        _, r = _measure(state, sp, 0, stage, dirty)
        rest = Body(r["regions"], r["oy"], r["pose"], stage, sp=sp)
    return im, Body(seen["regions"], seen["oy"], seen["pose"], stage, rest, sp)


def loop(state, sp, frames, stage="adult", dirty=False):
    """Every frame of the idle loop with its Body, measured once, for dressing in every look."""
    out = []
    for f in range(frames):
        im, seen = _measure(state, sp, f, stage, dirty)
        rest = out[0][1] if out else None
        out.append((im, Body(seen["regions"], seen["oy"], seen["pose"], stage, rest, sp)))
    return out


def tall(acc, stage):
    """Whether this accessory's picture is the taller hat canvas: an adult in a hat."""
    return ACCESSORIES[acc]["family"] == "hat" and stage == "adult"


def wear(im, acc, body, state, style=None):
    """Accessory `acc` in `style` (None: its default) drawn onto a built frame. An adult in a hat
    comes back on the HAT_CANVAS, the sprite at its foot."""
    all_styles = ACCESSORIES[acc]["styles"]
    draw = all_styles[style] if style is not None else next(iter(all_styles.values()))
    if tall(acc, body.stage):
        out = Image.new("RGBA", (HAT_CANVAS, HAT_CANVAS), (0, 0, 0, 0))
        out.alpha_composite(im, HAT_AT)
        draw(Pen(out, body, state == "frozen", *HAT_AT), body)
        return out
    out = im.copy()
    draw(Pen(out, body, state == "frozen"), body)
    return out


def dressed(state, sp, f=0, stage="adult", dirty=False, acc=None, style=None):
    im, body = measured(state, sp, f, stage, dirty)
    return wear(im, acc, body, state, style) if acc else im


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "."
    os.makedirs(out, exist_ok=True)
    shapes = [(sp, st) for sp in SPECIES for st in ("adult", "baby")]
    every = looks()
    a = os.path.join(out, "accessories_all.png")
    sheet(shapes, every, lambda s, l: dressed("content", s[0], stage=s[1], acc=l[0], style=l[1]),
          [f"{sp} {'bébé' if st == 'baby' else ''}" for sp, st in shapes],
          [(acc[:8] + " " + st[:5]).strip() for acc, st in every], a, K=4)
    faces = [("happy", False), ("content", False), ("sad", False), ("hungry", False), ("starving", False),
             ("frozen", False), ("sleeping", False), ("sick", False), ("content", True)]
    four = ["couronne", "echarpe", "ballon", "lunettes"]
    b = os.path.join(out, "accessories_faces.png")
    sheet(four, faces, lambda acc, fc: dressed(fc[0], "amanite", dirty=fc[1], acc=acc),
          four, [fc[0] + (" sale" if fc[1] else "") for fc in faces], b, K=4)
    # every switched side (the non-default one of a nœud, a fleur or a held item), on all 14 shapes
    switched = [(acc, st) for acc, st in every if acc in DEFAULT_SIDE or ACCESSORIES[acc]["family"] == "held"
                if file_style(acc, st) == st]
    rows = [(sh, face) for sh in shapes for face in ("content", "happy")]
    c = os.path.join(out, "accessories_sides.png")
    sheet(rows, switched, lambda r, l: dressed(r[1], r[0][0], stage=r[0][1], acc=l[0], style=l[1]),
          [f"{r[0][0]} {'bébé' if r[0][1] == 'baby' else ''}\n{r[1]}" for r in rows],
          [f"{acc}\n{st}" for acc, st in switched], c, K=4)
    # Soleil-Levant: every new look on all 14 shapes, then three of them across the faces
    soleil = [(acc, st) for acc, st in every
              if acc in ("onigiri", "sugegasa", "chapeau_chinois", "kanzashi", "eventail", "katana")]
    d = os.path.join(out, "soleil.png")
    sheet(shapes, soleil, lambda s, l: dressed("content", s[0], stage=s[1], acc=l[0], style=l[1]),
          [f"{sp} {'bébé' if st == 'baby' else ''}" for sp, st in shapes],
          [f"{acc[:9]}\n{st}" for acc, st in soleil], d, K=4)
    three = ["katana", "eventail", "sugegasa"]
    e = os.path.join(out, "soleil_faces.png")
    sheet(three, faces, lambda acc, fc: dressed(fc[0], "amanite", dirty=fc[1], acc=acc),
          three, [fc[0] + (" sale" if fc[1] else "") for fc in faces], e, K=4)
    print(c)
    print(a)
    print(b)
    print(d)
    print(e)
