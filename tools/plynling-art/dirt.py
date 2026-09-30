"""The dirt overlay: mud smudges and two rising stink wisps on a living Plynling's card picture
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
        stamp(left + (right - left) * 3 // 4, y - 1, CAP_DOT, "cap")
    stems = [y for y in range(N) if _span(g, y, "stem")]
    if stems:
        low = stems[-1] - 1                                             # a splash on the feet
        left, right = _span(g, low, "stem")
        stamp(left + 1, low, SPLASH, "stem")
        stamp(right - 2, low, DOT, "stem")


STINK = (150, 168, 104)
# A wisp: a little zigzag three rows tall, which reads as a wavy line at this size.
WISP = ((0, 0), (1, 1), (0, 2), (1, 3))


def stink(im, f):
    """Two stink wisps rising straight up beside the body's left flank, from the ground to just
    under the brim, fading as they climb, half a loop apart. The left, because the right already
    carries the heart, the sweat drop and the « z »s; and beside the body because the caps fill
    the top of the picture. Measured from the drawing, so they fit every species and stage."""
    px = im.load()
    solid = lambda x, y: 0 <= x < N and 0 <= y < N and px[x, y][3] == 255
    left = next((x for x in range(N) if solid(x, 24)), None)
    if left is None:
        return
    bottom = 27
    for x, phase in ((left - 4, 0), (left - 7, 8)):
        if x < 0:
            continue
        ceiling = max((y for y in range(bottom) if solid(x, y) or solid(x + 1, y)), default=-1)
        room = bottom - ceiling - 4                                     # rows the wisp can climb
        if room < 3:
            continue
        k = (f + phase) % 16                                            # 0..15 along its climb
        y0 = bottom - 3 - k * room // 16
        a = 220 if k < 9 else max(40, 220 - (k - 8) * 28)               # fading near the top
        for dx, dy in WISP:
            if not solid(x + dx, y0 + dy):                              # never over the Plynling
                px[x + dx, y0 + dy] = STINK + (a,)
