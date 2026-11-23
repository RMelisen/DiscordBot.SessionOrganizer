"""The accessories a Plynling wears, drawn on its own picture (cosmetics, docs/agents/economy.md).

Each accessory is one small drawing and a family that says where it sits: on the cap, over the
eyes, at the neck, on the hands, or held in the left hand. Some come in styles the player picks
(a colour, a position); the first style is the default. Where is measured from the drawing itself
(the cap's real top and rim, the first stem row under it, the arms), so one drawing fits the 7
species and their 7 babies, and follows the breath and the hop. Nothing moves sideways. The
accessory stays clean on a muddy Plynling and takes the ice tint on a frozen one.

    python tools/plynling-art/accessories.py <outdir>     # the two preview sheets
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
    "4": STRAW_L, "5": STRAW, "8": STRAW_D,
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


def on_cap(drawing, at, centre):
    """A small thing tucked on the cap's surface, `at` of the way across it."""
    def draw(pen, b):
        r = b.rest
        left, right = r.cap_span()
        x = int(round(left + (right - left) * at))
        top = r.cap_top_at(x)
        cx, cy = centre
        y = max((top if top is not None else r.cap_top) + 1, cy + HOP)      # clear of the hop
        pen.stamp(drawing, x - cx, y - cy)
    return draw


def held(drawing, grip=None):
    """In the left hand: the drawing's grip lands just outside the arm."""
    gx, gy = grip or (len(drawing[0]) - 1, len(drawing) - 1)

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

# key (the cosmetic key without « cos.accessory. ») -> family and styles; the first style is the
# default, the one shown until the player picks another.
ACCESSORIES = {
    "noeud": dict(family="cap", styles=styles(
        lambda swap: on_cap(recolour(NOEUD, swap), 0.3, (3, 1)), NOEUD_COLOURS.items())),
    "echarpe": dict(family="neck", styles={               # colour, then place: « bleue_haut »
        f"{colour}_{place}": (lambda c, pl: lambda pen, b: echarpe(pen, b, neck_row(b, pl), c))(colour, place)
        for colour in SCARF_COLOURS for place, _ in NECK_PLACES}),
    "fleur": dict(family="cap", styles=styles(
        lambda swap: on_cap(recolour(FLEUR, swap), 0.74, (2, 3)), FLEUR_COLOURS.items())),
    "casquette": dict(family="hat", styles=styles(lambda swap: hat(recolour(CASQUETTE, swap)), CASQUETTE_COLOURS.items())),
    "ballon": dict(family="held", styles=styles(lambda swap: held(recolour(BALLON, swap)), BALLON_COLOURS.items())),
    "lunettes": dict(family="eyes", styles={"": lunettes}),
    "haut_de_forme": dict(family="hat", styles={"": hat(HAUT_DE_FORME)}),
    "baguette": dict(family="held", styles={"": held(BAGUETTE)}),
    "couronne": dict(family="hat", styles={"": hat(COURONNE)}),
    "tulipe": dict(family="held", styles={"": held(TULIPE, TULIPE_GRIP)}),
    "glace": dict(family="held", styles={"": held(GLACE, GLACE_GRIP)}),
    "moufles": dict(family="hands", styles=styles(lambda swap: lambda pen, b: moufles(pen, b, swap), MOUFLE_COLOURS.items())),
    "plume_doree": dict(family="held", styles={"": held(PLUME)}),
    "collier_coquillages": dict(family="neck", styles=styles(lambda p: at_neck(collier_coquillages, p), NECK_PLACES)),
    "medaillon": dict(family="neck", styles=styles(lambda p: at_neck(medaillon, p), NECK_PLACES)),
    "bocal": dict(family="held", styles={"": held(BOCAL, BOCAL_GRIP)}),
    "canne_peche": dict(family="held", styles={"": held(CANNE)}),
    "chapeau_paille": dict(family="hat", styles={"": hat(CHAPEAU_PAILLE)}),
    "bonnet": dict(family="hat", styles=styles(lambda swap: hat(recolour(BONNET, swap)), BONNET_COLOURS.items())),
    "lanterne": dict(family="held", styles={"": held(LANTERNE, LANTERNE_GRIP)}),
    "couronne_fleurs": dict(family="hat", styles={"": hat(COURONNE_FLEURS)}),
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
    print(a)
    print(b)
