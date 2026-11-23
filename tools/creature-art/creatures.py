"""The Combe aux Mousses' creatures, 32×32 in the Plynlings' style: forms lit from the top left and
dithered between tones, a selective outline in each part's own dark tone, glossy eyes, the soft ground
shadow. Drawn by code so a tweak is an edit, like the Plynlings (`../plynling-art`), whose grid,
tone and light helpers this reuses."""
import math
import os
import sys

from PIL import Image

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "plynling-art"))
from common import N, Grid, lerp, tone, lit, INK  # noqa: E402

WHITE = (255, 255, 255)
IRIS = (104, 80, 118)
PINK = (246, 146, 160)
CUTS = (0.78, 0.55, 0.25, -0.05)

FERN = [(176, 222, 128), (132, 192, 96), (92, 154, 72), (62, 116, 58), (40, 80, 44)]
FERN_OUT = (30, 58, 36)


def hashed(x, y, salt=0):
    """A fixed 0..1 value per pixel, for texture that never changes between exports."""
    h = (x * 73856093) ^ (y * 19349663) ^ (salt * 83492791)
    return ((h * 2654435761) & 0xFFFF) / 0xFFFF


class Art:
    def __init__(self):
        self.g = Grid()
        self.ramps, self.outs = {}, {}
        self.glow = {}                      # (x, y) -> rgba, painted only where the drawing is empty

    def part(self, name, ramp, out):
        self.ramps[name], self.outs[name] = ramp, out

    def fill(self, inside, region, light, shift=0):
        ramp = self.ramps[region]
        lx, ly, lrx, lry = light
        for y in range(N):
            for x in range(N):
                if inside(x, y):
                    i = tone(lit(x, y, lx, ly, lrx, lry), CUTS, x, y) + shift
                    self.g.put(x, y, ramp[max(0, min(4, i))], region)

    def ellipse(self, cx, cy, rx, ry, region, clip=None, light=None, shift=0):
        def inside(x, y):
            return (((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1
                    and (clip is None or clip(x, y)))
        self.fill(inside, region, light or (cx, cy, rx, ry), shift)

    def region(self, x, y):
        return self.g.r[y][x] if 0 <= x < N and 0 <= y < N else None

    def px(self, pts, c, region=None):
        for x, y in pts:
            self.g.put(x, y, c, region)

    def line(self, x0, y0, x1, y1, c, region=None):
        n = max(abs(x1 - x0), abs(y1 - y0), 1)
        for k in range(n + 1):
            self.g.put(round(x0 + (x1 - x0) * k / n), round(y0 + (y1 - y0) * k / n), c, region)

    def frond(self, base, ctrl, tip, region, leaf=3.0, every=2):
        """A fern frond along a curve from base to tip, its leaflets shrinking toward the tip."""
        ramp = self.ramps[region]
        pts = []
        for k in range(61):
            t = k / 60
            x = (1 - t) ** 2 * base[0] + 2 * (1 - t) * t * ctrl[0] + t * t * tip[0]
            y = (1 - t) ** 2 * base[1] + 2 * (1 - t) * t * ctrl[1] + t * t * tip[1]
            pts.append((t, x, y))
        seen = set()
        for t, x, y in pts:
            self.g.put(round(x), round(y), ramp[2], region)
        last = None
        for i, (t, x, y) in enumerate(pts[:-1]):
            p = (round(x), round(y))
            if p in seen:
                continue
            seen.add(p)
            if last is not None and abs(p[0] - last[0]) + abs(p[1] - last[1]) < every:
                continue
            last = p
            tx, ty = pts[i + 1][1] - x, pts[i + 1][2] - y
            d = math.hypot(tx, ty) or 1
            tx, ty = tx / d, ty / d
            length = leaf * (1 - t) + 0.8
            for side, shade in ((1, 1), (-1, 2)):
                nx, ny = -ty * side, tx * side
                dx, dy = nx * 0.85 + tx * 0.55, ny * 0.85 + ty * 0.55
                for s in range(1, int(round(length)) + 1):
                    self.g.put(round(x + dx * s), round(y + dy * s), ramp[shade if s < length - 0.5 else 0], region)

    def outline(self):
        self.g.outline(lambda reg, ny: self.outs.get(reg, INK))

    def eye(self, x, y, iris=IRIS):
        """The Plynlings' glossy 2×2 eye: a shine top left, a soft reflection low right."""
        self.px([(x, y), (x + 1, y), (x, y + 1), (x + 1, y + 1)], INK)
        self.g.put(x, y, WHITE)
        self.g.put(x + 1, y + 1, iris)

    def image(self, shadow=None):
        im = Image.new("RGBA", (N, N), (0, 0, 0, 0))
        if shadow:
            x0, x1 = shadow
            for x in range(x0, x1 + 1):
                im.putpixel((x, 29), (0, 0, 0, 58))
            for x in range(x0 + 2, x1 - 1):
                im.putpixel((x, 30), (0, 0, 0, 38))
        for (x, y), c in self.glow.items():
            if 0 <= x < N and 0 <= y < N and self.g.c[y][x] is None:
                im.putpixel((x, y), c)
        for y in range(N):
            for x in range(N):
                if self.g.c[y][x] is not None:
                    im.putpixel((x, y), self.g.c[y][x] + (255,))
        return im


# ---- 1. Le Crapaud-Mousse: a stone that breathes, moss on its back --------------------------

def toad():
    a = Art()
    a.part("skin", [(204, 194, 160), (174, 164, 128), (142, 132, 102), (110, 102, 80), (80, 74, 60)], (50, 44, 40))
    a.part("belly", [(246, 238, 204), (230, 218, 178), (208, 194, 152), (176, 162, 124), (140, 128, 98)], (60, 52, 44))
    a.part("moss", [(200, 234, 128), (154, 208, 94), (112, 170, 74), (78, 130, 60), (52, 94, 48)], (36, 66, 40))
    body = (16, 21.0, 12.4, 8.4)
    for hx in (5.0, 27.0):                                              # the folded hind legs
        a.ellipse(hx, 24.4, 3.8, 3.6, "skin", clip=lambda x, y: y <= 27, light=(hx - 1, 23, 4, 4))
    a.ellipse(*body, "skin", clip=lambda x, y: y <= 26)
    a.ellipse(16, 25.2, 6.6, 3.2, "belly", clip=lambda x, y: a.region(x, y) == "skin", light=(14, 23, 7, 4))
    for ax in (10.0, 22.0):                                             # the front legs, planted
        inside = lambda x, y, ax=ax: ((x + 0.5 - ax) / 2.2) ** 2 + ((y + 0.5 - 25.6) / 2.8) ** 2 <= 1 and y <= 27
        a.fill(inside, "skin", (ax - 0.5, 25, 2.4, 3))
        for y in range(22, 28):
            for x in range(int(ax) - 3, int(ax) + 3):
                if inside(x, y) and not all(inside(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, -1))):
                    a.g.put(x, y, a.ramps["skin"][3 if x < ax else 4])
    for x0, x1 in ((8, 12), (20, 24)):                                  # splayed fingers
        for x in range(x0, x1 + 1, 2):
            a.g.put(x, 28, a.ramps["skin"][1], "skin")
    # the moss: cushions on the dome's top, each lit on its own
    for mx, my, mrx, mry in ((11.5, 14.6, 4.6, 2.8), (18.5, 13.8, 5.0, 3.0), (24.0, 16.0, 3.6, 2.4),
                             (15.0, 16.6, 3.4, 1.8), (7.0, 17.2, 2.6, 1.8)):
        a.ellipse(mx, my, mrx, mry, "moss", light=(mx - 0.5, my - 0.5, mrx, mry))
    for y in range(N):
        for x in range(N):
            if a.region(x, y) == "moss" and hashed(x, y, 1) > 0.84:
                a.g.put(x, y, a.ramps["moss"][4 if hashed(x, y, 2) > 0.5 else 0])
    for ex in (9.5, 22.5):                                              # eye bumps through the moss
        a.ellipse(ex, 16.2, 3.6, 3.0, "skin", light=(ex - 1, 15, 3.6, 3.0))
    a.outline()
    gold, gold_d, gold_hi = (248, 200, 76), (204, 142, 40), (255, 236, 150)
    for ex in (8, 21):                                                  # half-closed, golden, unimpressed
        a.px([(ex, 15), (ex + 1, 15), (ex + 2, 15), (ex + 3, 15)], a.ramps["skin"][3])
        a.px([(ex, 16), (ex + 1, 16), (ex + 2, 16), (ex + 3, 16)], INK)
        a.px([(ex, 17), (ex + 3, 17)], gold_d)
        a.px([(ex + 1, 17), (ex + 2, 17)], INK)
        a.px([(ex, 18), (ex + 1, 18), (ex + 2, 18), (ex + 3, 18)], gold)
        a.g.put(ex + 3, 18, gold_hi)
    a.px([(x, 21) for x in range(10, 22)], INK)                         # the wide, content mouth
    a.px([(9, 20), (22, 20)], INK)
    a.px([(14, 20), (17, 20)], a.ramps["skin"][4])                      # nostrils
    a.px([(8, 21), (23, 21)], (232, 170, 164))
    for x, y in ((6, 20), (26, 21), (4, 23), (28, 23), (12, 23), (20, 23)):   # warts
        if a.region(x, y) == "skin":
            a.g.put(x, y, a.ramps["skin"][0])
            if a.region(x + 1, y + 1) == "skin":
                a.g.put(x + 1, y + 1, a.ramps["skin"][3])
    return a.image(shadow=(2, 29))


# ---- 2. Les Phalènes-Lanternes: moths with softly glowing green wings ------------------------

def moths():
    a = Art()
    wing = [(248, 255, 232), (216, 250, 202), (178, 236, 172), (132, 204, 142), (90, 156, 112)]
    a.part("wing", wing, (50, 104, 78))
    a.part("body", [(255, 252, 240), (240, 234, 216), (214, 206, 184), (178, 168, 146), (138, 128, 110)], (84, 76, 66))

    def glowing(cx, cy, rx, ry, angle=0.0):
        ca, sa = math.cos(angle), math.sin(angle)
        for y in range(N):
            for x in range(N):
                dx, dy = x + 0.5 - cx, y + 0.5 - cy
                u, v = (dx * ca + dy * sa) / rx, (-dx * sa + dy * ca) / ry
                rr = u * u + v * v
                if rr <= 1:
                    # a lantern: brightest at its heart, deeper toward the rim
                    i = 0 if rr < 0.16 else 1 if rr < 0.44 else 2 if rr < 0.74 else 3
                    if 0.64 < rr < 0.84 and (x + y) % 2:
                        i = 3 if i == 2 else i
                    a.g.put(x, y, wing[i], "wing")

    # the tails first, trailing from the hind wings
    for side in (-1, 1):
        for x, y in ((11, 21), (11, 22), (10, 23), (10, 24), (9, 25), (9, 26), (8, 27)):
            for w in (0, -1):
                xx = x + w if side < 0 else 31 - (x + w)
                a.g.put(xx, y, wing[2 if w == 0 else 3], "wing")
        a.px([(7, 28), (8, 28)] if side < 0 else [(24, 28), (23, 28)], wing[1], "wing")
    glowing(9.2, 10.8, 7.6, 4.4, -0.55)
    glowing(22.8, 10.8, 7.6, 4.4, 0.55)
    glowing(11.0, 18.0, 4.6, 3.6, 0.5)
    glowing(21.0, 18.0, 4.6, 3.6, -0.5)
    a.ellipse(16, 15.0, 2.3, 6.6, "body")
    a.ellipse(16, 8.4, 2.8, 2.3, "body")
    for (mx, my) in ((4, 3), (27, 2)):                         # two small ones, further off
        a.px([(mx - 2, my), (mx - 1, my), (mx - 1, my + 1), (mx + 1, my), (mx + 2, my), (mx + 1, my + 1)],
             wing[1], "wing")
        a.px([(mx - 2, my), (mx + 2, my)], wing[0], "wing")
        a.px([(mx, my), (mx, my + 1)], a.ramps["body"][2], "body")
    a.outline()
    for side in (-1, 1):                                       # veins, and a ringed eyespot per wing
        for x, y in ((13, 12), (12, 11), (11, 10), (13, 16), (12, 17)):
            a.g.put(x if side < 0 else 31 - x, y, wing[2])
        for ex, ey, ring in ((7, 10, True), (10, 18, False)):
            xx = ex if side < 0 else 31 - ex - 1
            pts = [(xx, ey), (xx + 1, ey), (xx, ey + 1), (xx + 1, ey + 1)]
            a.px(pts, wing[4])
            a.g.put(xx + (0 if side < 0 else 1), ey, (255, 236, 160) if ring else wing[3])
    for y in (12, 14, 16, 18):                                 # the furry body's bands
        a.g.put(16, y, a.ramps["body"][2])
    ant = (176, 150, 112)
    for x, y in ((14, 6), (13, 5), (12, 4), (11, 3), (17, 6), (18, 5), (19, 4), (20, 3)):
        a.g.put(x, y, ant)                                     # feathered antennae
    for x, y in ((12, 5), (11, 4), (13, 4), (19, 5), (20, 4), (18, 4)):
        a.g.put(x, y, (214, 192, 150))
    for ex in (14, 17):                                        # small glossy eyes
        a.g.put(ex, 8, WHITE)
        a.g.put(ex, 9, INK)
    a.px([(14, 8), (17, 8)], INK)
    a.px([(14, 9), (17, 9)], INK)
    a.px([(14, 8), (17, 8)], INK)
    a.g.put(14, 8, WHITE)
    a.g.put(17, 8, WHITE)
    a.px([(13, 9), (18, 9)], (250, 196, 196))
    # the glow: a soft halo round the big one, and two small ones further off
    halo = (200, 250, 176)
    for y in range(N):
        for x in range(N):
            if a.g.c[y][x] is None:
                d = min((abs(x - xx) + abs(y - yy) for yy in range(max(0, y - 3), min(N, y + 4))
                         for xx in range(max(0, x - 3), min(N, x + 4)) if a.region(xx, yy) == "wing"), default=9)
                if d <= 1:
                    a.glow[(x, y)] = halo + (80,)
                elif d == 2 and (x + y) % 2 == 0:
                    a.glow[(x, y)] = halo + (44,)
    return a.image()


# ---- 3. Le Siffleur: round as an apple, brown as a dead leaf ----------------------------------

def whistler():
    a = Art()
    a.part("brown", [(222, 176, 118), (196, 146, 90), (162, 112, 66), (124, 82, 50), (88, 56, 38)], (60, 38, 30))
    a.part("cream", [(250, 236, 206), (238, 216, 178), (218, 190, 148), (186, 156, 116), (148, 120, 88)], (96, 70, 52))
    a.part("wing", [(186, 136, 84), (156, 108, 64), (128, 86, 52), (98, 64, 40), (70, 46, 32)], (54, 34, 28))
    a.part("beak", [(244, 196, 112), (224, 166, 80), (194, 132, 58), (152, 98, 44), (110, 70, 36)], (80, 50, 30))
    a.part("fern", FERN, FERN_OUT)
    body = (15.5, 16.8, 9.0, 8.4)
    a.part("moss", [(200, 234, 128), (154, 208, 94), (112, 170, 74), (78, 130, 60), (52, 94, 48)], (36, 66, 40))
    a.frond((24, 29), (31, 22), (30, 8), "fern", leaf=2.8)                   # ferns behind it
    a.frond((6, 29), (1, 24), (1, 16), "fern", leaf=2.4)
    a.ellipse(15.5, 28.8, 10.5, 3.0, "moss", clip=lambda x, y: y <= 28, shift=1)   # the mossy hump it sits on
    # the cocked tail, barred, then the round body over it
    for k in range(7):
        for w in (0, 1, 2):
            x, y = 22 + w + k // 2, 16 - k + (1 if w == 2 else 0)
            a.g.put(x, y, a.ramps["wing"][4 if k % 2 else (1 if w == 0 else 2)], "wing")
    a.ellipse(*body, "brown")
    a.ellipse(12.8, 20.4, 6.2, 5.0, "cream", clip=lambda x, y: a.region(x, y) == "brown", light=body)
    a.ellipse(19.5, 17.2, 5.6, 4.4, "wing", clip=lambda x, y: a.region(x, y) == "brown", light=(18, 14, 6, 5))
    a.ellipse(13.0, 9.8, 6.0, 2.6, "brown", clip=lambda x, y: a.region(x, y) == "brown", light=body, shift=1)
    a.px([(7, 15), (6, 15), (5, 16), (6, 16), (7, 16)], a.ramps["beak"][1], "beak")
    a.g.put(5, 16, a.ramps["beak"][2])
    a.outline()
    for x in range(17, 24):                                   # wing bars
        for y in (16, 18):
            if a.region(x, y) == "wing" and (x + y) % 2:
                a.g.put(x, y, a.ramps["wing"][4])
    for x, y in ((9, 11), (10, 11), (11, 10), (12, 10), (13, 10), (14, 11), (15, 11)):   # the pale brow
        a.g.put(x, y, a.ramps["cream"][1])
    a.eye(10, 12)
    a.g.put(6, 16, a.ramps["beak"][4])                         # shut: it whistles without opening it
    for x, y in ((13, 25), (12, 26), (13, 26), (18, 25), (18, 26), (19, 26)):
        a.g.put(x, y, (110, 70, 36))
    for y in range(N):
        for x in range(N):
            if a.region(x, y) == "moss" and hashed(x, y, 3) > 0.8:
                a.g.put(x, y, a.ramps["moss"][0])
    a.px([(11, 17), (12, 17)], PINK)
    for x, y in ((4, 4), (4, 5), (4, 6), (4, 7), (2, 7), (3, 7), (2, 8), (3, 8), (5, 4), (6, 5)):  # a note
        a.g.put(x, y, (240, 232, 214))
    return a.image(shadow=(4, 27))


# ---- 4. Le Hérisson-Châtaigne: spines like chestnut husks --------------------------------------

def hedgehog():
    a = Art()
    a.part("spine", [(222, 170, 102), (188, 132, 72), (152, 100, 54), (114, 72, 40), (80, 50, 30)], (54, 34, 26))
    a.part("skin", [(252, 230, 196), (238, 208, 168), (216, 182, 140), (182, 148, 110), (142, 110, 82)], (100, 72, 54))
    a.part("foot", [(150, 110, 84), (126, 90, 68), (104, 72, 54), (82, 56, 44), (60, 40, 32)], (44, 30, 26))
    cx, cy, rx, ry = 18.5, 19.0, 10.6, 8.6

    def spiky(x, y):
        dx, dy = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
        th = math.atan2(dy, dx)
        rr = math.hypot(dx, dy)
        tri = abs(((th * 15 / (2 * math.pi)) % 1) - 0.5) * 2         # 0 at a spike's tip, 1 between
        reach = 1 + (0.34 * (1 - tri) ** 1.5 if dy < 0.3 else 0)
        return rr <= reach and y <= 26

    a.fill(spiky, "spine", (cx, cy, rx, ry))
    for y in range(N):                                     # husk texture: pale spines, dark seams between
        for x in range(N):
            if a.region(x, y) == "spine":
                dx, dy = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
                if math.hypot(dx, dy) < 0.3:
                    continue
                th = math.atan2(dy, dx)
                tri = abs(((th * 15 / (2 * math.pi) + 0.5) % 1) - 0.5) * 2
                if tri < 0.2:
                    a.g.put(x, y, a.ramps["spine"][0 if dy < 0 else 1])
                elif tri > 0.86:
                    a.g.put(x, y, a.ramps["spine"][4])
    a.ellipse(10.0, 21.0, 6.6, 5.0, "skin")                          # the face
    a.ellipse(12.6, 16.4, 1.8, 1.8, "skin", light=(12, 16, 2, 2))   # the ear
    a.ellipse(14.0, 25.0, 6.0, 2.6, "skin", clip=lambda x, y: y <= 26, light=(10, 21, 7, 5))
    for fx in (9.5, 16.5, 23.5):
        a.ellipse(fx, 27.2, 2.2, 1.4, "foot", clip=lambda x, y: y <= 27)
    a.outline()
    a.px([(3, 21), (3, 22), (2, 21), (2, 22)], INK)                  # the nose
    a.g.put(2, 21, (120, 110, 120))
    a.eye(7, 18)
    a.px([(8, 23), (9, 23)], PINK)
    a.px([(4, 23), (5, 24), (6, 24), (7, 23)], INK)                  # a small, pleased mouth
    a.g.put(12, 16, PINK)
    return a.image(shadow=(4, 29))


# ---- 5. Le Lucane-Écorce: a bark-coloured stag beetle the size of a loaf ----------------------

def stag_beetle():
    a = Art()
    a.part("bark", [(190, 160, 124), (160, 130, 98), (128, 102, 76), (96, 76, 58), (66, 52, 42)], (42, 32, 28))
    a.part("horn", [(214, 140, 92), (180, 108, 66), (146, 80, 50), (110, 58, 38), (78, 40, 30)], (52, 28, 24))
    a.part("leg", [(110, 90, 72), (92, 74, 60), (76, 60, 50), (60, 46, 40), (44, 34, 30)], (32, 24, 22))
    # legs first, so the body sits over their roots
    for side in (-1, 1):
        for (x0, y0), (x1, y1), (x2, y2) in (((12, 14), (8, 12), (5, 9)), ((11, 19), (6, 19), (4, 22)),
                                             ((11, 24), (7, 26), (6, 28))):
            sx = lambda v: v if side < 0 else 31 - v
            a.line(sx(x0), y0, sx(x1), y1, a.ramps["leg"][2], "leg")
            a.line(sx(x1), y1, sx(x2), y2, a.ramps["leg"][3], "leg")
    a.ellipse(16, 21.5, 8.0, 7.2, "bark", clip=lambda x, y: y <= 28)             # wing cases
    a.ellipse(16, 12.6, 6.6, 3.2, "bark", light=(15, 12, 7, 3.4), shift=0)       # the shield
    a.ellipse(16, 9.0, 5.4, 2.6, "bark", light=(15, 8.5, 6, 3), shift=1)         # the head
    # the mandibles: thick pincers curving up and in, a tooth on the inside of each
    h = a.ramps["horn"]
    left = {(12, 6): 2, (13, 6): 3, (11, 5): 1, (12, 5): 2, (10, 4): 1, (11, 4): 2, (10, 3): 0, (11, 3): 2,
            (10, 2): 1, (11, 2): 1, (11, 1): 1, (12, 1): 2, (13, 2): 3, (12, 4): 3, (13, 4): 3}
    for (x, y), i in left.items():
        a.g.put(x, y, h[i], "horn")
        a.g.put(31 - x, y, h[min(4, i + 1)], "horn")
    a.outline()
    for y in range(14, 29):                                                       # bark grooves
        for x in range(9, 24):
            if a.region(x, y) == "bark" and y > 14:
                if x == 16:
                    a.g.put(x, y, a.ramps["bark"][4])
                elif x % 3 == 0 and hashed(x, y, 5) > 0.35:
                    a.g.put(x, y, a.ramps["bark"][3])
                elif hashed(x, y, 6) > 0.93:
                    a.g.put(x, y, (176, 196, 150))                                # lichen
    for x, y in ((12, 16), (12, 17), (11, 18), (19, 16), (19, 17)):              # a waxy sheen
        a.g.put(x, y, a.ramps["bark"][0])
    a.eye(12, 8)
    a.eye(18, 8)
    return a.image(shadow=(5, 26))


# ---- 6. Le Furet des Fougères: long, russet, whiskered, sitting up to stare -------------------

def ferret():
    a = Art()
    a.part("fur", [(244, 176, 112), (226, 142, 80), (194, 106, 58), (150, 76, 42), (106, 52, 32)], (74, 36, 26))
    a.part("cream", [(255, 246, 226), (248, 232, 204), (232, 210, 176), (204, 178, 142), (166, 138, 106)], (110, 80, 60))
    a.part("dark", [(150, 96, 70), (122, 74, 54), (98, 58, 44), (76, 44, 36), (56, 32, 28)], (40, 24, 22))
    # the bushy tail curled round its feet, behind the body
    a.ellipse(23.5, 25.5, 6.0, 2.8, "fur", clip=lambda x, y: y <= 28, light=(22, 23, 6, 3))
    a.ellipse(28.0, 25.2, 2.4, 2.2, "dark", clip=lambda x, y: a.region(x, y) == "fur")
    a.ellipse(16.5, 20.0, 5.8, 8.2, "fur", clip=lambda x, y: y <= 27)
    a.ellipse(14.6, 20.5, 3.0, 6.2, "cream", clip=lambda x, y: a.region(x, y) == "fur", light=(14, 18, 4, 6))
    a.ellipse(14.0, 9.4, 5.2, 4.3, "fur")                                         # the head
    a.ellipse(17.5, 5.2, 1.8, 1.8, "fur", light=(17, 5, 2, 2))                     # the ears
    a.ellipse(12.2, 5.4, 1.6, 1.6, "fur", light=(12, 5, 2, 2), shift=1)
    a.ellipse(10.6, 10.6, 3.2, 2.1, "cream", light=(11, 9, 4, 3))                 # the muzzle
    a.ellipse(13.5, 15.8, 1.6, 1.3, "cream", light=(13, 15, 2, 2))                 # the paws
    a.ellipse(16.2, 16.2, 1.6, 1.3, "cream", light=(16, 15, 2, 2))
    for fx in (13.0, 19.0):
        a.ellipse(fx, 27.6, 2.2, 1.2, "dark", clip=lambda x, y: y <= 28)
    a.outline()
    for x, y in ((11, 8), (14, 8), (12, 7), (13, 7), (15, 9), (11, 9)):           # the mask
        if a.region(x, y) == "fur":
            a.g.put(x, y, a.ramps["dark"][2])
    a.eye(12, 8)
    a.px([(7, 10), (8, 10)], INK)                                                 # the nose
    a.g.put(17, 5, PINK)
    for x, y in ((6, 9), (5, 9), (4, 8), (3, 8), (6, 11), (5, 11), (4, 12), (3, 12)):   # the whiskers
        a.g.put(x, y, (226, 214, 200))
    a.g.put(12, 5, PINK)
    a.g.put(10, 12, INK)
    a.g.put(11, 12, INK)
    return a.image(shadow=(8, 30))


# ---- 7. L'Escargot-Cloche: a copper shell that rings, a silver trail --------------------------

def snail():
    a = Art()
    a.part("shell", [(255, 220, 168), (240, 170, 100), (206, 122, 64), (160, 82, 44), (110, 54, 32)], (74, 36, 26))
    a.part("body", [(238, 228, 212), (216, 202, 182), (188, 172, 152), (152, 136, 120), (114, 100, 90)], (80, 68, 64))
    def foot(x, y):                                                               # tapering to a tail behind
        if not 3 <= x <= 28:
            return False
        top = 23.0 + max(0.0, (x - 9) / 19) * 2.8
        return top <= y + 0.5 <= 27.6
    a.fill(foot, "body", (14, 26.5, 17, 5))
    a.ellipse(6.4, 21.2, 3.4, 4.6, "body", light=(6, 20, 4, 5))                    # the head, raised
    a.px([(2, 25), (3, 25)], a.ramps["body"][2], "body")                         # the lower feelers
    a.ellipse(18.5, 15.5, 8.4, 8.2, "shell")
    a.outline()
    # the spiral, wound in from the rim
    cx, cy = 18.8, 15.6
    for k in range(260):
        t = k / 260 * 2.4 * 2 * math.pi
        r = 6.6 - t / (2 * math.pi) * 2.6
        if r < 0.6:
            break
        x, y = round(cx + r * math.cos(t + 2.4) - 0.5), round(cy + r * math.sin(t + 2.4) - 0.5)
        if a.region(x, y) == "shell":
            a.g.put(x, y, a.ramps["shell"][4 if r > 3 else 3])
    a.px([(13, 10), (14, 9), (15, 9)], (255, 246, 224))                           # the bell's glint
    stalk = a.outs["body"]
    a.line(5, 17, 4, 12, stalk)                                                   # eye stalks
    a.line(7, 17, 9, 13, stalk)
    a.eye(3, 10)
    a.eye(8, 11)
    a.px([(5, 22), (6, 22)], INK)                                                 # a slow smile
    a.g.put(4, 21, INK)
    a.g.put(7, 21, PINK)
    a.g.put(3, 21, PINK)
    for x, y, c in ((28, 6, 0), (29, 5, 1), (29, 8, 0), (30, 8, 1), (27, 3, 1), (28, 2, 0)):   # the ring
        a.g.put(x, y, ((250, 230, 150), (255, 248, 206))[c])
    for x in range(27, 32):                                                       # the silver trail
        a.glow[(x, 27)] = (214, 226, 244, 170 - (x - 27) * 24)
    for x, y in ((28, 27), (30, 27), (29, 26)):
        a.glow[(x, y)] = (250, 252, 255, 255)
    return a.image(shadow=(3, 26))


# ---- 8. Le Vieux Blaireau gris: the Combe's old keeper -----------------------------------------

def badger():
    a = Art()
    a.part("grey", [(214, 214, 220), (184, 184, 192), (150, 150, 160), (116, 116, 128), (84, 84, 96)], (52, 52, 62))
    a.part("dark", [(104, 98, 106), (80, 76, 84), (60, 56, 64), (44, 42, 50), (30, 28, 36)], (22, 20, 28))
    a.part("white", [(255, 255, 255), (244, 244, 246), (224, 224, 230), (196, 196, 206), (160, 160, 172)], (70, 70, 82))
    a.ellipse(16, 23.6, 14.0, 6.4, "grey", clip=lambda x, y: y <= 27)              # the body, lying down
    a.ellipse(16, 23.0, 7.4, 4.6, "dark", clip=lambda x, y: y <= 27, light=(14, 21, 8, 5))   # the black chest
    for fx in (11.5, 20.5):                                                         # front legs, black
        a.ellipse(fx, 26.0, 2.9, 2.8, "dark", clip=lambda x, y: y <= 28, light=(fx - 1, 24, 3, 3))
    for ex in (8.8, 23.2):                                                          # the ears
        a.ellipse(ex, 7.0, 2.0, 1.9, "white", light=(ex, 6, 3, 3))

    def head(x, y):
        if not 4 <= y <= 21:
            return False
        hw = 8.0 * math.sqrt(max(0.0, 1 - ((y + 0.5 - 11.5) / 7.2) ** 2)) if y < 12 else 8.0 - (y - 11.5) * 0.62
        return abs(x + 0.5 - 16) <= hw

    a.fill(head, "white", (16, 12, 8, 8))
    for y in range(4, 22):                                                          # the two dark stripes
        for x in range(N):
            if a.region(x, y) == "white" and y >= 5:
                d = abs(x + 0.5 - 16)
                inner = 1.6 if y > 9 else 1.8
                outer = inner + (3.2 if y < 15 else 3.2 - (y - 14) * 0.55)
                if inner < d <= outer:
                    i = 2 if x < 16 else 3
                    if (x + y) % 2 and d > outer - 1:
                        i += 1
                    a.g.put(x, y, a.ramps["dark"][i], "dark")
    for ex in (8, 23):
        a.g.put(ex + (1 if ex < 16 else 0), 7, a.ramps["dark"][3], "dark")
    a.outline()
    for ex in (11, 19):                                                             # old, kind, half-shut
        a.px([(ex, 12), (ex + 1, 12)], (150, 146, 160))
        a.px([(ex, 13), (ex + 1, 13)], INK)
        a.g.put(ex if ex < 16 else ex + 1, 13, WHITE)
    a.px([(15, 18), (16, 18), (15, 19), (16, 19), (14, 18), (17, 18)], INK)        # the big nose
    a.g.put(15, 18, (130, 126, 140))
    a.px([(10, 15), (21, 15)], (240, 176, 184))
    for x in (10, 12, 19, 21):                                                     # claws
        a.g.put(x, 28, (226, 220, 206))
    return a.image(shadow=(2, 29))


# ---- the silhouette: a creature not met yet, something in the ferns ---------------------------

def unknown():
    a = Art()
    a.part("shade", [(92, 96, 118), (80, 84, 104), (70, 74, 92), (60, 64, 80), (52, 54, 70)], (34, 36, 48))
    a.part("fern", [(120, 150, 120), (96, 128, 100), (76, 106, 84), (60, 86, 70), (46, 66, 56)], (30, 44, 38))
    a.ellipse(16, 18.4, 9.6, 9.2, "shade", clip=lambda x, y: y <= 27)
    a.frond((9, 29), (3, 22), (1, 13), "fern", leaf=2.8)
    a.frond((23, 29), (29, 22), (31, 14), "fern", leaf=2.8)
    a.frond((14, 29), (11, 25), (7, 19), "fern", leaf=2.4)
    a.frond((18, 29), (21, 25), (25, 20), "fern", leaf=2.4)
    a.outline()
    for ex in (12, 18):
        a.px([(ex, 17), (ex + 1, 17)], (255, 236, 150))
        a.g.put(ex, 16, (255, 252, 226))
    return a.image(shadow=(5, 27))


CREATURES = [
    ("toad", "Crapaud-Mousse", toad),
    ("moths", "Phalènes-Lanternes", moths),
    ("whistler", "Siffleur", whistler),
    ("hedgehog", "Hérisson-Châtaigne", hedgehog),
    ("stag", "Lucane-Écorce", stag_beetle),
    ("ferret", "Furet des Fougères", ferret),
    ("snail", "Escargot-Cloche", snail),
    ("badger", "Vieux Blaireau gris", badger),
    ("unknown", "??? (pas encore vu)", unknown),
]
