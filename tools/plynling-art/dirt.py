"""The dirt overlay: a grime tint, mud smudges, three rising stink wisps and a fly on a living Plynling's card picture
when it is « sale ».

Painted on the model grid *before* the face, so the face always sits on top of the mud and the
mud moves with the body through the breath and the hop (nothing moves side to side). Placed from
the drawing itself — its cap and stem regions — so it lands on every species and every stage.
Only the body is painted (never the outline), and the eyes and mouth are left clear.

`build(..., dirty=True)` switches it on through ACTIVE for the one drawing."""
from common import N

ACTIVE = False

MUD = (118, 84, 54)
MUD_DARK = (84, 58, 38)
MUD_LIGHT = (146, 110, 76)
# Small blobs, as (dx, dy, colour) from their anchor.
BLOB = ((0, 0, MUD), (1, 0, MUD_DARK), (-1, 0, MUD_LIGHT), (0, 1, MUD_DARK), (1, 1, MUD), (0, -1, MUD_LIGHT))
# On a cap, darker: a brown cap would swallow the stem's mud.
CAP_BLOB = ((0, 0, MUD_DARK), (1, 0, (64, 44, 30)), (-1, 0, MUD), (0, 1, (64, 44, 30)), (1, 1, MUD_DARK), (0, -1, MUD))
CAP_DOT = ((0, 0, (64, 44, 30)), (1, 0, MUD_DARK))
SPLASH = ((0, 0, MUD_DARK), (1, 0, MUD), (0, -1, MUD), (-1, 1, MUD_LIGHT))
DOT = ((0, 0, MUD_DARK), (1, 0, MUD))


def _body(g, x, y, region):
    """Part of that region — which the outline never is, so the outline stays crisp."""
    return 0 <= x < N and 0 <= y < N and g.r[y][x] == region


def _span(g, y, region):
    xs = [x for x in range(N) if g.r[y][x] == region]
    return (min(xs), max(xs)) if xs else None


def muddy(g, face_ox=0, face_oy=0):
    """Stamps the mud on grid g, leaving the eyes and the mouth clear. The face is drawn
    afterwards, so it covers any mud that reaches its blush or brows."""
    def clear(x, y):
        x, y = x - face_ox, y - face_oy
        eyes = 18 <= y <= 22 and (11 <= x <= 14 or 17 <= x <= 20)
        mouth = 23 <= y <= 26 and 12 <= x <= 19
        return not (eyes or mouth)

    def stamp(ax, ay, shape, region):
        for dx, dy, c in shape:
            x, y = ax + dx, ay + dy
            if _body(g, x, y, region) and clear(x, y):
                g.put(x, y, c, region)

    caps = [y for y in range(N) if _span(g, y, "cap")]
    if caps:
        y = caps[0] + (caps[-1] - caps[0]) * 2 // 3                    # the lower, wider part of the cap
        left, right = _span(g, y, "cap")
        stamp(left + (right - left) // 4, y, CAP_BLOB, "cap")
        stamp(left + (right - left) * 3 // 4, y - 1, CAP_BLOB, "cap")
        top = caps[0] + max(1, (caps[-1] - caps[0]) // 3)              # and one higher up
        if (r := _span(g, top, "cap")):
            stamp((r[0] + r[1]) // 2 + 1, top, CAP_DOT, "cap")
    stems = [y for y in range(N) if _span(g, y, "stem")]
    if stems:
        # a streak running down from under the brim, on the left
        left, _ = _span(g, stems[0], "stem")
        for dy, c in ((0, MUD_DARK), (1, MUD), (2, MUD), (3, MUD_LIGHT)):
            stamp(left + 1, stems[0] + dy, ((0, 0, c),), "stem")
        low = stems[-1] - 1                                             # splashes on both feet
        left, right = _span(g, low, "stem")
        stamp(left + 1, low, BLOB, "stem")
        stamp(right - 1, low, SPLASH, "stem")


# A brownish grime over the whole Plynling, so « sale » reads at a glance even where no mud landed.
GRIME, GRIME_SHARE = (132, 104, 72), 0.16

STINK = (128, 158, 74)
# A wisp: a little zigzag three rows tall, which reads as a wavy line at this size.
WISP = ((0, 0), (1, 1), (0, 2), (1, 3))
FLY, WING = (96, 92, 104), (226, 234, 242)       # mid-grey, so it reads on a dark theme too


def stink(im, f, fly=None):
    """Three stink wisps rising straight up beside the body's left flank, from the ground to just
    under the brim, fading as they climb, a third of a loop apart — and a fly hovering above them,
    bobbing up and down on the spot. The left, because the right already carries the heart, the
    sweat drop and the « z »s; and beside the body because the caps fill the top of the picture.
    Measured from the drawing, so they fit every species and stage. Nothing moves sideways."""
    px = im.load()
    solid = lambda x, y: 0 <= x < N and 0 <= y < N and px[x, y][3] == 255

    def put(x, y, c, a=255):
        if 0 <= x < N and 0 <= y < N and not solid(x, y):              # never over the Plynling
            px[x, y] = c + (a,)

    left = next((x for x in range(N) if solid(x, 24)), None)
    if left is None:
        return
    if fly is not None:
        x, y = fly
        y -= 1 if f % 4 < 2 else 0                                      # hopping up a pixel and back, in place
        put(x, y, FLY)                                                  # a body under two wings
        put(x + 1, y, FLY)
        put(x, y - 1, WING)
        put(x + 1, y - 1, WING)

    bottom = 27
    ceiling_at = lambda x: max((y for y in range(bottom) if solid(x, y) or solid(x + 1, y)), default=-1)
    for x, phase in ((left - 3, 0), (left - 6, 5), (left - 9, 11)):
        if x < 0:
            continue
        room = bottom - ceiling_at(x) - 4                               # rows the wisp can climb
        if room < 3:
            continue
        k = (f + phase) % 16                                            # 0..15 along its climb
        y0 = bottom - 3 - k * room // 16
        a = 255 if k < 9 else max(60, 255 - (k - 8) * 28)               # fading near the top
        for dx, dy in WISP:
            put(x + dx, y0 + dy, STINK, a)


_spots = {}


def fly_spot(g, key):
    """Where the fly hovers — found once per animation (`key`) on its first frame, the rest pose,
    so a species whose shape changes along the loop (the Amanite's skirt) cannot move it. It is
    in open air left of the body, as near as possible to just under the brim, searched on the model
    before the breath and the hop move it, with a margin wide enough that neither can reach it. It
    is drawn before the wisps, and opaque, so they go round it: it stays whole all loop."""
    if key in _spots:
        return _spots[key]
    solid = lambda x, y: 0 <= x < N and 0 <= y < N and g.c[y][x] is not None
    left = next((x for x in range(N) if solid(x, 24)), None)
    if left is None:
        return None

    def open_air(x, y):
        return all(not solid(x + dx, y + dy) for dx in range(-1, 3) for dy in range(-4, 3))
    # above eye level (row 17 at the latest): the starving cold sweat hangs beside the body lower
    # down. A cap too wide to leave room under its brim sends it to the free corner above.
    spots = [(x, y) for y in range(3, 18) for x in range(0, left - 2) if open_air(x, y)]
    _spots[key] = (min(spots, key=lambda s: (abs(s[0] - (left - 5)) + abs(s[1] - 15), s)) if spots else None)
    return _spots[key]
