"""One drawing function per mushroom species: each has its own silhouette, and all of them
wear the shared face and extras from sprites.py. sprites.build(state, sp) dispatches here
through DRAW.

The Cèpe is the original sprite, unchanged. The other five were reshaped after real species:
the fly agaric's skirt, the field mushroom's button cap, the green russula's flat cap, a
Mycena's bell, a chanterelle's funnel. Their face is clipped to the body so a narrow stem
never has blush floating beside it, and their sweat drops and frost are measured from the
model itself rather than from the Cèpe's coordinates.
"""
import math

from PIL import Image

from common import N, Grid, lerp, tone, SPECIES, SPOTS, STEM, STEM_OUT, INK
from motion import moved, pose as pose_for
from sprites import face, extras, star, sweat_drops, PALE, TEAR, TEAR_HI

ICE, SNOW, FROST_TINT = (206, 242, 255), (255, 255, 255), (176, 226, 255)
CX = 16.0                    # the face's own axis: eyes at 12-13 and 18-19, feet likewise


def stem_palette(p):
    """STEM, leaned toward the species' own colour when it has one (a gold chanterelle)."""
    tint = p.get("stem_tint")
    return STEM if tint is None else [lerp(c, tint[0], tint[1]) for c in STEM]


# ---- the Cèpe: the original sprite, unchanged ------------------------------------------

def cepe(state, frame=0, shadow=True, stage="adult"):
    """The original Plynling: a broad cap, gills, a chubby body. The adult is the original
    sprite, untouched; a baby is drawn by cepe_baby, and every other stage wears the adult."""
    if stage == "baby":
        return cepe_baby(state, frame, shadow)
    p = SPECIES["cepe"]
    cap = p["cap"]
    pose = pose_for("cepe", state, frame)
    dy = 0                                      # the breath is applied by motion.moved
    sag = 1 if state == "starving" else 0       # a starving Plynling's cap sags onto it
    g = Grid()

    for y, (a, b) in ((15 + dy + sag, (3, 28)), (16 + dy + sag, (6, 25))):     # gills
        for x in range(a, b + 1):
            c = p["gill"]
            if (x - 16) % 2 == 0:
                c = lerp(c, cap[4], 0.25)
            if abs(x + 0.5 - 16) < 7:
                c = lerp(c, cap[4], 0.3)
            g.put(x, y, c, "cap")

    spans = {16: (12, 19), 17: (11, 20), 26: (11, 20), 27: (12, 19)}             # body
    for y in range(16 + dy, 28):
        x0, x1 = spans[y - dy] if y - dy in (16, 17) else (spans[y] if y in (26, 27) else (10, 21))
        for x in range(x0, x1 + 1):
            t = (x + 0.5 - x0) / (x1 + 1 - x0)
            nx = 2 * t - 1
            nz = math.sqrt(max(0.0, 1 - nx * nx))
            i = tone(nx * -0.6 + nz * 0.8, (0.86, 0.58, 0.2, -0.2), x, y)
            if y <= 17 + dy + sag or y >= 26:
                i = min(4, i + 1)
            g.put(x, y, STEM[i], "stem")
    for y in (21 + dy, 22 + dy):
        g.put(9, y, STEM[2], "stem")
        g.put(22, y, STEM[3], "stem")
    for x in (12, 13, 18, 19):
        g.put(x, 28, STEM[3], "stem")

    cx, cy, rx, ry = 15.5, 14.0 + dy + sag, 14.6 + 0.9 * pose.widen, 11.4       # cap, wider on the out-breath
    for y in range(N):
        for x in range(N):
            if y > 14 + dy + sag:
                continue
            rr = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
            if rr > 1:
                continue
            d = ((x - 10) / 8.5) ** 2 + ((y - (6.5 + dy + sag)) / 5.2) ** 2
            i = 0 if d < 0.16 else (1 if d < 0.55 else 2)
            far = x > cx or y > cy - 3
            if rr > 0.78 and far:
                i = max(i, 3)
            elif 0.6 < rr <= 0.78 and far and (x + y) % 2 == 0:
                i = max(i, 3)
            if y >= 13 + dy + sag:
                i = max(i, 3)
            g.put(x, y, cap[i], "cap")
    for sx, sy, r in p["spots"]:
        for y in range(N):
            for x in range(N):
                oy = sy + dy + sag
                if (x - sx) ** 2 + (y - oy) ** 2 <= r * r and g.r[y][x] == "cap" and y < 13 + dy + sag:
                    g.put(x, y, p["spot"][1] if (x - sx) + (y - oy) > r * 0.55 else p["spot"][0])

    g.outline(lambda reg, ny: INK if ny >= 27 else (cap[4] if reg == "cap" else STEM_OUT))
    # the Cèpe keeps its original face (unclipped) and its original, fixed extras
    return finish(g, p, state, STEM, shadow=shadow, pose=pose, original=True)

# The baby Cèpe, body shape only: a small, squat button — a big round cap sitting low over a
# stubby body with no feet, the face set lower. It goes through finish() like the reshaped
# species (clipped face, measured sweat and frost), since the original's fixed extras were
# placed for the adult's silhouette. The other life stages wear the adult sprite.
BABY_TOP, BABY_BOTTOM, BABY_SPAN, BABY_ARMS = 20, 28, (11, 20), (24, 25)
BABY_CAP, BABY_CUT = (18.5, 11.6, 11.0), 18                        # cap: centre row, x and y radii


def cepe_baby(state, frame, shadow):
    p = SPECIES["cepe"]
    cap = p["cap"]
    pose = pose_for("cepe", state, frame)
    sag = 1 if state == "starving" else 0
    x0, x1 = BABY_SPAN
    g = Grid()

    stem(g, STEM, x0, x1, BABY_TOP, BABY_BOTTOM)
    for y in BABY_ARMS:
        g.put(x0 - 1, y, STEM[2], "stem")
        g.put(x1 + 1, y, STEM[3], "stem")

    cy, rx, ry = BABY_CAP
    cy += sag
    rx += 0.9 * pose.widen                                           # wider on the out-breath
    cut = BABY_CUT + sag
    gills(g, p, cut + 1, x0 - 4, x1 + 4)
    for y in range(cut + 1):
        for x in range(N):
            if ((x + 0.5 - 15.5) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1:
                g.put(x, y, shade_cap(x, y, cap, 15.5, cy, rx, ry, 10, cy - 7.5, 8.5, 5.2, cut - 1), "cap")
    for sx, sy, r in p["spots"]:                                      # the scales, moved with the cap
        oy = sy + (cy - 14.0)
        for y in range(N):
            for x in range(N):
                if (x - sx) ** 2 + (y - oy) ** 2 <= r * r and g.r[y][x] == "cap" and y < cut - 1:
                    g.put(x, y, p["spot"][1] if (x - sx) + (y - oy) > r * 0.55 else p["spot"][0])

    g.outline(lambda reg, ny: INK if ny >= 27 else (cap[4] if reg == "cap" else STEM_OUT))
    return finish(g, p, state, STEM, face_oy=BABY_FACE_OY, shadow=shadow, pose=pose)


# ---- shared pieces for the reshaped species -------------------------------------------

def stem(g, S, x0, x1, y0, y1, region="stem"):
    """A lit cylinder from row y0 to y1, its corners rounded top and bottom."""
    for y in range(y0, y1 + 1):
        a, b = (x0 + 1, x1 - 1) if y in (y0, y1) else (x0, x1)
        for x in range(a, b + 1):
            t = (x + 0.5 - a) / (b + 1 - a)
            nx = 2 * t - 1
            i = tone(nx * -0.6 + math.sqrt(max(0.0, 1 - nx * nx)) * 0.8, (0.86, 0.58, 0.2, -0.2), x, y)
            if y <= y0 + 1 or y >= y1:
                i = min(4, i + 1)
            g.put(x, y, S[i], region)


def shade_row(g, S, y, a, b, region, extra=0):
    for x in range(a, b + 1):
        t = (x + 0.5 - a) / (b + 1 - a)
        nx = 2 * t - 1
        i = tone(nx * -0.6 + math.sqrt(max(0.0, 1 - nx * nx)) * 0.8, (0.86, 0.58, 0.2, -0.2), x, y)
        g.put(x, y, S[min(4, i + extra)], region)


def feet(g, S, xs=(12, 13, 18, 19), y=28):
    for x in xs:
        g.put(x, y, S[3], "stem")


def shade_cap(x, y, cap, cx, cy, rx, ry, lit_x, lit_y, lit_rx, lit_ry, under_y):
    """A cap's shading: a soft sheen top-left, dithered into shadow on the far side."""
    rr = ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2
    d = ((x - lit_x) / lit_rx) ** 2 + ((y - lit_y) / lit_ry) ** 2
    i = 0 if d < 0.16 else (1 if d < 0.55 else 2)
    far = x + 0.5 > cx or y > cy - 3
    if rr > 0.78 and far:
        i = max(i, 3)
    elif 0.6 < rr <= 0.78 and far and (x + y) % 2 == 0:
        i = max(i, 3)
    if y >= under_y:
        i = max(i, 3)
    return cap[i]


def gills(g, p, y, a, b, lift=None):
    """One row of gills; `lift(x)` moves a column up or down to follow a tipped cap."""
    cap = p["cap"]
    for x in range(a, b + 1):
        c = p["gill"]
        if (x - 16) % 2 == 0:
            c = lerp(c, cap[4], 0.25)
        if abs(x + 0.5 - 16) < 6:
            c = lerp(c, cap[4], 0.3)
        g.put(x, y + (lift(x) if lift else 0), c, "cap")


def outline(g, cap, extra=None):
    out = {"cap": cap[4], "ring": STEM_OUT}
    if extra:
        out.update(extra)
    g.outline(lambda reg, ny: INK if ny >= 29 else out.get(reg, STEM_OUT))


# Every baby shares one frame, the Cèpe's (cepe_baby): a stubby body from row 20 down to a round
# stub at 28 (no feet), the cap ending at row 18 with its underside at 19, the face 3 rows low.
# Each species then draws its own smaller cap into it, so a baby still reads as its species.
BABY_FACE_OY = 3


def baby_body(g, S, top=20):
    stem(g, S, 11, 20, top, 28)


def scaled(spots, c0, c1, kx, ky):
    """Spots placed on an adult cap centred at c0, moved onto a baby cap centred at c1."""
    return [(c1[0] + (sx - c0[0]) * kx, c1[1] + (sy - c0[1]) * ky, r * min(kx, ky)) for sx, sy, r in spots]


def cap_profile(g):
    """Per column: the top and the bottom row of the cap, where there is one."""
    tops, bottoms = {}, {}
    for x in range(N):
        ys = [y for y in range(N) if g.r[y][x] == "cap"]
        if ys:
            tops[x], bottoms[x] = min(ys), max(ys)
    return tops, bottoms


def frost(im, tops, bottoms, pose=None):
    """Snow sitting on this cap's real top, icicles hanging from its real rim."""
    def px(x, y, c):
        if 0 <= x < N and 0 <= y < N:
            im.putpixel((x, y), c + (255,))
    xs = sorted(tops)
    left, right = xs[0], xs[-1]
    for f in (0.28, 0.45, 0.62, 0.78):
        x = left + int(round((right - left) * f))
        if x in tops:
            px(x, tops[x] - 1, SNOW)
            if f in (0.45, 0.62):
                px(x + 1, tops.get(x + 1, tops[x]) - 1, SNOW)
    for x in (left + 1, left + 3, right - 1, right - 3):
        if x in bottoms:
            px(x, bottoms[x] + 2, ICE)
            if x in (left + 1, right - 1):
                px(x, bottoms[x] + 3, ICE)
    star(lambda x, y, c, a=255: 0 <= x < N and 0 <= y < N and im.putpixel((x, y), c + (a,)), pose)


def face_on(g, state, S, ox=0, oy=0, **kw):
    """The shared face, moved onto this body and clipped to it — never onto its outline."""
    class Clip:
        def put(self, x, y, c, region=None):
            if 0 <= x < N and 0 <= y < N and g.r[y][x] is not None:
                g.put(x, y, c, region)
    face(Clip(), state, 0, ox=ox, oy=oy, skin=S, **kw)


def body_edges(g, y):
    xs = [x for x in range(N) if g.r[y][x] == "stem"]
    return (min(xs), max(xs)) if xs else (10, 21)


def sweat(im, state, g, oy, k=0):
    """The sweat drops, measured from this model: four pixels outside the body at eye level,
    which is exactly where they have always sat on the Cèpe. `k` slides them down the face;
    None means they have run off this frame."""
    if k is None:
        return
    def px(x, y, c):
        if 0 <= x < N and 0 <= y < N:
            im.putpixel((x, y), c + (255,))
    eye = 19 + oy
    left, right = body_edges(g, eye + 1)
    eye += k
    if state == "hungry":
        xr = right + 4
        for x, y in ((xr, eye - 2), (xr, eye - 1), (xr - 1, eye - 1), (xr, eye)):
            px(x, y, TEAR)
        px(xr, eye - 2, TEAR_HI)
    if state == "starving":
        xr, xl = right + 4, left - 4
        for x, y in ((xr, eye - 1), (xr, eye), (xr - 1, eye), (xr, eye + 1),
                     (xl, eye), (xl, eye + 1), (xl + 1, eye + 1), (xl, eye + 2)):
            px(x, y, TEAR)


def finish(g, p, state, S, face_oy=0, face_ox=0, shadow=True, pose=None, original=False):
    """Face, this frame's motion, ground shadow, the frozen/starving tint, and extras placed for
    this model. `original` is the Cèpe: its face is not clipped, and its sweat and frost sit
    where they always have rather than being measured."""
    pose = pose or pose_for(None, state, 0)
    tops, bottoms = cap_profile(g)
    kw = dict(blink=pose.blink, tear=pose.tear, drool=pose.drool)
    if original:
        face(g, state, 0, **kw)
    else:
        face_on(g, state, S, face_ox, face_oy, **kw)
    g = moved(g, pose)
    im = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    if shadow:
        lift = 2 if pose.hop == 2 else 0       # a smaller shadow under the top of the hop
        for x in range(8 + lift, 24 - lift):
            im.putpixel((x, 29), (0, 0, 0, 58))
        for x in range(10 + lift, 22 - lift):
            im.putpixel((x, 30), (0, 0, 0, 38))
    for y in range(N):
        for x in range(N):
            c = g.c[y][x]
            if c is None:
                continue
            if state == "frozen":
                c = lerp(c, FROST_TINT, 0.45)
            elif state == "starving":
                c = lerp(c, PALE, 0.28)
            im.putpixel((x, y), c + (255,))
    if original:
        extras(im, state, p, pose, pose.body)
    elif state == "frozen":
        frost(im, tops, bottoms, pose)
    elif state in ("hungry", "starving"):
        sweat(im, state, g, face_oy + pose.body[1], sweat_drops(pose))
        if p["sparkle"] and state == "hungry":
            extras(im, "content", p, pose)               # a rare species keeps its sparkle
    else:
        extras(im, state, p, pose)
    return im


# ---- Amanite: a fly agaric ----------------------------------------------------------------

def skirt(g, S, y0, mode):
    """The fly agaric's ring, hanging from row y0. At rest (0) it is three widening rows over a
    toothed hem. It moves like fabric: billowed (1) it is a pixel wider each side with its hem
    lifted, the scallops showing as shading; draped (-1) it hangs a row longer and straighter.
    The halves (0.5, -0.5) are the way back to rest."""
    rows = {                      # (first, last) column of each row, top down, and the teeth
        0:    ([(10, 21), (9, 22), (8, 23)], range(8, 24, 2)),
        1:    ([(10, 21), (8, 23), (7, 24)], ()),
        0.5:  ([(10, 21), (9, 22), (8, 23)], ()),
        -1:   ([(10, 21), (9, 22), (9, 22), (9, 22)], range(10, 22, 2)),
        -0.5: ([(10, 21), (9, 22), (8, 23), (9, 22)], ()),
    }[mode]
    spans, teeth = rows
    for k, (a, b) in enumerate(spans):
        c = S[min(k, 2)]
        for x in range(a, b + 1):
            shade = S[3] if x > 18 else c
            if mode == 1 and k == 2 and x % 2 == 0:
                shade = S[3]                                          # the lifted hem's scallops
            g.put(x, y0 + k, shade, "ring")
    for x in teeth:
        g.put(x, y0 + len(spans), S[3], "ring")                       # its ragged hem


AMANITE_SPOTS = ((9, 5.5, 1.4), (16, 2.5, 1.7), (22, 5, 1.4), (12.5, 8, 0.9), (19, 8, 1.0), (5, 8.2, 0.8), (26, 8, 0.8))


def amanite(state, frame=0, shadow=True, stage="adult"):
    """A wide red dome with white spots, a flared skirt under it, a longer and slimmer stem."""
    if stage == "baby":
        return amanite_baby(state, frame, shadow)
    p = SPECIES["amanite"]
    cap, S = p["cap"], stem_palette(p)
    pose = pose_for("amanite", state, frame)
    sag = 1 if state == "starving" else 0
    g = Grid()
    stem(g, S, 11, 20, 10 + sag, 27)
    feet(g, S)
    skirt(g, S, 11 + sag, pose.skirt)
    gills(g, p, 10 + sag, 4, 27)
    cx, cy, rx, ry = 15.5, 10.0 + sag, 12.4, 9.6
    for y in range(N):
        for x in range(N):
            if y > 9 + sag or ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 > 1:
                continue
            g.put(x, y, shade_cap(x, y, cap, cx, cy, rx, ry, 11, 3.5 + sag, 7, 3.6, 9 + sag), "cap")
    for sx, sy, r in AMANITE_SPOTS:
        for y in range(N):
            for x in range(N):
                if (x - sx) ** 2 + (y - sy - sag) ** 2 <= r * r and g.r[y][x] == "cap" and y < 9 + sag:
                    g.put(x, y, p["spot"][0] if (x - sx) + (y - sy - sag) <= r * 0.55 else p["spot"][1])
    outline(g, cap)
    return finish(g, p, state, S, shadow=shadow, pose=pose)


def amanite_baby(state, frame, shadow):
    """A smaller red dome, its spots scaled down with it, over a two-row skirt that still
    billows and drapes."""
    p = SPECIES["amanite"]
    cap, S = p["cap"], stem_palette(p)
    pose = pose_for("amanite", state, frame)
    sag = 1 if state == "starving" else 0
    g = Grid()
    baby_body(g, S, top=18)
    y0 = 18 + sag                                                     # the skirt, small
    rows = {0: [(10, 21), (9, 22)], 1: [(10, 21), (8, 23)], 0.5: [(10, 21), (9, 22)],
            -1: [(10, 21), (9, 22), (9, 22)], -0.5: [(10, 21), (9, 22), (9, 22)]}[pose.skirt]
    teeth = range(10, 22, 2) if pose.skirt in (0, -1) else ()
    for k, (a, b) in enumerate(rows):
        for x in range(a, b + 1):
            shade = S[3] if x > 18 else S[min(k, 2)]
            if pose.skirt == 1 and k == 1 and x % 2 == 0:
                shade = S[3]                                          # the lifted hem's scallops
            g.put(x, y0 + k, shade, "ring")
    for x in teeth:
        g.put(x, y0 + len(rows), S[3], "ring")
    gills(g, p, 17 + sag, 7, 24)
    cx, cy, rx, ry = 15.5, 17.0 + sag, 10.2, 8.4
    for y in range(17 + sag):
        for x in range(N):
            if ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1:
                g.put(x, y, shade_cap(x, y, cap, cx, cy, rx, ry, 12, 11.5 + sag, 6, 3.2, 16 + sag), "cap")
    for sx, sy, r in scaled(AMANITE_SPOTS, (15.5, 10.0), (15.5, 17.0 + sag), 10.2 / 12.4, 8.4 / 9.6):
        for y in range(N):
            for x in range(N):
                if (x - sx) ** 2 + (y - sy) ** 2 <= r * r and g.r[y][x] == "cap" and y < 16 + sag:
                    g.put(x, y, p["spot"][0] if (x - sx) + (y - sy) <= r * 0.55 else p["spot"][1])
    outline(g, cap)
    return finish(g, p, state, S, face_oy=BABY_FACE_OY, shadow=shadow, pose=pose)


# ---- Rosé des prés: a field mushroom ----------------------------------------------------------

def rose(state, frame=0, shadow=True, stage="adult"):
    """A round button cap with its rim tucked under, pink gills showing, a short thick stem."""
    if stage == "baby":
        return rose_baby(state, frame, shadow)
    p = SPECIES["rose"]
    cap, S = p["cap"], stem_palette(p)
    pose = pose_for("rose", state, frame)
    sag = 1 if state == "starving" else 0
    g = Grid()
    stem(g, S, 10, 21, 15 + sag, 27)
    feet(g, S)
    for x in range(10, 22):
        g.put(x, 18 + sag, S[1] if x < 16 else S[3], "stem")          # a thin ring
    gills(g, p, 15 + sag, 7, 24)
    gills(g, p, 16 + sag, 9, 22)
    cx, cy, rx, ry = 15.5, 10.5 + sag, 13.0, 9.6 + 1.0 * pose.puff       # puffed: its crown rises a row
    for y in range(N):
        for x in range(N):
            if y > 15 + sag or ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 > 1:
                continue
            if y >= 14 + sag and abs(x + 0.5 - cx) < 7:
                continue                                              # the opening under the curled rim
            g.put(x, y, shade_cap(x, y, cap, cx, cy, rx, ry, 10, 5 + sag, 8, 4.4, 14 + sag), "cap")
    outline(g, cap)
    return finish(g, p, state, S, face_oy=1, shadow=shadow, pose=pose)


def rose_baby(state, frame, shadow):
    """A smaller round button, rim tucked under with a peek of pink gills — and no ring yet:
    a field mushroom's ring only forms once its cap opens."""
    p = SPECIES["rose"]
    cap, S = p["cap"], stem_palette(p)
    pose = pose_for("rose", state, frame)
    sag = 1 if state == "starving" else 0
    g = Grid()
    baby_body(g, S)
    gills(g, p, 18 + sag, 9, 22)
    gills(g, p, 19 + sag, 11, 20)
    cx, cy, rx, ry = 15.5, 14.5 + sag, 10.8, 8.2 + 1.0 * pose.puff
    for y in range(19 + sag):
        for x in range(N):
            if ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 > 1:
                continue
            if y >= 18 + sag and abs(x + 0.5 - cx) < 6:
                continue                                              # the opening under the curled rim
            g.put(x, y, shade_cap(x, y, cap, cx, cy, rx, ry, 11, 10 + sag, 6.5, 3.8, 18 + sag), "cap")
    outline(g, cap)
    return finish(g, p, state, S, face_oy=BABY_FACE_OY, shadow=shadow, pose=pose)


# ---- Russule verte: a green russula ------------------------------------------------------------

def russule(state, frame=0, shadow=True, stage="adult"):
    """A broad flat cap dipping in the middle, on a body shaped like the Cèpe's."""
    if stage == "baby":
        return russule_baby(state, frame, shadow)
    p = SPECIES["russule"]
    cap, S = p["cap"], stem_palette(p)
    pose = pose_for("russule", state, frame)
    sag = 1 if state == "starving" else 0
    g = Grid()
    spans = {12: (12, 19), 13: (11, 20), 26: (11, 20), 27: (12, 19)}
    for y in range(12 + sag, 28):
        x0, x1 = spans.get(y - sag if y - sag in (12, 13) else y, (10, 21))
        for x in range(x0, x1 + 1):
            t = (x + 0.5 - x0) / (x1 + 1 - x0)
            nx = 2 * t - 1
            i = tone(nx * -0.6 + math.sqrt(max(0.0, 1 - nx * nx)) * 0.8, (0.86, 0.58, 0.2, -0.2), x, y)
            if y <= 13 + sag or y >= 26:
                i = min(4, i + 1)
            g.put(x, y, S[i], "stem")
    feet(g, S)
    def tip(x):                                                       # the same tilt as the cap's
        return int(round(1.2 * pose.tilt * (x + 0.5 - 15.5) / 14.0))
    gills(g, p, 12 + sag, 4, 27, tip)
    for x in range(2, 30):
        nx = (x + 0.5 - 15.5) / 14.0
        if abs(nx) > 1:
            continue
        lean = 1.2 * pose.tilt * nx                                   # one edge up, the other down
        top = 7.2 + sag + 2.0 * math.exp(-(nx / 0.38) ** 2) + 2.4 * nx ** 4 + lean
        bottom = 11.4 + sag - 0.6 * nx ** 2 + lean
        for y in range(int(top), int(bottom) + 1):
            if y + 0.5 < top:
                continue
            c = shade_cap(x, y, cap, 15.5, 9.5 + sag, 14.0, 3.5, 9, 8 + sag, 7, 1.6, 11 + sag)
            if abs(nx) < 0.3 and y + 0.5 - top < 1.2:
                c = cap[3]                                            # the dip, in shadow
            g.put(x, y, c, "cap")
    outline(g, cap)
    return finish(g, p, state, S, shadow=shadow, pose=pose)


def russule_baby(state, frame, shadow):
    """A smaller flat green plate with its dip, tipping like the adult's, gills and all."""
    p = SPECIES["russule"]
    cap, S = p["cap"], stem_palette(p)
    pose = pose_for("russule", state, frame)
    sag = 1 if state == "starving" else 0
    g = Grid()
    baby_body(g, S)
    half = 10.5

    def tip(x):
        return int(round(1.2 * pose.tilt * (x + 0.5 - 15.5) / half))
    gills(g, p, 19 + sag, 7, 24, tip)
    for x in range(N):
        nx = (x + 0.5 - 15.5) / half
        if abs(nx) > 1:
            continue
        lean = 1.2 * pose.tilt * nx
        top = 13.6 + sag + 1.6 * math.exp(-(nx / 0.38) ** 2) + 2.4 * nx ** 4 + lean
        bottom = 18.4 + sag - 0.6 * nx ** 2 + lean
        for y in range(int(top), int(bottom) + 1):
            if y + 0.5 < top:
                continue
            c = shade_cap(x, y, cap, 15.5, 16.8 + sag, half, 3.0, 10, 15.5 + sag, 5.5, 1.4, 18 + sag)
            if abs(nx) < 0.3 and y + 0.5 - top < 1.2:
                c = cap[3]                                            # the dip, in shadow
            g.put(x, y, c, "cap")
    outline(g, cap)
    return finish(g, p, state, S, face_oy=BABY_FACE_OY, shadow=shadow, pose=pose)


# ---- Mystique: a Mycena ----------------------------------------------------------------------

def mystique(state, frame=0, shadow=True, stage="adult"):
    """A bell with a small darker bump on top and a striped, glowing margin, on a spindle
    stem — slim under the cap and at the foot, full width only where the face is."""
    if stage == "baby":
        return mystique_baby(state, frame, shadow)
    p = SPECIES["mystique"]
    cap, glow, S = p["cap"], p["spot"], stem_palette(p)
    pose = pose_for("mystique", state, frame)
    sag = 1 if state == "starving" else 0
    y0 = 12 + sag
    g = Grid()
    for y in range(y0, 28):                                            # the spindle stem
        k = y - y0
        a, b = (12, 19) if (k <= 3 or y >= 24) else (11, 20)
        shade_row(g, S, y, a, b, "stem", 1 if (k <= 1 or y == 27) else 0)
        for x in (13, 17):                                             # faint fibres
            if k > 2 and (y + x) % 3 != 0 and g.r[y][x] == "stem":
                g.put(x, y, lerp(g.c[y][x], S[0], 0.35))
    feet(g, S)
    gills(g, p, y0, 8, 23)
    apex, rim = 1.0 + sag, 11.5 + sag
    for y in range(N):                                                 # the bell
        t = (y + 0.5 - apex) / (rim - apex)
        if not 0 <= t <= 1:
            continue
        half = 9.0 * math.sin(min(1.0, t * 1.05) * math.pi / 2) ** 0.7
        if t < 0.14:
            half = min(half, 2.2)                                      # the bump (umbo)
        for x in range(N):
            dx = x + 0.5 - CX
            if abs(dx) > half:
                continue
            u = dx / max(half, 0.1)
            c = cap[1] if u < -0.3 else (cap[2] if u < 0.5 else cap[3])
            if t < 0.2:
                c = cap[3] if u > -0.3 else cap[2]                     # the bump is darker
            if t > 0.35 and abs(u * 4 - round(u * 4)) < 0.15:
                c = cap[min(4, cap.index(c) + 1)]                      # striations
            g.put(x, y, c, "cap")
    for x in range(N):                                                 # the glowing rim
        if g.r[int(rim)][x] == "cap":
            g.put(x, int(rim), glow[0] if (x % 3 or pose.glow) else glow[1])
    outline(g, cap)
    im = finish(g, p, state, S, shadow=shadow, pose=pose)
    if state not in ("frozen", "starving"):                            # a soft halo under the rim
        by = pose.body[1]
        for x in range(8, 25, 3):
            hx, hy = x, int(rim) + 1 + by
            if 0 <= hx < N and im.getpixel((hx, hy))[3] == 0:
                im.putpixel((hx, hy), glow[0] + (pose.halo,))
    return im


def mystique_baby(state, frame, shadow):
    """A smaller bell, a little more closed as a young Mycena's is, its rim still glowing and
    its halo still breathing, on a short spindle."""
    p = SPECIES["mystique"]
    cap, glow, S = p["cap"], p["spot"], stem_palette(p)
    pose = pose_for("mystique", state, frame)
    sag = 1 if state == "starving" else 0
    g = Grid()
    for y in range(19 + sag, 29):                                       # a short spindle, round at the foot
        a, b = (12, 19) if (y <= 20 + sag or y >= 27) else (11, 20)
        shade_row(g, S, y, a, b, "stem", 1 if (y <= 20 + sag or y == 28) else 0)
    gills(g, p, 18 + sag, 10, 21)
    apex, rim = 7.0 + sag, 17.5 + sag
    for y in range(N):
        t = (y + 0.5 - apex) / (rim - apex)
        if not 0 <= t <= 1:
            continue
        half = 7.2 * math.sin(min(1.0, t * 1.05) * math.pi / 2) ** 0.8
        if t < 0.14:
            half = min(half, 1.8)                                      # the bump (umbo)
        for x in range(N):
            dx = x + 0.5 - CX
            if abs(dx) > half:
                continue
            u = dx / max(half, 0.1)
            c = cap[1] if u < -0.3 else (cap[2] if u < 0.5 else cap[3])
            if t < 0.2:
                c = cap[3] if u > -0.3 else cap[2]
            if t > 0.35 and abs(u * 3 - round(u * 3)) < 0.15:
                c = cap[min(4, cap.index(c) + 1)]                      # striations
            g.put(x, y, c, "cap")
    for x in range(N):
        if g.r[int(rim)][x] == "cap":
            g.put(x, int(rim), glow[0] if (x % 3 or pose.glow) else glow[1])
    outline(g, cap)
    im = finish(g, p, state, S, face_oy=BABY_FACE_OY, shadow=shadow, pose=pose)
    if state not in ("frozen", "starving"):                            # a soft halo under the rim
        by = pose.body[1]
        for x in range(9, 24, 3):
            hy = int(rim) + 1 + by
            if im.getpixel((x, hy))[3] == 0:
                im.putpixel((x, hy), glow[0] + (pose.halo,))
    return im


# ---- Doré: a chanterelle (girolle) ------------------------------------------------------------

def dore(state, frame=0, shadow=True, stage="adult"):
    """A golden funnel — wavy rim curling down at the edges, forked ridges — narrowing into a
    10 px gold stem, all centred on the face's axis, with a rounded foot."""
    if stage == "baby":
        return dore_baby(state, frame, shadow)
    p = SPECIES["dore"]
    cap, S = p["cap"], stem_palette(p)
    pose = pose_for("dore", state, frame)
    sag = 1 if state == "starving" else 0
    g = Grid()

    def rim_top(x):
        nx = (x + 0.5 - CX) / 13.5
        return (4.2 + sag + 0.7 * pose.ripple * math.sin(x * 0.7 + 0.6) + 0.25 * math.sin(x * 1.6)
                + 3.4 * abs(nx) ** 3)

    def span(y):
        s = (y - (5 + sag)) / 9.0
        if s <= 0:
            return 13.8
        if y >= 27:
            return 3.5                                                 # rounded foot: x 12..19
        if s >= 1:
            return 4.5                                                 # the stem: x 11..20
        return 4.5 + 9.3 * (1 - s) ** 1.7

    blend0, blend1 = 10 + sag, 15 + sag                                # cap gold fades into stem gold
    for y in range(28):
        for x in range(N):
            dx = x + 0.5 - CX
            h = span(y)
            if abs(dx) > h + (1.2 if y < 8 + sag else 0) or y < rim_top(x):
                continue
            u = max(-1.0, min(1.0, dx / h))
            depth = y - rim_top(x)
            if depth < 1.2:
                c = cap[0] if dx < 2 else cap[1]                       # the lit rim
            elif depth < 2.4 and abs(dx) < 8:
                c = cap[2]                                             # the shallow dip on top
            else:
                c = cap[1] if u < -0.3 else (cap[2] if u < 0.45 else cap[3])
            if y >= blend0:
                t = min(1.0, (y - blend0) / (blend1 - blend0))
                i = tone(-0.6 * u + math.sqrt(max(0.0, 1 - u * u)) * 0.8, (0.86, 0.58, 0.2, -0.2), x, y)
                if y >= 26:
                    i = min(4, i + 1)
                c = lerp(c, S[i], t)
            g.put(x, y, c, "cap" if y < blend0 else "stem")
    for k in range(-5, 6):                                             # forked ridges, fading onto the stem
        x_top, x_bot = CX + k * 2.4, CX + k * 0.7
        for y in range(8 + sag, blend1 + 1):
            f = (y - (8 + sag)) / (blend1 - (8 + sag))
            x = int(math.floor(x_top + (x_bot - x_top) * f))
            if g.c[y][x] is not None and y - rim_top(x) > 2.5:
                dark = cap[3] if k < 0 else cap[4]
                g.put(x, y, lerp(dark, g.c[y][x], max(0.0, f - 0.55) * 1.6))
    feet(g, S)
    outline(g, cap, {"stem": lerp(STEM_OUT, cap[4], 0.6)})
    return finish(g, p, state, S, shadow=shadow, pose=pose)


def dore_baby(state, frame, shadow):
    """A smaller golden funnel, its wavy rim still rippling, narrowing straight into the stub."""
    p = SPECIES["dore"]
    cap, S = p["cap"], stem_palette(p)
    pose = pose_for("dore", state, frame)
    sag = 1 if state == "starving" else 0
    g = Grid()

    def rim_top(x):
        nx = (x + 0.5 - CX) / 10.5
        return (9.6 + sag + 0.45 * pose.ripple * math.sin(x * 0.7 + 0.6) + 3.2 * abs(nx) ** 3)

    def span(y):
        s = (y - (10 + sag)) / 9.0
        if s <= 0:
            return 10.6
        if y >= 28:
            return 3.5                                                 # the round stub
        if s >= 1:
            return 4.5
        return 4.5 + 6.1 * (1 - s) ** 1.7

    blend0, blend1 = 15 + sag, 20 + sag                                # cap gold fades into stem gold
    for y in range(29):
        for x in range(N):
            dx = x + 0.5 - CX
            h = span(y)
            if abs(dx) > h + (1.0 if y < 12 + sag else 0) or y < rim_top(x):
                continue
            u = max(-1.0, min(1.0, dx / h))
            depth = y - rim_top(x)
            if depth < 1.2:
                c = cap[0] if dx < 2 else cap[1]                       # the lit rim
            elif depth < 2.2 and abs(dx) < 6:
                c = cap[2]                                             # the shallow dip on top
            else:
                c = cap[1] if u < -0.3 else (cap[2] if u < 0.45 else cap[3])
            if y >= blend0:
                t = min(1.0, (y - blend0) / (blend1 - blend0))
                i = tone(-0.6 * u + math.sqrt(max(0.0, 1 - u * u)) * 0.8, (0.86, 0.58, 0.2, -0.2), x, y)
                if y >= 27:
                    i = min(4, i + 1)
                c = lerp(c, S[i], t)
            g.put(x, y, c, "cap" if y < blend0 else "stem")
    for k in (-3, -1, 1, 3):                                           # a few soft ridges
        x_top, x_bot = CX + k * 2.6, CX + k * 0.9
        for y in range(12 + sag, blend1 + 1):
            f = (y - (12 + sag)) / (blend1 - (12 + sag))
            x = int(math.floor(x_top + (x_bot - x_top) * f))
            if g.c[y][x] is not None and y - rim_top(x) > 2.2:
                dark = cap[2] if k < 0 else cap[3]
                g.put(x, y, lerp(dark, g.c[y][x], max(0.0, f - 0.45) * 1.8))
    outline(g, cap, {"stem": lerp(STEM_OUT, cap[4], 0.6)})
    return finish(g, p, state, S, face_oy=BABY_FACE_OY, shadow=shadow, pose=pose)


DRAW = {"cepe": cepe, "amanite": amanite, "rose": rose, "russule": russule, "mystique": mystique, "dore": dore}
